// MPUSegementationDataAdapter
// (C) 2024-2025 G8gaming Ltd.
using Mediapipe.Unity;
using UnityEngine;

namespace Tracking4All
{
    public class MPUSegementationDataAdapter : BackendAdapter<Mediapipe.ImageFrame, ImageFrameIndex, SegmentationData>
    {
        private ImageSource solutionSettings;

        public MPUSegementationDataAdapter(int groupSize, MonoBehaviour owner, ImageSource settings, float aliveThreshold = -1)
            : base(groupSize, owner, aliveThreshold)
        {
            this.solutionSettings = settings;
        }

        protected override void SelfReady(int group)
        {
            base.SelfReady(group);

            SegmentationData frame = Get(group, 0);
            frame.Create(solutionSettings.textureWidth, solutionSettings.textureHeight);
        }

        protected override void SelfUpdate(int group, Mediapipe.ImageFrame from)
        {
            SegmentationData frame = Get(group, 0);
            if (frame.maskDataReference == null)
            {
                Logger.LogError("Segmentation Data Adapter not correctly readied.", this);
                return;
            }

            from.TryReadChannelNormalized(0, frame.maskDataReference, solutionSettings.isHorizontallyFlipped);
        }

        protected override void SelfDispose(int group)
        {
            base.SelfDispose(group);

            SegmentationData frame = Get(group, 0);
            frame.Dispose();
        }
    }

}