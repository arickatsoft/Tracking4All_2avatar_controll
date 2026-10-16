using UnityEngine;

public class RemyHitsAdamTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag of Remy's attack collider (e.g., Player, RemyWeapon)")]
    public string remyAttackTag = "Player";
    
    [Header("Visual Feedback")]
    [Tooltip("Reference to the HitFeedback component for camera shake")]
    public HitFeedback hitFeedback;
    
    [Header("New Hit System")]
    [Tooltip("Body part type for this hit zone")]
    public HitType bodyPartType = HitType.Body;
    
    [Header("Audio")]
    [Tooltip("Sound to play when Remy hits Adam")]
    public AudioClip hitSound;
    
    [Header("Effect Settings")]
    [Tooltip("Base damage for hits on this body part")]
    public float baseDamage = 25f;
    
    private bool canTrigger = true;
    private float cooldownTime = 0.5f; // Prevent multiple rapid triggers
    
    // References to new hit indicator systems
    private LocalHitIndicator uiHitIndicator;
    private LocalWorldHitIndicator worldHitIndicator;
    private BodyPartIdentifier bodyPartIdentifier;
    
    void Awake()
    {
        // Find HitFeedback if not assigned
        if (hitFeedback == null)
        {
            hitFeedback = FindObjectOfType<HitFeedback>();
            if (hitFeedback == null)
            {
                Debug.LogWarning("RemyHitsAdamTrigger: No HitFeedback component found in scene!");
            }
        }
        
        // Find new hit indicator systems
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
        // Check if Remy (or his weapon) hit Adam
        if (canTrigger && other.CompareTag(remyAttackTag))
        {
            Debug.Log("Remy hit Adam! Triggering LOCALIZED hit effect.");
            
            // Calculate hit position
            Vector3 hitPosition = other.ClosestPoint(transform.position);
            float damage = CalculateDamage();
            HitType hitType = GetHitType();
            
            // Trigger the NEW localized hit effect
            TriggerLocalizedHit(hitPosition, hitType, damage);
            
            // Play hit sound
            if (hitSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound(hitSound, hitPosition);
            }
            
            // Start cooldown to prevent multiple rapid triggers
            StartCooldown();
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Also check for actual collision (not just trigger)
        if (canTrigger && collision.gameObject.CompareTag(remyAttackTag))
        {
            Debug.Log("Remy physically contacted Adam! Triggering LOCALIZED hit effect.");
            
            // Calculate hit position
            Vector3 hitPosition = collision.contacts[0].point;
            float damage = CalculateDamage();
            HitType hitType = GetHitType();
            
            // Trigger the NEW localized hit effect
            TriggerLocalizedHit(hitPosition, hitType, damage);
            
            // Play hit sound
            if (hitSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound(hitSound, hitPosition);
            }
            
            // Start cooldown to prevent multiple rapid triggers
            StartCooldown();
        }
    }
    
    private void TriggerLocalizedHit(Vector3 hitPosition, HitType hitType, float damage)
    {
        Debug.Log($"🎯 Triggering hit indicator at position: {hitPosition}");
        
        // Show simple, reliable hit indicator
        if (SimpleHitIndicator.Instance != null)
        {
            SimpleHitIndicator.Instance.ShowHitAtPosition(hitPosition);
            Debug.Log("✅ SimpleHitIndicator called successfully!");
        }
        else
        {
            Debug.LogWarning("❌ SimpleHitIndicator not found! Creating one...");
            // Create the hit indicator if it doesn't exist
            GameObject indicatorGO = new GameObject("SimpleHitIndicator");
            indicatorGO.AddComponent<SimpleHitIndicator>();
            SimpleHitIndicator.Instance.ShowHitAtPosition(hitPosition);
        }
        
        // Show UI hit indicator (circle on screen)
        if (uiHitIndicator != null)
        {
            uiHitIndicator.ShowHitIndicator(hitPosition, hitType, damage);
            Debug.Log("✅ UI hit indicator called!");
        }
        else
        {
            Debug.LogWarning("⚠️ LocalHitIndicator not found - using SimpleHitIndicator only");
        }
        
        // Show 3D world hit indicator
        if (worldHitIndicator != null)
        {
            worldHitIndicator.ShowHitIndicator(hitPosition, hitType, damage);
            Debug.Log("✅ World hit indicator called!");
        }
        
        // Camera shake ONLY (no screen flash)
        if (hitFeedback != null)
        {
            hitFeedback.TriggerLocalizedHit(); // This only does camera shake
            Debug.Log("✅ Camera shake triggered!");
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
    
    private void StartCooldown()
    {
        canTrigger = false;
        Invoke(nameof(ResetCooldown), cooldownTime);
    }
    
    private void ResetCooldown()
    {
        canTrigger = true;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw a wire sphere to show the trigger area
        Gizmos.color = Color.red;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }
}