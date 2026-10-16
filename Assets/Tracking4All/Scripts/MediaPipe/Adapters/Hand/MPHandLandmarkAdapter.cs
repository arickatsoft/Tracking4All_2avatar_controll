// MPHandLandmarkAdapter
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public class MPHandLandmarkAdapter : ProxyAdapter<MPHandLandmarks, Landmark, HandLandmarks, Landmark>
    {
        public MPHandLandmarkAdapter(int groupSize, IProvider<MPHandLandmarks, Landmark> dataSource, float aliveThreshold = -1) : base(groupSize, dataSource, aliveThreshold)
        {
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
            PassthroughConversion(group, from);
        }
    }

}