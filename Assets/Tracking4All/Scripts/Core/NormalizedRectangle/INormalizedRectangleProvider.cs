// INormalizedRectangleProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public enum NormalizedRectangleIdentifier { Default };
    public interface INormalizedRectangleProvider : IProvider<NormalizedRectangleIdentifier, NormalizedRectangle>
    {
        public event GroupEvent OnNormalizedRectanglesUpdated
        {
            add { Lifecycle.OnUpdated += value; }

            remove { Lifecycle.OnUpdated -= value; }
        }

        public event GroupEvent OnNormalizedRectanglesReady
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

        public event GroupEvent OnNormalizedRectanglesDisposed
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

        public event GroupEvent OnNormalizedRectanglesResumed
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

        public event GroupEvent OnNormalizedRectanglesTimedOut
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