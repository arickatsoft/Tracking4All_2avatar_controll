// Avatar
// (C) 2024 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    [RequireComponent(typeof(AvatarSkeleton))]
    /// <summary>
    /// The (Humanoid) Avatar is comprised of AvatarJointProvider(s) that receive data from joint provider(s) to drive the motion of a humanoid using it's AvatarSkeleton.
    /// </summary>
    public class Avatar : MonoBehaviour
    {
        [SerializeField] protected AvatarSkeleton avatarSkeleton;
        [SerializeField] protected AvatarJointBoneProvider<PoseJoints, PoseJoint> pose;
        [SerializeField] protected AvatarJointBoneProvider<HandJoints, HandJoint> rightHand;
        [SerializeField] protected AvatarJointBoneProvider<HandJoints, HandJoint> leftHand;
        [SerializeField] protected AvatarJointBoneProvider<FaceJoints, FaceJoint> face;

        public FitFight.BodyTracking.DirectHumanoidRetargeter DirectTracking { get; set; }
        // A practice animation composer owns the final pose without stopping inference.
        public Behaviour PoseOverride { get; set; }
        public bool HasPoseOverride => PoseOverride != null && PoseOverride.isActiveAndEnabled;
        public PosePuppet PosePuppet => pose?.PosePuppet;
        public bool HasFreshPose => isActiveAndEnabled && (DirectTracking != null && DirectTracking.isActiveAndEnabled
            ? DirectTracking.HasFreshPose : pose != null && pose.HasFreshData);
        public bool HasReliableLeg(bool left) => HasFreshPose && (DirectTracking != null && DirectTracking.isActiveAndEnabled
            ? DirectTracking.HasReliableLeg(left) : pose.HasReliableLeg(left));

        private void OnValidate()
        {
            if (avatarSkeleton == null) avatarSkeleton = GetComponent<AvatarSkeleton>();
        }
        private void OnEnable()
        {
            ResetAvatar();
        }
        private void OnDisable()
        {
            ReleaseAvatar();
        }

        private void InitializeDrivers()
        {
            // Depending on the combination of available joints, set drivers to the skeleton.

            bool hasFace = face.HasProvider;
            bool hasLeftHand = leftHand.HasProvider;
            bool hasRightHand = rightHand.HasProvider;
            bool hasPose = pose.HasProvider;

            if (hasFace)
            {
                if (hasPose)
                {
                    SetDualWeightedPoseHeadDriver(HumanBodyBones.Head);
                }
                else
                {
                    SetAsFaceDriver(HumanBodyBones.Head);
                }
            }

            if (hasPose)
            {
                if (!hasFace)
                {
                    SetAsPoseDriver(HumanBodyBones.Head);
                }

                SetAsPoseDriver(HumanBodyBones.Hips);
                SetAsPoseDriver(HumanBodyBones.Spine);
                SetAsPoseDriver(HumanBodyBones.Chest);

                SetAsPoseDriver(HumanBodyBones.RightShoulder);
                SetAsPoseDriver(HumanBodyBones.RightUpperArm);
                SetAsPoseDriver(HumanBodyBones.RightLowerArm);
                SetAsPoseDriver(HumanBodyBones.RightUpperLeg);
                SetAsPoseDriver(HumanBodyBones.RightLowerLeg);
                SetAsPoseDriver(HumanBodyBones.RightFoot);

                SetAsPoseDriver(HumanBodyBones.LeftShoulder);
                SetAsPoseDriver(HumanBodyBones.LeftUpperArm);
                SetAsPoseDriver(HumanBodyBones.LeftLowerArm);
                SetAsPoseDriver(HumanBodyBones.LeftUpperLeg);
                SetAsPoseDriver(HumanBodyBones.LeftLowerLeg);
                SetAsPoseDriver(HumanBodyBones.LeftFoot);

                if (!hasRightHand) SetAsPoseDriver(HumanBodyBones.RightHand);
                if (!hasLeftHand) SetAsPoseDriver(HumanBodyBones.LeftHand);
            }

            if (hasRightHand)
            {
                SetAsRightHandDriver(HumanBodyBones.RightHand);

                SetAsRightHandDriver(HumanBodyBones.RightIndexDistal);
                SetAsRightHandDriver(HumanBodyBones.RightIndexIntermediate);
                SetAsRightHandDriver(HumanBodyBones.RightIndexProximal);

                SetAsRightHandDriver(HumanBodyBones.RightMiddleDistal);
                SetAsRightHandDriver(HumanBodyBones.RightMiddleIntermediate);
                SetAsRightHandDriver(HumanBodyBones.RightMiddleProximal);

                SetAsRightHandDriver(HumanBodyBones.RightRingDistal);
                SetAsRightHandDriver(HumanBodyBones.RightRingIntermediate);
                SetAsRightHandDriver(HumanBodyBones.RightRingProximal);

                SetAsRightHandDriver(HumanBodyBones.RightLittleDistal);
                SetAsRightHandDriver(HumanBodyBones.RightLittleIntermediate);
                SetAsRightHandDriver(HumanBodyBones.RightLittleProximal);

                SetAsRightHandDriver(HumanBodyBones.RightThumbDistal);
                SetAsRightHandDriver(HumanBodyBones.RightThumbIntermediate);
                SetAsRightHandDriver(HumanBodyBones.RightThumbProximal);
            }

            if (hasLeftHand)
            {
                SetAsLeftHandDriver(HumanBodyBones.LeftHand);

                SetAsLeftHandDriver(HumanBodyBones.LeftIndexDistal);
                SetAsLeftHandDriver(HumanBodyBones.LeftIndexIntermediate);
                SetAsLeftHandDriver(HumanBodyBones.LeftIndexProximal);

                SetAsLeftHandDriver(HumanBodyBones.LeftMiddleDistal);
                SetAsLeftHandDriver(HumanBodyBones.LeftMiddleIntermediate);
                SetAsLeftHandDriver(HumanBodyBones.LeftMiddleProximal);

                SetAsLeftHandDriver(HumanBodyBones.LeftRingDistal);
                SetAsLeftHandDriver(HumanBodyBones.LeftRingIntermediate);
                SetAsLeftHandDriver(HumanBodyBones.LeftRingProximal);

                SetAsLeftHandDriver(HumanBodyBones.LeftLittleDistal);
                SetAsLeftHandDriver(HumanBodyBones.LeftLittleIntermediate);
                SetAsLeftHandDriver(HumanBodyBones.LeftLittleProximal);

                SetAsLeftHandDriver(HumanBodyBones.LeftThumbDistal);
                SetAsLeftHandDriver(HumanBodyBones.LeftThumbIntermediate);
                SetAsLeftHandDriver(HumanBodyBones.LeftThumbProximal);
            }

        }

        /// <summary>
        /// Resets and rebuilds data to use the avatar. Useful if you switch out the provider at runtime.
        /// </summary>
        /// <param name="poseProvider"></param>
        public void ResetAvatar(
            IPoseJointProvider<PoseJoints> poseProvider = null,
            IHandJointProvider<HandJoints> rightHandProvider = null,
            IHandJointProvider<HandJoints> leftHandProvider = null,
            IFaceJointProvider<FaceJoints> faceProvider = null)
        {
            // can sometimes be null, probably due to serialized field not creating object yet???
            if (pose == null) pose = new AvatarJointBoneProvider<PoseJoints, PoseJoint>();
            if (rightHand == null) rightHand = new AvatarJointBoneProvider<HandJoints, HandJoint>();
            if (leftHand == null) leftHand = new AvatarJointBoneProvider<HandJoints, HandJoint>();
            if (face == null) face = new AvatarJointBoneProvider<FaceJoints, FaceJoint>();

            if (avatarSkeleton == null) avatarSkeleton = GetComponent<AvatarSkeleton>();

            // Reset the skeleton to the binding pose.
            avatarSkeleton.ResetPose();

            // Release drivers before rebuilding.
            ReleaseAvatar();

            // Rebuild drivers.
            pose.Reset(avatarSkeleton, poseProvider, PosePuppet.mapping);
            rightHand.Reset(avatarSkeleton, rightHandProvider, HandPuppet.GetMapping(Handedness.RIGHT));
            leftHand.Reset(avatarSkeleton, leftHandProvider, HandPuppet.GetMapping(Handedness.LEFT));
            face.Reset(avatarSkeleton, faceProvider, FacePuppet.mapping);

            // Set drivers on the skeleton.
            InitializeDrivers();
        }
        private void ReleaseAvatar()
        {
            if (pose.HasProvider) pose.Release(avatarSkeleton);
            if (rightHand.HasProvider) rightHand.Release(avatarSkeleton);
            if (leftHand.HasProvider) leftHand.Release(avatarSkeleton);
            if (face.HasProvider) face.Release(avatarSkeleton);
        }

        private void SetAsPoseDriver(HumanBodyBones bone)
        {
            var d = pose.GetDriver(bone);
            d.Initialize(transform, bone);
            avatarSkeleton.SetDriver(bone, d);
        }
        private void SetAsRightHandDriver(HumanBodyBones bone)
        {
            var d = rightHand.GetDriver(bone);
            d.Initialize(transform, bone);
            avatarSkeleton.SetDriver(bone, d);
        }
        private void SetAsLeftHandDriver(HumanBodyBones bone)
        {
            var d = leftHand.GetDriver(bone);
            d.Initialize(transform, bone);
            avatarSkeleton.SetDriver(bone, d);
        }
        private void SetAsFaceDriver(HumanBodyBones bone)
        {
            var d = face.GetDriver(bone);
            d.Initialize(transform, bone);
            avatarSkeleton.SetDriver(bone, d);
        }
        private void SetDualWeightedPoseHeadDriver(HumanBodyBones bone)
        {
            var d = face.GetDriver(bone);
            var d2 = pose.GetDriver(bone);

            var dualWeightedFaceDriver = new DualWeightedJointDriver(d2, d, 60f);
            dualWeightedFaceDriver.Initialize(transform, bone);
            avatarSkeleton.SetDriver(bone, dualWeightedFaceDriver);
        }
    }
}
