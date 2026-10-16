using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Use the authored fighting idle while no fresh tracked pose is available.</summary>
[DefaultExecutionOrder(9000)]
public sealed class FitFightGuardPose : MonoBehaviour
{
    private Animator animator;
    private Tracking4All.Avatar tracking;
        private PlayableGraph graph;
    private Vector3 stagePosition;
    private Quaternion stageRotation;
    private bool previousRootMotion;
    private Transform hips, leftShoulder, rightShoulder;
    private float facingSign = 1f;
    private Quaternion leftStandingFoot, rightStandingFoot;
    private float standingAnkleY;
    private Transform[] displayBones;
    private HumanBodyBones[] displayKinds;
    private Quaternion[] lastDisplay, nextDisplay;
    private bool haveDisplay;
    private readonly System.Collections.Generic.List<Transform> restoredBones = new System.Collections.Generic.List<Transform>();
    private readonly System.Collections.Generic.List<Quaternion> restoredRotations = new System.Collections.Generic.List<Quaternion>();
    public void SetStageAnchor()
    {
        stagePosition = transform.position;
        stageRotation = transform.rotation;
    }
    private void Start()
    {
        animator = GetComponent<Animator>();
        tracking = GetComponent<Tracking4All.Avatar>();
        SetStageAnchor();
        if (animator != null) { previousRootMotion = animator.applyRootMotion; animator.applyRootMotion = false; }
        var art = Resources.Load<FitFightPoseReference>("FitFightPoseReference");
        if (animator == null || !animator.isHuman || art == null || art.guard == null) return;
        var kinds = new System.Collections.Generic.List<HumanBodyBones>();
        for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            if (animator.GetBoneTransform((HumanBodyBones)i) != null) kinds.Add((HumanBodyBones)i);
        kinds.Sort((a, b) => BoneDepth(animator.GetBoneTransform(a)).CompareTo(BoneDepth(animator.GetBoneTransform(b))));
        displayKinds = kinds.ToArray(); displayBones = new Transform[kinds.Count];
        lastDisplay = new Quaternion[kinds.Count]; nextDisplay = new Quaternion[kinds.Count];
        for (int i = 0; i < kinds.Count; i++) displayBones[i] = animator.GetBoneTransform(kinds[i]);
        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        leftStandingFoot = Quaternion.Inverse(transform.rotation) * animator.GetBoneTransform(HumanBodyBones.LeftFoot).rotation;
        rightStandingFoot = Quaternion.Inverse(transform.rotation) * animator.GetBoneTransform(HumanBodyBones.RightFoot).rotation;
        leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        if (leftShoulder != null && rightShoulder != null)
            facingSign = Vector3.Dot(Vector3.Cross(rightShoulder.position - leftShoulder.position, Vector3.up), transform.forward) < 0 ? -1 : 1;
        graph = PlayableGraph.Create("FitFight untracked guard");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var clip = AnimationClipPlayable.Create(graph, art.guard);
        var output = AnimationPlayableOutput.Create(graph, "Guard", animator);
        output.SetSourcePlayable(clip);
    }
    private void LateUpdate()
    {
        if (!graph.IsValid() || (tracking != null && tracking.HasPoseOverride)) return;
        bool live = tracking != null && tracking.HasFreshPose;
        bool leftLive = live && tracking.HasReliableLeg(true), rightLive = live && tracking.HasReliableLeg(false);
        bool leftArmLive = live && ArmVisible(true), rightArmLive = live && ArmVisible(false);
        if (live) SetStageAnchor();
        if (leftLive && rightLive && leftArmLive && rightArmLive) { RememberDisplay(); return; }
        restoredBones.Clear(); restoredRotations.Clear();
        if (live)
        {
            // Retarget only measured body parts. Unseen legs and pelvis use the
            // authored planted stance, not inferred tilted/airborne geometry.
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var kind = (HumanBodyBones)i;
                bool left = kind == HumanBodyBones.LeftUpperLeg || kind == HumanBodyBones.LeftLowerLeg || kind == HumanBodyBones.LeftFoot || kind == HumanBodyBones.LeftToes;
                bool right = kind == HumanBodyBones.RightUpperLeg || kind == HumanBodyBones.RightLowerLeg || kind == HumanBodyBones.RightFoot || kind == HumanBodyBones.RightToes;
                if (kind == HumanBodyBones.Hips || (left && !leftLive) || (right && !rightLive)) continue;
                var bone = animator.GetBoneTransform(kind);
                if (bone != null && ((!leftArmLive && (bone == leftShoulder || bone.IsChildOf(leftShoulder)))
                    || (!rightArmLive && (bone == rightShoulder || bone.IsChildOf(rightShoulder))))) continue;
                if (bone != null) { restoredBones.Add(bone); restoredRotations.Add(bone.rotation); }
            }
            // Restore parents before children; enum order does not guarantee this.
            for (int i = 1; i < restoredBones.Count; i++)
                for (int j = i; j > 0 && BoneDepth(restoredBones[j - 1]) > BoneDepth(restoredBones[j]); j--)
                {
                    var b = restoredBones[j]; restoredBones[j] = restoredBones[j - 1]; restoredBones[j - 1] = b;
                    var r = restoredRotations[j]; restoredRotations[j] = restoredRotations[j - 1]; restoredRotations[j - 1] = r;
                }
        }
        graph.Evaluate(Time.unscaledDeltaTime);
        transform.SetPositionAndRotation(stagePosition, stageRotation);
        // The imported guard has its own facing direction. Align its torso with
        // the staged opponent; leave live tracked torso rotation untouched.
        if (hips != null && leftShoulder != null && rightShoulder != null)
        {
            Vector3 forward = Vector3.Cross(rightShoulder.position - leftShoulder.position, Vector3.up) * facingSign;
            if (forward.sqrMagnitude > .001f)
                hips.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(forward, transform.forward, Vector3.up), Vector3.up) * hips.rotation;
        }
        standingAnkleY = Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,
            animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
        RefineLeg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
        RefineLeg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
        var skeleton = live ? GetComponent<Tracking4All.AvatarSkeleton>() : null;
        if (skeleton != null && skeleton.UsesUprightBody) skeleton.ApplyStabilizedBody();
        for (int i = 0; i < restoredBones.Count; i++) restoredBones[i].rotation = restoredRotations[i];
        // Smooth only the fallback transition. Measured movement keeps its full response.
        if (haveDisplay)
        {
            for (int i = 0; i < displayBones.Length; i++) nextDisplay[i] = displayBones[i].rotation;
            float blend = 1 - Mathf.Exp(-Time.unscaledDeltaTime / .08f);
            for (int i = 0; i < displayBones.Length; i++)
            {
                var kind = displayKinds[i];
                bool left = kind == HumanBodyBones.LeftUpperLeg || kind == HumanBodyBones.LeftLowerLeg || kind == HumanBodyBones.LeftFoot || kind == HumanBodyBones.LeftToes;
                bool right = kind == HumanBodyBones.RightUpperLeg || kind == HumanBodyBones.RightLowerLeg || kind == HumanBodyBones.RightFoot || kind == HumanBodyBones.RightToes;
                bool fallback = !live || (left && !leftLive) || (right && !rightLive)
                    || (!leftArmLive && (displayBones[i] == leftShoulder || displayBones[i].IsChildOf(leftShoulder)))
                    || (!rightArmLive && (displayBones[i] == rightShoulder || displayBones[i].IsChildOf(rightShoulder)));
                displayBones[i].rotation = fallback ? Quaternion.Slerp(lastDisplay[i], nextDisplay[i], blend) : nextDisplay[i];
            }
        }
        RememberDisplay();
    }
    private bool ArmVisible(bool left)
    {
        var pose = tracking?.PosePuppet?.StabilizedPose;
        if (pose == null || !pose.isActiveAndEnabled) return true;
        var shoulder = pose.AvatarLandmark(left ? Tracking4All.PoseLandmarks.LEFT_SHOULDER : Tracking4All.PoseLandmarks.RIGHT_SHOULDER);
        var elbow = pose.AvatarLandmark(left ? Tracking4All.PoseLandmarks.LEFT_ELBOW : Tracking4All.PoseLandmarks.RIGHT_ELBOW);
        var wrist = pose.AvatarLandmark(left ? Tracking4All.PoseLandmarks.LEFT_WRIST : Tracking4All.PoseLandmarks.RIGHT_WRIST);
        return Mathf.Min(shoulder.Visibility, elbow.Visibility, wrist.Visibility) >= pose.Tuning.jointConfidence;
    }
    private void RememberDisplay()
    {
        if (displayBones == null) return;
        for (int i = 0; i < displayBones.Length; i++) lastDisplay[i] = displayBones[i].rotation;
        haveDisplay = true;
    }
    private static int BoneDepth(Transform bone)
    {
        int depth = 0;
        for (var parent = bone.parent; parent != null; parent = parent.parent) depth++;
        return depth;
    }
    // Narrow only the authored idle. Live camera poses bypass this entire path.
    private void RefineLeg(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones footBone)
    {
        var thigh = animator.GetBoneTransform(upper);
        var knee = animator.GetBoneTransform(lower);
        var foot = animator.GetBoneTransform(footBone);
        if (thigh == null || knee == null || foot == null) return;
        Vector3 origin = thigh.position, originalFoot = foot.position;
        Quaternion footRotation = stageRotation * (footBone == HumanBodyBones.LeftFoot ? leftStandingFoot : rightStandingFoot);
        Vector3 local = transform.InverseTransformPoint(originalFoot);
        float hipX = transform.InverseTransformPoint(hips.position).x;
        local.x = hipX + (local.x - hipX) * .76f;
        Vector3 target = transform.TransformPoint(local);
        target.y = standingAnkleY;
        float a = Vector3.Distance(origin, knee.position), b = Vector3.Distance(knee.position, originalFoot);
        Vector3 axis = (target - origin).normalized;
        float distance = Mathf.Clamp(Vector3.Distance(origin, target), Mathf.Abs(a - b) + .001f, a + b - .001f);
        Vector3 pole = Vector3.ProjectOnPlane(knee.position - origin, axis).normalized;
        if (pole.sqrMagnitude < .01f) pole = Vector3.ProjectOnPlane(transform.forward, axis).normalized;
        float along = (a * a - b * b + distance * distance) / (2f * distance);
        Vector3 bend = origin + axis * along + pole * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
        thigh.rotation = Quaternion.FromToRotation(knee.position - origin, bend - origin) * thigh.rotation;
        knee.rotation = Quaternion.FromToRotation(foot.position - knee.position, origin + axis * distance - knee.position) * knee.rotation;
        foot.rotation = footRotation;
    }
    private void OnDestroy() { if (graph.IsValid()) graph.Destroy(); if (animator != null) animator.applyRootMotion = previousRootMotion; }
}
