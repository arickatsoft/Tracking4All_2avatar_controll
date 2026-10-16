using UnityEngine;

public class HitFeedbackTester : MonoBehaviour
{
    [Header("Test Controls")]
    [Tooltip("Press this key to test the hit feedback")]
    public KeyCode testKey = KeyCode.T;
    
    private HitFeedback hitFeedback;
    
    void Start()
    {
        // Find the HitFeedback component on Adam
        GameObject adam = GameObject.FindWithTag("Enemy");
        if (adam != null)
        {
            hitFeedback = adam.GetComponent<HitFeedback>();
            if (hitFeedback != null)
            {
                Debug.Log("✅ HitFeedback component found and ready for testing!");
                Debug.Log($"🎮 Press '{testKey}' to test the hit feedback effect");
            }
            else
            {
                Debug.LogError("❌ HitFeedback component not found on Adam!");
            }
        }
        else
        {
            Debug.LogError("❌ No GameObject with 'Enemy' tag found!");
        }
    }
    
    void Update()
    {
        // Test hit feedback when key is pressed
        if (Input.GetKeyDown(testKey) && hitFeedback != null)
        {
            Debug.Log("🎯 Testing Hit Feedback - Screen should flash red and camera should shake!");
            hitFeedback.TriggerHit();
        }
    }
    
    void OnGUI()
    {
        // Show test instructions on screen
        GUI.Label(new Rect(10, 10, 300, 40), $"Press '{testKey}' to test hit feedback");
        
        if (hitFeedback == null)
        {
            GUI.Label(new Rect(10, 50, 300, 20), "❌ HitFeedback not found!");
        }
        else
        {
            GUI.Label(new Rect(10, 50, 300, 20), "✅ HitFeedback ready");
        }
    }
}