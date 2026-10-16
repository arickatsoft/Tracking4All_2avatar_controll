using UnityEngine;
using System.Collections;

public class FixedCollisionDetector : MonoBehaviour
{
    [Header("Character References")]
    public Transform remyTransform;
    public Transform adamTransform;
    
    [Header("Detection Settings")]
    [Tooltip("Distance threshold for detecting contact")]
    public float contactDistance = 1.5f;
    
    [Tooltip("Cooldown time between triggers (prevents spam)")]
    public float cooldownTime = 2.0f;
    
    [Header("Visual Feedback")]
    public HitFeedback hitFeedback;
    
    [Header("Blink-Only Mode")]
    [Tooltip("Only do screen blink, no camera shake")]
    public bool blinkOnlyMode = true;
    
    [Header("Debug")]
    public bool debugMode = false;
    
    private bool wasInContact = false;
    private bool canTrigger = true;
    private float lastTriggerTime = 0f;
    
    void Start()
    {
        // Auto-find characters if not assigned
        if (remyTransform == null)
        {
            GameObject remy = GameObject.FindGameObjectWithTag("Player");
            if (remy != null) remyTransform = remy.transform;
        }
        
        if (adamTransform == null)
        {
            GameObject adam = GameObject.FindGameObjectWithTag("Enemy");
            if (adam != null) adamTransform = adam.transform;
        }
        
        if (hitFeedback == null)
        {
            hitFeedback = FindObjectOfType<HitFeedback>();
        }
        
        if (debugMode)
        {
            Debug.Log($"FixedCollisionDetector: Setup complete - Cooldown: {cooldownTime}s");
        }
    }
    
    void Update()
    {
        if (remyTransform == null || adamTransform == null || !canTrigger) return;
        
        float distance = Vector3.Distance(remyTransform.position, adamTransform.position);
        bool currentlyInContact = distance <= contactDistance;
        
        // Only trigger on the moment contact starts (not continuously)
        if (currentlyInContact && !wasInContact && Time.time - lastTriggerTime > cooldownTime)
        {
            TriggerScreenEffect();
            lastTriggerTime = Time.time;
            StartCoroutine(CooldownRoutine());
        }
        
        wasInContact = currentlyInContact;
        
        if (debugMode && currentlyInContact)
        {
            Debug.Log($"Contact detected! Distance: {distance:F2} - Can trigger: {canTrigger}");
        }
    }
    
    void TriggerScreenEffect()
    {
        if (debugMode) Debug.Log("🎯 TRIGGERING SCREEN BLINK EFFECT!");
        
        if (hitFeedback != null)
        {
            if (blinkOnlyMode)
            {
                // Trigger only the screen flash, no camera shake
                StartCoroutine(ScreenBlinkOnly());
            }
            else
            {
                // Use the original hit feedback (includes camera shake)
                hitFeedback.TriggerHit();
            }
        }
        else
        {
            Debug.LogWarning("FixedCollisionDetector: No HitFeedback component found!");
        }
    }
    
    private IEnumerator ScreenBlinkOnly()
    {
        if (hitFeedback == null || hitFeedback.hitFlashImage == null) yield break;
        
        // Get the flash settings from HitFeedback
        float flashDuration = hitFeedback.flashDuration;
        float maxAlpha = hitFeedback.maxFlashAlpha;
        Color flashColor = Color.white; // or any color you prefer
        
        // Fade in
        float half = flashDuration * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, maxAlpha, t / half);
            hitFeedback.hitFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        
        // Fade out
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(maxAlpha, 0f, t / half);
            hitFeedback.hitFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        
        // Ensure it's fully transparent
        hitFeedback.hitFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        
        if (debugMode) Debug.Log("Screen blink effect completed");
    }
    
    private IEnumerator CooldownRoutine()
    {
        canTrigger = false;
        if (debugMode) Debug.Log($"Starting cooldown for {cooldownTime} seconds...");
        
        yield return new WaitForSeconds(cooldownTime);
        
        canTrigger = true;
        if (debugMode) Debug.Log("Cooldown complete - ready for next trigger");
    }
    
    void OnGUI()
    {
        if (!debugMode) return;
        
        GUI.Box(new Rect(420, 10, 300, 150), "FIXED COLLISION DETECTOR");
        
        int y = 35;
        GUI.Label(new Rect(430, y, 280, 20), $"Blink Only Mode: {blinkOnlyMode}");
        y += 20;
        GUI.Label(new Rect(430, y, 280, 20), $"Can Trigger: {canTrigger}");
        y += 20;
        GUI.Label(new Rect(430, y, 280, 20), $"Cooldown: {cooldownTime}s");
        y += 20;
        
        if (remyTransform != null && adamTransform != null)
        {
            float distance = Vector3.Distance(remyTransform.position, adamTransform.position);
            GUI.Label(new Rect(430, y, 280, 20), $"Distance: {distance:F2}");
            y += 20;
            GUI.Label(new Rect(430, y, 280, 20), $"In Contact: {wasInContact}");
        }
        
        if (GUI.Button(new Rect(430, y + 20, 150, 25), "Test Blink"))
        {
            TriggerScreenEffect();
        }
    }
}