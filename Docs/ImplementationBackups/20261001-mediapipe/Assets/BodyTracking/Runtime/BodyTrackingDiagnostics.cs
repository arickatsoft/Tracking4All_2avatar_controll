using System;
using System.Globalization;
using System.IO;
using Tracking4All;
using UnityEngine;

namespace FitFight.BodyTracking
{
    public sealed class BodyTrackingDiagnostics : MonoBehaviour
    {
        private BodyTrackingController controller;
        private Material previewMaterial;
        private RenderTexture preview;
        private bool expanded;
        private Vector2 scroll;
        private StreamWriter recording;
        private string recordingPath;
        private float nextSample, renderFPS, nextHealthLog;
        private int poseCount;
        private float poseFPS, windowStart;
        private IProviderLifecycle rawLifecycle;
        private void Start()
        {
            controller = GetComponent<BodyTrackingController>();
            var graph = GetComponentInChildren<MPUPoseTrackingSolution>(true);
            if (graph != null)
            {
                rawLifecycle = ((ILandmarkProvider<MPPoseLandmarks>)graph).Lifecycle;
                if (rawLifecycle != null) rawLifecycle.OnUpdated += CountPose;
            }
        }
        private void CountPose(int group) { System.Threading.Interlocked.Increment(ref poseCount); }
        private void Update()
        {
            renderFPS = Mathf.Lerp(renderFPS, 1f / Mathf.Max(Time.unscaledDeltaTime, .001f), .05f);
            if (Time.unscaledTime - windowStart > 1)
            {
                poseFPS = System.Threading.Interlocked.Exchange(ref poseCount, 0) / Mathf.Max(.001f, Time.unscaledTime - windowStart);
                windowStart = Time.unscaledTime;
            }
            if ((Debug.isDebugBuild || Application.isMobilePlatform) && controller != null && Time.unscaledTime >= nextHealthLog)
            {
                nextHealthLog = Time.unscaledTime + 3;
                var p = controller.RTMPose;
                if (p != null && p.isActiveAndEnabled)
                    Debug.Log($"[FitFight tracking] camera={controller.CameraFPS:F1} inference={p.InferenceFPS:F1} inferenceMs={p.LastInferenceMs:F0} render={renderFPS:F1} pose={p.PoseFPS:F1} textureUpdates={(p.CameraTexture as WebCamTexture)?.updateCount} completed={p.CompletedInferences} rotation={p.CameraRotation} flip={p.CameraVerticalFlip} aspect={p.CameraAspect:F3} upright={p.Tuning.uprightBody} pitch={p.cameraPitchDegrees:F0} status={p.PipelineStatus}");
            }
            if (recording != null && controller != null && Time.unscaledTime >= nextSample)
            {
                nextSample = Time.unscaledTime + .1f;
                var pose = controller.RTMPose;
                bool rtm = pose != null && pose.isActiveAndEnabled;
                recording.WriteLine(string.Join(",", Time.realtimeSinceStartupAsDouble.ToString("F3", CultureInfo.InvariantCulture),
                    rtm ? pose.EngineLabel : "Tracking4All", (rtm ? pose.PoseFPS : poseFPS).ToString("F1", CultureInfo.InvariantCulture),
                    renderFPS.ToString("F1", CultureInfo.InvariantCulture), rtm && pose.HasRecentPose ? pose.CameraToPoseMs.ToString("F2", CultureInfo.InvariantCulture) : "",
                    UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong().ToString(), rtm ? pose.DroppedFrames.ToString() : "",
                    controller.CameraFPS.ToString("F1", CultureInfo.InvariantCulture), controller.RetargetingStatus,
                    rtm ? pose.Tuning.prediction.ToString("F3", CultureInfo.InvariantCulture) : "",
                    rtm ? pose.Tuning.minCutoff.ToString("F2", CultureInfo.InvariantCulture) : "",
                    rtm ? pose.HasReliableLeg(true).ToString() : "", rtm ? pose.HasReliableLeg(false).ToString() : ""));
            }
        }
        private void OnGUI()
        {
            if (controller == null) return;
            // Do not cover the home screen's selection controls.
            var home = FindFirstObjectByType<FitFightSelectionView>();
            if (home != null && home.isActiveAndEnabled) return;
            float scale = Mathf.Max(1, Screen.width / 720f);
            var savedMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale;
            bool practice = FindFirstObjectByType<FitFightPracticeMode>() != null;
            float panelWidth = practice && !expanded ? 96 : Mathf.Min(355, width - 16);
            float left = practice ? width - panelWidth - 8 : 8;
            float top = practice ? Screen.height / scale * .11f : 8;
            GUILayout.BeginArea(new Rect(left, top, panelWidth, expanded ? Mathf.Min(760, Screen.height / scale - top - 8) : 42), GUI.skin.box);
            if (GUILayout.Button(expanded ? "Hide tracking controls" : "Tracking", GUILayout.Height(30))) expanded = !expanded;
            if (expanded)
            {
                scroll = GUILayout.BeginScrollView(scroll);
                GUILayout.Label(controller.Status);
                GUILayout.Label("Live engine comparison · no recording required");
                for(int i=0;i<BodyTrackingSelection.Labels.Length;i++)
                    if(GUILayout.Button(((int)BodyTrackingSelection.Selected==i ? "✓ " : "")+BodyTrackingSelection.Labels[i],GUILayout.Height(32)))
                        BodyTrackingSelection.Select((BodyTrackingSystem)i);
                if(GUILayout.Button("Sanity check · no recording",GUILayout.Height(34)))
                { FindFirstObjectByType<FitFightPracticeMode>()?.StartSanityCheck(); expanded=false; }
                var pose = controller.RTMPose;
                bool rtm = pose != null && pose.isActiveAndEnabled;
                GUILayout.Label($"Camera: {controller.CameraFPS:F0} | Render: {renderFPS:F0} | Pose: {(rtm ? pose.PoseFPS : poseFPS):F0} FPS");
                if (rtm)
                {
                    GUILayout.Label($"Compute: {pose.ComputeMs:F0} ms | AI: {pose.InferenceFPS:F0} FPS | Result age: {pose.LastInferenceMs:F0} ms");
                    GUILayout.Label(pose.UsesWorldLandmarks ? "MediaPipe learned 3D world landmarks" : "RTMPose 2D landmarks with estimated depth");
                    if (pose.CameraTexture != null)
                        GUILayout.Label($"Camera texture {pose.CameraTexture.width}x{pose.CameraTexture.height}, rotation {pose.CameraRotation}°, vertical flip {pose.CameraVerticalFlip}");
                    bool upright = GUILayout.Toggle(pose.Tuning.uprightBody, "Stable body motion / floor camera correction");
                    if (upright != pose.Tuning.uprightBody) { pose.Tuning.uprightBody = upright; pose.ResetCalibration(); }
                    GUILayout.Label("Allows bends and head movement while suppressing noise. The stable / fast preset uses direct bone control to avoid puppet twist. Calibrate standing still with both feet down.");
                    bool useDirect = GUILayout.Toggle(pose.Tuning.directAvatar, "Direct avatar (experimental)");
                    if (useDirect != pose.Tuning.directAvatar) controller.SetDirectAvatar(useDirect);
                    GUILayout.Label($"Requested camera: {pose.RequestedCameraFPS} FPS (actual shown above)");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("30 FPS")) pose.SetCameraFPS(30);
                    if (GUILayout.Button("60 FPS")) pose.SetCameraFPS(60);
                    GUILayout.EndHorizontal();
                    GUILayout.Label($"Left leg: {(pose.HasReliableLeg(true) ? "tracked" : "missing")} | Right leg: {(pose.HasReliableLeg(false) ? "tracked" : "missing")}");
                    if (controller.DirectDriver != null && controller.DirectDriver.isActiveAndEnabled)
                        GUILayout.Label($"Frame available → direct avatar: {controller.DirectDriver.FrameAvailableToAvatarMs:F1} ms");
                    GUILayout.Label(pose.PipelineStatus);
                    if (pose.HasRecentPose) GUILayout.Label($"Frame available → pose: {pose.CameraToPoseMs:F1} ms");
                    GUILayout.Label(pose.CalibrationCountdown > 0 ? $"Step back: calibration starts in {Mathf.CeilToInt(pose.CalibrationCountdown)} seconds" : pose.Calibrated ? "Calibration captured" : $"Stand upright: {pose.CalibrationFrames}/20 confident frames");
                    GUILayout.Label("Amber landmarks = detected, calibrating. Green = calibrated. Avatar starts after calibration; direct mode is optional.");
                    GUILayout.Label($"Height: {pose.userHeight:F2} m | Camera tilt: {pose.cameraPitchDegrees:F0}°");
                    pose.userHeight = GUILayout.HorizontalSlider(pose.userHeight, 1.2f, 2.2f);
                    pose.cameraPitchDegrees = GUILayout.HorizontalSlider(pose.cameraPitchDegrees, -45, 45);
                    GUILayout.Label("Set height/tilt, then recalibrate. Stand upright for 20 accepted torso frames. This resets body scale, standing leg proportions, crop and filter history.");
                    if (GUILayout.Button("Calibrate floor camera", GUILayout.Height(30))) pose.CalibrateFloorCamera();
                    if (GUILayout.Button("Apply manual tilt / reset crop")) pose.ResetCalibration();
                    var tuning = pose.Tuning;
                    tuning.cameraVerticalFov = Slider("Front camera vertical FOV estimate (degrees)", tuning.cameraVerticalFov, 40, 90);
                    GUILayout.Label("FOV estimates perspective correction. Leave at 60 unless known; recalibrate after changing it.");
                    tuning.filtering = GUILayout.Toggle(tuning.filtering, "Adaptive filtering (off = raw diagnostic)");
                    if (tuning.directAvatar)
                    {
                        tuning.directRotationSmoothing = GUILayout.Toggle(tuning.directRotationSmoothing, "Direct bone rotation smoothing (experimental)");
                        tuning.directRotationSpeedLimit = GUILayout.Toggle(tuning.directRotationSpeedLimit, "Direct bone rotation speed limit (experimental)");
                        if (tuning.directRotationSpeedLimit)
                            tuning.directMaxDegreesPerSecond = Slider("Rotation speed limit (degrees/s)", tuning.directMaxDegreesPerSecond, 180, 2160);
                        GUILayout.Label("Applies to direct tracking with punch assistance off. Compare steadiness and fast-move response before saving.");
                    }
                    tuning.noiseRadius = Slider("Ignore small joint noise (cm)", tuning.noiseRadius * 100, 0, 5) / 100;
                    tuning.confidenceHysteresis = Slider("Lost joint reacquisition margin", tuning.confidenceHysteresis, 0, .2f);
                    tuning.bodyNoiseDegrees = Slider("Body / head noise (degrees)", tuning.bodyNoiseDegrees, 0, 8);
                    tuning.maxBodyBend = Slider("Maximum forward bend (degrees)", tuning.maxBodyBend, 15, 75);
                    GUILayout.Label($"Body bend {pose.BodyAngles.x:F0}° | Head turn {pose.HeadAngles.y:F0}°");
                    tuning.minCutoff = Slider("Filter cutoff (Hz)", tuning.minCutoff, 1, 20);
                    tuning.speedCoefficient = Slider("Motion response", tuning.speedCoefficient, 0, 20);
                    tuning.velocityCutoff = Slider("Velocity cutoff (Hz)", tuning.velocityCutoff, 1, 30);
                    tuning.prediction = Slider("Prediction (ms)", tuning.prediction * 1000, 0, 40) / 1000;
                    tuning.jointConfidence = Slider("Joint confidence", tuning.jointConfidence, .15f, .7f);
                    tuning.legDeadband = Slider("Leg movement threshold (%)", tuning.legDeadband * 100, 0, 5) / 100;
                    tuning.occlusionGrace = Slider("Occlusion grace (ms)", tuning.occlusionGrace * 1000, 20, 150) / 1000;
                    tuning.depthSpeed = Slider("Depth response (m/s)", tuning.depthSpeed, 1, 12);
                    tuning.depthMotionGain = Slider("Depth motion gain", tuning.depthMotionGain, 0, 4);
                    tuning.maxPredictionDistance = Slider("Prediction distance cap (cm)", tuning.maxPredictionDistance * 100, 2, 25) / 100;
                    GUILayout.Label("Lower cutoff = steadier at rest. Higher motion response = quicker during movement. Prediction leads motion; too much causes overshoot. Confidence is a score gate, not a probability.");
                    GUILayout.Label("Missing joints hold briefly without prediction, then fade to a guard. Held joints cannot score hits. Single-camera depth is inferred by the model or reconstruction.");
                    if (GUILayout.Button("Save responsiveness settings")) tuning.Save();
                    if (GUILayout.Button("Apply stable / fast preset"))
                    {
                        pose.Tuning = new RTMPoseTuning { directAvatar = true };
                        controller.SetDirectAvatar(true);
                        pose.SetCameraFPS(60); pose.ResetCalibration();
                    }
                }
                if (!rtm)
                {
                    float cutoff = Slider("Rest cutoff (Hz)", controller.BaselineCutoff, 1, 10);
                    float gain = Slider("Movement catch-up", controller.BaselineMotionGain, 0, 40);
                    controller.SetBaselineSmoothing(cutoff, gain);
                    GUILayout.Label("Lower rest cutoff reduces shake. Higher catch-up reduces delay during larger movements. These controls apply to Tracking4All's local smoothing only (session settings).");
                    GUILayout.Label("Calibrate in practice resets the bag and retries a missing camera. Tracking4All does not use RTMPose's height/tilt calibration.");
                }
                if (GUILayout.Button("Retry selected engine", GUILayout.Height(30))) controller.RetryEngine();
                if (GUILayout.Button("Retry camera", GUILayout.Height(30))) controller.RetryCamera();
                if (GUILayout.Button(recording == null ? "Record benchmark CSV" : "Stop recording", GUILayout.Height(30))) ToggleRecording();
                if (recordingPath != null) GUILayout.Label("CSV: " + recordingPath);
                if (GUILayout.Button("Return to home", GUILayout.Height(30))) FindFirstObjectByType<PlayerSelectionManager>()?.ShowPlayerSelection();
                if (rtm) DrawPreview(pose);
                GUILayout.Label("Timing excludes camera exposure and display latency.");
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea(); GUI.matrix = savedMatrix;
        }
        private static float Slider(string label, float value, float min, float max)
        {
            GUILayout.Label($"{label}: {value:F2}");
            return GUILayout.HorizontalSlider(value, min, max);
        }
        private void DrawPreview(RTMPosePoseProvider pose)
        {
            if (pose.CameraTexture == null || pose.CameraAspect <= 0) return;
            if (previewMaterial == null) previewMaterial = new Material(Resources.Load<Shader>("RTMPose/Preview"));
            if (preview == null) preview = new RenderTexture(256, 256, 0);
            previewMaterial.SetInt("_Rotation", pose.CameraRotation);
            previewMaterial.SetInt("_VerticalFlip", pose.CameraVerticalFlip ? 1 : 0);
            if (Event.current.type == EventType.Repaint) Graphics.Blit(pose.CameraTexture, preview, previewMaterial);
            var area = GUILayoutUtility.GetRect(200, 170);
            float w = Mathf.Min(area.width, area.height * pose.CameraAspect);
            float h = w / pose.CameraAspect;
            var rect = new Rect(area.x, area.y, w, h);
            GUI.DrawTexture(rect, preview, ScaleMode.StretchToFill);
            if (!pose.HasRecentLandmarks) return;
            var oldColor = GUI.color; GUI.color = pose.HasRecentPose ? Color.green : new Color(1, .7f, .12f);
            for (int i = 0; i < 26; i++)
                if (pose.Scores[i] >= .35f)
                    GUI.DrawTexture(new Rect(rect.x + pose.Points[i].x * rect.width - 2,
                        rect.y + pose.Points[i].y * rect.height - 2, 4, 4), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
        public void StopRecording() { recording?.Dispose(); recording=null; }
        private void ToggleRecording()
        {
            if (recording != null) { recording.Dispose(); recording = null; return; }
            try
            {
                recordingPath = Path.Combine(Application.persistentDataPath, "tracking-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".csv");
                recording = new StreamWriter(recordingPath);
                recording.WriteLine("time_s,system,pose_fps,render_fps,frame_available_to_pose_ms,memory_bytes,dropped_frames,camera_fps,retargeting,prediction_s,min_cutoff_hz,left_leg_valid,right_leg_valid");
            }
            catch (Exception e) { recordingPath = e.Message; }
        }
        private void OnDisable() { recording?.Dispose(); recording = null; }
        private void OnDestroy()
        {
            if (rawLifecycle != null) rawLifecycle.OnUpdated -= CountPose;
            if (preview != null) { preview.Release(); Destroy(preview); }
            if (previewMaterial != null) Destroy(previewMaterial);
        }
    }
}
