using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnhancedHitFeedback : MonoBehaviour
{
    [Header("References")]
    public Transform cameraRig;        // assign your CameraRig here
    public Image hitFlashImage;        // assign the HitFlash UI Image here

    [Header("Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.3f;

    [Header("Flash Settings")]
    public float flashDuration = 0.3f;
    public float maxFlashAlpha = 0.5f;
    public Color defaultFlashColor = Color.red;

    // DEPRECATED: Use TriggerLocalizedHit() instead to avoid screen flash
    // Call this to trigger both effects with default red color
    public void TriggerHit()
    {
        Debug.LogWarning("DEPRECATED: TriggerHit() causes screen flash. Use TriggerLocalizedHit() instead!");
        TriggerHitWithColor(defaultFlashColor);
    }
    
    // NEW: Localized hit with just camera shake (no screen flash)
    public void TriggerLocalizedHit()
    {
        StopAllCoroutines();
        StartCoroutine(ShakeCamera());
        Debug.Log("Triggered localized hit - camera shake only, no screen flash!");
    }
    
    // Call this to trigger both effects with custom color
    public void TriggerHitWithColor(Color flashColor)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeCamera());
        StartCoroutine(FlashScreen(flashColor));
    }
    
    // Call this for screen blink only (no camera shake)
    public void TriggerScreenBlink(Color flashColor)
    {
        StopAllCoroutines();
        StartCoroutine(FlashScreen(flashColor));
    }

    private IEnumerator ShakeCamera()
    {
        if (cameraRig == null) yield break;
        
        Vector3 originalPos = cameraRig.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            cameraRig.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraRig.localPosition = originalPos;
    }

    private IEnumerator FlashScreen(Color flashColor)
    {
        if (hitFlashImage == null) yield break;
        
        // Fade in
        float half = flashDuration * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, maxFlashAlpha, t / half);
            hitFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        // Fade out
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(maxFlashAlpha, 0f, t / half);
            hitFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        hitFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }
}