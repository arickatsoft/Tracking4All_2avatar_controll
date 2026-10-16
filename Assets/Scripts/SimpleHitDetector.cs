//using UnityEngine;

//public class SimpleHitDetector : MonoBehaviour
//{
//    [Header("🎯 SIMPLE HIT DETECTION")]
//    public string attackerTag = "Player";
//    public bool enableDebugLogs = true;
//    public bool visualizeHitPosition = true;
    
//    [Header("Hit Circle Settings")]
//    public float circleSize = 2f;
//    public float circleDuration = 1f;
//    public Color circleColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange
    
//    void Start()
//    {
//        if (enableDebugLogs)
//        {
//            Debug.Log($"🎯 SimpleHitDetector initialized on {gameObject.name}");
//            Debug.Log($"   👀 Watching for attacker tag: '{attackerTag}'");
//            Debug.Log($"   🔲 Collider is trigger: {GetComponent<Collider>()?.isTrigger}");
//        }
//    }
    
//    void OnTriggerEnter(Collider other)
//    {
//        if (enableDebugLogs)
//        {
//            Debug.Log($"🔍 TRIGGER DETECTED: {other.name} (tag: '{other.tag}') entered {gameObject.name}");
//        }
        
//        if (other.CompareTag(attackerTag))
//        {
//            Vector3 hitPosition = CalculateHitPosition(other);
            
//            if (enableDebugLogs)
//            {
//                Debug.Log($"🎯 ✅ HIT CONFIRMED! {other.name} hit {gameObject.name}");
//                Debug.Log($"📍 Hit position: {hitPosition}");
//            }
            
//            ShowHitEffect(hitPosition);
//        }
//        else
//        {
//            if (enableDebugLogs)
//            {
//                Debug.Log($"🚫 Wrong tag: Expected '{attackerTag}', got '{other.tag}'");
//            }
//        }
//    }
    
//    void OnCollisionEnter(Collision collision)
//    {
//        if (enableDebugLogs)
//        {
//            Debug.Log($"💥 COLLISION: {collision.gameObject.name} (tag: '{collision.gameObject.tag}') collided with {gameObject.name}");
//        }
        
//        // Handle collision-based hits too
//        if (collision.gameObject.CompareTag(attackerTag))
//        {
//            Vector3 hitPosition = CalculateCollisionHitPosition(collision);
            
//            if (enableDebugLogs)
//            {
//                Debug.Log($"🎯 ✅ COLLISION HIT! {collision.gameObject.name} hit {gameObject.name}");
//                Debug.Log($"📍 Collision hit position: {hitPosition}");
//            }
            
//            ShowHitEffect(hitPosition);
//        }
//    }
    
//    Vector3 CalculateHitPosition(Collider attacker)
//    {
//        // Try to find the closest point between the attacker and this object
//        Collider myCollider = GetComponent<Collider>();
        
//        if (myCollider != null)
//        {
//            Vector3 closestPoint = myCollider.ClosestPoint(attacker.transform.position);
//            return closestPoint;
//        }
//        else
//        {
//            // Fallback to center position
//            return transform.position;
//        }
//    }
    
//    Vector3 CalculateCollisionHitPosition(Collision collision)
//    {
//        if (collision.contacts.Length > 0)
//        {
//            return collision.contacts[0].point;
//        }
//        else
//        {
//            return collision.transform.position;
//        }
//    }
    
//    void ShowHitEffect(Vector3 hitPosition)
//    {
//        if (enableDebugLogs)
//        {
//            Debug.Log($"🎨 Showing hit effect at: {hitPosition}");
//        }
        
//        // Try multiple methods to show the hit effect
//        bool effectShown = false;
        
//        // Method 1: SimpleHitIndicator
//        if (SimpleHitIndicator.Instance != null)
//        {
//            SimpleHitIndicator.Instance.ShowHitAtPosition(hitPosition);
//            effectShown = true;
            
//            if (enableDebugLogs)
//            {
//                Debug.Log("✅ Used SimpleHitIndicator.ShowHitAtPosition");
//            }
//        }
        
//        // Method 2: LocalHitIndicator
//        LocalHitIndicator localHit = FindObjectOfType<LocalHitIndicator>();
//        if (localHit != null)
//        {
//            // Try to call whatever method exists on LocalHitIndicator
//            // This would need to match the actual method name in LocalHitIndicator
//            effectShown = true;
            
//            if (enableDebugLogs)
//            {
//                Debug.Log("✅ Found LocalHitIndicator (method call depends on implementation)");
//            }
//        }
        
//        // Method 3: Direct visual effect as fallback
//        if (!effectShown || visualizeHitPosition)
//        {
//            StartCoroutine(CreateDirectHitEffect(hitPosition));
            
//            if (enableDebugLogs)
//            {
//                Debug.Log("✅ Creating direct hit effect as fallback/visualization");
//            }
//        }
        
//        if (!effectShown)
//        {
//            Debug.LogWarning("⚠️ No hit indicator system found! Creating direct effect only.");
//        }
//    }
    
//    System.Collections.IEnumerator CreateDirectHitEffect(Vector3 position)
//    {
//        // Create a simple sphere for the hit effect
//        GameObject hitSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//        hitSphere.name = "HitEffect_" + Time.time;
//        hitSphere.transform.position = position;
//        hitSphere.transform.localScale = Vector3.zero;
        
//        // Create material
//        Renderer renderer = hitSphere.GetComponent<Renderer>();
//        Material material = new Material(Shader.Find("Standard"));
//        material.color = circleColor;
//        material.SetFloat("_Mode", 3); // Transparent
//        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
//        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
//        material.SetInt("_ZWrite", 0);
//        material.DisableKeyword("_ALPHATEST_ON");
//        material.EnableKeyword("_ALPHABLEND_ON");
//        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
//        material.renderQueue = 3000;
//        renderer.material = material;
        
//        // Remove collider to avoid interference
//        Collider hitCollider = hitSphere.GetComponent<Collider>();
//        if (hitCollider != null) DestroyImmediate(hitCollider);
        
//        // Animate
//        float elapsed = 0f;
//        Vector3 startScale = Vector3.zero;
//        Vector3 targetScale = Vector3.one * circleSize;
//        Color startColor = circleColor;
        
//        while (elapsed < circleDuration)
//        {
//            float progress = elapsed / circleDuration;
            
//            // Scale animation
//            hitSphere.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            
//            // Fade animation
//            Color currentColor = startColor;
//            currentColor.a = startColor.a * (1f - progress);
//            material.color = currentColor;
            
//            elapsed += Time.deltaTime;
//            yield return null;
//        }
        
//        // Clean up
//        Destroy(hitSphere);
        
//        if (enableDebugLogs)
//        {
//            Debug.Log("🗑️ Hit effect animation completed and cleaned up");
//        }
//    }
    
//    // Manual test function
//    [ContextMenu("🧪 Test Hit Effect Here")]
//    public void TestHitEffectAtThisPosition()
//    {
//        Vector3 testPosition = transform.position + Vector3.up * 1f;
//        Debug.Log($"🧪 MANUAL TEST: Creating hit effect at {testPosition}");
//        ShowHitEffect(testPosition);
//    }
    
//    void OnDrawGizmos()
//    {
//        if (visualizeHitPosition)
//        {
//            // Draw a wire sphere to show the detection area
//            Gizmos.color = Color.yellow;
//            Gizmos.DrawWireSphere(transform.position, 1f);
            
//            // Draw the attacker tag
//            Gizmos.color = Color.red;
//            Gizmos.DrawRay(transform.position, Vector3.up * 2f);
//        }
//    }
//}