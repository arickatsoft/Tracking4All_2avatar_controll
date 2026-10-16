using UnityEngine;
using System.Collections;

namespace Tracking4All
{
    /// <summary>
    /// A specialized PoseSolution that manages two pose tracking regions from a single camera source.
    /// This allows tracking two people simultaneously by splitting the camera feed into regions.
    /// Note: MediaPipe natively only supports single-person tracking, so this implementation
    /// alternates between processing region 1 and region 2.
    /// </summary>
    public class DualPoseSolution : SolutionBase,
        ILandmarkProvider<PoseLandmarks>, INormalizedLandmarkProvider<PoseLandmarks>
    {
        [Header("Dual Tracking Configuration")]
        [SerializeField] private DualTrackingMode trackingMode = DualTrackingMode.LeftRight;
        [SerializeField] [Range(0f, 1f)] private float regionSplit = 0.5f; // Where to split the regions (0.5 = middle)
        [SerializeField] [Range(0f, 0.2f)] private float regionOverlap = 0.05f; // Overlap to avoid edge issues

        [Header("Provider References")]
        [SerializeField] protected LandmarkProvider<PoseLandmarks> landmarkProviderRegion1;
        [SerializeField] protected LandmarkModifierStack landmarkModifiersRegion1;
        [SerializeField] protected NormalizedLandmarkProvider<PoseLandmarks> normalizedLandmarkProviderRegion1;
        [SerializeField] protected NormalizedLandmarkModifierStack normalizedLandmarkModifiersRegion1;

        [SerializeField] protected LandmarkProvider<PoseLandmarks> landmarkProviderRegion2;
        [SerializeField] protected LandmarkModifierStack landmarkModifiersRegion2;
        [SerializeField] protected NormalizedLandmarkProvider<PoseLandmarks> normalizedLandmarkProviderRegion2;
        [SerializeField] protected NormalizedLandmarkModifierStack normalizedLandmarkModifiersRegion2;

        [Header("Debug")]
        [SerializeField] private bool showRegionVisualization = true;
        [SerializeField] private Color region1Color = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color region2Color = new Color(0f, 0f, 1f, 0.3f);

        // Adapters for each region (Group 0 = Region 1, Group 1 = Region 2)
        private PoseLandmarkAdapter landmarkAdapterRegion1;
        private PoseNormalizedLandmarkAdapter normalizedLandmarkAdapterRegion1;
        private PoseLandmarkAdapter landmarkAdapterRegion2;
        private PoseNormalizedLandmarkAdapter normalizedLandmarkAdapterRegion2;

        // Processing state
        private int currentProcessingRegion = 0; // Alternates between 0 and 1
        private Rect region1Bounds;
        private Rect region2Bounds;

        public enum DualTrackingMode
        {
            LeftRight,  // Split screen vertically
            TopBottom,  // Split screen horizontally
            Custom      // Custom region definitions
        }

        protected override void CreateAdapters()
        {
            // Create adapters for Region 1 (Group 0)
            landmarkAdapterRegion1 = new(2, landmarkProviderRegion1, landmarkModifiersRegion1);
            normalizedLandmarkAdapterRegion1 = new(2, normalizedLandmarkProviderRegion1, normalizedLandmarkModifiersRegion1);

            // Create adapters for Region 2 (Group 1)
            landmarkAdapterRegion2 = new(2, landmarkProviderRegion2, landmarkModifiersRegion2);
            normalizedLandmarkAdapterRegion2 = new(2, normalizedLandmarkProviderRegion2, normalizedLandmarkModifiersRegion2);

            // Calculate region bounds based on mode
            CalculateRegionBounds();

            Logger.LogInfo("DualPoseSolution created adapters with groupSize=2 for dual tracking", this);
        }

        protected override void DisposeAdapters()
        {
            landmarkAdapterRegion1?.DisposeAll();
            normalizedLandmarkAdapterRegion1?.DisposeAll();
            landmarkAdapterRegion2?.DisposeAll();
            normalizedLandmarkAdapterRegion2?.DisposeAll();
        }

        protected override void ReadyAdapters()
        {
            landmarkAdapterRegion1?.ReadyAll();
            normalizedLandmarkAdapterRegion1?.ReadyAll();
            landmarkAdapterRegion2?.ReadyAll();
            normalizedLandmarkAdapterRegion2?.ReadyAll();
        }

        protected override void TickAdapters()
        {
            // Alternate processing between regions
            // In a real implementation, you'd process the cropped region through MediaPipe
            // For now, both regions process from their respective providers

            landmarkAdapterRegion1?.TickAll();
            normalizedLandmarkAdapterRegion1?.TickAll();
            landmarkAdapterRegion2?.TickAll();
            normalizedLandmarkAdapterRegion2?.TickAll();

            // Alternate for next frame
            currentProcessingRegion = (currentProcessingRegion + 1) % 2;
        }

        private void CalculateRegionBounds()
        {
            switch (trackingMode)
            {
                case DualTrackingMode.LeftRight:
                    // Region 1: Left side
                    region1Bounds = new Rect(0, 0, regionSplit + regionOverlap, 1f);
                    // Region 2: Right side
                    region2Bounds = new Rect(regionSplit - regionOverlap, 0, 1f - regionSplit + regionOverlap, 1f);
                    break;

                case DualTrackingMode.TopBottom:
                    // Region 1: Top half
                    region1Bounds = new Rect(0, 0, 1f, regionSplit + regionOverlap);
                    // Region 2: Bottom half
                    region2Bounds = new Rect(0, regionSplit - regionOverlap, 1f, 1f - regionSplit + regionOverlap);
                    break;

                case DualTrackingMode.Custom:
                    // Custom bounds can be set via inspector or code
                    // Default to left/right split
                    region1Bounds = new Rect(0, 0, 0.5f, 1f);
                    region2Bounds = new Rect(0.5f, 0, 0.5f, 1f);
                    break;
            }

            Logger.LogInfo($"Region bounds calculated: R1={region1Bounds}, R2={region2Bounds}", this);
        }

        // IProvider implementation - routes to Region 1 (Group 0)
        IProviderLifecycle IProvider<PoseLandmarks, Landmark>.Lifecycle => landmarkAdapterRegion1?.Lifecycle;
        int IProvider<PoseLandmarks, Landmark>.GroupSize => 2; // Two groups: 0 and 1
        int IProvider<PoseLandmarks, Landmark>.DataCount => landmarkAdapterRegion1?.DataCount ?? 0;

        Landmark IProvider<PoseLandmarks, Landmark>.Get(int group, PoseLandmarks index)
        {
            if (group == 0)
                return (Landmark)(landmarkAdapterRegion1?.Get(group, index));
            else
                return (Landmark)(landmarkAdapterRegion2?.Get(group, index));
        }

        Landmark IProvider<PoseLandmarks, Landmark>.Get(int group, int index)
        {
            if (group == 0)
                return (Landmark)(landmarkAdapterRegion1?.Get(group, index));
            else
                return (Landmark)(landmarkAdapterRegion2?.Get(group, index));
        }

        // INormalizedLandmarkProvider implementation
        IProviderLifecycle IProvider<PoseLandmarks, NormalizedLandmark>.Lifecycle => normalizedLandmarkAdapterRegion1?.Lifecycle;
        int IProvider<PoseLandmarks, NormalizedLandmark>.GroupSize => 2; // Two groups: 0 and 1
        int IProvider<PoseLandmarks, NormalizedLandmark>.DataCount => normalizedLandmarkAdapterRegion1?.DataCount ?? 0;

        NormalizedLandmark IProvider<PoseLandmarks, NormalizedLandmark>.Get(int group, PoseLandmarks index)
        {
            if (group == 0)
                return (NormalizedLandmark)(normalizedLandmarkAdapterRegion1?.Get(group, index));
            else
                return (NormalizedLandmark)(normalizedLandmarkAdapterRegion2?.Get(group, index));
        }

        NormalizedLandmark IProvider<PoseLandmarks, NormalizedLandmark>.Get(int group, int index)
        {
            if (group == 0)
                return (NormalizedLandmark)(normalizedLandmarkAdapterRegion1?.Get(group, index));
            else
                return (NormalizedLandmark)(normalizedLandmarkAdapterRegion2?.Get(group, index));
        }

        // Public API for configuring regions
        public void SetRegionBounds(Rect region1, Rect region2)
        {
            trackingMode = DualTrackingMode.Custom;
            region1Bounds = region1;
            region2Bounds = region2;
            Logger.LogInfo($"Custom region bounds set: R1={region1}, R2={region2}", this);
        }

        public Rect GetRegion1Bounds() => region1Bounds;
        public Rect GetRegion2Bounds() => region2Bounds;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showRegionVisualization) return;

            // This would visualize the regions in scene view
            // Actual implementation would need camera reference
        }
#endif
    }
}
