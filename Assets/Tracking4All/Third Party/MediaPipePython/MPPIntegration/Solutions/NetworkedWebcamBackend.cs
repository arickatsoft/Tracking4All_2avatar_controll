// NetworkedWebcamBackend
// (C) 2024-2025 G8gaming Ltd.
using System;
using System.Collections;
using System.Net;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Uses TCP sockets with the python networkedwebcam example to send image data over the network into Tracking4All.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    public class NetworkedWebcamBackend : MonoBehaviour, IImageFrameProvider
    {
        [SerializeField] private NetWebCam cam;

        private TableGroup<NetWebCam> cameras;
        private ImageFrameAdapter imageFrameAdapter;

        private void Awake()
        {
            cameras = new TableGroup<NetWebCam>(1, 1);
            cameras.Set(0, 0, cam);
            imageFrameAdapter = new ImageFrameAdapter(1, this, cameras, 1f);
        }

        private void OnEnable()
        {
            StartCoroutine(ReadyRoutine());
        }
        private IEnumerator ReadyRoutine()
        {
            // Start WebCam.
            NetWebCam cam = cameras.Get(0, 0);
            cam.Ready();

            // Wait for the WebCam.
            yield return new WaitUntil(() => cam.IsPrepared);

            // Report ready when the WebCam is ready.
            imageFrameAdapter.Ready(0);
        }

        private void OnDisable()
        {
            imageFrameAdapter.DisposeAll();
        }
        private void Update()
        {
            if (cam.Update())
            {
                if (imageFrameAdapter.IsReady(0))
                {
                    imageFrameAdapter.Update(0, cameras);
                }
            }
        }

        // Adapter implementation
        public IProviderLifecycle Lifecycle => imageFrameAdapter?.Lifecycle;
        public int GroupSize => imageFrameAdapter.GroupSize;
        public int DataCount => imageFrameAdapter.DataCount;
        public ImageFrame Get(int group, ImageFrameIndex index)
        {
            return imageFrameAdapter.Get(group, index);
        }
        public ImageFrame Get(int group, int index)
        {
            return imageFrameAdapter.Get(group, index);
        }

        public class ImageFrameAdapter : BackendAdapter<TableGroup<NetWebCam>, ImageFrameIndex, ImageFrame>
        {
            private TableGroup<NetWebCam> data;

            public ImageFrameAdapter(int groupSize, MonoBehaviour owner, TableGroup<NetWebCam> data, float aliveThreshold)
                : base(groupSize, owner, aliveThreshold)
            {
                this.data = data;
            }


            protected override void SelfReady(int group)
            {
                base.SelfReady(group);

                NetWebCam source = data.Get(group, 0);
                ImageFrame frame = Get(group, 0);
                frame.Hook(source.texture, source);
            }
            protected override void SelfDispose(int group)
            {
                base.SelfDispose(group);

                ImageFrame frame = Get(group, 0);
                frame.Dispose();

                NetWebCam source = data.Get(group, 0);
                source.Dispose();
            }

            protected override void SelfUpdate(int group, TableGroup<NetWebCam> from)
            {

            }

        }

        [System.Serializable]
        public class NetWebCam : IVideoSourceProperties
        {
            public string host = "127.0.0.1";
            public int port = 52730;

            private ServerTCP serverTCP;

            private NetworkedWebCamSettings cameraSettings; // recovered over the network
            public Texture2D texture; // updated over the network

            private bool reconstructTextureFlag;
            private bool imageTextureDirty = false;
            private byte[] imageTextureBytes = new byte[0];

            public string Name => "NetCam(" + host + ":" + port + ")";

            public int Width => cameraSettings.RequestedWidth;

            public int Height => cameraSettings.RequestedHeight;

            public int FrameRate => cameraSettings.RequestedFrameRate;

            public bool IsPrepared => texture != null;

            public bool IsPlaying => IsPrepared;

            public bool IsVerticallyFlipped => false;

            public bool IsFrontFacing => true;

            public Mediapipe.Unity.RotationAngle Rotation => Mediapipe.Unity.RotationAngle.Rotation0;

            public void Ready()
            {
                serverTCP = new ServerTCP(host, port);
                serverTCP.Connect();
                serverTCP.OnPayloadConstructed += ServerTCP_OnPayloadConstructed;
            }
            public void Dispose()
            {
                serverTCP.OnPayloadConstructed -= ServerTCP_OnPayloadConstructed;
                serverTCP.Disconnect();
            }

            /// <summary>
            /// Returns true if texture was updated.
            /// </summary>
            /// <returns></returns>
            public bool Update()
            {
                if (reconstructTextureFlag)
                {
                    if (texture != null) Destroy(texture);
                    // Changed from RGB24 to RGBA32 to match TextureFramePool format expectations
                    // This prevents array size mismatch errors when copying texture data
                    texture = new Texture2D(cameraSettings.RequestedWidth, cameraSettings.RequestedHeight, TextureFormat.RGBA32, false);
                    // target.material.mainTexture = texture;

                    reconstructTextureFlag = false;
                }

                if (imageTextureDirty)
                {
                    texture.LoadImage(imageTextureBytes);

                    imageTextureDirty = false;
                    return true;
                }

                return false;
            }

            private void ServerTCP_OnPayloadConstructed(uint msgType, byte[] payload)
            {
                if (msgType == 117)
                {
                    int index = BitConverter.ToInt32(payload, 0);
                    index = IPAddress.NetworkToHostOrder(index);

                    int width = BitConverter.ToInt32(payload, 4);
                    width = IPAddress.NetworkToHostOrder(width);

                    int height = BitConverter.ToInt32(payload, 8);
                    height = IPAddress.NetworkToHostOrder(height);

                    byte[] floatBytes = new byte[4];
                    Array.Copy(payload, 12, floatBytes, 0, 4);
                    Array.Reverse(floatBytes);
                    float fps = BitConverter.ToSingle(floatBytes, 0);

                    cameraSettings = new NetworkedWebCamSettings(index, width, height, Mathf.RoundToInt(fps));
                    Logger.LogInfo("Recieved networked WebCam configuration (" + width + "x" + height + ").");

                    reconstructTextureFlag = true;
                }

                else if (msgType == 118 && imageTextureDirty == false)
                {
                    if (imageTextureBytes.Length != payload.Length)
                    {
                        imageTextureBytes = new byte[payload.Length];
                    }

                    Array.Copy(payload, imageTextureBytes, payload.Length);

                    imageTextureDirty = true;
                }

            }
        }

        public class NetworkedWebCamSettings : IWebcamSettings
        {
            public NetworkedWebCamSettings(int i, int w, int h, int fps)
            {
                WebcamDeviceIndex = i;
                RequestedWidth = w;
                RequestedHeight = h;
                RequestedFrameRate = fps;
            }

            public int WebcamDeviceIndex { get; private set; }

            public int RequestedWidth { get; private set; }

            public int RequestedHeight { get; private set; }

            public int RequestedFrameRate { get; private set; }
        }
    }


}