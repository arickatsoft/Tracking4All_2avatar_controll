// LivingMPHandAdapter
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    public class LivingMPHandAdapter : LivingProxyAdapterContainer,
        ILandmarkProvider<HandLandmarks>, INormalizedLandmarkProvider<HandLandmarks>
    {
        [SerializeField] private LandmarkProvider<MPHandLandmarks> landmarkProvider;
        [SerializeField] private NormalizedLandmarkProvider<MPHandLandmarks> normalizedLandmarkProvider;

        private MPHandLandmarkAdapter landmarkAdapter;
        private MPHandNormalizedLandmarkAdapter normalizedLandmarkAdapter;

        protected override void CreateProxyAdapters()
        {
            if (landmarkProvider.HasInterface) landmarkAdapter = new(Helpers.GetLength(typeof(Handedness)), landmarkProvider.Provider);
            if (normalizedLandmarkProvider.HasInterface) normalizedLandmarkAdapter = new(Helpers.GetLength(typeof(Handedness)), normalizedLandmarkProvider.Provider);
        }
        protected override void DisposeProxyAdapters()
        {
            if (landmarkAdapter != null) landmarkAdapter.DisposeAll();
            if (normalizedLandmarkAdapter != null) normalizedLandmarkAdapter.DisposeAll();
        }

        IProviderLifecycle IProvider<HandLandmarks, Landmark>.Lifecycle => landmarkAdapter?.Lifecycle;
        int IProvider<HandLandmarks, Landmark>.GroupSize => landmarkAdapter.GroupSize;
        int IProvider<HandLandmarks, Landmark>.DataCount => landmarkAdapter.DataCount;

        IProviderLifecycle IProvider<HandLandmarks, NormalizedLandmark>.Lifecycle => normalizedLandmarkAdapter?.Lifecycle;

        int IProvider<HandLandmarks, NormalizedLandmark>.GroupSize => normalizedLandmarkAdapter.GroupSize;

        int IProvider<HandLandmarks, NormalizedLandmark>.DataCount => normalizedLandmarkAdapter.DataCount;

        Landmark IProvider<HandLandmarks, Landmark>.Get(int group, HandLandmarks index)
        {
            return landmarkAdapter.Get(group, index);
        }
        Landmark IProvider<HandLandmarks, Landmark>.Get(int group, int index)
        {
            return landmarkAdapter.Get(group, index);
        }

        NormalizedLandmark IProvider<HandLandmarks, NormalizedLandmark>.Get(int group, HandLandmarks index)
        {
            return normalizedLandmarkAdapter.Get(group, index);
        }

        NormalizedLandmark IProvider<HandLandmarks, NormalizedLandmark>.Get(int group, int index)
        {
            return normalizedLandmarkAdapter.Get(group, index);
        }
    }
}