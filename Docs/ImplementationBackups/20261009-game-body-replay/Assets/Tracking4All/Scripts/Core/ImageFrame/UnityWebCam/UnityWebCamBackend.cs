using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Tracking4All
{
    [DefaultExecutionOrder(-950)]
    /// <summary>
    /// Provides UnityWebCams.
    /// </summary>
    public class UnityWebCamBackend : MonoBehaviour, ISettingsUser,
        IImageFrameProvider
    {
        // Parts taken from homuler webcamsource (mostly permissions stuff)

        [SerializeField] protected WebCamSettings cameraSettings;

        // Mobile camera APIs cannot reliably open the same camera for both fighters.
        private static UnityWebCamBackend cameraOwner;
#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool permissionRequestInFlight;
        private static PermissionCallbacks permissionCallbacks;
#endif
        private Coroutine readyingRoutine;
        private bool starting, paused;
        private double nextRetry, openedAt;
        private TableGroup<UnityWebCam> cameras;
        private ImageFrameAdapter imageFrameAdapter;
        private readonly FitFight.BodyTracking.PoseRateMeter frameRateMeter = new FitFight.BodyTracking.PoseRateMeter();
        private float? savedFrameRate;
        public bool NativeCpuRequested { get; private set; }
        public FitFight.BodyTracking.AndroidCpuCamera CpuCamera => cameras?.Get(0,0)?.CpuCamera;
        public string CapturePath => CpuCamera != null ? "camera2-cpu" : "unity-gpu-readback";
        private string runtimeNativeFailure;
        public string NativeFallback => runtimeNativeFailure ?? cameras?.Get(0,0)?.NativeError;
        public void UseNativeCpu(bool value)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(NativeCpuRequested == value) return;
            NativeCpuRequested=value;runtimeNativeFailure=null;
            if(isActiveAndEnabled) OnSettingsApplied();
#endif
        }
        public string CameraStatus { get; private set; } = "Starting camera";
        public float ActualFrameRate => frameRateMeter.Get(Time.realtimeSinceStartupAsDouble);
        public double LastFrameAvailableTime { get; private set; }
        public ulong FrameSequence { get; private set; }
        public int RequestedFrameRate => (int)cameraSettings.frameRate.Value;
        public void OverrideFrameRate(int? fps)
        {
            if (fps.HasValue)
            {
                if (!savedFrameRate.HasValue) savedFrameRate = cameraSettings.frameRate.Value;
                if (Mathf.Approximately(cameraSettings.frameRate.Value, fps.Value)) return;
                cameraSettings.frameRate.Set(fps.Value);
            }
            else
            {
                if (!savedFrameRate.HasValue) return;
                cameraSettings.frameRate.Set(savedFrameRate.Value); savedFrameRate = null;
            }
            if (isActiveAndEnabled) OnSettingsApplied();
        }

        private void Awake()
        {
            cameraSettings.cameraIndex.SetName(gameObject.name + " / Camera");
            cameraSettings.width.SetName(gameObject.name + " / Width");
            cameraSettings.height.SetName(gameObject.name + " / Height");
            cameraSettings.frameRate.SetName(gameObject.name + " / FPS");
            cameras = new TableGroup<UnityWebCam>(1, 1);

            imageFrameAdapter = new ImageFrameAdapter(1, this, cameras, -1);
        }

        private void OnEnable()
        {
            SettingsManager.Instance.AddSetting(cameraSettings, this);
            paused = false;
            Ready();
        }
        private void Ready()
        {
            if (starting || !isActiveAndEnabled || paused) return;
            starting = true;
            readyingRoutine = StartCoroutine(ReadyRoutine());
        }
        private IEnumerator ReadyRoutine()
        {
            // Ensure the coroutine handle is assigned before any early failure completes.
            yield return null;
            CameraStatus = "Waiting for camera access";
            yield return new WaitUntil(() => cameraOwner == null || cameraOwner == this);
            cameraOwner = this;
            yield return GetPermission();
            if (!HasCameraPermission())
            {
                CameraStatus = "Camera permission required: allow camera access in app settings";
                FinishStartup(false); yield break;
            }
            yield return new WaitUntil(() => !paused);
            CameraStatus = "Opening camera";
            UnityWebCam cam = cameras.Get(0, 0);
            cam.Dispose();
            cam.Parameterize(this, cameraSettings);
            cam.NativeRequested = NativeCpuRequested;
            cam.Create();
            yield return new WaitUntil(() => cam.IsPrepared || !cam.Parameterized);
            if (!cam.IsPrepared)
            {
                CameraStatus = WebCamTexture.devices.Length == 0 ? "No camera detected; reconnect camera" : "Camera failed to open; retrying";
                FinishStartup(false); yield break;
            }
            imageFrameAdapter.Ready(0);
            openedAt = Time.realtimeSinceStartupAsDouble;
            CameraStatus = $"{cam.Name} {cam.Width}x{cam.Height}";
            UnityEngine.Debug.Log("[FitFight Camera] " + CameraStatus, this);
            FinishStartup(true);
        }
        private void FinishStartup(bool success)
        {
            starting = false; readyingRoutine = null;
            nextRetry = Time.realtimeSinceStartupAsDouble + 2;
            if (!success && cameraOwner == this) cameraOwner = null;
        }
        private void OnDisable()
        {
            SettingsManager.Instance.RemoveSetting(cameraSettings);
            Stop();
        }
        private void Stop()
        {
            if (readyingRoutine != null) StopCoroutine(readyingRoutine);
            readyingRoutine = null; starting = false;
            frameRateMeter.Reset(); LastFrameAvailableTime = 0;
            imageFrameAdapter?.DisposeAll();
            cameras?.Get(0, 0)?.Dispose();
            if (cameraOwner == this) cameraOwner = null;
            nextRetry = Time.realtimeSinceStartupAsDouble + 2;
        }
        private void Update()
        {
            var cam = cameras.Get(0, 0);
            double now = Time.realtimeSinceStartupAsDouble;
            bool nativeUpdated = cam.PollNative();
            if (nativeUpdated || cam.UpdatedThisFrame)
            {
                LastFrameAvailableTime = nativeUpdated ? cam.CpuCamera.AvailableAt : now; FrameSequence++;
                frameRateMeter.Add(now);
                imageFrameAdapter.Update(0, cameras);
            }
            // Recover permission returns, disconnected cameras and stalled capture without calibration.
            if (CpuCamera != null && !string.IsNullOrEmpty(CpuCamera.Error))
            { runtimeNativeFailure=CpuCamera.Error;CameraStatus="Native camera failed: " + runtimeNativeFailure; NativeCpuRequested=false; Stop(); Ready(); return; }
            if (!paused && !starting && now >= nextRetry && HasCameraPermission()
                && (!cam.IsPrepared || now - System.Math.Max(openedAt, LastFrameAvailableTime) > 3))
            { Stop(); Ready(); }
        }
        private void OnApplicationPause(bool pause)
        {
            paused = pause;
            // Permission prompts may pause Android. Keep the request alive until its callback.
            if (pause && !starting) Stop();
            else if (!pause && !starting && isActiveAndEnabled) Ready();
        }
        private static bool HasCameraPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(Permission.Camera);
#elif UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return Application.HasUserAuthorization(UserAuthorization.WebCam);
#else
            return true;
#endif
        }
        private IEnumerator GetPermission()
        {
            if (HasCameraPermission()) yield break;
            CameraStatus = "Waiting for camera permission";
#if UNITY_ANDROID && !UNITY_EDITOR
            // Keep a single request alive even if a fighter switch cancels this coroutine.
            if (!permissionRequestInFlight)
            {
                permissionRequestInFlight = true;
                permissionCallbacks = new PermissionCallbacks();
                permissionCallbacks.PermissionGranted += _ => permissionRequestInFlight = false;
                permissionCallbacks.PermissionDenied += _ => permissionRequestInFlight = false;
                permissionCallbacks.PermissionDeniedAndDontAskAgain += _ => permissionRequestInFlight = false;
                Permission.RequestUserPermission(Permission.Camera, permissionCallbacks);
            }
            yield return new WaitUntil(() => !permissionRequestInFlight || HasCameraPermission());
#elif UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
#endif
        }
        public void OnSettingsApplied()
        {
            Stop();
            if (isActiveAndEnabled) Ready();
        }

        // provider implementation
        IProviderLifecycle IProvider<ImageFrameIndex, ImageFrame>.Lifecycle => imageFrameAdapter?.Lifecycle;
        int IProvider<ImageFrameIndex, ImageFrame>.GroupSize => 1;
        int IProvider<ImageFrameIndex, ImageFrame>.DataCount => 1;
        ImageFrame IProvider<ImageFrameIndex, ImageFrame>.Get(int group, ImageFrameIndex index)
        {
            return imageFrameAdapter.Get(group, index);
        }
        ImageFrame IProvider<ImageFrameIndex, ImageFrame>.Get(int group, int index)
        {
            return imageFrameAdapter.Get(group, index);
        }



        /// <summary>
        /// Convert from unity webcam to standardized image frame. 
        /// </summary>
        public class ImageFrameAdapter : BackendAdapter<TableGroup<UnityWebCam>, ImageFrameIndex, ImageFrame>
        {
            private TableGroup<UnityWebCam> data;

            public ImageFrameAdapter(int groupSize, MonoBehaviour owner, TableGroup<UnityWebCam> data, float aliveThreshold)
                : base(groupSize, owner, aliveThreshold)
            {
                this.data = data;
            }


            protected override void SelfReady(int group)
            {
                base.SelfReady(group);

                UnityWebCam source = data.Get(group, 0);
                ImageFrame frame = Get(group, 0);
                frame.Hook(source.Texture, source);
            }
            protected override void SelfDispose(int group)
            {
                base.SelfDispose(group);

                ImageFrame frame = Get(group, 0);
                frame.Dispose();
            }

            protected override void SelfUpdate(int group, TableGroup<UnityWebCam> from)
            {
                // Native capture replaces its preview texture when upright dimensions
                // change. Keep the shared frame hooked to the current live texture.
                var source=from.Get(group,0);
                var frame=Get(group,0);
                if(frame.Texture!=source.Texture) frame.Hook(source.Texture,source);
            }

        }
    }

}
