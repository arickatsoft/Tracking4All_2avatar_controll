using UnityEngine;

public class FixHitPosition : MonoBehaviour
{
    [Header("🎯 FIX HIT POSITION")]
    [Space(10)]
    
    [Header("Position Settings")]
    public string adamName = "Adam_Big";
    public string remyName = "Remy";
    public float chestHeight = 2.2f; // Higher up on chest
    public float shoulderWidth = 0.4f; // Narrower, focused on torso
    
    void Start()
    {
        FixHitPositions();
    }
    
    [ContextMenu("🎯 Fix Hit Positions")]
    public void FixHitPositions()
    {
        Debug.Log("🎯 FIXING HIT POSITIONS TO CHEST LEVEL...");
        Debug.Log("==========================================");
        
        FixCharacterHitPosition(adamName, "Enemy");
        FixCharacterHitPosition(remyName, "Player");
        
        Debug.Log("==========================================");
        Debug.Log("✅ Hit positions fixed to CHEST level!");
        Debug.Log("🎯 Hits should now appear at chest/shoulders, NOT pants!");
    }
    
    void FixCharacterHitPosition(string characterName, string attackerTag)
    {
        Debug.Log($"\n🎯 Fixing {characterName} hit position...");
        
        GameObject character = GameObject.Find(characterName);
        if (character == null)
        {
            Debug.LogError($"❌ {characterName} not found!");
            return;
        }
        
        // Remove ALL existing hit components and colliders
        RemoveAllHitComponents(character);
        RemoveAllColliders(character);
        
        // Create a chest-level collider
        CreateChestLevelCollider(character);
        
        // Add chest-level hit detection
        AddChestLevelHitDetection(character, attackerTag);
        
        Debug.Log($"✅ {characterName} hit position fixed to chest level!");
    }
    
    void RemoveAllHitComponents(GameObject character)
    {
        Debug.Log($"   🗑️ Removing all hit components...");
        
        // Remove from main object and all children
        Transform[] allTransforms = character.GetComponentsInChildren<Transform>();
        
        foreach (Transform t in allTransforms)
        {
            // Remove all hit-related components
            Component[] components = t.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null) continue;
                
                string compName = comp.GetType().Name;
                if (compName.Contains("Hit") && 
                    compName != "Transform" && 
                    compName != "GameObject")
                {
                    DestroyImmediate(comp);
                }
            }
        }
    }
    
    void RemoveAllColliders(GameObject character)
    {
        Debug.Log($"   🗑️ Removing all colliders...");
        
        // Remove from main object and all children
        Collider[] allColliders = character.GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            DestroyImmediate(col);
        }
    }
    
    void CreateChestLevelCollider(GameObject character)
    {
        Debug.Log($"   🔲 Creating chest-level collider...");
        
        // Create a collider positioned at chest level
        CapsuleCollider chestCollider = character.AddComponent<CapsuleCollider>();
        
        // Position it at chest/shoulder level (much higher than before)
        chestCollider.center = new Vector3(0, chestHeight, 0); // High on chest
        chestCollider.height = 0.8f; // Short, just chest area
        chestCollider.radius = shoulderWidth; // Narrow, just torso width
        chestCollider.isTrigger = true;
        
        Debug.Log($"      📍 Center: (0, {chestHeight}, 0) - CHEST LEVEL");
        Debug.Log($"      📏 Height: {chestCollider.height} - Just chest area");
        Debug.Log($"      📐 Radius: {chestCollider.radius} - Torso width");
        Debug.Log($"      🎯 IsTrigger: {chestCollider.isTrigger}");
    }
    
    void AddChestLevelHitDetection(GameObject character, string attackerTag)
    {
        Debug.Log($"   🎯 Adding chest-level hit detection...");
        
        ChestHitDetector detector = character.AddComponent<ChestHitDetector>();
        detector.attackerTag = attackerTag;
        detector.chestHeight = chestHeight;
        detector.enableDebugLogs = true;
        
        Debug.Log($"      ✅ Added ChestHitDetector");
        Debug.Log($"      👀 Watching for: '{attackerTag}'");
        Debug.Log($"      📍 Hit height: {chestHeight} (chest level)");
    }
    
    [ContextMenu("🧪 Test Chest Level Hits")]
    public void TestChestLevelHits()
    {
        Debug.Log("🧪 TESTING CHEST LEVEL HITS...");
        
        GameObject adam = GameObject.Find(adamName);
        GameObject remy = GameObject.Find(remyName);
        
        if (adam != null)
        {
            Vector3 chestPos = adam.transform.position + Vector3.up * chestHeight;
            CreateTestHitEffect(chestPos, "Adam CHEST Hit");
            Debug.Log($"🎯 Adam chest hit at: {chestPos}");
        }
        
        if (remy != null)
        {
            Vector3 chestPos = remy.transform.position + Vector3.up * chestHeight;
            CreateTestHitEffect(chestPos, "Remy CHEST Hit");
            Debug.Log($"🎯 Remy chest hit at: {chestPos}");
        }
        
        Debug.Log("✅ You should see hits at CHEST level, NOT pants level!");
    }
    
    void CreateTestHitEffect(Vector3 position, string name)
    {
        StartCoroutine(CreateChestHitCircle(position, name));
    }
    
    System.Collections.IEnumerator CreateChestHitCircle(Vector3 position, string name)
    {
        // Create a distinct chest-level hit effect
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        circle.name = name;
        circle.transform.position = position;
        circle.transform.localScale = Vector3.one * 0.3f;
        
        // Bright red color for chest hits (different from orange)
        Renderer renderer = circle.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.red; // RED for chest hits to distinguish from other hits
        renderer.material = mat;
        
        // Remove collider
        Destroy(circle.GetComponent<Collider>());
        
        Debug.Log($"🔴 Created CHEST hit: {name} at {position}");
        
        // Animate for 4 seconds (longer to see clearly)
        float duration = 4f;
        float elapsed = 0f;
        Vector3 startScale = circle.transform.localScale;
        Vector3 targetScale = startScale * 2f; // Smaller growth
        
        while (elapsed < duration)
        {
            if (circle == null) break;
            
            float progress = elapsed / duration;
            circle.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (circle != null)
        {
            Destroy(circle);
            Debug.Log($"🗑️ Destroyed chest hit: {name}");
        }
    }
    
    [ContextMenu("📊 Analyze Hit Positions")]
    public void AnalyzeHitPositions()
    {
        Debug.Log("📊 ANALYZING HIT POSITIONS...");
        Debug.Log("==============================");
        
        AnalyzeCharacterHitPosition(adamName);
        AnalyzeCharacterHitPosition(remyName);
        
        Debug.Log("==============================");
    }
    
    void AnalyzeCharacterHitPosition(string characterName)
    {
        GameObject character = GameObject.Find(characterName);
        if (character == null)
        {
            Debug.LogError($"❌ {characterName} not found!");
            return;
        }
        
        Debug.Log($"\n👤 {characterName} HIT POSITION ANALYSIS:");
        Debug.Log($"   📍 Character base position: {character.transform.position}");
        
        Collider collider = character.GetComponent<Collider>();
        if (collider != null)
        {
            Vector3 colliderCenter = character.transform.position + collider.bounds.center;
            Debug.Log($"   🔲 Collider center world pos: {colliderCenter}");
            Debug.Log($"   📏 Collider bounds: {collider.bounds}");
            
            float heightFromGround = colliderCenter.y - character.transform.position.y;
            Debug.Log($"   📐 Hit height from ground: {heightFromGround}");
            
            if (heightFromGround < 1.5f)
            {
                Debug.Log($"      🚨 TOO LOW! This will hit pants/waist area");
            }
            else if (heightFromGround > 2.0f)
            {
                Debug.Log($"      ✅ GOOD HEIGHT! This will hit chest/shoulder area");
            }
            else
            {
                Debug.Log($"      ⚠️ MEDIUM HEIGHT! Might hit stomach area");
            }
        }
        else
        {
            Debug.Log($"   ❌ No collider found!");
        }
        
        ChestHitDetector detector = character.GetComponent<ChestHitDetector>();
        if (detector != null)
        {
            Debug.Log($"   🎯 ChestHitDetector: ✅ Present");
            Debug.Log($"      📍 Chest height: {detector.chestHeight}");
            Debug.Log($"      👀 Attacker tag: '{detector.attackerTag}'");
        }
        else
        {
            Debug.Log($"   🎯 ChestHitDetector: ❌ Missing");
        }
    }
    
    /*
    void OnGUI()
    {
        GUI.Box(new Rect(10, 820, 400, 140), "🎯 FIX HIT POSITION");
        
        if (GUI.Button(new Rect(20, 845, 120, 25), "🎯 Fix Positions"))
        {
            FixHitPositions();
        }
        
        if (GUI.Button(new Rect(150, 845, 120, 25), "🧪 Test Chest"))
        {
            TestChestLevelHits();
        }
        
        if (GUI.Button(new Rect(280, 845, 120, 25), "📊 Analyze"))
        {
            AnalyzeHitPositions();
        }
        
        GUI.Label(new Rect(20, 880, 380, 20), "🚨 PROBLEM: Hits appearing at pants/waist level");
        GUI.Label(new Rect(20, 900, 380, 20), "🎯 SOLUTION: Move collider to chest level (Y=2.2)");
        GUI.Label(new Rect(20, 920, 380, 20), "✅ RESULT: RED circles at chest, not orange at pants");
        GUI.Label(new Rect(20, 940, 380, 20), $"⚙️ Current chest height: {chestHeight}");
    }
    */
}

// Specialized chest-level hit detector
public class ChestHitDetector : MonoBehaviour
{
    [Header("🎯 CHEST HIT DETECTOR")]
    public string attackerTag = "Player";
    public float chestHeight = 2.2f;
    public bool enableDebugLogs = true;
    
    void OnTriggerEnter(Collider other)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 CHEST TRIGGER: {other.name} (tag: '{other.tag}') -> {gameObject.name}");
        }
        
        if (other.CompareTag(attackerTag))
        {
            Vector3 chestHitPos = transform.position + Vector3.up * chestHeight;
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ CHEST HIT DETECTED!");
                Debug.Log($"   👊 Attacker: {other.name}");
                Debug.Log($"   🎯 Target: {gameObject.name}");
                Debug.Log($"   📍 CHEST Hit Position: {chestHitPos}");
                Debug.Log($"   📐 Height from ground: {chestHeight}");
            }
            
            ShowChestHit(chestHitPos);
        }
    }
    
    void ShowChestHit(Vector3 position)
    {
        // Use SimpleHitIndicator if available
        if (SimpleHitIndicator.Instance != null)
        {
            SimpleHitIndicator.Instance.ShowHitAtPosition(position);
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ Used SimpleHitIndicator at CHEST position: {position}");
            }
        }
        else
        {
            // Create our own chest-level effect
            StartCoroutine(CreateChestEffect(position));
        }
    }
    
    System.Collections.IEnumerator CreateChestEffect(Vector3 position)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "ChestHit";
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 0.4f;
        
        // Bright orange for chest hits
        Renderer renderer = effect.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.5f, 0f, 0.9f); // Bright orange
        renderer.material = mat;
        
        // Remove collider
        Destroy(effect.GetComponent<Collider>());
        
        if (enableDebugLogs)
        {
            Debug.Log($"🟠 Created CHEST effect at: {position}");
        }
        
        // Animate
        float duration = 2f;
        float elapsed = 0f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 targetScale = startScale * 3f;
        
        while (elapsed < duration && effect != null)
        {
            float progress = elapsed / duration;
            effect.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            
            // Fade
            Color color = mat.color;
            color.a = (1f - progress) * 0.9f;
            mat.color = color;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (effect != null)
        {
            Destroy(effect);
        }
    }
}
