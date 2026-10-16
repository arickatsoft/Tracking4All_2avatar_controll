using UnityEngine;

public class DisableOldHitSystems : MonoBehaviour
{
    [Header("Auto-Disable Old Hit Systems")]
    public bool disableOnStart = true;
    
    void Start()
    {
        if (disableOnStart)
        {
            DisableOldSystems();
        }
    }
    
    [ContextMenu("Disable Old Hit Systems")]
    public void DisableOldSystems()
    {
        Debug.Log("🔧 Disabling old hit detection systems that cause screen flash...");
        
        // Find and disable old hit triggers that cause screen flash
        DisableComponent<RemyHitsAdamTrigger>();
        
        // Find other problematic hit handlers
        var hitHandlers = FindObjectsOfType<HitCollisionHandler>();
        foreach (var handler in hitHandlers)
        {
            if (handler.enabled)
            {
                Debug.Log($"⚠️ Found active HitCollisionHandler on {handler.gameObject.name} - this may cause screen flash");
                // Don't auto-disable, just warn user
            }
        }
        
        // Find collision diagnostics that might interfere
        DisableComponent<CollisionDiagnostic>();
        DisableComponent<FixedCollisionDetector>();
        DisableComponent<RemyAdamCollisionDetector>();
        
        Debug.Log("✅ Old hit systems check complete!");
        Debug.Log("💡 If you still see screen flash, check that LocalHitSystemSetup has run correctly.");
    }
    
    private void DisableComponent<T>() where T : MonoBehaviour
    {
        var components = FindObjectsOfType<T>();
        foreach (var component in components)
        {
            if (component.enabled)
            {
                component.enabled = false;
                Debug.Log($"🚫 Disabled {typeof(T).Name} on {component.gameObject.name}");
            }
        }
        
        if (components.Length == 0)
        {
            Debug.Log($"✅ No {typeof(T).Name} components found");
        }
    }
}
