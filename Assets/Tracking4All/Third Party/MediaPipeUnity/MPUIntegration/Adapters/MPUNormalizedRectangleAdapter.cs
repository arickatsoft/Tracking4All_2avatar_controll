// MPUNormalizedRectangleAdapter
// (C) 2024-2025 G8gaming Ltd.
using Mediapipe;
using UnityEngine;

namespace Tracking4All
{
    public class MPUNormalizedRectangleAdapter : BackendAdapter<NormalizedRect, NormalizedRectangleIdentifier, NormalizedRectangle>
    {
        public MPUNormalizedRectangleAdapter(int groupSize, MonoBehaviour owner, float aliveThreshold = -1) : base(groupSize, owner, aliveThreshold)
        {
        }

        protected override void SelfUpdate(int group, NormalizedRect from)
        {
            Set(group, NormalizedRectangleIdentifier.Default,
                new NormalizedRectangle(from.XCenter, from.XCenter, from.Width, from.Height)
                );
        }
    }
}