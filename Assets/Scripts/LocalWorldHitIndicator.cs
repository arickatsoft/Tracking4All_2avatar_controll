using System.Collections;
using UnityEngine;

public class LocalWorldHitIndicator : MonoBehaviour
{
    [Header("Hit Effect Prefabs")]
    public GameObject hitCirclePrefab; // Circular hit effect
    public GameObject criticalHitPrefab; // Special effect for critical hits
    
    [Header("Body Part Hit Effects")]
    public Color headHitColor = Color.red;
    public Color bodyHitColor = Color.orange;
    public Color limbHitColor = Color.yellow;
    
    [Header("Effect Settings")]
    public float hitIndicatorDuration = 1.0f;
    public float maxIndicatorSize = 2.0f;
    public AnimationCurve sizeOverTime = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve alphaOverTime = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    public static LocalWorldHitIndicator Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void ShowHitIndicator(Vector3 hitPosition, HitType hitType, float damage)
    {
        StartCoroutine(DisplayHitEffect(hitPosition, hitType, damage));
    }
    
    private IEnumerator DisplayHitEffect(Vector3 worldPosition, HitType hitType, float damage)
    {
        // Create hit indicator at world position
        GameObject hitEffect = Instantiate(GetHitPrefab(hitType), worldPosition, Quaternion.identity);
        
        // Get components for animation
        Transform effectTransform = hitEffect.transform;
        Renderer effectRenderer = hitEffect.GetComponent<Renderer>();
        
        if (effectRenderer == null)
        {
            Debug.LogWarning("Hit effect prefab missing Renderer component!");
            Destroy(hitEffect);
            yield break;
        }
        
        Material effectMaterial = effectRenderer.material;
        
        // Set initial values
        Color hitColor = GetHitColor(hitType);
        effectMaterial.color = hitColor;
        effectTransform.localScale = Vector3.zero;
        
        // Animate the hit indicator
        float elapsedTime = 0f;
        Vector3 initialScale = Vector3.zero;
        Vector3 targetScale = Vector3.one * (maxIndicatorSize * (damage / 100f)); // Scale with damage
        
        while (elapsedTime < hitIndicatorDuration)
        {
            float progress = elapsedTime / hitIndicatorDuration;
            
            // Animate size
            float sizeMultiplier = sizeOverTime.Evaluate(progress);
            effectTransform.localScale = Vector3.Lerp(initialScale, targetScale, sizeMultiplier);
            
            // Animate transparency
            float alpha = alphaOverTime.Evaluate(progress);
            Color currentColor = hitColor;
            currentColor.a = alpha;
            effectMaterial.color = currentColor;
            
            // Make indicator face camera
            if (Camera.main != null)
            {
                effectTransform.LookAt(Camera.main.transform);
                effectTransform.Rotate(0, 180, 0); // Flip to face camera properly
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Cleanup
        Destroy(hitEffect);
    }
    
    private GameObject GetHitPrefab(HitType hitType)
    {
        return hitType == HitType.Critical ? criticalHitPrefab : hitCirclePrefab;
    }
    
    private Color GetHitColor(HitType hitType)
    {
        switch(hitType)
        {
            case HitType.Head: return headHitColor;
            case HitType.Body: return bodyHitColor;
            case HitType.Limb: return limbHitColor;
            case HitType.Critical: return Color.white;
            default: return bodyHitColor;
        }
    }
}
