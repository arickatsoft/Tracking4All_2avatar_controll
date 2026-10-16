// NETWORKING SCRIPT - DISABLED FOR LOCAL PLAY
// Uncomment and use this when you add Unity Netcode packages back

/*
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UIHitIndicator : NetworkBehaviour
{
    [Header("UI References")]
    public Canvas hitIndicatorCanvas;
    public GameObject hitCircleUIPrefab; // UI Image with circular sprite
    
    [Header("Hit Colors")]
    public Color headHitColor = Color.red;
    public Color bodyHitColor = Color.orange;
    public Color limbHitColor = Color.yellow;
    public Color criticalHitColor = Color.white;
    
    [Header("Animation Settings")]
    public float indicatorDuration = 1.2f;
    public float maxSize = 150f; // UI size in pixels
    public AnimationCurve scaleAnimation = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve fadeAnimation = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [ClientRpc]
    public void ShowUIHitIndicatorClientRpc(Vector3 worldHitPosition, HitType hitType, float damage)
    {
        // Convert world position to screen position
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldHitPosition);
        
        // Only show if position is on screen
        if (screenPosition.z > 0 && 
            screenPosition.x >= 0 && screenPosition.x <= Screen.width &&
            screenPosition.y >= 0 && screenPosition.y <= Screen.height)
        {
            StartCoroutine(ShowUIHitEffect(screenPosition, hitType, damage));
        }
    }
    
    private IEnumerator ShowUIHitEffect(Vector3 screenPosition, HitType hitType, float damage)
    {
        // Create UI hit indicator
        GameObject hitUI = Instantiate(hitCircleUIPrefab, hitIndicatorCanvas.transform);
        RectTransform rectTransform = hitUI.GetComponent<RectTransform>();
        Image hitImage = hitUI.GetComponent<Image>();
        
        // Position at hit location
        rectTransform.position = screenPosition;
        rectTransform.localScale = Vector3.zero;
        
        // Set color based on hit type
        hitImage.color = GetHitColor(hitType);
        
        // Calculate target size based on damage
        float targetSize = maxSize * (0.5f + (damage / 200f)); // Scales with damage
        
        // Animate the hit indicator
        float elapsedTime = 0f;
        Color initialColor = hitImage.color;
        
        while (elapsedTime < indicatorDuration)
        {
            float progress = elapsedTime / indicatorDuration;
            
            // Scale animation
            float scaleValue = scaleAnimation.Evaluate(progress);
            rectTransform.localScale = Vector3.one * scaleValue * (targetSize / maxSize);
            
            // Fade animation
            float alphaValue = fadeAnimation.Evaluate(progress);
            Color currentColor = initialColor;
            currentColor.a = alphaValue;
            hitImage.color = currentColor;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Cleanup
        Destroy(hitUI);
    }
    
    private Color GetHitColor(HitType hitType)
    {
        switch(hitType)
        {
            case HitType.Head: return headHitColor;
            case HitType.Body: return bodyHitColor;
            case HitType.Limb: return limbHitColor;
            case HitType.Critical: return criticalHitColor;
            default: return bodyHitColor;
        }
    }
}
*/

// FOR NOW: Use LocalHitIndicator.cs instead!