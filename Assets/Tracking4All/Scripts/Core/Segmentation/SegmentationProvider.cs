// SegmentationProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    [System.Serializable]
    public class SegmentationProvider : InterfaceProvider<ISegmentationProvider>, ISegmentationProvider
    {
        public IProviderLifecycle Lifecycle => Provider.Lifecycle;

        public int GroupSize => Provider.GroupSize;

        public int DataCount => Provider.DataCount;

        public Segmentation Get(int group, SegmentationType index)
        {
            return Provider.Get(group, index);
        }

        public Segmentation Get(int group, int index)
        {
            return Provider.Get(group, index);
        }
    }
}