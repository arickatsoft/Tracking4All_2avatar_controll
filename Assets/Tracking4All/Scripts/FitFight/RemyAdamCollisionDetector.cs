using UnityEngine;
using System.Collections;

public class RemyAdamCollisionDetector : MonoBehaviour
{
    [Header("Character References")]
    [Tooltip("Reference to Remy GameObject")]
    public Transform remyTransform;
    
    [Tooltip("Reference to Adam GameObject")]
    public Transform adamTransform;
    
    [Header("Detection Settings")]
    [Tooltip("Distance threshold for detecting contact between Remy and Adam")]
    public float contactDistance = 1.5f;
    
    [Tooltip("How often to check for collision (in seconds)")]
    public float checkInterval = 0.1f;
    
    [Header("Visual Feedback")]
    [Tooltip("Reference to HitFeedback for screen effects")]
    public HitFeedback hitFeedback;
    
    [Tooltip("Color for the screen blink when Remy hits Adam")]
    public Color blinkColor = Color.white;
    
    [Header("Audio")]
    [Tooltip("Sound to play when contact is detected")]
    public AudioClip contactSound;
    
    [Header("Debug")]
    [Tooltip("Show debug information in console")]
    public bool debugMode = false;
    
    private bool wasInContact = false;
    private bool canTrigger = true;
    private float cooldownDuration = 1.0f;
    
    void Start()
    {
        // Find character references if not assigned
        if (remyTransform == null)
        {
            GameObject remyObj = GameObject.FindGameObjectWithTag("Player");
            if (remyObj != null)
            {
                remyTransform = remyObj.transform;
                if (debugMode) Debug.Log("Found Remy by Player tag");
            }
        }
        
        if (adamTransform == null)
        {
            GameObject adamObj = GameObject.FindGameObjectWithTag("Enemy");
            if (adamObj != null)
            {
                adamTransform = adamObj.transform;
                if (debugMode) Debug.Log("Found Adam by Enemy tag");
            }
        }
        
        // Find HitFeedback if not assigned
        if (hitFeedback == null)
        {
            hitFeedback = FindObjectOfType<HitFeedback>();
        }
        
        // Start checking for collisions
        if (remyTransform != null && adamTransform != null)
        {
            StartCoroutine(CheckForContact());
            if (debugMode) Debug.Log("RemyAdamCollisionDetector: Started monitoring contact between Remy and Adam");
        }
        else
        {
            Debug.LogError("RemyAdamCollisionDetector: Could not find Remy or Adam! Please assign references.");
        }
    }
    
    private IEnumerator CheckForContact()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            
            if (remyTransform != null && adamTransform != null && canTrigger)
            {
                float distance = Vector3.Distance(remyTransform.position, adamTransform.position);
                
                if (distance <= contactDistance)
                {
                    if (!wasInContact)
                    {
                        // Contact detected for the first time
                        OnContactDetected();
                        wasInContact = true;
                    }
                }
                else
                {
                    // No longer in contact
                    wasInContact = false;
                }
                
                if (debugMode && distance <= contactDistance + 0.5f)
                {
                    Debug.Log($"Distance between Remy and Adam: {distance:F2} (Contact threshold: {contactDistance})");
                }
            }
        }
    }
    
    private void OnContactDetected()
    {
        if (debugMode) Debug.Log("CONTACT DETECTED! Remy hit Adam - Triggering screen blink!");
        
        // Trigger screen blink effect
        if (hitFeedback != null)
        {
            hitFeedback.TriggerHit();
        }
        
        // Play contact sound
        if (contactSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(contactSound);
        }
        
        // Start cooldown to prevent rapid triggering
        StartCooldown();
    }
    
    private void StartCooldown()
    {
        canTrigger = false;
        Invoke(nameof(ResetCooldown), cooldownDuration);
    }
    
    private void ResetCooldown()
    {
        canTrigger = true;
        if (debugMode) Debug.Log("Contact detection cooldown reset - ready for next trigger");
    }
    
    void OnDrawGizmos()
    {
        // Draw debug visualization
        if (remyTransform != null && adamTransform != null)
        {
            // Draw line between characters
            Gizmos.color = wasInContact ? Color.red : Color.green;
            Gizmos.DrawLine(remyTransform.position, adamTransform.position);
            
            // Draw contact distance sphere around Adam
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(adamTransform.position, contactDistance);
            
            // Draw character positions
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(remyTransform.position, 0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(adamTransform.position, 0.2f);
        }
    }
    
    void OnGUI()
    {
        if (debugMode && remyTransform != null && adamTransform != null)
        {
            float distance = Vector3.Distance(remyTransform.position, adamTransform.position);
            GUI.Label(new Rect(10, 10, 300, 20), $"Remy-Adam Distance: {distance:F2}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Contact Threshold: {contactDistance:F2}");
            GUI.Label(new Rect(10, 50, 300, 20), $"In Contact: {wasInContact}");
            GUI.Label(new Rect(10, 70, 300, 20), $"Can Trigger: {canTrigger}");
        }
    }
}