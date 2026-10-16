// LivingMPHolisticHandAdapter
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    public class LivingMPHolisticHandAdapter : LivingProxyAdapterContainer,
        ILandmarkProvider<HandLandmarks>, INormalizedLandmarkProvider<HandLandmarks>
    {
        // Customized hand landmark adapter for holistic.
        // More strict about hand structure and more willing to drop frames that are anomalies...
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

    public class MPHandHolisticLandmarkAdapter : ProxyAdapter<MPHandLandmarks, Landmark, HandLandmarks, Landmark>
    {
        private const float PalmFlipToleranceStartValue = 50f;

        private bool filterEnabled;
        private Vector3 lastUp;
        private float palmFlipTolerance = PalmFlipToleranceStartValue;

        public MPHandHolisticLandmarkAdapter(int groupSize, IProvider<MPHandLandmarks, Landmark> dataSource, bool filterEnabled, float aliveThreshold = -1) : base(groupSize, dataSource, aliveThreshold)
        {
            this.filterEnabled = filterEnabled;
        }

        protected override void SelfTimeout(int group)
        {
            for (int i = 0; i < DataCount; ++i)
            {
                Set(group, i, Get(group, i).Disposed());
            }
        }

        protected override void SelfUpdate(int group, IProvider<MPHandLandmarks, Landmark> from)
        {
            if (IsLandmarkUpdatePassed(group))
                PassthroughConversion(group, from);
        }

        protected bool IsLandmarkUpdatePassed(int group)
        {
            if (!filterEnabled) return true;

            MPHandLandmarks r0 = MPHandLandmarks.RING1;
            MPHandLandmarks i0 = MPHandLandmarks.INDEX1;
            MPHandLandmarks m0 = MPHandLandmarks.MIDDLE1;
            MPHandLandmarks wrist = MPHandLandmarks.WRIST;

            Vector3 handForwardCentre = Helpers.Average(Get(group, m0), Get(group, r0), Get(group, i0));
            Vector3 handForward = (handForwardCentre - Get(group, wrist));
            Vector3 handUp = Helpers.CalculateNormal(Get(group, wrist), Get(group, i0), Get(group, r0));
            Vector3 handRight = Vector3.Cross(handForward, handUp);
            if (group == (int)Handedness.LEFT) handUp *= -1;

            Vector3 dirp = (Get(group, MPHandLandmarks.PINKY2) - Get(group, wrist)).normalized;
            Vector3 dirr = (Get(group, MPHandLandmarks.RING2) - Get(group, wrist)).normalized;

            Vector3 dirpb = (Get(group, MPHandLandmarks.PINKY3) - Get(group, MPHandLandmarks.PINKY1)).normalized;
            Vector3 dirib = (Get(group, MPHandLandmarks.INDEX3) - Get(group, MPHandLandmarks.INDEX1)).normalized;
            Vector3 dirmb = (Get(group, MPHandLandmarks.MIDDLE3) - Get(group, MPHandLandmarks.MIDDLE1)).normalized;

            if (Vector3.Dot(dirp, handUp) > 0.01
                || Vector3.Dot(dirr, handUp) > 0.01) return false; // skip if finger is in stupid place

            float deviation = Mathf.Acos(Vector3.Dot(lastUp, handUp)) * Mathf.Rad2Deg;
            lastUp = handUp;

            if (deviation > palmFlipTolerance) // resist palm flips
            {
                palmFlipTolerance += 10f;
                return false;
            }

            palmFlipTolerance = PalmFlipToleranceStartValue;

            return true;
        }
        private Vector3 Get(int group, MPHandLandmarks l)
        {
            return DataSource.Get(group, l).Position;
        }
    }
}