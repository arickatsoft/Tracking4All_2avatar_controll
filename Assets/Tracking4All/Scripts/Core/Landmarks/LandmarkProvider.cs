// LandmarkProvider
// (C) 2024-2025 G8gaming Ltd.
using System;

namespace Tracking4All
{
    [System.Serializable]
    public class LandmarkProvider<T> : InterfaceProvider<ILandmarkProvider<T>>, ILandmarkProvider<T>
        where T : System.Enum
    {

        public event GroupEvent OnLandmarksUpdated
        {
            add { if (Lifecycle != null) Lifecycle.OnUpdated += value; }

            remove { if (Lifecycle != null) Lifecycle.OnUpdated -= value; }
        }

        public event GroupEvent OnLandmarksReady
        {
            add
            {
                if (Lifecycle != null) Lifecycle.OnReady += value;
            }

            remove
            {
                if (Lifecycle != null) Lifecycle.OnReady -= value;
            }
        }

        public event GroupEvent OnLandmarksDisposed
        {
            add
            {
                if (Lifecycle != null) Lifecycle.OnDisposed += value;
            }

            remove
            {
                if (Lifecycle != null) Lifecycle.OnDisposed -= value;
            }
        }

        public event GroupEvent OnLandmarksResumed
        {
            add
            {
                if (Lifecycle != null) Lifecycle.OnResumed += value;
            }

            remove
            {
                if (Lifecycle != null) Lifecycle.OnResumed -= value;
            }
        }

        public event GroupEvent OnLandmarksTimedOut
        {
            add
            {
                if (Lifecycle != null) Lifecycle.OnTimedOut += value;
            }

            remove
            {
                if (Lifecycle != null) Lifecycle.OnTimedOut -= value;
            }
        }

        public IProviderLifecycle Lifecycle => Provider?.Lifecycle;

        public int GroupSize => Provider?.GroupSize ?? 0;

        public int DataCount => Provider?.DataCount ?? 0;

        public Landmark Get(int group, T index)
        {
            return (Landmark)(Provider?.Get(group, index));
        }

        public Landmark Get(int group, int index)
        {
            return (Landmark)(Provider?.Get(group, index));
        }

        public static explicit operator LandmarkProvider<T>(LivingMPPoseAdapter v)
        {
            throw new NotImplementedException();
        }
    }
}