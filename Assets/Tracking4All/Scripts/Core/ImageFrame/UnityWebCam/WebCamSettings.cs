// WebCamSettings
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public interface IVideoSourceSettings
    {
        public int RequestedWidth { get; }
        public int RequestedHeight { get; }
        public int RequestedFrameRate { get; }
    }
    public interface IWebcamSettings : IVideoSourceSettings
    {
        public int WebcamDeviceIndex { get; }
    }
}