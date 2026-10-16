// LivingSegmentationDataAdapter
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    public class LivingSegmentationDataAdapter : LivingProxyAdapterContainer,
        ISegmentationDataProvider
    {
        // Basically just an empty shell for redirection.

        [SerializeField] private SegmentationDataProvider segmentationDataProvider;

        public IProviderLifecycle Lifecycle => segmentationDataProvider.Provider.Lifecycle;

        public int GroupSize => segmentationDataProvider.Provider.GroupSize;

        public int DataCount => segmentationDataProvider.Provider.DataCount;

        public SegmentationData Get(int group, SegmentationType index)
        {
            return segmentationDataProvider.Provider.Get(group, index);
        }

        public SegmentationData Get(int group, int index)
        {
            return segmentationDataProvider.Provider.Get(group, index);
        }

        protected override void CreateProxyAdapters()
        {

        }

        protected override void DisposeProxyAdapters()
        {

        }
    }
}