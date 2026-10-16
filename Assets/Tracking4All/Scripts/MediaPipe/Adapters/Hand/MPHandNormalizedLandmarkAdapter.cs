using UnityEngine.Scripting;

namespace Tracking4All
{
    [Preserve]
    public class MPHandNormalizedLandmarkAdapter : ProxyAdapter<MPHandLandmarks, NormalizedLandmark, HandLandmarks, NormalizedLandmark>
    {
        public MPHandNormalizedLandmarkAdapter(int groupSize, IProvider<MPHandLandmarks, NormalizedLandmark> dataSource, float aliveThreshold = -1)
            : base(groupSize, dataSource, aliveThreshold)
        {
        }

        protected override void SelfTimeout(int group)
        {
            for (int i = 0; i < DataCount; ++i)
            {
                Set(group, i, Get(group, i).Disposed());
            }
        }

        protected override void SelfUpdate(int group, IProvider<MPHandLandmarks, NormalizedLandmark> from)
        {
            PassthroughConversion(group, from);
        }
    }
}