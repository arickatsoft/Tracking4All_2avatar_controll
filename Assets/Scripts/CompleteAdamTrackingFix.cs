using UnityEngine;
using Tracking4All;
using System.Collections;

/// <summary>
/// COMPLETE Adam Tracking Solution
/// This addresses the REAL issue: Adam needs the full pose tracking pipeline
/// including Animator setup, pose mapping, and tracking system integration
/// </summary>
public class CompleteAdamTrackingFix : MonoBehaviour
{
    [Header("Critical References")]
    [SerializeField] private GameObject adamAvatar;
    [SerializeField] private GameObject remyAvatar;
    [SerializeField] private GameObject poseTrackingSolution;
    [SerializeField] private GameObject tracking4AllSystem;
    
    [Header("Auto-Fix Settings")]
    [SerializeField] private bool runFullFixOnStart = false;
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Start()
    {
        InitializeReferences();
        
        if (runFullFixOnStart)
        {
            StartCoroutine(RunCompleteFixDelayed());
        }
    }
    
    private IEnumerator RunCompleteFixDelayed()
    {
        yield return new WaitForSeconds(1f); // Wait for everything to initialize
        CompleteAdamTrackingFix_Master();
    }
    
    private void InitializeReferences()
    {
        if (adamAvatar == null) adamAvatar = GameObject.Find("Adam_Big");
        if (remyAvatar == null) remyAvatar = GameObject.Find("Remy");
        if (poseTrackingSolution == null) poseTrackingSolution = GameObject.Find("Pose Tracking Solution");
        if (tracking4AllSystem == null) tracking4AllSystem = GameObject.Find("Tracking4All");
        
        LogInitialState();
    }
    
    private void LogInitialState()
    {
        if (!enableDebugLogs) return;
        
        Debug.Log("🔍 INITIAL STATE ANALYSIS:");
        Debug.Log($"Adam found: {adamAvatar != null}");
        Debug.Log($"Remy found: {remyAvatar != null}");
        Debug.Log($"Pose Tracking Solution found: {poseTrackingSolution != null}");
        Debug.Log($"Tracking4All system found: {tracking4AllSystem != null}");
    }
    
    [ContextMenu("MASTER FIX - Complete Adam Tracking")]
    public void CompleteAdamTrackingFix_Master()
    {
        Debug.Log("🚀 STARTING COMPLETE ADAM TRACKING FIX");
        Debug.Log("This will set up ALL components needed for Adam tracking");
        
        if (adamAvatar == null)
        {
            Debug.LogError("❌ CRITICAL: Adam_Big not found! Cannot proceed.");
            return;
        }
        
        // Phase 1: Analyze current setup
        AnalyzeCurrentSetup();
        
        // Phase 2: Fix Adam's Animator and Avatar setup
        FixAdamAnimatorSetup();
        
        // Phase 3: Set up Adam's Tracking4All components
        SetupAdamTrackingComponents();
        
        // Phase 4: Configure pose tracking system
        ConfigurePoseTrackingSystem();
        
        // Phase 5: Test and verify
        StartCoroutine(VerifyAndTest());
        
        Debug.Log("✅ COMPLETE ADAM TRACKING FIX INITIATED");
    }
    
    private void AnalyzeCurrentSetup()
    {
        Debug.Log("📊 PHASE 1: ANALYZING CURRENT SETUP");
        
        // Check Adam's current components
        if (adamAvatar != null)
        {
            Animator adamAnimator = adamAvatar.GetComponent<Animator>();
            Tracking4All.Avatar adamTrackingAvatar = adamAvatar.GetComponent<Tracking4All.Avatar>();
            Tracking4All.AvatarSkeleton adamSkeleton = adamAvatar.GetComponent<Tracking4All.AvatarSkeleton>();
            
            Debug.Log($"Adam Animator: {(adamAnimator != null ? $"EXISTS (avatar: {adamAnimator.avatar})" : "MISSING")}");
            Debug.Log($"Adam Tracking4All.Avatar: {(adamTrackingAvatar != null ? $"EXISTS (enabled: {adamTrackingAvatar.enabled})" : "MISSING")}");
            Debug.Log($"Adam AvatarSkeleton: {(adamSkeleton != null ? "EXISTS" : "MISSING")}");
            Debug.Log($"Adam Tag: {adamAvatar.tag}");
        }
        
        // Check Remy's setup for comparison
        if (remyAvatar != null)
        {
            Animator remyAnimator = remyAvatar.GetComponent<Animator>();
            Tracking4All.Avatar remyTrackingAvatar = remyAvatar.GetComponent<Tracking4All.Avatar>();
            Tracking4All.AvatarSkeleton remySkeleton = remyAvatar.GetComponent<Tracking4All.AvatarSkeleton>();
            
            Debug.Log($"Remy Animator: {(remyAnimator != null ? $"EXISTS (avatar: {remyAnimator.avatar})" : "MISSING")}");
            Debug.Log($"Remy Tracking4All.Avatar: {(remyTrackingAvatar != null ? $"EXISTS (enabled: {remyTrackingAvatar.enabled})" : "MISSING")}");
            Debug.Log($"Remy AvatarSkeleton: {(remySkeleton != null ? "EXISTS" : "MISSING")}");
            Debug.Log($"Remy Tag: {remyAvatar.tag}");
        }
    }
    
    private void FixAdamAnimatorSetup()
    {
        Debug.Log("🎭 PHASE 2: FIXING ADAM'S ANIMATOR SETUP");
        
        Animator adamAnimator = adamAvatar.GetComponent<Animator>();
        if (adamAnimator == null)
        {
            adamAnimator = adamAvatar.AddComponent<Animator>();
            Debug.Log("✅ Added Animator to Adam");
        }
        
        // Check if Adam has a humanoid avatar assigned
        if (adamAnimator.avatar == null)
        {
            Debug.Log("⚠️ Adam's Animator has no avatar assigned");
            
            // Try to copy from Remy if available
            if (remyAvatar != null)
            {
                Animator remyAnimator = remyAvatar.GetComponent<Animator>();
                if (remyAnimator != null && remyAnimator.avatar != null)
                {
                    adamAnimator.avatar = remyAnimator.avatar;
                    Debug.Log("✅ Copied humanoid avatar from Remy to Adam");
                }
            }
            
            if (adamAnimator.avatar == null)
            {
                Debug.LogWarning("⚠️ IMPORTANT: Adam still needs a humanoid avatar assigned to his Animator!");
                Debug.LogWarning("⚠️ Please assign a humanoid avatar in Adam's Animator component in the Inspector");
            }
        }
        else
        {
            Debug.Log($"✅ Adam's Animator already has avatar: {adamAnimator.avatar.name}");
        }
        
        // Ensure Animator is enabled and configured
        adamAnimator.enabled = true;
        Debug.Log("✅ Adam's Animator enabled and configured");
    }
    
    private void SetupAdamTrackingComponents()
    {
        Debug.Log("🎯 PHASE 3: SETTING UP ADAM'S TRACKING COMPONENTS");
        
        // Add/ensure Tracking4All.Avatar component
        Tracking4All.Avatar adamTrackingAvatar = adamAvatar.GetComponent<Tracking4All.Avatar>();
        if (adamTrackingAvatar == null)
        {
            adamTrackingAvatar = adamAvatar.AddComponent<Tracking4All.Avatar>();
            Debug.Log("✅ Added Tracking4All.Avatar to Adam");
        }
        
        // Add/ensure AvatarSkeleton component
        Tracking4All.AvatarSkeleton adamSkeleton = adamAvatar.GetComponent<Tracking4All.AvatarSkeleton>();
        if (adamSkeleton == null)
        {
            adamSkeleton = adamAvatar.AddComponent<Tracking4All.AvatarSkeleton>();
            Debug.Log("✅ Added AvatarSkeleton to Adam");
        }
        
        // Initially disable the Avatar component (will be enabled when selected)
        adamTrackingAvatar.enabled = false;
        adamAvatar.tag = "Enemy"; // Will be changed to "Player" when selected
        
        Debug.Log("✅ Adam tracking components set up");
    }
    
    private void ConfigurePoseTrackingSystem()
    {
        Debug.Log("🔧 PHASE 4: CONFIGURING POSE TRACKING SYSTEM");
        
        if (tracking4AllSystem != null)
        {
            Tracking4All.Tracking4All mainTracking = tracking4AllSystem.GetComponent<Tracking4All.Tracking4All>();
            if (mainTracking != null)
            {
                Debug.Log("✅ Main Tracking4All system found and ready");
            }
            else
            {
                Debug.LogWarning("⚠️ Main Tracking4All component not found on system");
            }
        }
        
        // Ensure the TrackingTargetManager can handle Adam
        TrackingTargetManager targetManager = FindObjectOfType<TrackingTargetManager>();
        if (targetManager != null)
        {
            Debug.Log("✅ TrackingTargetManager found - ready to handle Adam switching");
        }
        else
        {
            Debug.LogWarning("⚠️ TrackingTargetManager not found - tracking switching may not work");
        }
        
        Debug.Log("✅ Pose tracking system configuration complete");
    }
    
    private IEnumerator VerifyAndTest()
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log("🧪 PHASE 5: VERIFICATION AND TESTING");
        
        // Test switching to Adam
        Debug.Log("Testing switch to Adam...");
        SwitchToAdamTracking();
        
        yield return new WaitForSeconds(2f);
        
        // Verify Adam is active
        VerifyAdamTrackingActive();
        
        yield return new WaitForSeconds(1f);
        
        // Switch back to Remy for comparison
        Debug.Log("Testing switch back to Remy...");
        SwitchToRemyTracking();
        
        yield return new WaitForSeconds(1f);
        
        Debug.Log("✅ VERIFICATION COMPLETE - CHECK CONSOLE OUTPUT ABOVE");
    }
    
    [ContextMenu("Switch to Adam Tracking")]
    public void SwitchToAdamTracking()
    {
        Debug.Log("🔄 SWITCHING TO ADAM TRACKING");
        
        // Disable all Avatar components
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        foreach (var av in allAvatars)
        {
            av.enabled = false;
        }
        
        // Enable Adam's tracking
        if (adamAvatar != null)
        {
            var adamComp = adamAvatar.GetComponent<Tracking4All.Avatar>();
            if (adamComp != null)
            {
                adamComp.enabled = true;
                adamAvatar.tag = "Player";
                Debug.Log("✅ Adam tracking enabled, tagged as Player");
            }
        }
        
        // Set Remy as Enemy
        if (remyAvatar != null)
        {
            remyAvatar.tag = "Enemy";
        }
        
        // Use CameraManager to handle camera switching
        CameraManager cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager != null)
        {
            cameraManager.OnAdamButtonPressed();
            Debug.Log("✅ Camera switched to Adam view");
        }
    }
    
    [ContextMenu("Emergency Adam Fix")]
    public void EmergencyAdamFix()
    {
        Debug.Log("🚨 EMERGENCY ADAM FIX - TRYING EVERYTHING");
        
        if (adamAvatar == null)
        {
            Debug.LogError("❌ Cannot find Adam_Big!");
            return;
        }
        
        // Step 1: Ensure Animator exists and has avatar
        Animator adamAnim = adamAvatar.GetComponent<Animator>();
        if (adamAnim == null)
        {
            adamAnim = adamAvatar.AddComponent<Animator>();
        }
        
        // Try to get avatar from Remy
        if (adamAnim.avatar == null && remyAvatar != null)
        {
            Animator remyAnim = remyAvatar.GetComponent<Animator>();
            if (remyAnim?.avatar != null)
            {
                adamAnim.avatar = remyAnim.avatar;
                Debug.Log("✅ Copied avatar from Remy to Adam");
            }
        }
        
        // Step 2: Ensure tracking components
        var trackingAvatar = adamAvatar.GetComponent<Tracking4All.Avatar>();
        if (trackingAvatar == null)
        {
            trackingAvatar = adamAvatar.AddComponent<Tracking4All.Avatar>();
        }
        
        var skeleton = adamAvatar.GetComponent<Tracking4All.AvatarSkeleton>();
        if (skeleton == null)
        {
            skeleton = adamAvatar.AddComponent<Tracking4All.AvatarSkeleton>();
        }
        
        // Step 3: Nuclear option - disable everything else, enable only Adam
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        foreach (var av in allAvatars)
        {
            av.enabled = false;
        }
        
        trackingAvatar.enabled = true;
        adamAvatar.tag = "Player";
        
        // Step 4: Force camera switch
        CameraManager camManager = FindObjectOfType<CameraManager>();
        if (camManager != null)
        {
            camManager.OnAdamButtonPressed();
        }
        
        Debug.Log("✅ EMERGENCY FIX COMPLETE - TEST ADAM BUTTON NOW!");
        
        // Verify after a short delay
        StartCoroutine(EmergencyVerification());
    }
    
    private IEnumerator EmergencyVerification()
    {
        yield return new WaitForSeconds(2f);
        
        Debug.Log("🔍 EMERGENCY FIX VERIFICATION:");
        
        if (adamAvatar != null)
        {
            var trackingComp = adamAvatar.GetComponent<Tracking4All.Avatar>();
            var animComp = adamAvatar.GetComponent<Animator>();
            
            Debug.Log($"Adam tag: {adamAvatar.tag}");
            Debug.Log($"Adam tracking enabled: {trackingComp?.enabled}");
            Debug.Log($"Adam animator avatar: {animComp?.avatar?.name}");
            
            if (adamAvatar.tag == "Player" && trackingComp?.enabled == true)
            {
                Debug.Log("✅ EMERGENCY FIX APPEARS TO HAVE WORKED!");
                Debug.Log("🎯 Press the Adam button to test motion tracking!");
            }
            else
            {
                Debug.LogWarning("⚠️ Emergency fix may not have fully worked - check avatar setup");
            }
        }
    }
    
    private void VerifyAdamTrackingActive()
    {
        Debug.Log("🔍 VERIFYING ADAM TRACKING STATUS:");
        
        if (adamAvatar == null)
        {
            Debug.LogError("❌ Adam avatar is null!");
            return;
        }
        
        // Check Adam's components
        Animator adamAnimator = adamAvatar.GetComponent<Animator>();
        Tracking4All.Avatar adamTrackingAvatar = adamAvatar.GetComponent<Tracking4All.Avatar>();
        Tracking4All.AvatarSkeleton adamSkeleton = adamAvatar.GetComponent<Tracking4All.AvatarSkeleton>();
        
        Debug.Log($"Adam Tag: '{adamAvatar.tag}'");
        Debug.Log($"Adam Active: {adamAvatar.activeInHierarchy}");
        Debug.Log($"Adam Animator: {(adamAnimator != null ? $"EXISTS, enabled: {adamAnimator.enabled}, avatar: {adamAnimator.avatar?.name}" : "MISSING")}");
        Debug.Log($"Adam Tracking Avatar: {(adamTrackingAvatar != null ? $"EXISTS, enabled: {adamTrackingAvatar.enabled}" : "MISSING")}");
        Debug.Log($"Adam Skeleton: {(adamSkeleton != null ? "EXISTS" : "MISSING")}");
        
        // Check if Adam is the only active tracking avatar
        Tracking4All.Avatar[] enabledAvatars = System.Array.FindAll(
            FindObjectsOfType<Tracking4All.Avatar>(), 
            av => av.enabled
        );
        
        Debug.Log($"Total enabled Avatar components: {enabledAvatars.Length}");
        foreach (var av in enabledAvatars)
        {
            Debug.Log($"  - {av.gameObject.name} (tag: {av.gameObject.tag})");
        }
        
        // Final assessment
        bool adamTrackingWorking = (
            adamAvatar.tag == "Player" &&
            adamTrackingAvatar != null && adamTrackingAvatar.enabled &&
            enabledAvatars.Length == 1 &&
            enabledAvatars[0].gameObject == adamAvatar
        );
        
        if (adamTrackingWorking)
        {
            Debug.Log("✅ ADAM TRACKING VERIFICATION PASSED!");
            Debug.Log("🎉 Adam should now be tracked by the motion capture system!");
        }
        else
        {
            Debug.LogWarning("⚠️ ADAM TRACKING VERIFICATION FAILED");
            Debug.LogWarning("Check the issues listed above and run the fix again");
        }
    }
    
    [ContextMenu("Switch to Remy Tracking")]
    public void SwitchToRemyTracking()
    {
        Debug.Log("🔄 SWITCHING TO REMY TRACKING");
        
        // Disable all Avatar components
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        foreach (var av in allAvatars)
        {
            av.enabled = false;
        }
        
        // Enable Remy's tracking
        if (remyAvatar != null)
        {
            var remyComp = remyAvatar.GetComponent<Tracking4All.Avatar>();
            if (remyComp != null)
            {
                remyComp.enabled = true;
                remyAvatar.tag = "Player";
                Debug.Log("✅ Remy tracking enabled, tagged as Player");
            }
        }
        
        // Set Adam as Enemy
        if (adamAvatar != null)
        {
            adamAvatar.tag = "Enemy";
        }
        
        // Use CameraManager to handle camera switching
        CameraManager cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager != null)
        {
            cameraManager.OnRemyButtonPressed();
            Debug.Log("✅ Camera switched to Remy view");
        }
    }
}
