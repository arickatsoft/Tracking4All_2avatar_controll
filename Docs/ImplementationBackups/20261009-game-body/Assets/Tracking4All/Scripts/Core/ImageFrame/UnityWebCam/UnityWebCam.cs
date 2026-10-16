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

        public Texture Texture => texture;

        public bool IsPrepared => live && texture != null;
        public bool IsPlaying => live && texture != null && texture.isPlaying;
        public bool UpdatedThisFrame => live && texture != null && texture.didUpdateThisFrame;
        public uint UpdateCounter => texture != null ? texture.updateCount : 0;

        public bool IsVerticallyFlipped => IsPrepared && texture.videoVerticallyMirrored;
        public bool IsFrontFacing => IsPrepared && (webCamDevice is WebCamDevice valueOfWebCamDevice) && valueOfWebCamDevice.isFrontFacing;
        public RotationAngle Rotation => !IsPrepared ? RotationAngle.Rotation0 : (RotationAngle)texture.videoRotationAngle;

        public bool Parameterized { get; private set; }

        public int Width => IsPrepared ? texture.width : 0;
        public int Height => IsPrepared ? texture.height : 0;
        public int FrameRate => IsPrepared ? (int)texture.requestedFPS : 0;

        public string Name => IsPrepared ? texture.deviceName + "" : "N?A";

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
