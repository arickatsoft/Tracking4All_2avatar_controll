using Tracking4All;
using UnityEngine;

public class MPPPoseLandmarkAdapter : BackendAdapter<PythonServerSolution.StringData, MPPoseLandmarks, Landmark>
{
    private ISolutionSettings solutionSettings;

    public MPPPoseLandmarkAdapter(ISolutionSettings settings, MonoBehaviour owner, int groupSize, float aliveThreshold = -1)
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
        for (int i = 0; i < DataCount; i++)
        {
            Set(group, i, Get(group, i).Disposed());
        }
    }

    float visibility = 1, presence = 1;
    Vector3 position;

    protected Landmark GetConverted(int i)
    {
        return new Landmark(
                new Vector3((solutionSettings.Mirror ? 1 : -1) * (solutionSettings.PerspectiveFlip ? -1 : 1) * position.x,
                    -position.y,
                    (solutionSettings.PerspectiveFlip ? 1 : -1) * position.z),
                visibility,
                presence
            );
    }
}
