using System;
using System.IO;
using System.Text;
using FitFight.BodyTracking;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RTMPoseValidation
{
    [MenuItem("Tools/FitFight/RTMPose/Run validation")]
    public static void Run()
    {
        var report = new StringBuilder();
        try
        {
            var xs = new float[26 * 384]; var ys = new float[26 * 512];
            var points = new Vector2[26]; var scores = new float[26];
            for (int i = 0; i < 26; i++) { xs[i * 384 + 192] = .9f; ys[i * 512 + 256] = .8f; }
            RTMPoseMath.Decode(xs, ys, new Rect(.2f, .1f, .6f, .8f), points, scores);
            Check(Vector2.Distance(points[25], new Vector2(.5f, .5f)) < .0001f && Mathf.Abs(scores[25] - .8f) < .001f, "SimCC decode and inverse ROI", report);
            bool rejected = false;
            try { RTMPoseMath.Decode(new float[17 * 384], ys, new Rect(), points, scores); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "Reject COCO17 model output", report);
            Array.Clear(scores, 0, scores.Length);
            var roi = RTMPoseMath.TrackROI(points, scores, 16f / 9f);
            Check(roi.xMin <= 0 && roi.yMin <= 0 && roi.xMax >= 1 && roi.yMax >= 1,
                "Lost person reacquires full image", report);
            var filter = new PoseJointFilter();
            filter.Step(Vector3.zero, 1, 1, .02f, out _);
            filter.Step(Vector3.right * .1f, 1, 1.02, .02f, out _);
            var predicted = filter.Step(Vector3.zero, 0, 1.06, .02f, out float valid);
            Check(valid > 0 && RTMPoseMath.Finite(predicted) && predicted.magnitude < .3f, "Short occlusion uses bounded prediction", report);
            filter.Step(Vector3.zero, 0, 1.3, .02f, out valid);
            Check(valid == 0, "Long occlusion expires", report);
            Check(RTMPoseMath.ToMediaPipe[29] == 24 && RTMPoseMath.ToMediaPipe[30] == 25
                && RTMPoseMath.ToMediaPipe[31] == 20 && RTMPoseMath.ToMediaPipe[32] == 21, "Halpe heel/toe mapping", report);

            var tuning = new RTMPoseTuning { filtering = false, prediction = .025f };
            filter.Reset();
            filter.Step(Vector3.zero, 1, 2, 0, tuning, out _);
            var onset = filter.Step(Vector3.right * .1f, 1, 2.02, .025f, tuning, out _);
            Check(onset.x >= .1f && onset.x <= .22f, "Movement onset responds in the current sample", report);
            var stopped = filter.Step(Vector3.right * .1f, 1, 2.04, .025f, tuning, out _);
            Check(Mathf.Abs(stopped.x - .1f) < .0001f, "Prediction stops when the measurement stops", report);
            var reversed = filter.Step(Vector3.right * .05f, 1, 2.06, .025f, tuning, out _);
            Check(reversed.x < .05f, "Prediction changes direction with a reversal", report);
            filter.Predict(2.3, .025f, tuning, out valid);
            Check(valid == 0, "Missing joints expire instead of completing a kick", report);
            Check(RTMPosePoseProvider.ReliableChain(.6f, .5f, .4f, .3f), "Leg reliability requires hip, knee and ankle only", report);
            Check(!RTMPosePoseProvider.ReliableChain(.6f, .2f, .4f, .3f), "Missing knee cannot claim a tracked leg", report);
            float d30 = 0, d60 = 0;
            for (int i = 0; i < 3; i++) d30 = Skeleton25DSolver.AdvanceDepth(d30, 2, 1f / 30, 0, tuning);
            for (int i = 0; i < 6; i++) d60 = Skeleton25DSolver.AdvanceDepth(d60, 2, 1f / 60, 0, tuning);
            Check(Mathf.Abs(d30 - d60) < .0001f, "Depth response independent of inference frame rate", report);
            Check(RTMPosePoseProvider.ObservationFresh(1.12, 1, 1.12, .08f), "120 ms inference remains usable on delivery", report);
            Check(!RTMPosePoseProvider.ObservationFresh(1.23, 1, 1.12, .08f), "Delivery grace expires without another pose", report);
            Check(!RTMPosePoseProvider.ObservationFresh(1.3, 1, 1.29, .15f), "Absolute observation age stays bounded", report);
            var acquisition = new Skeleton25DSolver();
            var weakScores = new float[26]; for (int i = 0; i < 26; i++) weakScores[i] = .4f;
            Check(!acquisition.Solve(new Vector2[26], weakScores, 1, 1, 1.7f, 20, 0, new Vector3[26], new float[26]),
                "Weak background detections cannot start calibration", report);
            Check(RTMPosePoseProvider.DiagnosticObservationFresh(1.4, 1, 1.4, .4f)
                && !RTMPosePoseProvider.ObservationFresh(1.4, 1, 1.4, .08f),
                "Slow inference remains visible for diagnosis without driving the avatar", report);
            Check(!RTMPosePoseProvider.DiagnosticObservationFresh(2.2, 1, 1.4, .4f),
                "Delayed diagnostic landmarks also expire", report);
            Check(RTMPosePoseProvider.IsNewCameraFrame(false, 0, 0), "First camera notification schedules inference", report);
            Check(!RTMPosePoseProvider.IsNewCameraFrame(true, 5, 5), "Repeated notification does not duplicate inference", report);
            Check(RTMPosePoseProvider.IsNewCameraFrame(true, 5, 6), "Next delivered frame schedules without Texture.updateCount", report);
            var steady = new RTMPoseTuning { minCutoff = 2, speedCoefficient = 0, prediction = 0 };
            filter.Reset(); filter.Step(Vector3.zero, 1, 0, 0, steady, out _);
            float filteredEnergy = 0;
            for (int i = 1; i <= 120; i++)
            {
                float raw = i % 2 == 0 ? .01f : -.01f;
                var value = filter.Step(Vector3.right * raw, 1, i / 60.0, 0, steady, out _);
                filteredEnergy += value.x * value.x;
            }
            Check(filteredEnergy < 120 * .0001f * .1f, "Small direction reversals remain smoothed", report);
            float alphaRest = Tracking4All.SimpleSmooth.AdaptiveAlpha(.002f, 1f / 60, 2, 20);
            float alphaMove = Tracking4All.SimpleSmooth.AdaptiveAlpha(.3f, 1f / 60, 2, 20);
            Check(alphaMove > alphaRest * 2 && alphaRest > 0 && alphaMove < 1, "Baseline smooths rest noise and opens for movement", report);
            var rotation = DirectHumanoidRetargeter.AimRotation(Vector3.right, Vector3.forward, Quaternion.identity);
            Check(Vector3.Distance(rotation * Vector3.right, Vector3.forward) < .0001f, "Direct retarget rotates the bind axis onto the measured limb", report);

            ValidateFloorCamera(report);
            ValidateStableMotion(report);
            ValidateFullBodyDirections(report);

            var home = FitFightSelectionView.Create(out Button adam, out Button remy);
            try
            {
                Check(home.GetComponentsInChildren<Toggle>().Length == 5 && adam != null && remy != null && home.GetComponent<FitFightSelectionView>().PracticeButton != null,
                    "Home contains all five tracking choices and fighter buttons", report);
                BodyTrackingSelector.Create(home.transform);
                Check(home.GetComponentsInChildren<Toggle>().Length == 5, "Selector attachment is idempotent", report);
            }
            finally { UnityEngine.Object.DestroyImmediate(home); }

            var asset = Resources.Load<ModelAsset>("RTMPose/Body26");
            Check(asset != null, "Bundled ONNX model imported", report);
            var shader = Resources.Load<ComputeShader>("RTMPose/Preprocess");
            Check(shader != null && SystemInfo.supportsComputeShaders, "GPU preprocessing available", report);
            using var input = new Tensor<float>(new TensorShape(1, 3, 256, 192));
            int kernel = shader.FindKernel("Preprocess");
            shader.SetTexture(kernel, "CameraTexture", Texture2D.blackTexture);
            shader.SetVector("ROI", new Vector4(0, 0, 1, 1));
            shader.SetInt("Rotation", 0); shader.SetInt("VerticalFlip", 0);
            shader.SetBuffer(kernel, "Input", ComputeTensorData.Pin(input).buffer);
            shader.Dispatch(kernel, 24, 32, 1);
            using (var cpu = input.ReadbackAndClone())
                Check(Mathf.Abs(cpu[0] - (-123.675f / 58.395f)) < .001f
                    && Mathf.Abs(cpu[49152] - (-116.28f / 57.12f)) < .001f
                    && Mathf.Abs(cpu[98304] - (-103.53f / 57.375f)) < .001f, "GPU RGB normalization and NCHW layout", report);
            // A real inference smoke test. Timing here includes cold startup and is not a benchmark.
            using var worker = new Worker(ModelLoader.Load(asset), BackendType.GPUCompute);
            worker.Schedule(input);
            using var x = (worker.PeekOutput("simcc_x") as Tensor<float>).ReadbackAndClone();
            using var y = (worker.PeekOutput("simcc_y") as Tensor<float>).ReadbackAndClone();
            Check(x.shape == new TensorShape(1, 26, 384) && y.shape == new TensorShape(1, 26, 512), "Real GPU inference: Body26 output shapes", report);
            RTMPoseMath.Decode(x.DownloadToArray(), y.DownloadToArray(), new Rect(0, 0, 1, 1), points, scores);
            for (int i = 0; i < 26; i++) Check(RTMPoseMath.Finite(scores[i]), "Finite confidence " + i, report);
            var readback = new Body26Readback();
            for (int pass = 0; pass < 3; pass++)
            {
                worker.Schedule(input);
                readback.Begin(worker.PeekOutput("simcc_x") as Tensor<float>, worker.PeekOutput("simcc_y") as Tensor<float>);
                UnityEngine.Rendering.AsyncGPUReadback.WaitAllRequests();
                Check(readback.Done && !readback.HasError, "Callback readback completed, pass " + pass, report);
                RTMPoseMath.Decode(readback.X, readback.Y, new Rect(0, 0, 1, 1), points, scores);
                Check(readback.X.Length == 9984 && readback.Y.Length == 13312, "Pooled GPU buffers decoded at logical lengths", report);
            }
            ValidateReferencePerson(worker, shader, input, kernel, report);
            report.AppendLine("PASS: all checks completed.");
        }
        catch (Exception e) { report.AppendLine("FAIL: " + e); Debug.LogException(e); }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/RTMPose-validation.txt", report.ToString());
        Debug.Log(report.ToString());
    }
    // Project a known upright body through a low, upward-facing pinhole camera.
    // This is independent of the inverse mapping used by the runtime solver.
    public static Vector2[] FloorFixture(float pitch, bool kick = false, bool punch = false, float squat = 0, float bend = 0, float headTurn = 0, int armPose = 0, float sideBend = 0, bool rightKick = false)
    {
        var body = new Vector3[26];
        body[11] = new Vector3(-.14f, .9f, 0); body[12] = new Vector3(.14f, .9f, 0);
        body[5] = new Vector3(-.2f, 1.393f, 0); body[6] = new Vector3(.2f, 1.393f, 0);
        body[7] = new Vector3(-.23f, 1.12f, -.12f); body[8] = new Vector3(.23f, 1.12f, -.12f);
        body[9] = new Vector3(-.18f, 1.34f, -.25f); body[10] = new Vector3(.18f, 1.34f, -.25f);
        for (int side = 0; side < 2; side++)
        {
            float x = side == 0 ? -.14f : .14f;
            body[13 + side] = new Vector3(x, .49f, 0);
            body[15 + side] = new Vector3(x, .08f, 0);
            body[20 + side] = body[22 + side] = new Vector3(x, .03f, -.14f);
            body[24 + side] = new Vector3(x, .03f, .04f);
        }
        body[0] = new Vector3(0, 1.62f, -.08f);
        body[1] = new Vector3(-.03f, 1.65f, -.07f); body[2] = new Vector3(.03f, 1.65f, -.07f);
        body[3] = new Vector3(-.075f, 1.62f, 0); body[4] = new Vector3(.075f, 1.62f, 0);
        body[17] = new Vector3(0, 1.70f, 0); body[18] = new Vector3(0, 1.46f, 0); body[19] = new Vector3(0, .9f, 0);
        if (kick)
        {
            int side = rightKick ? 1 : 0; float x = rightKick ? .14f : -.14f;
            body[13 + side] = new Vector3(x, 1.04f, -.39f);
            body[15 + side] = new Vector3(x, .94f, -.79f);
            body[20 + side] = body[22 + side] = body[15 + side] + new Vector3(0, -.04f, -.14f);
            body[24 + side] = body[15 + side] + new Vector3(0, -.04f, .04f);
        }
        if (punch) { body[7] = new Vector3(-.2f, 1.393f, -.316f); body[9] = new Vector3(-.2f, 1.393f, -.564f); }
        // Known physical arm directions: overhead, T pose, and bent elbows with hands up.
        for (int side = 0; side < 2 && armPose != 0; side++)
        {
            float sign = side == 0 ? -1 : 1;
            Vector3 shoulder = body[5 + side];
            body[7 + side] = shoulder + (armPose == 1 ? Vector3.up : Vector3.right * sign) * .316f;
            body[9 + side] = body[7 + side] + (armPose == 2 ? Vector3.right * sign : Vector3.up) * .248f;
        }
        // Independent physical transformations, projected by the camera below.
        Vector3 facePivot = (body[3] + body[4]) * .5f;
        for (int i = 0; i < 5; i++) body[i] = facePivot + Quaternion.Euler(0, headTurn, 0) * (body[i] - facePivot);
        if (squat > 0)
        {
            for (int i = 0; i <= 12; i++) body[i].y -= squat;
            for (int i = 17; i <= 19; i++) body[i].y -= squat;
            for (int i = 13; i <= 14; i++) { body[i].y -= squat * .3f; body[i].z -= squat; }
        }
        Vector3 pelvis = (body[11] + body[12]) * .5f;
        for (int i = 0; i <= 10; i++) body[i] = pelvis + Quaternion.Euler(-bend, 0, sideBend) * (body[i] - pelvis);
        for (int i = 17; i <= 18; i++) body[i] = pelvis + Quaternion.Euler(-bend, 0, sideBend) * (body[i] - pelvis);
        var points = new Vector2[26];
        float focal = .5f / Mathf.Tan(30 * Mathf.Deg2Rad);
        for (int i = 0; i < 26; i++)
        {
            Vector3 camera = Quaternion.Euler(pitch, 0, 0) * (body[i] + Vector3.forward * 2.8f);
            points[i] = new Vector2(.5f + focal * camera.x / (camera.z * .5625f), .5f - focal * camera.y / camera.z);
        }
        return points;
    }
    private static void ValidateFloorCamera(StringBuilder report)
    {
        var tuning = new RTMPoseTuning { filtering = false, prediction = 0, uprightBody = true };
        var confidence = new float[26]; var scores = new float[26]; var result = new Vector3[26];
        for (int i = 0; i < 26; i++) scores[i] = .9f;
        foreach (float pitch in new[] { 0f, 20f, 40f })
        {
            var solver = new Skeleton25DSolver();
            var standing = FloorFixture(pitch);
            for (int frame = 0; frame < 25; frame++)
                solver.Solve(standing, scores, .5625f, frame / 30.0, 1.7f, pitch, 0, result, confidence, tuning);
            Check(solver.Calibrated, $"Floor camera {pitch} degrees calibrates", report);
            Vector3 up = (result[5] + result[6] - result[11] - result[12]) * .5f;
            Check(Vector3.Angle(up, Vector3.up) < .01f && Mathf.Abs(result[5].y - result[6].y) < .001f,
                $"Floor camera {pitch}: upright torso and level shoulders", report);
            Check(Mathf.Abs(result[15].z) < .001f && Mathf.Abs(result[16].z) < .001f
                && Mathf.Abs(result[15].y - result[16].y) < .001f,
                $"Floor camera {pitch}: standing legs do not acquire false depth or asymmetric lift", report);
            Vector3 standingFoot = result[15], support = result[16], hand = result[9];
            var moving = FloorFixture(pitch, true, true);
            solver.Solve(moving, scores, .5625f, 25 / 30.0, 1.7f, pitch, 0, result, confidence, tuning);
            Check(result[15].y > standingFoot.y + .35f && Vector3.Distance(result[16], support) < .005f,
                $"Floor camera {pitch}: kick lifts immediately while support leg stays planted", report);
            Check(Vector3.Distance(result[9], hand) > .1f,
                $"Floor camera {pitch}: punch still moves the wrist", report);
            Check(Vector3.Angle((result[5] + result[6] - result[11] - result[12]) * .5f, Vector3.up) < .01f,
                $"Floor camera {pitch}: moving limbs cannot tilt the torso", report);
            for (int frame = 26; frame < 36; frame++)
                solver.Solve(FloorFixture(pitch, punch: true), scores, .5625f, frame / 30.0, 1.7f, pitch, 0, result, confidence, tuning);
            Check(Mathf.Abs(result[9].y - result[5].y) < .1f,
                $"Floor camera {pitch}: a horizontal punch does not become an overhead strike", report);
            scores[13] = 0;
            solver.Solve(moving, scores, .5625f, 1.5, 1.7f, pitch, 0, result, confidence, tuning);
            Check(confidence[13] == 0, $"Floor camera {pitch}: correction does not invent a missing knee", report);
            scores[13] = .9f;
        }
        var saved = new RTMPoseTuning();
        JsonUtility.FromJsonOverwrite("{\"filtering\":true,\"directAvatar\":false}", saved);
        Check(saved.uprightBody, "Existing saved settings enable new upright correction", report);
    }
    private static void ValidateStableMotion(StringBuilder report)
    {
        var tuning = new RTMPoseTuning { prediction = 0 };
        foreach (float interval in new[] { 1f / 60, 1f / 30, .1f })
        {
            tuning.observationInterval = interval;
            var filter = new PoseJointFilter(); filter.Step(Vector3.zero, .9f, 0, 0, tuning, out _);
            float energy = 0;
            for (int i = 1; i <= 120; i++)
            {
                var noise = new Vector3(Mathf.Sin(i * 2.1f), Mathf.Cos(i * 1.7f), 0) * .008f;
                energy += filter.Step(noise, .9f, i * interval, .01f, tuning, out _).sqrMagnitude;
            }
            Check(energy < .00001f, $"Stable motion {1 / interval:F0} Hz: 8 mm rest jitter rejected", report);
            double t = 121 * interval;
            Vector3 onset = filter.Step(Vector3.right * .2f, .9f, t, 0, tuning, out _);
            Check(onset.x > .16f, $"Stable motion {1 / interval:F0} Hz: over 80% of 20 cm motion in first sample", report);
            Vector3 stopped = filter.Step(Vector3.right * .2f, 0, t + interval, .04f, tuning, out _);
            Vector3 held = filter.Predict(t + interval + .01f, .04f, tuning, out _);
            Check(!filter.HasObservation && Vector3.Distance(stopped, held) < .00001f,
                $"Stable motion {1 / interval:F0} Hz: missing joints stop extrapolating", report);
            filter.Step(Vector3.one, .34f, t + 1, 0, tuning, out float usable);
            Check(!filter.HasObservation && usable == 0, "Weak joint cannot reacquire at the keep threshold", report);
        }
        var slow = new PoseJointFilter(); slow.Step(Vector3.zero, .9f, 0, 0, tuning, out _);
        Vector3 value = Vector3.zero;
        for (int i = 1; i <= 100; i++) value = slow.Step(Vector3.right * i * .001f, .9f, i / 30.0, 0, tuning, out _);
        Check(value.x > .05f, "Slow deliberate motion escapes the anchored dead zone", report);
        var scores = new float[26]; for (int i = 0; i < 26; i++) scores[i] = .9f;
        foreach (float pitch in new[] { 0f, 20f, 40f })
        {
            var solver = new Skeleton25DSolver(); var result = new Vector3[26]; var confidence = new float[26];
            tuning = new RTMPoseTuning { filtering = true, prediction = 0, observationInterval = .1f };
            for (int i = 0; i < 25; i++) solver.Solve(FloorFixture(pitch), scores, .5625f, i * .1, 1.7f, pitch, 0, result, confidence, tuning);
            float standingDrop = result[11].y - result[15].y;
            // Independent image-space noise exercises the entire depth reconstruction,
            // not only the joint filter in isolation.
            var quiet = new Skeleton25DSolver();
            var quietResult = new Vector3[26]; var quietConfidence = new float[26];
            for (int i = 0; i < 25; i++) quiet.Solve(FloorFixture(pitch), scores, .5625f, i * .1, 1.7f, pitch, 0, quietResult, quietConfidence, tuning);
            var anchor = (Vector3[])quietResult.Clone(); float maximumNoise = 0;
            for (int i = 25; i < 125; i++)
            {
                var noisy = FloorFixture(pitch);
                for (int j = 0; j < 26; j++) noisy[j] += new Vector2(Mathf.Sin(i * 1.7f + j), Mathf.Cos(i * 2.1f + j)) * .001f;
                quiet.Solve(noisy, scores, .5625f, i * .1, 1.7f, pitch, .01f, quietResult, quietConfidence, tuning);
                foreach (int joint in new[] { 7, 9, 8, 10, 13, 15, 14, 16 })
                    maximumNoise = Mathf.Max(maximumNoise, Vector3.Distance(anchor[joint], quietResult[joint]));
            }
            Check(maximumNoise < .015f, $"Motion {pitch}°: image noise stays below 1.5 cm after depth reconstruction ({maximumNoise * 100:F2} cm)", report);
            for (int i = 25; i < 35; i++) solver.Solve(FloorFixture(pitch, squat: .3f), scores, .5625f, i * .1, 1.7f, pitch, 0, result, confidence, tuning);
            Check(result[11].y - result[15].y < standingDrop - .15f, $"Motion {pitch}°: squat shortens hip-to-floor distance", report);
            for (int i = 35; i < 45; i++) solver.Solve(FloorFixture(pitch, bend: 70, headTurn: 40), scores, .5625f, i * .1, 1.7f, pitch, 0, result, confidence, tuning);
            Check(solver.TorsoAngles.x > 20 && solver.TorsoAngles.x <= tuning.maxBodyBend,
                $"Motion {pitch}°: forward bend moves the torso within its angle limit ({solver.TorsoAngles.x:F1}°)", report);
            // Test head motion separately from an extreme bend that can hide the face.
            for (int i = 45; i < 55; i++) solver.Solve(FloorFixture(pitch, headTurn: 40), scores, .5625f, i * .1, 1.7f, pitch, 0, result, confidence, tuning);
            Check(Mathf.Abs(solver.HeadAngles.y) > 8 && Mathf.Abs(solver.HeadAngles.y) <= 50,
                $"Motion {pitch}°: head turn is tracked and bounded ({solver.HeadAngles.y:F1}°)", report);
        }
    }
    private static void ValidateFullBodyDirections(StringBuilder report)
    {
        var scores = new float[26]; for (int i = 0; i < 26; i++) scores[i] = .9f;
        var tuning = new RTMPoseTuning { filtering = false, prediction = 0 };
        foreach (float pitch in new[] { 0f, 20f, 40f })
        {
            var solver = new Skeleton25DSolver(); var result = new Vector3[26]; var confidence = new float[26];
            int frame = 0;
            void Hold(Vector2[] points) { for (int i = 0; i < 25; i++) solver.Solve(points, scores, .5625f, frame++ * .1, 1.7f, pitch, 0, result, confidence, tuning); }
            Hold(FloorFixture(pitch));
            foreach (int pose in new[] { 1, 2, 3 })
            {
                Hold(FloorFixture(pitch, armPose: pose));
                for (int side = 0; side < 2; side++)
                {
                    Vector3 upper = result[7 + side] - result[5 + side], forearm = result[9 + side] - result[7 + side];
                    float sign = side == 0 ? -1 : 1;
                    Check(pose == 1 ? upper.y > .25f && forearm.y > .20f : upper.x * sign > .25f && (pose == 2 ? forearm.x * sign > .20f : forearm.y > .20f),
                        $"Body {pitch}°: side {side}, arm pose {pose} retains the requested limb direction", report);
                    Check(Mathf.Abs(Vector3.Angle(upper, forearm) - (pose == 3 ? 90 : 0)) < 15,
                        $"Body {pitch}°: side {side}, arm pose {pose} preserves straight/bent elbow", report);
                }
            }
            Hold(FloorFixture(pitch)); var neutral = (Vector3[])result.Clone();
            foreach (bool right in new[] { false, true })
            {
                Hold(FloorFixture(pitch, kick: true, rightKick: right)); int side = right ? 1 : 0;
                Check(result[15 + side].y > neutral[15 + side].y + .35f && Vector3.Distance(result[16 - side], neutral[16 - side]) < .005f,
                    $"Body {pitch}°: {(right ? "right" : "left")} leg raises while opposite foot remains planted", report);
                Hold(FloorFixture(pitch));
                Check(Vector3.Distance(result[15 + side], neutral[15 + side]) < .005f,
                    $"Body {pitch}°: lifted leg returns to its standing position", report);
            }
            foreach (float lean in new[] { -25f, 25f })
            {
                Hold(FloorFixture(pitch, sideBend: lean));
                Check(solver.TorsoAngles.z * Mathf.Sign(lean) > 15 && Mathf.Abs(solver.TorsoAngles.z) <= 35,
                    $"Body {pitch}°: side bend {lean}° retains sign and respects limit", report);
            }
            Hold(FloorFixture(pitch));
            Check(solver.TorsoAngles.magnitude < 1, $"Body {pitch}°: torso recovers to calibrated neutral", report);
        }
    }
    private static void ValidateReferencePerson(Worker worker, ComputeShader shader, Tensor<float> input, int kernel, StringBuilder report)
    {
        // Optional local fixture extracted from the user's practice clip, never shown as a live feed.
        const string fixture = "Logs/Practice/reference-contact-sheet.jpg";
        if (!File.Exists(fixture)) { report.AppendLine("SKIP: reference person fixture unavailable"); return; }
        var sheet = new Texture2D(2, 2); var person = new Texture2D(56, 144, TextureFormat.RGB24, false);
        var cameraFrame = new RenderTexture(56, 144, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        try
        {
            sheet.LoadImage(File.ReadAllBytes(fixture));
            person.SetPixels(sheet.GetPixels(204, 480, 56, 144)); person.Apply();
            var roi = RTMPoseMath.FitAspect(new Rect(0, 0, 1, 1), 56f / 144);
            Graphics.Blit(person, cameraFrame);
            shader.SetTexture(kernel, "CameraTexture", cameraFrame);
            shader.SetVector("ROI", new Vector4(roi.x, roi.y, roi.width, roi.height));
            shader.SetInt("Rotation", 0); shader.SetInt("VerticalFlip", 0);
            shader.SetBuffer(kernel, "Input", ComputeTensorData.Pin(input).buffer);
            shader.Dispatch(kernel, 24, 32, 1); worker.Schedule(input);
            using var x = (worker.PeekOutput("simcc_x") as Tensor<float>).ReadbackAndClone();
            using var y = (worker.PeekOutput("simcc_y") as Tensor<float>).ReadbackAndClone();
            var points = new Vector2[26]; var scores = new float[26];
            RTMPoseMath.Decode(x.DownloadToArray(), y.DownloadToArray(), roi, points, scores);
            var solver = new Skeleton25DSolver(); var positions = new Vector3[26]; var confidence = new float[26];
            report.AppendLine($"Reference torso scores: {scores[5]:F2}, {scores[6]:F2}, {scores[11]:F2}, {scores[12]:F2}");
            for (int i = 0; i < 20; i++)
                Check(solver.Solve(points, scores, 56f / 144, i / 30.0, 1.7f, 20, 0, positions, confidence),
                    "Recorded person GPU inference → skeleton, frame " + i, report);
            Check(solver.Calibrated && confidence[13] > .3f && confidence[14] > .3f, "Recorded full body calibrates and supplies both knees", report);
            solver.Reset();
            var weak = (float[])scores.Clone(); weak[11] = .4f;
            for (int i = 0; i < 40; i++)
                solver.Solve(points, i % 3 == 1 ? weak : scores, 56f / 144, i / 30.0, 1.7f, 20, 0, positions, confidence);
            Check(solver.Calibrated, "Brief hip confidence dips do not prevent calibration", report);
            solver.Reset();
            solver.Solve(points, scores, 56f / 144, 0, 1.7f, 20, 0, positions, confidence);
            solver.Solve(points, weak, 56f / 144, .1, 1.7f, 20, 0, positions, confidence);
            Check(solver.CalibrationFrames == 1, "Weak frame pauses calibration without adding a sample", report);
            solver.Solve(points, weak, 56f / 144, .3, 1.7f, 20, 0, positions, confidence);
            Check(solver.CalibrationFrames == 0, "Prolonged torso loss resets acquisition", report);
            for (int i = 0; i < 25; i++)
                solver.Solve(points, scores, 56f / 144, i * .3, 1.7f, 20, 0, positions, confidence);
            Check(!solver.Calibrated, "Scattered confident background detections cannot accumulate calibration", report);
            solver.Reset();
            var mobileTuning = new RTMPoseTuning { observationInterval = .2f };
            for (int i = 0; i < 20; i++)
                solver.Solve(points, scores, 56f / 144, i * .2, 1.7f, 20, 0, positions, confidence, mobileTuning);
            Check(solver.Calibrated, "5 FPS mobile calibration completes without a fixed 2.5-second deadline", report);
            solver.Reset();
            solver.Solve(points, scores, 56f / 144, 0, 1.7f, 20, 0, positions, confidence, mobileTuning);
            solver.Solve(points, weak, 56f / 144, .8, 1.7f, 20, 0, positions, confidence, mobileTuning);
            Check(solver.CalibrationFrames == 0, "Mobile cadence allowance still expires prolonged torso loss", report);
            ValidateUncalibratedOverlay(points, scores, report);
        }
        finally { cameraFrame.Release(); UnityEngine.Object.DestroyImmediate(cameraFrame); UnityEngine.Object.DestroyImmediate(sheet); UnityEngine.Object.DestroyImmediate(person); }
    }
    private static void ValidateUncalibratedOverlay(Vector2[] points, float[] scores, StringBuilder report)
    {
        var source = new GameObject("RTMPose preview test");
        var graphic = new GameObject("Uncalibrated overlay test", typeof(RectTransform));
        try
        {
            var provider = source.AddComponent<RTMPosePoseProvider>();
            Array.Copy(points, provider.Points, 26); Array.Copy(scores, provider.Scores, 26);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            double now = Time.realtimeSinceStartupAsDouble;
            typeof(RTMPosePoseProvider).GetField("lastDecodedAt", flags).SetValue(provider, now);
            typeof(RTMPosePoseProvider).GetField("lastDecodedCapture", flags).SetValue(provider, now);
            Check(provider.HasRecentLandmarks && !provider.HasRecentPose && !provider.Calibrated,
                "2D detections are available before calibration without enabling avatar control", report);
            var overlay = graphic.AddComponent<FitFight.Practice.PracticeSkeletonOverlay>();
            overlay.Sample(provider, null, false);
            var populate = typeof(FitFight.Practice.PracticeSkeletonOverlay).GetMethod("OnPopulateMesh", flags, null, new[] { typeof(VertexHelper) }, null);
            using var mesh = new VertexHelper(); populate.Invoke(overlay, new object[] { mesh });
            Check(mesh.currentVertCount > 0, "Camera overlay draws detected landmarks before calibration", report);
            var vertex = new UIVertex(); mesh.PopulateUIVertex(ref vertex, 0);
            Check(vertex.color.r > vertex.color.g && vertex.color.g > vertex.color.b,
                "Uncalibrated detections are visibly amber", report);
            provider.ResetCalibration(); overlay.Sample(provider, null, false);
            populate.Invoke(overlay, new object[] { mesh });
            Check(mesh.currentVertCount == 0 && !provider.HasRecentLandmarks, "Reset removes stale landmark overlay", report);
        }
        finally { UnityEngine.Object.DestroyImmediate(graphic); UnityEngine.Object.DestroyImmediate(source); }
    }
    private static void Check(bool condition, string name, StringBuilder report)
    {
        if (!condition) throw new Exception(name);
        report.AppendLine("PASS: " + name);
    }
}
