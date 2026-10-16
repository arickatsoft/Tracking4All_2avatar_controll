// SegmentationDataProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    [System.Serializable]
    public class SegmentationDataProvider : InterfaceProvider<ISegmentationDataProvider>, ISegmentationDataProvider
    {
        public IProviderLifecycle Lifecycle => Provider.Lifecycle;

        public int GroupSize => Provider.GroupSize;

        public int DataCount => Provider.DataCount;

        public SegmentationData Get(int group, SegmentationType index)
        {
            return Provider.Get(group, index);
        }

        public SegmentationData Get(int group, int index)
        {
            return Provider.Get(group, index);
        }
    }
}