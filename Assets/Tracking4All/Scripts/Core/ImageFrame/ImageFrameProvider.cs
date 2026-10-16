// ImageFrameProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    [System.Serializable]
    public class ImageFrameProvider : InterfaceProvider<IImageFrameProvider>, IImageFrameProvider
    {
        public IProviderLifecycle Lifecycle => Provider.Lifecycle;

        public int GroupSize => Provider.GroupSize;

        public int DataCount => Provider.DataCount;

        public ImageFrame Get(int group, ImageFrameIndex index)
        {
            return Provider.Get(group, index);
        }

        public ImageFrame Get(int group, int index)
        {
            return Provider.Get(group, index);
        }
    }
}