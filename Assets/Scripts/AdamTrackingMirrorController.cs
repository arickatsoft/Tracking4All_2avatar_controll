using UnityEngine;
using Tracking4All;

public class AdamTrackingMirrorController : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebugLogging = true;
    
    [Header("Tracking Settings")]
    [Tooltip("Mirror tracking for Adam when he's facing opposite to Remy")]
    public bool mirrorForAdam = true;
    
    void Start()
    {
        Debug.Log("🪞 AdamTrackingMirrorController initialized");
    }
    
    public void SetMirrorMode(bool enableMirror)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"🪞 Setting mirror mode: {enableMirror}");
        }
        
        // Find the Pose Solution that contains the solution settings
        PoseSolution poseSolution = FindObjectOfType<PoseSolution>();
        
        if (poseSolution == null)
        {
            Debug.LogError("❌ PoseSolution not found! Cannot set mirror mode.");
            return;
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"✅ Found PoseSolution: {poseSolution.gameObject.name}");
        }
        
        // Try to access the solution settings through reflection
        // Since we need to access private/internal fields
        var solutionType = poseSolution.GetType();
        
        // Look for fields that might contain solution settings
        var fields = solutionType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        
        foreach (var field in fields)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"🔍 Found field: {field.Name} of type: {field.FieldType.Name}");
            }
            
            // Look for solution settings provider or similar
            if (field.FieldType.Name.Contains("Settings") || 
                field.FieldType.Name.Contains("Provider") ||
                field.Name.Contains("settings") ||
                field.Name.Contains("provider"))
            {
                var settingsObject = field.GetValue(poseSolution);
                if (settingsObject != null)
                {
                    if (enableDebugLogging)
                    {
                        Debug.Log($"✅ Found settings object: {settingsObject.GetType().Name}");
                    }
                    
                    // Try to set mirror property
                    if (TrySetMirrorProperty(settingsObject, enableMirror))
                    {
                        Debug.Log($"✅ Successfully set mirror mode to {enableMirror}");
                        return;
                    }
                }
            }
        }
        
        // Alternative approach: Find settings through the scene hierarchy
        TryFindAndSetMirrorThroughHierarchy(enableMirror);
    }
    
    private bool TrySetMirrorProperty(object settingsObject, bool enableMirror)
    {
        var settingsType = settingsObject.GetType();
        
        // Look for Mirror property
        var mirrorProperty = settingsType.GetProperty("Mirror");
        if (mirrorProperty != null && mirrorProperty.CanWrite)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"🪞 Setting Mirror property to {enableMirror}");
            }
            mirrorProperty.SetValue(settingsObject, enableMirror);
            return true;
        }
        
        // Look for Mirror field
        var mirrorField = settingsType.GetField("Mirror", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (mirrorField != null)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"🪞 Setting Mirror field to {enableMirror}");
            }
            mirrorField.SetValue(settingsObject, enableMirror);
            return true;
        }
        
        // If this object has a provider, try to access its Mirror property
        var providerProperty = settingsType.GetProperty("Provider");
        if (providerProperty != null)
        {
            var provider = providerProperty.GetValue(settingsObject);
            if (provider != null)
            {
                return TrySetMirrorProperty(provider, enableMirror);
            }
        }
        
        return false;
    }
    
    private void TryFindAndSetMirrorThroughHierarchy(bool enableMirror)
    {
        // Look for Settings Manager or similar objects
        GameObject settingsManager = GameObject.Find("Settings Manager");
        if (settingsManager != null)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"📋 Found Settings Manager: {settingsManager.name}");
            }
            
            // Check components on settings manager
            var components = settingsManager.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (TrySetMirrorProperty(comp, enableMirror))
                {
                    Debug.Log($"✅ Successfully set mirror through Settings Manager");
                    return;
                }
            }
        }
        
        // Look for any components that might contain SolutionSettingsProvider
        MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();
        foreach (var component in allComponents)
        {
            // Check if this component has a SolutionSettingsProvider field
            var componentType = component.GetType();
            var fields = componentType.GetFields(System.Reflection.BindingFlags.Public | 
                                                System.Reflection.BindingFlags.NonPublic | 
                                                System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.FieldType.Name == "SolutionSettingsProvider")
                {
                    var provider = field.GetValue(component);
                    if (provider != null && TrySetMirrorProperty(provider, enableMirror))
                    {
                        Debug.Log($"✅ Successfully set mirror through SolutionSettingsProvider");
                        return;
                    }
                }
            }
        }
        
        Debug.LogWarning("⚠️ Could not find a way to set mirror mode. Manual setup may be required.");
    }
    
    public void EnableMirrorForAdam()
    {
        SetMirrorMode(true);
    }
    
    public void DisableMirrorForAdam()
    {
        SetMirrorMode(false);
    }
}
