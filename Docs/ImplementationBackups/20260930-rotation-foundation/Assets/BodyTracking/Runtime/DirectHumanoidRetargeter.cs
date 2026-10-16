using System;
using System.Collections.Generic;
using Tracking4All;
using UnityEngine;

namespace FitFight.BodyTracking
{
    /// <summary>Experimental direct bone rotation path. Reads RTMPose, not puppet transforms.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class DirectHumanoidRetargeter : MonoBehaviour
    {
        private RTMPosePoseProvider provider;
        private global::Tracking4All.Avatar avatar;
        private AvatarSkeleton skeleton;
        private PosePuppet puppet;
        private bool previousBypass;
        private Quaternion inverseBindRoot;
        private readonly List<AimBone> limbs = new List<AimBone>();
        public bool HasFreshPose => provider != null && provider.isActiveAndEnabled && provider.HasRecentPose;
        public bool HasReliableLeg(bool left) => HasFreshPose && provider.HasReliableLeg(left);
        public float FrameAvailableToAvatarMs { get; private set; }
        public int LastAppliedSequence { get; private set; } = -1;
        public global::Tracking4All.Avatar Target => avatar;
        private struct AimBone
        {
            public HumanBodyBones bone;
            public PoseLandmarks from, to;
            public Vector3 bindDirection;
            public Quaternion bindRotation;
        }

        public void Bind(global::Tracking4All.Avatar target, RTMPosePoseProvider source)
        {
            Unbind();
            var rig = target.GetComponent<AvatarSkeleton>();
            var animator = target.GetComponent<Animator>();
            if (rig == null || animator == null || !animator.isHuman)
                throw new InvalidOperationException("Direct tracking needs a humanoid avatar and AvatarSkeleton.");
            avatar = target; provider = source; skeleton = rig; puppet = target.PosePuppet;
            inverseBindRoot = Quaternion.Inverse(rig.BindingRootRotation);
            Add(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, PoseLandmarks.LEFT_SHOULDER, PoseLandmarks.LEFT_ELBOW);
            Add(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, PoseLandmarks.LEFT_ELBOW, PoseLandmarks.LEFT_WRIST);
            Add(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, PoseLandmarks.RIGHT_SHOULDER, PoseLandmarks.RIGHT_ELBOW);
            Add(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, PoseLandmarks.RIGHT_ELBOW, PoseLandmarks.RIGHT_WRIST);
            Add(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, PoseLandmarks.LEFT_HIP, PoseLandmarks.LEFT_KNEE);
            Add(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, PoseLandmarks.LEFT_KNEE, PoseLandmarks.LEFT_ANKLE);
            Add(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, PoseLandmarks.RIGHT_HIP, PoseLandmarks.RIGHT_KNEE);
            Add(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, PoseLandmarks.RIGHT_KNEE, PoseLandmarks.RIGHT_ANKLE);
            Add(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes, PoseLandmarks.LEFT_HEEL, PoseLandmarks.LEFT_FOOT);
            Add(HumanBodyBones.RightFoot, HumanBodyBones.RightToes, PoseLandmarks.RIGHT_HEEL, PoseLandmarks.RIGHT_FOOT);
            if (puppet != null) { previousBypass = puppet.RetargetingBypassed; puppet.RetargetingBypassed = true; }
            avatar.DirectTracking = this; skeleton.DirectTracking = this;
        }
        private void Add(HumanBodyBones bone, HumanBodyBones child, PoseLandmarks from, PoseLandmarks to)
        {
            if (!skeleton.HasBone(bone) || !skeleton.HasBone(child)) return;
            limbs.Add(new AimBone { bone = bone, from = from, to = to,
                bindDirection = inverseBindRoot * (skeleton.GetBindingPosition(child) - skeleton.GetBindingPosition(bone)),
                bindRotation = inverseBindRoot * skeleton.GetBindingRotation(bone) });
        }
        private void LateUpdate()
        {
            if (avatar == null || !avatar.isActiveAndEnabled || avatar.HasPoseOverride || !HasFreshPose) return;
            Quaternion root = avatar.transform.rotation;
            var lh = provider.AvatarLandmark(PoseLandmarks.LEFT_HIP);
            var rh = provider.AvatarLandmark(PoseLandmarks.RIGHT_HIP);
            var ls = provider.AvatarLandmark(PoseLandmarks.LEFT_SHOULDER);
            var rs = provider.AvatarLandmark(PoseLandmarks.RIGHT_SHOULDER);
            Vector3 up = (ls.Position + rs.Position - lh.Position - rh.Position) * .5f;
            Vector3 across = rs.Position - ls.Position;
            Vector3 forward = Vector3.Cross(across, up);
            if (provider.Tuning.uprightBody) skeleton.ApplyStabilizedBody();
            else if (forward.sqrMagnitude > .0001f && up.sqrMagnitude > .0001f)
            {
                var torso = Quaternion.LookRotation(forward, up);
                RotateTorso(HumanBodyBones.Hips, root * torso);
                RotateTorso(HumanBodyBones.Spine, root * torso);
                RotateTorso(HumanBodyBones.Chest, root * torso);
                RotateTorso(HumanBodyBones.Neck, root * torso);
                RotateTorso(HumanBodyBones.Head, root * torso);
            }
            // Absolute rotations in parent-to-child order keep the rig's actual bone lengths.
            foreach (var limb in limbs)
            {
                if (!provider.IsJointObserved(limb.from) || !provider.IsJointObserved(limb.to)) continue;
                var a = provider.AvatarLandmark(limb.from); var b = provider.AvatarLandmark(limb.to);
                if (a.Visibility < provider.Tuning.jointConfidence || b.Visibility < provider.Tuning.jointConfidence) continue;
                var direction = b.Position - a.Position;
                if (!RTMPoseMath.Finite(direction) || direction.sqrMagnitude < .000001f) continue;
                skeleton.SetRotation(limb.bone, root * AimRotation(limb.bindDirection, direction, limb.bindRotation));
            }
            LastAppliedSequence = provider.PoseSequence;
            FrameAvailableToAvatarMs = provider.ObservationAgeMs;
        }
        public static Quaternion AimRotation(Vector3 bindDirection, Vector3 targetDirection, Quaternion bindRotation)
            => Quaternion.FromToRotation(bindDirection, targetDirection) * bindRotation;
        private void RotateTorso(HumanBodyBones bone, Quaternion direction)
        {
            if (skeleton.HasBone(bone)) skeleton.SetRotation(bone, direction * inverseBindRoot * skeleton.GetBindingRotation(bone));
        }
        public void Unbind()
        {
            if (puppet != null) puppet.RetargetingBypassed = previousBypass;
            if (avatar != null && avatar.DirectTracking == this) avatar.DirectTracking = null;
            if (skeleton != null && skeleton.DirectTracking == this) skeleton.DirectTracking = null;
            provider = null; avatar = null; skeleton = null; puppet = null; limbs.Clear(); LastAppliedSequence = -1;
        }
        private void OnDisable() { Unbind(); }
        private void OnDestroy() { Unbind(); }
    }
}
