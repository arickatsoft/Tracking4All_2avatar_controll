// EditorFrameworkIntroWindow
// (C) 2024-2025 G8gaming Ltd.
using UnityEditor;
using UnityEngine;

namespace Tracking4All
{

    public class EditorFrameworkIntroWindow : EditorWindow
    {
        private const string StartupWindowShownAndAgreedKey = "Tracking4AllStartupShownAndAgreed_3_" + version;

        private const float windowWidth = 430f;
        private const float windowHeight = 380f;

        private const string version = "1.0.8";
        private const string windowTitle = "Tracking4All v" + version;
        private const string welcomeMessage = "Thank you for purchasing Tracking4All :)";
        private const string descriptionText =
            "Documentation and changelogs are available in the Assets/Tracking4All folder.\n\n" +
            "You can quick start by running one of the examples in Assets/Tracking4All/Scenes.\n\n" +
            "Reminder that your License to use Tracking4All is governed by the EULA at \"https://www.tracking4all.com/eula\". " +
            "Use or access of the framework constitutes agreement to the license terms/EULA.\n\n" +
            "Any questions: support@g8gaming.ca";
        private const string closeButtonText = "Ok";
        private const string hyperlinkText = "EULA";
        private const string hyperlinkURL = "https://www.tracking4all.com/eula";

        private const int titleFontSize = 16;
        private const int descriptionFontSize = 13;
        private const int buttonFontSize = 14;
        private const int buttonHeight = 30;
        private const int buttonWidth = 100;

        private static readonly Color hyperlinkNormalColor = new Color(0.2f, 0.5f, 1.0f);  // Bright blue
        private static readonly Color hyperlinkHoverColor = new Color(0.3f, 0.7f, 1.0f);   // Lighter blue on hover

        private static bool reoccuring;
        private static bool isAgreed = false;


        private static bool Agreed()
        {
            return EditorPrefs.GetBool(StartupWindowShownAndAgreedKey, false);
        }

        [InitializeOnLoadMethod]
        private static void ShowWindowOnStartup()
        {
            if (!Agreed())
            {
                EditorApplication.update += ShowOnce;
            }
        }
        private void OnDestroy()
        {
            if (isAgreed)
            {
                EditorPrefs.SetBool(StartupWindowShownAndAgreedKey, true);
            }

            if (!Agreed())
            {
                reoccuring = true;
                ShowWindowOnStartup();
            }
        }

        private static void ShowOnce()
        {
            var window = GetWindow<EditorFrameworkIntroWindow>(windowTitle);

            float screenWidth = Screen.currentResolution.width;
            float screenHeight = Screen.currentResolution.height;
            window.position = new Rect((screenWidth - windowWidth) / 2, (screenHeight - windowHeight) / 2, windowWidth, windowHeight);

            window.minSize = new Vector2(windowWidth, windowHeight);
            window.maxSize = new Vector2(windowWidth, windowHeight);

            EditorApplication.update -= ShowOnce;
        }

        private void OnGUI()
        {
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);

            GUILayout.Space(15);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = titleFontSize,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 10, 10)
            };
            GUILayout.Label(welcomeMessage, titleStyle);

            GUIStyle descriptionStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = descriptionFontSize,
                padding = new RectOffset(15, 15, 5, 10),
            };
            GUILayout.Label(descriptionText, descriptionStyle);

            GUILayout.Space(10);

            GUIStyle hyperlinkStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = hyperlinkNormalColor },
                fontSize = descriptionFontSize,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 10)
            };

            Rect hyperlinkRect = GUILayoutUtility.GetRect(new GUIContent(hyperlinkText), hyperlinkStyle);
            EditorGUIUtility.AddCursorRect(hyperlinkRect, MouseCursor.Link);

            if (hyperlinkRect.Contains(Event.current.mousePosition))
            {
                hyperlinkStyle.normal.textColor = hyperlinkHoverColor;
                if (Event.current.type == EventType.MouseDown)
                {
                    Application.OpenURL(hyperlinkURL);
                }
            }

            GUI.Label(hyperlinkRect, hyperlinkText, hyperlinkStyle);

            // GUILayout.Space(10);

            /*// Add a centered checkbox
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUIStyle checkboxStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = descriptionFontSize,
                padding = new RectOffset(15, 15, 5, 5),
            };
            checkboxStyle.normal.textColor = reoccuring ? Color.red : checkboxStyle.normal.textColor;
            isAgreed = GUILayout.Toggle(isAgreed, " I Have Read and Agree to the EULA", checkboxStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);*/
            isAgreed = true;

            // "OK" button becomes interactable only if the checkbox is checked
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = buttonFontSize,
                fixedHeight = buttonHeight,
                fixedWidth = buttonWidth,
                margin = new RectOffset(0, 0, 10, 10)
            };

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = isAgreed; // Enable or disable the button based on checkbox state
            if (GUILayout.Button(closeButtonText, buttonStyle))
            {
                Close();
            }
            GUI.enabled = true; // Reset GUI.enabled to ensure other GUI elements are not affected
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}