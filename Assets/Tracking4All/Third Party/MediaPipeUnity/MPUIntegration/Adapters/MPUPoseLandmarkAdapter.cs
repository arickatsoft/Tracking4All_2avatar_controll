using Tracking4All;
using UnityEngine;
using Landmark = Tracking4All.Landmark;

public class MPUPoseLandmarkAdapter : BackendAdapter<Mediapipe.LandmarkList, MPPoseLandmarks, Landmark>
{
    private ISolutionSettings settings;

    public MPUPoseLandmarkAdapter(ISolutionSettings settings, MonoBehaviour owner, int groupSize) : base(groupSize, owner)
    {
        this.settings = settings;
    }

    protected Landmark GetConverted(Mediapipe.Landmark landmark)
    {
        // original: -x
        return new Landmark(
                new Vector3(landmark.HasX ? (settings.Mirror ? 1 : -1) * (settings.PerspectiveFlip ? -1 : 1) * landmark.X : 0,
                    landmark.HasY ? -landmark.Y : 0,
                    landmark.HasZ ? (settings.PerspectiveFlip ? 1 : -1) * landmark.Z : 0),
                landmark.Visibility,
                landmark.Presence
            );
    }

    protected override void SelfUpdate(int group, Mediapipe.LandmarkList from)
    {
        for (int i = 0; i < DataCount; ++i)
        {
            Set(group, i, GetConverted(from.Landmark[i]));
        }
    }

    protected override void SelfTimeout(int group)
    {

    }
}
