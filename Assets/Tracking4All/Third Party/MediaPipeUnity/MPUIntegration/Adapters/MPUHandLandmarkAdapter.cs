// MPUHandLandmarkAdapter
// (C) 2024-2025 G8gaming Ltd.
using Mediapipe;
using UnityEngine;

namespace Tracking4All
{
    public class MPUHandLandmarkAdapter : BackendAdapter<Mediapipe.LandmarkList, MPHandLandmarks, Landmark>
    {
        private ISolutionSettings solutionSettings;
        private bool flipX;

        public MPUHandLandmarkAdapter(ISolutionSettings solutionSettings, MonoBehaviour owner, int groupSize, bool flipX = false) : base(groupSize, owner)
        {
            this.solutionSettings = solutionSettings;
            this.flipX = flipX;
        }

        protected override void SelfTimeout(int group)
        {
            for (int i = 0; i < DataCount; ++i)
            {
                Set(group, i, Get(group, i).Disposed());
            }
        }
        protected override void SelfUpdate(int group, LandmarkList from)
        {
            Mediapipe.Landmark landmark;
            Landmark conversion;

            for (int i = 0; i < DataCount; ++i)
            {
                landmark = from.Landmark[i];

                if (this.flipX) landmark.X *= -1;

                conversion = new Landmark(
                        new Vector3(landmark.HasX ? (solutionSettings.Mirror ? -1 : 1) * (solutionSettings.PerspectiveFlip ? -1 : 1) * landmark.X : 0,
                            landmark.HasY ? -landmark.Y : 0,
                            landmark.HasZ ? (solutionSettings.PerspectiveFlip ? 1 : -1) * landmark.Z : 0),
                        landmark.Visibility,
                        landmark.Presence
                    );

                Set(group, i, conversion);
            }
        }
    }
}