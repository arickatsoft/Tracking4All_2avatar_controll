using UnityEngine;

public class HitDetectionDebugger : MonoBehaviour
{
    [Header("Hit Detection Analysis")]
    public bool debugOnStart = true;
    
    void Start()
    {
        if (debugOnStart)
        {
            AnalyzeHitDetection();
        }
    }
    
    [ContextMenu("Analyze Hit Detection")]
    public void AnalyzeHitDetection()
    {
        Debug.Log("🔍 ANALYZING HIT DETECTION SETUP...");
        Debug.Log("=====================================");
        
        AnalyzeCharacter("Adam_Big");
        AnalyzeCharacter("Remy");
        
        CheckHitSystems();
        
        Debug.Log("=====================================");
        Debug.Log("🔍 Analysis complete! Check logs above.");
    }
    
    void AnalyzeCharacter(string characterName)
    {
        Debug.Log($"\n👤 ANALYZING: {characterName}");
        
        GameObject character = GameObject.Find(characterName);
        if (character == null)
        {
            Debug.LogError($"❌ {characterName} not found!");
            return;
        }
        
        Debug.Log($"✅ Found {characterName} at position: {character.transform.position}");
        
        // Check components
        Component[] components = character.GetComponents<Component>();
        Debug.Log($"📦 Components on {characterName}:");
        
        bool hasHitDetection = false;
        
        foreach (Component comp in components)
        {
            string compName = comp.GetType().Name;
            Debug.Log($"   - {compName}");
            
            // Check for hit detection components
            if (compName.Contains("Hit") || compName.Contains("Collision") || compName.Contains("Trigger"))
            {
                hasHitDetection = true;
                Debug.Log($"     🎯 HIT DETECTION COMPONENT FOUND!");
                
                // Check if it's enabled
                if (comp is MonoBehaviour mb)
                {
                    Debug.Log($"     📊 Enabled: {mb.enabled}");
                }
            }
            
            // Check colliders
            if (comp is Collider col)
            {
                Debug.Log($"     🔲 Collider - Is Trigger: {col.isTrigger}, Enabled: {col.enabled}");
            }
        }
        
        if (!hasHitDetection)
        {
            Debug.LogWarning($"⚠️ {characterName} has NO hit detection components!");
        }
        
        // Check tags
        Debug.Log($"🏷️ Tag: {character.tag}");
    }
    
    void CheckHitSystems()
    {
        Debug.Log($"\n🎯 CHECKING HIT INDICATOR SYSTEMS:");
        
        // Check SimpleHitIndicator
        if (SimpleHitIndicator.Instance != null)
        {
            Debug.Log("✅ SimpleHitIndicator exists");
        }
        else
        {
            Debug.LogError("❌ SimpleHitIndicator NOT found!");
        }
        
        // Check other hit systems
        LocalHitIndicator localUI = FindObjectOfType<LocalHitIndicator>();
        Debug.Log($"LocalHitIndicator: {(localUI != null ? "✅ Found" : "❌ Missing")}");
        
        LocalWorldHitIndicator localWorld = FindObjectOfType<LocalWorldHitIndicator>();
        Debug.Log($"LocalWorldHitIndicator: {(localWorld != null ? "✅ Found" : "❌ Missing")}");
        
        HitFeedback hitFeedback = FindObjectOfType<HitFeedback>();
        Debug.Log($"HitFeedback: {(hitFeedback != null ? "✅ Found" : "❌ Missing")}");
    }
    
    [ContextMenu("Force Create Hit Detection")]
    public void ForceCreateHitDetection()
    {
        Debug.Log("🔧 FORCE CREATING HIT DETECTION...");
        
        GameObject adam = GameObject.Find("Adam_Big");
        GameObject remy = GameObject.Find("Remy");
        
        if (adam != null)
        {
            SetupHitDetectionOnCharacter(adam, "Player");
        }
        
        if (remy != null)
        {
            SetupHitDetectionOnCharacter(remy, "Enemy");
        }
        
        Debug.Log("✅ Force setup complete!");
    }
    
    void SetupHitDetectionOnCharacter(GameObject character, string attackerTag)
    {
        Debug.Log($"🔧 Setting up hit detection on {character.name}");
        
        // Ensure collider exists and is trigger
        Collider col = character.GetComponent<Collider>();
        if (col == null)
        {
            col = character.AddComponent<CapsuleCollider>();
            Debug.Log("➕ Added CapsuleCollider");
        }
        col.isTrigger = true;
        
        // Add simple hit detection script
        SimpleHitDetector detector = character.GetComponent<SimpleHitDetector>();
        if (detector == null)
        {
            detector = character.AddComponent<SimpleHitDetector>();
            detector.attackerTag = attackerTag;
            Debug.Log($"➕ Added SimpleHitDetector with attacker tag: {attackerTag}");
        }
    }
}

// Simple hit detector that just shows circles
public class SimpleHitDetector : MonoBehaviour
{
    public string attackerTag = "Player";
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(attackerTag))
        {
            Debug.Log($"🎯 HIT DETECTED! {other.name} hit {gameObject.name}");
            
            Vector3 hitPosition = other.ClosestPoint(transform.position);
            Debug.Log($"📍 Hit position: {hitPosition}");
            
            // Show hit circle
            if (SimpleHitIndicator.Instance != null)
            {
                SimpleHitIndicator.Instance.ShowHitAtPosition(hitPosition);
                Debug.Log("✅ Showing hit circle via SimpleHitIndicator");
            }
            else
            {
                Debug.LogError("❌ SimpleHitIndicator not found!");
            }
        }
        else
        {
            Debug.Log($"🚫 Wrong tag hit: {other.name} (tag: {other.tag}) vs expected: {attackerTag}");
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"💥 COLLISION: {collision.gameObject.name} collided with {gameObject.name}");
    }
}
