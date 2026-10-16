// UnityWebCam
// (C) 2024-2025 G8gaming Ltd.
using Mediapipe.Unity;
using System;
using System.Collections;
using UnityEngine;

namespace Tracking4All
{

    /// <summary>
    /// An instance of a unity instantiated WebCam.
    /// </summary>
    public class UnityWebCam : IDisposableData, IVideoSourceProperties
    {
        public IWebcamSettings Settings => settings;

        private MonoBehaviour owner;
        private WebCamDevice webCamDevice;
        private WebCamTexture texture;
        private IWebcamSettings settings;
        private Coroutine initializationRoutine;
        private bool live;
        private FitFight.BodyTracking.AndroidCpuCamera cpuCamera;
        public FitFight.BodyTracking.AndroidCpuCamera CpuCamera => cpuCamera;
        public bool NativeRequested { get; set; }
        public string NativeError { get; private set; }
        public string NativeLifecycle { get; private set; }
        public bool PollNative() => cpuCamera != null && cpuCamera.Poll();

        public Texture Texture => cpuCamera != null ? cpuCamera.Texture : texture;

        public bool IsPrepared => live && (cpuCamera != null ? cpuCamera.IsPrepared : texture != null);
        public bool IsPlaying => live && (cpuCamera != null ? cpuCamera.IsPlaying : texture != null && texture.isPlaying);
        public bool UpdatedThisFrame => live && texture != null && texture.didUpdateThisFrame;
        public uint UpdateCounter => texture != null ? texture.updateCount : 0;

        public bool IsVerticallyFlipped => IsPrepared && (cpuCamera != null ? cpuCamera.IsVerticallyFlipped : texture.videoVerticallyMirrored);
        public bool IsFrontFacing => IsPrepared && (cpuCamera != null ? cpuCamera.IsFrontFacing : webCamDevice.isFrontFacing);
        public RotationAngle Rotation => !IsPrepared ? RotationAngle.Rotation0 : cpuCamera != null ? cpuCamera.Rotation : (RotationAngle)texture.videoRotationAngle;

        public bool Parameterized { get; private set; }

        public int Width => IsPrepared ? Texture.width : 0;
        public int Height => IsPrepared ? Texture.height : 0;
        public int FrameRate => IsPrepared ? Settings.RequestedFrameRate : 0;

        public string Name => IsPrepared ? cpuCamera != null ? cpuCamera.Name : texture.deviceName : "N?A";

        public void Parameterize(MonoBehaviour owner, IWebcamSettings settings)
        {
            this.settings = settings;
            this.owner = owner;

            Parameterized = true;
        }

        public void Create()
        {
            if (texture != null)
            {
                Logger.LogError("Webcam must be disposed before being created!", this);
                return;
            }

            initializationRoutine = owner.StartCoroutine(InitializeWebcamProcess());
        }
        private IEnumerator InitializeWebcamProcess()
        {
            NativeError=null;
            if (texture != null)
            {
                Logger.LogInfo("Disposing current webcam " + texture.name + "...", this);
                DisposeWebCam();
                Logger.LogInfo("Disposed current webcam.", this);
            }

            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Logger.LogError("No webcam devices were detected.", this);
                Parameterized = false;
                initializationRoutine = null;
                yield break;
            }

            int deviceIndex = Settings.WebcamDeviceIndex;
            if (deviceIndex < 0 || deviceIndex >= devices.Length)
            {
                // Device Simulator and many Android devices expose only one camera
                // through WebCamTexture. Keep tracking alive with that source.
                Logger.LogWarning($"The webcam device {deviceIndex} is out of range. Detected {devices.Length} devices; using device 0.", this);
                deviceIndex = 0;
            }

            webCamDevice = devices[deviceIndex];

#if UNITY_ANDROID && !UNITY_EDITOR
            if (NativeRequested)
            {
                NativeError=null;
                cpuCamera = new FitFight.BodyTracking.AndroidCpuCamera(webCamDevice.name, webCamDevice.isFrontFacing, Settings.RequestedFrameRate);
                double nativeDeadline = Time.realtimeSinceStartupAsDouble + 8;
                while (!cpuCamera.IsPrepared && string.IsNullOrEmpty(cpuCamera.Error) && Time.realtimeSinceStartupAsDouble < nativeDeadline)
                { cpuCamera.Poll(); yield return null; }
                if (cpuCamera.IsPrepared)
                { live=true; initializationRoutine=null; yield break; }
                NativeError=string.IsNullOrEmpty(cpuCamera.Error) ? "Camera2 startup timed out" : cpuCamera.Error;
                Logger.LogWarning("Native camera fallback: " + NativeError, this);
                cpuCamera.Dispose(); NativeLifecycle=cpuCamera.LifecycleTrace;cpuCamera=null;
            }
#endif
            Logger.LogInfo("Launching Webcam: " + webCamDevice.name + ".", this);
            texture = new WebCamTexture(
                webCamDevice.name,
                Settings.RequestedWidth, Settings.RequestedHeight,
                Settings.RequestedFrameRate);
            texture.Play();

            yield return WaitForWebCamTexture();

            initializationRoutine = null;
        }
        private IEnumerator WaitForWebCamTexture()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 10;
            UnityEngine.Debug.Log("Waiting for Webcam to load...");
            yield return new WaitUntil(() => texture == null || texture.width > 16 || Time.realtimeSinceStartupAsDouble >= deadline);
            if (texture == null || texture.width <= 16)
            {
                string device = webCamDevice.name;
                DisposeWebCam();
                Parameterized = false;
                initializationRoutine = null;
                Logger.LogError("Failed to start Webcam " + device + ". Check permission, then retry camera.", this);
            }
            else
            {
                live = true;
                Logger.LogInfo("Started Webcam: " + webCamDevice.name + " successfully @" + texture.width + "x" + texture.height, this);
            }
        }

        private void DisposeWebCam()
        {
            live = false;
            if(cpuCamera!=null) { cpuCamera.Dispose();NativeLifecycle=cpuCamera.LifecycleTrace;cpuCamera=null; }
            if (texture != null)
            {
                if (texture.isPlaying) texture.Stop();
                WebCamTexture.Destroy(texture);
            }
            texture = null;
        }
        public void Dispose()
        {
            if (initializationRoutine != null)
            {
                owner.StopCoroutine(initializationRoutine);
                initializationRoutine = null;
            }
            DisposeWebCam();
            settings = null;
            Parameterized = false;
        }
    }
}
