using UnityEngine;

public class RealHitSetup : MonoBehaviour
{
    [Header("🎯 REAL HIT SYSTEM SETUP")]
    [Space(10)]
    
    [Header("Character Setup")]
    public string adamName = "Adam_Big";
    public string remyName = "Remy";
    
    [Header("Hit Detection Setup")]
    public bool setupOnStart = true;
    public bool replaceOldDetection = true;
    public bool enableDetailedLogs = true;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupRealHitSystem();
        }
    }
    
    [ContextMenu("🔧 Setup Real Hit System")]
    public void SetupRealHitSystem()
    {
        Debug.Log("🔧 SETTING UP REAL HIT SYSTEM...");
        Debug.Log("=====================================");
        
        // Step 1: Ensure SimpleHitIndicator exists
        EnsureSimpleHitIndicator();
        
        // Step 2: Setup Adam (can be hit by Remy/Enemy)
        SetupCharacterRealHitDetection(adamName, "Enemy");
        
        // Step 3: Setup Remy (can be hit by Adam/Player)  
        SetupCharacterRealHitDetection(remyName, "Player");
        
        // Step 4: Set character tags properly
        SetupCharacterTags();
        
        Debug.Log("=====================================");
        Debug.Log("✅ REAL HIT SYSTEM SETUP COMPLETE!");
        Debug.Log("🎯 Now when characters collide, you should see orange circles at the exact contact points!");
    }
    
    void EnsureSimpleHitIndicator()
    {
        Debug.Log("🔧 Ensuring SimpleHitIndicator exists...");
        
        if (SimpleHitIndicator.Instance == null)
        {
            GameObject hitIndicatorGO = new GameObject("SimpleHitIndicator");
            hitIndicatorGO.AddComponent<SimpleHitIndicator>();
            Debug.Log("✅ Created SimpleHitIndicator");
        }
        else
        {
            Debug.Log("✅ SimpleHitIndicator already exists");
        }
    }
    
    void SetupCharacterRealHitDetection(string characterName, string attackerTag)
    {
        Debug.Log($"\n🔧 Setting up real hit detection for {characterName}...");
        
        GameObject character = GameObject.Find(characterName);
        if (character == null)
        {
            Debug.LogError($"❌ {characterName} not found in scene!");
            return;
        }
        
        Debug.Log($"✅ Found {characterName} at position: {character.transform.position}");
        
        // Remove old hit detection components if replacing
        if (replaceOldDetection)
        {
            RemoveOldHitComponents(character);
        }
        
        // Add RealHitDetection component
        RealHitDetection realHit = character.GetComponent<RealHitDetection>();
        if (realHit == null)
        {
            realHit = character.AddComponent<RealHitDetection>();
            Debug.Log($"   ➕ Added RealHitDetection component");
        }
        
        // Configure the real hit detection
        realHit.attackerTag = attackerTag;
        realHit.enableDebugLogs = enableDetailedLogs;
        realHit.showHitPosition = true;
        realHit.circleSize = 2f;
        realHit.circleDuration = 1.5f;
        
        Debug.Log($"   🎯 Configured to detect hits from: '{attackerTag}'");
        
        // Ensure proper collider setup
        SetupColliderForRealHit(character);
        
        Debug.Log($"   ✅ {characterName} real hit detection setup complete!");
    }
    
    void RemoveOldHitComponents(GameObject character)
    {
        Debug.Log($"   🗑️ Removing old hit detection components from {character.name}...");
        
        // Remove old SimpleHitDetector
        SimpleHitDetector oldDetector = character.GetComponent<SimpleHitDetector>();
        if (oldDetector != null)
        {
            DestroyImmediate(oldDetector);
            Debug.Log($"      ❌ Removed SimpleHitDetector");
        }
        
        // Remove other old hit components
        LocalHitTrigger oldTrigger = character.GetComponent<LocalHitTrigger>();
        if (oldTrigger != null)
        {
            DestroyImmediate(oldTrigger);
            Debug.Log($"      ❌ Removed LocalHitTrigger");
        }
        
        // Check for any other hit-related components
        Component[] allComponents = character.GetComponents<Component>();
        foreach (Component comp in allComponents)
        {
            if (comp == null) continue;
            
            string compName = comp.GetType().Name;
            if (compName.Contains("Hit") && compName != "RealHitDetection" && !(comp is Collider))
            {
                Debug.Log($"      ⚠️ Found other hit component: {compName} (keeping for now)");
            }
        }
    }
    
    void SetupColliderForRealHit(GameObject character)
    {
        Debug.Log($"   🔲 Setting up collider for {character.name}...");
        
        Collider col = character.GetComponent<Collider>();
        
        if (col == null)
        {
            // Add a CapsuleCollider that fits the character
            CapsuleCollider capsule = character.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = Vector3.up * 1f; // Center it on the character
            col = capsule;
            
            Debug.Log($"      ➕ Added CapsuleCollider (height: {capsule.height}, radius: {capsule.radius})");
        }
        
        // Set as trigger for hit detection
        col.isTrigger = true;
        Debug.Log($"      ✅ Set collider as trigger: {col.GetType().Name}");
        
        // If we want both trigger and collision detection, add a second collider
        Collider[] allColliders = character.GetComponents<Collider>();
        bool hasNonTriggerCollider = false;
        
        foreach (Collider c in allColliders)
        {
            if (!c.isTrigger)
            {
                hasNonTriggerCollider = true;
                break;
            }
        }
        
        if (!hasNonTriggerCollider)
        {
            // Add a second collider for physics/collision detection
            CapsuleCollider physicsCollider = character.AddComponent<CapsuleCollider>();
            physicsCollider.height = 2f;
            physicsCollider.radius = 0.4f; // Slightly smaller
            physicsCollider.center = Vector3.up * 1f;
            physicsCollider.isTrigger = false; // This one is for physics
            
            Debug.Log($"      ➕ Added physics CapsuleCollider for collision detection");
        }
    }
    
    void SetupCharacterTags()
    {
        Debug.Log($"\n🏷️ Setting up character tags...");
        
        GameObject adam = GameObject.Find(adamName);
        GameObject remy = GameObject.Find(remyName);
        
        if (adam != null)
        {
            adam.tag = "Player";
            Debug.Log($"   ✅ Set {adamName} tag to 'Player'");
        }
        
        if (remy != null)
        {
            remy.tag = "Enemy";
            Debug.Log($"   ✅ Set {remyName} tag to 'Enemy'");
        }
        
        Debug.Log($"   💡 Hit Detection Logic:");
        Debug.Log($"      - {adamName} (Player) will be hit by {remyName} (Enemy)");
        Debug.Log($"      - {remyName} (Enemy) will be hit by {adamName} (Player)");
    }
    
    [ContextMenu("🧪 Test Real Hit Detection")]
    public void TestRealHitDetection()
    {
        Debug.Log("🧪 TESTING REAL HIT DETECTION...");
        
        GameObject adam = GameObject.Find(adamName);
        GameObject remy = GameObject.Find(remyName);
        
        if (adam == null || remy == null)
        {
            Debug.LogError("❌ Characters not found for testing!");
            return;
        }
        
        RealHitDetection adamHit = adam.GetComponent<RealHitDetection>();
        RealHitDetection remyHit = remy.GetComponent<RealHitDetection>();
        
        Debug.Log($"🎯 Adam has RealHitDetection: {adamHit != null}");
        Debug.Log($"🎯 Remy has RealHitDetection: {remyHit != null}");
        
        // Test hit at Adam's position
        if (adamHit != null)
        {
            Vector3 testHitPos = adam.transform.position + Vector3.forward * 0.5f + Vector3.up * 1f;
            adamHit.ShowHitAtPosition(testHitPos);
            Debug.Log($"✅ Created test hit on Adam at: {testHitPos}");
        }
        
        // Test hit at Remy's position
        if (remyHit != null)
        {
            Vector3 testHitPos = remy.transform.position + Vector3.forward * 0.5f + Vector3.up * 1f;
            remyHit.ShowHitAtPosition(testHitPos);
            Debug.Log($"✅ Created test hit on Remy at: {testHitPos}");
        }
        
        Debug.Log("🎯 You should see orange circles at the front of both characters!");
    }
    
    [ContextMenu("🔍 Analyze Current Setup")]
    public void AnalyzeCurrentSetup()
    {
        Debug.Log("🔍 ANALYZING CURRENT SETUP...");
        Debug.Log("==============================");
        
        GameObject adam = GameObject.Find(adamName);
        GameObject remy = GameObject.Find(remyName);
        
        AnalyzeCharacter(adam, adamName);
        AnalyzeCharacter(remy, remyName);
        
        Debug.Log("==============================");
    }
    
    void AnalyzeCharacter(GameObject character, string name)
    {
        if (character == null)
        {
            Debug.LogError($"❌ {name} not found!");
            return;
        }
        
        Debug.Log($"\n👤 {name} Analysis:");
        Debug.Log($"   📍 Position: {character.transform.position}");
        Debug.Log($"   🏷️ Tag: '{character.tag}'");
        Debug.Log($"   ✅ Active: {character.activeInHierarchy}");
        
        RealHitDetection realHit = character.GetComponent<RealHitDetection>();
        Debug.Log($"   🎯 RealHitDetection: {(realHit != null ? "✅ Present" : "❌ Missing")}");
        
        if (realHit != null)
        {
            Debug.Log($"      👀 Watching for: '{realHit.attackerTag}'");
            Debug.Log($"      📊 Debug logs: {realHit.enableDebugLogs}");
        }
        
        Collider[] colliders = character.GetComponents<Collider>();
        Debug.Log($"   🔲 Colliders: {colliders.Length}");
        foreach (Collider col in colliders)
        {
            Debug.Log($"      - {col.GetType().Name}: IsTrigger={col.isTrigger}, Enabled={col.enabled}");
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 540, 400, 120), "🎯 REAL HIT SYSTEM SETUP");
        
        if (GUI.Button(new Rect(20, 565, 120, 25), "Setup Real Hits"))
        {
            SetupRealHitSystem();
        }
        
        if (GUI.Button(new Rect(150, 565, 120, 25), "Test Real Hits"))
        {
            TestRealHitDetection();
        }
        
        if (GUI.Button(new Rect(280, 565, 120, 25), "Analyze Setup"))
        {
            AnalyzeCurrentSetup();
        }
        
        GUI.Label(new Rect(20, 595, 380, 20), "🎯 Real hits show circles at exact contact points");
        GUI.Label(new Rect(20, 615, 380, 20), "🔍 Move characters together to test collision detection");
        GUI.Label(new Rect(20, 635, 380, 20), "📊 Check console for detailed hit detection logs");
    }
}
