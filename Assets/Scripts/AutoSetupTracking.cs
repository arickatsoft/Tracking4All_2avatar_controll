using UnityEngine;

/// <summary>
/// Automatic setup script that creates and configures all required components
/// for proper Adam/Remy camera and tracking switching
/// </summary>
public class AutoSetupTracking : MonoBehaviour
{
    [Header("Setup Options")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupTrackingSystem();
        }
    }
    
    [ContextMenu("Setup Tracking System")]
    public void SetupTrackingSystem()
    {
        if (enableDebugLogs) Debug.Log("AutoSetupTracking: Starting automatic setup...");
        
        // Step 1: Ensure TrackingTargetManager exists
        TrackingTargetManager trackingManager = SetupTrackingTargetManager();
        
        // Step 2: Ensure CameraManager is properly configured
        CameraManager cameraManager = SetupCameraManager(trackingManager);
        
        // Step 3: Ensure avatars have proper components
        SetupAvatarComponents();
        
        // Step 4: Connect UI buttons if they exist
        ConnectUIButtons(cameraManager, trackingManager);
        
        if (enableDebugLogs) Debug.Log("AutoSetupTracking: ✅ Setup complete!");
    }
    
    private TrackingTargetManager SetupTrackingTargetManager()
    {
        // Look for existing TrackingTargetManager
        TrackingTargetManager trackingManager = FindObjectOfType<TrackingTargetManager>();
        
        if (trackingManager == null)
        {
            // Create new GameObject for TrackingTargetManager
            GameObject trackingManagerGO = new GameObject("TrackingTargetManager");
            trackingManager = trackingManagerGO.AddComponent<TrackingTargetManager>();
            
            if (enableDebugLogs) Debug.Log("AutoSetupTracking: Created TrackingTargetManager");
        }
        else
        {
            if (enableDebugLogs) Debug.Log("AutoSetupTracking: Found existing TrackingTargetManager");
        }
        
        return trackingManager;
    }
    
    private CameraManager SetupCameraManager(TrackingTargetManager trackingManager)
    {
        // Look for existing CameraManager
        CameraManager cameraManager = FindObjectOfType<CameraManager>();
        
        if (cameraManager == null)
        {
            // Create new GameObject for CameraManager
            GameObject cameraManagerGO = new GameObject("CameraManager");
            cameraManager = cameraManagerGO.AddComponent<CameraManager>();
            
            if (enableDebugLogs) Debug.Log("AutoSetupTracking: Created CameraManager");
        }
        else
        {
            if (enableDebugLogs) Debug.Log("AutoSetupTracking: Found existing CameraManager");
        }
        
        // Try to connect the tracking manager via reflection since it's a private field
        // This is a workaround for the setup - in practice you'd set this in the inspector
        var trackingField = typeof(CameraManager).GetField("trackingTargetManager", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (trackingField != null)
        {
            trackingField.SetValue(cameraManager, trackingManager);
            if (enableDebugLogs) Debug.Log("AutoSetupTracking: Connected TrackingTargetManager to CameraManager");
        }
        
        return cameraManager;
    }
    
    private void SetupAvatarComponents()
    {
        // Setup Remy
        GameObject remy = GameObject.Find("Remy");
        if (remy != null)
        {
            EnsureAvatarComponents(remy, "Remy");
        }
        
        // Setup Adam
        GameObject adam = GameObject.Find("Adam_Big");
        if (adam != null)
        {
            EnsureAvatarComponents(adam, "Adam");
        }
    }
    
    private void EnsureAvatarComponents(GameObject avatar, string avatarName)
    {
        // Check for Tracking4All.Avatar component
        var avatarComponent = avatar.GetComponent<Tracking4All.Avatar>();
        if (avatarComponent == null)
        {
            avatarComponent = avatar.AddComponent<Tracking4All.Avatar>();
            if (enableDebugLogs) Debug.Log($"AutoSetupTracking: Added Avatar component to {avatarName}");
        }
        
        // Check for AvatarSkeleton component  
        var skeleton = avatar.GetComponent<Tracking4All.AvatarSkeleton>();
        if (skeleton == null)
        {
            skeleton = avatar.AddComponent<Tracking4All.AvatarSkeleton>();
            if (enableDebugLogs) Debug.Log($"AutoSetupTracking: Added AvatarSkeleton to {avatarName}");
        }
        
        // Disable by default - TrackingTargetManager will enable the correct one
        avatarComponent.enabled = false;
    }
    
    private void ConnectUIButtons(CameraManager cameraManager, TrackingTargetManager trackingManager)
    {
        // Try to find and connect UI buttons
        PlayerSelectionUI playerUI = FindObjectOfType<PlayerSelectionUI>();
        if (playerUI != null)
        {
            if (enableDebugLogs) Debug.Log("AutoSetupTracking: Found PlayerSelectionUI - buttons should be connected via CameraManager");
        }
        
        // Try to find other button handlers
        AdamButtonHandler adamHandler = FindObjectOfType<AdamButtonHandler>();
        if (adamHandler != null)
        {
            if (enableDebugLogs) Debug.Log("AutoSetupTracking: Found AdamButtonHandler - should work with CameraManager");
        }
    }
    
    [ContextMenu("Test Adam Switch")]
    public void TestAdamSwitch()
    {
        CameraManager cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager != null)
        {
            cameraManager.OnAdamButtonPressed();
            if (enableDebugLogs) Debug.Log("AutoSetupTracking: Tested Adam switch");
        }
    }
    
    [ContextMenu("Test Remy Switch")]
    public void TestRemySwitch()
    {
        CameraManager cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager != null)
        {
            cameraManager.OnRemyButtonPressed();
            if (enableDebugLogs) Debug.Log("AutoSetupTracking: Tested Remy switch");
        }
    }
}
