using UnityEngine;

public class FixedRealHitDetection : MonoBehaviour
{
    [Header("🎯 FIXED REAL HIT DETECTION SYSTEM")]
    [Space(10)]
    
    [Header("Hit Detection Settings")]
    public string attackerTag = "Player";
    public bool enableDebugLogs = true;
    public bool showHitPosition = true;
    
    [Header("Hit Circle Settings")]
    public float circleSize = 2f;
    public float circleDuration = 1.5f;
    public Color hitColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange
    
    [Header("Body Part Detection")]
    public bool detectFullBody = true;
    public bool detectSpecificBodyParts = false;
    
    private Collider myCollider;
    
    void Start()
    {
        myCollider = GetComponent<Collider>();
        
        if (myCollider == null)
        {
            // Add a main body collider if none exists
            myCollider = gameObject.AddComponent<CapsuleCollider>();
            CapsuleCollider capsule = (CapsuleCollider)myCollider;
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = Vector3.up * 1f;
            
            if (enableDebugLogs)
            {
                Debug.Log($"➕ Added main body CapsuleCollider to {gameObject.name}");
            }
        }
        
        // Ensure it's a trigger for hit detection
        myCollider.isTrigger = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 FixedRealHitDetection initialized on {gameObject.name}");
            Debug.Log($"   👀 Watching for tag: '{attackerTag}'");
            Debug.Log($"   🔲 Main Collider: {myCollider.GetType().Name} (IsTrigger: {myCollider.isTrigger})");
        }
        
        // Set up full body detection if enabled
        if (detectFullBody)
        {
            SetupFullBodyDetection();
        }
    }
    
    void SetupFullBodyDetection()
    {
        Debug.Log($"🔧 Setting up full body hit detection for {gameObject.name}...");
        
        // Remove any existing body part colliders that might be causing the "left foot only" issue
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child != transform && child.name.ToLower().Contains("foot"))
            {
                Collider childCollider = child.GetComponent<Collider>();
                if (childCollider != null && childCollider.isTrigger)
                {
                    Debug.Log($"   ⚠️ Found foot collider on {child.name} - might be interfering");
                    // Don't destroy it, just note it
                }
            }
        }
        
        // Ensure our main collider covers the full body
        if (myCollider is CapsuleCollider capsule)
        {
            capsule.height = 2.5f; // Taller to cover full body
            capsule.radius = 0.6f; // Wider to cover arms
            capsule.center = Vector3.up * 1.25f; // Centered on body
            
            Debug.Log($"   ✅ Adjusted main collider for full body coverage (height: {capsule.height}, radius: {capsule.radius})");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"🔍 TRIGGER: {other.name} (tag: '{other.tag}') -> {gameObject.name}");
            Debug.Log($"   📍 Other position: {other.transform.position}");
            Debug.Log($"   📍 My position: {transform.position}");
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
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log($"🚫 Wrong tag: Expected '{attackerTag}', got '{other.tag}'");
            }
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
            // Fallback: Use position between the two objects
            hitPosition = (transform.position + attacker.transform.position) / 2f;
            
            if (enableDebugLogs)
            {
                Debug.Log($"   ⚠️ Fallback: Using midpoint between objects");
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
    
    void ShowHitAtPosition(Vector3 hitPosition)
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
            StartCoroutine(CreateFixedHitEffect(hitPosition));
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ Created custom hit effect");
            }
        }
        
        // Show visual debug if enabled
        if (showHitPosition)
        {
            StartCoroutine(ShowFixedDebugMarker(hitPosition));
        }
    }
    
    System.Collections.IEnumerator CreateFixedHitEffect(Vector3 position)
    {
        // Create a sphere for the hit effect
        GameObject hitSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hitSphere.name = $"FixedHit_{Time.time:F2}";
        hitSphere.transform.position = position;
        hitSphere.transform.localScale = Vector3.one * 0.2f; // Start small
        
        // FIXED: Use Destroy instead of DestroyImmediate
        Collider hitCollider = hitSphere.GetComponent<Collider>();
        if (hitCollider != null) 
        {
            Destroy(hitCollider); // CHANGED: Was DestroyImmediate
        }
        
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
            Debug.Log($"🟠 Created fixed hit sphere at {position}");
        }
        
        // Animate: grow and fade
        float elapsed = 0f;
        Vector3 startScale = hitSphere.transform.localScale;
        Vector3 targetScale = Vector3.one * circleSize;
        Color startColor = hitColor;
        
        while (elapsed < circleDuration)
        {
            if (hitSphere == null) break; // Safety check
            
            float progress = elapsed / circleDuration;
            
            // Scale up
            float scaleProgress = Mathf.Lerp(0f, 1f, progress);
            hitSphere.transform.localScale = Vector3.Lerp(startScale, targetScale, scaleProgress);
            
            // Fade out
            float alphaProgress = 1f - progress;
            Color currentColor = startColor;
            currentColor.a = startColor.a * alphaProgress;
            
            if (material != null)
            {
                material.color = currentColor;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (hitSphere != null)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"🗑️ Destroying fixed hit sphere");
            }
            Destroy(hitSphere);
        }
    }
    
    System.Collections.IEnumerator ShowFixedDebugMarker(Vector3 position)
    {
        // Create a small debug marker to show exact hit position
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "DEBUG_FixedHitMarker";
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.1f;
        
        // Make it bright green
        Renderer renderer = marker.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.green;
        renderer.material = material;
        
        // FIXED: Use Destroy instead of DestroyImmediate
        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            Destroy(markerCollider); // CHANGED: Was DestroyImmediate
        }
        
        yield return new WaitForSeconds(5f); // Show for 5 seconds
        
        if (marker != null)
        {
            Destroy(marker);
        }
    }
    
    // Manual test for specific positions
    [ContextMenu("🧪 Test Hit at Center")]
    public void TestHitAtCenter()
    {
        Vector3 testPosition = transform.position + Vector3.forward * 1f + Vector3.up * 1.5f;
        Debug.Log($"🧪 MANUAL TEST: Creating hit at center {testPosition}");
        ShowHitAtPosition(testPosition);
    }
    
    [ContextMenu("🔍 Analyze My Colliders")]
    public void AnalyzeMyColliders()
    {
        Debug.Log($"🔍 ANALYZING COLLIDERS on {gameObject.name}:");
        
        Collider[] colliders = GetComponentsInChildren<Collider>();
        Debug.Log($"   📦 Total colliders: {colliders.Length}");
        
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            Debug.Log($"   {i+1}. {col.name} - {col.GetType().Name}");
            Debug.Log($"      📊 IsTrigger: {col.isTrigger}, Enabled: {col.enabled}");
            Debug.Log($"      📍 Position: {col.transform.position}");
            Debug.Log($"      🎯 On GameObject: {col.gameObject.name}");
            
            if (col.gameObject.name.ToLower().Contains("foot"))
            {
                Debug.Log($"      ⚠️ FOOT COLLIDER DETECTED - this might be causing 'left foot only' hits");
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (showHitPosition && Application.isPlaying)
        {
            // Draw main body detection area
            Gizmos.color = Color.yellow;
            if (myCollider is CapsuleCollider capsule)
            {
                Gizmos.DrawWireCube(transform.position + capsule.center, 
                    new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2));
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
            
            // Draw forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}
