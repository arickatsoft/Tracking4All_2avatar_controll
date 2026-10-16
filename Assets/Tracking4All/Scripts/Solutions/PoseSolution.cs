using UnityEngine;

namespace Tracking4All
{
    public class PoseSolution : SolutionBase,
        ILandmarkProvider<PoseLandmarks>, INormalizedLandmarkProvider<PoseLandmarks>
    {
        [SerializeField] protected LandmarkProvider<PoseLandmarks> landmarkProvider;
        [SerializeField] protected LandmarkModifierStack landmarkModifiers;
        [SerializeField] protected NormalizedLandmarkProvider<PoseLandmarks> normalizedLandmarkProvider;
        [SerializeField] protected NormalizedLandmarkModifierStack normalizedLandmarkModifiers;

        private PoseLandmarkAdapter landmarkAdapter;
        private PoseNormalizedLandmarkAdapter normalizedLandmarkAdapter;
        public FitFight.BodyTracking.RTMPosePoseProvider ActiveRTMPose { get; private set; }
        public void ConfigureBaselineSmoothing(float cutoff, float gain)
        {
            landmarkModifiers?.ConfigureBodySmoothing(cutoff, gain);
            normalizedLandmarkModifiers?.ConfigureBodySmoothing(cutoff, gain);
        }
        public void FlushRTMPose()
        {
            // A readback finishing in LateUpdate can reach the avatar before this frame renders.
            if (isActiveAndEnabled && ActiveRTMPose != null) TickAdapters();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Runtime attachment also covers the existing scenes and prefabs without migration.
            FitFight.BodyTracking.BodyTrackingController.Attach(this);
        }

        public FitFight.BodyTracking.IBodyPoseProvider BaselineProvider =>
            new FitFight.BodyTracking.Tracking4AllPoseProvider(landmarkProvider, normalizedLandmarkProvider);

        public void SetBodyPoseProvider(FitFight.BodyTracking.IBodyPoseProvider provider)
        {
            bool alternative = provider is FitFight.BodyTracking.RTMPosePoseProvider;
            ActiveRTMPose = provider as FitFight.BodyTracking.RTMPosePoseProvider;
            if (landmarkModifiers != null) landmarkModifiers.BypassTemporalSmoothing = alternative;
            if (normalizedLandmarkModifiers != null) normalizedLandmarkModifiers.BypassTemporalSmoothing = alternative;
            landmarkAdapter.ReplaceDataSource(provider.World, isActiveAndEnabled);
            normalizedLandmarkAdapter.ReplaceDataSource(provider.Normalized, isActiveAndEnabled);
        }

        protected override void CreateAdapters()
        {
            landmarkAdapter = new(1, landmarkProvider, landmarkModifiers);
            normalizedLandmarkAdapter = new(1, normalizedLandmarkProvider, normalizedLandmarkModifiers);
        }

        protected override void DisposeAdapters()
        {
            landmarkAdapter?.DisposeAll();
            normalizedLandmarkAdapter?.DisposeAll();
        }

        protected override void ReadyAdapters()
        {
            landmarkAdapter?.ReadyAll();
            normalizedLandmarkAdapter?.ReadyAll();
        }

        protected override void TickAdapters()
        {
            landmarkAdapter?.TickAll();
            normalizedLandmarkAdapter?.TickAll();
        }

        IProviderLifecycle IProvider<PoseLandmarks, Landmark>.Lifecycle => landmarkAdapter?.Lifecycle;

        int IProvider<PoseLandmarks, Landmark>.GroupSize => landmarkAdapter?.GroupSize ?? 0;

        int IProvider<PoseLandmarks, Landmark>.DataCount => landmarkAdapter?.DataCount ?? 0;

        Landmark IProvider<PoseLandmarks, Landmark>.Get(int group, PoseLandmarks index)
        {
            return (Landmark)(landmarkAdapter?.Get(group, index));
        }

        Landmark IProvider<PoseLandmarks, Landmark>.Get(int group, int index)
        {
            return (Landmark)(landmarkAdapter?.Get(group, index));
        }

        IProviderLifecycle IProvider<PoseLandmarks, NormalizedLandmark>.Lifecycle => normalizedLandmarkAdapter?.Lifecycle;

        int IProvider<PoseLandmarks, NormalizedLandmark>.GroupSize => normalizedLandmarkAdapter?.GroupSize ?? 0;

        int IProvider<PoseLandmarks, NormalizedLandmark>.DataCount => normalizedLandmarkAdapter?.DataCount ?? 0;

        NormalizedLandmark IProvider<PoseLandmarks, NormalizedLandmark>.Get(int group, PoseLandmarks index)
        {
            return (NormalizedLandmark)(normalizedLandmarkAdapter?.Get(group, index));
        }

        NormalizedLandmark IProvider<PoseLandmarks, NormalizedLandmark>.Get(int group, int index)
        {
            return (NormalizedLandmark)(normalizedLandmarkAdapter?.Get(group, index));
        }
    }

    public class PoseLandmarkAdapter : SolutionAdapter<PoseLandmarks, Landmark>
    {
        public PoseLandmarkAdapter(int groupSize, IProvider<PoseLandmarks, Landmark> dataSource, IModifier<Landmark> modifier, float aliveThreshold = -1) : base(groupSize, dataSource, modifier, aliveThreshold)
        {
        }

        protected override void SelfTimeout(int group)
        {

        }
    }
    
    public class PoseNormalizedLandmarkAdapter : SolutionAdapter<PoseLandmarks, NormalizedLandmark>
    {
        public PoseNormalizedLandmarkAdapter(int groupSize, IProvider<PoseLandmarks, NormalizedLandmark> dataSource, IModifier<NormalizedLandmark> modifier, float aliveThreshold = -1) : base(groupSize, dataSource, modifier, aliveThreshold)
        {
        }

        protected override void SelfTimeout(int group)
        {

        }
    }
}
