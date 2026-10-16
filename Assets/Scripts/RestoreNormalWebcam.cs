using UnityEngine;
using Tracking4All;

/// <summary>
/// Temporarily disables dual tracking and restores normal webcam functionality
/// </summary>
[DefaultExecutionOrder(-500)]
public class RestoreNormalWebcam : MonoBehaviour
{
    [Header("Emergency Mode")]
    [SerializeField] private bool disableDualTracking = true;
    [SerializeField] private bool fixSettingsManager = true;
    
    private void Awake()
    {
        if (disableDualTracking)
        {
            DisableDualTrackingComponents();
        }
        
        if (fixSettingsManager)
        {
            FixSettingsManagerIssues();
        }
    }
    
    private void DisableDualTrackingComponents()
    {
        Debug.Log("=== DISABLING DUAL TRACKING FOR NORMAL WEBCAM ===");
        
        // Disable all dual tracking components
        var dualSetups = FindObjectsOfType<DualTrackingSolutionSetup>();
        foreach (var setup in dualSetups)
        {
            setup.enabled = false;
            Debug.Log("Disabled DualTrackingSolutionSetup");
        }
        
        var networkProviders = FindObjectsOfType<NetworkCameraImageProvider>();
        foreach (var provider in networkProviders)
        {
            provider.enabled = false;
            Debug.Log("Disabled NetworkCameraImageProvider");
        }
        
        var networkBackends = FindObjectsOfType<NetworkedWebcamBackend>();
        foreach (var backend in networkBackends)
        {
            backend.enabled = false;
            Debug.Log("Disabled NetworkedWebcamBackend");
        }
    }
    
    private void FixSettingsManagerIssues()
    {
        Debug.Log("=== FIXING SETTINGS MANAGER ISSUES ===");
        
        // Remove duplicate SettingsManagers
        var settingsManagers = FindObjectsOfType<SettingsManager>();
        if (settingsManagers.Length > 1)
        {
            for (int i = 1; i < settingsManagers.Length; i++)
            {
                Debug.Log($"Destroying duplicate SettingsManager #{i}");
                DestroyImmediate(settingsManagers[i].gameObject);
            }
        }
        
        // Fix the remaining SettingsManager
        var remainingManager = FindObjectOfType<SettingsManager>();
        if (remainingManager != null)
        {
            CreateEmergencySettingsUI(remainingManager);
        }
    }
    
    private void CreateEmergencySettingsUI(SettingsManager settingsManager)
    {
        // Create invisible settings content
        GameObject settingsContent = new GameObject("EmergencySettingsContent");
        settingsContent.transform.SetParent(settingsManager.transform);
        settingsContent.SetActive(false); // Keep it invisible
        
        // Add required components
        var rectTransform = settingsContent.AddComponent<RectTransform>();
        
        // Create minimal prefabs (all inactive)
        GameObject intPrefab = CreateInvisiblePrefab("IntPrefab");
        GameObject floatPrefab = CreateInvisiblePrefab("FloatPrefab");
        GameObject boolPrefab = CreateInvisiblePrefab("BoolPrefab");
        GameObject dropdownPrefab = CreateInvisiblePrefab("DropdownPrefab");
        
        // Set via reflection
        SetSettingsField(settingsManager, "settingsMenuContent", settingsContent);
        SetSettingsField(settingsManager, "intPrefab", intPrefab);
        SetSettingsField(settingsManager, "floatPrefab", floatPrefab);
        SetSettingsField(settingsManager, "boolPrefab", boolPrefab);
        SetSettingsField(settingsManager, "dropdownPrefab", dropdownPrefab);
        
        Debug.Log("✅ Emergency SettingsManager UI created (invisible)");
    }
    
    private GameObject CreateInvisiblePrefab(string name)
    {
        GameObject prefab = new GameObject(name);
        prefab.AddComponent<RectTransform>();
        prefab.SetActive(false);
        return prefab;
    }
    
    private void SetSettingsField(SettingsManager manager, string fieldName, object value)
    {
        var field = typeof(SettingsManager).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);
            
        if (field != null)
        {
            field.SetValue(manager, value);
            Debug.Log($"✅ Set {fieldName}");
        }
        else
        {
            Debug.LogWarning($"Could not find field: {fieldName}");
        }
    }
    
    [ContextMenu("Re-enable Dual Tracking")]
    public void ReEnableDualTracking()
    {
        Debug.Log("=== RE-ENABLING DUAL TRACKING ===");
        
        var dualSetups = FindObjectsOfType<DualTrackingSolutionSetup>();
        foreach (var setup in dualSetups)
        {
            setup.enabled = true;
        }
        
        var networkProviders = FindObjectsOfType<NetworkCameraImageProvider>();
        foreach (var provider in networkProviders)
        {
            provider.enabled = true;
        }
        
        var networkBackends = FindObjectsOfType<NetworkedWebcamBackend>();
        foreach (var backend in networkBackends)
        {
            backend.enabled = true;
        }
        
        Debug.Log("✅ Dual tracking re-enabled");
    }
}