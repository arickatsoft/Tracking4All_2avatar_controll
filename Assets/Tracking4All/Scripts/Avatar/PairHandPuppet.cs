// PairHandPuppet
// (C) 2024 G8gaming Ltd.
using System;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Allows you to access 2 hand joint providers as one while respecting mirroring options.
    /// </summary>
    public class PairHandPuppet : MonoBehaviour,
        IHandJointProvider<HandJoints>
    {
        public SolutionSettingsProvider settings;
        public PuppetJointProvider<HandJoints, HandJoint> rightHand;
        public PuppetJointProvider<HandJoints, HandJoint> leftHand;

        public int DataCount => rightHand.DataCount;

        public float LastUpdateTime => lastUpdateTime;

        public IProviderLifecycle Lifecycle => throw new NotImplementedException();
        public int GroupSize => 2;

        public event GroupEvent OnUpdated;

        private float lastUpdateTime;

        private void OnEnable()
        {
            if (rightHand.HasInterface)
            {
                rightHand.Lifecycle.OnUpdated += Hand_OnJointsUpdated;
            }

            if (leftHand.HasInterface)
            {
                leftHand.Lifecycle.OnUpdated += Hand_OnJointsUpdated;
            }
        }
        private void OnDisable()
        {
            if (rightHand.HasInterface)
            {
                rightHand.Lifecycle.OnUpdated -= Hand_OnJointsUpdated;
            }

            if (leftHand.HasInterface)
            {
                leftHand.Lifecycle.OnUpdated -= Hand_OnJointsUpdated;
            }
        }

        private void Hand_OnJointsUpdated(int group)
        {
            lastUpdateTime = Helpers.GetTime();

            OnUpdated?.Invoke(group);
        }


        // Access must respect mirroring.

        public HandJoint Get(int group, HandJoints index)
        {
            if (settings.Mirror)
            {
                group = (int)((Handedness)group).Flip();
            }

            switch ((Handedness)group)
            {
                case Handedness.LEFT:
                    return leftHand.Get(group, index);
                case Handedness.RIGHT:
                    return rightHand.Get(group, index);
            }

            throw new NotImplementedException();
        }
        public HandJoint Get(int group, int index)
        {
            if (settings.Mirror)
            {
                group = (int)((Handedness)group).Flip();
            }

            switch ((Handedness)group)
            {
                case Handedness.LEFT:
                    return leftHand.Get(group, index);
                case Handedness.RIGHT:
                    return rightHand.Get(group, index);
            }

            throw new NotImplementedException("hand pair puppet " + group);
        }

        public HandJoint GetAbsoluteJoint(int group, HandJoints index)
        {
            // ??? mirror, nah
            switch ((Handedness)group)
            {
                case Handedness.LEFT:
                    return leftHand.Get(group, index);
                case Handedness.RIGHT:
                    return rightHand.Get(group, index);
            }

            throw new NotImplementedException();
        }
    }
}