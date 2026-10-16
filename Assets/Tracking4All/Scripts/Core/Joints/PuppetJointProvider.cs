// JointProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    /// <summary>
    /// Allows you to assign and use a joint provider interface in the unity inspector. 
    /// </summary>
    /// <typeparam name="INDEXER"></typeparam>
    [System.Serializable]
    public class PuppetJointProvider<INDEXER, JOINT_DATA> : InterfaceProvider<IJointProvider<INDEXER, JOINT_DATA>>,
        IJointProvider<INDEXER, JOINT_DATA>
        where INDEXER : System.Enum
        where JOINT_DATA : PuppetJoint, new()
    {
        public IProviderLifecycle Lifecycle => Provider.Lifecycle;

        public int GroupSize => Provider.GroupSize;

        public int DataCount => Provider.DataCount;

        public JOINT_DATA Get(int group, INDEXER index)
        {
            return Provider.Get(group, index);
        }

        public JOINT_DATA Get(int group, int index)
        {
            return Provider.Get(group, index);
        }

        public JOINT_DATA GetAbsoluteJoint(int group, INDEXER indexer)
        {
            return Provider.GetAbsoluteJoint(group, indexer);
        }

    }
}