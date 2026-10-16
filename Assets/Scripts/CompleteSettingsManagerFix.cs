using UnityEngine;
using UnityEngine.UI;
using Tracking4All;

/// <summary>
/// Complete fix for SettingsManager - creates all required UI elements
/// Based on screenshot showing: Settings Menu Content, Dropdown Prefab, Floor Prefab, Int Prefab, Bool Prefab
/// </summary>
public class CompleteSettingsManagerFix : MonoBehaviour
{
    [Header("Auto Fix All Settings UI")]
    [SerializeField] private bool fixOnStart = true;
    
    private void Start()
    {
        if (fixOnStart)
        {
            FixAllSettingsManager();
        }
    }
    
    [ContextMenu("Fix All SettingsManager")]
    public void FixAllSettingsManager()
    {
        Debug.Log("=== COMPLETE SETTINGS MANAGER FIX ===");
        
        var settingsManager = FindObjectOfType<SettingsManager>();
        if (settingsManager == null)
        {
            Debug.LogError("SettingsManager not found!");
            return;
        }
        
        // Create all required UI elements
        CreateCompleteSettingsUI(settingsManager);
        
        Debug.Log("✅ SettingsManager completely fixed");
    }
    
    private void CreateCompleteSettingsUI(SettingsManager settingsManager)
    {
        // 1. Settings Menu Content
        GameObject settingsContent = CreateSettingsMenuContent(settingsManager);
        
        // 2. Create prefab containers
        GameObject dropdownPrefab = CreateDropdownPrefab();
        GameObject floorPrefab = CreateFloorPrefab();
        GameObject intPrefab = CreateIntPrefab();
        GameObject boolPrefab = CreateBoolPrefab();
        
        // 3. Use reflection to set all fields
        SetAllSettingsFields(settingsManager, settingsContent, dropdownPrefab, floorPrefab, intPrefab, boolPrefab);
        
        Debug.Log("Created complete settings UI system");
    }
    
    private GameObject CreateSettingsMenuContent(SettingsManager settingsManager)
    {
        GameObject settingsContent = new GameObject("Settings Menu Content");
        settingsContent.transform.SetParent(settingsManager.transform);
        
        // Add Canvas Group for UI management
        CanvasGroup canvasGroup = settingsContent.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // Add RectTransform
        RectTransform rectTransform = settingsContent.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Add Vertical Layout Group for organization
        VerticalLayoutGroup layout = settingsContent.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(20, 20, 20, 20);
        
        return settingsContent;
    }
    
    private GameObject CreateDropdownPrefab()
    {
        GameObject prefab = new GameObject("Dropdown Prefab");
        prefab.AddComponent<RectTransform>();
        
        // Add dropdown component
        Dropdown dropdown = prefab.AddComponent<Dropdown>();
        
        // Create template
        GameObject template = new GameObject("Template");
        template.transform.SetParent(prefab.transform);
        RectTransform templateRT = template.AddComponent<RectTransform>();
        templateRT.anchoredPosition = new Vector2(0, -30);
        templateRT.sizeDelta = new Vector2(160, 150);
        
        dropdown.template = templateRT;
        
        return prefab;
    }
    
    private GameObject CreateFloorPrefab()
    {
        GameObject prefab = new GameObject("Floor Prefab");
        RectTransform rt = prefab.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 30);
        
        // Add text component
        Text text = prefab.AddComponent<Text>();
        text.text = "Floor Setting";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        
        return prefab;
    }
    
    private GameObject CreateIntPrefab()
    {
        GameObject prefab = new GameObject("Int Prefab");
        RectTransform rt = prefab.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 30);
        
        // Add InputField for integer input
        InputField inputField = prefab.AddComponent<InputField>();
        inputField.contentType = InputField.ContentType.IntegerNumber;
        
        // Add Text component for display
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(prefab.transform);
        Text text = textObj.AddComponent<Text>();
        text.text = "0";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        inputField.textComponent = text;
        
        return prefab;
    }
    
    private GameObject CreateBoolPrefab()
    {
        GameObject prefab = new GameObject("Bool Prefab");
        RectTransform rt = prefab.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 30);
        
        // Add Toggle component
        Toggle toggle = prefab.AddComponent<Toggle>();
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(prefab.transform);
        RectTransform bgRT = background.AddComponent<RectTransform>();
        bgRT.sizeDelta = new Vector2(20, 20);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = Color.white;
        toggle.targetGraphic = bgImage;
        
        // Create checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(background.transform);
        RectTransform checkRT = checkmark.AddComponent<RectTransform>();
        checkRT.sizeDelta = new Vector2(20, 20);
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = Color.green;
        toggle.graphic = checkImage;
        
        return prefab;
    }
    
    private void SetAllSettingsFields(SettingsManager settingsManager, GameObject settingsContent, 
        GameObject dropdownPrefab, GameObject floorPrefab, GameObject intPrefab, GameObject boolPrefab)
    {
        var settingsType = typeof(SettingsManager);
        
        // Set Settings Menu Content
        SetField(settingsManager, settingsType, "settingsMenuContent", settingsContent);
        
        // Set all prefab fields
        SetField(settingsManager, settingsType, "dropdownPrefab", dropdownPrefab);
        SetField(settingsManager, settingsType, "floorPrefab", floorPrefab);
        SetField(settingsManager, settingsType, "intPrefab", intPrefab);
        SetField(settingsManager, settingsType, "boolPrefab", boolPrefab);
        
        Debug.Log("✅ All SettingsManager fields assigned");
    }
    
    private void SetField(SettingsManager settingsManager, System.Type type, string fieldName, GameObject value)
    {
        var field = type.GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(settingsManager, value);
            Debug.Log($"✅ Set {fieldName}");
        }
        else
        {
            Debug.LogWarning($"Could not find field: {fieldName}");
            
            // Try SerializedProperty method in editor
            #if UNITY_EDITOR
            TrySetViaSerializedProperty(settingsManager, fieldName, value);
            #endif
        }
    }
    
    #if UNITY_EDITOR
    private void TrySetViaSerializedProperty(SettingsManager settingsManager, string propertyName, GameObject value)
    {
        var serializedObject = new UnityEditor.SerializedObject(settingsManager);
        var property = serializedObject.FindProperty(propertyName);
        
        if (property != null)
        {
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
            Debug.Log($"✅ Set {propertyName} via SerializedProperty");
        }
        else
        {
            Debug.LogWarning($"Property {propertyName} not found - manual assignment needed");
        }
    }
    #endif
}