using UnityEngine;

public class CollisionDiagnostic : MonoBehaviour
{
    [Header("Debug Info")]
    public bool showDebugInfo = false;
    public KeyCode testKey = KeyCode.Space;
    
    private Transform remy;
    private Transform adam;
    private HitFeedback hitFeedback;
    
    void Start()
    {
        Debug.Log("=== COLLISION DIAGNOSTIC STARTED ===");
        
        // Find Remy
        GameObject remyObj = GameObject.FindGameObjectWithTag("Player");
        if (remyObj != null)
        {
            remy = remyObj.transform;
            Debug.Log($"✓ Found Remy: {remy.name} at position {remy.position}");
        }
        else
        {
            Debug.LogError("✗ Could not find Remy (Player tag)");
        }
        
        // Find Adam
        GameObject adamObj = GameObject.FindGameObjectWithTag("Enemy");
        if (adamObj != null)
        {
            adam = adamObj.transform;
            Debug.Log($"✓ Found Adam: {adam.name} at position {adam.position}");
        }
        else
        {
            Debug.LogError("✗ Could not find Adam (Enemy tag)");
        }
        
        // Find HitFeedback
        hitFeedback = FindObjectOfType<HitFeedback>();
        if (hitFeedback != null)
        {
            Debug.Log($"✓ Found HitFeedback component on: {hitFeedback.gameObject.name}");
        }
        else
        {
            Debug.LogError("✗ Could not find HitFeedback component");
        }
        
        Debug.Log("=== DIAGNOSTIC SETUP COMPLETE ===");
    }
    
    void Update()
    {
        // Test key to trigger screen blink manually
        if (Input.GetKeyDown(testKey))
        {
            Debug.Log("Manual screen blink test triggered!");
            TestScreenBlink();
        }
        
        // Monitor positions if both characters found
        if (remy != null && adam != null)
        {
            float distance = Vector3.Distance(remy.position, adam.position);
            
            // Log when very close
            if (distance < 3.0f)
            {
                Debug.Log($"Characters close! Distance: {distance:F2} - Remy: {remy.position} - Adam: {adam.position}");
                
                // Auto-trigger if very close
                if (distance < 1.5f)
                {
                    Debug.Log("AUTO-TRIGGERING SCREEN BLINK!");
                    TestScreenBlink();
                }
            }
        }
    }
    
    void TestScreenBlink()
    {
        if (hitFeedback != null)
        {
            Debug.Log("Triggering HitFeedback.TriggerHit()");
            hitFeedback.TriggerHit();
        }
        else
        {
            Debug.LogError("Cannot trigger screen blink - HitFeedback not found!");
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUI.Box(new Rect(10, 10, 400, 200), "COLLISION DIAGNOSTIC");
        
        int y = 35;
        GUI.Label(new Rect(20, y, 350, 20), $"Remy Found: {(remy != null ? "YES" : "NO")}");
        y += 20;
        GUI.Label(new Rect(20, y, 350, 20), $"Adam Found: {(adam != null ? "YES" : "NO")}");
        y += 20;
        GUI.Label(new Rect(20, y, 350, 20), $"HitFeedback Found: {(hitFeedback != null ? "YES" : "NO")}");
        y += 20;
        
        if (remy != null && adam != null)
        {
            float distance = Vector3.Distance(remy.position, adam.position);
            GUI.Label(new Rect(20, y, 350, 20), $"Distance: {distance:F2}");
            y += 20;
            GUI.Label(new Rect(20, y, 350, 20), $"Remy Pos: {remy.position}");
            y += 20;
            GUI.Label(new Rect(20, y, 350, 20), $"Adam Pos: {adam.position}");
            y += 20;
        }
        
        GUI.Label(new Rect(20, y, 350, 20), $"Press {testKey} to test screen blink");
        y += 20;
        
        if (GUI.Button(new Rect(20, y, 200, 30), "Test Screen Blink"))
        {
            TestScreenBlink();
        }
    }
}