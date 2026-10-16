// HandNormalizedLandmarkAdapter
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public class HandNormalizedLandmarkAdapter : SolutionAdapter<HandLandmarks, NormalizedLandmark>
    {
        public HandNormalizedLandmarkAdapter(int groupSize, IProvider<HandLandmarks, NormalizedLandmark> dataSource, IModifier<NormalizedLandmark> modifier, float aliveThreshold = -1)
            : base(groupSize, dataSource, modifier, aliveThreshold)
        {
        }

        protected override void SelfTimeout(int group)
        {

        }
    }
}