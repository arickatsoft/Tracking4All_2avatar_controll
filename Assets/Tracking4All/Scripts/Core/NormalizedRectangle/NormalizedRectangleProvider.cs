// NormalizedRectangleProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    [System.Serializable]
    public class NormalizedRectangleProvider : InterfaceProvider<INormalizedRectangleProvider>, INormalizedRectangleProvider
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

        public IProviderLifecycle Lifecycle => Provider.Lifecycle;

        public int GroupSize => Provider.GroupSize;

        public int DataCount => Provider.DataCount;

        public NormalizedRectangle Get(int group, NormalizedRectangleIdentifier index)
        {
            return Provider.Get(group, index);
        }

        public NormalizedRectangle Get(int group, int index)
        {
            return Provider.Get(group, index);
        }
    }
}