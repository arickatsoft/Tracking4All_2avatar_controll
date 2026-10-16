// InterfaceProviderEditor
// (C) 2024-2025 G8gaming Ltd.
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using System.Text;

namespace Tracking4All
{
    [CustomPropertyDrawer(typeof(FaceDataProvider))]
    public class FaceDataProviderUIE : InterfaceProviderUIEditor
    {
        protected override void UpdateMessage(Rect position, SerializedProperty property, GUIContent label)
        {
            LifecycleMessage(position, property, label);
        }
    }
    [CustomPropertyDrawer(typeof(ImageFrameProvider))]
    public class ImageFrameProviderUIE : InterfaceProviderUIEditor
    {
        protected override void UpdateMessage(Rect position, SerializedProperty property, GUIContent label)
        {
            LifecycleMessage(position, property, label);
        }
    }
    [CustomPropertyDrawer(typeof(LifecycleProvider))]
    public class LifecycleProviderUIE : InterfaceProviderUIEditor
    {
    }

    [CustomPropertyDrawer(typeof(SegmentationProvider))]
    public class SegmentationProviderUIE : InterfaceProviderUIEditor
    {
        protected override void UpdateMessage(Rect position, SerializedProperty property, GUIContent label)
        {
            LifecycleMessage(position, property, label);
        }
    }
    [CustomPropertyDrawer(typeof(SegmentationDataProvider))]
    public class SegmentationDataProviderUIE : InterfaceProviderUIEditor
    {
        protected override void UpdateMessage(Rect position, SerializedProperty property, GUIContent label)
        {
            LifecycleMessage(position, property, label);
        }
    }
    [CustomPropertyDrawer(typeof(NormalizedLandmarkProvider<>))]
    public class NormalizedLandmarkProviderUIE : InterfaceProviderUIEditor
    {
        protected override void UpdateMessage(Rect position, SerializedProperty property, GUIContent label)
        {
            LifecycleMessage(position, property, label);
        }
    }
    [CustomPropertyDrawer(typeof(LandmarkProvider<>))]
    public class LandmarkProviderUIE : InterfaceProviderUIEditor
    {
        protected override void UpdateMessage(Rect position, SerializedProperty property, GUIContent label)
        {
            LifecycleMessage(position, property, label);
        }
    }

    [CustomPropertyDrawer(typeof(SolutionSettingsProvider))]
    public class AdapterSettingsProviderUIE : InterfaceProviderUIEditor
    {
    }

    /// <summary>
    /// Experimental custom status drawer for interface providers.
    /// <para>Currently only works fully when serialized at a surface level.</para>
    /// </summary>
    [CustomPropertyDrawer(typeof(InterfaceProvider<>))]
    public class InterfaceProviderUIEditor : PropertyDrawer
    {
        public static float LINE_HEIGHT => EditorHelpers.LINE_HEIGHT;
        public static float DOWN_BIAS = 0;
        bool PlayMode => EditorApplication.isPlaying;

        protected SerializedProperty provider;

        private StringBuilder performantMessage;
        private string message = "BUG: report this";
        private Color messageColor = Color.gray;
        public void SetMessage(string msg, Color msgColor)
        {
            message = msg;
            messageColor = msgColor;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (provider == null)
            {
                provider = property.FindPropertyRelative("provider");
            }

            if (PlayMode)
            {
                return LINE_HEIGHT * 2.1f + DOWN_BIAS * 2f;
            }

            return LINE_HEIGHT * 1.1f + DOWN_BIAS * 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Color c = GUI.backgroundColor;
            GUI.backgroundColor = Color.Lerp(c, messageColor, .5f);

            float y = position.y + DOWN_BIAS;

            /*Rect labelRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
            EditorGUI.LabelField(labelRect, new GUIContent(property.displayName), EditorStyles.label);*/
            Rect propertyFieldRect = new Rect(position.x, y, position.width, LINE_HEIGHT);
            EditorGUI.PropertyField(propertyFieldRect, property.FindPropertyRelative("provider"), new GUIContent(property.displayName));

            Rect messageRect = new Rect(position.x, y + LINE_HEIGHT * 1f, position.width, LINE_HEIGHT);

            if (EditorHelpers.Check(property) && PrefabStageUtility.GetCurrentPrefabStage() == null)
            {
                EditorGUI.LabelField(propertyFieldRect,
                    new GUIContent("", "Status: " + message + "\n\nRequires: " + EditorHelpers.Type(property, "?", false)));

                // Set message.
                UpdateMessage(position, property, label);

                // Display message.
                if (message != null)
                {
                    var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    style.normal.textColor = messageColor;
                    style.fontSize = Mathf.CeilToInt(style.fontSize * 1f);
                    style.alignment = TextAnchor.MiddleLeft;
                    style.clipping = TextClipping.Clip;

                    if (PlayMode)
                        EditorGUI.LabelField(messageRect, message, style);
                }
            }
            else
            {
                if (PlayMode)
                    EditorGUI.LabelField(messageRect, "Interface provider.", EditorStyles.centeredGreyMiniLabel);
            }

            GUI.backgroundColor = c;

            EditorGUI.EndProperty();
        }

        protected virtual void UpdateMessage(Rect position, SerializedProperty property, GUIContent label)
        {
            DefaultInterfaceMessage(position, property, label);
        }

        protected void DefaultInterfaceMessage(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!EditorApplication.isPlaying) EditorHelpers.CallVoid(property, "Reset", false); // do not reset when playing.

            bool hasInterface = (bool)EditorHelpers.Get(property, "HasInterface", false, false);

            if (hasInterface)
            {
                SetMessage("Valid provider.", Color.green);
            }
            else
            {
                SetMessage("Valid provider could not be found from what is currently assigned.", Color.red);
            }
        }
        protected void LifecycleMessage(Rect position, SerializedProperty property, GUIContent label)
        {
            bool u = (bool)EditorHelpers.Get(property, "EditorOnly_HasInterfaceRaw", false, true);
            // Debug.Log(u);
            if (PlayMode && u)
            {
                IProviderLifecycle f = (IProviderLifecycle)EditorHelpers.Get(property, "Lifecycle", null, true);

                if (f != null)
                {
                    f.EDITORONLY_GetStateStringRepresentation(ref performantMessage);
                    SetMessage($"Linked: {performantMessage.ToString()}", Color.cyan);
                }
                else
                {
                    SetMessage("Could not link provider reference in editor...", Color.red);
                }

                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
            else
            {
                DefaultInterfaceMessage(position, property, label);
            }
        }
    }
}