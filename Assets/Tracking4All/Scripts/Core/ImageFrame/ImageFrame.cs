// ImageTexture
// (C) 2024-2025 G8gaming Ltd.
using Mediapipe.Unity;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// The Tracking4All standardized data type for image references.
    /// </summary>
    public class ImageFrame : IDisposableData, IVideoSourceProperties
    {
        protected Texture texture;

        public Texture Texture => texture;
        public IVideoSourceProperties SourceProperties { get; private set; }

        public void Hook(Texture imageReference, IVideoSourceProperties properties)
        {
            texture = imageReference;
            SourceProperties = properties;
        }
        public void Dispose()
        {
            if (texture != null) Texture.Destroy(texture);
            texture = null;
        }

        public bool SourcePropertiesAvailable => SourceProperties != null;

        public int Width => SourceProperties.Width;

        public int Height => SourceProperties.Height;

        public int FrameRate => SourceProperties.FrameRate;

        public bool IsPrepared => SourceProperties.IsPrepared;

        public bool IsPlaying => SourceProperties.IsPlaying;

        public bool IsVerticallyFlipped => SourceProperties.IsVerticallyFlipped;

        public bool IsFrontFacing => SourceProperties.IsFrontFacing;

        public RotationAngle Rotation => SourceProperties.Rotation;

        public string Name => SourceProperties.Name;
    }
}