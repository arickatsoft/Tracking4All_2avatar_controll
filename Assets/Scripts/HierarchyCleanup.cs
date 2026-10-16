using UnityEngine;

public class HierarchyCleanup : MonoBehaviour
{
    [Header("Auto-Clean Conflicting Systems")]
    public bool cleanOnStart = true;
    
    void Start()
    {
        if (cleanOnStart)
        {
            CleanupHitSystems();
        }
    }
    
    [ContextMenu("Clean Up Hit Systems")]
    public void CleanupHitSystems()
    {
        Debug.Log("🧹 Cleaning up conflicting hit systems...");
        
        // Keep only the essential hit system components
        KeepEssentialSystems();
        
        // Remove duplicate/conflicting systems
        RemoveConflictingSystems();
        
        // Ensure SimpleHitIndicator is present and working
        EnsureSimpleHitIndicator();
        
        Debug.Log("✅ Cleanup complete! Testing hit detection...");
    }
    
    private void KeepEssentialSystems()
    {
        Debug.Log("📋 Essential systems to keep:");
        Debug.Log("✅ Adam_Big - Main character");
        Debug.Log("✅ Remy - Main character");
        Debug.Log("✅ SoundManager - Audio system");
        Debug.Log("✅ HitFeedback - Camera shake");
        
        // Find essential GameObjects
        GameObject adam = GameObject.Find("Adam_Big");
        GameObject remy = GameObject.Find("Remy");
        
        if (adam != null)
        {
            Debug.Log($"✅ Found Adam_Big at position: {adam.transform.position}");
        }
        
        if (remy != null)
        {
            Debug.Log($"✅ Found Remy at position: {remy.transform.position}");
        }
    }
    
    private void RemoveConflictingSystems()
    {
        Debug.Log("🚫 Removing potentially conflicting systems...");
        
        // List of GameObjects that might be causing conflicts
        string[] conflictingObjects = {
            "NetworkGameManager",
            "UIHitIndicatorManager", 
            "WorldHitIndicatorManager",
            "HitSystemSetupManager",
            "LocalHitSystemManager"
        };
        
        foreach (string objName in conflictingObjects)
        {
            GameObject obj = GameObject.Find(objName);
            if (obj != null)
            {
                Debug.Log($"🚫 Disabling conflicting system: {objName}");
                obj.SetActive(false);
            }
        }
    }
    
    private void EnsureSimpleHitIndicator()
    {
        Debug.Log("🎯 Ensuring SimpleHitIndicator is ready...");
        
        if (SimpleHitIndicator.Instance == null)
        {
            GameObject indicatorGO = new GameObject("SimpleHitIndicator");
            var indicator = indicatorGO.AddComponent<SimpleHitIndicator>();
            
            // Make sure it's configured for visibility
            indicator.hitColor = new Color(1f, 0.5f, 0f, 0.9f); // Bright orange
            indicator.circleSize = 3.0f; // Larger size
            indicator.duration = 2.0f; // Longer duration
            
            Debug.Log("✅ Created SimpleHitIndicator with enhanced visibility settings");
        }
        else
        {
            Debug.Log("✅ SimpleHitIndicator already exists");
        }
    }
    
    [ContextMenu("Test Hit Detection")]
    public void TestHitDetection()
    {
        Debug.Log("🧪 Testing hit detection...");
        
        GameObject adam = GameObject.Find("Adam_Big");
        if (adam != null && SimpleHitIndicator.Instance != null)
        {
            Vector3 testPosition = adam.transform.position + Vector3.up * 1.5f;
            SimpleHitIndicator.Instance.ShowHitAtPosition(testPosition);
            Debug.Log($"🎯 Test hit circle should appear at: {testPosition}");
        }
        else
        {
            Debug.LogError("❌ Cannot test - Adam_Big or SimpleHitIndicator not found!");
        }
    }
}
