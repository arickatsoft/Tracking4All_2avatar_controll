// ISegmentationBackendProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    /// <summary>
    /// Provides data from a segmentation backend generically.
    /// <para>I.e. custom segmentation providers convert to this provider.</para>
    /// </summary>
    public interface ISegmentationDataProvider : IProvider<SegmentationType, SegmentationData>
    {

        public event GroupEvent OnSegmentationUpdated
        {
            add { Lifecycle.OnUpdated += value; }

            remove { Lifecycle.OnUpdated -= value; }
        }

        public event GroupEvent OnSegmentationReady
        {
            add
            {
                Lifecycle.OnReady += value;
            }

            remove
            {
                Lifecycle.OnReady -= value;
            }
        }

        public event GroupEvent OnSegmentationDisposed
        {
            add
            {
                Lifecycle.OnDisposed += value;
            }

            remove
            {
                Lifecycle.OnDisposed -= value;
            }
        }

        public event GroupEvent OnSegmentationResumed
        {
            add
            {
                Lifecycle.OnResumed += value;
            }

            remove
            {
                Lifecycle.OnResumed -= value;
            }
        }

        public event GroupEvent OnSegmentationTimedOut
        {
            add
            {
                Lifecycle.OnTimedOut += value;
            }

            remove
            {
                Lifecycle.OnTimedOut -= value;
            }
        }

    }
}