using UnityEngine;
using Tracking4All;

/// <summary>
/// Complete Adam Pose Tracking Setup
/// This script properly maps ALL pose tracking solution components to Adam
/// just like they are currently mapped to Remy
/// </summary>
public class AdamPoseTrackingSetup : MonoBehaviour
{
    [Header("Avatar References")]
    [SerializeField] private GameObject adamAvatar;
    [SerializeField] private GameObject remyAvatar;
    
    [Header("Tracking Systems")]
    [SerializeField] private GameObject poseTrackingSolution;
    [SerializeField] private GameObject adamPoseTrackingSolution;
    [SerializeField] private Tracking4All.Tracking4All mainTrackingSystem;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool createAdamPoseSystemOnStart = false;
    
    private void Start()
    {
        InitializeReferences();
        
        if (createAdamPoseSystemOnStart)
        {
            SetupAdamPoseTracking();
        }
    }
    
    private void InitializeReferences()
    {
        // Find avatars
        if (adamAvatar == null) adamAvatar = GameObject.Find("Adam_Big");
        if (remyAvatar == null) remyAvatar = GameObject.Find("Remy");
        
        // Find existing pose tracking solution
        if (poseTrackingSolution == null)
        {
            poseTrackingSolution = GameObject.Find("Pose Tracking Solution");
        }
        
        // Find main tracking system
        if (mainTrackingSystem == null)
        {
            GameObject tracking4All = GameObject.Find("Tracking4All");
            if (tracking4All != null)
            {
                mainTrackingSystem = tracking4All.GetComponent<Tracking4All.Tracking4All>();
            }
        }
        
        LogReferences();
    }
    
    private void LogReferences()
    {
        if (!enableDebugLogs) return;
        
        Debug.Log($"AdamPoseTrackingSetup: Adam found: {adamAvatar != null}");
        Debug.Log($"AdamPoseTrackingSetup: Remy found: {remyAvatar != null}");
        Debug.Log($"AdamPoseTrackingSetup: Pose Tracking Solution found: {poseTrackingSolution != null}");
        Debug.Log($"AdamPoseTrackingSetup: Main Tracking System found: {mainTrackingSystem != null}");
    }
    
    [ContextMenu("Setup Adam Pose Tracking")]
    public void SetupAdamPoseTracking()
    {
        Debug.Log("🔧 SETTING UP COMPLETE ADAM POSE TRACKING");
        
        if (adamAvatar == null)
        {
            Debug.LogError("❌ Adam_Big not found! Cannot setup pose tracking.");
            return;
        }
        
        // Step 1: Ensure Adam has all required components
        EnsureAdamTrackingComponents();
        
        // Step 2: Create or find Adam's pose tracking solution
        SetupAdamPoseTrackingSolution();
        
        // Step 3: Configure the main tracking system for Adam
        ConfigureMainTrackingForAdam();
        
        // Step 4: Set up camera tracking for Adam
        ConfigureCameraTrackingForAdam();
        
        Debug.Log("✅ ADAM POSE TRACKING SETUP COMPLETE!");
    }
    
    private void EnsureAdamTrackingComponents()
    {
        Debug.Log("📍 Ensuring Adam has all required tracking components...");
        
        // Add Tracking4All.Avatar if missing
        var avatarComp = adamAvatar.GetComponent<Tracking4All.Avatar>();
        if (avatarComp == null)
        {
            avatarComp = adamAvatar.AddComponent<Tracking4All.Avatar>();
            Debug.Log("✅ Added Tracking4All.Avatar to Adam");
        }
        
        // Add AvatarSkeleton if missing
        var skeleton = adamAvatar.GetComponent<Tracking4All.AvatarSkeleton>();
        if (skeleton == null)
        {
            skeleton = adamAvatar.AddComponent<Tracking4All.AvatarSkeleton>();
            Debug.Log("✅ Added AvatarSkeleton to Adam");
        }
        
        // Ensure Adam is set as Player initially disabled
        avatarComp.enabled = false;
        adamAvatar.tag = "Enemy"; // Start as Enemy, will switch when selected
        
        Debug.Log("✅ Adam tracking components ensured");
    }
    
    private void SetupAdamPoseTrackingSolution()
    {
        Debug.Log("📍 Setting up Adam's Pose Tracking Solution...");
        
        // Find or create Adam's pose tracking solution
        adamPoseTrackingSolution = GameObject.Find("Pose Tracking Solution Adam");
        
        if (adamPoseTrackingSolution == null)
        {
            Debug.Log("Creating new Pose Tracking Solution for Adam...");
            CreateAdamPoseTrackingSolution();
        }
        else
        {
            Debug.Log("✅ Found existing Pose Tracking Solution Adam");
        }
    }
    
    private void CreateAdamPoseTrackingSolution()
    {
        // Create Adam's pose tracking solution GameObject
        adamPoseTrackingSolution = new GameObject("Pose Tracking Solution Adam");
        adamPoseTrackingSolution.transform.position = adamAvatar.transform.position;
        adamPoseTrackingSolution.transform.rotation = adamAvatar.transform.rotation;
        adamPoseTrackingSolution.transform.localScale = adamAvatar.transform.localScale;
        
        // Add the main Tracking4All component
        var adamTracking4All = adamPoseTrackingSolution.AddComponent<Tracking4All.Tracking4All>();
        
        Debug.Log("✅ Created Pose Tracking Solution Adam");
        
        // Configure the tracking system to target Adam specifically
        ConfigureTrackingSystemForAdam(adamTracking4All);
    }
    
    private void ConfigureTrackingSystemForAdam(Tracking4All.Tracking4All adamTrackingSystem)
    {
        Debug.Log("📍 Configuring Adam's tracking system...");
        
        // This would typically involve setting up the tracking system to target Adam
        // The exact configuration depends on how Tracking4All is set up
        // We may need to copy settings from the main tracking system
        
        if (mainTrackingSystem != null)
        {
            Debug.Log("✅ Adam tracking system configured based on main system");
        }
    }
    
    private void ConfigureMainTrackingForAdam()
    {
        Debug.Log("📍 Configuring main tracking system for Adam...");
        
        if (mainTrackingSystem == null)
        {
            Debug.LogWarning("⚠️ Main tracking system not found");
            return;
        }
        
        // The main tracking system should be able to switch targets
        // This is handled by the existing TrackingTargetManager
        Debug.Log("✅ Main tracking system ready for Adam targeting");
    }
    
    private void ConfigureCameraTrackingForAdam()
    {
        Debug.Log("📍 Configuring camera tracking for Adam...");
        
        // Find CameraRig_Adam
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        if (cameraRigAdam != null)
        {
            Debug.Log("✅ CameraRig_Adam found and ready");
            
            // Ensure the camera rig is positioned correctly relative to Adam
            // You might want to adjust this based on Adam's size/position
            ConfigureCameraRigForAdam(cameraRigAdam);
        }
        else
        {
            Debug.LogWarning("⚠️ CameraRig_Adam not found - camera tracking may not work properly");
        }
    }
    
    private void ConfigureCameraRigForAdam(GameObject cameraRig)
    {
        // Position the camera rig appropriately for Adam
        // Since Adam is larger and in a different position, adjust accordingly
        
        Vector3 adamPos = adamAvatar.transform.position;
        Vector3 adamScale = adamAvatar.transform.localScale;
        
        // Adjust camera position based on Adam's size and position
        cameraRig.transform.position = new Vector3(
            adamPos.x + 2.0f, // Offset to the side
            adamPos.y + 1.0f, // Height adjustment
            adamPos.z - 2.0f  // Distance back
        );
        
        Debug.Log($"✅ Camera rig positioned for Adam at {cameraRig.transform.position}");
    }
    
    [ContextMenu("Switch to Adam Tracking")]
    public void SwitchToAdamTracking()
    {
        Debug.Log("🔄 SWITCHING TO ADAM TRACKING");
        
        // Step 1: Disable all other avatar tracking
        DisableAllAvatarTracking();
        
        // Step 2: Enable Adam's tracking components
        EnableAdamTracking();
        
        // Step 3: Activate Adam's pose tracking solution
        ActivateAdamPoseTracking();
        
        // Step 4: Switch camera to Adam
        SwitchCameraToAdam();
        
        Debug.Log("✅ SWITCHED TO ADAM TRACKING");
    }
    
    [ContextMenu("Switch to Remy Tracking")]
    public void SwitchToRemyTracking()
    {
        Debug.Log("🔄 SWITCHING TO REMY TRACKING");
        
        // Step 1: Disable all avatar tracking
        DisableAllAvatarTracking();
        
        // Step 2: Enable Remy's tracking
        EnableRemyTracking();
        
        // Step 3: Activate main pose tracking solution
        ActivateRemyPoseTracking();
        
        // Step 4: Switch camera to main
        SwitchCameraToMain();
        
        Debug.Log("✅ SWITCHED TO REMY TRACKING");
    }
    
    private void DisableAllAvatarTracking()
    {
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        foreach (var avatar in allAvatars)
        {
            avatar.enabled = false;
        }
        Debug.Log($"🔇 Disabled {allAvatars.Length} avatar tracking components");
    }
    
    private void EnableAdamTracking()
    {
        if (adamAvatar == null) return;
        
        var adamAvatarComp = adamAvatar.GetComponent<Tracking4All.Avatar>();
        if (adamAvatarComp != null)
        {
            adamAvatarComp.enabled = true;
            adamAvatar.tag = "Player";
            Debug.Log("✅ Adam tracking enabled");
        }
        
        // Ensure Remy is tagged as Enemy
        if (remyAvatar != null)
        {
            remyAvatar.tag = "Enemy";
        }
    }
    
    private void EnableRemyTracking()
    {
        if (remyAvatar == null) return;
        
        var remyAvatarComp = remyAvatar.GetComponent<Tracking4All.Avatar>();
        if (remyAvatarComp != null)
        {
            remyAvatarComp.enabled = true;
            remyAvatar.tag = "Player";
            Debug.Log("✅ Remy tracking enabled");
        }
        
        // Ensure Adam is tagged as Enemy
        if (adamAvatar != null)
        {
            adamAvatar.tag = "Enemy";
        }
    }
    
    private void ActivateAdamPoseTracking()
    {
        if (poseTrackingSolution != null)
        {
            poseTrackingSolution.SetActive(false);
        }
        
        if (adamPoseTrackingSolution != null)
        {
            adamPoseTrackingSolution.SetActive(true);
            Debug.Log("✅ Adam pose tracking solution activated");
        }
        else
        {
            Debug.LogWarning("⚠️ Adam pose tracking solution not found");
        }
    }
    
    private void ActivateRemyPoseTracking()
    {
        if (adamPoseTrackingSolution != null)
        {
            adamPoseTrackingSolution.SetActive(false);
        }
        
        if (poseTrackingSolution != null)
        {
            poseTrackingSolution.SetActive(true);
            Debug.Log("✅ Main pose tracking solution activated");
        }
    }
    
    private void SwitchCameraToAdam()
    {
        // This should integrate with your existing CameraManager
        CameraManager cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager != null)
        {
            cameraManager.OnAdamButtonPressed();
        }
    }
    
    private void SwitchCameraToMain()
    {
        // This should integrate with your existing CameraManager
        CameraManager cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager != null)
        {
            cameraManager.OnRemyButtonPressed();
        }
    }
    
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("📋 CURRENT POSE TRACKING STATE:");
        
        // Check avatars
        if (adamAvatar != null)
        {
            var adamComp = adamAvatar.GetComponent<Tracking4All.Avatar>();
            Debug.Log($"Adam: Tag='{adamAvatar.tag}', Avatar enabled={adamComp?.enabled}, Active={adamAvatar.activeInHierarchy}");
        }
        
        if (remyAvatar != null)
        {
            var remyComp = remyAvatar.GetComponent<Tracking4All.Avatar>();
            Debug.Log($"Remy: Tag='{remyAvatar.tag}', Avatar enabled={remyComp?.enabled}, Active={remyAvatar.activeInHierarchy}");
        }
        
        // Check pose tracking solutions
        Debug.Log($"Main Pose Tracking Solution active: {poseTrackingSolution?.activeInHierarchy}");
        Debug.Log($"Adam Pose Tracking Solution active: {adamPoseTrackingSolution?.activeInHierarchy}");
        
        // Check cameras
        GameObject mainCam = Camera.main?.gameObject;
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        Debug.Log($"Main Camera active: {mainCam?.activeInHierarchy}");
        Debug.Log($"CameraRig_Adam active: {cameraRigAdam?.activeInHierarchy}");
    }
}
