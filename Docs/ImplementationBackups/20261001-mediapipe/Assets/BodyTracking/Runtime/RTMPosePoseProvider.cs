using System;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Tracking4All;
using Unity.InferenceEngine;
using UnityEngine;

namespace FitFight.BodyTracking
{
    [DefaultExecutionOrder(-700)]
    public sealed class RTMPosePoseProvider : MonoBehaviour, IBodyPoseProvider,
        ILandmarkProvider<PoseLandmarks>, INormalizedLandmarkProvider<PoseLandmarks>
    {
        [Range(1.2f, 2.2f)] public float userHeight = 1.7f;
        [Range(-45, 45)] public float cameraPitchDegrees = 20;
        public RTMPoseTuning Tuning = new RTMPoseTuning();
        private void Awake() { Tuning = RTMPoseTuning.Load(); }

        public void CalibrateFloorCamera()
        {
            // Gravity estimates the elevation of the screen normal, independent of portrait rotation.
            if (Application.isMobilePlatform && Input.acceleration.sqrMagnitude > .25f)
            {
                Vector3 gravity = Input.acceleration.normalized;
                UnclampedCameraPitch = Mathf.Atan2(Mathf.Abs(gravity.z),
                    new Vector2(gravity.x, gravity.y).magnitude) * Mathf.Rad2Deg;
                cameraPitchDegrees = Mathf.Clamp(UnclampedCameraPitch, 0, 45);
            }
            ResetCalibration();
            calibrationNotBefore = Time.realtimeSinceStartupAsDouble + 5;
        }
        private double calibrationNotBefore;
        public float UnclampedCameraPitch { get; private set; }
        public string StandingCalibrationHint => solver.CalibrationStatus;
        public void AcquireStandingReference()
        {
            CalibrateFloorCamera();
            calibrationNotBefore = Time.realtimeSinceStartupAsDouble;
            solver.RequireStandingCalibration = true;
        }
        [Serializable] public struct Diagnostics
        {
            public string kind, calibrationStatus, engine;
            public bool learnedWorldLandmarks;
            public double time;
            public int sequence, calibrationFrames, droppedFrames;
            public bool calibrated;
            public float scale, pitch, gravityPitch, cameraFPS, inferenceFPS, poseFPS, inferenceMs;
            public float scheduleMs, scheduledToReadbackMs, readbackToConsumeMs, frameAgeAtScheduleMs;
            public float[] neutralLegProjection, confidence;
            public bool[] observed;
            public Vector3[] joints;
        }
        private readonly Vector3[] acceptedSolved = new Vector3[26];
        private readonly float[] acceptedSolvedScores = new float[26];
        public Diagnostics CaptureDiagnostics()
        {
            var observed = new bool[26]; for (int i = 0; i < 26; i++) observed[i] = UsesWorldLandmarks ? worldPose.Observed(i) : solver.JointObserved(i);
            return new Diagnostics { kind="reconstruction", engine=EngineLabel, learnedWorldLandmarks=UsesWorldLandmarks, time=Time.realtimeSinceStartupAsDouble,
                sequence=PoseSequence, calibrationFrames=CalibrationFrames, calibrated=Calibrated,
                calibrationStatus=solver.CalibrationStatus, scale=solver.CalibrationScale,
                neutralLegProjection=solver.NeutralLegProjection, pitch=cameraPitchDegrees, gravityPitch=UnclampedCameraPitch,
                cameraFPS=CameraFPS, inferenceFPS=InferenceFPS, poseFPS=PoseFPS, inferenceMs=LastInferenceMs, droppedFrames=DroppedFrames,
                scheduleMs=ScheduleMs, scheduledToReadbackMs=ScheduledToReadbackMs, readbackToConsumeMs=ReadbackToConsumeMs, frameAgeAtScheduleMs=FrameAgeAtScheduleMs,
                joints=(Vector3[])acceptedSolved.Clone(), confidence=(float[])acceptedSolvedScores.Clone(), observed=observed };
        }
        public float CalibrationCountdown => Mathf.Max(0, (float)(calibrationNotBefore - Time.realtimeSinceStartupAsDouble));
        public event Action PoseReady;
        private readonly PoseRateMeter poseRate = new PoseRateMeter();
        private readonly PoseRateMeter inferenceRate = new PoseRateMeter();
        private bool acceptedPose;
        private bool discardPending;
        public int CompletedInferences { get; private set; }
        public int PoseSequence { get; private set; }
        // Immutable until the next accepted observation, independent of render prediction
        // and diagnostic detections that fail the 200 ms/calibration gates.
        private readonly Vector2[] acceptedImage = new Vector2[26];
        private readonly float[] acceptedImageScores = new float[26];
        public Rect AcceptedROI { get; private set; }
        public float AcceptedAspect { get; private set; }
        public int AcceptedRotation { get; private set; }
        public bool AcceptedVerticalFlip { get; private set; }
        // Main-thread snapshot, independent of later predictions and rejected detections.
        public void CopyAcceptedImage(Vector2[] points, float[] scores)
        {
            if (points == null || points.Length != 26 || scores == null || scores.Length != 26)
                throw new ArgumentException("Accepted image snapshots require 26 points and scores.");
            Array.Copy(acceptedImage, points, 26); Array.Copy(acceptedImageScores, scores, 26);
        }
        public double AcceptedCaptureTime => lastPoseAt;
        public double AcceptedReceiveTime => lastReceivedAt;
        public Vector2 AcceptedImagePoint(PoseLandmarks joint)
        {
            int index = RTMPoseMath.ToMediaPipe[(int)(settings != null && settings.Mirror ? joint.Flip() : joint)];
            return acceptedImage[index];
        }
        public float AcceptedImageConfidence(PoseLandmarks joint)
        {
            int index = RTMPoseMath.ToMediaPipe[(int)(settings != null && settings.Mirror ? joint.Flip() : joint)];
            return acceptedImageScores[index];
        }
        public float ObservationAgeMs => (float)(Time.realtimeSinceStartupAsDouble - lastPoseAt) * 1000;
        public float InferenceFPS => inferenceRate.Get(Time.realtimeSinceStartupAsDouble);
        public float CameraFPS => (cameraFrames as UnityWebCamBackend)?.ActualFrameRate ?? 0;
        public int RequestedCameraFPS => (cameraFrames as UnityWebCamBackend)?.RequestedFrameRate ?? 0;
        public BodyTrackingSystem ConfiguredEngine { get; private set; } = BodyTrackingSystem.RTMPose;
        public string EngineLabel => BodyTrackingSelection.Label(ConfiguredEngine);
        public bool UsesWorldLandmarks => ConfiguredEngine == BodyTrackingSystem.MediaPipeLite || ConfiguredEngine == BodyTrackingSystem.MediaPipeFull;
        public int EngineGeneration { get; private set; }
        public float ComputeMs { get; private set; }
        private PoseInferenceBackend native;
        private ComputeBuffer nativeInput;
        private Coroutine modelLoading;
        private int requestGeneration;
        private string nativeError;
        private double gpuCompletedAt;
        private readonly MediaPipeWorldPose worldPose = new MediaPipeWorldPose();
        private readonly Vector3[] measuredWorld = new Vector3[26];
        private PoseInferenceResult nativeResult;
        private bool BackendReady => worker != null || native != null && native.Ready;
        private Worker worker;
        private Tensor<float> input, outputX, outputY;
        private ComputeShader preprocess;
        private RenderTexture cameraSnapshot;
        private double lastInferenceCapture = double.NaN;
        public float LastInferenceMs { get; private set; }
        public float ScheduleMs { get; private set; }
        public float ScheduledToReadbackMs { get; private set; }
        public float ReadbackToConsumeMs { get; private set; }
        public float FrameAgeAtScheduleMs { get; private set; }
        private double scheduledAt;
        private int kernel;
        private IImageFrameProvider cameraFrames;
        private ISolutionSettings settings;
        private readonly SimpleLifecycleManager lifecycle = new SimpleLifecycleManager(1);
        private readonly Landmark[] world = new Landmark[33];
        private readonly NormalizedLandmark[] normalized = new NormalizedLandmark[33];
        private readonly Vector3[] solved = new Vector3[26];
        private readonly float[] solvedScores = new float[26];
        private readonly Skeleton25DSolver solver = new Skeleton25DSolver { RequireStandingCalibration = true };
        private bool pending, haveFrame;
        private readonly Body26Readback readback = new Body26Readback();
        private ulong lastCameraFrame;
        public static bool IsNewCameraFrame(bool havePrevious, ulong previous, ulong current) => !havePrevious || previous != current;
        private double capturedAt, lastPoseAt, lastFullFrame, lastReceivedAt;
        private double lastDecodedAt = double.NegativeInfinity, lastDecodedCapture = double.NegativeInfinity;
        // Raw 2D detections can be inspected before body-scale calibration permits avatar motion.
        public static bool DiagnosticObservationFresh(double now, double captured, double received, float interval)
            => now - captured <= 1 && now - received <= Mathf.Clamp(interval * 1.5f, .2f, .5f);
        public bool HasRecentLandmarks => isActiveAndEnabled && DiagnosticObservationFresh(Time.realtimeSinceStartupAsDouble,
            lastDecodedCapture, lastDecodedAt, Tuning.observationInterval);
        private string pipelineStatus = "Waiting for camera frames";
        public string PipelineStatus => CameraFPS < 1 ? (cameraFrames as UnityWebCamBackend)?.CameraStatus ?? "Waiting for camera" : HasRecentPose ? "Pose tracking live" : pipelineStatus;
        private float aspect;
        private int rotation;
        private bool verticalFlip;
        private Rect roi, inferenceROI;
        public readonly Vector2[] Points = new Vector2[26];
        public readonly float[] Scores = new float[26];
        public event Action<string> Failed;
        public float CameraToPoseMs { get; private set; }
        public float PoseFPS => poseRate.Get(Time.realtimeSinceStartupAsDouble);
        public int DroppedFrames { get; private set; }
        public bool Calibrated => solver.Calibrated;
        public Quaternion BodyRotation => UsesWorldLandmarks ? worldPose.BodyRotation(solved, settings != null && settings.Mirror, settings != null && settings.PerspectiveFlip) : Quaternion.Euler(solver.TorsoAngles.x, solver.TorsoAngles.y * (settings != null && settings.Mirror ? 1 : -1),
            solver.TorsoAngles.z * (settings != null && settings.Mirror ? 1 : -1));
        public Quaternion HeadRotation => Quaternion.Euler(solver.HeadAngles.x,
            solver.HeadAngles.y * (settings != null && settings.Mirror ? 1 : -1),
            solver.HeadAngles.z * (settings != null && settings.Mirror ? 1 : -1));
        public Vector3 BodyAngles => UsesWorldLandmarks ? SignedEuler(BodyRotation.eulerAngles) : solver.TorsoAngles;
        private static Vector3 SignedEuler(Vector3 v) => new Vector3(Mathf.DeltaAngle(0,v.x),Mathf.DeltaAngle(0,v.y),Mathf.DeltaAngle(0,v.z));
        public Vector3 HeadAngles => solver.HeadAngles;
        public bool IsJointObserved(PoseLandmarks joint)
        {
            var mapped = settings != null && settings.Mirror ? joint.Flip() : joint;
            int i=RTMPoseMath.ToMediaPipe[(int)mapped];
            return HasRecentPose && (UsesWorldLandmarks ? worldPose.Observed(i) : solver.JointObserved(i));
        }
        public int CalibrationFrames => solver.CalibrationFrames;
        public static bool ObservationFresh(double now, double captured, double received, float grace)
            => now - captured <= .25 && now - received <= grace;
        public bool HasRecentPose => lifecycle.IsAlive(0) && ObservationFresh(Time.realtimeSinceStartupAsDouble, lastPoseAt, lastReceivedAt, Tuning.TrackingGrace);
        public Texture CameraTexture => cameraFrames?.Get(0, 0)?.Texture;
        public int CameraRotation => rotation;
        public bool CameraVerticalFlip => verticalFlip;
        public float CameraAspect => aspect;
        public ILandmarkProvider<PoseLandmarks> World => this;
        public INormalizedLandmarkProvider<PoseLandmarks> Normalized => this;
        public IProviderLifecycle Lifecycle => lifecycle;
        public int GroupSize => 1;
        public int DataCount => 33;

        public void Prepare(IImageFrameProvider frames, ISolutionSettings poseSettings, BodyTrackingSystem engine = BodyTrackingSystem.RTMPose)
        {
            Release();
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsAsyncGPUReadback)
                throw new NotSupportedException("Compute shaders and asynchronous GPU readback are required.");
            ConfiguredEngine = engine; EngineGeneration++;
            cameraFrames = frames ?? throw new ArgumentNullException(nameof(frames)); settings = poseSettings;
            var shader = Resources.Load<ComputeShader>("RTMPose/Preprocess");
            if (shader == null) throw new InvalidOperationException("Pose preprocessing shader is missing.");
            preprocess = Instantiate(shader);
            kernel = preprocess.FindKernel(UsesWorldLandmarks ? "MediaPipeRGBA" : "Preprocess");
            if (engine == BodyTrackingSystem.RTMPose)
            {
                var asset = Resources.Load<ModelAsset>("RTMPose/Body26");
                if (asset == null) throw new InvalidOperationException("RTMPose model is missing.");
                worker = new Worker(ModelLoader.Load(asset), BackendType.GPUCompute);
                input = new Tensor<float>(new TensorShape(1,3,256,192));
            }
            else if (engine == BodyTrackingSystem.RTMPoseNCNN)
            {
                var param = Resources.Load<TextAsset>("PoseComparison/NcnnParam");
                var weights = Resources.Load<TextAsset>("PoseComparison/NcnnWeights");
                if (param == null || weights == null) throw new InvalidOperationException("Native RTMPose model is missing.");
                native = new PoseInferenceBackend(engine,param.bytes,weights.bytes);
            }
            else if (UsesWorldLandmarks) modelLoading = StartCoroutine(LoadMediaPipeModel(requestGeneration));
            else throw new ArgumentException("Unsupported comparison engine.",nameof(engine));
            CompletedInferences = DroppedFrames = PoseSequence = 0; ComputeMs=LastInferenceMs=CameraToPoseMs=0; inferenceRate.Reset();
            ResetCalibration(); pipelineStatus="Loading " + EngineLabel;
        }
        private IEnumerator LoadMediaPipeModel(int generation)
        {
            string model=ConfiguredEngine == BodyTrackingSystem.MediaPipeLite ? "pose_landmarker_lite.bytes" : "pose_landmarker_full.bytes";
            string path=Application.streamingAssetsPath + "/" + model;
            if (!path.Contains("://")) path="file://"+path;
            using var request=UnityWebRequest.Get(path);
            yield return request.SendWebRequest();
            if (generation != requestGeneration) yield break;
            if (request.result != UnityWebRequest.Result.Success) nativeError="Could not load MediaPipe model: "+request.error;
            else native=new PoseInferenceBackend(ConfiguredEngine,request.downloadHandler.data);
            modelLoading=null;
        }

        public void SetCameraFPS(int fps)
        {
            Tuning.requestedCameraFPS = fps >= 60 ? 60 : 30;
            ResetCalibration();
            (cameraFrames as UnityWebCamBackend)?.OverrideFrameRate(Tuning.requestedCameraFPS);
        }
        public bool HasReliableLeg(bool left)
        {
            int hip = left ? 11 : 12, knee = left ? 13 : 14, ankle = left ? 15 : 16;
            if (settings != null && settings.Mirror) { hip = left ? 12 : 11; knee = left ? 14 : 13; ankle = left ? 16 : 15; }
            return HasRecentPose && ReliableChain(solvedScores[hip], solvedScores[knee], solvedScores[ankle], Tuning.jointConfidence);
        }
        public bool HasMeasuredLeg(bool left) => HasReliableLeg(left)
            && IsJointObserved(left ? PoseLandmarks.LEFT_HIP : PoseLandmarks.RIGHT_HIP)
            && IsJointObserved(left ? PoseLandmarks.LEFT_KNEE : PoseLandmarks.RIGHT_KNEE)
            && IsJointObserved(left ? PoseLandmarks.LEFT_ANKLE : PoseLandmarks.RIGHT_ANKLE);
        public static bool ReliableChain(float hip, float knee, float ankle, float threshold)
            => hip >= threshold && knee >= threshold && ankle >= threshold;
        public Landmark AvatarLandmark(PoseLandmarks index)
            => Get(0, settings != null && settings.Mirror ? index.Flip() : index);

        public void ResetCalibration()
        {
            worldPose.Reset(); solver.Reset(); acceptedPose = false; discardPending = pending; haveFrame = false; lastFullFrame = double.NegativeInfinity;
            Array.Clear(acceptedSolved, 0, 26); Array.Clear(acceptedSolvedScores, 0, 26);
            lifecycle.SetDisposed(0);
            Array.Clear(Scores, 0, Scores.Length);
            lastDecodedAt = lastDecodedCapture = double.NegativeInfinity;
            lastPoseAt = lastReceivedAt = 0; poseRate.Reset(); Tuning.observationInterval = 1f / 30;
            lastInferenceCapture = double.NaN;
            pipelineStatus = "Waiting for shoulders and hips in frame";
        }

        private void Update()
        {
            try { if (nativeError != null) throw new InvalidOperationException(nativeError); if (BackendReady) Tick(); }
            catch (Exception e) { Debug.LogException(e,this); Failed?.Invoke(e.Message); enabled = false; Release(); }
        }
        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (replayingRecording) return;
#endif
            try { if (BackendReady) { ConsumeResult(Time.realtimeSinceStartupAsDouble); RefreshPrediction(Time.realtimeSinceStartupAsDouble); } }
            catch (Exception e) { Debug.LogException(e,this); Failed?.Invoke(e.Message); enabled = false; Release(); }
        }
        private void ConsumeResult(double now)
        {
            if (!pending) return;
            if (native != null)
            {
                if (!native.TryTake(out nativeResult)) return;
                ComputeMs=nativeResult.ComputeMs;
                ScheduledToReadbackMs=(float)(gpuCompletedAt-scheduledAt)*1000;
                ReadbackToConsumeMs=(float)(now-gpuCompletedAt)*1000;
            }
            else
            {
                if (!readback.Done) return;
                ScheduledToReadbackMs=(float)(readback.CompletedAt-scheduledAt)*1000;
                ReadbackToConsumeMs=(float)(now-readback.CompletedAt)*1000;
                ComputeMs=ScheduledToReadbackMs;
            }
            pending = false; CompletedInferences++; inferenceRate.Add(now);
            LastInferenceMs = (float)(now - capturedAt) * 1000;
            // Measure cadence before calibration; using only published poses creates a startup dependency.
            if (!discardPending && !double.IsNaN(lastInferenceCapture))
                Tuning.observationInterval = Mathf.Clamp((float)(capturedAt - lastInferenceCapture), 1f / 120, .5f);
            if (!discardPending) lastInferenceCapture = capturedAt;
            if (!discardPending && (native != null || !readback.HasError) && now - capturedAt <= 1)
            {
                if (UsesWorldLandmarks)
                {
                    if (!nativeResult.HasPose) { Array.Clear(Scores,0,26); pipelineStatus="No person detected"; discardPending=false; return; }
                    MediaPipeWorldPose.Map(nativeResult,Points,Scores,measuredWorld);
                }
                else RTMPoseMath.Decode(native != null ? nativeResult.X : readback.X, native != null ? nativeResult.Y : readback.Y, inferenceROI, Points, Scores);
                lastDecodedAt = now; lastDecodedCapture = capturedAt;
                
                int previousCalibrationFrames=solver.CalibrationFrames;
                bool usableFrame=now-capturedAt<=.2 && now>=calibrationNotBefore;
                bool solvedFrame=usableFrame && solver.Solve(Points, Scores, aspect, capturedAt, userHeight, cameraPitchDegrees,
                    0, solved, solvedScores, Tuning);
                if (UsesWorldLandmarks && solver.CalibrationFrames < previousCalibrationFrames) worldPose.Reset();
                if (UsesWorldLandmarks && solver.CalibrationFrames > previousCalibrationFrames) worldPose.CollectNeutral(measuredWorld);
                if (solvedFrame)
                {
                    if (solver.Calibrated)
                    {
                        if (UsesWorldLandmarks) worldPose.Step(measuredWorld,Scores,capturedAt,userHeight,Tuning,solved,solvedScores);
                        Array.Copy(solved, acceptedSolved, 26); Array.Copy(solvedScores, acceptedSolvedScores, 26);
                        for (int i = 0; i < 26; i++)
                        {
                            acceptedImage[i] = new Vector2(Points[i].x * aspect, 1 - Points[i].y);
                            acceptedImageScores[i] = Scores[i];
                        }
                        acceptedPose = true; lastPoseAt = capturedAt; lastReceivedAt = now; poseRate.Add(now); PoseSequence++;
                        AcceptedROI = inferenceROI; AcceptedAspect = aspect;
                        AcceptedRotation = rotation; AcceptedVerticalFlip = verticalFlip;
                        CameraToPoseMs = (float)(now - capturedAt) * 1000;
                    }
                }
                if (lastReceivedAt != now) pipelineStatus = solver.RequireStandingCalibration && !solver.Calibrated
                    ? $"{solver.CalibrationStatus} · {solver.CalibrationFrames}/20"
                    : $"{(solver.Calibrated ? "Torso lost" : $"Calibrating {solver.CalibrationFrames}/20")}: shoulders {Scores[5]:F2}/{Scores[6]:F2}, hips {Scores[11]:F2}/{Scores[12]:F2}. Stand upright, face camera.";
                if (now - capturedAt > .2)
                {
                    DroppedFrames++;
                    pipelineStatus = $"Detected frame is {LastInferenceMs:F0} ms old: preview only; avatar needs results under 200 ms";
                }
                roi = RTMPoseMath.TrackROI(Points, Scores, aspect);
            }
            else
            {
                DroppedFrames++;
                pipelineStatus = readback.HasError ? "GPU readback failed" : "Inference exceeded 200 ms: reduce GPU load";
            }
            discardPending = false;
        }
        private void RefreshPrediction(double now)
        {
            if (!acceptedPose) return;
            if (!ObservationFresh(now, lastPoseAt, lastReceivedAt, Tuning.TrackingGrace))
            {
                for (int i = 0; i < 33; i++) { world[i].Dispose(); normalized[i].Dispose(); }
                lifecycle.SetTimedOut(0); return;
            }
            // Joint loss is timed from delivery, independently of unavoidable GPU processing.
            // Extrapolation still has a hard 40 ms / distance cap.
            if (UsesWorldLandmarks) worldPose.Predict(lastPoseAt+now-lastReceivedAt,CameraToPoseMs/1000,Tuning,solved,solvedScores);
            else solver.Predict(lastPoseAt + now - lastReceivedAt, Tuning, solved, solvedScores, CameraToPoseMs / 1000);
            Publish(now);
        }
        private void Tick()
        {
            double now = Time.realtimeSinceStartupAsDouble;
#if UNITY_EDITOR
            if (replayingRecording) { lastPoseAt=lastReceivedAt=now; return; }
#endif
            ConsumeResult(now);
            RefreshPrediction(now);
            if (cameraFrames.Lifecycle == null || !cameraFrames.Lifecycle.IsAlive(0)) return;
            var frame = cameraFrames.Get(0, 0);
            if (frame?.Texture is not WebCamTexture camera || !camera.isPlaying || camera.width <= 16) return;
            // Texture.updateCount can remain unchanged on Android WebCamTexture even
            // while didUpdateThisFrame is true. Count delivered camera frames ourselves.
            ulong frameSequence = cameraFrames is UnityWebCamBackend source ? source.FrameSequence : (ulong)Time.frameCount;
            // The backend retains the newest available texture. When readback finishes
            // between camera deliveries, use that unprocessed frame immediately.
            if (cameraFrames is UnityWebCamBackend latest)
            { if (frameSequence == 0 || now-latest.LastFrameAvailableTime > .05) return; }
            else if (!camera.didUpdateThisFrame) return;
            if (!IsNewCameraFrame(haveFrame, lastCameraFrame, frameSequence)) return;
            if (pending) { DroppedFrames++; return; } // No inference request queue.
            int nextRotation = camera.videoRotationAngle;
            bool nextFlip = camera.videoVerticallyMirrored;
            float nextAspect = nextRotation == 90 || nextRotation == 270
                ? camera.height / (float)camera.width : camera.width / (float)camera.height;
            if (nextRotation != rotation || nextFlip != verticalFlip || Mathf.Abs(nextAspect - aspect) > .001f)
                ResetCalibration();
            rotation = nextRotation; verticalFlip = nextFlip; aspect = nextAspect;
            lastCameraFrame = frameSequence; haveFrame = true;
            capturedAt = cameraFrames is UnityWebCamBackend backend ? backend.LastFrameAvailableTime : now;
            // Periodic full-frame reacquisition handles an escaped wrist/person without a detector.
            // Follow the detected body crop while calibration is still collecting samples.
            if (now - lastFullFrame >= 1.0 || !HasRecentLandmarks)
            { roi = RTMPoseMath.FitAspect(new Rect(0, 0, 1, 1), aspect); lastFullFrame = now; }
            if (UsesWorldLandmarks) roi=new Rect(0,0,1,1);
            inferenceROI = roi;
            // Android camera textures may be external GPU textures. Resolve into an ordinary
            // 2D render texture before compute sampling; also freeze the pixels for this inference.
            int snapshotWidth = Mathf.Min(camera.width, 960);
            int snapshotHeight = Mathf.Max(1, Mathf.RoundToInt(camera.height * snapshotWidth / (float)camera.width));
            if (cameraSnapshot == null || cameraSnapshot.width != snapshotWidth || cameraSnapshot.height != snapshotHeight)
            {
                ReleaseCameraSnapshot();
                cameraSnapshot = new RenderTexture(snapshotWidth, snapshotHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                cameraSnapshot.Create();
            }
            scheduledAt=Time.realtimeSinceStartupAsDouble;
            FrameAgeAtScheduleMs=(float)(scheduledAt-capturedAt)*1000;
            Graphics.Blit(camera, cameraSnapshot);
            preprocess.SetTexture(kernel, "CameraTexture", cameraSnapshot);
            preprocess.SetVector("ROI", new Vector4(roi.x, roi.y, roi.width, roi.height));
            preprocess.SetInt("Rotation", rotation);
            preprocess.SetInt("VerticalFlip", verticalFlip ? 1 : 0);
            if (native != null)
            {
                int w=192,h=256;
                if (UsesWorldLandmarks) { h=512; w=Mathf.Max(32,Mathf.RoundToInt(h*aspect)); if(w>512) { w=512; h=Mathf.Max(32,Mathf.RoundToInt(w/aspect)); } }
                int count=UsesWorldLandmarks ? w*h : 3*w*h;
                if(nativeInput == null || nativeInput.count != count) { nativeInput?.Release(); nativeInput=new ComputeBuffer(count,4); }
                preprocess.SetBuffer(kernel,UsesWorldLandmarks ? "RGBA" : "Input",nativeInput);
                preprocess.SetInt("ImageWidth",w); preprocess.SetInt("ImageHeight",h);
                preprocess.Dispatch(kernel,(w+7)/8,(h+7)/8,1);
                native.PrepareFrameBuffer(w,h);
                int generation=requestGeneration; var destination=native; long timestamp=(long)(capturedAt*1000);
                pending=true;
                AsyncGPUReadback.Request(nativeInput, request =>
                {
                    if(generation != requestGeneration) return;
                    if(request.hasError) { nativeError="Camera GPU readback failed"; return; }
                    gpuCompletedAt=Time.realtimeSinceStartupAsDouble;
                    try
                    {
                        if(UsesWorldLandmarks)
                        { request.GetData<uint>().CopyTo(destination.FrameRGBA); destination.Begin(null,destination.FrameRGBA,w,h,timestamp); }
                        else
                        { request.GetData<float>().CopyTo(destination.FrameFloats); destination.Begin(destination.FrameFloats,null,w,h,timestamp); }
                    }
                    catch(Exception e) { nativeError=e.Message; }
                });
            }
            else
            {
                preprocess.SetBuffer(kernel, "Input", ComputeTensorData.Pin(input).buffer);
                preprocess.Dispatch(kernel, 24, 32, 1);
                worker.Schedule(input);
                outputX = worker.PeekOutput("simcc_x") as Tensor<float>;
                outputY = worker.PeekOutput("simcc_y") as Tensor<float>;
                if (outputX == null || outputY == null || outputX.shape != new TensorShape(1, 26, 384)
                    || outputY.shape != new TensorShape(1, 26, 512))
                    throw new InvalidOperationException("Expected RTMPose-s Halpe26 SimCC model outputs.");
                readback.Begin(outputX, outputY); pending = true;
            }
            ScheduleMs=(float)(Time.realtimeSinceStartupAsDouble-scheduledAt)*1000;
        }

        private void Publish(double now, bool? frontOverride = null)
        {
            bool front = frontOverride ?? (cameraFrames?.Get(0, 0)?.SourceProperties?.IsFrontFacing ?? false);
            for (int i = 0; i < 33; i++)
            {
                int j = RTMPoseMath.ToMediaPipe[i];
                Vector3 p = solved[j];
                // Same convention as MPUPoseLandmarkAdapter, before the existing scene transforms.
                p.x *= (settings.Mirror ? 1 : -1) * (settings.PerspectiveFlip ? -1 : 1);
                p.z *= settings.PerspectiveFlip ? 1 : -1;
                world[i] = new Landmark(p, solvedScores[j], solvedScores[j]);
                var uv = Points[j];
                normalized[i] = new NormalizedLandmark(new Vector3(front ? 1 - uv.x : uv.x, 1 - uv.y, 0), Scores[j], Scores[j]);
            }
            // Body26 has no fingers. Give the retargeter's hand direction a short forearm extension.
            for (int side = 0; side < 2; side++)
            {
                int wrist = 15 + side, elbow = 13 + side;
                var hand = world[wrist];
                hand.Position += (world[wrist].Position - world[elbow].Position).normalized * .08f;
                hand.Visibility = Mathf.Min(hand.Visibility, world[elbow].Visibility);
                Vector3 across = (world[12].Position - world[11].Position).normalized * .025f;
                world[21 + side] = hand;
                hand.Position += across; world[19 + side] = hand;
                hand.Position -= across * 2; world[17 + side] = hand;
            }
            lifecycle.SetReady(0); lifecycle.NotifyUpdated(0);
            PoseReady?.Invoke();
        }
#if UNITY_EDITOR
        private bool replayingRecording;
        // Real native inference through the normal consume/calibrate/publish path, without a physical editor camera.
        public bool EditorSubmitNativeFrame(float[] chw,uint[] rgba,int width,int height)
        {
            if(native == null || !native.Ready || pending) return false;
            aspect=width/(float)height; rotation=0; verticalFlip=false;
            inferenceROI=new Rect(0,0,1,1);
            capturedAt=scheduledAt=gpuCompletedAt=Time.realtimeSinceStartupAsDouble;
            pending=true; native.Begin(chw,rgba,width,height,(long)(capturedAt*1000)); return true;
        }

        // Replay uses the same solver, publication and observed-joint gates as the phone.
        public bool SubmitRecordedObservation(Vector2[] points, float[] scores, float imageAspect, double captureTime)
        {
            Array.Copy(points, Points, 26); Array.Copy(scores, Scores, 26);
            aspect=imageAspect;
            if (!solver.Solve(Points, Scores, aspect, captureTime, userHeight, cameraPitchDegrees, 0, solved, solvedScores, Tuning) || !solver.Calibrated) return false;
            double now=Time.realtimeSinceStartupAsDouble;
            replayingRecording=true;
            acceptedPose=true; lastPoseAt=lastReceivedAt=now; PoseSequence++;
            Publish(now, false); return true;
        }
#endif

        public Landmark Get(int group, PoseLandmarks index) => Get(group, (int)index);
        public Landmark Get(int group, int index) => group == 0 && index >= 0 && index < 33 ? world[index] : default;
        NormalizedLandmark IProvider<PoseLandmarks, NormalizedLandmark>.Get(int group, PoseLandmarks index)
            => group == 0 && (int)index >= 0 && (int)index < 33 ? normalized[(int)index] : default;
        NormalizedLandmark IProvider<PoseLandmarks, NormalizedLandmark>.Get(int group, int index)
            => group == 0 && index >= 0 && index < 33 ? normalized[index] : default;
        private void ReleaseCameraSnapshot()
        {
            if (cameraSnapshot == null) return;
            cameraSnapshot.Release(); Destroy(cameraSnapshot); cameraSnapshot = null;
        }
        private void OnDisable() { Release(); }
        private void OnDestroy() { Release(); }
        public void Release()
        {
            requestGeneration++;
            if(modelLoading != null) { StopCoroutine(modelLoading); modelLoading=null; }
            native?.Dispose(); native=null; nativeResult=null; nativeError=null;
            nativeInput?.Release(); nativeInput=null; worldPose.Reset();
            pending = false; acceptedPose = false; readback.Reset(); calibrationNotBefore = 0;
            lastDecodedAt = lastDecodedCapture = double.NegativeInfinity;
            worker?.Dispose(); worker = null;
            input?.Dispose(); input = null;
            outputX = outputY = null;
            if (preprocess != null) Destroy(preprocess);
            preprocess = null; ReleaseCameraSnapshot();
            for (int i = 0; i < 33; i++) { world[i].Dispose(); normalized[i].Dispose(); }
            lifecycle.SetDisposed(0);
        }
    }
}
