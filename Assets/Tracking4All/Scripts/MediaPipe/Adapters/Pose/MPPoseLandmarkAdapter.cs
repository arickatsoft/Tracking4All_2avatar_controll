namespace Tracking4All
{
    /// <summary>
    /// Convert any MP based pose landmark provider to a standard landmark pose provider.
    /// </summary>
    public class MPPoseLandmarkAdapter : ProxyAdapter<MPPoseLandmarks, Landmark, PoseLandmarks, Landmark>
    {
        public MPPoseLandmarkAdapter(int groupSize, IProvider<MPPoseLandmarks, Landmark> dataSource, float aliveThreshold = -1) : base(groupSize, dataSource, aliveThreshold)
        {
        }

        protected override void SelfTimeout(int group)
        {

        }

        protected override void SelfUpdate(int group, IProvider<MPPoseLandmarks, Landmark> from)
        {
            PassthroughConversion(group, from);
        }
    }
}