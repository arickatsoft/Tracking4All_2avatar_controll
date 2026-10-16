using Tracking4All;
using UnityEngine;

public class MPPHandLandmarkAdapter : BackendAdapter<PythonServerSolution.StringData, MPHandLandmarks, Landmark>
{
    private ISolutionSettings solutionSettings;
    private bool flipX;

    public MPPHandLandmarkAdapter(ISolutionSettings settings, MonoBehaviour owner, int groupSize, float aliveThreshold = -1, bool flipX = false) : base(groupSize, owner, aliveThreshold)
    {
        solutionSettings = settings;
        this.flipX = flipX;
    }

    protected override void SelfUpdate(int group, PythonServerSolution.StringData from)
    {
        foreach (string l in from.lines)
        {
            string[] s = l.Split('|');
            if (s.Length < 4) continue;

            int landmarkIndex;
            if (!int.TryParse(s[0], out landmarkIndex)) continue;

            position = new Vector3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));

            Set(group, landmarkIndex, GetConverted(landmarkIndex));
        }
    }

    protected override void SelfTimeout(int group)
    {

    }

    const float visibility = 1, presence = 1;
    Vector3 position;
    protected Landmark GetConverted(int i)
    {
        if (this.flipX) position.x *= -1;

        return new Landmark(
                new Vector3((solutionSettings.Mirror ? -1 : 1) * (solutionSettings.PerspectiveFlip ? -1 : 1) * position.x,
                    -position.y,
                    (solutionSettings.PerspectiveFlip ? 1 : -1) * position.z),
                visibility,
                presence
            );
    }
}
