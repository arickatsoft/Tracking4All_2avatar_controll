using System;
using System.Collections;
using System.IO;
using System.Text;
using FitFight.BodyTracking;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PracticeValidation
{
    private static double pumpUntil;
    private static void Pump()
    {
        if (EditorApplication.timeSinceStartup > pumpUntil) { EditorApplication.update -= Pump; return; }
        EditorApplication.QueuePlayerLoopUpdate();
    }
    private static void KeepSimulationRunning()
    {
        Application.runInBackground = true;
        pumpUntil = EditorApplication.timeSinceStartup + 20;
        EditorApplication.update -= Pump; EditorApplication.update += Pump;
        var gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
    }
    [MenuItem("Tools/FitFight/Practice/Enter via home button")]
    public static void Enter()
    {
        KeepSimulationRunning();
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var home = Object.FindFirstObjectByType<FitFightSelectionView>();
        if (home != null) home.PracticeButton.onClick.Invoke();
    }
    [MenuItem("Tools/FitFight/Practice/Report framing")]
    public static void Report()
    {
        var report = new StringBuilder();
        report.AppendLine($"VIEWPORT {Screen.width}x{Screen.height}, safe={Screen.safeArea}");
        foreach (var device in WebCamTexture.devices) report.AppendLine($"CAMERA DEVICE {device.name}, front={device.isFrontFacing}");
        var hud = GameObject.Find("Practice HUD");
        if (hud != null)
        {
            var rect = hud.transform.Find("Safe area") as RectTransform;
            report.AppendLine($"HUD active={hud.activeInHierarchy}, anchors={rect.anchorMin}..{rect.anchorMax}");
        }
        foreach (var avatar in Object.FindObjectsByType<Tracking4All.Avatar>(FindObjectsSortMode.None))
        {
            var animator = avatar.GetComponent<Animator>();
            report.AppendLine($"{avatar.name}: position={avatar.transform.position}, scale={avatar.transform.lossyScale}, height={FitFightPracticeMode.StandingHeight(animator)}");
            foreach (var bone in new[] { HumanBodyBones.Head, HumanBodyBones.Hips, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
                report.AppendLine($"{bone}: {animator.GetBoneTransform(bone).position}");
        }
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            report.AppendLine($"{camera.name}: enabled={camera.enabled}, position={camera.transform.position}, rotation={camera.transform.eulerAngles}, fov={camera.fieldOfView}, aspect={camera.aspect}");
        foreach (var control in Object.FindObjectsByType<BodyTrackingController>(FindObjectsSortMode.None))
            report.AppendLine($"TRACKER {control.name}: {control.Status}, {control.RetargetingStatus}, camera={control.CameraFPS:F1}, inference={control.RTMPose?.InferenceFPS:F1}, pose={control.RTMPose?.PoseFPS:F1}, age={control.RTMPose?.ObservationAgeMs:F1}, sequence={control.RTMPose?.PoseSequence}, camera_to_pose_ms={control.RTMPose?.CameraToPoseMs:F1}, calibration={control.RTMPose?.Calibrated}, pipeline={control.RTMPose?.PipelineStatus}");
        File.WriteAllText("Logs/Practice/framing.txt", report.ToString());
    }

    [MenuItem("Tools/FitFight/Practice/Capture game view")]
    public static void Capture()
    {
        KeepSimulationRunning();
        if (!EditorApplication.isPlaying || Object.FindFirstObjectByType<FitFightPracticeMode>()?.Ready != true) return;
        Directory.CreateDirectory("Logs/Practice");
        ScreenCapture.CaptureScreenshot("Logs/Practice/game-view.png");
    }
    [MenuItem("Tools/FitFight/Practice/Select RTMPose")]
    public static void SelectRTM() { BodyTrackingSelection.Select(BodyTrackingSystem.RTMPose); }
    [MenuItem("Tools/FitFight/Practice/Select Tracking4All")]
    public static void SelectBaseline() { BodyTrackingSelection.Select(BodyTrackingSystem.Tracking4All); }
    [MenuItem("Tools/FitFight/Practice/Toggle direct avatar")]
    public static void ToggleDirect()
    {
        foreach (var control in Object.FindObjectsByType<BodyTrackingController>(FindObjectsSortMode.None))
            if (control.RTMPose != null) control.SetDirectAvatar(!control.RTMPose.Tuning.directAvatar);
    }
    [MenuItem("Tools/FitFight/Practice/Validate return and reenter")]
    public static void RoundTrip()
    {
        KeepSimulationRunning();
        var owner = Object.FindFirstObjectByType<PlayerSelectionManager>();
        owner.StartCoroutine(CheckRoundTrip(owner));
    }
    private static IEnumerator CheckRoundTrip(PlayerSelectionManager owner)
    {
        var report = new StringBuilder();
        var target = Object.FindFirstObjectByType<Tracking4All.Avatar>();
        var controller = Object.FindFirstObjectByType<BodyTrackingController>();
        if (controller != null && controller.RTMPose != null)
        {
            controller.SetDirectAvatar(true);
            yield return null;
            Check(controller.DirectDriver != null && controller.DirectDriver.Target == target,
                "Direct avatar binds to the local fighter", report);
            Check(target.PosePuppet.RetargetingBypassed, "Direct mode bypasses puppet retargeting", report);
            controller.SetDirectAvatar(false);
            Check(target.DirectTracking == null && !target.PosePuppet.RetargetingBypassed,
                "Returning to puppet restores its driver", report);
        }
        owner.ShowPlayerSelection();
        yield return null;
        Check(Object.FindFirstObjectByType<FitFightPracticeMode>() == null, "Home releases practice camera and stage", report);
        Check(target.transform.localScale == Vector3.one, "Home restores original fighter scale", report);
        var home = Object.FindFirstObjectByType<FitFightSelectionView>();
        Check(home != null && home.gameObject.activeInHierarchy, "Home UI restored", report);
        // StartPractice intentionally cancels the selector's coroutines. Finish the check from the editor loop.
        double deadline = EditorApplication.timeSinceStartup + 5;
        EditorApplication.CallbackFunction finish = null;
        finish = () =>
        {
            var practice = Object.FindFirstObjectByType<FitFightPracticeMode>();
            if (practice?.Ready != true && EditorApplication.timeSinceStartup < deadline) return;
            EditorApplication.update -= finish;
            try
            {
                Check(practice != null && practice.Ready, "Practice can be entered again", report);
                Check(Object.FindObjectsByType<PracticeHeavyBag>(FindObjectsSortMode.None).Length == 1, "Exactly one bag after reentry", report);
                report.AppendLine("PASS: all session lifecycle checks.");
            }
            catch (Exception e) { report.AppendLine("FAIL: " + e.Message); }
            File.WriteAllText("Logs/Practice/session-validation.txt", report.ToString());
            Debug.Log(report.ToString());
        };
        EditorApplication.update += finish;
        home.PracticeButton.onClick.Invoke();
    }

    [MenuItem("Tools/FitFight/Practice/Return home")]
    public static void Home() { Object.FindFirstObjectByType<PlayerSelectionManager>().ShowPlayerSelection(); }
    [MenuItem("Tools/FitFight/Practice/Run contact simulation")]
    public static void Run()
    {
        KeepSimulationRunning();
        var mode = Object.FindFirstObjectByType<FitFightPracticeMode>();
        if (mode == null || !mode.Ready) throw new InvalidOperationException("Enter practice first.");
        mode.StartCoroutine(Simulate(mode));
    }
    private static IEnumerator Simulate(FitFightPracticeMode mode)
    {
        var report = new StringBuilder();
        Directory.CreateDirectory("Logs/Practice");
        var bag = mode.Bag;
        // Suspend live sampling during deterministic contact tests; physics and feedback keep running.
        mode.enabled = false;
        try
        {
            var trackedAvatar = Object.FindFirstObjectByType<Tracking4All.Avatar>();
            Check(Mathf.Abs(FitFightPracticeMode.StandingHeight(trackedAvatar.GetComponent<Animator>()) - 1.7f) < .01f,
                "Practice fighter normalized to 1.7 m independent of pose", report);
            bag.ResetBag(); Physics.SyncTransforms();
            var animator = trackedAvatar.GetComponent<Animator>();
            foreach (var bone in new[] { HumanBodyBones.Head, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
            {
                Vector3 screen = mode.PracticeCamera.WorldToViewportPoint(animator.GetBoneTransform(bone).position);
                Check(screen.z > 0 && screen.x > .03f && screen.x < .97f && screen.y > .10f && screen.y < .84f,
                    "Practice framing includes " + bone + " above the controls", report);
            }
            int initial = bag.HitCount;
            float s = bag.Surface.radius / .20f;
            double time = Time.realtimeSinceStartupAsDouble;
            Vector3 c = bag.Surface.bounds.center;
            Vector3 outside = c + Vector3.back * .6f * s;
            Check(!bag.Sample(0, outside, .016f, time, true), "First tracking sample never hits", report);
            Check(!bag.Sample(0, outside + Vector3.left * .06f * s, .016f, time + .016, true), "A miss produces no hit", report);
            bag.ResetContacts();
            bag.Sample(0, outside, .033f, time, true);
            Check(bag.Sample(0, c + Vector3.back * .22f * s, .033f, time + .033, true), "Left punch reaches bag", report);
            Check(!bag.Sample(0, c, .033f, time + .4, true), "Holding inside bag cannot score twice", report);
            Check(bag.HitCount == initial + 1 && bag.EffectCount == bag.HitCount && bag.SoundCount == bag.HitCount,
                "One contact emits exactly one splash and sound", report);
        }
        catch (Exception ex) { report.AppendLine("FAIL: " + ex); Finish(mode, report); yield break; }
        Vector3 rest = bag.Body.position;
        yield return new WaitForSeconds(.4f);
        try
        {
            Check(Vector3.Distance(rest, bag.Body.position) > .005f, "Punch impulse moves the suspended bag in physics", report);
            Check(bag.GetComponent<ConfigurableJoint>() != null && Vector3.Distance(rest, bag.Body.position) < 1.3f,
                "Bag remains attached to its suspension", report);
            float s = bag.Surface.radius / .20f;
            int initial = bag.HitCount;
            for (int limb = 1; limb < 4; limb++)
            {
                bag.ResetBag(); Physics.SyncTransforms();
                Vector3 c = bag.Surface.bounds.center + Vector3.down * (limb >= 2 ? .25f * s : 0);
                double time = Time.realtimeSinceStartupAsDouble + limb;
                bag.Sample(limb, c + Vector3.back * .60f * s, .033f, time, true);
                Check(bag.Sample(limb, c + Vector3.back * .22f * s, .033f, time + .033, true),
                    limb == 1 ? "Right punch contact" : limb == 2 ? "Left kick contact" : "Right kick contact", report);
            }
            Check(bag.HitCount == initial + 3, "All four limbs register independently", report);
            bag.ResetBag(); Physics.SyncTransforms();
            Vector3 center = bag.Surface.bounds.center;
            bag.Sample(0, center + Vector3.back * .6f * s, .033f, 200, false);
            Check(!bag.Sample(0, center, .033f, 200.033, true), "Tracking reacquisition inside bag is ignored", report);
            bag.ResetContacts();
            bag.Sample(0, center + Vector3.back * 3 * s, .033f, 201, true);
            Check(!bag.Sample(0, center, .033f, 201.033, true), "Teleport is ignored", report);
            Check(PracticeHeavyBag.SweptContact(bag.Surface, center + Vector3.back * .7f * s,
                center + Vector3.forward * .7f * s, .075f * s, out _), "Fast sweep detects contact even when both endpoints miss", report);
            Check(bag.GetComponent<AudioSource>().clip == null && !bag.GetComponent<AudioSource>().playOnAwake,
                "Audio is contact-triggered, never ambient autoplay", report);
            foreach (bool kick in new[] { false, true })
            {
                var clip = PracticeHeavyBag.ImpactSound(kick); var samples = new float[clip.samples]; clip.GetData(samples, 0);
                float peak = 0; foreach (float v in samples) peak = Mathf.Max(peak, Mathf.Abs(v));
                Check(peak > .1f && peak < 1 && clip.length > .1f, kick ? "Kick audio has valid nonclipping waveform" : "Punch audio has valid nonclipping waveform", report);
                Object.Destroy(clip);
            }
            report.AppendLine("PASS: all practice simulation checks.");
            ScreenCapture.CaptureScreenshot("Logs/Practice/impact-simulation.png");
        }
        catch (Exception ex) { report.AppendLine("FAIL: " + ex); }
        Finish(mode, report);
    }
    private static void Finish(FitFightPracticeMode mode, StringBuilder report)
    {
        mode.Bag.ResetContacts(); mode.enabled = true;
        File.WriteAllText("Logs/Practice/validation.txt", report.ToString()); Debug.Log(report.ToString());
    }
    private static void Check(bool condition, string title, StringBuilder report)
    { if (!condition) throw new Exception(title); report.AppendLine("PASS: " + title); }
}
