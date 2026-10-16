using UnityEngine;

public class RealHitDetection : MonoBehaviour
{
    [Header("🎯 REAL HIT DETECTION SYSTEM")]
    [Space(10)]
    
    [Header("Hit Detection Settings")]
    public string attackerTag = "Player";
    public bool enableDebugLogs = true;
    public bool showHitPosition = true;
    
    [Header("Hit Circle Settings")]
    public float circleSize = 2f;
    public float circleDuration = 1.5f;
    public Color hitColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange
    
    [Header("Advanced Hit Detection")]
    public LayerMask hitLayers = -1;
    public float hitDetectionRadius = 0.5f;
    
    private Collider myCollider;
    
    void Start()
    {
        myCollider = GetComponent<Collider>();
        
        if (myCollider == null)
        {
            // Add a collider if none exists
            myCollider = gameObject.AddComponent<CapsuleCollider>();
            if (enableDebugLogs)
            {
                Debug.Log($"➕ Added CapsuleCollider to {gameObject.name}");
            }
        }
        
        // Ensure it's a trigger for hit detection
        myCollider.isTrigger = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 RealHitDetection initialized on {gameObject.name}");
            Debug.Log($"   👀 Watching for tag: '{attackerTag}'");
            Debug.Log($"   🔲 Collider: {myCollider.GetType().Name} (IsTrigger: {myCollider.isTrigger})");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"🔍 TRIGGER: {other.name} (tag: '{other.tag}') -> {gameObject.name}");
        }
        
        if (other.CompareTag(attackerTag))
        {
            Vector3 hitPosition = CalculateRealHitPosition(other);
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎯 ✅ REAL HIT DETECTED!");
                Debug.Log($"   👊 Attacker: {other.name}");
                Debug.Log($"   🎯 Target: {gameObject.name}");
                Debug.Log($"   📍 Hit Position: {hitPosition}");
            }
            
            ShowHitAtPosition(hitPosition);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"💥 COLLISION: {collision.gameObject.name} -> {gameObject.name}");
        }
        
        if (collision.gameObject.CompareTag(attackerTag))
        {
            // Use the actual contact point from the collision
            Vector3 hitPosition = GetCollisionHitPosition(collision);
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎯 ✅ COLLISION HIT DETECTED!");
                Debug.Log($"   💥 Contact point: {hitPosition}");
                Debug.Log($"   📊 Contact count: {collision.contacts.Length}");
            }
            
            ShowHitAtPosition(hitPosition);
        }
    }
    
    Vector3 CalculateRealHitPosition(Collider attacker)
    {
        Vector3 hitPosition;
        
        // Method 1: Use closest point between colliders (most accurate)
        if (myCollider != null)
        {
            Vector3 closestPointOnMe = myCollider.ClosestPoint(attacker.transform.position);
            Vector3 closestPointOnAttacker = attacker.ClosestPoint(transform.position);
            
            // Average the two closest points for the hit position
            hitPosition = (closestPointOnMe + closestPointOnAttacker) / 2f;
            
            if (enableDebugLogs)
            {
                Debug.Log($"   📍 Closest on {gameObject.name}: {closestPointOnMe}");
                Debug.Log($"   📍 Closest on {attacker.name}: {closestPointOnAttacker}");
                Debug.Log($"   🎯 Average hit position: {hitPosition}");
            }
        }
        else
        {
            // Fallback: Use attacker's position
            hitPosition = attacker.transform.position;
            
            if (enableDebugLogs)
            {
                Debug.Log($"   ⚠️ Fallback: Using attacker position");
            }
        }
        
        return hitPosition;
    }
    
    Vector3 GetCollisionHitPosition(Collision collision)
    {
        if (collision.contacts.Length > 0)
        {
            // Use the first contact point (most accurate)
            Vector3 contactPoint = collision.contacts[0].point;
            
            if (enableDebugLogs)
            {
                Debug.Log($"   📍 Using contact point: {contactPoint}");
                Debug.Log($"   📐 Contact normal: {collision.contacts[0].normal}");
            }
            
            return contactPoint;
        }
        else
        {
            // Fallback to object centers
            Vector3 midpoint = (transform.position + collision.transform.position) / 2f;
            
            if (enableDebugLogs)
            {
                Debug.Log($"   ⚠️ No contact points, using midpoint: {midpoint}");
            }
            
            return midpoint;
        }
    }
    
    public void ShowHitAtPosition(Vector3 hitPosition)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"🎨 Creating hit circle at: {hitPosition}");
        }
        
        // Try SimpleHitIndicator first
        if (SimpleHitIndicator.Instance != null)
        {
            SimpleHitIndicator.Instance.ShowHitAtPosition(hitPosition);
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ Used SimpleHitIndicator.ShowHitAtPosition");
            }
        }
        else
        {
            // Create our own hit effect
            StartCoroutine(CreateRealHitEffect(hitPosition));
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ Created custom hit effect");
            }
        }
        
        // Show visual debug if enabled
        if (showHitPosition)
        {
            StartCoroutine(ShowDebugMarker(hitPosition));
        }
    }
    
    System.Collections.IEnumerator CreateRealHitEffect(Vector3 position)
    {
        // Create a sphere for the hit effect
        GameObject hitSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hitSphere.name = $"RealHit_{Time.time:F2}";
        hitSphere.transform.position = position;
        hitSphere.transform.localScale = Vector3.one * 0.2f; // Start small
        
        // Remove collider to avoid interference
        DestroyImmediate(hitSphere.GetComponent<Collider>());
        
        // Create material
        Renderer renderer = hitSphere.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = hitColor;
        
        // Make it transparent
        material.SetFloat("_Mode", 3);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        
        renderer.material = material;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🟠 Created hit sphere at {position}");
        }
        
        // Animate: grow and fade
        float elapsed = 0f;
        Vector3 startScale = hitSphere.transform.localScale;
        Vector3 targetScale = Vector3.one * circleSize;
        Color startColor = hitColor;
        
        while (elapsed < circleDuration)
        {
            float progress = elapsed / circleDuration;
            
            // Scale up
            float scaleProgress = Mathf.Lerp(0f, 1f, progress);
            hitSphere.transform.localScale = Vector3.Lerp(startScale, targetScale, scaleProgress);
            
            // Fade out
            float alphaProgress = 1f - progress;
            Color currentColor = startColor;
            currentColor.a = startColor.a * alphaProgress;
            material.color = currentColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🗑️ Destroying hit sphere");
        }
        
        Destroy(hitSphere);
    }
    
    System.Collections.IEnumerator ShowDebugMarker(Vector3 position)
    {
        // Create a small debug marker to show exact hit position
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "DEBUG_HitMarker";
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.1f;
        
        // Make it bright green
        Renderer renderer = marker.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.green;
        renderer.material = material;
        
        // Remove collider
        DestroyImmediate(marker.GetComponent<Collider>());
        
        yield return new WaitForSeconds(5f); // Show for 5 seconds
        
        Destroy(marker);
    }
    
    // Manual test for specific positions
    [ContextMenu("🧪 Test Hit at My Position")]
    public void TestHitAtMyPosition()
    {
        Vector3 testPosition = transform.position + Vector3.up * 1f;
        Debug.Log($"🧪 MANUAL TEST: Creating hit at {testPosition}");
        ShowHitAtPosition(testPosition);
    }
    
    void OnDrawGizmos()
    {
        if (showHitPosition && Application.isPlaying)
        {
            // Draw hit detection area
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, hitDetectionRadius);
            
            // Draw forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}
