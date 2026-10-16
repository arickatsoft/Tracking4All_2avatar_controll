// LifecycleProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    [System.Serializable]
    public class LifecycleProvider : InterfaceProvider<IProviderLifecycle>, IProviderLifecycle
    {
        public int GroupSize => Provider.GroupSize;

        public event GroupEvent OnReady
        {
            add
            {
                Provider.OnReady += value;
            }

            remove
            {
                Provider.OnReady -= value;
            }
        }

        public event GroupEvent OnDisposed
        {
            add
            {
                Provider.OnDisposed += value;
            }

            remove
            {
                Provider.OnDisposed -= value;
            }
        }

        public event GroupEvent OnResumed
        {
            add
            {
                Provider.OnResumed += value;
            }

            remove
            {
                Provider.OnResumed -= value;
            }
        }

        public event GroupEvent OnUpdated
        {
            add
            {
                Provider.OnUpdated += value;
            }

            remove
            {
                Provider.OnUpdated -= value;
            }
        }

        public event GroupEvent OnTimedOut
        {
            add
            {
                Provider.OnTimedOut += value;
            }

            remove
            {
                Provider.OnTimedOut -= value;
            }
        }

        public bool IsAlive(int group)
        {
            return Provider.IsAlive(group);
        }

        public bool IsDisposed(int group)
        {
            return Provider.IsDisposed(group);
        }

        public bool IsReady(int group)
        {
            return Provider.IsReady(group);
        }
    }
}