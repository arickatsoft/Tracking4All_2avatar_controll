using UnityEngine;

public class MinimalHitFixer : MonoBehaviour
{
    //[Header("🚀 MINIMAL HIT SYSTEM FIXER")]
    [Tooltip("This script will quickly fix your hit system issues")]
    
    void Start()
    {
        FixHitSystemNow();
    }
    
    [ContextMenu("🔧 Fix Hit System Now")]
    public void FixHitSystemNow()
    {
        Debug.Log("🔧 FIXING HIT SYSTEM...");
        
        // Step 1: Disable SplashScreenTester (causing rectangle issue)
        DisableSplashScreenTester();
        
        // Step 2: Ensure SimpleHitIndicator exists
        EnsureSimpleHitIndicator();
        
        // Step 3: Add hit detection to characters
        SetupCharacterHitDetection();
        
        Debug.Log("✅ HIT SYSTEM FIXED!");
        Debug.Log("🧪 Press T key to test - should see orange circle, not rectangle");
    }
    
    void DisableSplashScreenTester()
    {
        // Find and disable SplashScreenTester components
        SplashScreenTester[] splashTesters = FindObjectsOfType<SplashScreenTester>();
        
        if (splashTesters.Length > 0)
        {
            foreach (var tester in splashTesters)
            {
                tester.enabled = false;
                Debug.Log($"❌ DISABLED SplashScreenTester on {tester.gameObject.name}");
            }
            Debug.Log("✅ SplashScreenTester disabled - this was causing the rectangle!");
        }
        else
        {
            Debug.Log("ℹ️ No SplashScreenTester found");
        }
    }
    
    void EnsureSimpleHitIndicator()
    {
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
    
    void SetupCharacterHitDetection()
    {
        // Setup Adam_Big
        SetupCharacter("Adam_Big", "Enemy");
        
        // Setup Remy
        SetupCharacter("Remy", "Player");
    }
    
    void SetupCharacter(string characterName, string attackerTag)
    {
        GameObject character = GameObject.Find(characterName);
        
        if (character == null)
        {
            Debug.LogWarning($"⚠️ {characterName} not found in scene");
            return;
        }
        
        Debug.Log($"🔧 Setting up {characterName}...");
        
        // Ensure collider exists and is trigger
        Collider col = character.GetComponent<Collider>();
        if (col == null)
        {
            col = character.AddComponent<CapsuleCollider>();
            Debug.Log($"   ➕ Added CapsuleCollider to {characterName}");
        }
        col.isTrigger = true;
        
        // Add SimpleHitDetector if not present
        SimpleHitDetector detector = character.GetComponent<SimpleHitDetector>();
        if (detector == null)
        {
            detector = character.AddComponent<SimpleHitDetector>();
            Debug.Log($"   ➕ Added SimpleHitDetector to {characterName}");
        }
        
        detector.attackerTag = attackerTag;
        //detector.enableDebugLogs = true;
        
        Debug.Log($"   ✅ {characterName} setup complete (watches for: {attackerTag})");
    }
    
    [ContextMenu("🧪 Test Orange Circle")]
    public void TestOrangeCircle()
    {
        Debug.Log("🧪 TESTING ORANGE CIRCLE...");
        
        GameObject adam = GameObject.Find("Adam_Big");
        Vector3 testPos = adam != null ? adam.transform.position + Vector3.up * 2f : Vector3.up * 2f;
        
        if (SimpleHitIndicator.Instance != null)
        {
            SimpleHitIndicator.Instance.ShowHitAtPosition(testPos);
            Debug.Log("✅ Called SimpleHitIndicator.ShowHitAtPosition");
            Debug.Log("🎯 You should see an orange circle above Adam");
        }
        else
        {
            Debug.LogError("❌ SimpleHitIndicator.Instance is null!");
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 350, 120), "🚀 MINIMAL HIT FIXER");
        
        GUI.Label(new Rect(20, 35, 330, 20), "🔧 Quick fix for hit system issues");
        
        if (GUI.Button(new Rect(20, 60, 120, 25), "Fix Hit System"))
        {
            FixHitSystemNow();
        }
        
        if (GUI.Button(new Rect(150, 60, 120, 25), "Test Orange Circle"))
        {
            TestOrangeCircle();
        }
        
        GUI.Label(new Rect(20, 95, 330, 20), "Press T after fixing - should see orange circle");
        GUI.Label(new Rect(20, 110, 330, 20), "Check console for detailed results");
    }
}
