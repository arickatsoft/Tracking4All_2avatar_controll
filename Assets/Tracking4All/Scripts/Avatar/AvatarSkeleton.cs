// AvatarAnimator
// (C) 2024 G8gaming Ltd.
using System.Collections.Generic;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Interface for Avatar to deal with the animator and expose bones.
    /// </summary>
    [RequireComponent(typeof(Animator)), DefaultExecutionOrder(-5)] // NOTE: this must run before anything related to avatars.
    public class AvatarSkeleton : MonoBehaviour
    {
        // Handles animating the Avatar at a low level.
        [SerializeField] private Animator animator;

        private readonly Dictionary<HumanBodyBones, AvatarSkeletonBone> bones = new();
        // Each human body bone should have only 1 corresponding driver assigned.
        private readonly Dictionary<HumanBodyBones, ISkeletonJointDriver> activeDrivers = new();
        public FitFight.BodyTracking.DirectHumanoidRetargeter DirectTracking { get; set; }
        public Quaternion BindingRootRotation { get; private set; }
        private Avatar trackingAvatar;
        public bool UsesUprightBody => trackingAvatar != null && trackingAvatar.PosePuppet != null && trackingAvatar.PosePuppet.UsesUprightBody;
        private static readonly HumanBodyBones[] UprightBones = { HumanBodyBones.Hips, HumanBodyBones.Spine,
            HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Neck, HumanBodyBones.Head };
        public static bool IsUprightBone(HumanBodyBones bone) => System.Array.IndexOf(UprightBones, bone) >= 0;
        public void ApplyStabilizedBody()
        {
            foreach (var bone in UprightBones)
                if (TryGetStabilizedRotation(bone, out var rotation)) SetRotation(bone, rotation);
        }
        public bool TryGetStabilizedRotation(HumanBodyBones bone, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            var pose = trackingAvatar?.PosePuppet?.StabilizedPose;
            if (pose == null || !HasBone(bone) || !IsUprightBone(bone)) return false;
            Quaternion root = transform.rotation, bind = Quaternion.Inverse(BindingRootRotation);
            Quaternion torso = pose.BodyRotation, head = pose.HeadRotation;
            // Absolute parent-to-child rotations preserve bone lengths and let the pelvis,
            // spine and head move without accumulating the same bend at every joint.
            float weight = bone == HumanBodyBones.Hips ? .25f : bone == HumanBodyBones.Spine ? .65f : 1;
            Quaternion motion = Quaternion.Slerp(Quaternion.identity, torso, weight);
            if (bone == HumanBodyBones.Neck) motion *= Quaternion.Slerp(Quaternion.identity, head, .4f);
            if (bone == HumanBodyBones.Head) motion *= head;
            rotation = root * motion * bind * GetBindingRotation(bone);
            return true;
        }

        private void OnValidate()
        {
            if (animator == null) animator = GetComponent<Animator>();
            else
            {
                if (animator.avatar == null)
                {
                    Logger.LogError("The Animator that the AvatarSkeleton is using must have a humanoid avatar assigned.", gameObject.name);
                }
            }
        }
        private void Awake()
        {
            trackingAvatar = GetComponent<Avatar>();
            BindSkeleton();
        }
        /// <summary>
        /// Updates skeleton bones to treat the current pose as the binding pose for the skeleton.
        /// </summary>
        private void BindSkeleton()
        {
            if (animator == null) animator = GetComponent<Animator>();

            this.bones.Clear();
            BindingRootRotation = transform.rotation;

            // Create the avatar animator bones.
            HumanBodyBones[] bones = (HumanBodyBones[])System.Enum.GetValues(typeof(HumanBodyBones));
            foreach (var bone in bones)
            {
                if (bone == HumanBodyBones.LastBone) continue;

                // Construct bone reference (1 reference for every bone supported by the animator) 
                Transform boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform)
                {
                    this.bones.Add(bone, new AvatarSkeletonBone(boneTransform));
                }
            }
        }

        /// <summary>
        /// Update the drivers on skeleton.
        /// </summary>
        private void LateUpdate()
        {
            if (trackingAvatar != null && trackingAvatar.HasPoseOverride) return;
            if (DirectTracking != null && DirectTracking.isActiveAndEnabled) return;
            bool upright = UsesUprightBody;
            if (upright) ApplyStabilizedBody();
            // TODO: this could potentially be 1 frame later then the latest puppet update... use a custom lifecycle maybe.
            foreach (var d in activeDrivers)
            {
                if (!upright || !IsUprightBone(d.Key))
                    SetRotation(d.Key, d.Value.GetAnimationDelta() * GetBindingRotation(d.Key));
            }
        }

        /// <summary>
        /// Set the driver at the bone to the input driver.
        /// </summary>
        /// <param name="bone"></param>
        /// <param name="driver"></param>
        public void SetDriver(HumanBodyBones bone, ISkeletonJointDriver driver)
        {
            if (activeDrivers.ContainsKey(bone))
            {
                Logger.LogError("Duplicate drivers on avatar (" + bone + "). Only one driver per joint is supported.", gameObject.name);
                return;
            }
            if (!bones.ContainsKey(bone))
            {
                Logger.LogWarning("The avatar does not support driver " + bone + ", is the avatar correctly configued or missing bones?", gameObject.name);
                return;
            }

            _RemoveDriver(bone);
            activeDrivers.Add(bone, driver);
        }
        /// <summary>
        /// Remove the driver at the bone as the inputted driver.
        /// </summary>
        /// <param name="bone"></param>
        /// <param name="driver"></param>
        public void RemoveDriver(HumanBodyBones bone, ISkeletonJointDriver driver)
        {
            if (activeDrivers.ContainsKey(bone) && activeDrivers[bone].Equals(driver))
            {
                _RemoveDriver(bone);
            }
        }
        private void _RemoveDriver(HumanBodyBones bone)
        {
            if (!activeDrivers.ContainsKey(bone)) return;

            activeDrivers[bone].Dispose();
            activeDrivers.Remove(bone);
        }

        /// <summary>
        /// Reset the entire skeleton to the binding pose.
        /// </summary>
        public void ResetPose()
        {
            foreach (var bone in bones)
            {
                bone.Value.Reset();
            }
        }


        // Exposed get/set state of bones.
        public bool HasBone(HumanBodyBones bone)
        {
            return bones.ContainsKey(bone);
        }
        public Quaternion GetBindingRotation(HumanBodyBones bone)
        {
            if (!bones.ContainsKey(bone))
            {
                Logger.LogError("Bone with identifier " + bone + " does not exist is that avatar correctly configured?", gameObject.name);
                return Quaternion.identity;
            }

            return bones[bone].BindingRotation;
        }
        public void SetRotation(HumanBodyBones bone, Quaternion rotation)
        {
            if (!bones.ContainsKey(bone))
            {
                Logger.LogError("Bone with identifier " + bone + " does not exist is that avatar correctly configured?", gameObject.name);
                return;
            }

            bones[bone].BoneTransform.rotation = rotation;
        }
        public Transform GetTransform(HumanBodyBones bone)
        {
            if (!HasBone(bone)) return null;

            return bones[bone].BoneTransform;
        }
        public Vector3 GetBindingPosition(HumanBodyBones bone) => bones[bone].BindingPosition;


        // Internal to only avatar skeleton, do not leak references out since they can be destroyed via bindskeleton.
        public class AvatarSkeletonBone
        {
            private Transform bone;
            private Quaternion bindingRotation;

            public Transform BoneTransform => bone;
            public Quaternion BindingRotation => bindingRotation;
            public Vector3 BindingPosition { get; private set; }

            public AvatarSkeletonBone(Transform bone)
            {
                this.bone = bone;
                bindingRotation = bone.rotation;
                BindingPosition = bone.position;
            }

            /// <summary>
            /// Reset the bone to the binding rotation.
            /// </summary>
            public void Reset()
            {
                bone.transform.rotation = BindingRotation;
            }
        }
    }

}
