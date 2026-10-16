namespace Tracking4All
{
    public class MPPoseNormalizedLandmarkAdapter : ProxyAdapter<MPPoseLandmarks, NormalizedLandmark, PoseLandmarks, NormalizedLandmark>
    {
        public MPPoseNormalizedLandmarkAdapter(int groupSize, IProvider<MPPoseLandmarks, NormalizedLandmark> dataSource, float aliveThreshold = -1) : base(groupSize, dataSource, aliveThreshold)
        {
        }

        protected override void SelfTimeout(int group)
        {

        }

        protected override void SelfUpdate(int group, IProvider<MPPoseLandmarks, NormalizedLandmark> from)
        {
            PassthroughConversion(group, from);
        }
    }
}