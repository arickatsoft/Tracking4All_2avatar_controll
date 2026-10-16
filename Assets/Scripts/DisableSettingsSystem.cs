using System.Linq;
using UnityEngine;
using Tracking4All;

/// <summary>
/// Completely disables the settings system to restore normal webcam functionality
/// This prevents all SettingsManager errors by removing the problematic components
/// </summary>
[DefaultExecutionOrder(-3000)]
public class DisableSettingsSystem : MonoBehaviour
{
    [Header("Emergency Disable")]
    [SerializeField] private bool disableAllSettings = true;
    
    private void Awake()
    {
        if (disableAllSettings)
        {
            Debug.Log("=== DISABLING ENTIRE SETTINGS SYSTEM ===");
            DisableAllSettingsComponents();
        }
    }
    
    private void DisableAllSettingsComponents()
    {
        // Disable all SettingsManager instances
        var settingsManagers = FindObjectsOfType<SettingsManager>();
        foreach (var manager in settingsManagers)
        {
            manager.enabled = false;
            Debug.Log("Disabled SettingsManager");
        }
        
        // Destroy all dual tracking components
        DestroyDualTrackingComponents();
        
        // Disable any other problematic components
        DisableProblematicComponents();
        
        Debug.Log("✅ Settings system completely disabled - normal webcam should work");
    }
    
    private void DestroyDualTrackingComponents()
    {
        // Remove dual tracking setup
        var dualSetups = FindObjectsOfType<DualTrackingSolutionSetup>();
        foreach (var setup in dualSetups)
        {
            DestroyImmediate(setup.gameObject);
            Debug.Log("Destroyed DualTrackingSolutionSetup");
        }
        
        // Remove network camera providers
        var networkProviders = FindObjectsOfType<NetworkCameraImageProvider>();
        foreach (var provider in networkProviders)
        {
            DestroyImmediate(provider.gameObject);
            Debug.Log("Destroyed NetworkCameraImageProvider");
        }
        
        // Remove network webcam backends
        var networkBackends = FindObjectsOfType<NetworkedWebcamBackend>();
        foreach (var backend in networkBackends)
        {
            DestroyImmediate(backend.gameObject);
            Debug.Log("Destroyed NetworkedWebcamBackend");
        }
        
        // Remove any other fix scripts
        var otherFixes = FindObjectsOfType<RestoreNormalWebcam>();
        foreach (var fix in otherFixes)
        {
            DestroyImmediate(fix.gameObject);
        }
        
        var properFixes = FindObjectsOfType<ProperSettingsManagerFix>();
        foreach (var fix in properFixes)
        {
            DestroyImmediate(fix.gameObject);
        }
    }
    
    private void DisableProblematicComponents()
    {
        // Find and log what's causing the settings to register
        var unityWebCamBackends = FindObjectsOfType<UnityWebCamBackend>();
        Debug.Log($"Found {unityWebCamBackends.Length} UnityWebCamBackend components");
        
        // Don't disable UnityWebCamBackend as that's needed for the webcam
        // Just make sure SettingsManager is disabled so it can't register settings
        
        Debug.Log("Settings registration should be blocked now");
    }
    
    [ContextMenu("Force Clean Scene")]
    public void ForceCleanScene()
    {
        Debug.Log("=== FORCE CLEANING SCENE ===");
        
        // Remove ALL settings-related components
        var settingsManagers = FindObjectsOfType<SettingsManager>();
        foreach (var sm in settingsManagers)
        {
            DestroyImmediate(sm.gameObject);
        }
        
        // Remove all fix scripts
        var allFixScripts = FindObjectsOfType<MonoBehaviour>().Where(mb => 
            mb.GetType().Name.Contains("Fix") || 
            mb.GetType().Name.Contains("Settings") ||
            mb.GetType().Name.Contains("Dual"));
            
        foreach (var script in allFixScripts)
        {
            if (script != this) // Don't destroy self
            {
                DestroyImmediate(script.gameObject);
                Debug.Log($"Destroyed {script.GetType().Name}");
            }
        }
        
        Debug.Log("✅ Scene forcibly cleaned");
    }
}