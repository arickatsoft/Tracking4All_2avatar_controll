// DualWeightedJointDriver
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Combines 2 joint drivers; the 'primary' is reliable but less accurate, the 'secondary' is less reliable but more accurate.
    /// </summary>
    public class DualWeightedJointDriver : ISkeletonJointDriver
    {
        private ISkeletonJointDriver primary, secondary;
        private float primaryAngularThreshold;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primary">Reliable but less accurate driver.</param>
        /// <param name="secondary">Less reliable but more accurate driver.</param>
        /// <param name="primaryAngularThreshold">When the drivers deviate by this amount prefer the primary driver fully.</param>
        public DualWeightedJointDriver(ISkeletonJointDriver primary, ISkeletonJointDriver secondary, float primaryAngularThreshold)
        {
            this.primary = primary;
            this.secondary = secondary;
            this.primaryAngularThreshold = primaryAngularThreshold;
        }

        public void Dispose()
        {
            primary.Dispose();
            secondary.Dispose();
        }

        public Quaternion GetAnimationDelta()
        {
            Quaternion t = primary.GetAnimationDelta();
            Quaternion s = secondary.GetAnimationDelta();

            float v = Mathf.InverseLerp(0, primaryAngularThreshold, Quaternion.Angle(t, s));
            Quaternion final = Quaternion.Lerp(s, t, v);

            return final;
        }

        public void Initialize(Transform jointAbsoluteParent, HumanBodyBones bone)
        {
            primary.Initialize(jointAbsoluteParent, bone);
            secondary.Initialize(jointAbsoluteParent, bone);
        }
    }
}