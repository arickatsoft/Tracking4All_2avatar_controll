using System.Collections;
using System.Collections.Generic;
using FitFight.BodyTracking;
using FitFight.Practice;
using Tracking4All;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reversible, scene-local solo practice with the selected camera tracking backend.</summary>
[DefaultExecutionOrder(10000)]
public sealed class FitFightPracticeMode : MonoBehaviour
{
    public PracticeHeavyBag Bag => gym != null ? gym.Bag : null;
    public Camera PracticeCamera { get; private set; }
    public bool Ready { get; private set; }
    private PlayerSelectionManager owner;
    private GameObject fighter, opponent, ui;
    private Tracking4All.Avatar avatar;
    private Animator animator;
    public AssistedPunchController Punches { get; private set; }
    private Text punchModeLabel, recordingLabel, recordingPrompt;
    private PracticeGym gym;
    private FitFightArenaPresentation presentation;
    private FitFightFighterGrounding grounding;
    private float oldFloor;
    private bool addedGrounding;
    private Vector3 savedPosition, savedScale;
    private Quaternion savedRotation;
    private bool opponentActive, fighterActive, trackingEnabled, ended;
    private readonly List<Behaviour> suspended = new List<Behaviour>();
    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private readonly Transform[] limbs = new Transform[4];
    private readonly Transform[] parents = new Transform[4];
    private readonly HumanBodyBones[] bones = { HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot };
    private readonly HumanBodyBones[] parentBones = { HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg };
    private Text stats, status, cameraNotice;
    private bool cameraAvailable;
    private RawImage previewImage;
    private PracticeSkeletonOverlay skeletonOverlay;
    private Material previewMaterial;
    private RenderTexture preview;
    private RectTransform safeRoot;
    private float height, lastHitTime = -10, nextStatus;
    private Vector3 center;
    private Color oldAmbient;
    private UnityEngine.Rendering.AmbientMode oldAmbientMode;
    private bool oldFog;

    public void Begin(GameObject selected, GameObject other, PlayerSelectionManager selection)
    {
        owner = selection; fighter = selected; opponent = other;
        if (fighter == null || (animator = fighter.GetComponentInChildren<Animator>()) == null || !animator.isHuman)
        { Debug.LogError("Practice needs a humanoid fighter."); owner.ShowPlayerSelection(); return; }
        StartCoroutine(Initialize());
    }
    private IEnumerator Initialize()
    {
        // Let the existing scene initialization finish before taking ownership of framing.
        yield return null; yield return null;
        savedPosition = fighter.transform.position; savedRotation = fighter.transform.rotation;
        savedScale = fighter.transform.localScale;
        fighterActive = fighter.activeSelf; opponentActive = opponent != null && opponent.activeSelf;
        fighter.SetActive(true);
        avatar = fighter.GetComponent<Tracking4All.Avatar>();
        trackingEnabled = avatar != null && avatar.enabled;
        if (avatar != null) avatar.enabled = true;
        if (opponent != null) opponent.SetActive(false);
        presentation = GetComponent<FitFightArenaPresentation>();
        if (presentation != null) presentation.SetPracticeVisible(false);
        foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Suspend(camera);
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None)) Suspend(light);
        foreach (var rig in FindObjectsByType<FitFightArenaCamera>(FindObjectsSortMode.None)) Suspend(rig);
        // Hide the debug skeleton meshes; their transforms remain available to the selected retargeter.
        foreach (var puppet in FindObjectsByType<PosePuppet>(FindObjectsSortMode.None))
            foreach (var renderer in puppet.GetComponentsInChildren<Renderer>())
                if (renderer.enabled) { renderer.enabled = false; hiddenRenderers.Add(renderer); }
        oldAmbient = RenderSettings.ambientLight; oldAmbientMode = RenderSettings.ambientMode; oldFog = RenderSettings.fog;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.30f, .30f, .30f); RenderSettings.fog = false;
        // Bone-chain lengths stay constant while a user crouches or raises a foot.
        // Normalize imported model units once; never size the gym from a live pose's vertical span.
        float measuredHeight = StandingHeight(animator);
        height = 1.7f;
        fighter.transform.localScale = savedScale * (height / measuredHeight);
        var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        for (int i = 0; i < 4; i++) { limbs[i] = animator.GetBoneTransform(bones[i]); parents[i] = animator.GetBoneTransform(parentBones[i]); }
        float arm = ArmLength(true) * .5f + ArmLength(false) * .5f;
        float reach = Mathf.Max(height * .34f, arm * .87f) + .20f * height / 1.7f;
        var origin = new Vector3(100, 0, 100);
        fighter.transform.SetPositionAndRotation(origin + Vector3.up * (fighter.transform.position.y - foot.position.y + height * .045f), Quaternion.identity);
        foreach (var guard in fighter.GetComponentsInChildren<FitFightGuardPose>()) guard.SetStageAnchor();
        grounding = fighter.GetComponent<FitFightFighterGrounding>();
        if (grounding != null) oldFloor = grounding.FloorY;
        else { grounding = fighter.AddComponent<FitFightFighterGrounding>(); addedGrounding = true; }
        grounding.Configure(0, height);
        gym = new GameObject("FitFight Practice Gym").AddComponent<PracticeGym>();
        gym.transform.position = origin;
        gym.Build(height, reach);
        Bag.Hit += OnHit;
        if (avatar != null)
        {
            Punches = fighter.AddComponent<AssistedPunchController>();
            Punches.Bind(avatar, Bag);
        }
        PracticeCamera = new GameObject("Practice Camera").AddComponent<Camera>();
        PracticeCamera.transform.SetParent(gym.transform, false);
        PracticeCamera.clearFlags = CameraClearFlags.SolidColor;
        PracticeCamera.backgroundColor = new Color(.025f, .03f, .04f);
        PracticeCamera.nearClipPlane = .05f; PracticeCamera.farClipPlane = 40;
        PracticeCamera.fieldOfView = 34;
        PracticeCamera.gameObject.AddComponent<PracticeBackdrop>(); PracticeCamera.depth = 5;
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) Suspend(listener);
        PracticeCamera.gameObject.AddComponent<AudioListener>();
        center = origin + new Vector3(0, height * .55f, reach * .31f);
        BuildUI();
        Ready = true;
    }
    private void Suspend(Behaviour behaviour)
    {
        if (behaviour.enabled) { behaviour.enabled = false; suspended.Add(behaviour); }
    }
    public static float StandingHeight(Animator rig)
    {
        float Chain(params HumanBodyBones[] chain)
        {
            float length = 0; Transform previous = null;
            foreach (var bone in chain)
            {
                var current = rig.GetBoneTransform(bone);
                if (current == null) continue;
                if (previous != null) length += Vector3.Distance(previous.position, current.position);
                previous = current;
            }
            return length;
        }
        float leg = .5f * (Chain(HumanBodyBones.LeftFoot, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips)
            + Chain(HumanBodyBones.RightFoot, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips));
        float torso = Chain(HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest,
            HumanBodyBones.UpperChest, HumanBodyBones.Neck, HumanBodyBones.Head);
        return Mathf.Max(.1f, (leg + torso) * 1.08f);
    }
    private float ArmLength(bool left)
    {
        var upper = animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
        var lower = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
        var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        return Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);
    }
    private void LateUpdate()
    {
        if (!Ready || ended) return;
        float aspect = Mathf.Max(.4f, Screen.width / (float)Mathf.Max(1, Screen.height));
        // Fit full extension and the bag, including room for a swing on portrait phones.
        float distance = height * Mathf.Max(2.65f, 1.32f / aspect);
        PracticeCamera.transform.position = center + new Vector3(.85f, .32f, -.66f).normalized * distance;
        PracticeCamera.transform.LookAt(center);
        bool fresh = avatar != null && avatar.HasFreshPose;
        var rtm = ActivePose();
        for (int i = 0; i < 4; i++)
        {
            if (limbs[i] == null) continue;
            bool reliable = fresh && (i < 2 || (avatar.HasReliableLeg(i == 2)
                && (rtm != null ? rtm.HasMeasuredLeg(i == 2) : avatar.PosePuppet == null || avatar.PosePuppet.HasMeasuredLeg(i == 2))));
            if (rtm != null && i < 2)
            {
                var wrist = rtm.AvatarLandmark(i == 0 ? PoseLandmarks.LEFT_WRIST : PoseLandmarks.RIGHT_WRIST);
                var elbow = rtm.AvatarLandmark(i == 0 ? PoseLandmarks.LEFT_ELBOW : PoseLandmarks.RIGHT_ELBOW);
                reliable &= wrist.Visibility >= rtm.Tuning.jointConfidence && elbow.Visibility >= rtm.Tuning.jointConfidence
                    && rtm.IsJointObserved(i == 0 ? PoseLandmarks.LEFT_WRIST : PoseLandmarks.RIGHT_WRIST)
                    && rtm.IsJointObserved(i == 0 ? PoseLandmarks.LEFT_ELBOW : PoseLandmarks.RIGHT_ELBOW);
            }
            Vector3 endpoint = limbs[i].position;
            // Cover the fist/shoe tip beyond the wrist/ankle joint; no gesture-based hit substitution.
            if (i < 2 && parents[i] != null) endpoint += (endpoint - parents[i].position).normalized * height * .035f;
            else
            {
                var toe = animator.GetBoneTransform(i == 2 ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes);
                if (toe != null) endpoint = Vector3.Lerp(endpoint, toe.position, .7f);
            }
            if (Punches != null && Punches.Active)
            {
                // Assisted practice owns a stable full-body punch stance. Mirroring mode
                // remains available for directly tracked kicks and other body movements.
                if (i < 2) Bag.SampleAssistedPunch(i, Punches.ActionId, endpoint, Time.unscaledDeltaTime,
                    Time.realtimeSinceStartupAsDouble, Punches.Side == i && Punches.State != AssistedPunchController.Motion.Guard,
                    Punches.Side == i && Punches.CanHit);
            }
            else Bag.Sample(i, endpoint, Time.unscaledDeltaTime, Time.realtimeSinceStartupAsDouble, reliable,
                Punches == null || !Punches.FullBodyDiagnostic);
        }
        Punches?.RecordFinalPose();
        if (punchModeLabel != null) punchModeLabel.text = Punches != null && Punches.Active ? "PUNCH ASSIST: ON" : "BODY MIRRORING";
        if (recordingPrompt != null)
        {
            recordingPrompt.text = Punches != null ? Punches.RecordingPrompt : "";
            recordingPrompt.transform.parent.gameObject.SetActive(Punches != null && Punches.Recording);
        }
        if (recordingLabel != null) recordingLabel.text = Punches != null && Punches.Recording ? "STOP RECORDING" : "RECORD TEST";
        UpdatePreview(rtm);
        var safe = Screen.safeArea;
        // Device Simulator can retain its phone safe area for a frame after switching to Game view.
        // Never let a stale or invalid inset expand the HUD beyond the current render viewport.
        if (safe.width <= 0 || safe.height <= 0 || safe.xMin < 0 || safe.yMin < 0
            || safe.xMax > Screen.width + 1 || safe.yMax > Screen.height + 1)
            safe = new Rect(0, 0, Screen.width, Screen.height);
        safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        if (Time.unscaledTime >= nextStatus)
        {
            nextStatus = Time.unscaledTime + .2f;
            status.text = !cameraAvailable ? "STARTING CAMERA · CHECK CAMERA PERMISSION" : fresh ? "TRACKING LIVE  ·  " + (rtm != null ? "RTMPose" : "Tracking4All") : rtm != null && !rtm.Calibrated
                ? rtm.CalibrationCountdown > 0 ? $"STEP BACK · CALIBRATION IN {Mathf.CeilToInt(rtm.CalibrationCountdown)} SECONDS"
                : rtm.LastInferenceMs > 200 ? $"INFERENCE {rtm.LastInferenceMs:F0} MS · TOO SLOW\nOPEN TRACKING FOR DIAGNOSTICS"
                : $"CALIBRATING {rtm.CalibrationFrames}/20 · CAM {rtm.CameraFPS:F0} / AI {rtm.InferenceFPS:F0} FPS\nSTAND UPRIGHT · SHOW SHOULDERS AND HIPS"
                : "STEP BACK UNTIL YOUR FULL BODY IS VISIBLE";
            if (cameraAvailable && Punches != null && Punches.Active && (fresh || Punches.Fresh)) status.text = Punches.Status;
            if (Punches != null && !string.IsNullOrEmpty(Punches.Error)) status.text = Punches.Error;
            stats.text = Bag.HitCount.ToString("00") + "  HITS   /   " + (Time.unscaledTime - lastHitTime < 1.2f ? Bag.LastHit : "PUNCH PRACTICE");
        }
    }
    private RTMPosePoseProvider ActivePose()
    {
        var source = avatar != null && avatar.PosePuppet != null ? avatar.PosePuppet.PoseSource : null;
        return source != null ? source.GetComponent<PoseSolution>()?.ActiveRTMPose : null;
    }
    private void OnHit() { lastHitTime = Time.unscaledTime; nextStatus = 0; }
    public void Recalibrate()
    {
        Punches?.ResetMotion();
        ActivePose()?.CalibrateFloorCamera();
        if (!cameraAvailable)
        {
            var source = avatar?.PosePuppet?.PoseSource;
            var graph = source != null ? source.GetComponentInChildren<MPUPoseTrackingSolution>(true) : null;
            if (graph?.CameraFrames is UnityWebCamBackend backend) backend.OnSettingsApplied();
        }
        Bag?.ResetBag();
        if (status != null) status.text = "STAND UPRIGHT  ·  KEEP BOTH FEET AND HANDS IN FRAME";
    }
    private void UpdatePreview(RTMPosePoseProvider pose)
    {
        var source = avatar != null && avatar.PosePuppet != null ? avatar.PosePuppet.PoseSource : null;
        var graph = source != null ? source.GetComponentInChildren<MPUPoseTrackingSolution>(true) : null;
        Texture texture = pose != null ? pose.CameraTexture : graph?.CameraFrames?.Get(0, 0)?.Texture;
        if (texture is not WebCamTexture camera || !camera.isPlaying || camera.width <= 16)
        {
            cameraAvailable = false; previewImage.enabled = false; skeletonOverlay.enabled = false;
            cameraNotice.gameObject.SetActive(true); return;
        }
        cameraAvailable = true; skeletonOverlay.enabled = true; cameraNotice.gameObject.SetActive(false);
        int rotation = camera.videoRotationAngle;
        float aspect = rotation == 90 || rotation == 270 ? camera.height / (float)camera.width : camera.width / (float)camera.height;
        previewImage.enabled = true;
        if (previewMaterial == null) previewMaterial = new Material(Resources.Load<Shader>("RTMPose/Preview"));
        if (preview == null) { preview = new RenderTexture(256, 256, 0); previewImage.texture = preview; }
        previewMaterial.SetInt("_Rotation", rotation); previewMaterial.SetInt("_VerticalFlip", camera.videoVerticallyMirrored ? 1 : 0);
        if (camera.didUpdateThisFrame) Graphics.Blit(texture, preview, previewMaterial);
        previewImage.GetComponent<AspectRatioFitter>().aspectRatio = aspect;
        skeletonOverlay.Sample(pose, graph, graph?.CameraFrames?.Get(0, 0)?.IsFrontFacing == true);
    }
    private void BuildUI()
    {
        ui = new GameObject("Practice HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 60;
        var scaler = ui.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280); scaler.matchWidthOrHeight = .5f;
        safeRoot = Rect(ui.transform, "Safe area", Vector2.zero, Vector2.one);
        TextAt("PRACTICE", 22, new Vector2(.32f, .938f), new Vector2(.72f, .982f));
        stats = TextAt("00 HITS", 16, new Vector2(.57f, .904f), new Vector2(.97f, .94f));
        status = TextAt("STEP BACK · FULL BODY IN FRAME", 13, new Vector2(.30f, .091f), new Vector2(.96f, .145f));
        ButtonAt("HOME", new Vector2(.025f, .943f), new Vector2(.23f, .982f), () => owner.ShowPlayerSelection());
        ButtonAt("CALIBRATE", new Vector2(.30f, .027f), new Vector2(.61f, .073f), Recalibrate);
        ButtonAt("RESET BAG", new Vector2(.65f, .027f), new Vector2(.97f, .073f), () => { Punches?.ResetMotion(); Bag.ResetBag(); });
        ButtonAt("PUNCH ASSIST: ON", new Vector2(.30f, .185f), new Vector2(.65f, .222f), () =>
        {
            if (Punches == null) return;
            Punches.SetAssistance(!Punches.Active);
            punchModeLabel.text = Punches.Active ? "PUNCH ASSIST: ON" : "BODY MIRRORING";
        });
        punchModeLabel = safeRoot.Find("PUNCH ASSIST: ON/Label").GetComponent<Text>();
        if (Punches == null || !Punches.Active) punchModeLabel.text = "BODY MIRRORING";
        ButtonAt("RECORD TEST", new Vector2(.67f, .185f), new Vector2(.97f, .222f), () => Punches?.ToggleRecording());
        ButtonAt("FULL BODY TEST", new Vector2(.30f, .148f), new Vector2(.97f, .179f), () => Punches?.StartFullBodyRecording());
        recordingLabel = safeRoot.Find("RECORD TEST/Label").GetComponent<Text>();
        recordingPrompt = TextAt("", 14, new Vector2(.30f, .23f), new Vector2(.97f, .285f));
        var promptPanel = recordingPrompt.transform.parent.gameObject.AddComponent<Image>();
        promptPanel.color = new Color(.025f, .03f, .04f, .92f); promptPanel.raycastTarget = false;
        promptPanel.transform.SetAsFirstSibling();
        var previewRect = Rect(safeRoot, "Full body camera panel", new Vector2(.025f, .022f), new Vector2(.275f, .32f));
        var border = previewRect.gameObject.AddComponent<Image>(); border.color = new Color(.06f, .06f, .06f, .55f); border.raycastTarget = false;
        var label = Label(Rect(previewRect, "Camera label", new Vector2(.035f, .91f), new Vector2(.965f, .995f)), "LIVE CAMERA", 11);
        cameraNotice = Label(Rect(previewRect, "No camera notice", new Vector2(.04f, .35f), new Vector2(.96f, .65f)), "CONNECT\nCAMERA", 14);
        cameraNotice.alignment = TextAnchor.MiddleCenter;
        var content = Rect(previewRect, "Camera bounds", new Vector2(.025f, .015f), new Vector2(.975f, .90f));
        previewImage = Rect(content, "Preview image", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>(); previewImage.raycastTarget = false;
        var fitter = previewImage.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        skeletonOverlay = Rect(previewImage.transform, "Tracked body overlay", Vector2.zero, Vector2.one).gameObject.AddComponent<PracticeSkeletonOverlay>();
        skeletonOverlay.raycastTarget = false;
    }
    private void ButtonAt(string title, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(safeRoot, title, min, max);
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.08f, .09f, .10f, .73f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        var label = Label(rect, title, 15); label.alignment = TextAnchor.MiddleCenter;
    }

    private Text TextAt(string value, int size, Vector2 min, Vector2 max, Color? color = null)
    {
        var text = Label(Rect(safeRoot, value, min, max), value, size); text.color = color ?? Color.white; return text;
    }
    private static Text Label(RectTransform parent, string value, int size)
    {
        var text = Rect(parent, "Label", Vector2.zero, Vector2.one).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value; text.fontSize = size;
        text.fontStyle = FontStyle.Bold; text.color = Color.white; text.alignment = TextAnchor.MiddleLeft;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = size; text.raycastTarget = false;
        return text;
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    public void End()
    {
        if (ended) return;
        ended = true; StopAllCoroutines();
        if (Punches != null) { Punches.enabled = false; Destroy(Punches); Punches = null; }
        if (Ready)
        {
            foreach (var behaviour in suspended) if (behaviour != null) behaviour.enabled = true;
            foreach (var renderer in hiddenRenderers) if (renderer != null) renderer.enabled = true;
            if (fighter != null)
            {
                fighter.transform.localScale = savedScale;
                fighter.transform.SetPositionAndRotation(savedPosition, savedRotation);
                foreach (var guard in fighter.GetComponentsInChildren<FitFightGuardPose>()) guard.SetStageAnchor();
            }
            if (grounding != null)
            {
                if (addedGrounding) Destroy(grounding);
                else grounding.Configure(oldFloor, height);
            }
            if (avatar != null) avatar.enabled = trackingEnabled;
            if (fighter != null) fighter.SetActive(fighterActive);
            if (opponent != null) opponent.SetActive(opponentActive);
            if (presentation != null) presentation.SetPracticeVisible(true);
            RenderSettings.ambientLight = oldAmbient; RenderSettings.ambientMode = oldAmbientMode; RenderSettings.fog = oldFog;
        }
        if (gym != null) { gym.gameObject.SetActive(false); Destroy(gym.gameObject); }
        if (ui != null) { ui.SetActive(false); Destroy(ui); }
        if (preview != null) { preview.Release(); Destroy(preview); }
        if (previewMaterial != null) Destroy(previewMaterial);
        Ready = false; Destroy(this);
    }
    private void OnDestroy() { if (!ended) End(); }
}
