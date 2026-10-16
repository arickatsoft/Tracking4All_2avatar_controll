using UnityEngine;
using Tracking4All;

/// <summary>
/// Quick fix for Adam tracking issue
/// The main problem is that Adam_Big has "Enemy" tag instead of "Player"
/// </summary>
public class AdamTrackingQuickFix : MonoBehaviour
{
    [Header("Quick Fix Settings")]
    [SerializeField] private bool runFixOnStart = false;
    
    private void Start()
    {
        if (runFixOnStart)
        {
            FixAdamTrackingImmediate();
        }
    }
    
    [ContextMenu("Fix Adam Tracking NOW")]
    public void FixAdamTrackingImmediate()
    {
        Debug.Log("🔧 FIXING ADAM TRACKING - IMMEDIATE FIX");
        
        GameObject adam = GameObject.Find("Adam_Big");
        GameObject remy = GameObject.Find("Remy");
        
        if (adam == null)
        {
            Debug.LogError("❌ Adam_Big not found!");
            return;
        }
        
        // CORE ISSUE #1: Adam has wrong tag
        Debug.Log($"Adam current tag: '{adam.tag}' (should be 'Player')");
        if (adam.tag != "Player")
        {
            adam.tag = "Player";
            Debug.Log("✅ Changed Adam tag to 'Player'");
        }
        
        // Prevent tag conflicts
        if (remy != null && remy.tag == "Player")
        {
            remy.tag = "Enemy";
            Debug.Log("✅ Changed Remy tag to 'Enemy' to avoid conflict");
        }
        
        // CORE ISSUE #2: Ensure Adam has Avatar component
        var adamAvatar = adam.GetComponent<Tracking4All.Avatar>();
        if (adamAvatar == null)
        {
            adamAvatar = adam.AddComponent<Tracking4All.Avatar>();
            Debug.Log("✅ Added Avatar component to Adam");
        }
        
        // CORE ISSUE #3: Ensure Adam has AvatarSkeleton
        var adamSkeleton = adam.GetComponent<Tracking4All.AvatarSkeleton>();
        if (adamSkeleton == null)
        {
            adamSkeleton = adam.AddComponent<Tracking4All.AvatarSkeleton>();
            Debug.Log("✅ Added AvatarSkeleton to Adam");
        }
        
        // CORE ISSUE #4: Disable all other Avatar components
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        foreach (var avatar in allAvatars)
        {
            avatar.enabled = false;
        }
        Debug.Log($"✅ Disabled {allAvatars.Length} Avatar components");
        
        // CORE ISSUE #5: Enable only Adam's Avatar
        adamAvatar.enabled = true;
        Debug.Log("✅ Enabled Adam's Avatar component");
        
        // Verification
        StartCoroutine(VerifyFix());
        
        Debug.Log("🎉 ADAM TRACKING FIX COMPLETE!");
    }
    
    private System.Collections.IEnumerator VerifyFix()
    {
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("🔍 VERIFYING FIX...");
        
        GameObject adam = GameObject.Find("Adam_Big");
        if (adam != null)
        {
            Debug.Log($"Adam tag: {adam.tag}");
            
            var avatarComp = adam.GetComponent<Tracking4All.Avatar>();
            Debug.Log($"Adam Avatar component enabled: {avatarComp?.enabled}");
        }
        
        Tracking4All.Avatar[] enabledAvatars = System.Array.FindAll(FindObjectsOfType<Tracking4All.Avatar>(), av => av.enabled);
        Debug.Log($"Total enabled Avatar components: {enabledAvatars.Length}");
        
        foreach (var av in enabledAvatars)
        {
            Debug.Log($"  - {av.gameObject.name} (tag: {av.gameObject.tag})");
        }
        
        if (enabledAvatars.Length == 1 && enabledAvatars[0].gameObject.name.Contains("Adam"))
        {
            Debug.Log("✅ VERIFICATION PASSED: Adam tracking should now work!");
        }
        else
        {
            Debug.LogWarning("⚠️ VERIFICATION ISSUE: Expected exactly 1 enabled Avatar on Adam");
        }
    }
    
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("🔍 CURRENT ADAM TRACKING STATE:");
        
        GameObject adam = GameObject.Find("Adam_Big");
        GameObject remy = GameObject.Find("Remy");
        
        if (adam != null)
        {
            Debug.Log($"Adam_Big - Tag: '{adam.tag}', Active: {adam.activeInHierarchy}");
            var avatarComp = adam.GetComponent<Tracking4All.Avatar>();
            Debug.Log($"  Avatar component: {(avatarComp != null ? $"EXISTS (enabled: {avatarComp.enabled})" : "MISSING")}");
            
            var skeletonComp = adam.GetComponent<Tracking4All.AvatarSkeleton>();
            Debug.Log($"  AvatarSkeleton: {(skeletonComp != null ? "EXISTS" : "MISSING")}");
        }
        
        if (remy != null)
        {
            Debug.Log($"Remy - Tag: '{remy.tag}', Active: {remy.activeInHierarchy}");
            var avatarComp = remy.GetComponent<Tracking4All.Avatar>();
            Debug.Log($"  Avatar component: {(avatarComp != null ? $"EXISTS (enabled: {avatarComp.enabled})" : "MISSING")}");
        }
        
        Tracking4All.Avatar[] allAvatars = FindObjectsOfType<Tracking4All.Avatar>();
        Debug.Log($"Total Avatar components in scene: {allAvatars.Length}");
        foreach (var av in allAvatars)
        {
            Debug.Log($"  - {av.gameObject.name}: enabled={av.enabled}");
        }
    }
}
