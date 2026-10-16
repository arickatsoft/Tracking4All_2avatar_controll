using Tracking4All;

public class MPPPoseSolution : PythonServerSolution,
    ILandmarkProvider<MPPoseLandmarks>, INormalizedLandmarkProvider<MPPoseLandmarks>
{
    private StringData dataLandmarks = new StringData();
    private StringData dataNormalizedLandmarks = new StringData();

    private MPPPoseLandmarkAdapter landmarks;
    private MPPPoseNormalizedLandmarkAdapter normalizedLandmarks;

    public IProviderLifecycle Lifecycle => ((IProvider<MPPoseLandmarks, Landmark>)landmarks).Lifecycle;

    public int GroupSize => ((IProvider<MPPoseLandmarks, Landmark>)landmarks).GroupSize;

    public int DataCount => ((IProvider<MPPoseLandmarks, Landmark>)landmarks).DataCount;

    IProviderLifecycle IProvider<MPPoseLandmarks, NormalizedLandmark>.Lifecycle => normalizedLandmarks.Lifecycle;

    int IProvider<MPPoseLandmarks, NormalizedLandmark>.GroupSize => normalizedLandmarks.GroupSize;

    int IProvider<MPPoseLandmarks, NormalizedLandmark>.DataCount => normalizedLandmarks.DataCount;

    private void Awake()
    {
        landmarks = new MPPPoseLandmarkAdapter(this, this, 1);
        normalizedLandmarks = new MPPPoseNormalizedLandmarkAdapter(this, this, 1);
        landmarks.ReadyAll();
        normalizedLandmarks.ReadyAll();
    }
    private void OnDisable()
    {
        landmarks.DisposeAll();
        normalizedLandmarks.DisposeAll();
    }

    protected override void ParseData(string[] received)
    {
        base.ParseData(received);

        dataLandmarks.lines = received[0].Split('\n');
        landmarks.Update(0, dataLandmarks);

        if (received.Length > 1)
        {
            dataNormalizedLandmarks.lines = received[1].Split('\n');
            normalizedLandmarks.Update(0, dataNormalizedLandmarks);
        }
    }

    public Landmark Get(int group, MPPoseLandmarks index)
    {
        return ((IProvider<MPPoseLandmarks, Landmark>)landmarks).Get(group, index);
    }

    public Landmark Get(int group, int index)
    {
        return ((IProvider<MPPoseLandmarks, Landmark>)landmarks).Get(group, index);
    }

    NormalizedLandmark IProvider<MPPoseLandmarks, NormalizedLandmark>.Get(int group, MPPoseLandmarks index)
    {
        return normalizedLandmarks.Get(group, index);
    }

    NormalizedLandmark IProvider<MPPoseLandmarks, NormalizedLandmark>.Get(int group, int index)
    {
        return normalizedLandmarks.Get(group, index);
    }

}
