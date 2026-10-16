using UnityEngine;
using Tracking4All;

/// <summary>
/// Creates minimal UI setup for SettingsManager to prevent UnassignedReferenceException
/// This allows NetworkedWebcamBackend to initialize without full settings UI
/// </summary>
public class MinimalSettingsManagerFix : MonoBehaviour
{
    [Header("Auto Fix")]
    [SerializeField] private bool fixOnStart = true;
    
    private void Start()
    {
        if (fixOnStart)
        {
            FixSettingsManager();
        }
    }
    
    [ContextMenu("Fix SettingsManager")]
    public void FixSettingsManager()
    {
        Debug.Log("=== FIXING SETTINGS MANAGER ===");
        
        var settingsManager = FindObjectOfType<SettingsManager>();
        if (settingsManager == null)
        {
            Debug.LogError("SettingsManager not found!");
            return;
        }
        
        // Create minimal UI container for settings
        CreateMinimalSettingsUI(settingsManager);
        
        Debug.Log("✅ SettingsManager fixed with minimal UI");
    }
    
    private void CreateMinimalSettingsUI(SettingsManager settingsManager)
    {
        // Create a simple GameObject to act as settings menu content
        GameObject settingsContent = new GameObject("Settings Menu Content");
        settingsContent.transform.SetParent(settingsManager.transform);
        
        // Add RectTransform for UI compatibility
        RectTransform rectTransform = settingsContent.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Use reflection to set the settingsMenuContent field
        SetSettingsMenuContent(settingsManager, settingsContent);
        
        Debug.Log("Created minimal settings UI container");
    }
    
    private void SetSettingsMenuContent(SettingsManager settingsManager, GameObject content)
    {
        // Use reflection to access the private settingsMenuContent field
        var field = typeof(SettingsManager).GetField("settingsMenuContent", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(settingsManager, content);
            Debug.Log("✅ Set settingsMenuContent via reflection");
        }
        else
        {
            Debug.LogWarning("Could not find settingsMenuContent field");
            
            // Alternative: Try to set it through serialized properties
            SetViaSerializedProperty(settingsManager, content);
        }
    }
    
    private void SetViaSerializedProperty(SettingsManager settingsManager, GameObject content)
    {
        #if UNITY_EDITOR
        var serializedObject = new UnityEditor.SerializedObject(settingsManager);
        var property = serializedObject.FindProperty("settingsMenuContent");
        
        if (property != null)
        {
            property.objectReferenceValue = content;
            serializedObject.ApplyModifiedProperties();
            Debug.Log("✅ Set settingsMenuContent via SerializedProperty");
        }
        else
        {
            Debug.LogWarning("Could not set settingsMenuContent - manual assignment needed");
        }
        #else
        Debug.LogWarning("SerializedProperty method only works in editor");
        #endif
    }
}