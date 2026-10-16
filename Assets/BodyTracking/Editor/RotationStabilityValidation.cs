using System;
using System.IO;
using System.Text;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

/// <summary>Deterministic stability/response checks; no camera or device-speed claims.</summary>
public static class RotationStabilityValidation
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
        Report.AppendLine("PASS " + message);
    }
    [MenuItem("Tools/FitFight/RTMPose/Validate rotation stability")]
    public static void Run()
    {
        Report.Clear(); checks = 0;
        try
        {
            var scalar = new FitFight.BodyTracking.OneEuroFilterFloat();
            scalar.Filter(0, 0);
            float value = scalar.Filter(1, .1);
            Check(scalar.Filter(100, .1) == value && scalar.Filter(100, 0) == value, "Scalar ignores duplicate/backward time");
            Check(scalar.Filter(float.NaN, .2) == value, "Scalar rejects nonfinite input");
            scalar.Reset(); Check(scalar.Filter(3, 1) == 3, "Scalar reset discards history");
            var q = Quaternion.Euler(20, 175, -15);
            var filter = new FitFight.BodyTracking.OneEuroFilterQuaternion();
            filter.Filter(q, 0);
            var negative = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            Check(Quaternion.Angle(q, filter.Filter(negative, .1)) < .05f, "q and -q describe the same rotation");
            Check(Quaternion.Angle(q, filter.Filter(Quaternion.identity, .1)) < .05f, "Rotation ignores duplicate time");
            Check(Quaternion.Angle(q, filter.Filter(Quaternion.identity, -.1)) < .05f, "Rotation ignores backward time");
            Check(Quaternion.Angle(q, filter.Filter(new Quaternion(), .2)) < .05f, "Zero quaternion rejected");
            Check(Quaternion.Angle(q, filter.Filter(new Quaternion(float.NaN, 0, 0, 1), .2)) < .05f, "NaN quaternion rejected");
            Check(Quaternion.Angle(q, filter.Filter(Quaternion.identity, double.PositiveInfinity)) < .05f, "Nonfinite time rejected");
            filter.Reset(); Check(Quaternion.Angle(Quaternion.identity, filter.Filter(Quaternion.identity, 1)) < .05f, "Rotation reset seeds new pose");
            foreach (var bone in new[] { HumanBodyBones.Chest, HumanBodyBones.Head, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftUpperLeg })
            foreach (int hz in new[] { 30, 60, 120 })
            {
                var preset = BoneFilterPresets.ForBone(bone);
                filter = new FitFight.BodyTracking.OneEuroFilterQuaternion(preset.minCutoff, preset.beta);
                filter.Filter(Quaternion.identity, 0);
                float energy = 0;
                for (int i = 1; i <= hz * 2; i++)
                {
                    var result = filter.Filter(Quaternion.AngleAxis(i % 2 == 0 ? 1 : -1, Vector3.up), i / (double)hz);
                    float angle = Quaternion.Angle(Quaternion.identity, result);
                    energy += angle * angle;
                    if (i == hz * 2) Check(Mathf.Abs(Quaternion.Dot(result, result) - 1) < .0001f, bone + " unit quaternion at " + hz + " Hz");
                }
                float rms = Mathf.Sqrt(energy / (hz * 2));
                Check(rms < .5f, bone + " reduces ±1° rest jitter: RMS=" + rms.ToString("F3") + " at " + hz + " Hz");
                // Measure response as well as jitter: a frozen output cannot pass.
                var target = Quaternion.AngleAxis(90, Vector3.up);
                Quaternion response = Quaternion.identity;
                for (int i = 1; i <= Mathf.CeilToInt(hz * .2f); i++) response = filter.Filter(target, 2 + i / (double)hz);
                Check(Quaternion.Angle(response, target) < 15, bone + " reaches within 15° of a 90° step in 200 ms at " + hz + " Hz");
            }
            foreach (int hz in new[] { 30, 60, 120 })
            {
                var state = new BoneRotationFilter(HumanBodyBones.LeftUpperArm);
                var target = Quaternion.AngleAxis(90, Vector3.up);
                Quaternion previous = state.Step(target, Quaternion.identity, 0, false, true, 180);
                bool bounded = true;
                for (int i = 1; i <= hz; i++)
                {
                    var result = state.Step(target, Quaternion.identity, i / (double)hz, false, true, 180);
                    bounded &= Quaternion.Angle(previous, result) <= 180f / hz + .06f; previous = result;
                }
                Check(bounded && Quaternion.Angle(previous, target) < .05f, "Speed cap and target convergence at " + hz + " Hz");
                state.Hold();
                var nextTarget = Quaternion.AngleAxis(-90, Vector3.up);
                var resumed = state.Step(nextTarget, previous, 10, true, true, 180);
                Check(Quaternion.Angle(previous, resumed) <= 9.06f, "Long gap cannot buy more than 50 ms of catch-up at " + hz + " Hz");
                Check(Quaternion.Angle(resumed, state.Step(target, previous, 10, true, true, 180)) < .05f, "Duplicate render time cannot advance cap");
                state.Reset(); var handoff = Quaternion.Euler(40, 10, 20);
                Check(Quaternion.Angle(handoff, state.Step(target, handoff, 11, true, true, 180)) < .05f, "Animation handoff seeds visible pose");
            }
            var raw = new BoneRotationFilter(HumanBodyBones.Head);
            raw.Step(Quaternion.identity, Quaternion.identity, 0, false, false, 180);
            Check(Quaternion.Angle(q, raw.Step(q, Quaternion.identity, .1, false, false, 180)) < .05f, "Disabled stages pass targets unchanged after seeding");
            var tuning = new RTMPoseTuning();
            JsonUtility.FromJsonOverwrite("{\"directAvatar\":true}", tuning);
            Check(!tuning.directRotationSmoothing && !tuning.directRotationSpeedLimit && tuning.directMaxDegreesPerSecond == 1080, "Old saved settings preserve A/B opt-in defaults");
            tuning.directMaxDegreesPerSecond = float.NaN; tuning.Clamp();
            Check(tuning.directMaxDegreesPerSecond == 1080, "Invalid speed setting repaired");
            Report.AppendLine("PASS: " + checks + " checks. Synthetic only; device A/B acceptance remains pending.");
        }
        catch (Exception e) { Report.AppendLine("FAIL " + e); throw; }
        finally
        {
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/rotation-stability-validation.txt", Report.ToString());
            Debug.Log(Report.ToString());
        }
    }
    public static void RunBatch()
    {
        try { Run(); PunchValidation.Run(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
