using Tracking4All;
using UnityEngine;

public class MPPHandNormalizedLandmarkAdapter : BackendAdapter<PythonServerSolution.StringData, MPHandLandmarks, NormalizedLandmark>
{
    private ISolutionSettings solutionSettings;
    private bool flipX;

    public MPPHandNormalizedLandmarkAdapter(ISolutionSettings settings, MonoBehaviour owner, int groupSize, float aliveThreshold = -1, bool flipX = false) : base(groupSize, owner, aliveThreshold)
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
    protected NormalizedLandmark GetConverted(int i)
    {
        return new NormalizedLandmark(
            new Vector3(position.x, 1f - position.y, position.z),
            visibility,
            presence
        );
    }
}
