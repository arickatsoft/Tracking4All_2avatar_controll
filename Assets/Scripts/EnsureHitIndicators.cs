using UnityEngine;

public class EnsureHitIndicators : MonoBehaviour
{
    [Header("Auto-Setup Hit Indicators")]
    public bool setupOnStart = true;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupHitIndicators();
        }
    }
    
    [ContextMenu("Setup Hit Indicators")]
    public void SetupHitIndicators()
    {
        Debug.Log("🎯 Setting up hit indicator systems...");
        
        // Ensure SimpleHitIndicator exists
        if (SimpleHitIndicator.Instance == null)
        {
            GameObject indicatorGO = new GameObject("SimpleHitIndicator");
            indicatorGO.AddComponent<SimpleHitIndicator>();
            Debug.Log("✅ Created SimpleHitIndicator");
        }
        else
        {
            Debug.Log("✅ SimpleHitIndicator already exists");
        }
        
        // Check for LocalHitIndicator
        LocalHitIndicator localUI = FindObjectOfType<LocalHitIndicator>();
        if (localUI == null)
        {
            Debug.LogWarning("⚠️ LocalHitIndicator not found - will use SimpleHitIndicator only");
        }
        else
        {
            Debug.Log("✅ LocalHitIndicator found");
        }
        
        // Check for LocalWorldHitIndicator
        LocalWorldHitIndicator localWorld = FindObjectOfType<LocalWorldHitIndicator>();
        if (localWorld == null)
        {
            Debug.LogWarning("⚠️ LocalWorldHitIndicator not found - will use SimpleHitIndicator only");
        }
        else
        {
            Debug.Log("✅ LocalWorldHitIndicator found");
        }
        
        Debug.Log("🎯 Hit indicator setup complete!");
        Debug.Log("💡 You should now see orange circles when hits occur!");
    }
}
