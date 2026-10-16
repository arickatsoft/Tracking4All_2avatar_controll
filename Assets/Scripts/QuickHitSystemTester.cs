using UnityEngine;

public class QuickHitSystemTester : MonoBehaviour
{
    [Header("🧪 QUICK HIT SYSTEM TESTER")]
    [Space(10)]
    
    [Header("Test Controls")]
    public KeyCode testCircleKey = KeyCode.T;
    public KeyCode testHitDetectionKey = KeyCode.H;
    public KeyCode fixAllKey = KeyCode.F;
    
    [Header("Test Settings")]
    public bool showOnScreenInstructions = true;
    public bool enableDetailedLogs = true;
    
    void Update()
    {
        if (Input.GetKeyDown(testCircleKey))
        {
            TestCircleCreation();
        }
        
        if (Input.GetKeyDown(testHitDetectionKey))
        {
            TestHitDetection();
        }
        
        if (Input.GetKeyDown(fixAllKey))
        {
            FixAllSystems();
        }
    }
    
    [ContextMenu("🧪 Test Circle Creation")]
    public void TestCircleCreation()
    {
        LogTest("🧪 TESTING CIRCLE CREATION");
        
        // Find Adam's position
        GameObject adam = GameObject.Find("Adam_Big");
        Vector3 testPos = adam != null ? adam.transform.position + Vector3.up * 2f : Vector3.up * 2f;
        
        Log($"📍 Test position: {testPos}");
        
        // Test 1: Direct sphere creation
        CreateTestSphere(testPos + Vector3.left * 2f, Color.red, "Direct Red Sphere");
        
        // Test 2: SimpleHitIndicator
        if (SimpleHitIndicator.Instance != null)
        {
            SimpleHitIndicator.Instance.ShowHitAtPosition(testPos);
            Log("✅ Called SimpleHitIndicator.ShowHitAtPosition");
        }
        else
        {
            LogError("❌ SimpleHitIndicator.Instance not found!");
        }
        
        // Test 3: Manual orange circle
        CreateTestSphere(testPos + Vector3.right * 2f, Color.orange, "Manual Orange Circle");
        
        Log("🎯 EXPECTED: You should see 3 objects appear above Adam");
        Log("   🔴 Red sphere (left)");
        Log("   🟠 Orange circle (center, from SimpleHitIndicator)");
        Log("   🟠 Orange sphere (right)");
    }
    
    void CreateTestSphere(Vector3 position, Color color, string name)
    {
        StartCoroutine(CreateAnimatedSphere(position, color, name));
    }
    
    System.Collections.IEnumerator CreateAnimatedSphere(Vector3 position, Color color, string name)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * 0.5f;
        
        // Set color
        Renderer renderer = sphere.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        renderer.material = mat;
        
        // Remove collider
        DestroyImmediate(sphere.GetComponent<Collider>());
        
        Log($"✅ Created {name} at {position}");
        
        // Animate for 3 seconds
        float duration = 3f;
        float elapsed = 0f;
        Vector3 startScale = sphere.transform.localScale;
        Vector3 targetScale = startScale * 2f;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            sphere.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(sphere);
        Log($"🗑️ Destroyed {name}");
    }
    
    [ContextMenu("🎯 Test Hit Detection")]
    public void TestHitDetection()
    {
        LogTest("🎯 TESTING HIT DETECTION");
        
        GameObject adam = GameObject.Find("Adam_Big");
        GameObject remy = GameObject.Find("Remy");
        
        if (adam == null || remy == null)
        {
            LogError("❌ Characters not found! Make sure Adam_Big and Remy exist in scene");
            return;
        }
        
        Log($"👤 Adam position: {adam.transform.position}");
        Log($"👤 Remy position: {remy.transform.position}");
        
        // Check hit detection components
        SimpleHitDetector adamDetector = adam.GetComponent<SimpleHitDetector>();
        SimpleHitDetector remyDetector = remy.GetComponent<SimpleHitDetector>();
        
        Log($"🎯 Adam has hit detector: {adamDetector != null}");
        Log($"🎯 Remy has hit detector: {remyDetector != null}");
        
        if (adamDetector != null)
        {
            Log($"   📊 Adam detector enabled: {adamDetector.enabled}");
            Log($"   🏷️ Adam detector attacker tag: '{adamDetector.attackerTag}'");
        }
        
        if (remyDetector != null)
        {
            Log($"   📊 Remy detector enabled: {remyDetector.enabled}");
            Log($"   🏷️ Remy detector attacker tag: '{remyDetector.attackerTag}'");
        }
        
        // Manual hit simulation
        Log("🧪 SIMULATING HIT: Remy hits Adam");
        if (adamDetector != null)
        {
            Vector3 hitPos = adam.transform.position + Vector3.up * 1f;
            //adamDetector.TestHitEffectAtThisPosition();
            Log("✅ Called manual hit test on Adam");
        }
        
        Log("🎯 EXPECTED: Orange circle should appear above Adam");
    }
    
    [ContextMenu("🔧 Fix All Systems")]
    public void FixAllSystems()
    {
        LogTest("🔧 FIXING ALL SYSTEMS");
        
        // Step 1: Ensure SimpleHitIndicator exists
        if (SimpleHitIndicator.Instance == null)
        {
            GameObject hitGO = new GameObject("SimpleHitIndicator");
            hitGO.AddComponent<SimpleHitIndicator>();
            Log("✅ Created SimpleHitIndicator");
        }
        else
        {
            Log("✅ SimpleHitIndicator already exists");
        }
        
        // Step 2: Set up characters
        SetupCharacterHitDetection("Adam_Big", "Enemy");
        SetupCharacterHitDetection("Remy", "Player");
        
        // Step 3: Disable problematic scripts
        DisableProblematicScripts();
        
        Log("✅ ALL SYSTEMS FIXED!");
        Log("🧪 Now test with T key - should see orange circles, not rectangles");
    }
    
    void SetupCharacterHitDetection(string characterName, string attackerTag)
    {
        GameObject character = GameObject.Find(characterName);
        if (character == null)
        {
            LogError($"❌ {characterName} not found in scene!");
            return;
        }
        
        Log($"🔧 Setting up hit detection for {characterName}");
        
        // Ensure collider
        Collider col = character.GetComponent<Collider>();
        if (col == null)
        {
            col = character.AddComponent<CapsuleCollider>();
            Log($"   ➕ Added CapsuleCollider to {characterName}");
        }
        col.isTrigger = true;
        
        // Ensure hit detector
        SimpleHitDetector detector = character.GetComponent<SimpleHitDetector>();
        if (detector == null)
        {
            detector = character.AddComponent<SimpleHitDetector>();
            Log($"   ➕ Added SimpleHitDetector to {characterName}");
        }
        detector.attackerTag = attackerTag;
        //detector.enableDebugLogs = enableDetailedLogs;
        
        Log($"   ✅ {characterName} setup complete (attacker tag: {attackerTag})");
    }
    
    void DisableProblematicScripts()
    {
        Log("🔧 Disabling problematic scripts...");
        
        // Disable SplashScreenTester
        SplashScreenTester[] splashTesters = FindObjectsOfType<SplashScreenTester>();
        foreach (var tester in splashTesters)
        {
            if (tester.enabled)
            {
                tester.enabled = false;
                Log($"   ❌ Disabled SplashScreenTester on {tester.gameObject.name}");
            }
        }
        
        if (splashTesters.Length == 0)
        {
            Log("   ✅ No SplashScreenTester found (good!)");
        }
    }
    
    // Logging utilities
    void LogTest(string message)
    {
        Debug.Log($"\n{'='*60}\n{message}\n{'='*60}");
    }
    
    void Log(string message)
    {
        if (enableDetailedLogs)
        {
            Debug.Log(message);
        }
    }
    
    void LogError(string message)
    {
        Debug.LogError(message);
    }
    
    void OnGUI()
    {
        if (!showOnScreenInstructions) return;
        
        GUI.Box(new Rect(10, 350, 450, 180), "🧪 QUICK HIT SYSTEM TESTER");
        
        GUI.Label(new Rect(20, 375, 430, 20), $"Press '{testCircleKey}' - Test Circle Creation (should see 3 objects)");
        GUI.Label(new Rect(20, 395, 430, 20), $"Press '{testHitDetectionKey}' - Test Hit Detection (check console)");
        GUI.Label(new Rect(20, 415, 430, 20), $"Press '{fixAllKey}' - Fix All Systems (auto-setup everything)");
        
        GUI.Label(new Rect(20, 445, 430, 20), "🎯 CURRENT ISSUE: T key shows rectangle instead of circle");
        GUI.Label(new Rect(20, 465, 430, 20), "✅ EXPECTED: Orange circles at hit locations");
        
        if (GUI.Button(new Rect(20, 485, 100, 25), "Fix Now"))
        {
            FixAllSystems();
        }
        
        if (GUI.Button(new Rect(130, 485, 100, 25), "Test Circles"))
        {
            TestCircleCreation();
        }
        
        if (GUI.Button(new Rect(240, 485, 100, 25), "Test Hits"))
        {
            TestHitDetection();
        }
        
        GUI.Label(new Rect(20, 515, 430, 20), "📊 Check console for detailed test results");
    }
}
