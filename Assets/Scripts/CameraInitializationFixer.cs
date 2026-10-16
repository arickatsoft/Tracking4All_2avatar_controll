using UnityEngine;
using Tracking4All;

/// <summary>
/// Fixes camera initialization issues that prevent NetworkedWebcamBackend from starting
/// Addresses the SettingsManager NullReferenceException and ensures proper camera setup
/// </summary>
public class CameraInitializationFixer : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool debugOnStart = true;
    [SerializeField] private bool attemptAutoFix = true;
    
    private void Start()
    {
        if (debugOnStart)
        {
            Invoke(nameof(DiagnoseCameraIssues), 1f);
        }
        
        if (attemptAutoFix)
        {
            Invoke(nameof(AttemptCameraFix), 2f);
        }
    }
    
    [ContextMenu("Diagnose Camera Issues")]
    public void DiagnoseCameraIssues()
    {
        Debug.Log("=== CAMERA INITIALIZATION DIAGNOSIS ===");
        
        // Check SettingsManager
        CheckSettingsManager();
        
        // Check WebCam availability
        CheckWebCamAvailability();
        
        // Check NetworkedWebcamBackend status
        CheckNetworkedWebcamBackend();
        
        // Check Tracking4All core
        CheckTracking4AllCore();
    }
    
    private void CheckSettingsManager()
    {
        Debug.Log("--- SettingsManager Check ---");
        
        var settingsManager = FindObjectOfType<SettingsManager>();
        if (settingsManager == null)
        {
            Debug.LogWarning("⚠️ SettingsManager not found in scene");
            Debug.LogWarning("This might cause NullReferenceException in WebCamSettings");
            
            // Try to create one
            CreateSettingsManager();
        }
        else
        {
            Debug.Log("✅ SettingsManager found");
        }
    }
    
    private void CreateSettingsManager()
    {
        Debug.Log("Creating SettingsManager...");
        
        // Look for existing Settings UI or create minimal setup
        GameObject settingsGO = new GameObject("SettingsManager");
        SettingsManager settingsManager = settingsGO.AddComponent<SettingsManager>();
        
        Debug.Log("✅ SettingsManager created");
    }
    
    private void CheckWebCamAvailability()
    {
        Debug.Log("--- WebCam Availability Check ---");
        
        WebCamDevice[] devices = WebCamTexture.devices;
        Debug.Log($"Available cameras: {devices.Length}");
        
        if (devices.Length == 0)
        {
            Debug.LogError("❌ No camera devices found!");
            Debug.LogError("SOLUTION: Ensure camera permission granted and camera available");
            return;
        }
        
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"Camera {i}: {devices[i].name} (Front: {devices[i].isFrontFacing})");
        }
    }
    
private void CheckNetworkedWebcamBackend()
    {
        Debug.Log("--- NetworkedWebcamBackend Status ---");
        
        var backends = FindObjectsOfType<NetworkedWebcamBackend>();
        Debug.Log($"NetworkedWebcamBackend components found: {backends.Length}");
        
        foreach (var backend in backends)
        {
            Debug.Log($"Backend: {backend.name}");
            Debug.Log($"  GameObject Active: {backend.gameObject.activeInHierarchy}");
            Debug.Log($"  Component Enabled: {backend.enabled}");
            
            if (backend.Lifecycle != null)
            {
                Debug.Log($"  Lifecycle Ready: {backend.Lifecycle.IsReady(0)}");
                Debug.Log($"  Lifecycle Disposed: {backend.Lifecycle.IsDisposed(0)}");
                Debug.Log($"  Lifecycle Alive: {backend.Lifecycle.IsAlive(0)}");
            }
            else
            {
                Debug.LogWarning("  ⚠️ Lifecycle is null");
            }
        }
    }
    
    private void CheckTracking4AllCore()
    {
        Debug.Log("--- Tracking4All Core Check ---");
        
        var tracking4All = FindObjectOfType<Tracking4All.Tracking4All>();
        if (tracking4All == null)
        {
            Debug.LogWarning("⚠️ Tracking4All core manager not found");
            Debug.LogWarning("This is required for proper provider initialization");
            
            // Create minimal Tracking4All setup
            CreateTracking4AllCore();
        }
        else
        {
            Debug.Log("✅ Tracking4All core found");
        }
    }
    
    private void CreateTracking4AllCore()
    {
        Debug.Log("Creating minimal Tracking4All core...");
        
        GameObject coreGO = new GameObject("Tracking4All");
        var core = coreGO.AddComponent<Tracking4All.Tracking4All>();
        
        Debug.Log("✅ Tracking4All core created");
    }
    
    [ContextMenu("Attempt Camera Fix")]
    public void AttemptCameraFix()
    {
        Debug.Log("=== ATTEMPTING CAMERA FIX ===");
        
        // Step 1: Ensure core components exist
        EnsureCoreComponents();
        
        // Step 2: Reset NetworkedWebcamBackend
        ResetNetworkedWebcamBackend();
        
        // Step 3: Force camera re-initialization
        ForceWebCamReinit();
        
        Debug.Log("=== CAMERA FIX ATTEMPT COMPLETE ===");
    }
    
    private void EnsureCoreComponents()
    {
        Debug.Log("Ensuring core components...");
        
        // Ensure Tracking4All core exists
        if (FindObjectOfType<Tracking4All.Tracking4All>() == null)
        {
            CreateTracking4AllCore();
        }
        
        // Ensure SettingsManager exists
        if (FindObjectOfType<SettingsManager>() == null)
        {
            CreateSettingsManager();
        }
    }
    
    private void ResetNetworkedWebcamBackend()
    {
        Debug.Log("Resetting NetworkedWebcamBackend...");
        
        var backends = FindObjectsOfType<NetworkedWebcamBackend>();
        foreach (var backend in backends)
        {
            // Disable and re-enable to force reinitialization
            backend.enabled = false;
            
            // Wait a frame then re-enable
            StartCoroutine(ReEnableBackend(backend));
        }
    }
    
    private System.Collections.IEnumerator ReEnableBackend(NetworkedWebcamBackend backend)
    {
        yield return null; // Wait one frame
        
        if (backend != null)
        {
            backend.enabled = true;
            Debug.Log($"Re-enabled NetworkedWebcamBackend: {backend.name}");
        }
    }
    
    private void ForceWebCamReinit()
    {
        Debug.Log("Attempting to force WebCam reinitialization...");
        
        // Look for any WebCamTexture instances and restart them
        WebCamTexture[] webCams = FindObjectsOfType<WebCamTexture>();
        Debug.Log($"Found {webCams.Length} WebCamTexture instances");
        
        foreach (var webCam in webCams)
        {
            if (webCam.isPlaying)
            {
                Debug.Log($"Restarting WebCam: {webCam.deviceName}");
                webCam.Stop();
                webCam.Play();
            }
        }
    }
    
    [ContextMenu("Show Quick Fixes")]
    public void ShowQuickFixes()
    {
        Debug.Log("=== QUICK FIXES FOR CAMERA ISSUES ===");
        Debug.Log("");
        Debug.Log("1. SETTINGS MANAGER NULL REFERENCE:");
        Debug.Log("   - Add SettingsManager component to scene");
        Debug.Log("   - Or disable WebCamSettings registration temporarily");
        Debug.Log("");
        Debug.Log("2. CAMERA PERMISSIONS (Mac):");
        Debug.Log("   - System Preferences → Security & Privacy → Camera");
        Debug.Log("   - Enable camera access for Unity");
        Debug.Log("");
        Debug.Log("3. UNITY SIMULATOR LIMITATIONS:");
        Debug.Log("   - Mac Unity Editor might have camera access issues");
        Debug.Log("   - Test on actual Android device for best results");
        Debug.Log("");
        Debug.Log("4. ALTERNATIVE TESTING:");
        Debug.Log("   - Skip local Unity testing");
        Debug.Log("   - Build directly to Android and test there");
        Debug.Log("   - Use Android device as host, Mac as Python client");
        Debug.Log("");
        Debug.Log("5. BYPASS CAMERA FOR NETWORK TESTING:");
        Debug.Log("   - Temporarily disable camera requirements");
        Debug.Log("   - Test just the network connection (port 52730)");
    }
}