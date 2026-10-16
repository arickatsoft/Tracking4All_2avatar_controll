// NETWORKING SCRIPT - DISABLED FOR LOCAL PLAY  
// Uncomment and use this when you add Unity Netcode packages back

/*
using UnityEngine;
using Unity.Netcode;

public class EnhancedNetworkHitTrigger : NetworkBehaviour
{
    [Header("Settings")]
    public string attackerTag = "Player";
    
    [Header("Hit Detection")]
    public HitType bodyPartType = HitType.Body; // Set per body part
    public float baseDamage = 25f;
    
    [Header("Audio")]
    public AudioClip hitSound;
    
    [Header("References")]
    private UIHitIndicator hitIndicator;
    private NetworkedHitIndicator worldHitIndicator;
    private BodyPartIdentifier bodyPartIdentifier;
    
    private bool canTrigger = true;
    private float cooldownTime = 0.5f;
    
    void Start()
    {
        // Find hit indicator systems
        hitIndicator = FindObjectOfType<UIHitIndicator>();
        worldHitIndicator = FindObjectOfType<NetworkedHitIndicator>();
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
        
        // Only process on server to prevent duplicate hits
        if (!IsServer) return;
        
        ProcessHit(other);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (!canTrigger || !collision.gameObject.CompareTag(attackerTag)) return;
        
        // Only process on server to prevent duplicate hits
        if (!IsServer) return;
        
        ProcessHit(collision.collider);
    }
    
    private void ProcessHit(Collider attacker)
    {
        // Calculate hit details
        Vector3 hitPosition = attacker.ClosestPoint(transform.position);
        float damage = CalculateDamage();
        HitType hitType = DetermineHitType();
        
        // Process hit on server
        ProcessHitServerRpc(hitPosition, hitType, damage, attacker.gameObject.name);
        
        StartCooldown();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void ProcessHitServerRpc(Vector3 hitPosition, HitType hitType, float damage, string attackerName)
    {
        // Validate hit on server (add your validation logic here)
        if (ValidateHit(hitPosition, damage))
        {
            Debug.Log($"Hit registered: {attackerName} hit {gameObject.name} for {damage} damage at {hitType} body part");
            
            // Notify all clients to show hit effect
            ShowHitEffectClientRpc(hitPosition, hitType, damage);
        }
    }
    
    [ClientRpc]
    private void ShowHitEffectClientRpc(Vector3 hitPosition, HitType hitType, float damage)
    {
        // Show UI hit indicator
        if (hitIndicator != null)
        {
            hitIndicator.ShowUIHitIndicatorClientRpc(hitPosition, hitType, damage);
        }
        
        // Show 3D world hit indicator
        if (worldHitIndicator != null)
        {
            worldHitIndicator.ShowHitIndicatorClientRpc(hitPosition, hitType, damage);
        }
        
        // Play hit sound locally on each client
        if (hitSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(hitSound, hitPosition);
        }
        
        // Optional: Camera shake for the hit player only
        if (IsOwner) // Only shake camera of the player who got hit
        {
            // Find and trigger camera shake if available
            var hitFeedback = FindObjectOfType<HitFeedback>();
            if (hitFeedback != null)
            {
                // Use reduced shake for localized hits
                StartCoroutine(hitFeedback.ShakeCamera());
            }
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
    
    private bool ValidateHit(Vector3 hitPosition, float damage)
    {
        // Add server-side validation logic here
        // Check for reasonable damage values, position validity, etc.
        return damage > 0 && damage <= 200; // Allow up to 200 damage for critical hits
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
*/

// FOR NOW: Use LocalHitTrigger.cs instead!