using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-scoped presentation, configured by the existing player selector.</summary>
public sealed class FitFightArenaPresentation : MonoBehaviour
{
    private FitFightStreetArena arena;
    private GameObject hud;
    private RectTransform safeHud;
    private Text localName, remoteName;
    // The Tracking4All pose graph renders its live input through this small viewport.
    // It must remain active during gameplay; it is not a second gameplay camera.
    private readonly System.Collections.Generic.List<Camera> trackingPreviews = new System.Collections.Generic.List<Camera>();
    private readonly System.Collections.Generic.List<string> trackingPreviewTags = new System.Collections.Generic.List<string>();
    private Button menu;
    private Transform originalMenuParent;
    private Vector2 menuMin, menuMax, menuOffsetMin, menuOffsetMax;
    private Rect lastSafe;
    private Vector2Int lastScreen;

    public void SetPracticeVisible(bool visible)
    {
        if (arena != null) arena.gameObject.SetActive(visible);
        if (hud != null) hud.SetActive(visible);
    }

    public void Configure(GameObject remy, GameObject adam, Camera main, GameObject adamRig, bool localIsAdam)
    {
        if (remy == null || adam == null) return;
        foreach (var coordinates in FindObjectsOfType<Tracking4All.TransformTargets>(true))
            if (coordinates.gameObject.scene == gameObject.scene) coordinates.UprightCoordinates = true;
        // The serialized reference in the old scene points at a preview camera.
        // Resolve the actual game camera instead of converting the preview to a full-screen view.
        KeepTrackingPreview();
        Camera gameCamera = FindGameplayCamera(main);
        if (gameCamera != null) main = gameCamera;
        Transform local = localIsAdam ? adam.transform : remy.transform;
        Transform remote = localIsAdam ? remy.transform : adam.transform;
        if (main != null && !main.name.Contains("Preview")) SetupCamera(main, local, remote);
        if (adamRig != null)
            foreach (var camera in adamRig.GetComponentsInChildren<Camera>(true))
                if (camera.targetTexture == null && !camera.name.Contains("Preview")) SetupCamera(camera, local, remote);
        if (arena == null)
        {
            foreach (var fighter in new[] { remy, adam })
                foreach (var animator in fighter.GetComponentsInChildren<Animator>(true))
                    if (animator.isHuman && animator.GetComponent<FitFightGuardPose>() == null)
                        animator.gameObject.AddComponent<FitFightGuardPose>();
            arena = new GameObject("FitFight Street Court").AddComponent<FitFightStreetArena>();
            StartCoroutine(BuildAfterInitialization(remy.transform, adam.transform));
            BuildHud();
        }
        arena.SetView(local, remote);
        localName.text = localIsAdam ? "Adam" : "Remy";
        remoteName.text = localIsAdam ? "Remy" : "Adam";
    }

    private static Camera FindGameplayCamera(Camera serializedCamera)
    {
        if (serializedCamera != null && !serializedCamera.name.Contains("Preview")) return serializedCamera;

        Camera taggedCamera = null;
        foreach (var candidate in FindObjectsOfType<Camera>())
        {
            if (candidate.name.Contains("Preview")) continue;
            if (candidate.name == "Main Camera Adam") return candidate;
            if (candidate.CompareTag("MainCamera")) taggedCamera = candidate;
        }
        return taggedCamera;
    }

    private void KeepTrackingPreview()
    {
        foreach (var candidate in FindObjectsOfType<Camera>())
        {
            if (candidate.name != "Preview Camera" || trackingPreviews.Contains(candidate)) continue;
            trackingPreviews.Add(candidate);
            trackingPreviewTags.Add(candidate.tag);
            // The old scene accidentally tags this picture-in-picture camera as MainCamera.
            // Removing that tag prevents Camera.main from selecting it while retaining its viewport.
            candidate.tag = "Untagged";
            candidate.enabled = true;
        }
    }

    private IEnumerator BuildAfterInitialization(Transform first, Transform second)
    {
        yield return null;
        if (arena != null && first != null && second != null)
        {
            var a = FitFightArenaCamera.FighterBounds(first, first.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            var b = FitFightArenaCamera.FighterBounds(second, second.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            Vector3 heading = Vector3.ProjectOnPlane(second.position - first.position, Vector3.up);
            if (heading.sqrMagnitude < .001f) heading = Vector3.forward;
            heading.Normalize();
            float separation = Mathf.Max(a.size.y, b.size.y) * .82f;
            Vector3 midpoint = (first.position + second.position) * .5f;
            first.position = new Vector3(midpoint.x, first.position.y, midpoint.z) - heading * separation * .5f;
            second.position = new Vector3(midpoint.x, second.position.y + FitFightStreetArena.FootLevel(first, a.min.y) - FitFightStreetArena.FootLevel(second, b.min.y), midpoint.z) + heading * separation * .5f;
            first.rotation = Quaternion.LookRotation(heading);
            second.rotation = Quaternion.LookRotation(-heading);
            foreach (var root in new[] { first, second })
                foreach (var guard in root.GetComponentsInChildren<FitFightGuardPose>()) guard.SetStageAnchor();
            arena.Build(first, second);
            foreach (var framing in FindObjectsOfType<FitFightArenaCamera>(true)) framing.ResetFraming();
            foreach (var root in new[] { first, second })
            {
                var grounding = root.GetComponent<FitFightFighterGrounding>();
                if (grounding == null) grounding = root.gameObject.AddComponent<FitFightFighterGrounding>();
                grounding.Configure(arena.transform.position.y, Mathf.Max(a.size.y, b.size.y));
            }
            foreach (var button in FindObjectsOfType<Button>())
            {
                var label = button.GetComponentInChildren<Text>();
                if (label == null || label.text.Trim().ToUpperInvariant() != "MENU") continue;
                menu = button;
                var rect = (RectTransform)button.transform;
                originalMenuParent = rect.parent;
                menuMin = rect.anchorMin; menuMax = rect.anchorMax;
                menuOffsetMin = rect.offsetMin; menuOffsetMax = rect.offsetMax;
                rect.SetParent(safeHud, false);
                rect.anchorMin = new Vector2(.025f, .922f); rect.anchorMax = new Vector2(.16f, .944f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                label.color = Color.white;
                label.resizeTextForBestFit = true;
                label.resizeTextMaxSize = 28;
                if (button.image != null) button.image.color = new Color(.30f, .35f, .37f);
                break;
            }
        }
    }

    private static void SetupCamera(Camera camera, Transform local, Transform remote)
    {
        var framing = camera.GetComponent<FitFightArenaCamera>();
        if (framing == null) framing = camera.gameObject.AddComponent<FitFightArenaCamera>();
        framing.Configure(local, remote);
    }

    private void BuildHud()
    {
        hud = new GameObject("FitFight Arena HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = .5f;
        safeHud = MakeRect(hud.transform, "Safe HUD", Vector2.zero, Vector2.one);
        localName = Card("Remy", new Vector2(.025f, .874f), new Vector2(.35f, .944f), new Color(0, .74f, .30f));
        remoteName = Card("Adam", new Vector2(.65f, .874f), new Vector2(.975f, .944f), new Color(.98f, .015f, .025f));
        UpdateSafeArea();
    }

    private Text Card(string title, Vector2 min, Vector2 max, Color accent)
    {
        var rect = MakeRect(safeHud, title, min, max);
        var background = rect.gameObject.AddComponent<FitFightRoundedPanel>();
        background.color = new Color(.18f, .24f, .30f, .91f);
        background.raycastTarget = false;
        var name = MakeRect(rect, "Name", new Vector2(.1f, .31f), new Vector2(.9f, .68f)).gameObject.AddComponent<Text>();
        name.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        name.fontSize = 45;
        name.resizeTextForBestFit = true;
        name.resizeTextMinSize = 16;
        name.resizeTextMaxSize = 45;
        name.color = Color.white;
        name.alignment = TextAnchor.MiddleLeft;
        name.raycastTarget = false;
        // Fighter identity accent, not a fabricated health meter. Match state is separate.
        var stripe = MakeRect(rect, "Fighter color", new Vector2(.1f, .10f), new Vector2(.9f, .235f)).gameObject.AddComponent<FitFightRoundedPanel>();
        stripe.CornerRoundness = .5f;
        stripe.color = accent;
        stripe.raycastTarget = false;
        return name;
    }

    private static RectTransform MakeRect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafe || lastScreen.x != Screen.width || lastScreen.y != Screen.height)
            UpdateSafeArea();
    }

    private void UpdateSafeArea()
    {
        if (safeHud == null || Screen.width <= 0 || Screen.height <= 0) return;
        lastSafe = Screen.safeArea;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
        safeHud.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
        safeHud.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height);
    }

    private void OnDestroy()
    {
        if (menu != null && originalMenuParent != null)
        {
            var rect = (RectTransform)menu.transform;
            rect.SetParent(originalMenuParent, false);
            rect.anchorMin = menuMin; rect.anchorMax = menuMax;
            rect.offsetMin = menuOffsetMin; rect.offsetMax = menuOffsetMax;
        }
        if (hud != null) Destroy(hud);
        if (arena != null) Destroy(arena.gameObject);
        for (var index = 0; index < trackingPreviews.Count; index++)
            if (trackingPreviews[index] != null) trackingPreviews[index].tag = trackingPreviewTags[index];
    }
}
