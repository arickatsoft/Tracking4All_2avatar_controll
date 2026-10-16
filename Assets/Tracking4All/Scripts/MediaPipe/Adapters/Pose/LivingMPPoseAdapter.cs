using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Wrapper around MPPoseLandmarkAdapter to make it a living unity component.
    /// </summary>
    public class LivingMPPoseAdapter : LivingProxyAdapterContainer,
        ILandmarkProvider<PoseLandmarks>, INormalizedLandmarkProvider<PoseLandmarks>,
        ISolutionSettings
    {
        [SerializeField] private LandmarkProvider<MPPoseLandmarks> landmarkProvider;
        [SerializeField] private NormalizedLandmarkProvider<MPPoseLandmarks> normalizedLandmarkProvider;

        private MPPoseLandmarkAdapter landmarkAdapter;
        private MPPoseNormalizedLandmarkAdapter normalizedLandmarkAdapter;

        protected override void CreateProxyAdapters()
        {
            if (landmarkProvider.HasInterface) landmarkAdapter = new(1, landmarkProvider);
            if (normalizedLandmarkProvider.HasInterface) normalizedLandmarkAdapter = new(1, normalizedLandmarkProvider);
        }

        protected override void DisposeProxyAdapters()
        {
            if (landmarkAdapter != null) landmarkAdapter.DisposeAll();
            if (normalizedLandmarkAdapter != null) normalizedLandmarkAdapter.DisposeAll();
        }

        IProviderLifecycle IProvider<PoseLandmarks, Landmark>.Lifecycle => landmarkAdapter?.Lifecycle;

        int IProvider<PoseLandmarks, Landmark>.GroupSize => landmarkAdapter.GroupSize;

        int IProvider<PoseLandmarks, Landmark>.DataCount => landmarkAdapter.DataCount;

        Landmark IProvider<PoseLandmarks, Landmark>.Get(int group, PoseLandmarks index)
        {
            return landmarkAdapter.Get(group, index);
        }

        Landmark IProvider<PoseLandmarks, Landmark>.Get(int group, int index)
        {
            return landmarkAdapter.Get(group, index);
        }

                IProviderLifecycle IProvider<PoseLandmarks, NormalizedLandmark>.Lifecycle => normalizedLandmarkAdapter?.Lifecycle;

                int IProvider<PoseLandmarks, NormalizedLandmark>.GroupSize => normalizedLandmarkAdapter?.GroupSize ?? 0;

                int IProvider<PoseLandmarks, NormalizedLandmark>.DataCount => normalizedLandmarkAdapter?.DataCount ?? 0;

        NormalizedLandmark IProvider<PoseLandmarks, NormalizedLandmark>.Get(int group, PoseLandmarks index)
        {
            return normalizedLandmarkAdapter.Get(group, index);
        }

        NormalizedLandmark IProvider<PoseLandmarks, NormalizedLandmark>.Get(int group, int index)
        {
            return normalizedLandmarkAdapter.Get(group, index);
        }
    }
}