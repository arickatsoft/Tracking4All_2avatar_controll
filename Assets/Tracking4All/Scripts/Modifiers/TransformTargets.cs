using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Transform landmarks so they behave as if it was a child of the input transform parent.
    /// </summary>
    public class TransformTargets : Transformer
    {
        [SerializeField] protected Transform parent;

        protected virtual Transform Parent => parent;

        public bool UprightCoordinates { get; set; }
        protected override Matrix4x4 LocalToWorldMatrix
        {
            get
            {
                if (!UprightCoordinates) return Parent.localToWorldMatrix;
                Vector3 forward = Vector3.ProjectOnPlane(Parent.forward, Vector3.up);
                Quaternion facing = forward.sqrMagnitude > .001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
                return Matrix4x4.TRS(Parent.position, facing, Parent.lossyScale);
            }
        }
    }
}
