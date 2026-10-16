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
        private Animator animator;
        private bool previousAnimatorEnabled;
        private SkinnedMeshRenderer[] skins;
        private bool[] previousSkinUpdates;
        private Quaternion inverseBindRoot;
        private readonly List<AimBone> limbs = new List<AimBone>();
        private readonly Dictionary<HumanBodyBones, BoneRotationFilter> rotations = new Dictionary<HumanBodyBones, BoneRotationFilter>();
        private readonly Dictionary<HumanBodyBones, Quaternion> appliedTargets = new Dictionary<HumanBodyBones, Quaternion>();
        public bool TryGetAppliedTarget(HumanBodyBones bone, out Quaternion target) => appliedTargets.TryGetValue(bone, out target);
        private static readonly HumanBodyBones[] BodyBones = { HumanBodyBones.Hips, HumanBodyBones.Spine,
            HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Neck, HumanBodyBones.Head };
        private bool previousSmoothing, previousSpeedLimit, previousUpright;
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
            this.animator=animator; previousAnimatorEnabled=animator.enabled;
            skins=target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            previousSkinUpdates=new bool[skins.Length];
            for(int i=0;i<skins.Length;i++)
            {
                previousSkinUpdates[i]=skins[i].updateWhenOffscreen;
                skins[i].updateWhenOffscreen=true;
            }
            inverseBindRoot = Quaternion.Inverse(rig.BindingRootRotation);
            foreach (var bone in BodyBones)
                if (skeleton.HasBone(bone)) rotations.Add(bone, new BoneRotationFilter(bone));
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
            rotations.Add(bone, new BoneRotationFilter(bone));
            limbs.Add(new AimBone { bone = bone, from = from, to = to,
                bindDirection = inverseBindRoot * (skeleton.GetBindingPosition(child) - skeleton.GetBindingPosition(bone)),
                bindRotation = inverseBindRoot * skeleton.GetBindingRotation(bone) });
        }
        private void LateUpdate()
        {
            appliedTargets.Clear();
            if (avatar == null || !avatar.isActiveAndEnabled || avatar.HasPoseOverride || !HasFreshPose)
            { if(animator!=null) animator.enabled=previousAnimatorEnabled; ResetRotations(); return; }
            // Manual humanoid outputs can keep their cached skinning pose even after
            // exposed bone transforms change. Direct tracking owns transforms and
            // skinning together; restore Animator evaluation for assist/fallback.
            animator.enabled=false;
            var tuning = provider.Tuning;
            foreach(var skin in skins) if(skin!=null) skin.updateWhenOffscreen=true;
            if (previousSmoothing != tuning.directRotationSmoothing || previousSpeedLimit != tuning.directRotationSpeedLimit
                || previousUpright != tuning.uprightBody) ResetRotations();
            previousSmoothing = tuning.directRotationSmoothing; previousSpeedLimit = tuning.directRotationSpeedLimit;
            previousUpright = tuning.uprightBody;
            Quaternion root = avatar.transform.rotation;
            var lh = provider.AvatarLandmark(PoseLandmarks.LEFT_HIP);
            var rh = provider.AvatarLandmark(PoseLandmarks.RIGHT_HIP);
            var ls = provider.AvatarLandmark(PoseLandmarks.LEFT_SHOULDER);
            var rs = provider.AvatarLandmark(PoseLandmarks.RIGHT_SHOULDER);
            Vector3 up = (ls.Position + rs.Position - lh.Position - rh.Position) * .5f;
            Vector3 across = rs.Position - ls.Position;
            Vector3 forward = Vector3.Cross(across, up);
            bool reliableTorso = Observed(PoseLandmarks.LEFT_HIP) && Observed(PoseLandmarks.RIGHT_HIP)
                && Observed(PoseLandmarks.LEFT_SHOULDER) && Observed(PoseLandmarks.RIGHT_SHOULDER);
            if (provider.Tuning.uprightBody)
            {
                foreach (var bone in BodyBones)
                    if (reliableTorso && skeleton.TryGetStabilizedRotation(bone, out var target)) ApplyRotation(bone, target);
                    else HoldRotation(bone, root);
            }
            else if (reliableTorso && RTMPoseMath.Finite(forward) && RTMPoseMath.Finite(up)
                && forward.sqrMagnitude > .0001f && up.sqrMagnitude > .0001f)
            {
                var torso = Quaternion.LookRotation(forward, up);
                foreach (var bone in BodyBones) RotateTorso(bone, root * torso);
            }
            else foreach (var bone in BodyBones) HoldRotation(bone, root);
            // Absolute rotations in parent-to-child order keep the rig's actual bone lengths.
            foreach (var limb in limbs)
            {
                if (!Observed(limb.from) || !Observed(limb.to)) { HoldRotation(limb.bone, root); continue; }
                var a = provider.AvatarLandmark(limb.from); var b = provider.AvatarLandmark(limb.to);
                var direction = b.Position - a.Position;
                if (!RTMPoseMath.Finite(direction) || direction.sqrMagnitude < .000001f) { HoldRotation(limb.bone, root); continue; }
                ApplyRotation(limb.bone, root * AimRotation(limb.bindDirection, direction, limb.bindRotation));
            }
            LastAppliedSequence = provider.PoseSequence;
            FrameAvailableToAvatarMs = provider.ObservationAgeMs;
        }
        public static Quaternion AimRotation(Vector3 bindDirection, Vector3 targetDirection, Quaternion bindRotation)
            => Quaternion.FromToRotation(bindDirection, targetDirection) * bindRotation;
        private void RotateTorso(HumanBodyBones bone, Quaternion direction)
        {
            if (skeleton.HasBone(bone)) ApplyRotation(bone, direction * inverseBindRoot * skeleton.GetBindingRotation(bone));
        }
        private bool Observed(PoseLandmarks joint)
        {
            var value = provider.AvatarLandmark(joint);
            return provider.IsJointObserved(joint) && value.Visibility >= provider.Tuning.jointConfidence
                && RTMPoseMath.Finite(value.Position);
        }
        private void ApplyRotation(HumanBodyBones bone, Quaternion worldTarget)
        {
            appliedTargets[bone] = worldTarget;
            var tuning = provider.Tuning;
            if (!tuning.directRotationSmoothing && !tuning.directRotationSpeedLimit)
            { skeleton.SetRotation(bone, worldTarget); return; }
            Quaternion root = avatar.transform.rotation, inverseRoot = Quaternion.Inverse(root);
            // Filter relative to the avatar root so game-facing changes do not become tracking lag.
            Quaternion value = rotations[bone].Step(inverseRoot * worldTarget,
                inverseRoot * skeleton.GetTransform(bone).rotation, Time.realtimeSinceStartupAsDouble,
                tuning.directRotationSmoothing, tuning.directRotationSpeedLimit, tuning.directMaxDegreesPerSecond);
            skeleton.SetRotation(bone, root * value);
        }
        private void HoldRotation(HumanBodyBones bone, Quaternion root)
        {
            if (!rotations.TryGetValue(bone, out var filter)) return;
            filter.Hold();
            if ((provider.Tuning.directRotationSmoothing || provider.Tuning.directRotationSpeedLimit) && filter.HasValue)
                skeleton.SetRotation(bone, root * filter.Value);
        }
        private void ResetRotations()
        {
            foreach (var filter in rotations.Values) filter.Reset();
        }
        public void Unbind()
        {
            if(skins!=null) for(int i=0;i<skins.Length;i++) if(skins[i]!=null)
                skins[i].updateWhenOffscreen=previousSkinUpdates[i];
            skins=null;
            if(animator!=null) animator.enabled=previousAnimatorEnabled;
            animator=null;
            if (puppet != null) puppet.RetargetingBypassed = previousBypass;
            if (avatar != null && avatar.DirectTracking == this) avatar.DirectTracking = null;
            if (skeleton != null && skeleton.DirectTracking == this) skeleton.DirectTracking = null;
            provider = null; avatar = null; skeleton = null; puppet = null; limbs.Clear(); rotations.Clear(); appliedTargets.Clear(); LastAppliedSequence = -1;
            FrameAvailableToAvatarMs = 0;
        }
        private void OnDisable() { Unbind(); }
        private void OnDestroy() { Unbind(); }
    }
}
