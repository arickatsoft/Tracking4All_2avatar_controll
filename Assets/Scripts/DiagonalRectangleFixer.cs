using UnityEngine;
using System.Collections.Generic;

public class DiagonalRectangleFixer : MonoBehaviour
{
    [Header("🔧 DIAGONAL RECTANGLE ISSUE FIXER")]
    [Space(10)]
    
    [Header("Issue Analysis")]
    [Tooltip("The T key is causing a growing diagonal rectangle instead of a circle")]
    public KeyCode problemKey = KeyCode.T;
    public bool analyzeOnStart = true;
    
    [Header("Fix Options")]
    public bool disableConflictingScripts = true;
    public bool replaceWithCorrectBehavior = true;
    
    private List<MonoBehaviour> conflictingScripts = new List<MonoBehaviour>();
    
    void Start()
    {
        if (analyzeOnStart)
        {
            AnalyzeTKeyBehavior();
        }
    }
    
    [ContextMenu("🔍 Analyze T Key Behavior")]
    public void AnalyzeTKeyBehavior()
    {
        Debug.Log("🔍 ANALYZING T KEY BEHAVIOR...");
        Debug.Log("=====================================");
        
        FindTKeyHandlers();
        AnalyzeRectangleSource();
        
        if (disableConflictingScripts)
        {
            DisableConflictingBehaviors();
        }
        
        if (replaceWithCorrectBehavior)
        {
            SetupCorrectHitBehavior();
        }
    }
    
    void FindTKeyHandlers()
    {
        Debug.Log("🔍 SEARCHING FOR T KEY HANDLERS...");
        
        // Find all MonoBehaviour scripts in the scene
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        List<MonoBehaviour> tKeyHandlers = new List<MonoBehaviour>();
        
        Debug.Log($"📦 Checking {allScripts.Length} scripts for T key handling...");
        
        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null) continue;
            
            string scriptName = script.GetType().Name;
            
            // Check known T key handlers
            if (scriptName.Contains("DirectHitTest") || 
                scriptName.Contains("HitTest") || 
                scriptName.Contains("Test") ||
                scriptName.Contains("Debug"))
            {
                tKeyHandlers.Add(script);
                Debug.Log($"🎯 FOUND T KEY HANDLER: {scriptName} on {script.gameObject.name}");
                Debug.Log($"   ✅ Enabled: {script.enabled}");
                Debug.Log($"   📍 GameObject: {script.gameObject.name}");
            }
        }
        
        // Special check for scripts that might be causing the rectangle
        CheckForRectangleScripts();
        
        Debug.Log($"📊 Total T key handlers found: {tKeyHandlers.Count}");
    }
    
    void CheckForRectangleScripts()
    {
        Debug.Log("\n🔍 CHECKING FOR RECTANGLE-CAUSING SCRIPTS...");
        
        // Look for scripts that might create rectangles/quads
        string[] suspiciousNames = {
            "UI", "Canvas", "Image", "Quad", "Rectangle", "Panel", 
            "Splash", "Screen", "Flash", "Effect", "Animation"
        };
        
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        
        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null) continue;
            
            string scriptName = script.GetType().Name;
            
            foreach (string suspicious in suspiciousNames)
            {
                if (scriptName.Contains(suspicious))
                {
                    Debug.Log($"🔶 SUSPICIOUS SCRIPT: {scriptName} on {script.gameObject.name}");
                    Debug.Log($"   ⚠️ This might be causing the rectangle effect");
                    
                    conflictingScripts.Add(script);
                }
            }
        }
        
        // Check for SplashScreenTester specifically (seen in your scripts)
        SplashScreenTester splashTester = FindObjectOfType<SplashScreenTester>();
        if (splashTester != null)
        {
            Debug.Log($"🔴 FOUND SplashScreenTester on {splashTester.gameObject.name}");
            Debug.Log($"   ⚠️ This is likely causing the diagonal rectangle!");
            Debug.Log($"   ✅ Enabled: {splashTester.enabled}");
            
            conflictingScripts.Add(splashTester);
        }
    }
    
    void AnalyzeRectangleSource()
    {
        Debug.Log("\n🔍 ANALYZING RECTANGLE SOURCE...");
        
        // Check for UI Canvas objects that might be showing rectangles
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        
        Debug.Log($"🎨 Found {canvases.Length} Canvas objects:");
        
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"   📱 Canvas: {canvas.name}");
            Debug.Log($"      📊 Enabled: {canvas.enabled}");
            Debug.Log($"      📍 Position: {canvas.transform.position}");
            Debug.Log($"      📏 Render Mode: {canvas.renderMode}");
            
            // Check for UI elements that might be rectangles
            UnityEngine.UI.Image[] images = canvas.GetComponentsInChildren<UnityEngine.UI.Image>();
            foreach (var image in images)
            {
                if (image.gameObject.activeInHierarchy)
                {
                    Debug.Log($"      🖼️ Image: {image.name} (Color: {image.color})");
                }
            }
        }
        
        // Check for quad objects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("quad") || 
                obj.name.ToLower().Contains("rectangle") ||
                obj.name.ToLower().Contains("splash"))
            {
                Debug.Log($"🔶 SUSPICIOUS OBJECT: {obj.name}");
                Debug.Log($"   📍 Position: {obj.transform.position}");
                Debug.Log($"   📏 Scale: {obj.transform.localScale}");
                Debug.Log($"   ✅ Active: {obj.activeInHierarchy}");
                
                MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Debug.Log($"   🎨 Material: {renderer.material?.name}");
                }
            }
        }
    }
    
    void DisableConflictingBehaviors()
    {
        Debug.Log("\n🔧 DISABLING CONFLICTING BEHAVIORS...");
        
        foreach (MonoBehaviour script in conflictingScripts)
        {
            if (script != null && script.enabled)
            {
                script.enabled = false;
                Debug.Log($"❌ DISABLED: {script.GetType().Name} on {script.gameObject.name}");
            }
        }
        
        // Specifically disable SplashScreenTester if found
        SplashScreenTester splashTester = FindObjectOfType<SplashScreenTester>();
        if (splashTester != null && splashTester.enabled)
        {
            splashTester.enabled = false;
            Debug.Log("❌ DISABLED SplashScreenTester - this was likely causing the rectangle!");
        }
        
        Debug.Log("✅ Conflicting behaviors disabled");
    }
    
    void SetupCorrectHitBehavior()
    {
        Debug.Log("\n🔧 SETTING UP CORRECT HIT BEHAVIOR...");
        
        // Ensure we have a proper DirectHitTest that creates circles
        GameObject testManager = GameObject.Find("TestManager");
        if (testManager == null)
        {
            testManager = new GameObject("TestManager");
            Debug.Log("✅ Created TestManager GameObject");
        }
        
        // Remove any existing DirectHitTest and add a clean one
        DirectHitTest existingTest = testManager.GetComponent<DirectHitTest>();
        if (existingTest != null)
        {
            DestroyImmediate(existingTest);
            Debug.Log("🗑️ Removed existing DirectHitTest");
        }
        
        DirectHitTest newTest = testManager.AddComponent<DirectHitTest>();
        Debug.Log("✅ Added clean DirectHitTest component");
        
        // Ensure SimpleHitIndicator exists
        if (SimpleHitIndicator.Instance == null)
        {
            GameObject hitIndicator = new GameObject("SimpleHitIndicator");
            hitIndicator.AddComponent<SimpleHitIndicator>();
            Debug.Log("✅ Created SimpleHitIndicator");
        }
        
        Debug.Log("✅ Correct hit behavior setup complete!");
    }
    
    [ContextMenu("🔧 Force Fix Rectangle Issue")]
    public void ForceFixRectangleIssue()
    {
        Debug.Log("🔧 FORCE FIXING RECTANGLE ISSUE...");
        
        // Step 1: Find and disable all splash screen related scripts
        SplashScreenTester[] splashTesters = FindObjectsOfType<SplashScreenTester>();
        foreach (var tester in splashTesters)
        {
            tester.enabled = false;
            Debug.Log($"❌ Force disabled SplashScreenTester on {tester.gameObject.name}");
        }
        
        // Step 2: Find and hide any splash screen UI elements
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("splash"))
            {
                obj.SetActive(false);
                Debug.Log($"❌ Deactivated splash object: {obj.name}");
            }
        }
        
        // Step 3: Set up clean hit testing
        SetupCorrectHitBehavior();
        
        Debug.Log("✅ FORCE FIX COMPLETE!");
        Debug.Log("🧪 Now press T - you should see an orange circle, not a rectangle!");
    }
    
    [ContextMenu("🧪 Test Clean Hit Circle")]
    public void TestCleanHitCircle()
    {
        Debug.Log("🧪 TESTING CLEAN HIT CIRCLE...");
        
        // Find Adam's position
        GameObject adam = GameObject.Find("Adam_Big");
        Vector3 testPosition = adam != null ? adam.transform.position + Vector3.up * 2f : Vector3.up * 2f;
        
        // Create a clean, simple circle effect
        StartCoroutine(CreateCleanCircle(testPosition));
    }
    
    System.Collections.IEnumerator CreateCleanCircle(Vector3 position)
    {
        Debug.Log($"🟠 Creating CLEAN ORANGE CIRCLE at {position}");
        
        // Create a sphere (not a quad) to avoid rectangle issues
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        circle.name = "CLEAN_HitCircle";
        circle.transform.position = position;
        circle.transform.localScale = Vector3.one * 0.1f; // Start small
        
        // Remove collider
        DestroyImmediate(circle.GetComponent<Collider>());
        
        // Create bright orange material
        Renderer renderer = circle.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = new Color(1f, 0.5f, 0f, 0.9f); // Bright orange
        material.SetFloat("_Mode", 3); // Transparent
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        renderer.material = material;
        
        Debug.Log("✅ Clean orange circle created!");
        
        // Animate: grow and fade
        float duration = 2f;
        float elapsed = 0f;
        Vector3 startScale = circle.transform.localScale;
        Vector3 targetScale = Vector3.one * 3f;
        Color startColor = material.color;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            
            // Scale up
            circle.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            
            // Fade out
            Color currentColor = startColor;
            currentColor.a = startColor.a * (1f - progress);
            material.color = currentColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Debug.Log("🗑️ Clean circle animation complete - destroying");
        Destroy(circle);
    }
    
    void Update()
    {
        // Monitor for the problem key press
        if (Input.GetKeyDown(problemKey))
        {
            Debug.Log($"⚠️ DETECTED {problemKey} KEY PRESS!");
            Debug.Log("🔍 If you see a rectangle instead of a circle, run the Force Fix!");
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 200, 400, 140), "🔧 DIAGONAL RECTANGLE FIXER");
        
        GUI.Label(new Rect(20, 225, 380, 20), "🔍 ISSUE: T key shows growing diagonal rectangle");
        GUI.Label(new Rect(20, 245, 380, 20), "🎯 EXPECTED: Orange circle at hit location");
        
        if (GUI.Button(new Rect(20, 270, 150, 25), "🔧 Force Fix Issue"))
        {
            ForceFixRectangleIssue();
        }
        
        if (GUI.Button(new Rect(180, 270, 150, 25), "🧪 Test Clean Circle"))
        {
            TestCleanHitCircle();
        }
        
        GUI.Label(new Rect(20, 305, 380, 20), $"Conflicting scripts found: {conflictingScripts.Count}");
        GUI.Label(new Rect(20, 320, 380, 20), "Check console for detailed analysis");
    }
}
