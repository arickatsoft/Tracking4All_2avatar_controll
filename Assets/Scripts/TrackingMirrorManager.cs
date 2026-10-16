using UnityEngine;
using Tracking4All;

public class TrackingMirrorManager : MonoBehaviour
{
    [Header("Mirror Settings")]
    [Tooltip("Enable this to automatically set mirror mode when Adam is selected")]
    public bool autoMirrorForAdam = true;
    
    [Header("Debug Settings")]
    public bool enableDebugLogging = true;
    
    private PoseSolution poseSolution;
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeMirrorManager();
    }
    
    void InitializeMirrorManager()
    {
        // Find the PoseSolution in the scene
        poseSolution = FindObjectOfType<PoseSolution>();
        
        if (poseSolution != null)
        {
            isInitialized = true;
            LogDebug($"✅ TrackingMirrorManager initialized with PoseSolution: {poseSolution.gameObject.name}");
        }
        else
        {
            LogDebug("❌ PoseSolution not found! Mirror functionality will not work.");
        }
    }
    
    public void SetMirrorMode(bool enableMirror)
    {
        if (!isInitialized)
        {
            InitializeMirrorManager();
        }
        
        if (!isInitialized)
        {
            LogDebug("❌ Cannot set mirror mode - TrackingMirrorManager not initialized");
            return;
        }
        
        LogDebug($"🪞 Setting mirror mode: {enableMirror}");
        
        // Try multiple approaches to set the mirror property
        bool success = false;
        
        // Approach 1: Try to access through component fields using reflection
        success = TrySetMirrorViaReflection(poseSolution, enableMirror);
        
        if (!success)
        {
            // Approach 2: Look for Settings Manager or similar objects
            success = TrySetMirrorViaSettingsManager(enableMirror);
        }
        
        if (!success)
        {
            // Approach 3: Try to find SolutionSettingsProvider components directly
            success = TrySetMirrorViaSettingsProviders(enableMirror);
        }
        
        if (!success)
        {
            // Approach 4: Try to find MirrorableTransformTargets
            success = TrySetMirrorViaMirrorableTargets(enableMirror);
        }
        
        if (success)
        {
            LogDebug($"✅ Successfully set mirror mode to {enableMirror}");
        }
        else
        {
            LogDebug($"❌ Failed to set mirror mode. You may need to manually configure mirror settings in the Tracking4All settings.");
        }
    }
    
    private bool TrySetMirrorViaReflection(PoseSolution solution, bool enableMirror)
    {
        try
        {
            var solutionType = solution.GetType();
            var fields = solutionType.GetFields(System.Reflection.BindingFlags.NonPublic | 
                                              System.Reflection.BindingFlags.Instance | 
                                              System.Reflection.BindingFlags.Public);
            
            foreach (var field in fields)
            {
                LogDebug($"🔍 Checking field: {field.Name} of type: {field.FieldType.Name}");
                
                // Look for settings-related fields
                if (field.FieldType.Name.Contains("Settings") || 
                    field.FieldType.Name.Contains("Provider") ||
                    field.Name.Contains("settings") ||
                    field.Name.Contains("provider"))
                {
                    var settingsObject = field.GetValue(solution);
                    if (settingsObject != null && TrySetMirrorProperty(settingsObject, enableMirror))
                    {
                        return true;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            LogDebug($"⚠️ Reflection approach failed: {e.Message}");
        }
        
        return false;
    }
    
    private bool TrySetMirrorViaSettingsManager(bool enableMirror)
    {
        GameObject settingsManager = GameObject.Find("Settings Manager");
        if (settingsManager != null)
        {
            LogDebug($"📋 Found Settings Manager: {settingsManager.name}");
            
            var components = settingsManager.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (TrySetMirrorProperty(comp, enableMirror))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private bool TrySetMirrorViaSettingsProviders(bool enableMirror)
    {
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
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    private bool TrySetMirrorViaMirrorableTargets(bool enableMirror)
    {
        // Look for MirrorableTransformTargets components
        var mirrorableTargets = FindObjectsOfType<MirrorableTransformTargets>();
        foreach (var target in mirrorableTargets)
        {
            LogDebug($"🎯 Found MirrorableTransformTargets: {target.gameObject.name}");
            
            // Try to access the adapterSettingsProvider field
            var targetType = target.GetType();
            var providerField = targetType.GetField("adapterSettingsProvider", 
                                                   System.Reflection.BindingFlags.NonPublic | 
                                                   System.Reflection.BindingFlags.Instance | 
                                                   System.Reflection.BindingFlags.Public);
            
            if (providerField != null)
            {
                var provider = providerField.GetValue(target);
                if (provider != null && TrySetMirrorProperty(provider, enableMirror))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private bool TrySetMirrorProperty(object targetObject, bool enableMirror)
    {
        if (targetObject == null) return false;
        
        var targetType = targetObject.GetType();
        
        // Try property first
        var mirrorProperty = targetType.GetProperty("Mirror");
        if (mirrorProperty != null && mirrorProperty.CanWrite)
        {
            LogDebug($"🪞 Setting Mirror property on {targetType.Name}");
            mirrorProperty.SetValue(targetObject, enableMirror);
            return true;
        }
        
        // Try field
        var mirrorField = targetType.GetField("Mirror", System.Reflection.BindingFlags.Public | 
                                                        System.Reflection.BindingFlags.NonPublic | 
                                                        System.Reflection.BindingFlags.Instance);
        if (mirrorField != null)
        {
            LogDebug($"🪞 Setting Mirror field on {targetType.Name}");
            mirrorField.SetValue(targetObject, enableMirror);
            return true;
        }
        
        // Check if this object has a Provider property and try recursively
        var providerProperty = targetType.GetProperty("Provider");
        if (providerProperty != null)
        {
            var provider = providerProperty.GetValue(targetObject);
            if (provider != null)
            {
                return TrySetMirrorProperty(provider, enableMirror);
            }
        }
        
        return false;
    }
    
    public void EnableMirrorForAdam()
    {
        SetMirrorMode(true);
    }
    
    public void DisableMirrorForRemy()
    {
        SetMirrorMode(false);
    }
    
    public void OnAdamSelected()
    {
        if (autoMirrorForAdam)
        {
            LogDebug("🔄 Adam selected - enabling mirror mode");
            EnableMirrorForAdam();
        }
    }
    
    public void OnRemySelected()
    {
        if (autoMirrorForAdam)
        {
            LogDebug("🔄 Remy selected - disabling mirror mode");
            DisableMirrorForRemy();
        }
    }
    
    private void LogDebug(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[TrackingMirrorManager] {message}");
        }
    }
}
