// ILandmarkProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public interface ILandmarkProvider<IDENTIFIER> : IProvider<IDENTIFIER, Landmark>
        where IDENTIFIER : System.Enum
    {
        public event GroupEvent OnLandmarksUpdated
        {
            add { Lifecycle.OnUpdated += value; }

            remove { Lifecycle.OnUpdated -= value; }
        }

        public event GroupEvent OnLandmarksReady
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

        public event GroupEvent OnLandmarksDisposed
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

        public event GroupEvent OnLandmarksResumed
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

        public event GroupEvent OnLandmarksTimedOut
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