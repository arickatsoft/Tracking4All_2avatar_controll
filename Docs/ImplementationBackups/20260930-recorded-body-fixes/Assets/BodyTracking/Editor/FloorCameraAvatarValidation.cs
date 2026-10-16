using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Editor-only synthetic camera replay through the actual provider and both avatar drivers.</summary>
public static class FloorCameraAvatarValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [MenuItem("Tools/FitFight/RTMPose/Validate floor camera avatar")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Start practice in Play mode first.");
        var owner = Object.FindFirstObjectByType<PlayerSelectionManager>();
        owner.StartCoroutine(Replay());
    }
    private static IEnumerator Replay()
    {
        var controller = Object.FindFirstObjectByType<BodyTrackingController>();
        var provider = controller.RTMPose;
        Tracking4All.Avatar avatar = null;
        foreach (var item in Object.FindObjectsByType<Tracking4All.Avatar>(FindObjectsSortMode.None))
            if (item.PosePuppet != null && item.PosePuppet.PoseSource == controller.gameObject) { avatar = item; break; }
        if (avatar == null) throw new InvalidOperationException("Local tracked avatar missing.");
        var skeleton = avatar.GetComponent<Tracking4All.AvatarSkeleton>();
        var animator = avatar.GetComponent<Animator>();
        var grounding = avatar.GetComponent<FitFightFighterGrounding>();
        if (provider == null || !provider.isActiveAndEnabled || grounding == null)
            throw new InvalidOperationException("Select RTMPose and enter practice first.");
        var report = new StringBuilder(); bool complete = false;
        var type = typeof(RTMPosePoseProvider);
        var workerField = type.GetField("worker", Private);
        var worker = workerField.GetValue(provider);
        var output = (Vector3[])type.GetField("solved", Private).GetValue(provider);
        var outputScores = (float[])type.GetField("solvedScores", Private).GetValue(provider);
        bool previousDirect = provider.Tuning.directAvatar, previousUpright = provider.Tuning.uprightBody;
        var punches = Object.FindFirstObjectByType<FitFightPracticeMode>()?.Punches;
        bool wasAssisted = punches != null && punches.Active;
        if (wasAssisted) punches.SetAssistance(false);
        var banner = new GameObject("Floor camera simulation label", typeof(Canvas), typeof(CanvasScaler));
        banner.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        banner.GetComponent<Canvas>().sortingOrder = 32000;
        var label = new GameObject("Simulation", typeof(RectTransform), typeof(Image)); label.transform.SetParent(banner.transform, false);
        label.GetComponent<Image>().color = new Color(.05f, .2f, .1f, .95f);
        var rect = label.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.1f, .82f); rect.anchorMax = new Vector2(.9f, .90f); rect.offsetMin = rect.offsetMax = Vector2.zero;
        var textObject = new GameObject("Text", typeof(Text)); textObject.transform.SetParent(label.transform, false);
        var textRect = textObject.GetComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        var labelText = textObject.GetComponent<Text>(); labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.alignment = TextAnchor.MiddleCenter; labelText.resizeTextForBestFit = true; labelText.resizeTextMinSize = 9; labelText.resizeTextMaxSize = 24;
        Directory.CreateDirectory("Logs/Practice");
        try
        {
            // Pause inference only while this labelled editor simulation supplies observations.
            workerField.SetValue(provider, null); provider.Tuning.uprightBody = true;
            var scores = new float[26]; for (int i = 0; i < scores.Length; i++) scores[i] = .9f;
            foreach (bool direct in new[] { false, true })
            {
                controller.SetDirectAvatar(direct);
                var solver = (Skeleton25DSolver)type.GetField("solver", Private).GetValue(provider);
                solver.Reset();
                var tuning = new RTMPoseTuning { filtering = false, prediction = 0 };
                var standing = RTMPoseValidation.FloorFixture(40);
                for (int i = 0; i < 25; i++) solver.Solve(standing, scores, .5625f, i / 30.0, 1.7f, 40, 0, output, outputScores, tuning);
                Vector3 stanceFoot = Vector3.zero, stanceHand = Vector3.zero;
                float upperLength = 0, lowerLength = 0, standingHip = 0;
                int sample = 25;
                foreach (string action in new[] { "standing", "kick", "squat", "bend", "head" })
                {
                    bool kick = action == "kick";
                    string name = (direct ? "direct" : "puppet") + "-" + action;
                    labelText.text = "SIMULATED 40° FLOOR CAMERA\n" + name;
                    for (int frame = 0; frame < 12; frame++)
                    {
                        solver.Solve(RTMPoseValidation.FloorFixture(40, kick, kick,
                            squat: action == "squat" ? .3f : 0, bend: action == "bend" ? 70 : 0,
                            headTurn: action == "head" ? 40 : 0), scores, .5625f,
                            sample++ / 30.0, 1.7f, 40, 0, output, outputScores, tuning);
                        double now = Time.realtimeSinceStartupAsDouble;
                        type.GetField("lastPoseAt", Private).SetValue(provider, now);
                        type.GetField("lastReceivedAt", Private).SetValue(provider, now);
                        type.GetMethod("Publish", Private).Invoke(provider, new object[] { now });
                        yield return null;
                    }
                    yield return null; // Explicit camera render below also works when Game view is not focused.
                    Quaternion bindFrame = avatar.transform.rotation * Quaternion.Inverse(skeleton.BindingRootRotation);
                    if (action == "standing" || action == "kick" || action == "squat")
                        foreach (var bone in new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest })
                            if (skeleton.HasBone(bone)) Check(Quaternion.Angle(skeleton.GetTransform(bone).rotation,
                                bindFrame * skeleton.GetBindingRotation(bone)) < 3, name + ": stable " + bone, report);
                    if (action == "bend" || action == "head")
                    {
                        var bone = action == "bend" ? HumanBodyBones.Chest : HumanBodyBones.Head;
                        float angle = Quaternion.Angle(skeleton.GetTransform(bone).rotation, bindFrame * skeleton.GetBindingRotation(bone));
                        Check(angle > 8 && angle < 80, name + ": visible body/head rotation " + angle.ToString("F1"), report);
                    }
                    Vector3 foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                    Vector3 hand = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                    float upper = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position,
                        animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position);
                    float lower = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position, foot);
                    if (action == "standing") { stanceFoot = foot; stanceHand = hand; upperLength = upper; lowerLength = lower;
                        standingHip = animator.GetBoneTransform(HumanBodyBones.Hips).position.y; }
                    if (action == "squat") Check(animator.GetBoneTransform(HumanBodyBones.Hips).position.y < standingHip - .12f,
                        name + ": visible pelvis lowers for squat", report);
                    if (kick)
                    {
                        Check(foot.y > stanceFoot.y + .2f, name + ": visible kicking foot rises", report);
                        Check(Vector3.Distance(hand, stanceHand) > .1f, name + ": visible punching hand moves", report);
                        Check(Mathf.Abs(upper - upperLength) < .001f && Mathf.Abs(lower - lowerLength) < .001f,
                            name + ": rig leg lengths preserved", report);
                    }
                    float lowerFoot = Mathf.Min(foot.y, animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
                    Check(lowerFoot >= grounding.FloorY && lowerFoot - grounding.FloorY < .18f,
                        name + ": supporting ankle remains at shoe height above floor", report);
                    report.AppendLine($"{name}: ankleY={foot.y:F3}, supportY={lowerFoot:F3}, floor={grounding.FloorY:F3}");
                    var camera = GameObject.Find("Practice Camera").GetComponent<Camera>();
                    var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
                    var target = new RenderTexture(544, 900, 24);
                    var capture = new Texture2D(544, 900, TextureFormat.RGB24, false);
                    try
                    {
                        camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                        capture.ReadPixels(new Rect(0, 0, 544, 900), 0, 0); capture.Apply();
                        File.WriteAllBytes("Logs/Practice/floor-camera-" + name + ".png", capture.EncodeToPNG());
                    }
                    finally { camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                        target.Release(); Object.Destroy(target); Object.Destroy(capture); }
                }
            }
            complete = true;
        }
        finally
        {
            workerField.SetValue(provider, worker); provider.Tuning.uprightBody = previousUpright;
            controller.SetDirectAvatar(previousDirect); provider.ResetCalibration(); Object.Destroy(banner);
            if (wasAssisted && punches != null) punches.SetAssistance(true);
            report.AppendLine(complete ? "PASS: both avatar paths and floor contact." : "FAIL: replay interrupted; see Unity Console.");
            File.WriteAllText("Logs/Practice/floor-camera-avatar-validation.txt", report.ToString());
        }
    }
    private static void Check(bool ok, string message, StringBuilder report)
    {
        report.AppendLine((ok ? "PASS: " : "FAIL: ") + message);
        if (!ok) throw new InvalidOperationException(message);
    }
}
