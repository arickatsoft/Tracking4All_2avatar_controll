using UnityEngine;
using UnityEngine.UI;

namespace FitFight.BodyTracking
{
    public sealed class BodyTrackingSelector : MonoBehaviour
    {
        private readonly Toggle[] choices = new Toggle[5];
        public static void Create(Transform parent)
        {
            if (parent.GetComponentInChildren<BodyTrackingSelector>(true) != null) return;
            var root = MakeRect(parent, "Body tracking system", new Vector2(.07f, .555f), new Vector2(.93f, .695f));
            var view = root.gameObject.AddComponent<BodyTrackingSelector>();
            Text(root, "BODY TRACKING", 25, new Vector2(0, .72f), Vector2.one);
            var group = root.gameObject.AddComponent<ToggleGroup>();
            for(int i=0;i<view.choices.Length;i++)
            {
                int index=i; int row=i/2; int column=i%2;
                var min=new Vector2(column*.51f,.47f-row*.22f);
                var max=new Vector2(column*.51f+.49f,.68f-row*.22f);
                view.choices[i]=Choice(root,BodyTrackingSelection.Label((BodyTrackingSystem)i),min,max,group);
                view.choices[i].onValueChanged.AddListener(on => { if(on) BodyTrackingSelection.Select((BodyTrackingSystem)index); });
            }
            view.Refresh();
        }
        private void OnEnable() { BodyTrackingSelection.Changed += Refresh; Refresh(); }
        private void OnDisable() { BodyTrackingSelection.Changed -= Refresh; }
        private void Refresh()
        {
            for(int i=0;i<choices.Length;i++) if(choices[i]!=null) choices[i].SetIsOnWithoutNotify((int)BodyTrackingSelection.Selected==i);
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
            Text(root, label, 21, new Vector2(.06f, .05f), new Vector2(.95f, .95f));
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
