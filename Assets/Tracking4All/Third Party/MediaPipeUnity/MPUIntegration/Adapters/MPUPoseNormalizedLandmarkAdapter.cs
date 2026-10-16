using Tracking4All;
using UnityEngine;
using NormalizedLandmark = Tracking4All.NormalizedLandmark;

public class MPUPoseNormalizedLandmarkAdapter : BackendAdapter<Mediapipe.NormalizedLandmarkList, MPPoseLandmarks, NormalizedLandmark>
{
    private ISolutionSettings settings;
    private IVideoSourceProperties videoSourceProperties;

    public MPUPoseNormalizedLandmarkAdapter(ISolutionSettings settings, MonoBehaviour owner, int groupSize) : base(groupSize, owner)
    {
        this.settings = settings;
    }

    public void Hook(IVideoSourceProperties properties)
    {
        this.videoSourceProperties = properties;
    }

    protected NormalizedLandmark GetConverted(Mediapipe.NormalizedLandmark landmark)
    {
        return new NormalizedLandmark(
            new Vector3(
                landmark.HasX ? (videoSourceProperties?.IsFrontFacing == true ? 1f - landmark.X : landmark.X) : 0,
                landmark.HasY ? 1f - landmark.Y : 0,
                landmark.HasZ ? landmark.Z : 0),
            landmark.Visibility,
            landmark.Presence
        );
    }

    protected override void SelfUpdate(int group, Mediapipe.NormalizedLandmarkList from)
    {
        for (int i = 0; i < DataCount; ++i)
        {
            Set(group, i, GetConverted(from.Landmark[i]));
        }
    }

    protected override void SelfTimeout(int group)
    {
        for (int i = 0; i < DataCount; ++i)
        {
            Set(group, i, Get(group, i).Disposed());
        }
    }
}