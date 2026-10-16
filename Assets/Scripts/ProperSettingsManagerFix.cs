using UnityEngine;
using UnityEngine.UI;
using Tracking4All;

/// <summary>
/// Proper fix for SettingsManager using correct UI component types
/// Creates actual UI prefabs with the right components that SettingsManager expects
/// </summary>
[DefaultExecutionOrder(-2000)]
public class ProperSettingsManagerFix : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("=== PROPER SETTINGS MANAGER FIX ===");
        
        // Remove duplicates first
        RemoveDuplicateSettingsManagers();
        
        // Fix the remaining one
        var settingsManager = FindObjectOfType<SettingsManager>();
        if (settingsManager != null)
        {
            CreateProperSettingsUI(settingsManager);
        }
        else
        {
            Debug.LogError("No SettingsManager found!");
        }
    }
    
    private void RemoveDuplicateSettingsManagers()
    {
        var managers = FindObjectsOfType<SettingsManager>();
        if (managers.Length > 1)
        {
            Debug.Log($"Found {managers.Length} SettingsManager instances, removing duplicates");
            for (int i = 1; i < managers.Length; i++)
            {
                DestroyImmediate(managers[i].gameObject);
            }
        }
    }
    
    private void CreateProperSettingsUI(SettingsManager settingsManager)
    {
        // 1. Create settings menu content (GameObject)
        GameObject settingsContent = CreateSettingsMenuContent(settingsManager);
        
        // 2. Create TextInputSettingsUI for int and float
        GameObject textInputPrefab = CreateTextInputSettingsUIPrefab();
        
        // 3. Create ToggleSettingsUI for bool
        GameObject togglePrefab = CreateToggleSettingsUIPrefab();
        
        // 4. Create DropdownSettingsUI for dropdowns
        GameObject dropdownPrefab = CreateDropdownSettingsUIPrefab();
        
        // 5. Set all fields using reflection
        SetSettingsManagerField(settingsManager, "settingsMenuContent", settingsContent);
        SetSettingsManagerField(settingsManager, "intPrefab", textInputPrefab);
        SetSettingsManagerField(settingsManager, "floatPrefab", textInputPrefab);
        SetSettingsManagerField(settingsManager, "boolPrefab", togglePrefab);
        SetSettingsManagerField(settingsManager, "dropdownPrefab", dropdownPrefab);
        
        Debug.Log("✅ Proper SettingsManager UI created with correct component types");
    }
    
    private GameObject CreateSettingsMenuContent(SettingsManager settingsManager)
    {
        GameObject content = new GameObject("Settings Menu Content");
        content.transform.SetParent(settingsManager.transform);
        
        // Add CanvasGroup for UI control
        CanvasGroup canvasGroup = content.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // Keep invisible
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // Add RectTransform
        RectTransform rectTransform = content.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Add layout group
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        
        return content;
    }
    
    private GameObject CreateTextInputSettingsUIPrefab()
    {
        GameObject prefab = new GameObject("TextInputSettingsUI Prefab");
        RectTransform rectTransform = prefab.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 30);
        
        // Add the TextInputSettingsUI component
        TextInputSettingsUI textInputUI = prefab.AddComponent<TextInputSettingsUI>();
        
        // Create InputField
        InputField inputField = prefab.AddComponent<InputField>();
        inputField.contentType = InputField.ContentType.DecimalNumber;
        
        // Create Text for input display
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(prefab.transform);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        
        Text text = textObj.AddComponent<Text>();
        text.text = "";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        
        inputField.textComponent = text;
        
        // Create Label Text
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(prefab.transform);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchoredPosition = new Vector2(-100, 0);
        labelRT.sizeDelta = new Vector2(80, 30);
        
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = "Setting";
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 14;
        labelText.color = Color.white;
        
        prefab.SetActive(false); // Keep inactive as prefab
        return prefab;
    }
    
    private GameObject CreateToggleSettingsUIPrefab()
    {
        GameObject prefab = new GameObject("ToggleSettingsUI Prefab");
        RectTransform rectTransform = prefab.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 30);
        
        // Add the ToggleSettingsUI component
        ToggleSettingsUI toggleUI = prefab.AddComponent<ToggleSettingsUI>();
        
        // Create Toggle
        Toggle toggle = prefab.AddComponent<Toggle>();
        
        // Create Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(prefab.transform);
        RectTransform bgRT = background.AddComponent<RectTransform>();
        bgRT.sizeDelta = new Vector2(20, 20);
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        toggle.targetGraphic = bgImage;
        
        // Create Checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(background.transform);
        RectTransform checkRT = checkmark.AddComponent<RectTransform>();
        checkRT.sizeDelta = new Vector2(20, 20);
        
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = Color.green;
        toggle.graphic = checkImage;
        
        // Create Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(prefab.transform);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchoredPosition = new Vector2(30, 0);
        labelRT.sizeDelta = new Vector2(150, 30);
        
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = "Toggle Setting";
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 14;
        labelText.color = Color.white;
        
        prefab.SetActive(false);
        return prefab;
    }
    
    private GameObject CreateDropdownSettingsUIPrefab()
    {
        GameObject prefab = new GameObject("DropdownSettingsUI Prefab");
        RectTransform rectTransform = prefab.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 30);
        
        // Add the DropdownSettingsUI component
        DropdownSettingsUI dropdownUI = prefab.AddComponent<DropdownSettingsUI>();
        
        // Create Dropdown
        Dropdown dropdown = prefab.AddComponent<Dropdown>();
        
        // Create Label Text for dropdown
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(prefab.transform);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchoredPosition = new Vector2(0, 0);
        labelRT.sizeDelta = new Vector2(160, 30);
        
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = "Option 1";
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 14;
        labelText.color = Color.white;
        dropdown.captionText = labelText;
        
        // Create Template (required for dropdown)
        GameObject template = new GameObject("Template");
        template.transform.SetParent(prefab.transform);
        RectTransform templateRT = template.AddComponent<RectTransform>();
        templateRT.anchoredPosition = new Vector2(0, -40);
        templateRT.sizeDelta = new Vector2(160, 100);
        
        dropdown.template = templateRT;
        template.SetActive(false);
        
        prefab.SetActive(false);
        return prefab;
    }
    
    private void SetSettingsManagerField(SettingsManager manager, string fieldName, GameObject prefabObject)
    {
        var field = typeof(SettingsManager).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            // For settingsMenuContent, we can directly assign the GameObject
            if (fieldName == "settingsMenuContent")
            {
                field.SetValue(manager, prefabObject);
                Debug.Log($"✅ Set {fieldName} (GameObject)");
            }
            else
            {
                // For UI prefabs, we need to get the specific UI component
                Component uiComponent = null;
                switch (fieldName)
                {
                    case "intPrefab":
                    case "floatPrefab":
                        uiComponent = prefabObject.GetComponent<TextInputSettingsUI>();
                        break;
                    case "boolPrefab":
                        uiComponent = prefabObject.GetComponent<ToggleSettingsUI>();
                        break;
                    case "dropdownPrefab":
                        uiComponent = prefabObject.GetComponent<DropdownSettingsUI>();
                        break;
                }
                
                if (uiComponent != null)
                {
                    field.SetValue(manager, uiComponent);
                    Debug.Log($"✅ Set {fieldName} ({uiComponent.GetType().Name})");
                }
                else
                {
                    Debug.LogError($"Could not find required UI component for {fieldName}");
                }
            }
        }
        else
        {
            Debug.LogError($"Could not find field: {fieldName}");
        }
    }
}