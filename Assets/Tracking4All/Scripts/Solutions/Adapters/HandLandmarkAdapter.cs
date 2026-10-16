// HandLandmarkAdapter
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public class HandLandmarkAdapter : SolutionAdapter<HandLandmarks, Landmark>
    {
        public HandLandmarkAdapter(int groupSize, IProvider<HandLandmarks, Landmark> dataSource, IModifier<Landmark> modifier, float aliveThreshold = -1) : base(groupSize, dataSource, modifier, aliveThreshold)
        {
        }

        protected override void SelfTimeout(int group)
        {

        }
    }
}