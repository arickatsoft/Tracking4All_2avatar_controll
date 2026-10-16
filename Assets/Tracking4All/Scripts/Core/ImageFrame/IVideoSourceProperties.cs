// IVideoSourceProperties
// (C) 2024-2025 G8gaming Ltd.
using Mediapipe.Unity;

namespace Tracking4All
{
    public interface IVideoSourceProperties
    {
        public string Name { get; }

        public int Width { get; }
        public int Height { get; }
        public int FrameRate { get; }

        public bool IsPrepared { get; }
        public bool IsPlaying { get; }
        public bool IsVerticallyFlipped { get; }
        public bool IsFrontFacing { get; }
        public RotationAngle Rotation { get; }
    }
}
