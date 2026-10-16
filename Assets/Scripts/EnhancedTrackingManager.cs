using UnityEngine;
using Tracking4All;

/// <summary>
/// Enhanced TrackingTargetManager with better debugging and more robust switching
/// </summary>
public class EnhancedTrackingManager : MonoBehaviour
{
    [Header("Avatar References")]
    [SerializeField] private GameObject remyAvatar;
    [SerializeField] private GameObject adamAvatar;
    
    [Header("Tracking System")]
    [SerializeField] private GameObject tracking4AllSystem;
    [SerializeField] private Tracking4All.Tracking4All trackingMain;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool forceComponentCreation = true;
    [SerializeField] private bool useAggressiveSwitching = true;
    
    private Tracking4All.Avatar currentActiveAvatar;
    private string currentTarget = "Remy";
    
    private void Start()
    {
        InitializeReferences();
        // Wait a frame for everything to initialize, then set default
        StartCoroutine(InitializeDefault());
    }
    
    private System.Collections.IEnumerator InitializeDefault()
    {
        yield return new WaitForEndOfFrame();
        SwitchTrackingTarget("Remy");
    }
    
    private void InitializeReferences()
    {
        if (enableDebugLogs) Debug.Log("EnhancedTrackingManager: Initializing references...");
        
        // Find avatars if not assigned
        if (remyAvatar == null)
        {
            remyAvatar = GameObject.Find("Remy");
            if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: Found Remy avatar: {remyAvatar != null}");
        }
        
        if (adamAvatar == null)
        {
            adamAvatar = GameObject.Find("Adam_Big");
            if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: Found Adam avatar: {adamAvatar != null}");
        }
        
        // Find tracking system if not assigned
        if (tracking4AllSystem == null)
        {
            tracking4AllSystem = GameObject.Find("Tracking4All");
            if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: Found Tracking4All system: {tracking4AllSystem != null}");
        }
        
        if (trackingMain == null && tracking4AllSystem != null)
        {
            trackingMain = tracking4AllSystem.GetComponent<Tracking4All.Tracking4All>();
            if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: Found Tracking4All component: {trackingMain != null}");
        }
        
        // Ensure avatars have required components
        if (forceComponentCreation)
        {
            EnsureAvatarComponents();
        }
    }
    
    private void EnsureAvatarComponents()
    {
        if (enableDebugLogs) Debug.Log("EnhancedTrackingManager: Ensuring avatar components...");
        
        if (remyAvatar != null)
        {
            EnsureComponentsForAvatar(remyAvatar, "Remy");
        }
        
        if (adamAvatar != null)
        {
            EnsureComponentsForAvatar(adamAvatar, "Adam");
        }
    }
    
    private void EnsureComponentsForAvatar(GameObject avatar, string name)
    {
        // Ensure Avatar component
        var avatarComp = avatar.GetComponent<Tracking4All.Avatar>();
        if (avatarComp == null)
        {
            avatarComp = avatar.AddComponent<Tracking4All.Avatar>();
            if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: Added Avatar component to {name}");
        }
        
        // Ensure AvatarSkeleton component
        var skeleton = avatar.GetComponent<Tracking4All.AvatarSkeleton>();
        if (skeleton == null)
        {
            skeleton = avatar.AddComponent<Tracking4All.AvatarSkeleton>();
            if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: Added AvatarSkeleton to {name}");
        }
        
        // Start disabled
        avatarComp.enabled = false;
    }
    
    public void SwitchTrackingTarget(string targetPlayer)
    {
        if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: === SWITCHING TRACKING TO {targetPlayer.ToUpper()} ===");
        
        currentTarget = targetPlayer;
        GameObject targetAvatar = null;
        
        // Determine target avatar
        switch (targetPlayer.ToLower())
        {
            case "adam":
                targetAvatar = adamAvatar;
                break;
            case "remy":
                targetAvatar = remyAvatar;
                break;
            default:
                Debug.LogError($"EnhancedTrackingManager: Unknown target player: {targetPlayer}");
                return;
        }
        
        if (targetAvatar == null)
        {
            Debug.LogError($"EnhancedTrackingManager: Target avatar {targetPlayer} is null!");
            return;
        }
        
        // Step 1: Disable ALL Avatar components aggressively
        DisableAllAvatarTracking();
        
        // Step 2: Wait a frame to ensure changes take effect
        StartCoroutine(ActivateTargetAfterFrame(targetAvatar, targetPlayer));
    }
    
    private System.Collections.IEnumerator ActivateTargetAfterFrame(GameObject targetAvatar, string playerName)
    {
        yield return new WaitForEndOfFrame();
        
        if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: Activating {playerName} after frame delay...");
        
        // Ensure components exist
        EnsureComponentsForAvatar(targetAvatar, playerName);
        
        // Get the Avatar component
        var avatarComponent = targetAvatar.GetComponent<Tracking4All.Avatar>();
        if (avatarComponent == null)
        {
            Debug.LogError($"EnhancedTrackingManager: Could not find Avatar component on {playerName}!");
            yield break;
        }
        
        // Enable the component
        avatarComponent.enabled = true;
        currentActiveAvatar = avatarComponent;
        
        if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: ✅ {playerName} Avatar component enabled");
        
        // Additional aggressive steps for Adam
        if (playerName.ToLower() == "adam" && useAggressiveSwitching)
        {
            yield return new WaitForSeconds(0.1f);
            PerformAggressiveAdamSwitch(targetAvatar, avatarComponent);
        }
        
        // Verify the switch worked
        yield return new WaitForSeconds(0.2f);
        VerifyTrackingSwitch(playerName);
    }
    
    private void PerformAggressiveAdamSwitch(GameObject adamAvatar, Tracking4All.Avatar adamComponent)
    {
        if (enableDebugLogs) Debug.Log("EnhancedTrackingManager: Performing aggressive Adam switch...");
        
        // Try multiple approaches to ensure Adam tracking works
        
        // Approach 1: Disable/Re-enable
        adamComponent.enabled = false;
        adamComponent.enabled = true;
        
        // Approach 2: Ensure GameObject is active
        if (!adamAvatar.activeInHierarchy)
        {
            adamAvatar.SetActive(true);
            if (enableDebugLogs) Debug.Log("EnhancedTrackingManager: Activated Adam GameObject");
        }
        
        // Approach 3: Set tag
        adamAvatar.tag = "Player";
        
        // Approach 4: Try to refresh the tracking system connection
        if (trackingMain != null)
        {
            // Force a refresh on the tracking system if possible
            if (enableDebugLogs) Debug.Log("EnhancedTrackingManager: Attempting to refresh tracking system");
        }
        
        currentActiveAvatar = adamComponent;
    }
    
    private void DisableAllAvatarTracking()
    {
        if (enableDebugLogs) Debug.Log("EnhancedTrackingManager: Disabling all Avatar tracking components...");
        
        // Method 1: Find all Avatar components by type
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        foreach (Tracking4All.Avatar avatar in allAvatars)
        {
            if (avatar.enabled)
            {
                avatar.enabled = false;
                if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: Disabled tracking for {avatar.gameObject.name}");
            }
        }
        
        // Method 2: Explicitly disable known avatars
        if (remyAvatar != null)
        {
            var remyComp = remyAvatar.GetComponent<Tracking4All.Avatar>();
            if (remyComp != null && remyComp.enabled)
            {
                remyComp.enabled = false;
                if (enableDebugLogs) Debug.Log("EnhancedTrackingManager: Explicitly disabled Remy Avatar");
            }
        }
        
        if (adamAvatar != null)
        {
            var adamComp = adamAvatar.GetComponent<Tracking4All.Avatar>();
            if (adamComp != null && adamComp.enabled)
            {
                adamComp.enabled = false;
                if (enableDebugLogs) Debug.Log("EnhancedTrackingManager: Explicitly disabled Adam Avatar");
            }
        }
    }
    
    private void VerifyTrackingSwitch(string expectedTarget)
    {
        if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: Verifying tracking switch to {expectedTarget}...");
        
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        int enabledCount = 0;
        string enabledTarget = "none";
        
        foreach (var avatar in allAvatars)
        {
            if (avatar.enabled)
            {
                enabledCount++;
                enabledTarget = avatar.gameObject.name;
                if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: Found enabled Avatar on {avatar.gameObject.name}");
            }
        }
        
        if (enabledCount == 1)
        {
            bool correctTarget = (expectedTarget.ToLower() == "adam" && enabledTarget.Contains("Adam")) ||
                                (expectedTarget.ToLower() == "remy" && enabledTarget.Contains("Remy"));
            
            if (correctTarget)
            {
                if (enableDebugLogs) Debug.Log($"EnhancedTrackingManager: ✅ SUCCESS - Tracking correctly switched to {expectedTarget}");
            }
            else
            {
                Debug.LogWarning($"EnhancedTrackingManager: ⚠️ WARNING - Expected {expectedTarget} but found {enabledTarget}");
            }
        }
        else
        {
            Debug.LogError($"EnhancedTrackingManager: ❌ ERROR - Found {enabledCount} enabled Avatars, expected exactly 1");
        }
    }
    
    // Public interface methods
    public void OnAdamSelected()
    {
        SwitchTrackingTarget("Adam");
    }
    
    public void OnRemySelected()
    {
        SwitchTrackingTarget("Remy");
    }
    
    public Tracking4All.Avatar GetCurrentActiveAvatar()
    {
        return currentActiveAvatar;
    }
    
    public string GetCurrentTarget()
    {
        return currentTarget;
    }
    
    // Debug methods
    [ContextMenu("Force Switch to Adam")]
    public void ForceAdamSwitch()
    {
        OnAdamSelected();
    }
    
    [ContextMenu("Force Switch to Remy")]
    public void ForceRemySwitch()
    {
        OnRemySelected();
    }
    
    [ContextMenu("Log Current State")]
    public void LogCurrentState()
    {
        Debug.Log("=== ENHANCED TRACKING MANAGER STATE ===");
        Debug.Log($"Current target: {currentTarget}");
        Debug.Log($"Current active avatar: {currentActiveAvatar?.gameObject.name ?? "null"}");
        
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        Debug.Log($"Found {allAvatars.Length} Avatar components:");
        
        foreach (var avatar in allAvatars)
        {
            Debug.Log($"  - {avatar.gameObject.name}: enabled={avatar.enabled}, active={avatar.gameObject.activeInHierarchy}");
        }
        
        Debug.Log("=== END STATE ===");
    }
}
