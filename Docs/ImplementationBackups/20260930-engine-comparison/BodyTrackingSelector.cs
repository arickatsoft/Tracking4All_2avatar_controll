using UnityEngine;
using UnityEngine.UI;

namespace FitFight.BodyTracking
{
    public sealed class BodyTrackingSelector : MonoBehaviour
    {
        private Toggle baseline, rtm;
        public static void Create(Transform parent)
        {
            if (parent.GetComponentInChildren<BodyTrackingSelector>(true) != null) return;
            var root = MakeRect(parent, "Body tracking system", new Vector2(.07f, .555f), new Vector2(.93f, .695f));
            var view = root.gameObject.AddComponent<BodyTrackingSelector>();
            Text(root, "BODY TRACKING", 25, new Vector2(0, .72f), Vector2.one);
            var group = root.gameObject.AddComponent<ToggleGroup>();
            view.baseline = Choice(root, "Tracking4All", new Vector2(0, .2f), new Vector2(.49f, .70f), group);
            view.rtm = Choice(root, "RTMPose", new Vector2(.51f, .2f), new Vector2(1, .70f), group);
            Text(root, "RTMPose-s Body26 · experimental", 19, Vector2.zero, new Vector2(1, .2f));
            view.Refresh();
            view.baseline.onValueChanged.AddListener(on => { if (on) BodyTrackingSelection.Select(BodyTrackingSystem.Tracking4All); });
            view.rtm.onValueChanged.AddListener(on => { if (on) BodyTrackingSelection.Select(BodyTrackingSystem.RTMPose); });
        }
        private void OnEnable() { BodyTrackingSelection.Changed += Refresh; Refresh(); }
        private void OnDisable() { BodyTrackingSelection.Changed -= Refresh; }
        private void Refresh()
        {
            if (baseline == null || rtm == null) return;
            bool useRTM = BodyTrackingSelection.Selected == BodyTrackingSystem.RTMPose;
            baseline.SetIsOnWithoutNotify(!useRTM); rtm.SetIsOnWithoutNotify(useRTM);
        }
        private static Toggle Choice(Transform parent, string label, Vector2 min, Vector2 max, ToggleGroup group)
        {
            var root = MakeRect(parent, label, min, max);
            var background = root.gameObject.AddComponent<Image>(); background.color = new Color(.12f, .19f, .27f);
            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background; toggle.group = group;
            var selected = MakeRect(root, "Selected", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            selected.color = new Color(.13f, .56f, .75f); selected.raycastTarget = false;
            toggle.graphic = selected;
            Text(root, label, 31, new Vector2(.06f, .05f), new Vector2(.95f, .95f));
            return toggle;
        }
        private static RectTransform MakeRect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private static void Text(Transform parent, string value, int size, Vector2 min, Vector2 max)
        {
            var text = MakeRect(parent, value, min, max).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = size; text.fontStyle = FontStyle.Bold;
            text.color = Color.white; text.alignment = TextAnchor.MiddleLeft;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = size;
            text.raycastTarget = false;
        }
    }
}
