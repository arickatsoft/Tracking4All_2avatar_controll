// ISegmentationProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public interface ISegmentationProvider : IProvider<SegmentationType, Segmentation>
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