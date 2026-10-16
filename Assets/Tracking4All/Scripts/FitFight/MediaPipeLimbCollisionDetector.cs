using UnityEngine;
using System.Collections.Generic;

public class MediaPipeLimbCollisionDetector : MonoBehaviour
{
    [Header("Target")]
    public Transform adam;
    
    [Header("Detection Settings")]
    [Tooltip("Distance threshold for limb-to-Adam contact")]
    public float contactDistance = 0.8f;
    
    [Tooltip("Cooldown between triggers")]
    public float cooldownTime = 1.0f;
    
    [Header("Limb References")]
    [Tooltip("Drag Remy's hand/fist transforms here")]
    public Transform[] limbTransforms;
    
    [Header("Auto-Find Limbs")]
    [Tooltip("Automatically find hand/foot bones")]
    public bool autoFindLimbs = true;
    
    [Header("Visual Feedback")]
    public HitFeedback hitFeedback;
    
    [Header("Debug")]
    public bool debugMode = false;
    public bool showGizmos = false;
    
    private bool canTrigger = true;
    private List<Transform> detectedLimbs = new List<Transform>();
    private float lastTriggerTime = 0f;
    
    void Start()
    {
        // Find Adam if not assigned
        if (adam == null)
        {
            GameObject adamObj = GameObject.FindGameObjectWithTag("Enemy");
            if (adamObj != null) adam = adamObj.transform;
        }
        
        // Find HitFeedback if not assigned
        if (hitFeedback == null)
        {
            hitFeedback = FindObjectOfType<HitFeedback>();
        }
        
        // Auto-find limb transforms
        if (autoFindLimbs)
        {
            FindLimbTransforms();
        }
        
        if (debugMode)
        {
            Debug.Log($"MediaPipeLimbCollisionDetector: Found {detectedLimbs.Count} limb transforms");
            foreach (var limb in detectedLimbs)
            {
                Debug.Log($"  - {limb.name} at {limb.position}");
            }
        }
    }
    
    void FindLimbTransforms()
    {
        detectedLimbs.Clear();
        
        // Find Remy GameObject
        GameObject remy = GameObject.FindGameObjectWithTag("Player");
        if (remy == null) return;
        
        // Look for hand and foot bones
        string[] limbNames = {
            "mixamorig:LeftHand",
            "mixamorig:RightHand", 
            "mixamorig:LeftFoot",
            "mixamorig:RightFoot",
            "mixamorig:Head",
            // Also try simpler names in case of different naming
            "LeftHand", "RightHand", "LeftFoot", "RightFoot", "Head"
        };
        
        foreach (string limbName in limbNames)
        {
            Transform limbTransform = FindChildRecursive(remy.transform, limbName);
            if (limbTransform != null && !detectedLimbs.Contains(limbTransform))
            {
                detectedLimbs.Add(limbTransform);
                if (debugMode) Debug.Log($"Found limb: {limbName} -> {limbTransform.name}");
            }
        }
        
        // Also add any manually assigned limbs
        if (limbTransforms != null)
        {
            foreach (var limb in limbTransforms)
            {
                if (limb != null && !detectedLimbs.Contains(limb))
                {
                    detectedLimbs.Add(limb);
                }
            }
        }
    }
    
    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
            {
                return child;
            }
            Transform result = FindChildRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
    
    void Update()
    {
        if (adam == null || !canTrigger) return;
        
        // Check each limb against Adam
        foreach (Transform limb in detectedLimbs)
        {
            if (limb == null) continue;
            
            float distance = Vector3.Distance(limb.position, adam.position);
            
            if (distance <= contactDistance)
            {
                if (debugMode) Debug.Log($"🥊 CONTACT! {limb.name} hit Adam! Distance: {distance:F2}");
                TriggerHitEffect(limb.name);
                StartCooldown();
                break; // Only trigger once per frame
            }
            
            if (debugMode && distance < contactDistance + 0.5f)
            {
                Debug.Log($"{limb.name} near Adam: {distance:F2}");
            }
        }
    }
    
    void TriggerHitEffect(string limbName)
    {
        if (debugMode) Debug.Log($"🎯 {limbName} HIT ADAM! Triggering screen blink!");
        
        if (hitFeedback != null)
        {
            // Just trigger the screen blink without camera shake
            StartCoroutine(ScreenBlinkOnly());
        }
    }
    
    private System.Collections.IEnumerator ScreenBlinkOnly()
    {
        if (hitFeedback == null || hitFeedback.hitFlashImage == null) yield break;
        
        Color blinkColor = Color.white;
        float flashDuration = 0.3f;
        float maxAlpha = 0.7f;
        
        // Fade in
        float half = flashDuration * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, maxAlpha, t / half);
            hitFeedback.hitFlashImage.color = new Color(blinkColor.r, blinkColor.g, blinkColor.b, alpha);
            yield return null;
        }
        
        // Fade out
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(maxAlpha, 0f, t / half);
            hitFeedback.hitFlashImage.color = new Color(blinkColor.r, blinkColor.g, blinkColor.b, alpha);
            yield return null;
        }
        
        hitFeedback.hitFlashImage.color = new Color(blinkColor.r, blinkColor.g, blinkColor.b, 0f);
    }
    
    void StartCooldown()
    {
        canTrigger = false;
        lastTriggerTime = Time.time;
        Invoke("ResetCooldown", cooldownTime);
    }
    
    void ResetCooldown()
    {
        canTrigger = true;
        if (debugMode) Debug.Log("Hit detection ready");
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos || adam == null) return;
        
        // Draw Adam's hit zone
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(adam.position, contactDistance);
        
        // Draw limb positions and distances
        foreach (Transform limb in detectedLimbs)
        {
            if (limb == null) continue;
            
            float distance = Vector3.Distance(limb.position, adam.position);
            Gizmos.color = distance <= contactDistance ? Color.red : Color.green;
            Gizmos.DrawWireSphere(limb.position, 0.1f);
            
            // Draw line from limb to Adam
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(limb.position, adam.position);
        }
    }
    
    void OnGUI()
    {
        if (!debugMode) return;
        
        GUI.Box(new Rect(10, 340, 400, 200), "LIMB COLLISION DETECTOR");
        
        int y = 365;
        GUI.Label(new Rect(20, y, 380, 20), $"Limbs Found: {detectedLimbs.Count}");
        y += 20;
        GUI.Label(new Rect(20, y, 380, 20), $"Contact Distance: {contactDistance:F1}");
        y += 20;
        GUI.Label(new Rect(20, y, 380, 20), $"Can Trigger: {canTrigger}");
        y += 20;
        
        // Show distances for each limb
        for (int i = 0; i < detectedLimbs.Count && i < 6; i++)
        {
            if (detectedLimbs[i] != null && adam != null)
            {
                float distance = Vector3.Distance(detectedLimbs[i].position, adam.position);
                string limbName = detectedLimbs[i].name.Replace("mixamorig:", "");
                GUI.Label(new Rect(20, y, 380, 20), $"{limbName}: {distance:F2}");
                y += 18;
            }
        }
    }
}