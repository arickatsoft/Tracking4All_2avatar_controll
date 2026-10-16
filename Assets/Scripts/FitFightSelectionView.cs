using UnityEngine;
using UnityEngine.UI;

/// <summary>Portrait-first selection UI; retains PlayerSelectionManager's existing actions.</summary>
public sealed class FitFightSelectionView : MonoBehaviour
{
    private RectTransform safeRoot;
    private RectTransform adam, remy;
    public Button PracticeButton { get; private set; }
    private Rect lastSafeArea;
    private Vector2Int lastSize;

    public static GameObject Create(out Button adamButton, out Button remyButton)
    {
        var root = new GameObject("FitFight Selection", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FitFightSelectionView));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        Panel(root.transform, "Scrim", new Color(0.025f, 0.04f, 0.07f, 0.9f), Vector2.zero, Vector2.one);
        var view = root.GetComponent<FitFightSelectionView>();
        view.safeRoot = Rect(root.transform, "Safe area", Vector2.zero, Vector2.one);
        var parent = view.safeRoot;
        Label(parent, "FIT / FIGHT", 94, Color.white, new Vector2(.07f, .79f), new Vector2(.93f, .9f));
        Label(parent, "YOUR MOVEMENT. YOUR FIGHT.", 27, new Color(.3f, .88f, 1f),
            new Vector2(.07f, .73f), new Vector2(.93f, .79f));
        Panel(parent, "Accent", new Color(.3f, .88f, 1f), new Vector2(.07f, .70f), new Vector2(.20f, .705f));
        FitFight.BodyTracking.BodyTrackingSelector.Create(parent);
        Label(parent, "STEP INTO THE ARENA", 39, Color.white, new Vector2(.07f, .485f), new Vector2(.93f, .54f));
        Label(parent, "Choose your fighter.\nYour opponent selects the other fighter.", 30,
            new Color(.67f, .73f, .82f), new Vector2(.07f, .40f), new Vector2(.93f, .485f));
        adamButton = Fighter(parent, "ADAM", "01", new Color(.13f, .56f, .75f));
        remyButton = Fighter(parent, "REMY", "02", new Color(.69f, .24f, .29f));
        view.adam = (RectTransform)adamButton.transform;
        view.remy = (RectTransform)remyButton.transform;
        view.PracticeButton = Fighter(parent, "PRACTICE / HEAVY BAG", "+", new Color(.16f, .32f, .30f));
        Place((RectTransform)view.PracticeButton.transform, new Vector2(.07f, .06f), new Vector2(.93f, .15f));
        Label(parent, "CAMERA-POWERED COMBAT", 23, new Color(.58f, .64f, .73f),
            new Vector2(.07f, .012f), new Vector2(.93f, .045f));
        view.UpdateLayout();
        return root;
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || lastSize.x != Screen.width || lastSize.y != Screen.height)
            UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (safeRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
        lastSafeArea = Screen.safeArea;
        lastSize = new Vector2Int(Screen.width, Screen.height);
        safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        bool portrait = lastSafeArea.height >= lastSafeArea.width;
        Place(adam, new Vector2(.07f, portrait ? .28f : .20f), new Vector2(portrait ? .93f : .48f, .375f));
        Place(remy, new Vector2(portrait ? .07f : .52f, .17f), new Vector2(.93f, portrait ? .265f : .375f));
        if (!portrait) Place(remy, new Vector2(.52f, .20f), new Vector2(.93f, .375f));
    }

    private static Button Fighter(Transform parent, string name, string number, Color color)
    {
        var rect = Panel(parent, name, color, Vector2.zero, Vector2.one);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(.72f, .72f, .72f);
        colors.fadeDuration = .08f;
        button.colors = colors;
        Label(rect, number, 29, new Color(1, 1, 1, .65f), new Vector2(.05f, .12f), new Vector2(.15f, .88f));
        Label(rect, name, 47, Color.white, new Vector2(.19f, .12f), new Vector2(.82f, .88f));
        Label(rect, ">", 45, Color.white, new Vector2(.88f, .12f), new Vector2(.97f, .88f));
        return button;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Place(rect, min, max);
        return rect;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static RectTransform Panel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        var rect = Rect(parent, name, min, max);
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    private static void Label(Transform parent, string value, int size, Color color, Vector2 min, Vector2 max)
    {
        var text = Rect(parent, value, min, max).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = size;
        text.raycastTarget = false;
    }
}
