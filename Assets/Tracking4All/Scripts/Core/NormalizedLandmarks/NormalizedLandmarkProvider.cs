namespace Tracking4All
{
    [System.Serializable]
    public class NormalizedLandmarkProvider<INDEXER> : InterfaceProvider<INormalizedLandmarkProvider<INDEXER>>,
        INormalizedLandmarkProvider<INDEXER>
        where INDEXER : System.Enum
    {
        public IProviderLifecycle Lifecycle => Provider?.Lifecycle;

        public int GroupSize => Provider?.GroupSize ?? 0;

        public int DataCount => Provider?.DataCount ?? 0;

        public NormalizedLandmark Get(int group, INDEXER index)
        {
            return (NormalizedLandmark)(Provider?.Get(group, index));
        }

        public NormalizedLandmark Get(int group, int index)
        {
            return (NormalizedLandmark)(Provider?.Get(group, index));
        }
    }
}
