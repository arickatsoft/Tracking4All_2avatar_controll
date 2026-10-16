using UnityEngine;
using System.Collections.Generic;

public class HorizontalCollisionDetector : MonoBehaviour
{
    [Header("Target")]
    public Transform adam;
    
    [Header("Detection Settings")]
    [Tooltip("Horizontal distance threshold (ignores Y axis)")]
    public float horizontalContactDistance = 1.5f;
    
    [Tooltip("Cooldown between triggers")]
    public float cooldownTime = 1.0f;
    
    [Header("Visual Feedback")]
    public HitFeedback hitFeedback;
    
    [Header("Debug")]
    public bool debugMode = false;
    
    private bool canTrigger = true;
    private List<Transform> detectedLimbs = new List<Transform>();
    
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
        
        // Find limbs
        FindLimbTransforms();
        
        if (debugMode)
        {
            Debug.Log($"HorizontalCollisionDetector: Found {detectedLimbs.Count} limbs");
        }
    }
    
    void FindLimbTransforms()
    {
        detectedLimbs.Clear();
        
        GameObject remy = GameObject.FindGameObjectWithTag("Player");
        if (remy == null) return;
        
        string[] limbNames = {
            "mixamorig:LeftHand",
            "mixamorig:RightHand", 
            "mixamorig:LeftFoot",
            "mixamorig:RightFoot"
        };
        
        foreach (string limbName in limbNames)
        {
            Transform limbTransform = FindChildRecursive(remy.transform, limbName);
            if (limbTransform != null)
            {
                detectedLimbs.Add(limbTransform);
                if (debugMode) Debug.Log($"Found: {limbName}");
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
        
        foreach (Transform limb in detectedLimbs)
        {
            if (limb == null) continue;
            
            // Calculate HORIZONTAL distance only (ignore Y axis)
            Vector3 limbPos = new Vector3(limb.position.x, 0, limb.position.z);
            Vector3 adamPos = new Vector3(adam.position.x, 0, adam.position.z);
            float horizontalDistance = Vector3.Distance(limbPos, adamPos);
            
            if (horizontalDistance <= horizontalContactDistance)
            {
                Debug.Log($"🥊 HORIZONTAL HIT! {limb.name} horizontally close to Adam! Distance: {horizontalDistance:F2}");
                TriggerHitEffect(limb.name);
                StartCooldown();
                break;
            }
            
            if (debugMode && horizontalDistance < horizontalContactDistance + 0.5f)
            {
                Debug.Log($"{limb.name} horizontally near Adam: {horizontalDistance:F2}");
            }
        }
    }
    
    void TriggerHitEffect(string limbName)
    {
        Debug.Log($"🎯 {limbName} HIT ADAM HORIZONTALLY! Triggering screen blink!");
        
        if (hitFeedback != null)
        {
            StartCoroutine(ScreenBlinkOnly());
        }
    }
    
    private System.Collections.IEnumerator ScreenBlinkOnly()
    {
        if (hitFeedback == null || hitFeedback.hitFlashImage == null) yield break;
        
        Color blinkColor = Color.white;
        float flashDuration = 0.4f;
        float maxAlpha = 0.8f;
        
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
        Debug.Log("Screen blink completed!");
    }
    
    void StartCooldown()
    {
        canTrigger = false;
        Invoke("ResetCooldown", cooldownTime);
    }
    
    void ResetCooldown()
    {
        canTrigger = true;
        if (debugMode) Debug.Log("Ready for next hit detection");
    }
    
    void OnGUI()
    {
        if (!debugMode) return;
        
        GUI.Box(new Rect(10, 120, 350, 120), "HORIZONTAL HIT DETECTOR");
        
        int y = 145;
        GUI.Label(new Rect(20, y, 320, 20), $"Horizontal Distance: {horizontalContactDistance:F1}");
        y += 20;
        GUI.Label(new Rect(20, y, 320, 20), $"Can Trigger: {canTrigger}");
        y += 20;
        
        if (adam != null && detectedLimbs.Count > 0)
        {
            foreach (Transform limb in detectedLimbs)
            {
                if (limb != null)
                {
                    Vector3 limbPos = new Vector3(limb.position.x, 0, limb.position.z);
                    Vector3 adamPos = new Vector3(adam.position.x, 0, adam.position.z);
                    float distance = Vector3.Distance(limbPos, adamPos);
                    
                    string limbName = limb.name.Replace("mixamorig:", "");
                    GUI.Label(new Rect(20, y, 320, 18), $"{limbName}: {distance:F2}");
                    y += 18;
                }
            }
        }
    }
}