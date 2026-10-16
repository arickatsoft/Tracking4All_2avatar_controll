using Tracking4All;
using UnityEngine;

public class MPPPoseNormalizedLandmarkAdapter : BackendAdapter<PythonServerSolution.StringData, MPPoseLandmarks, NormalizedLandmark>
{
    private ISolutionSettings solutionSettings;

    public MPPPoseNormalizedLandmarkAdapter(ISolutionSettings settings, MonoBehaviour owner, int groupSize, float aliveThreshold = -1)
        : base(groupSize, owner, aliveThreshold)
    {
        solutionSettings = settings;
    }

    protected override void SelfUpdate(int group, PythonServerSolution.StringData from)
    {
        foreach (string l in from.lines)
        {
            string[] s = l.Split('|');
            if (s.Length < 4) continue;

            int landmark;
            if (!int.TryParse(s[0], out landmark)) continue;

            position = new Vector3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));

            Set(group, landmark, GetConverted(landmark));
        }
    }

    protected override void SelfTimeout(int group)
    {
        for (int i = 0; i < DataCount; ++i)
        {
            Set(group, i, Get(group, i).Disposed());
        }
    }

    float visibility = 1, presence = 1;
    Vector3 position;

    protected NormalizedLandmark GetConverted(int i)
    {
        return new NormalizedLandmark(
            new Vector3(position.x, 1f - position.y, position.z),
            visibility,
            presence
        );
    }
}
