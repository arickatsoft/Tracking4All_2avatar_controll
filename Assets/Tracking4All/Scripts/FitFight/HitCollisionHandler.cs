using UnityEngine;

public class HitCollisionHandler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag of the attacking collider (e.g., LeftFoot)")]
    public string weaponTag = "LeftFoot";

    [Header("Hit Settings")]
    [Tooltip("Body part type for this collision handler")]
    public HitType bodyPartType = HitType.Body;
    
    [Tooltip("Base damage for this body part")]
    public float baseDamage = 25f;

    [Header("Sound")]
    [Tooltip("Sound to play when Remy gets hit (Adam's attack)")]
    public AudioClip getHitSound;

    private HitFeedback hitFeedback;
    private Animator animator;
    
    // NEW: References to localized hit system
    private LocalHitIndicator uiHitIndicator;
    private LocalWorldHitIndicator worldHitIndicator;
    private BodyPartIdentifier bodyPartIdentifier;

    void Awake()
    {
        hitFeedback = Object.FindFirstObjectByType<HitFeedback>();
        if (hitFeedback == null)
            Debug.LogWarning("No HitFeedback found in scene!");

        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogWarning("No Animator found on this GameObject!");
            
        // NEW: Find localized hit systems
        uiHitIndicator = FindObjectOfType<LocalHitIndicator>();
        worldHitIndicator = FindObjectOfType<LocalWorldHitIndicator>();
        
        // Get or add body part identifier
        bodyPartIdentifier = GetComponent<BodyPartIdentifier>();
        if (bodyPartIdentifier == null)
        {
            bodyPartIdentifier = gameObject.AddComponent<BodyPartIdentifier>();
            bodyPartIdentifier.hitType = bodyPartType;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the tag exists first to avoid errors
        if (!TagExists(weaponTag))
        {
            Debug.LogWarning($"⚠️ Tag '{weaponTag}' doesn't exist! Please add it in Project Settings → Tags and Layers");
            return;
        }
        
        if (other.CompareTag(weaponTag))
        {
            // Calculate hit details
            Vector3 hitPosition = other.ClosestPoint(transform.position);
            float damage = CalculateDamage();
            HitType hitType = GetHitType();
            
            Debug.Log($"Hit detected! Showing LOCALIZED hit indicator instead of screen flash.");
            
            // NEW: Show localized hit indicators instead of screen flash
            ShowLocalizedHit(hitPosition, hitType, damage);

            // Play hit-react animation
            animator?.SetTrigger("HitReact");

            // Play hit sound via SoundManager
            SoundManager.Instance?.PlaySound(getHitSound, hitPosition);
        }
    }
    
    private bool TagExists(string tagName)
    {
        try
        {
            GameObject.FindWithTag(tagName);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }
    
    private void ShowLocalizedHit(Vector3 hitPosition, HitType hitType, float damage)
    {
        Debug.Log($"🎯 Showing hit at position: {hitPosition}");
        
        // Show simple, reliable hit indicator
        if (SimpleHitIndicator.Instance != null)
        {
            SimpleHitIndicator.Instance.ShowHitAtPosition(hitPosition);
            Debug.Log("✅ SimpleHitIndicator called!");
        }
        else
        {
            Debug.LogWarning("❌ SimpleHitIndicator not found! Creating one...");
            GameObject indicatorGO = new GameObject("SimpleHitIndicator");
            indicatorGO.AddComponent<SimpleHitIndicator>();
            SimpleHitIndicator.Instance.ShowHitAtPosition(hitPosition);
        }
        
        // Show UI hit indicator
        if (uiHitIndicator != null)
        {
            uiHitIndicator.ShowHitIndicator(hitPosition, hitType, damage);
        }
        
        // Show 3D world hit indicator
        if (worldHitIndicator != null)
        {
            worldHitIndicator.ShowHitIndicator(hitPosition, hitType, damage);
        }
        
        // Camera shake ONLY (no screen flash)
        if (hitFeedback != null)
        {
            hitFeedback.TriggerLocalizedHit(); // Only camera shake
        }
    }
    
    private float CalculateDamage()
    {
        float multiplier = bodyPartIdentifier != null ? bodyPartIdentifier.GetDamageMultiplier() : 1.0f;
        return baseDamage * multiplier;
    }
    
    private HitType GetHitType()
    {
        return bodyPartIdentifier != null ? bodyPartIdentifier.GetHitType() : bodyPartType;
    }
}
