// IHandJointProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public interface IHandJointProvider<INDEXER> : IJointProvider<INDEXER, HandJoint>
        where INDEXER : System.Enum
    {
        // public new Dictionary<INDEXER, HumanBodyBones> HumanMapping { get; }
    }
}