// MPUImageSource
// (C) 2024-2025 G8gaming Ltd.
using Mediapipe.Unity;
using System.Collections;
using UnityEngine;

namespace Tracking4All
{

    public class MPUImageSource : ImageSource, IDisposableData
    {
        public override string sourceName => imageFrame.Name;

        public override string[] sourceCandidateNames => new string[0];
        public override ResolutionStruct[] availableResolutions => new ResolutionStruct[0];
        public override bool isPrepared => imageFrame.IsPrepared;
        public override bool isPlaying => imageFrame.IsPlaying;
        public override bool isVerticallyFlipped => imageFrame.IsVerticallyFlipped;
        public override bool isFrontFacing => imageFrame.IsFrontFacing;
        public override RotationAngle rotation => imageFrame.Rotation;

        private ImageFrame imageFrame;
        public void Hook(ImageFrame frame)
        {
            this.imageFrame = frame;
            this.resolution = new ResolutionStruct(imageFrame.Width, imageFrame.Height, imageFrame.FrameRate);
        }
        public void Dispose()
        {
            imageFrame.Dispose();
        }

        public override Texture GetCurrentTexture()
        {
            return imageFrame.Texture;
        }

        // nothing here applies
        public override void Pause()
        {

        }
        public override IEnumerator Play()
        {
            yield break;
        }
        public override IEnumerator Resume()
        {
            yield break;
        }
        public override void SelectSource(int sourceId)
        {

        }
        public override void Stop()
        {

        }
    }
}