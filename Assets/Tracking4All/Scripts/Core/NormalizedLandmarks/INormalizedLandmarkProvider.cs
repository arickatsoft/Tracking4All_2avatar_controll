namespace Tracking4All
{
    public interface INormalizedLandmarkProvider<INDEXER> : IProvider<INDEXER, NormalizedLandmark>
        where INDEXER : System.Enum
    {
        public event GroupEvent OnNormalizedLandmarksUpdated
        {
            add { Lifecycle.OnUpdated += value; }

            remove { Lifecycle.OnUpdated -= value; }
        }

        public event GroupEvent OnNormalizedLandmarksReady
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

        public event GroupEvent OnNormalizedLandmarksDisposed
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

        public event GroupEvent OnNormalizedLandmarksResumed
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

        public event GroupEvent OnNormalizedLandmarksTimedOut
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