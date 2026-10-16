// ISkeletonJointDriver
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Drives avatar skeleton.
    /// </summary>
    public interface ISkeletonJointDriver
    {
        /// <summary>
        /// Called when the driver is initialized/created.
        /// </summary>
        public void Initialize(Transform jointAbsoluteParent, HumanBodyBones bone);

        /// <summary>
        /// Get the current animation delta for the driver to be applied on top of the binding pose.
        /// </summary>
        /// <returns></returns>
        public Quaternion GetAnimationDelta();

        /// <summary>
        /// Called when the driver is removed from the avatar.
        /// </summary>
        public void Dispose();
    }

}