using System;
using Tracking4All;
using UnityEngine;

namespace FitFight.BodyTracking
{
    /// <summary>Only switches the local solution's inference; UDP opponent providers stay untouched.</summary>
    public sealed class BodyTrackingController : MonoBehaviour
    {
        private PoseSolution solution;
        private MPUPoseTrackingSolution mediaPipe;
        private RTMPosePoseProvider rtmPose;
        private IBodyPoseProvider baseline;
        private bool usingRTM, originalEnabled;
        private DirectHumanoidRetargeter direct;
        private global::Tracking4All.Avatar localAvatar;
        private LegGroundLock legacyLock;
        private bool lockWasEnabled;
        private float nextBindingCheck;
        private UnityWebCamBackend cameraBackend;
        private static int previousTargetFPS, frameTargetOwners;
        private bool ownsFrameTarget;
        public float BaselineCutoff { get; private set; } = 2;
        public float BaselineMotionGain { get; private set; } = 20;
        public void SetBaselineSmoothing(float cutoff, float gain)
        {
            BaselineCutoff = Mathf.Clamp(cutoff, 1, 10); BaselineMotionGain = Mathf.Clamp(gain, 0, 40);
            solution?.ConfigureBaselineSmoothing(BaselineCutoff, BaselineMotionGain);
        }
        public void RetryEngine() { RestoreBaseline(); ApplySelection(); }
        public void ApplyGameTestProfile()
        {
            if(rtmPose==null) return;
            bool restart=rtmPose.Tuning.mediaPipeGpu || !rtmPose.Tuning.nativeCpuCamera;
            var tuning=rtmPose.Tuning;
            tuning.nativeCpuCamera=true;tuning.mediaPipeGpu=false;
            tuning.filtering=tuning.mediaPipeRegionFiltering=tuning.mediaPipeMotionAwareFiltering=tuning.uprightBody=true;
            tuning.prediction=.01f;tuning.requestedCameraFPS=60;
            if(restart) RetryEngine();
            else
            {
                // A prior runtime fallback must not silently survive a new
                // explicit game test just because the saved preference is on.
                cameraBackend?.UseNativeCpu(true);
                rtmPose.SetCameraFPS(60);
            }
        }
        public void RetryCamera() { cameraBackend?.OnSettingsApplied(); }
        public float CameraFPS => cameraBackend != null ? cameraBackend.ActualFrameRate : 0;
        public DirectHumanoidRetargeter DirectDriver => direct;
        public string RetargetingStatus => !usingRTM ? "Puppet" : rtmPose.Tuning.directAvatar
            ? direct != null && direct.Target != null ? "Direct avatar" : "Direct: select a local fighter"
            : "Responsive puppet";
        public string Status { get; private set; } = "Tracking4All";
        public RTMPosePoseProvider RTMPose => rtmPose;

        public static void Attach(PoseSolution target)
        {
            var controller = target.GetComponent<BodyTrackingController>();
            if (controller == null) controller = target.gameObject.AddComponent<BodyTrackingController>();
            if (controller.solution != null) return;
            controller.solution = target;
            controller.baseline = target.BaselineProvider;
            controller.mediaPipe = target.GetComponentInChildren<MPUPoseTrackingSolution>(true);
            controller.cameraBackend = controller.mediaPipe != null ? controller.mediaPipe.CameraFrames as UnityWebCamBackend : null;
            if (controller.mediaPipe != null && target.GetComponent<BodyTrackingDiagnostics>() == null)
                target.gameObject.AddComponent<BodyTrackingDiagnostics>();
            controller.SetBaselineSmoothing(2, 20);
            controller.ApplySelection();
        }

        private void OnEnable()
        {
            BodyTrackingSelection.Changed += ApplySelection;
            if (solution != null) ApplySelection();
        }
        private void OnDisable()
        {
            BodyTrackingSelection.Changed -= ApplySelection;
            RestoreBaseline();
        }

        private void ApplySelection()
        {
            if (solution == null || !isActiveAndEnabled) return;
            if (BodyTrackingSelection.Selected == BodyTrackingSystem.Tracking4All)
            { RestoreBaseline(); Status = "Tracking4All"; return; }
            if (usingRTM && rtmPose.ConfiguredEngine == BodyTrackingSelection.Selected) return;
            if (usingRTM) RestoreBaseline();
            if (mediaPipe == null)
            { Status = "RTMPose unavailable: no local camera solution"; return; }
            if (rtmPose == null)
            {
                rtmPose = gameObject.AddComponent<RTMPosePoseProvider>();
                rtmPose.enabled = false;
                rtmPose.Failed += OnFailure;
                rtmPose.PoseReady += solution.FlushRTMPose;
            }
            try
            {
                // Allocate and validate the model before stopping the working baseline.
                rtmPose.Prepare(mediaPipe.CameraFrames, mediaPipe, BodyTrackingSelection.Selected);
                originalEnabled = mediaPipe.enabled;
                usingRTM = true;
                mediaPipe.enabled = false; // OnDisable stops its graph, retaining the camera backend.
                cameraBackend?.UseNativeCpu(rtmPose.UsesWorldLandmarks && rtmPose.Tuning.nativeCpuCamera);
                solution.SetBodyPoseProvider(rtmPose);
                rtmPose.enabled = true;
                rtmPose.SetCameraFPS(rtmPose.Tuning.requestedCameraFPS);
                if (frameTargetOwners++ == 0) previousTargetFPS = Application.targetFrameRate;
                ownsFrameTarget = true;
                if (Application.targetFrameRate < 60) Application.targetFrameRate = 60;
                RefreshAvatarBinding();
                Status = rtmPose.EngineLabel;
            }
            catch (Exception e) { OnFailure(e.Message); }
        }

        private void Update()
        {
            if (!usingRTM || Time.unscaledTime < nextBindingCheck) return;
            nextBindingCheck = Time.unscaledTime + .25f;
            RefreshAvatarBinding();
        }
        public void SetDirectAvatar(bool useDirect)
        {
            if (rtmPose == null) return;
            rtmPose.Tuning.directAvatar = useDirect;
            RefreshAvatarBinding();
        }
        private void RefreshAvatarBinding()
        {
            global::Tracking4All.Avatar target = null;
            foreach (var candidate in FindObjectsByType<global::Tracking4All.Avatar>(FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled && candidate.PosePuppet != null && candidate.PosePuppet.PoseSource == solution.gameObject)
                { target = candidate; break; }
            if (target != localAvatar)
            {
                ReleaseAvatarBinding(); localAvatar = target;
                if (target != null)
                {
                    legacyLock = target.GetComponent<LegGroundLock>();
                    if (legacyLock != null) { lockWasEnabled = legacyLock.enabled; legacyLock.enabled = false; }
                }
            }
            if (!rtmPose.Tuning.directAvatar || localAvatar == null)
            { if (direct != null) direct.enabled = false; return; }
            if (direct != null && direct.Target == localAvatar && direct.isActiveAndEnabled) return;
            if (direct == null) direct = gameObject.AddComponent<DirectHumanoidRetargeter>();
            direct.enabled = true;
            try { direct.Bind(localAvatar, rtmPose); }
            catch (Exception e) { direct.enabled = false; rtmPose.Tuning.directAvatar = false; Debug.LogWarning(e.Message, this); }
        }
        private void ReleaseAvatarBinding()
        {
            if (direct != null) direct.enabled = false;
            if (legacyLock != null) legacyLock.enabled = lockWasEnabled;
            legacyLock = null; localAvatar = null;
        }
        private void OnFailure(string error)
        {
            RestoreBaseline();
            Status = BodyTrackingSelection.Label(BodyTrackingSelection.Selected) + " failed; Tracking4All active: " + error;
            Debug.LogError(Status, this);
        }

        private void RestoreBaseline()
        {
            if (rtmPose != null) { rtmPose.enabled = false; rtmPose.Release(); }
            ReleaseAvatarBinding();
            if (!usingRTM) return;
            usingRTM = false;
            if (ownsFrameTarget)
            {
                ownsFrameTarget = false;
                if (--frameTargetOwners == 0) Application.targetFrameRate = previousTargetFPS;
            }
            cameraBackend?.UseNativeCpu(false);
            cameraBackend?.OverrideFrameRate(null);
            solution.SetBodyPoseProvider(baseline);
            if (mediaPipe != null)
            {
                mediaPipe.enabled = originalEnabled;
                if (mediaPipe.isActiveAndEnabled) mediaPipe.Restart();
            }
        }
    }
}
