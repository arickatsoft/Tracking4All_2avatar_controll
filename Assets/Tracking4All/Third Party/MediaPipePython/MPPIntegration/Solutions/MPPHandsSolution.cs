using Tracking4All;

public class MPPHandsSolution : PythonServerSolution,
    ILandmarkProvider<MPHandLandmarks>, INormalizedLandmarkProvider<MPHandLandmarks>
{
    private string[] temp;
    private string tempS;
    private StringData data = new StringData();

    private MPPHandLandmarkAdapter landmarks;
    private MPPHandNormalizedLandmarkAdapter normalizedLandmarks;

    public IProviderLifecycle Lifecycle => landmarks.Lifecycle;

    public int GroupSize => ((IProvider<MPHandLandmarks, Landmark>)landmarks).GroupSize;

    public int DataCount => ((IProvider<MPHandLandmarks, Landmark>)landmarks).DataCount;

    IProviderLifecycle IProvider<MPHandLandmarks, NormalizedLandmark>.Lifecycle => normalizedLandmarks.Lifecycle;

    int IProvider<MPHandLandmarks, NormalizedLandmark>.GroupSize => normalizedLandmarks.GroupSize;

    int IProvider<MPHandLandmarks, NormalizedLandmark>.DataCount => normalizedLandmarks.DataCount;

    private void Awake()
    {
        landmarks = new MPPHandLandmarkAdapter(this, this, Helpers.GetLength(typeof(Handedness)));
        normalizedLandmarks = new MPPHandNormalizedLandmarkAdapter(this, this, Helpers.GetLength(typeof(Handedness)));
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

        ParseDataSet(received[0], 0);
        if (received.Length > 1)
            ParseDataSet(received[1], 1);
    }
    private void ParseDataSet(string received, int set)
    {
        tempS = "";
        temp = received.Split('\n');

        Handedness scanning = Handedness.RIGHT;
        foreach (var s in temp)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;

            if (s == "Right")
            {
                TryPushUpdate(scanning, set);
                scanning = Handedness.RIGHT;
                tempS = "";
            }
            else if (s == "Left")
            {
                TryPushUpdate(scanning, set);
                scanning = Handedness.LEFT;
                tempS = "";
            }
            else
            {
                tempS += s + "\n";
            }
        }
        TryPushUpdate(scanning, set);
    }
    private void TryPushUpdate(Handedness scanning, int set)
    {
        if (tempS != "")
        {
            data.lines = tempS.Split('\n');
            if (set == 0)
                landmarks.Update((int)scanning, data);
            else if (set == 1)
                normalizedLandmarks.Update((int)scanning, data);
        }
    }

    public Landmark Get(int group, MPHandLandmarks index)
    {
        return ((IProvider<MPHandLandmarks, Landmark>)landmarks).Get(group, index);
    }

    public Landmark Get(int group, int index)
    {
        return ((IProvider<MPHandLandmarks, Landmark>)landmarks).Get(group, index);
    }

    NormalizedLandmark IProvider<MPHandLandmarks, NormalizedLandmark>.Get(int group, MPHandLandmarks index)
    {
        return normalizedLandmarks.Get(group, index);
    }

    NormalizedLandmark IProvider<MPHandLandmarks, NormalizedLandmark>.Get(int group, int index)
    {
        return normalizedLandmarks.Get(group, index);
    }

}
