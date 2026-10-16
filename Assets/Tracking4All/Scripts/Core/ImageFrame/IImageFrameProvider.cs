// IImageFrameProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public enum ImageFrameIndex { DEFAULT };
    public interface IImageFrameProvider : IProvider<ImageFrameIndex, ImageFrame>
    {
    }
}