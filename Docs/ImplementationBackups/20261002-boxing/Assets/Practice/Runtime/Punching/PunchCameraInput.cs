using System;
using Tracking4All;
using FitFight.BodyTracking;
using UnityEngine;

namespace FitFight.Practice
{
    /// <summary>Atomic snapshots of independent observations before display filtering.</summary>
    public sealed class PunchCameraInput : IDisposable
    {
        [Serializable]
        public struct Frame
        {
            public long sequence;
            public double time, received;
            public bool rtm;
            public PunchArmObservation left, right;
            public Vector2 ls, rs, lh, rh, le, re, lw, rw;
            // Present only while recording. Native model order, before feature normalization.
            public string jointLayout;
            public Vector2[] imageJoints;
            public float[] jointScores;
            public float imageAspect;
            public int cameraRotation;
            public bool cameraVerticalFlip, mirrored;
            public Rect roi;
        }
        public volatile bool CaptureFullBody;
        private readonly PoseSolution solution;
        private readonly MPUPoseTrackingSolution baseline;
        private readonly INormalizedLandmarkProvider<MPPoseLandmarks> raw;
        private readonly object gate = new object();
        private Frame pending;
        private long baselineSequence, consumed = -1, rtmSequence = -1;
        private bool lastRTM;
        private int lastEngineGeneration=-1;
        private float aspect = 1;
        private bool mirrored;
        private int cameraRotation;
        private bool cameraVerticalFlip;
        private readonly double clockOffset;
        private static double Clock => System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency;
        public PunchCameraInput(PoseSolution source)
        {
            solution = source;
            baseline = source != null ? source.GetComponentInChildren<MPUPoseTrackingSolution>(true) : null;
            raw = baseline as INormalizedLandmarkProvider<MPPoseLandmarks>;
            clockOffset = Time.realtimeSinceStartupAsDouble - Clock;
            if (raw?.Lifecycle != null) raw.Lifecycle.OnUpdated += ReceiveBaseline;
        }
        private void ReceiveBaseline(int group)
        {
            // Provider lifecycle may deliver on a worker. Only read its completed snapshot;
            // Unity scene/camera properties are cached on the main thread.
            lock (gate)
            {
                Vector2 P(int i) { var p = raw.Get(0, i).Position; return new Vector2(p.x * aspect, p.y); }
                float C(int i) => raw.Get(0, i).Visibility;
                int l = mirrored ? 1 : 0, r = 1 - l;
                pending = Make(++baselineSequence, Clock + clockOffset, Clock + clockOffset, false,
                    P(11+l), P(11+r), P(23+l), P(23+r), P(13+l), P(13+r), P(15+l), P(15+r),
                    Mathf.Min(C(11), C(12), C(23), C(24), C(13+l), C(15+l)),
                    Mathf.Min(C(11), C(12), C(23), C(24), C(13+r), C(15+r)));
                if (CaptureFullBody)
                {
                    pending.jointLayout = "MediaPipe33";
                    pending.imageJoints = new Vector2[33]; pending.jointScores = new float[33];
                    for (int i = 0; i < 33; i++) { pending.imageJoints[i] = P(i); pending.jointScores[i] = C(i); }
                    pending.imageAspect = aspect; pending.cameraRotation = cameraRotation;
                    pending.cameraVerticalFlip = cameraVerticalFlip; pending.mirrored = mirrored;
                    pending.roi = new Rect(0, 0, 1, 1);
                }
            }
        }
        public bool TryRead(out Frame frame, out bool changedEngine)
        {
            frame = default;
            var pose = solution != null ? solution.ActiveRTMPose : null;
            bool rtm = pose != null && pose.isActiveAndEnabled;
            changedEngine = rtm != lastRTM || rtm && lastEngineGeneration != pose.EngineGeneration; lastRTM = rtm;
            if(rtm) lastEngineGeneration=pose.EngineGeneration;
            if (changedEngine) { consumed = baselineSequence; rtmSequence = -1; }
            if (rtm)
            {
                if (!pose.HasRecentPose || pose.PoseSequence == rtmSequence) return false;
                rtmSequence = pose.PoseSequence;
                Vector2 P(PoseLandmarks joint) => pose.AcceptedImagePoint(joint);
                float C(PoseLandmarks joint) => pose.AcceptedImageConfidence(joint);
                float torso = Mathf.Min(C(PoseLandmarks.LEFT_SHOULDER), C(PoseLandmarks.RIGHT_SHOULDER), C(PoseLandmarks.LEFT_HIP), C(PoseLandmarks.RIGHT_HIP));
                frame = Make(rtmSequence, pose.AcceptedCaptureTime, pose.AcceptedReceiveTime, true,
                    P(PoseLandmarks.LEFT_SHOULDER), P(PoseLandmarks.RIGHT_SHOULDER), P(PoseLandmarks.LEFT_HIP), P(PoseLandmarks.RIGHT_HIP),
                    P(PoseLandmarks.LEFT_ELBOW), P(PoseLandmarks.RIGHT_ELBOW), P(PoseLandmarks.LEFT_WRIST), P(PoseLandmarks.RIGHT_WRIST),
                    Mathf.Min(torso, C(PoseLandmarks.LEFT_ELBOW), C(PoseLandmarks.LEFT_WRIST)),
                    Mathf.Min(torso, C(PoseLandmarks.RIGHT_ELBOW), C(PoseLandmarks.RIGHT_WRIST)));
                if (CaptureFullBody)
                {
                    frame.jointLayout = "Halpe26"; frame.imageJoints = new Vector2[26]; frame.jointScores = new float[26];
                    pose.CopyAcceptedImage(frame.imageJoints, frame.jointScores);
                    frame.roi = pose.AcceptedROI; frame.imageAspect = pose.AcceptedAspect;
                    frame.cameraRotation = pose.AcceptedRotation; frame.cameraVerticalFlip = pose.AcceptedVerticalFlip;
                    frame.mirrored = baseline != null && baseline.Mirror;
                }
                return true;
            }
            if (baseline == null || !baseline.isActiveAndEnabled) return false;
            var camera = baseline.CameraFrames?.Get(0, 0)?.Texture as WebCamTexture;
            lock (gate)
            {
                mirrored = baseline.Mirror;
                if (camera != null && camera.width > 16)
                {
                    aspect = camera.videoRotationAngle % 180 == 0 ? camera.width / (float)camera.height : camera.height / (float)camera.width;
                    cameraRotation = camera.videoRotationAngle; cameraVerticalFlip = camera.videoVerticallyMirrored;
                }
                if (pending.sequence == consumed || pending.sequence == 0) return false;
                consumed = pending.sequence; frame = pending;
                // Baseline timestamps are delivery, not physical camera capture timestamps.
                return Time.realtimeSinceStartupAsDouble - frame.received <= .2;
            }
        }
        private static Frame Make(long seq, double time, double received, bool rtm, Vector2 ls, Vector2 rs,
            Vector2 lh, Vector2 rh, Vector2 le, Vector2 re, Vector2 lw, Vector2 rw, float lc, float rc)
        {
            PunchFeatures.TryCreate(ls, rs, (lh+rh)*.5f, le, lw, lc, out var left);
            PunchFeatures.TryCreate(rs, ls, (lh+rh)*.5f, re, rw, rc, out var right);
            return new Frame { sequence=seq, time=time, received=received, rtm=rtm, left=left, right=right,
                ls=ls, rs=rs, lh=lh, rh=rh, le=le, re=re, lw=lw, rw=rw };
        }
        public void Dispose() { if (raw?.Lifecycle != null) raw.Lifecycle.OnUpdated -= ReceiveBaseline; }
    }
}
