using UnityEngine;

public class DirectHitTest : MonoBehaviour
{
    [Header("Direct Hit Circle Test")]
    public KeyCode testKey = KeyCode.T;
    public Transform targetPosition; // Drag Adam_Big here
    
    void Update()
    {
        // Press T to force a hit circle to appear
        if (Input.GetKeyDown(testKey))
        {
            Debug.Log("🧪 DIRECT TEST: Creating hit circle manually...");
            CreateTestHitCircle();
        }
    }
    
    void CreateTestHitCircle()
    {
        // Find a target position
        Vector3 hitPosition = Vector3.zero;
        
        if (targetPosition != null)
        {
            hitPosition = targetPosition.position + Vector3.up * 1.5f;
        }
        else
        {
            // Try to find Adam_Big automatically
            GameObject adam = GameObject.Find("Adam_Big");
            if (adam != null)
            {
                hitPosition = adam.transform.position + Vector3.up * 1.5f;
                Debug.Log($"Found Adam_Big at: {adam.transform.position}");
            }
            else
            {
                hitPosition = new Vector3(0, 2, 0); // Default position
                Debug.Log("Using default position for test");
            }
        }
        
        Debug.Log($"🎯 Creating test hit circle at: {hitPosition}");
        
        // Create hit circle directly
        StartCoroutine(CreateVisibleHitCircle(hitPosition));
    }
    
    System.Collections.IEnumerator CreateVisibleHitCircle(Vector3 position)
    {
        Debug.Log("🟠 Creating orange circle...");
        
        // Create a bright orange quad
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Quad);
        circle.name = "TEST_HitCircle";
        circle.transform.position = position;
        circle.transform.localScale = Vector3.one * 2f; // Start at full size for visibility
        
        // Create bright orange material
        Renderer renderer = circle.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Unlit/Color"));
        material.color = new Color(1f, 0.5f, 0f, 0.8f); // Bright orange
        renderer.material = material;
        
        // Remove collider
        Collider collider = circle.GetComponent<Collider>();
        if (collider != null) DestroyImmediate(collider);
        
        // Make it face the camera
        if (Camera.main != null)
        {
            circle.transform.LookAt(Camera.main.transform);
            circle.transform.Rotate(0, 180, 0);
        }
        
        Debug.Log("✅ Orange circle created! Should be visible now.");
        
        // Animate for 2 seconds
        float duration = 2f;
        float elapsed = 0f;
        Vector3 startScale = circle.transform.localScale;
        Color startColor = material.color;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            
            // Scale animation
            float scale = Mathf.Lerp(1f, 3f, progress);
            circle.transform.localScale = startScale * scale;
            
            // Fade animation  
            float alpha = Mathf.Lerp(0.8f, 0f, progress);
            Color currentColor = startColor;
            currentColor.a = alpha;
            material.color = currentColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Debug.Log("🗑️ Destroying test circle");
        Destroy(circle);
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 60), $"Press '{testKey}' to test hit circle\n\nThis will create an orange circle\nabove Adam_Big for testing");
        
        if (Camera.main != null)
        {
            GUI.Label(new Rect(10, 80, 300, 20), $"Camera position: {Camera.main.transform.position}");
        }
        
        GameObject adam = GameObject.Find("Adam_Big");
        if (adam != null)
        {
            GUI.Label(new Rect(10, 100, 300, 20), $"Adam position: {adam.transform.position}");
        }
    }
}
