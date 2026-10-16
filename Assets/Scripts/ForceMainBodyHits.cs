using UnityEngine;

public class ForceMainBodyHits : MonoBehaviour
{
    [Header("💪 FORCE MAIN BODY HIT DETECTION")]
    [Space(10)]
    
    [Header("Settings")]
    public string adamName = "Adam_Big";
    public string remyName = "Remy";
    public bool aggressiveFix = true;
    
    void Start()
    {
        ForceFixMainBodyHits();
    }
    
    [ContextMenu("💪 Force Fix Main Body Hits")]
    public void ForceFixMainBodyHits()
    {
        Debug.Log("💪 FORCING MAIN BODY HIT DETECTION...");
        Debug.Log("=====================================");
        
        // Aggressive fix for both characters
        ForceFixCharacter(adamName, "Enemy");
        ForceFixCharacter(remyName, "Player");
        
        Debug.Log("=====================================");
        Debug.Log("✅ FORCED MAIN BODY HIT DETECTION COMPLETE!");
        Debug.Log("🎯 Hits should now appear on torso/chest area, NOT feet!");
    }
    
    void ForceFixCharacter(string characterName, string attackerTag)
    {
        Debug.Log($"\n💪 FORCE FIXING {characterName}...");
        
        GameObject character = GameObject.Find(characterName);
        if (character == null)
        {
            Debug.LogError($"❌ {characterName} not found!");
            return;
        }
        
        // STEP 1: COMPLETELY DISABLE ALL CHILD COLLIDERS
        DisableAllChildColliders(character);
        
        // STEP 2: REMOVE ALL EXISTING HIT DETECTION EVERYWHERE
        RemoveAllHitDetection(character);
        
        // STEP 3: CREATE CLEAN MAIN BODY COLLIDER
        CreateCleanMainBodyCollider(character);
        
        // STEP 4: ADD MAIN BODY HIT DETECTION ONLY
        AddMainBodyHitDetectionOnly(character, attackerTag);
        
        Debug.Log($"✅ {characterName} forced to main body hits only!");
    }
    
    void DisableAllChildColliders(GameObject character)
    {
        Debug.Log($"   🚫 DISABLING ALL CHILD COLLIDERS...");
        
        // Get ALL colliders on children (not the main object)
        Collider[] allColliders = character.GetComponentsInChildren<Collider>();
        
        int disabledCount = 0;
        foreach (Collider col in allColliders)
        {
            // Skip the main character's collider
            if (col.gameObject == character) continue;
            
            // Disable child colliders completely
            col.enabled = false;
            disabledCount++;
            
            Debug.Log($"      ❌ DISABLED: {col.gameObject.name} ({col.GetType().Name})");
            
            // Also remove any hit detection components from children
            RemoveHitComponentsFromObject(col.gameObject);
        }
        
        Debug.Log($"   📊 Disabled {disabledCount} child colliders");
    }
    
    void RemoveAllHitDetection(GameObject character)
    {
        Debug.Log($"   🗑️ REMOVING ALL HIT DETECTION...");
        
        // Remove from main object
        RemoveHitComponentsFromObject(character);
        
        // Remove from all children too
        Transform[] children = character.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            RemoveHitComponentsFromObject(child.gameObject);
        }
    }
    
    void RemoveHitComponentsFromObject(GameObject obj)
    {
        // Remove all types of hit detection components
        Component[] hitComponents = obj.GetComponents<Component>();
        foreach (Component comp in hitComponents)
        {
            if (comp == null) continue;
            
            string compName = comp.GetType().Name;
            if (compName.Contains("Hit") && 
                compName != "Transform" && 
                compName != "GameObject")
            {
                DestroyImmediate(comp);
                Debug.Log($"      🗑️ Removed {compName} from {obj.name}");
            }
        }
    }
    
    void CreateCleanMainBodyCollider(GameObject character)
    {
        Debug.Log($"   🔲 CREATING CLEAN MAIN BODY COLLIDER...");
        
        // Remove ALL existing colliders from main object
        Collider[] existingColliders = character.GetComponents<Collider>();
        foreach (Collider col in existingColliders)
        {
            DestroyImmediate(col);
            Debug.Log($"      🗑️ Removed existing collider: {col.GetType().Name}");
        }
        
        // Create a single, clean main body collider
        CapsuleCollider mainCollider = character.AddComponent<CapsuleCollider>();
        
        // Position it on the torso/chest area (NOT feet)
        mainCollider.center = new Vector3(0, 1.5f, 0); // Higher up on torso
        mainCollider.height = 1.5f; // Shorter, focused on torso
        mainCollider.radius = 0.6f; // Wide enough for arms/shoulders
        mainCollider.isTrigger = true; // For hit detection
        
        Debug.Log($"      ✅ Created main body collider:");
        Debug.Log($"         📍 Center: {mainCollider.center} (torso level)");
        Debug.Log($"         📏 Height: {mainCollider.height} (torso only)");
        Debug.Log($"         📐 Radius: {mainCollider.radius}");
        Debug.Log($"         🎯 IsTrigger: {mainCollider.isTrigger}");
    }
    
    void AddMainBodyHitDetectionOnly(GameObject character, string attackerTag)
    {
        Debug.Log($"   🎯 ADDING MAIN BODY HIT DETECTION ONLY...");
        
        // Add our custom main body hit detector
        MainBodyHitDetector detector = character.AddComponent<MainBodyHitDetector>();
        detector.attackerTag = attackerTag;
        detector.hitPosition = MainBodyHitDetector.HitPosition.Torso;
        detector.enableDebugLogs = true;
        
        Debug.Log($"      ✅ Added MainBodyHitDetector");
        Debug.Log($"      👀 Watching for: '{attackerTag}'");
        Debug.Log($"      🎯 Hit position: Torso");
    }
    
    [ContextMenu("🧪 Test Main Body Hits")]
    public void TestMainBodyHits()
    {
        Debug.Log("🧪 TESTING MAIN BODY HITS...");
        
        GameObject adam = GameObject.Find(adamName);
        GameObject remy = GameObject.Find(remyName);
        
        if (adam != null)
        {
            Vector3 torsoPos = adam.transform.position + Vector3.up * 1.5f; // Torso level
            CreateTestHitEffect(torsoPos, "Adam Torso Hit");
        }
        
        if (remy != null)
        {
            Vector3 torsoPos = remy.transform.position + Vector3.up * 1.5f; // Torso level
            CreateTestHitEffect(torsoPos, "Remy Torso Hit");
        }
        
        Debug.Log("🎯 You should see hits at TORSO level, NOT feet!");
    }
    
    void CreateTestHitEffect(Vector3 position, string name)
    {
        StartCoroutine(CreateTestHitCircle(position, name));
    }
    
    System.Collections.IEnumerator CreateTestHitCircle(Vector3 position, string name)
    {
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        circle.name = name;
        circle.transform.position = position;
        circle.transform.localScale = Vector3.one * 0.5f;
        
        // Bright orange color
        Renderer renderer = circle.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.5f, 0f, 1f); // Bright orange
        renderer.material = mat;
        
        // Remove collider
        Destroy(circle.GetComponent<Collider>());
        
        Debug.Log($"🟠 Created test hit: {name} at {position}");
        
        // Animate for 3 seconds
        float duration = 3f;
        float elapsed = 0f;
        Vector3 startScale = circle.transform.localScale;
        Vector3 targetScale = startScale * 3f;
        
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
        }
    }
    
    [ContextMenu("📊 Analyze Current Setup")]
    public void AnalyzeCurrentSetup()
    {
        Debug.Log("📊 ANALYZING CURRENT SETUP...");
        Debug.Log("==============================");
        
        AnalyzeCharacterSetup(adamName);
        AnalyzeCharacterSetup(remyName);
        
        Debug.Log("==============================");
    }
    
    void AnalyzeCharacterSetup(string characterName)
    {
        GameObject character = GameObject.Find(characterName);
        if (character == null)
        {
            Debug.LogError($"❌ {characterName} not found!");
            return;
        }
        
        Debug.Log($"\n👤 {characterName} ANALYSIS:");
        
        // Check main body collider
        Collider mainCollider = character.GetComponent<Collider>();
        Debug.Log($"   🔲 Main collider: {(mainCollider != null ? "✅ Present" : "❌ Missing")}");
        
        if (mainCollider != null)
        {
            Debug.Log($"      📍 Center: {mainCollider.bounds.center}");
            Debug.Log($"      📏 Size: {mainCollider.bounds.size}");
            Debug.Log($"      🎯 IsTrigger: {mainCollider.isTrigger}");
            Debug.Log($"      ✅ Enabled: {mainCollider.enabled}");
        }
        
        // Check main body hit detection
        MainBodyHitDetector detector = character.GetComponent<MainBodyHitDetector>();
        Debug.Log($"   🎯 MainBodyHitDetector: {(detector != null ? "✅ Present" : "❌ Missing")}");
        
        if (detector != null)
        {
            Debug.Log($"      👀 Attacker tag: '{detector.attackerTag}'");
            Debug.Log($"      📍 Hit position: {detector.hitPosition}");
        }
        
        // Check for disabled child colliders
        Collider[] allColliders = character.GetComponentsInChildren<Collider>();
        int enabledChildren = 0;
        int disabledChildren = 0;
        
        foreach (Collider col in allColliders)
        {
            if (col.gameObject == character) continue; // Skip main
            
            if (col.enabled)
            {
                enabledChildren++;
                Debug.Log($"      ⚠️ ENABLED child collider: {col.gameObject.name}");
            }
            else
            {
                disabledChildren++;
            }
        }
        
        Debug.Log($"   👶 Child colliders: {enabledChildren} enabled, {disabledChildren} disabled");
        
        if (enabledChildren > 0)
        {
            Debug.Log($"      🚨 PROBLEM: Child colliders still enabled - may cause foot hits!");
        }
        else
        {
            Debug.Log($"      ✅ GOOD: All child colliders disabled");
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 680, 400, 120), "💪 FORCE MAIN BODY HITS");
        
        if (GUI.Button(new Rect(20, 705, 120, 25), "💪 Force Fix"))
        {
            ForceFixMainBodyHits();
        }
        
        if (GUI.Button(new Rect(150, 705, 120, 25), "🧪 Test Torso"))
        {
            TestMainBodyHits();
        }
        
        if (GUI.Button(new Rect(280, 705, 120, 25), "📊 Analyze"))
        {
            AnalyzeCurrentSetup();
        }
        
        GUI.Label(new Rect(20, 740, 380, 20), "🎯 PROBLEM: Hits still on feet instead of torso");
        GUI.Label(new Rect(20, 760, 380, 20), "💪 SOLUTION: Disable ALL child colliders, main body only");
        GUI.Label(new Rect(20, 780, 380, 20), "✅ RESULT: Hits should appear at chest/torso level");
    }
}

// Simple main body hit detector that only works on torso
public class MainBodyHitDetector : MonoBehaviour
{
    public enum HitPosition { Torso, Head, Center }
    
    [Header("🎯 MAIN BODY HIT DETECTOR")]
    public string attackerTag = "Player";
    public HitPosition hitPosition = HitPosition.Torso;
    public bool enableDebugLogs = true;
    
    void OnTriggerEnter(Collider other)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 MAIN BODY TRIGGER: {other.name} (tag: '{other.tag}') -> {gameObject.name}");
        }
        
        if (other.CompareTag(attackerTag))
        {
            Vector3 hitPos = CalculateMainBodyHitPosition();
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ MAIN BODY HIT DETECTED!");
                Debug.Log($"   👊 Attacker: {other.name}");
                Debug.Log($"   🎯 Target: {gameObject.name}");
                Debug.Log($"   📍 Hit Position: {hitPos} ({hitPosition})");
            }
            
            ShowMainBodyHit(hitPos);
        }
    }
    
    Vector3 CalculateMainBodyHitPosition()
    {
        Vector3 basePos = transform.position;
        
        switch (hitPosition)
        {
            case HitPosition.Torso:
                return basePos + Vector3.up * 1.5f; // Chest/torso level
            case HitPosition.Head:
                return basePos + Vector3.up * 2f; // Head level
            case HitPosition.Center:
            default:
                return basePos + Vector3.up * 1f; // Center
        }
    }
    
    void ShowMainBodyHit(Vector3 position)
    {
        // Use SimpleHitIndicator if available
        if (SimpleHitIndicator.Instance != null)
        {
            SimpleHitIndicator.Instance.ShowHitAtPosition(position);
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ Used SimpleHitIndicator at main body position");
            }
        }
        else
        {
            // Create our own effect
            StartCoroutine(CreateMainBodyEffect(position));
        }
    }
    
    System.Collections.IEnumerator CreateMainBodyEffect(Vector3 position)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "MainBodyHit";
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 0.3f;
        
        // Orange material
        Renderer renderer = effect.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.5f, 0f, 0.8f);
        renderer.material = mat;
        
        // Remove collider
        Destroy(effect.GetComponent<Collider>());
        
        // Animate
        float duration = 2f;
        float elapsed = 0f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 targetScale = startScale * 4f;
        
        while (elapsed < duration && effect != null)
        {
            float progress = elapsed / duration;
            effect.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            
            // Fade
            Color color = mat.color;
            color.a = (1f - progress) * 0.8f;
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
