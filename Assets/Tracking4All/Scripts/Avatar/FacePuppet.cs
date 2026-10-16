using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tracking4All
{
    [System.Serializable]
    public class FaceJoint : PuppetJoint
    {

    }
    public enum FaceJoints { Head, Neck }
    public interface IFaceJointProvider<INDEXER> : IJointProvider<INDEXER, FaceJoint>
        where INDEXER : System.Enum
    {
        public Dictionary<INDEXER, HumanBodyBones> HumanMapping { get; }
    }

    [DefaultExecutionOrder(-1)]
    public class FacePuppet : PuppetBase<FaceJoints, FaceJoint>,
        IFaceJointProvider<FaceJoints>
    {
        [Header("General")]
        [SerializeField] protected FaceDataProvider faceProvider;
        [SerializeField] protected PuppetJointProvider<PoseJoints, PoseJoint> optionalPosePuppet;
        [SerializeField] protected PuppetBodyPart[] faceBodyParts;

        protected override Table<FaceJoint> Joints => joints;

        protected void Start()
        {
            Transform originalParent = transform.parent;
            if (optionalPosePuppet.HasInterface)
            {
                transform.SetParent(
                    optionalPosePuppet.GetAbsoluteJoint(0, PoseJoints.Neck).PuppetJointTransform,
                    true);

                if (transform.parent != originalParent && transform.parent != null)
                    transform.localPosition = Vector3.zero;
            }
        }

        protected override void HookProviders()
        {
            if (CanHookProvider(faceProvider))
            {
                faceProvider.OnFaceDatasUpdated += FaceProvider_OnFaceDatasUpdate;
            }
        }        protected override void ReleaseProviders()
        {
            if (CanHookProvider(faceProvider))
            {
                faceProvider.OnFaceDatasUpdated -= FaceProvider_OnFaceDatasUpdate;
            }
        }

        private void FaceProvider_OnFaceDatasUpdate(int group)
        {
            Tick(0);
        }
        /// <summary>
        /// Calculate the transforms from the face data.
        /// </summary>
        protected override void CalculateTransforms()
        {
            CalculateHead();
        }

        private void CalculateHead()
        {
            Matrix4x4 m = faceProvider.Get(0, FaceDataIdentifier.DEFAULT).FaceOrientationMatrix;
            Vector3 c0 = m.GetColumn(0);
            Vector3 c1 = m.GetColumn(1);
            Vector3 c2 = m.GetColumn(2);
            Vector3 c3 = m.GetColumn(3);

            SetRotation(HumanBodyBones.Head,
                Quaternion.LookRotation(c2, c1));

            /*if (optionalPosePuppet.HasInterface)
            {
                Quaternion chest = optionalPosePuppet.Get(0, PoseJoints.Chest).PuppetJointTransform.rotation;
                SetRotation(HumanBodyBones.Neck, Quaternion.Lerp(chest, Quaternion.LookRotation(c2, c1), .8f));
            }*/
        }

        public FaceJoint Get(int group, FaceJoints index)
        {
            if (settings.Mirror) return joints.Get(System.Convert.ToInt32(index.Flip()));

            return joints.Get(System.Convert.ToInt32(index));
        }
        public FaceJoint Get(int group, int index)
        {
            return Get(group, (FaceJoints)index);
        }
        public FaceJoint GetAbsoluteJoint(int group, FaceJoints indexer)
        {
            // Get, but absolutely relative to the puppet
            return joints.Get((int)indexer);
        }

        protected override void ConstructJoint(FaceJoints joint, HumanBodyBones bone)
        {
            //if (settings.Mirror) joint = joint.Flip();

            joints.Get((int)joint).Reconstruct(bone, puppetPartMap[bone].BodyPart);
        }

        public override Dictionary<FaceJoints, HumanBodyBones> HumanMapping => mapping;
        public static readonly Dictionary<FaceJoints, HumanBodyBones> mapping = new Dictionary<FaceJoints, HumanBodyBones>()
        {
            { FaceJoints.Head, HumanBodyBones.Head },
        };

        public override Dictionary<HumanBodyBones, FaceJoints> JointMapping => reversedMapping;

        public IProviderLifecycle Lifecycle => this;

        public override int GroupSize => 1;

        public static readonly Dictionary<HumanBodyBones, FaceJoints> reversedMapping = mapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        protected override List<PuppetBodyPart> GetPartsRange()
        {
            List<PuppetBodyPart> range = new List<PuppetBodyPart>();
            range.AddRange(faceBodyParts);
            return range;
        }

        protected void ConstructJoint(FaceJoint joint, HumanBodyBones bone)
        {
            joints.Get(System.Convert.ToInt32(bone))
                .Reconstruct(bone, puppetPartMap[bone].BodyPart);
        }
    }
}