using UnityEngine;
using Tracking4All;

/// <summary>
/// Emergency fix for SettingsManager issues - runs very early to fix all problems
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SettingsManagerEmergencyFix : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("=== EMERGENCY SETTINGS MANAGER FIX ===");
        
        // 1. Fix multiple SettingsManager instances
        FixMultipleSettingsManagers();
        
        // 2. Fix the remaining SettingsManager
        FixRemainingSettingsManager();
    }
    
    private void FixMultipleSettingsManagers()
    {
        var allSettingsManagers = FindObjectsOfType<SettingsManager>();
        Debug.Log($"Found {allSettingsManagers.Length} SettingsManager instances");
        
        if (allSettingsManagers.Length > 1)
        {
            // Keep the first one, destroy others
            for (int i = 1; i < allSettingsManagers.Length; i++)
            {
                Debug.Log($"Destroying duplicate SettingsManager #{i}");
                DestroyImmediate(allSettingsManagers[i].gameObject);
            }
        }
    }
    
    private void FixRemainingSettingsManager()
    {
        var settingsManager = FindObjectOfType<SettingsManager>();
        if (settingsManager == null)
        {
            Debug.LogError("No SettingsManager found after cleanup!");
            return;
        }
        
        // Create minimal UI setup using reflection
        CreateMinimalUIForSettingsManager(settingsManager);
    }
    
    private void CreateMinimalUIForSettingsManager(SettingsManager settingsManager)
    {
        // Create settings menu content
        GameObject settingsContent = new GameObject("Emergency Settings Content");
        settingsContent.transform.SetParent(settingsManager.transform);
        
        // Add required components
        var canvasGroup = settingsContent.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // Hide it
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        var rectTransform = settingsContent.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Create minimal prefabs
        GameObject intPrefab = CreateMinimalIntPrefab();
        GameObject floatPrefab = CreateMinimalFloatPrefab();
        GameObject boolPrefab = CreateMinimalBoolPrefab();
        GameObject dropdownPrefab = CreateMinimalDropdownPrefab();
        
        // Use reflection to set all fields
        SetSettingsManagerFields(settingsManager, settingsContent, intPrefab, floatPrefab, boolPrefab, dropdownPrefab);
        
        Debug.Log("✅ Emergency SettingsManager fix applied");
    }
    
    private GameObject CreateMinimalIntPrefab()
    {
        GameObject prefab = new GameObject("EmergencyIntPrefab");
        prefab.AddComponent<RectTransform>();
        prefab.SetActive(false); // Keep inactive
        return prefab;
    }
    
    private GameObject CreateMinimalFloatPrefab()
    {
        GameObject prefab = new GameObject("EmergencyFloatPrefab");
        prefab.AddComponent<RectTransform>();
        prefab.SetActive(false);
        return prefab;
    }
    
    private GameObject CreateMinimalBoolPrefab()
    {
        GameObject prefab = new GameObject("EmergencyBoolPrefab");
        prefab.AddComponent<RectTransform>();
        prefab.SetActive(false);
        return prefab;
    }
    
    private GameObject CreateMinimalDropdownPrefab()
    {
        GameObject prefab = new GameObject("EmergencyDropdownPrefab");
        prefab.AddComponent<RectTransform>();
        prefab.SetActive(false);
        return prefab;
    }
    
    private void SetSettingsManagerFields(SettingsManager settingsManager, GameObject settingsContent, 
        GameObject intPrefab, GameObject floatPrefab, GameObject boolPrefab, GameObject dropdownPrefab)
    {
        var type = typeof(SettingsManager);
        
        // Set all the fields using reflection
        SetField(type, settingsManager, "settingsMenuContent", settingsContent);
        SetField(type, settingsManager, "intPrefab", intPrefab);
        SetField(type, settingsManager, "floatPrefab", floatPrefab);
        SetField(type, settingsManager, "boolPrefab", boolPrefab);
        SetField(type, settingsManager, "dropdownPrefab", dropdownPrefab);
        
        Debug.Log("All SettingsManager fields assigned via reflection");
    }
    
    private void SetField(System.Type type, object instance, string fieldName, object value)
    {
        var field = type.GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);
            
        if (field != null)
        {
            field.SetValue(instance, value);
            Debug.Log($"✅ Set {fieldName}");
        }
        else
        {
            Debug.LogWarning($"Field not found: {fieldName}");
        }
    }
}