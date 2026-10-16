using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tracking4All
{
    /// <summary>
    /// Calculates joints given pose landmarks provider, provides joints as output.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class PosePuppet : PuppetBase<PoseJoints, PoseJoint>, IPoseJointProvider<PoseJoints>
    {
        [Header("General")]
        [SerializeField] protected LandmarkProvider<PoseLandmarks> poseProvider;
        public GameObject PoseSource => poseProvider?.Source;
        public bool RetargetingBypassed { get; set; }
        public FitFight.BodyTracking.RTMPosePoseProvider StabilizedPose => (poseProvider?.Provider as PoseSolution)?.ActiveRTMPose;
        public bool UsesUprightBody
        {
            get
            {
                var rtm = (poseProvider?.Provider as PoseSolution)?.ActiveRTMPose;
                return rtm != null && rtm.isActiveAndEnabled && rtm.Tuning.uprightBody && rtm.HasRecentPose;
            }
        }
        [SerializeField] protected PuppetBodyPart[] centreBodyParts;
        [SerializeField] protected bool generateEmptyCentreParts; // editor only flags to auto generate the parts given the hierarchy.
        [SerializeField] protected PuppetBodyPart[] rightBodyParts;
        [SerializeField] protected bool generateEmptyRightParts;
        [SerializeField] protected PuppetBodyPart[] leftBodyParts;
        [SerializeField] protected bool generateEmptyLeftParts;
        [Header("Additional Configuration")]
        [SerializeField] protected PoleConfiguration kneePoleConfiguration;
        [SerializeField] protected PoleConfiguration elbowPoleConfiguration;
        [SerializeField] protected float kneeSpacingBias = .03f;
        [Header("Leg stability")]
        [SerializeField] private bool stabilizeLegs = true;
        [Tooltip("Fraction of tracked leg length needed to move the leg. 0.04 is about 3 cm for a 75 cm leg. Larger movements pass through immediately.")]
        [Range(0f, .15f)] [SerializeField] private float legMovementThreshold = .04f;
        private readonly Vector3[] acceptedLegPoints = new Vector3[8];
        private readonly Vector3[] measuredLegPoints = new Vector3[8];
        private readonly bool[] legInitialized = new bool[2];
        private readonly bool[] legFallback = new bool[2];
        private readonly float[] legLastSeen = { float.NegativeInfinity, float.NegativeInfinity };
        private readonly bool[] legMeasuredThisFrame = new bool[2];
        public bool HasReliableLeg(bool left)
        {
            if (!isActiveAndEnabled) return false;
            // Match Get(PoseJoints), which flips the output joint when mirrored.
            int side = (settings.Mirror ? !left : left) ? 0 : 1;
            // Turning off stabilization must not make the guard suppress every leg.
            return (!stabilizeLegs && !responsiveLegs) || (Time.unscaledTime - lastLegSample < .35f
                && legInitialized[side] && !legFallback[side]);
        }
        public bool HasMeasuredLeg(bool left)
        {
            int side = (settings.Mirror ? !left : left) ? 0 : 1;
            return HasReliableLeg(left) && (!stabilizeLegs || responsiveLegs || legMeasuredThisFrame[side]);
        }
        private readonly Vector3[] measuredHips = new Vector3[2];
        private float lastLegSample = float.NegativeInfinity;
        private bool legSampleMirror;
        private bool responsiveLegs;
        private static readonly PoseLandmarks[] legPointIndices = {
            PoseLandmarks.LEFT_KNEE, PoseLandmarks.LEFT_ANKLE, PoseLandmarks.LEFT_HEEL, PoseLandmarks.LEFT_FOOT,
            PoseLandmarks.RIGHT_KNEE, PoseLandmarks.RIGHT_ANKLE, PoseLandmarks.RIGHT_HEEL, PoseLandmarks.RIGHT_FOOT };

        protected override Table<PoseJoint> Joints => joints;

        protected override void HookProviders()
        {
            if (CanHookProvider(poseProvider))
            {
                poseProvider.OnLandmarksUpdated += PoseProvider_OnLandmarksUpdated;
            }
        }
        protected override void ReleaseProviders()
        {
            legInitialized[0] = legInitialized[1] = false;
            if (CanHookProvider(poseProvider))
            {
                poseProvider.OnLandmarksUpdated -= PoseProvider_OnLandmarksUpdated;
            }
        }

        private void OnValidate()
        {
            if (generateEmptyCentreParts && centreBodyParts.Length == 0)
            {
                Logger.LogInfo("Generating parts.");
                centreBodyParts = GetBodyParts(new string[] { "Joint" }, new string[] { ".R", ".L" });
            }

            if (generateEmptyRightParts && rightBodyParts.Length == 0)
            {
                Logger.LogInfo("Generating parts.");
                rightBodyParts = GetBodyParts(new string[] { "Joint", ".R" }, new string[] { ".L" });
            }

            if (generateEmptyLeftParts && leftBodyParts.Length == 0)
            {
                Logger.LogInfo("Generating parts.");
                leftBodyParts = GetBodyParts(new string[] { "Joint", ".L" }, new string[] { ".R" });
            }

            generateEmptyCentreParts = false;
            generateEmptyRightParts = false;
            generateEmptyLeftParts = false;
        }

        private void PoseProvider_OnLandmarksUpdated(int group)
        {
            if (RetargetingBypassed) return;
            // Do not turn a low-confidence room detection into an animated body.
            // Hips/feet may be outside the frame while the arms are still usable.
            var rtm = (poseProvider.Provider as PoseSolution)?.ActiveRTMPose;
            float threshold = rtm != null && rtm.isActiveAndEnabled ? rtm.Tuning.jointConfidence : .35f;
            if (poseProvider.Get(group, PoseLandmarks.LEFT_SHOULDER).Visibility < threshold
                || poseProvider.Get(group, PoseLandmarks.RIGHT_SHOULDER).Visibility < threshold) return;
            Tick(0);
        }

        /// <summary>
        /// Allows runtime replacement of the pose provider.
        /// Properly unsubscribes from old provider and subscribes to new one.
        /// </summary>
        /// <param name="newProvider">The new LandmarkProvider to use</param>
        public void SetPoseProvider(LandmarkProvider<PoseLandmarks> newProvider)
        {
            if (newProvider == null)
            {
                Logger.LogError("Cannot set null pose provider!", gameObject.name);
                return;
            }

            // Unsubscribe from old provider if we're initialized and have one
            if (initialized && poseProvider != null && poseProvider.HasInterface)
            {
                poseProvider.OnLandmarksUpdated -= PoseProvider_OnLandmarksUpdated;
                Logger.LogInfo($"Unsubscribed from old provider on {gameObject.name}");
            }
            
            // Assign new provider
            poseProvider = newProvider;
            legInitialized[0] = legInitialized[1] = false;
            
            // Subscribe to new provider if we're initialized
            if (initialized && poseProvider != null && poseProvider.HasInterface)
            {
                poseProvider.OnLandmarksUpdated += PoseProvider_OnLandmarksUpdated;
                Logger.LogInfo($"✅ Successfully subscribed to new provider on {gameObject.name}");
            }
            else if (!poseProvider.HasInterface)
            {
                Logger.LogWarning($"New provider does not have valid interface on {gameObject.name}");
            }
        }

        /// <summary>
        /// Calculate the transforms from the pose data.
        /// </summary>
        protected override void CalculateTransforms()
        {
            UpdateLegSamples();
            Vector3 leftHip = Get(PoseLandmarks.LEFT_HIP);
            Vector3 leftShoulder = Get(PoseLandmarks.LEFT_SHOULDER);
            Vector3 rightHip = Get(PoseLandmarks.RIGHT_HIP);
            Vector3 rightShoulder = Get(PoseLandmarks.RIGHT_SHOULDER);

            CalculateTorso(HumanBodyBones.Hips,
                Vector3.Lerp(leftHip, leftShoulder, 0f), Vector3.Lerp(rightHip, rightShoulder, 0f),
                PoseLandmarks.LEFT_SHOULDER, PoseLandmarks.RIGHT_SHOULDER);
            CalculateTorso(HumanBodyBones.Spine,
                Vector3.LerpUnclamped(leftHip, leftShoulder, .3f), Vector3.LerpUnclamped(rightHip, rightShoulder, .3f),
                PoseLandmarks.LEFT_SHOULDER, PoseLandmarks.RIGHT_SHOULDER);
            CalculateTorso(HumanBodyBones.Chest,
                Vector3.LerpUnclamped(leftHip, leftShoulder, .75f), Vector3.LerpUnclamped(rightHip, rightShoulder, .75f),
                PoseLandmarks.LEFT_SHOULDER, PoseLandmarks.RIGHT_SHOULDER);

            CalculateHeadNeck();

            CalculateShoulder(HumanBodyBones.RightShoulder,
                PoseLandmarks.RIGHT_SHOULDER, PoseLandmarks.LEFT_SHOULDER, PoseLandmarks.RIGHT_HIP, false);
            CalculateElbow(HumanBodyBones.RightUpperArm,
                PoseLandmarks.RIGHT_SHOULDER, PoseLandmarks.RIGHT_ELBOW, PoseLandmarks.RIGHT_WRIST, Get(PoseLandmarks.RIGHT_ELBOW));
            CalculateForearm(HumanBodyBones.RightLowerArm,
                PoseLandmarks.RIGHT_WRIST, PoseLandmarks.RIGHT_ELBOW, PoseLandmarks.RIGHT_SHOULDER);
            CalculateHand(HumanBodyBones.RightHand,
                PoseLandmarks.RIGHT_WRIST, PoseLandmarks.RIGHT_INDEX, PoseLandmarks.RIGHT_PINKY);

            CalculateUpperLeg(HumanBodyBones.RightUpperLeg,
                PoseLandmarks.RIGHT_HIP, PoseLandmarks.RIGHT_KNEE, PoseLandmarks.RIGHT_ANKLE);
            CalculateLowerLeg(HumanBodyBones.RightLowerLeg,
                PoseLandmarks.RIGHT_HIP, PoseLandmarks.RIGHT_KNEE, PoseLandmarks.RIGHT_ANKLE);
            CalculateFoot(HumanBodyBones.RightFoot,
                PoseLandmarks.RIGHT_HIP, PoseLandmarks.RIGHT_KNEE, PoseLandmarks.RIGHT_HEEL, PoseLandmarks.RIGHT_FOOT);


            // Copied
            CalculateShoulder(HumanBodyBones.LeftShoulder,
                PoseLandmarks.LEFT_SHOULDER, PoseLandmarks.RIGHT_SHOULDER, PoseLandmarks.LEFT_HIP, true);
            CalculateElbow(HumanBodyBones.LeftUpperArm,
                PoseLandmarks.LEFT_SHOULDER, PoseLandmarks.LEFT_ELBOW, PoseLandmarks.LEFT_WRIST, Get(PoseLandmarks.LEFT_ELBOW), true);
            CalculateForearm(HumanBodyBones.LeftLowerArm,
                PoseLandmarks.LEFT_WRIST, PoseLandmarks.LEFT_ELBOW, PoseLandmarks.LEFT_SHOULDER, true);
            CalculateHand(HumanBodyBones.LeftHand,
                PoseLandmarks.LEFT_WRIST, PoseLandmarks.LEFT_INDEX, PoseLandmarks.LEFT_PINKY, true);

            CalculateUpperLeg(HumanBodyBones.LeftUpperLeg,
                PoseLandmarks.LEFT_HIP, PoseLandmarks.LEFT_KNEE, PoseLandmarks.LEFT_ANKLE, true);
            CalculateLowerLeg(HumanBodyBones.LeftLowerLeg,
                PoseLandmarks.LEFT_HIP, PoseLandmarks.LEFT_KNEE, PoseLandmarks.LEFT_ANKLE, true);
            CalculateFoot(HumanBodyBones.LeftFoot,
                PoseLandmarks.LEFT_HIP, PoseLandmarks.LEFT_KNEE, PoseLandmarks.LEFT_HEEL, PoseLandmarks.LEFT_FOOT, true);
        }

        private void CalculateHeadNeck()
        {
            if (UsesUprightBody)
            {
                // Flat face landmarks cannot supply head pitch from a floor camera.
                var upright = puppetPartMap[HumanBodyBones.Chest].BodyPart.rotation;
                SetRotation(HumanBodyBones.Head, upright);
                SetRotation(HumanBodyBones.Neck, upright);
                return;
            }
            Vector3 faceCenter = (Get(PoseLandmarks.LEFT_EAR) + Get(PoseLandmarks.RIGHT_EAR)) / 2f;
            Vector3 faceCenterSurface = (Get(PoseLandmarks.LEFT_EYE_INNER) + Get(PoseLandmarks.RIGHT_EYE_INNER) + Get(PoseLandmarks.NOSE)) / 3f;
            Vector3 rotAxis = (Get(PoseLandmarks.RIGHT_EAR) - Get(PoseLandmarks.LEFT_EAR)).normalized;
            Vector3 faceForward = (faceCenterSurface - faceCenter).normalized;
            Vector3 faceUp = Quaternion.AngleAxis(-90f, rotAxis) * faceForward;
            DrawForward(faceCenter, faceForward);
            DrawUp(faceCenter, faceUp);
            SetRotation(HumanBodyBones.Head, faceForward, faceUp);
            SetRotation(HumanBodyBones.Neck,
                Quaternion.Lerp(puppetPartMap[HumanBodyBones.Chest].BodyPart.rotation, Quaternion.LookRotation(faceForward, faceUp), .8f));
        }
        private bool ArmObserved(PoseLandmarks shoulder, PoseLandmarks elbow, PoseLandmarks wrist)
        {
            var rtm = StabilizedPose;
            return rtm == null || !rtm.isActiveAndEnabled
                || (rtm.IsJointObserved(shoulder) && rtm.IsJointObserved(elbow) && rtm.IsJointObserved(wrist));
        }
        private void CalculateElbow(HumanBodyBones bone, PoseLandmarks shoulder, PoseLandmarks elbow, PoseLandmarks wrist, Vector3 centre, bool special = false)
        {
            if (!ArmObserved(shoulder, elbow, wrist)) return;
            Vector3 armBone = centre;
            Vector3 armForward = (Get(elbow) - Get(shoulder)).normalized;
            Vector3 armRight = (special ? -1 : 1) * CalculateNormal(Get(wrist), ElbowPole(elbow, special), Get(shoulder));
            Vector3 armUp = (special ? -1 : 1) * Vector3.Cross(armForward, armRight);
            DrawForward(armBone, armForward);
            DrawRight(armBone, armRight);
            DrawUp(armBone, armUp);

            SetRotation(bone, -armRight, armUp);
        }
        private void CalculateShoulder(HumanBodyBones bone, PoseLandmarks targetShoulder, PoseLandmarks otherShoulder, PoseLandmarks targetHip, bool special)
        {
            Vector3 upperSpine = (Get(targetShoulder) + Get(otherShoulder)) / 2f;
            Vector3 shoulderBone = Get(targetShoulder);

            Vector3 shoulderForward = (Get(targetShoulder) - upperSpine).normalized;
            Vector3 shoulderRight = (special ? -1 : 1) * CalculateNormal(upperSpine, shoulderBone, Get(targetHip));
            Vector3 shoulderUp = (special ? -1 : 1) * Vector3.Cross(shoulderForward, shoulderRight);
            DrawForward(shoulderBone, shoulderForward);
            DrawRight(shoulderBone, shoulderRight);
            DrawUp(shoulderBone, shoulderUp);

            SetRotation(bone, -shoulderRight, shoulderUp);
        }
        private void CalculateForearm(HumanBodyBones bone, PoseLandmarks wrist, PoseLandmarks elbow, PoseLandmarks shoulder, bool special = false)
        {
            if (!ArmObserved(shoulder, elbow, wrist)) return;
            Vector3 forearmBone = Get(wrist);
            Vector3 forearmForward = (Get(wrist) - Get(elbow)).normalized;
            Vector3 forearmRight = (special ? -1 : 1) * CalculateNormal(Get(wrist), ElbowPole(elbow, special), Get(shoulder));
            Vector3 forearmUp = (special ? -1 : 1) * Vector3.Cross(forearmForward, forearmRight);

            DrawForward(forearmBone, forearmForward);
            DrawRight(forearmBone, forearmRight);
            DrawUp(forearmBone, forearmUp);

            SetRotation(bone, -forearmRight, forearmUp);
        }
        private void CalculateHand(HumanBodyBones bone, PoseLandmarks wrist, PoseLandmarks index, PoseLandmarks pinky, bool left = false)
        {
            var rtm = StabilizedPose;
            if (rtm != null && rtm.isActiveAndEnabled && !rtm.IsJointObserved(wrist)) return;
            Vector3 handBone = Get(wrist);
            Vector3 handForward = (Get(wrist) - (Get(index) + Get(pinky)) / 2f).normalized;
            Vector3 handUp = (left ? -1 : 1) * CalculateNormal(Get(pinky), Get(index), Get(wrist));

            Vector3 handRight = (left ? -1 : 1) * Vector3.Cross(handForward, handUp);
            DrawForward(handBone, handForward);
            DrawRight(handBone, handRight);
            DrawUp(handBone, handUp);

            SetRotation(bone, -handRight, handUp);
        }
        private void CalculateTorso(HumanBodyBones bone, Vector3 leftHip, Vector3 rightHip, PoseLandmarks leftShoulder, PoseLandmarks rightShoulder)
        {
            Vector3 torsoCentre = Helpers.Average(leftHip, rightHip, Get(leftShoulder), Get(rightShoulder));
            Vector3 n1 = CalculateNormal(rightHip, Get(leftShoulder), leftHip);
            Vector3 n2 = CalculateNormal(rightHip, Get(rightShoulder), leftHip);
            Vector3 torsoForward = (n1 + n2) / 2f;
            Vector3 torsoRight = (rightHip - leftHip).normalized;
            Vector3 torsoUp = Vector3.Cross(torsoForward, torsoRight);
            DrawForward(torsoCentre, torsoForward);
            DrawRight(torsoCentre, torsoRight);
            DrawUp(torsoCentre, torsoUp);
            SetRotation(bone, torsoForward, torsoUp);
        }
        private void CalculateUpperLeg(HumanBodyBones bone, PoseLandmarks rightHip, PoseLandmarks rightKnee, PoseLandmarks rightFoot, bool mirror = false)
        {
            Vector3 c = Get(rightHip);
            Vector3 forward = (Knee(rightKnee, mirror) - Get(rightHip)).normalized;
            Vector3 right = CalculateNormal(Get(rightHip), KneePole(rightKnee, mirror), Foot(rightFoot, mirror));
            Vector3 up = Vector3.Cross(forward, right);
            DrawForward(c, forward);
            DrawRight(c, right);
            DrawUp(c, up);
            SetRotation(bone, up, -forward);
        }
        private void CalculateLowerLeg(HumanBodyBones bone, PoseLandmarks rightHip, PoseLandmarks rightKnee, PoseLandmarks rightFoot, bool mirror = false)
        {
            Vector3 c = Get(rightKnee);
            Vector3 forward = (Foot(rightFoot, mirror) - Knee(rightKnee, mirror)).normalized;
            Vector3 right = CalculateNormal(Get(rightHip), KneePole(rightKnee, mirror), Foot(rightFoot, mirror));
            Vector3 up = Vector3.Cross(forward, right);
            DrawForward(c, forward);
            DrawRight(c, right);
            DrawUp(c, up);
            SetRotation(bone, up, -forward);
        }
        private void CalculateFoot(HumanBodyBones bone, PoseLandmarks rightHip, PoseLandmarks rightKnee, PoseLandmarks rightFoot, PoseLandmarks rightToes, bool mirror = false)
        {
            Vector3 c = Get(rightKnee);
            Vector3 forward = (Foot(rightToes, mirror) - Foot(rightFoot, mirror)).normalized;
            Vector3 right = CalculateNormal(Get(rightHip), KneePole(rightKnee, mirror), Foot(rightFoot, mirror));
            Vector3 up = Vector3.Cross(forward, right);
            DrawForward(c, forward);
            DrawRight(c, right);
            DrawUp(c, up);
            SetRotation(bone, forward, up);
        }

        private Vector3 KneePole(PoseLandmarks knee, bool left)
        {
            if (!kneePoleConfiguration.usePole) return Get(knee);

            return Get(knee) + GetForward(HumanBodyBones.Hips) * kneePoleConfiguration.forwardDistance
                + GetRight(HumanBodyBones.Hips) * kneePoleConfiguration.lateralDistance * (left ? -1 : 1);
        }
        private Vector3 Knee(PoseLandmarks knee, bool left)
        {
            return Get(knee) + GetRight(HumanBodyBones.Hips) * kneeSpacingBias * (left ? -1 : 1);
        }
        private Vector3 Foot(PoseLandmarks foot, bool left)
        {
            return Get(foot) + GetRight(HumanBodyBones.Hips) * kneeSpacingBias * .6f * (left ? -1 : 1);
        }
        private Vector3 ElbowPole(PoseLandmarks elbow, bool left)
        {
            if (!elbowPoleConfiguration.usePole) return Get(elbow);

            return Get(elbow) - GetForward(HumanBodyBones.Hips) * elbowPoleConfiguration.forwardDistance
                + GetRight(HumanBodyBones.Hips) * elbowPoleConfiguration.lateralDistance * (left ? -1 : 1);
        }

        protected Vector3 Get(PoseLandmarks index)
        {
            if (stabilizeLegs || responsiveLegs)
                for (int i = 0; i < legPointIndices.Length; i++)
                    if (index == legPointIndices[i] && legInitialized[i / 4])
                        return measuredHips[i / 4] + acceptedLegPoints[i];
            return GetRaw(index);
        }

        private void UpdateLegSamples()
        {
            var rtm = (poseProvider.Provider as PoseSolution)?.ActiveRTMPose;
            responsiveLegs = rtm != null && rtm.isActiveAndEnabled;
            if (responsiveLegs) { UpdateResponsiveLegs(rtm); return; }
            if (!stabilizeLegs) { legInitialized[0] = legInitialized[1] = false; return; }
            if (Time.unscaledTime - lastLegSample > .35f || legSampleMirror != settings.Mirror)
                legInitialized[0] = legInitialized[1] = false;
            legSampleMirror = settings.Mirror;
            lastLegSample = Time.unscaledTime;
            for (int side = 0; side < 2; side++)
            {
                measuredHips[side] = GetRaw(side == 0 ? PoseLandmarks.LEFT_HIP : PoseLandmarks.RIGHT_HIP);
                int start = side * 4;
                // A missing leg is not a held kick. Supply a relaxed standing leg
                // even on the very first frame, when there is no accepted pose yet.
                float confidenceThreshold = legInitialized[side] && !legFallback[side] ? .35f : .5f;
                bool visible = ResponsiveValid(side == 0 ? PoseLandmarks.LEFT_HIP : PoseLandmarks.RIGHT_HIP, confidenceThreshold);
                // Toe occlusion affects foot orientation, not the entire knee/ankle chain.
                for (int i = start; i < start + 2; i++)
                {
                    var index = settings.Mirror ? legPointIndices[i].Flip() : legPointIndices[i];
                    float confidence = poseProvider.Get(0, index).Visibility;
                    var point = GetRaw(legPointIndices[i]);
                    visible &= confidence >= confidenceThreshold && LegMotionDeadband.IsFinite(point);
                }
                legMeasuredThisFrame[side] = visible;
                if (!visible)
                {
                    // Brief confidence dips hold the display pose, but cannot score a hit.
                    if (legInitialized[side] && !legFallback[side] && Time.unscaledTime - legLastSeen[side] <= .1f) continue;
                    Vector3 shoulder = GetRaw(side == 0 ? PoseLandmarks.LEFT_SHOULDER : PoseLandmarks.RIGHT_SHOULDER);
                    Vector3 torso = shoulder - measuredHips[side];
                    Vector3 up = torso.y >= 0 ? Vector3.up : Vector3.down;
                    Vector3 across = GetRaw(PoseLandmarks.RIGHT_HIP) - GetRaw(PoseLandmarks.LEFT_HIP);
                    Vector3 forward = Vector3.Cross(across, up).normalized;
                    if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
                    float standingLength = LegMotionDeadband.IsFinite(torso) ? Mathf.Clamp(torso.magnitude * 1.8f, .3f, 1.4f) : .8f;
                    LegMotionDeadband.StandingFallback(acceptedLegPoints, start, standingLength, up, forward);
                    legInitialized[side] = legFallback[side] = true;
                    continue;
                }
                legLastSeen[side] = Time.unscaledTime;
                for (int i = start; i < start + 4; i++)
                {
                    if (i < start + 2 || ResponsiveValid(legPointIndices[i], confidenceThreshold))
                        measuredLegPoints[i] = GetRaw(legPointIndices[i]) - measuredHips[side];
                    else
                    {
                        var offset = legInitialized[side] ? acceptedLegPoints[i] - acceptedLegPoints[start + 1]
                            : Vector3.forward * (i == start + 2 ? -.05f : .14f);
                        measuredLegPoints[i] = measuredLegPoints[start + 1] + Vector3.ClampMagnitude(offset, .25f);
                    }
                }
                float length = measuredLegPoints[start].magnitude
                    + (measuredLegPoints[start + 1] - measuredLegPoints[start]).magnitude;
                float threshold = Mathf.Max(.001f, length) * legMovementThreshold;
                // Compare against the last accepted pose, not the previous frame:
                // slow deliberate motion accumulates; a fast kick has no smoothing delay.
                if (LegMotionDeadband.Filter(measuredLegPoints, acceptedLegPoints, start, threshold, legInitialized[side] && !legFallback[side]))
                {
                    legInitialized[side] = true;
                }
                legFallback[side] = false;
            }
        }

        private void UpdateResponsiveLegs(FitFight.BodyTracking.RTMPosePoseProvider rtm)
        {
            if (Time.unscaledTime - lastLegSample > .2f || legSampleMirror != settings.Mirror)
                legInitialized[0] = legInitialized[1] = false;
            legSampleMirror = settings.Mirror; lastLegSample = Time.unscaledTime;
            for (int side = 0; side < 2; side++)
            {
                int start = side * 4;
                var hip = side == 0 ? PoseLandmarks.LEFT_HIP : PoseLandmarks.RIGHT_HIP;
                measuredHips[side] = GetRaw(hip);
                // Toes/heels control foot orientation only. They cannot veto a knee or ankle.
                bool visible = ResponsiveValid(hip, rtm.Tuning.jointConfidence)
                    && ResponsiveValid(legPointIndices[start], rtm.Tuning.jointConfidence)
                    && ResponsiveValid(legPointIndices[start + 1], rtm.Tuning.jointConfidence);
                legFallback[side] = !visible;
                if (!visible) continue; // The guard uses this per-leg flag after the short prediction grace.
                for (int i = 0; i < 2; i++) measuredLegPoints[start + i] = GetRaw(legPointIndices[start + i]) - measuredHips[side];
                var ankle = measuredLegPoints[start + 1];
                for (int i = 2; i < 4; i++)
                {
                    var landmark = legPointIndices[start + i];
                    if (ResponsiveValid(landmark, rtm.Tuning.jointConfidence))
                        measuredLegPoints[start + i] = GetRaw(landmark) - measuredHips[side];
                    else
                    {
                        // Carry a last-known orientation with the moving ankle; never pin it in world space.
                        Vector3 offset = legInitialized[side]
                            ? acceptedLegPoints[start + i] - acceptedLegPoints[start + 1]
                            : Vector3.forward * (i == 2 ? -.05f : .14f);
                        measuredLegPoints[start + i] = ankle + Vector3.ClampMagnitude(offset, .25f);
                    }
                }
                float length = measuredLegPoints[start].magnitude + (ankle - measuredLegPoints[start]).magnitude;
                LegMotionDeadband.Filter(measuredLegPoints, acceptedLegPoints, start,
                    length * rtm.Tuning.legDeadband, legInitialized[side]);
                legInitialized[side] = true;
            }
        }
        private bool ResponsiveValid(PoseLandmarks index, float threshold)
        {
            var actual = settings.Mirror ? index.Flip() : index;
            var sample = poseProvider.Get(0, actual);
            return sample.Visibility >= threshold && LegMotionDeadband.IsFinite(sample.Position);
        }

        private Vector3 GetRaw(PoseLandmarks index)
        {
            if (settings.Mirror)
                return ((IProvider<PoseLandmarks, Landmark>)poseProvider).Get(0, index.Flip()).Position;

            return ((IProvider<PoseLandmarks, Landmark>)poseProvider).Get(0, index).Position;
        }

        public PoseJoint Get(int group, PoseJoints index)
        {
            if (settings.Mirror) return joints.Get(System.Convert.ToInt32(index.Flip()));

            return joints.Get(System.Convert.ToInt32(index));
        }
        public PoseJoint Get(int group, int index)
        {
            return Get(group, (PoseJoints)index);
        }
        public PoseJoint GetAbsoluteJoint(int group, PoseJoints indexer)
        {
            // Get, but absolutely relative to the puppet
            return joints.Get((int)indexer);
        }

        protected override void ConstructJoint(PoseJoints joint, HumanBodyBones bone)
        {
            //if (settings.Mirror) joint = joint.Flip();

            joints.Get((int)joint).Reconstruct(bone, puppetPartMap[bone].BodyPart);
        }

        public override Dictionary<PoseJoints, HumanBodyBones> HumanMapping => mapping;
        public static readonly Dictionary<PoseJoints, HumanBodyBones> mapping = new Dictionary<PoseJoints, HumanBodyBones>()
        {
            { PoseJoints.Hips, HumanBodyBones.Hips },
            { PoseJoints.Spine, HumanBodyBones.Spine },
            { PoseJoints.Chest, HumanBodyBones.Chest },
            { PoseJoints.Neck, HumanBodyBones.Neck },
            { PoseJoints.Head, HumanBodyBones.Head },

            { PoseJoints.RightShoulder, HumanBodyBones.RightShoulder },
            { PoseJoints.RightUpperArm, HumanBodyBones.RightUpperArm },
            { PoseJoints.RightLowerArm, HumanBodyBones.RightLowerArm },
            { PoseJoints.RightHand, HumanBodyBones.RightHand },
            { PoseJoints.RightUpperLeg, HumanBodyBones.RightUpperLeg },
            { PoseJoints.RightLowerLeg, HumanBodyBones.RightLowerLeg },
            { PoseJoints.RightFoot, HumanBodyBones.RightFoot },

            { PoseJoints.LeftShoulder, HumanBodyBones.LeftShoulder },
            { PoseJoints.LeftUpperArm, HumanBodyBones.LeftUpperArm },
            { PoseJoints.LeftLowerArm, HumanBodyBones.LeftLowerArm },
            { PoseJoints.LeftHand, HumanBodyBones.LeftHand },
            { PoseJoints.LeftUpperLeg, HumanBodyBones.LeftUpperLeg },
            { PoseJoints.LeftLowerLeg, HumanBodyBones.LeftLowerLeg },
            { PoseJoints.LeftFoot, HumanBodyBones.LeftFoot }
        };

        public override Dictionary<HumanBodyBones, PoseJoints> JointMapping => reversedMapping;

        public IProviderLifecycle Lifecycle => this;

        public override int GroupSize => 1;

        public static readonly Dictionary<HumanBodyBones, PoseJoints> reversedMapping = mapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        protected override List<PuppetBodyPart> GetPartsRange()
        {
            List<PuppetBodyPart> range = new List<PuppetBodyPart>();
            range.AddRange(centreBodyParts);
            range.AddRange(rightBodyParts);
            range.AddRange(leftBodyParts);
            return range;
        }

        protected void ConstructJoint(PoseJoint joint, HumanBodyBones bone)
        {
            joints.Get(System.Convert.ToInt32(bone))
                .Reconstruct(bone, puppetPartMap[bone].BodyPart);
        }

        [System.Serializable]
        public class PoleConfiguration
        {
            [Tooltip("Whether or not to use the pole to orientate the knees, if false this is legacy behavior.")]
            public bool usePole = true;
            [Tooltip("The forward distance from the joint to place the pole.")]
            public float forwardDistance = 0.2f;
            [Tooltip("The right/left distance from the joint to place the pole.")]
            public float lateralDistance = 0.2f;
        }
    }
}
