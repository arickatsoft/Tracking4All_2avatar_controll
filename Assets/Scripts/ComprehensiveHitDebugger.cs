using UnityEngine;
using System.Collections.Generic;

public class ComprehensiveHitDebugger : MonoBehaviour
{
    [Header("🔍 COMPREHENSIVE HIT SYSTEM DEBUGGER")]
    [Space(10)]
    
    [Header("Quick Tests")]
    public KeyCode directTestKey = KeyCode.T;
    public KeyCode fullAnalysisKey = KeyCode.Y;
    public KeyCode fixAllKey = KeyCode.U;
    
    [Header("Debug Settings")]
    public bool debugOnStart = false;
    public bool enableVisualDebug = false;
    
    [Header("Target Characters")]
    public string adamName = "Adam_Big";
    public string remyName = "Remy";
    
    private List<string> debugResults = new List<string>();
    
    void Start()
    {
        if (debugOnStart)
        {
            RunFullAnalysis();
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(directTestKey))
        {
            TestDirectCircleCreation();
        }
        
        if (Input.GetKeyDown(fullAnalysisKey))
        {
            RunFullAnalysis();
        }
        
        if (Input.GetKeyDown(fixAllKey))
        {
            FixAllIssues();
        }
    }
    
    [ContextMenu("🧪 Test Direct Circle Creation")]
    public void TestDirectCircleCreation()
    {
        LogHeader("🧪 DIRECT CIRCLE CREATION TEST");
        
        // Find Adam's position
        GameObject adam = GameObject.Find(adamName);
        Vector3 testPosition = adam != null ? adam.transform.position + Vector3.up * 2f : Vector3.up * 2f;
        
        Debug.Log($"🎯 Creating test circle at: {testPosition}");
        
        // Test different circle creation methods
        TestMethod1_DirectQuad(testPosition);
        TestMethod2_SimpleHitIndicator(testPosition);
        TestMethod3_LocalHitIndicator(testPosition);
    }
    
    void TestMethod1_DirectQuad(Vector3 position)
    {
        Debug.Log("📝 METHOD 1: Direct Quad Creation");
        
        StartCoroutine(CreateDirectQuad(position + Vector3.right * 2f));
    }
    
    System.Collections.IEnumerator CreateDirectQuad(Vector3 position)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "DEBUG_DirectQuad";
        quad.transform.position = position;
        quad.transform.localScale = Vector3.one * 3f;
        
        // Bright red material
        Renderer renderer = quad.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = Color.red;
        renderer.material = mat;
        
        // Remove collider
        DestroyImmediate(quad.GetComponent<Collider>());
        
        Debug.Log("🔴 RED QUAD created - should be visible!");
        
        yield return new WaitForSeconds(3f);
        Destroy(quad);
        Debug.Log("🗑️ Red quad destroyed");
    }
    
    void TestMethod2_SimpleHitIndicator(Vector3 position)
    {
        Debug.Log("📝 METHOD 2: SimpleHitIndicator");
        
        if (SimpleHitIndicator.Instance != null)
        {
            SimpleHitIndicator.Instance.ShowHitAtPosition(position);
            Debug.Log("✅ Called SimpleHitIndicator.ShowHitAtPosition");
        }
        else
        {
            Debug.LogError("❌ SimpleHitIndicator.Instance is NULL!");
        }
    }
    
    void TestMethod3_LocalHitIndicator(Vector3 position)
    {
        Debug.Log("📝 METHOD 3: LocalHitIndicator");
        
        LocalHitIndicator localHit = FindObjectOfType<LocalHitIndicator>();
        if (localHit != null)
        {
            // Try to call whatever method exists
            Debug.Log("✅ Found LocalHitIndicator");
            // We'll check what methods it has in the analysis
        }
        else
        {
            Debug.LogError("❌ LocalHitIndicator not found!");
        }
    }
    
    [ContextMenu("🔍 Run Full Analysis")]
    public void RunFullAnalysis()
    {
        debugResults.Clear();
        
        LogHeader("🔍 COMPREHENSIVE HIT SYSTEM ANALYSIS");
        
        AnalyzeScene();
        AnalyzeCharacters();
        AnalyzeHitSystems();
        AnalyzeTags();
        AnalyzeCurrentIssue();
        
        LogHeader("📋 ANALYSIS SUMMARY");
        foreach (string result in debugResults)
        {
            Debug.Log(result);
        }
    }
    
    void AnalyzeScene()
    {
        LogSection("🎬 SCENE ANALYSIS");
        
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Log($"📷 Main Camera: {mainCam.name} at {mainCam.transform.position}");
        }
        else
        {
            LogError("❌ No main camera found!");
        }
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        Log($"📦 Total GameObjects in scene: {allObjects.Length}");
    }
    
    void AnalyzeCharacters()
    {
        LogSection("👥 CHARACTER ANALYSIS");
        
        AnalyzeCharacter(adamName);
        AnalyzeCharacter(remyName);
    }
    
    void AnalyzeCharacter(string characterName)
    {
        GameObject character = GameObject.Find(characterName);
        
        if (character == null)
        {
            LogError($"❌ {characterName} NOT FOUND in scene!");
            return;
        }
        
        Log($"👤 Found {characterName} at position: {character.transform.position}");
        Log($"   🏷️ Tag: '{character.tag}'");
        Log($"   ✅ Active: {character.activeInHierarchy}");
        
        // Check all components
        Component[] components = character.GetComponents<Component>();
        Log($"   📦 Components ({components.Length}):");
        
        bool hasHitDetection = false;
        bool hasCollider = false;
        
        foreach (Component comp in components)
        {
            if (comp == null) continue;
            
            string compName = comp.GetType().Name;
            Log($"      - {compName}");
            
            // Check for hit-related components
            if (compName.Contains("Hit") || compName.Contains("Trigger"))
            {
                hasHitDetection = true;
                Log($"        🎯 HIT COMPONENT FOUND!");
                
                if (comp is MonoBehaviour mb)
                {
                    Log($"        📊 Enabled: {mb.enabled}");
                }
            }
            
            // Check colliders
            if (comp is Collider col)
            {
                hasCollider = true;
                Log($"        🔲 Collider: Type={col.GetType().Name}, IsTrigger={col.isTrigger}, Enabled={col.enabled}");
            }
        }
        
        if (!hasHitDetection)
        {
            LogWarning($"⚠️ {characterName} has NO hit detection components!");
        }
        
        if (!hasCollider)
        {
            LogWarning($"⚠️ {characterName} has NO colliders!");
        }
    }
    
    void AnalyzeHitSystems()
    {
        LogSection("🎯 HIT SYSTEM ANALYSIS");
        
        // Check SimpleHitIndicator
        if (SimpleHitIndicator.Instance != null)
        {
            Log($"✅ SimpleHitIndicator.Instance EXISTS");
            Log($"   📍 GameObject: {SimpleHitIndicator.Instance.gameObject.name}");
            Log($"   ✅ Enabled: {SimpleHitIndicator.Instance.enabled}");
        }
        else
        {
            LogError("❌ SimpleHitIndicator.Instance is NULL!");
        }
        
        // Check other hit systems
        CheckComponent<LocalHitIndicator>("LocalHitIndicator");
        CheckComponent<LocalWorldHitIndicator>("LocalWorldHitIndicator");
        CheckComponent<LocalHitTrigger>("LocalHitTrigger");
        CheckComponent<HitDetectionDebugger>("HitDetectionDebugger");
        CheckComponent<DirectHitTest>("DirectHitTest");
    }
    
    void CheckComponent<T>(string name) where T : Component
    {
        T[] components = FindObjectsOfType<T>();
        if (components.Length > 0)
        {
            Log($"✅ {name}: Found {components.Length} instance(s)");
            foreach (T comp in components)
            {
                Log($"   📍 On: {comp.gameObject.name}");
                if (comp is MonoBehaviour mb)
                {
                    Log($"   ✅ Enabled: {mb.enabled}");
                }
            }
        }
        else
        {
            LogError($"❌ {name}: NOT FOUND");
        }
    }
    
    void AnalyzeTags()
    {
        LogSection("🏷️ TAG ANALYSIS");
        
        string[] requiredTags = { "Player", "Enemy", "LeftFoot", "RightFoot", "LeftHand", "RightHand", "Head", "Body" };
        
        foreach (string tag in requiredTags)
        {
            try
            {
                GameObject.FindWithTag(tag);
                Log($"✅ Tag '{tag}' exists");
            }
            catch
            {
                LogError($"❌ Tag '{tag}' is NOT DEFINED!");
            }
        }
    }
    
    void AnalyzeCurrentIssue()
    {
        LogSection("🔍 CURRENT ISSUE ANALYSIS");
        
        Log("🎯 Investigating: 'clicking on T causes a growing diagonal rectangle from the enemy knees'");
        
        // Check if there are any existing test scripts
        DirectHitTest[] testScripts = FindObjectsOfType<DirectHitTest>();
        if (testScripts.Length > 0)
        {
            Log($"✅ Found {testScripts.Length} DirectHitTest script(s)");
            foreach (DirectHitTest test in testScripts)
            {
                Log($"   📍 On GameObject: {test.gameObject.name}");
                Log($"   🔑 Test Key: {test.testKey}");
                Log($"   ✅ Enabled: {test.enabled}");
            }
        }
        
        // Check for any other T key handlers
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        Log($"🔍 Checking {allScripts.Length} scripts for T key handlers...");
        
        // This would require reflection to check properly, but we can at least see what's there
    }
    
    [ContextMenu("🔧 Fix All Issues")]
    public void FixAllIssues()
    {
        LogHeader("🔧 FIXING ALL DETECTED ISSUES");
        
        FixMissingTags();
        EnsureHitSystemSetup();
        CreateTestObjects();
        
        Log("✅ All fixes applied! Press Play and test again.");
    }
    
    void FixMissingTags()
    {
        LogSection("🏷️ FIXING MISSING TAGS");
        
        // Note: We can't actually create tags from script in runtime
        // But we can tell the user what to do
        
        Log("⚠️ MANUAL ACTION REQUIRED:");
        Log("1. Go to Edit → Project Settings");
        Log("2. Go to Tags and Layers → Tags");
        Log("3. Add these tags: Player, Enemy, LeftFoot, RightFoot, LeftHand, RightHand, Head, Body");
        
        // We can at least set character tags if they exist
        GameObject adam = GameObject.Find(adamName);
        GameObject remy = GameObject.Find(remyName);
        
        if (adam != null && TagExists("Player"))
        {
            adam.tag = "Player";
            Log($"✅ Set {adamName} tag to 'Player'");
        }
        
        if (remy != null && TagExists("Enemy"))
        {
            remy.tag = "Enemy";
            Log($"✅ Set {remyName} tag to 'Enemy'");
        }
    }
    
    bool TagExists(string tagName)
    {
        try
        {
            GameObject.FindWithTag(tagName);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    void EnsureHitSystemSetup()
    {
        LogSection("🎯 ENSURING HIT SYSTEM SETUP");
        
        // Ensure SimpleHitIndicator exists
        if (SimpleHitIndicator.Instance == null)
        {
            GameObject hitIndicatorGO = new GameObject("SimpleHitIndicator");
            hitIndicatorGO.AddComponent<SimpleHitIndicator>();
            Log("✅ Created SimpleHitIndicator");
        }
        
        // Add hit detection to characters
        AddHitDetectionToCharacter(adamName, "Enemy");  // Adam can be hit by Enemy (Remy)
        AddHitDetectionToCharacter(remyName, "Player"); // Remy can be hit by Player (Adam)
    }
    
    void AddHitDetectionToCharacter(string characterName, string attackerTag)
    {
        GameObject character = GameObject.Find(characterName);
        if (character == null) return;
        
        // Ensure collider
        Collider col = character.GetComponent<Collider>();
        if (col == null)
        {
            col = character.AddComponent<CapsuleCollider>();
            Log($"✅ Added CapsuleCollider to {characterName}");
        }
        col.isTrigger = true;
        
        // Add simple hit detector
        SimpleHitDetector detector = character.GetComponent<SimpleHitDetector>();
        if (detector == null)
        {
            detector = character.AddComponent<SimpleHitDetector>();
            detector.attackerTag = attackerTag;
            Log($"✅ Added SimpleHitDetector to {characterName} (attacker tag: {attackerTag})");
        }
    }
    
    void CreateTestObjects()
    {
        LogSection("🧪 CREATING TEST OBJECTS");
        
        // Create a test manager if it doesn't exist
        if (GameObject.Find("TestManager") == null)
        {
            GameObject testManager = new GameObject("TestManager");
            testManager.AddComponent<DirectHitTest>();
            Log("✅ Created TestManager with DirectHitTest");
        }
    }
    
    // Utility methods for logging
    void LogHeader(string message)
    {
        Debug.Log($"\n{'='*50}\n{message}\n{'='*50}");
    }
    
    void LogSection(string message)
    {
        Debug.Log($"\n--- {message} ---");
    }
    
    void Log(string message)
    {
        Debug.Log(message);
        debugResults.Add($"✅ {message}");
    }
    
    void LogWarning(string message)
    {
        Debug.LogWarning(message);
        debugResults.Add($"⚠️ {message}");
    }
    
    void LogError(string message)
    {
        Debug.LogError(message);
        debugResults.Add($"❌ {message}");
    }
    
    void OnGUI()
    {
        if (!enableVisualDebug) return;
        
        GUI.Box(new Rect(10, 10, 400, 180), "🔍 COMPREHENSIVE HIT DEBUGGER");
        
        GUI.Label(new Rect(20, 35, 380, 20), $"Press '{directTestKey}' - Test Direct Circle Creation");
        GUI.Label(new Rect(20, 55, 380, 20), $"Press '{fullAnalysisKey}' - Run Full Analysis");
        GUI.Label(new Rect(20, 75, 380, 20), $"Press '{fixAllKey}' - Fix All Issues");
        
        GUI.Label(new Rect(20, 105, 380, 20), "🎯 Expected Result:");
        GUI.Label(new Rect(20, 125, 380, 20), "- Press T: RED QUAD should appear for 3 seconds");
        GUI.Label(new Rect(20, 145, 380, 20), "- Orange circle should appear above Adam");
        GUI.Label(new Rect(20, 165, 380, 20), "- Check console for detailed analysis");
    }
}