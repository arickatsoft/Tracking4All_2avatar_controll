using UnityEngine;
using Tracking4All;

/// <summary>
/// Manages the motion tracking target switching between Adam and Remy
/// This script ensures that when Adam is selected, the motion tracking follows Adam instead of Remy
/// Updated to fix namespace conflicts
/// </summary>
public class TrackingTargetManager : MonoBehaviour
{
    [Header("Avatar References")]
    [SerializeField] private GameObject remyAvatar;
    [SerializeField] private GameObject adamAvatar;
    
    [Header("Tracking System")]
    [SerializeField] private GameObject tracking4AllSystem;
    [SerializeField] private Tracking4All.Tracking4All trackingMain;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private Tracking4All.Avatar currentActiveAvatar;
    
    private void Start()
    {
        InitializeReferences();
        // Default to Remy tracking
        SwitchTrackingTarget("Remy");
    }
    
    private void InitializeReferences()
    {
        // Find avatars if not assigned
        if (remyAvatar == null)
        {
            remyAvatar = GameObject.Find("Remy");
            if (enableDebugLogs) Debug.Log($"TrackingTargetManager: Found Remy avatar: {remyAvatar != null}");
        }
        
        if (adamAvatar == null)
        {
            adamAvatar = GameObject.Find("Adam_Big");
            if (enableDebugLogs) Debug.Log($"TrackingTargetManager: Found Adam avatar: {adamAvatar != null}");
        }
        
        // Find tracking system if not assigned
        if (tracking4AllSystem == null)
        {
            tracking4AllSystem = GameObject.Find("Tracking4All");
            if (enableDebugLogs) Debug.Log($"TrackingTargetManager: Found Tracking4All system: {tracking4AllSystem != null}");
        }
        
        if (trackingMain == null && tracking4AllSystem != null)
        {
            trackingMain = tracking4AllSystem.GetComponent<Tracking4All.Tracking4All>();
            if (enableDebugLogs) Debug.Log($"TrackingTargetManager: Found Tracking4All component: {trackingMain != null}");
        }
    }
    
    /// <summary>
    /// Switches the tracking target to the specified player
    /// </summary>
    /// <param name="targetPlayer">"Adam" or "Remy"</param>
    public void SwitchTrackingTarget(string targetPlayer)
    {
        if (enableDebugLogs) Debug.Log($"TrackingTargetManager: Switching tracking target to {targetPlayer}");
        
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
                Debug.LogError($"TrackingTargetManager: Unknown target player: {targetPlayer}");
                return;
        }
        
        if (targetAvatar == null)
        {
            Debug.LogError($"TrackingTargetManager: Target avatar {targetPlayer} is null!");
            return;
        }
        
        // Disable all existing Avatar components
        DisableAllAvatarTracking();
        
        // Enable/create tracking for target avatar
        SetupTrackingForAvatar(targetAvatar, targetPlayer);
        
        if (enableDebugLogs) Debug.Log($"TrackingTargetManager: ✅ Successfully switched tracking to {targetPlayer}");
    }
    
    private void DisableAllAvatarTracking()
    {
        // Find all Avatar components in the scene and disable them
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        
        foreach (Tracking4All.Avatar avatar in allAvatars)
        {
            avatar.enabled = false;
            if (enableDebugLogs) Debug.Log($"TrackingTargetManager: Disabled tracking for {avatar.gameObject.name}");
        }
    }
    
    private void SetupTrackingForAvatar(GameObject targetAvatar, string playerName)
    {
        // Get or add Avatar component
        Tracking4All.Avatar avatarComponent = targetAvatar.GetComponent<Tracking4All.Avatar>();
        
        if (avatarComponent == null)
        {
            if (enableDebugLogs) Debug.Log($"TrackingTargetManager: Adding Avatar component to {playerName}");
            avatarComponent = targetAvatar.AddComponent<Tracking4All.Avatar>();
        }
        
        // Get or add AvatarSkeleton component if needed
        AvatarSkeleton skeleton = targetAvatar.GetComponent<AvatarSkeleton>();
        if (skeleton == null)
        {
            if (enableDebugLogs) Debug.Log($"TrackingTargetManager: Adding AvatarSkeleton to {playerName}");
            skeleton = targetAvatar.AddComponent<AvatarSkeleton>();
        }
        
        // Enable the Avatar component
        avatarComponent.enabled = true;
        currentActiveAvatar = avatarComponent;
        
        // Try to connect to the main tracking system
        if (trackingMain != null)
        {
            // The Tracking4All system should automatically detect the active Avatar component
            if (enableDebugLogs) Debug.Log($"TrackingTargetManager: Connected {playerName} to Tracking4All system");
        }
        
        if (enableDebugLogs) Debug.Log($"TrackingTargetManager: ✅ Tracking setup complete for {playerName}");
    }
    
    /// <summary>
    /// Called when Adam button is pressed
    /// </summary>
    public void OnAdamSelected()
    {
        SwitchTrackingTarget("Adam");
    }
    
    /// <summary>
    /// Called when Remy button is pressed
    /// </summary>
    public void OnRemySelected()
    {
        SwitchTrackingTarget("Remy");
    }
    
    /// <summary>
    /// Gets the currently active avatar component
    /// </summary>
    public Tracking4All.Avatar GetCurrentActiveAvatar()
    {
        return currentActiveAvatar;
    }
    
    /// <summary>
    /// Force refresh the tracking system
    /// </summary>
    public void RefreshTracking()
    {
        if (currentActiveAvatar != null)
        {
            string currentPlayer = currentActiveAvatar.gameObject.name.Contains("Adam") ? "Adam" : "Remy";
            if (enableDebugLogs) Debug.Log($"TrackingTargetManager: Refreshing tracking for {currentPlayer}");
            SwitchTrackingTarget(currentPlayer);
        }
    }
}
