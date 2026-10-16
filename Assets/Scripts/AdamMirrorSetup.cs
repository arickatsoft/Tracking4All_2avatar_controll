using UnityEngine;

/// <summary>
/// Simple component to quickly set up Adam mirror functionality.
/// Attach this to any GameObject in the scene for automatic mirror management.
/// </summary>
public class AdamMirrorSetup : MonoBehaviour
{
    [Header("Quick Setup")]
    [Tooltip("Check this to automatically enable mirror mode when Adam is selected")]
    public bool enableAutoMirror = true;
    
    [Header("Manual Control")]
    [Tooltip("Click this button in play mode to test mirror ON")]
    public bool testMirrorOn = false;
    
    [Tooltip("Click this button in play mode to test mirror OFF")]  
    public bool testMirrorOff = false;
    
    private TrackingMirrorManager mirrorManager;
    
    void Start()
    {
        // Find existing mirror manager or create one
        mirrorManager = FindObjectOfType<TrackingMirrorManager>();
        
        if (mirrorManager == null && enableAutoMirror)
        {
            // Create the mirror manager
            GameObject managerObj = new GameObject("TrackingMirrorManager");
            mirrorManager = managerObj.AddComponent<TrackingMirrorManager>();
            
            Debug.Log("🪞 AdamMirrorSetup: Created TrackingMirrorManager automatically");
        }
        
        if (mirrorManager != null)
        {
            mirrorManager.autoMirrorForAdam = enableAutoMirror;
            Debug.Log($"🪞 AdamMirrorSetup: Auto mirror for Adam = {enableAutoMirror}");
        }
    }
    
    void Update()
    {
        // Handle test buttons in the inspector
        if (testMirrorOn)
        {
            testMirrorOn = false;
            TestMirrorOn();
        }
        
        if (testMirrorOff)
        {
            testMirrorOff = false;
            TestMirrorOff();
        }
    }
    
    void TestMirrorOn()
    {
        if (mirrorManager == null)
        {
            Debug.LogWarning("⚠️ No TrackingMirrorManager found for testing");
            return;
        }
        
        Debug.Log("🧪 Testing: Mirror ON (Adam mode)");
        mirrorManager.EnableMirrorForAdam();
    }
    
    void TestMirrorOff()
    {
        if (mirrorManager == null)
        {
            Debug.LogWarning("⚠️ No TrackingMirrorManager found for testing");
            return;
        }
        
        Debug.Log("🧪 Testing: Mirror OFF (Remy mode)");
        mirrorManager.DisableMirrorForRemy();
    }
    
    /// <summary>
    /// Call this method when Adam is selected
    /// </summary>
    public void OnAdamSelected()
    {
        if (mirrorManager != null)
        {
            mirrorManager.OnAdamSelected();
        }
        else
        {
            Debug.LogWarning("⚠️ TrackingMirrorManager not found - cannot set mirror for Adam");
        }
    }
    
    /// <summary>
    /// Call this method when Remy is selected
    /// </summary>
    public void OnRemySelected()
    {
        if (mirrorManager != null)
        {
            mirrorManager.OnRemySelected();
        }
        else
        {
            Debug.LogWarning("⚠️ TrackingMirrorManager not found - cannot disable mirror for Remy");
        }
    }
}
