using System;
using System.IO;
using System.Reflection;
using System.Text;
using FitFight.BodyTracking;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;

public static class FullBodyTestValidation
{
    private static int checks;
    private static readonly StringBuilder report = new StringBuilder();
    public static void Run()
    {
        checks = 0; report.Clear();
        try { Guide(); Snapshot(); Recording(); report.AppendLine($"PASS: {checks} checks."); }
        catch (Exception e) { report.AppendLine("FAIL: " + e); throw; }
        finally { Directory.CreateDirectory("Logs/Practice"); File.WriteAllText("Logs/Practice/full-body-validation.txt", report.ToString()); }
    }
    private static void Check(bool value, string description)
    {
        if (!value) throw new Exception(description);
        checks++; report.AppendLine("PASS " + description);
    }
    private static void Guide()
    {
        var guide = new FullBodyRecordingGuide();
        int count = 0; double previous = -1; var spoken = new StringBuilder();
        for (double time = 0; time < FullBodyRecordingGuide.Duration; time += .25)
        {
            if (!guide.TryAdvance(time, out var cue)) continue;
            Check(cue.at > previous && FullBodyRecordingGuide.Label(time) == cue.label,
                "Each body instruction is ordered and matches the visible cue: " + cue.label);
            Check(!string.IsNullOrWhiteSpace(cue.speech) && !guide.TryAdvance(time + .01, out _),
                "Each body stage speaks once: " + cue.label);
            previous = cue.at; count++; spoken.Append(cue.speech);
        }
        Check(count == 23 && !guide.TryAdvance(FullBodyRecordingGuide.Duration, out _), "Four-minute guide completes without repeating a stage");
        foreach (string movement in new[] { "above your head", "to the sides", "Bend both elbows", "torso forward", "torso left", "shoulders left", "left knee", "right knee", "left leg", "right leg", "squat", "heels", "out of the camera" })
            Check(spoken.ToString().Contains(movement), "Spoken guide includes " + movement);
        guide.Reset(); Check(guide.TryAdvance(0, out _), "Restart resets the body guide");
        Check(guide.TryAdvance(62, out var latest) && latest.at == 62 && !guide.TryAdvance(62.1, out _),
            "Delayed frames speak the current exercise without queuing expired instructions");
    }
    private static void Snapshot()
    {
        var obj = new GameObject("Accepted landmark snapshot test");
        try
        {
            var provider = obj.AddComponent<RTMPosePoseProvider>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var accepted = (Vector2[])typeof(RTMPosePoseProvider).GetField("acceptedImage", flags).GetValue(provider);
            var confidence = (float[])typeof(RTMPosePoseProvider).GetField("acceptedImageScores", flags).GetValue(provider);
            accepted[25] = new Vector2(.42f, .08f); confidence[25] = .87f;
            var points = new Vector2[26]; var scores = new float[26]; provider.CopyAcceptedImage(points, scores);
            provider.Points[25] = Vector2.one; provider.Scores[25] = 0;
            Check(points[25] == accepted[25] && scores[25] == .87f, "Snapshot includes the last heel and uses accepted rather than pending inference data");
            points[25] = Vector2.zero; scores[25] = 0;
            Check(accepted[25].x == .42f && confidence[25] == .87f, "Recorder cannot mutate provider landmark buffers");
            bool rejected = false; try { provider.CopyAcceptedImage(new Vector2[25], scores); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Incomplete body capture arrays are rejected");
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
    }
    private static void Recording()
    {
        var root = new GameObject("Body record test"); var r = new PunchSessionRecorder("{\"kind\":\"session\",\"test\":\"full_body\",\"schemaVersion\":2}");
        try
        {
            var points = new Vector2[26]; var scores = new float[26]; points[25] = new Vector2(.3f, .12f); scores[25] = .9f;
            r.Observation(new PunchCameraInput.Frame { sequence=77, jointLayout="Halpe26", imageJoints=points, jointScores=scores, cameraRotation=90, imageAspect=.5625f }, null, null);
            var guide = new FullBodyRecordingGuide(); guide.TryAdvance(62, out var cue); r.Cue("full_body", cue, 100, 162.1);
            r.Pose(root.transform, new[] { root.transform }, root.transform, root.transform, "Mirroring", 0, 77, 0, 0, 0,
                new PunchSessionRecorder.PoseContext { poseOwner="DirectTracking", leftFoot=new Vector3(1,2,3),
                    leftContact=new PracticeHeavyBag.ContactSample { gap=.2f, reason="No surface contact" } });
            points[25] = Vector2.zero; r.Dispose();
            double deadline = EditorApplication.timeSinceStartup + 3;
            while (!r.Finished && EditorApplication.timeSinceStartup < deadline) System.Threading.Thread.Sleep(10);
            Check(r.Finished && r.Error == null && r.Dropped == 0, "Body session drains all diagnostic records");
            string[] lines = File.ReadAllLines(r.Path);
            Check(lines.Length == 5 && lines[1].Contains("\"leftState\":\"Disabled\"") && lines[1].Contains("\"jointLayout\":\"Halpe26\"") && lines[1].Contains("\"x\":0.3"),
                "Body observations serialize all model joints without requiring punch detectors");
            Check(lines[2].Contains("\"scheduledAt\":162") && lines[2].Contains("\"recordedAt\":162.1") && lines[2].Contains("torso forward"),
                "Speech scheduling and actual enqueue time are recorded separately");
            Check(lines[3].Contains("\"leftFoot\":{\"x\":1.0") && lines[3].Contains("DirectTracking") && lines[3].Contains("No surface contact"),
                "Final pose records feet, owner and contact rejection context");
        }
        finally { r.Dispose(); UnityEngine.Object.DestroyImmediate(root); }
    }
}
