// FaceDataProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    [System.Serializable]
    public class FaceDataProvider : InterfaceProvider<IFaceDataProvider>, IFaceDataProvider
    {

        public event GroupEvent OnFaceDatasUpdated
        {
            add { Lifecycle.OnUpdated += value; }

            remove { Lifecycle.OnUpdated -= value; }
        }

        public event GroupEvent OnFaceDatasReady
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

        public event GroupEvent OnFaceDatasDisposed
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

        public event GroupEvent OnFaceDatasResumed
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

        public event GroupEvent OnFaceDatasTimedOut
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

        public FaceData Get(int group, FaceDataIdentifier index)
        {
            return Provider.Get(group, index);
        }

        public FaceData Get(int group, int index)
        {
            return Provider.Get(group, index);
        }
    }
}