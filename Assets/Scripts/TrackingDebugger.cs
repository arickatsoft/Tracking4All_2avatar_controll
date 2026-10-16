using UnityEngine;
using Tracking4All;

/// <summary>
/// Debug script to help identify why tracking isn't switching to Adam
/// This will help us see what's happening with the tracking system
/// </summary>
public class TrackingDebugger : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] private bool enableVerboseLogging = true;
    [SerializeField] private bool logEveryFrame = false;
    
    [Header("Manual Testing")]
    [SerializeField] private KeyCode testAdamKey = KeyCode.A;
    [SerializeField] private KeyCode testRemyKey = KeyCode.R;
    
    private CameraManager cameraManager;
    private TrackingTargetManager trackingManager;
    
    private void Start()
    {
        // Find components
        cameraManager = FindObjectOfType<CameraManager>();
        trackingManager = FindObjectOfType<TrackingTargetManager>();
        
        LogSystemStatus();
    }
    
    private void Update()
    {
        // Manual testing keys
        if (Input.GetKeyDown(testAdamKey))
        {
            Debug.Log("TrackingDebugger: Manual Adam test triggered");
            TestAdamSwitch();
        }
        
        if (Input.GetKeyDown(testRemyKey))
        {
            Debug.Log("TrackingDebugger: Manual Remy test triggered");
            TestRemySwitch();
        }
        
        if (logEveryFrame)
        {
            LogCurrentTrackingState();
        }
    }
    
    private void LogSystemStatus()
    {
        Debug.Log("=== TRACKING DEBUGGER SYSTEM STATUS ===");
        
        // Check for required GameObjects
        GameObject remy = GameObject.Find("Remy");
        GameObject adam = GameObject.Find("Adam_Big");
        GameObject tracking4All = GameObject.Find("Tracking4All");
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        
        Debug.Log($"Remy found: {remy != null}");
        Debug.Log($"Adam_Big found: {adam != null}");
        Debug.Log($"Tracking4All found: {tracking4All != null}");
        Debug.Log($"CameraRig_Adam found: {cameraRigAdam != null}");
        
        // Check for managers
        Debug.Log($"CameraManager found: {cameraManager != null}");
        Debug.Log($"TrackingTargetManager found: {trackingManager != null}");
        
        // Check Avatar components
        if (remy != null)
        {
            var remyAvatar = remy.GetComponent<Tracking4All.Avatar>();
            var remySkeleton = remy.GetComponent<Tracking4All.AvatarSkeleton>();
            Debug.Log($"Remy Avatar component: {remyAvatar != null} (enabled: {remyAvatar?.enabled})");
            Debug.Log($"Remy AvatarSkeleton: {remySkeleton != null}");
        }
        
        if (adam != null)
        {
            var adamAvatar = adam.GetComponent<Tracking4All.Avatar>();
            var adamSkeleton = adam.GetComponent<Tracking4All.AvatarSkeleton>();
            Debug.Log($"Adam Avatar component: {adamAvatar != null} (enabled: {adamAvatar?.enabled})");
            Debug.Log($"Adam AvatarSkeleton: {adamSkeleton != null}");
        }
        
        // Check Tracking4All main component
        if (tracking4All != null)
        {
            var mainTracking = tracking4All.GetComponent<Tracking4All.Tracking4All>();
            Debug.Log($"Main Tracking4All component: {mainTracking != null}");
        }
        
        Debug.Log("=== END SYSTEM STATUS ===");
    }
    
    [ContextMenu("Test Adam Switch")]
    public void TestAdamSwitch()
    {
        Debug.Log("TrackingDebugger: Testing Adam switch...");
        
        if (cameraManager != null)
        {
            cameraManager.OnAdamButtonPressed();
        }
        
        // Wait a frame then check results
        StartCoroutine(CheckSwitchResults("Adam"));
    }
    
    [ContextMenu("Test Remy Switch")]
    public void TestRemySwitch()
    {
        Debug.Log("TrackingDebugger: Testing Remy switch...");
        
        if (cameraManager != null)
        {
            cameraManager.OnRemyButtonPressed();
        }
        
        // Wait a frame then check results
        StartCoroutine(CheckSwitchResults("Remy"));
    }
    
    private System.Collections.IEnumerator CheckSwitchResults(string targetPlayer)
    {
        yield return new WaitForEndOfFrame();
        
        Debug.Log($"=== SWITCH RESULTS FOR {targetPlayer} ===");
        
        // Check camera states
        GameObject mainCam = Camera.main?.gameObject;
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        
        if (mainCam != null)
        {
            Debug.Log($"Main Camera active: {mainCam.activeInHierarchy}");
        }
        
        if (cameraRigAdam != null)
        {
            Debug.Log($"CameraRig_Adam active: {cameraRigAdam.activeInHierarchy}");
        }
        
        // Check Avatar component states
        CheckAvatarStates();
        
        // Check tags
        GameObject remy = GameObject.Find("Remy");
        GameObject adam = GameObject.Find("Adam_Big");
        
        if (remy != null)
        {
            Debug.Log($"Remy tag: {remy.tag}");
        }
        
        if (adam != null)
        {
            Debug.Log($"Adam tag: {adam.tag}");
        }
        
        Debug.Log($"=== END SWITCH RESULTS ===");
    }
    
    private void LogCurrentTrackingState()
    {
        CheckAvatarStates();
    }
    
    private void CheckAvatarStates()
    {
        // Find all Avatar components and check their states
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        
        Debug.Log($"Found {allAvatars.Length} Avatar components:");
        
        foreach (var avatar in allAvatars)
        {
            Debug.Log($"  - {avatar.gameObject.name}: enabled={avatar.enabled}, active={avatar.gameObject.activeInHierarchy}");
        }
        
        if (trackingManager != null)
        {
            var currentActive = trackingManager.GetCurrentActiveAvatar();
            Debug.Log($"TrackingTargetManager reports active avatar: {currentActive?.gameObject.name ?? "null"}");
        }
    }
    
    [ContextMenu("Force Fix Adam Tracking")]
    public void ForceFixAdamTracking()
    {
        Debug.Log("TrackingDebugger: Force fixing Adam tracking...");
        
        GameObject adam = GameObject.Find("Adam_Big");
        if (adam != null)
        {
            // Disable all other Avatar components first
            Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
            foreach (var av in allAvatars)
            {
                av.enabled = false;
            }
            
            // Ensure Adam has the components
            var adamAvatar = adam.GetComponent<Tracking4All.Avatar>();
            if (adamAvatar == null)
            {
                adamAvatar = adam.AddComponent<Tracking4All.Avatar>();
                Debug.Log("Added Avatar component to Adam");
            }
            
            var adamSkeleton = adam.GetComponent<Tracking4All.AvatarSkeleton>();
            if (adamSkeleton == null)
            {
                adamSkeleton = adam.AddComponent<Tracking4All.AvatarSkeleton>();
                Debug.Log("Added AvatarSkeleton to Adam");
            }
            
            // Enable Adam's tracking
            adamAvatar.enabled = true;
            adam.tag = "Player";
            
            Debug.Log("Force enabled Adam tracking");
            
            // Check results
            StartCoroutine(CheckSwitchResults("Adam (Force Fixed)"));
        }
    }
    
    [ContextMenu("Log Full System Analysis")]
    public void LogFullSystemAnalysis()
    {
        LogSystemStatus();
        CheckAvatarStates();
        
        // Additional analysis
        Debug.Log("=== ADDITIONAL ANALYSIS ===");
        
        // Check if there are multiple Tracking4All systems
        Tracking4All.Tracking4All[] allTrackingSystems = FindObjectsOfType<Tracking4All.Tracking4All>();
        Debug.Log($"Found {allTrackingSystems.Length} Tracking4All systems");
        
        for (int i = 0; i < allTrackingSystems.Length; i++)
        {
            Debug.Log($"  System {i}: {allTrackingSystems[i].gameObject.name} (active: {allTrackingSystems[i].gameObject.activeInHierarchy})");
        }
    }
}
