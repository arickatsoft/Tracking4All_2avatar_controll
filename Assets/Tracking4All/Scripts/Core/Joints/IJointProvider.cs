// IJointProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public interface IJointProvider<INDEXER, JOINT_TYPE> : IProvider<INDEXER, JOINT_TYPE>
        where INDEXER : System.Enum
        where JOINT_TYPE : PuppetJoint, new()
    {
        public event GroupEvent OnJointsReady
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

        public event GroupEvent OnJointsDisposed
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

        public event GroupEvent OnJointsResumed
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

        public event GroupEvent OnUpdated
        {
            add
            {
                Lifecycle.OnUpdated += value;
            }

            remove
            {
                Lifecycle.OnUpdated -= value;
            }
        }

        public event GroupEvent OnJointsTimedOut
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

        /// <summary>
        /// Get the joint relative to the avatar/puppet only.
        /// <para>I.e. ignores mirroring and such (you probably don't want this).</para>
        /// </summary>
        /// <param name="group"></param>
        /// <param name="indexer"></param>
        /// <returns></returns>
        public JOINT_TYPE GetAbsoluteJoint(int group, INDEXER indexer);
    }
}