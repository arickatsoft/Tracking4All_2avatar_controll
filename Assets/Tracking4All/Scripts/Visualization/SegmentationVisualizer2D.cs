// SegmentationVisualizer2D
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;
using UnityEngine.UI;

namespace Tracking4All
{
    public class SegmentationVisualizer2D : MonoBehaviour
    {
        [SerializeField] protected SegmentationProvider segmentationProvider;
        [SerializeField] private Shader segmentationShader;
        [SerializeField] private RawImage image; // the target image
        [SerializeField] private Color humanColor;

        private SegmentationMaterialAdapter segmentationMaterialAdapter;

        protected void Awake()
        {
            image.enabled = false;

            segmentationMaterialAdapter = new(segmentationProvider.Provider.GroupSize, segmentationProvider.Provider, segmentationShader);

            segmentationMaterialAdapter.OnReady += SegmentationMaterialAdapter_OnReady;
        }
        private void OnValidate()
        {
            UpdateParameters();
        }
        private void UpdateParameters()
        {
            if (segmentationMaterialAdapter != null)
            {
                SegmentationMaterial mat = segmentationMaterialAdapter.Get(0, 0);
                if (mat != null)
                {
                    mat.SetParams(humanColor);
                }
            }
        }

        private void SegmentationMaterialAdapter_OnReady(int group)
        {
            image.enabled = true;
            image.material = segmentationMaterialAdapter.Get(0, 0).Material;
            UpdateParameters();
        }

        protected void OnDestroy()
        {
            segmentationMaterialAdapter.DisposeAll();
            image.enabled = false;
        }

        public class SegmentationMaterial : IDisposableData
        {
            private static int SEGMENTATION_BUFFER = Shader.PropertyToID("_SegmentationBuffer");
            private static int SEGMENTATION_WIDTH = Shader.PropertyToID("_ResolutionX");
            private static int SEGMENTATION_HEIGHT = Shader.PropertyToID("_ResolutionY");

            private Material material;
            public Material Material => material;

            public void SetParams(Color color)
            {
                material.SetColor("_Color", color);
            }

            public void Hook(Shader shader, Segmentation segmentation)
            {
                if (material != null)
                {
                    Logger.LogError("Tried to create material without first disposing!", this);
                    Dispose();
                }

                material = new Material(shader);

                // hook buffer references
                material.SetFloat(SEGMENTATION_WIDTH, segmentation.Width);
                material.SetFloat(SEGMENTATION_HEIGHT, segmentation.Height);
                segmentation.SetMaterialBuffer(material, SEGMENTATION_BUFFER);
            }
            public void Dispose()
            {
                Material.Destroy(material);
                material = null;
            }
        }
        public class SegmentationMaterialAdapter : ProxyAdapter<SegmentationType, Segmentation, SegmentationType, SegmentationMaterial>
        {
            // convert segmentation to materials for downstream usage

            private Shader materialShader;

            public SegmentationMaterialAdapter(int groupSize, IProvider<SegmentationType, Segmentation> dataSource, Shader shader, float aliveThreshold = -1) : base(groupSize, dataSource, aliveThreshold)
            {
                materialShader = shader;
            }

            protected override void SelfReady(int group)
            {
                base.SelfReady(group);

                Segmentation data = DataSource.Get(group, 0);
                SegmentationMaterial m = Get(group, 0);
                m.Hook(materialShader, data);
            }
            protected override void SelfDispose(int group)
            {
                base.SelfDispose(group);

                SegmentationMaterial m = Get(group, 0);
                m.Dispose();
            }

            protected override void SelfUpdate(int group, IProvider<SegmentationType, Segmentation> from)
            {

            }
        }
    }
}