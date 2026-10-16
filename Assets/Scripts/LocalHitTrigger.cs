using UnityEngine;

public class LocalHitTrigger : MonoBehaviour
{
    [Header("Settings")]
    public string attackerTag = "Player";
    
    [Header("Hit Detection")]
    public HitType bodyPartType = HitType.Body; // Set per body part
    public float baseDamage = 25f;
    
    [Header("Audio")]
    public AudioClip hitSound;
    
    [Header("References")]
    private LocalHitIndicator hitIndicator;
    private LocalWorldHitIndicator worldHitIndicator;
    private BodyPartIdentifier bodyPartIdentifier;
    
    private bool canTrigger = true;
    private float cooldownTime = 0.5f;
    
    void Start()
    {
        // Find hit indicator systems
        hitIndicator = FindObjectOfType<LocalHitIndicator>();
        worldHitIndicator = FindObjectOfType<LocalWorldHitIndicator>();
        bodyPartIdentifier = GetComponent<BodyPartIdentifier>();
        
        // If no body part identifier, add one
        if (bodyPartIdentifier == null)
        {
            bodyPartIdentifier = gameObject.AddComponent<BodyPartIdentifier>();
            bodyPartIdentifier.hitType = bodyPartType;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!canTrigger || !other.CompareTag(attackerTag)) return;
        
        ProcessHit(other);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (!canTrigger || !collision.gameObject.CompareTag(attackerTag)) return;
        
        ProcessHit(collision.collider);
    }
    
    private void ProcessHit(Collider attacker)
    {
        // Calculate hit details
        Vector3 hitPosition = attacker.ClosestPoint(transform.position);
        float damage = CalculateDamage();
        HitType hitType = DetermineHitType();
        
        Debug.Log($"Hit registered: {attacker.gameObject.name} hit {gameObject.name} for {damage} damage at {hitType} body part");
        
        // Show hit effects immediately (no networking)
        ShowHitEffect(hitPosition, hitType, damage);
        
        StartCooldown();
    }
    
    private void ShowHitEffect(Vector3 hitPosition, HitType hitType, float damage)
    {
        // Show UI hit indicator
        if (hitIndicator != null)
        {
            hitIndicator.ShowHitIndicator(hitPosition, hitType, damage);
        }
        
        // Show 3D world hit indicator
        if (worldHitIndicator != null)
        {
            worldHitIndicator.ShowHitIndicator(hitPosition, hitType, damage);
        }
        
        // Play hit sound
        if (hitSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(hitSound, hitPosition);
        }
        
        // Camera shake for enhanced feedback
        var hitFeedback = FindObjectOfType<HitFeedback>();
        if (hitFeedback != null)
        {
            // Use localized hit (just camera shake, no screen flash)
            hitFeedback.TriggerLocalizedHit();
        }
    }
    
    private float CalculateDamage()
    {
        float multiplier = bodyPartIdentifier != null ? bodyPartIdentifier.GetDamageMultiplier() : 1.0f;
        return baseDamage * multiplier;
    }
    
    private HitType DetermineHitType()
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
