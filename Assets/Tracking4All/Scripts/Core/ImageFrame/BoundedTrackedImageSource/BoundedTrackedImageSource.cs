// BoundedImageSource
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Focus on a bounding box from an image source.
    /// </summary>
    public class BoundedTrackedImageSource : MonoBehaviour, IImageFrameProvider
    {
        [SerializeField] private NormalizedLandmarkProvider<PoseLandmarks> normalizedLandmarkProvider;
        [SerializeField] private NormalizedRectangleProvider detectionBoundProvider;
        [SerializeField] private ImageFrameProvider sourceProvider;
        [SerializeField] private AdapterController controller;
        [SerializeField] private Shader shader;
        [SerializeField] private float boundMultiplier = .3f;

        private ZoomImageFrameAdapter imageFrameAdapter;
        private Vector4 latestBounds = new Vector4(0, 0, 1, 1);

        private void Awake()
        {
            imageFrameAdapter = new ZoomImageFrameAdapter(1, sourceProvider, shader, controller);
        }
        private void OnEnable()
        {
            detectionBoundProvider.OnNormalizedRectanglesUpdated += DetectionBoundProvider_OnNormalizedRectanglesUpdated;
        }
        private void OnDisable()
        {
            detectionBoundProvider.OnNormalizedRectanglesUpdated -= DetectionBoundProvider_OnNormalizedRectanglesUpdated;
        }
        private void DetectionBoundProvider_OnNormalizedRectanglesUpdated(int group)
        {
            var bounds = detectionBoundProvider.Get(group, NormalizedRectangleIdentifier.Default); // using the normalized landmarks for the centre + some scale factor based on this probably will be best
            latestBounds = new Vector4(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        private void Update()
        {
            float size = (latestBounds.z + latestBounds.w) / 2f;
            size *= boundMultiplier;

            NormalizedLandmark nl = normalizedLandmarkProvider.Get(0, PoseLandmarks.NOSE);
            controller.bounds = new Vector4(Mathf.Clamp01((1f - nl.Position.x) - size / 2f), Mathf.Clamp01(nl.Position.y - size / 2f), size, size);
        }

        [System.Serializable]
        public class AdapterController
        {
            public Vector4 bounds = new Vector4(0, 0, 1, 1);
        }

        public class ZoomImageFrameAdapter : ProxyAdapter<ImageFrameIndex, ImageFrame, ImageFrameIndex, ImageFrame>
        {
            private AdapterController controller;

            private Shader shader;
            private Material material;
            private RenderTexture renderTexture;
            private Texture2D outTexture;

            public ZoomImageFrameAdapter(int groupSize, IProvider<ImageFrameIndex, ImageFrame> dataSource,
                Shader shader, AdapterController controller, float aliveThreshold = -1)
                : base(groupSize, dataSource, aliveThreshold)
            {
                this.shader = shader;
                this.controller = controller;
            }

            protected override void SelfReady(int group)
            {
                base.SelfReady(group);

                ImageFrame from = DataSource.Get(group, 0);
                ImageFrame to = Get(group, 0);

                material = new Material(shader);
                material.SetTexture("_MainTex", from.Texture);

                renderTexture = new RenderTexture(from.Width, from.Height, 0, RenderTextureFormat.ARGB32);
                renderTexture.Create();

                outTexture = new Texture2D(from.Width, from.Height, TextureFormat.RGBA32, false);

                to.Hook(outTexture, from);
            }
            protected override void SelfUpdate(int group, IProvider<ImageFrameIndex, ImageFrame> from)
            {
                ImageFrame fromFrame = DataSource.Get(group, 0);

                material.SetVector("_UVRect", controller.bounds);

                Graphics.Blit(fromFrame.Texture, renderTexture, material);

                RenderTexture.active = renderTexture;
                outTexture.ReadPixels(new UnityEngine.Rect(0, 0, fromFrame.Width, fromFrame.Height), 0, 0);
                outTexture.Apply();
                RenderTexture.active = null;
            }
            protected override void SelfDispose(int group)
            {
                base.SelfDispose(group);

                Destroy(material);
                Destroy(outTexture);
                Destroy(renderTexture);
            }
        }

        // interface implementation
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
    }
}