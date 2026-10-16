using UnityEngine;

public class BodyPartIdentifier : MonoBehaviour
{
    [Header("Body Part Configuration")]
    public HitType hitType = HitType.Body;
    
    [Header("Damage Settings")]
    public float damageMultiplier = 1.0f;
    
    [Header("Visual Settings")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.green;
    
    void Start()
    {
        // Set damage multiplier based on body part type
        switch(hitType)
        {
            case HitType.Head:
                damageMultiplier = 2.0f; // Head hits do double damage
                break;
            case HitType.Body:
                damageMultiplier = 1.0f; // Normal damage
                break;
            case HitType.Limb:
                damageMultiplier = 0.7f; // Limb hits do reduced damage
                break;
            case HitType.Critical:
                damageMultiplier = 3.0f; // Critical hits do triple damage
                break;
        }
    }
    
    public float GetDamageMultiplier()
    {
        return damageMultiplier;
    }
    
    public HitType GetHitType()
    {
        return hitType;
    }
    
    void OnDrawGizmosSelected()
    {
        if (showGizmo)
        {
            Gizmos.color = gizmoColor;
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                }
                else if (col is CapsuleCollider capsule)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    // Approximate capsule with sphere for gizmo
                    Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                }
            }
        }
    }
}