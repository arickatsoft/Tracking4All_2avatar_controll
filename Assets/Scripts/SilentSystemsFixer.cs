using UnityEngine;
using UnityEngine.EventSystems;

public class SilentSystemsFixer : MonoBehaviour
{
    //[Header("🔇 SILENT SYSTEMS FIXER")]
    [Tooltip("Silently fixes duplicate AudioListeners and EventSystems without any console output")]
    
    void Awake()
    {
        // Fix immediately on Awake (before Start)
        SilentFix();
    }
    
    void Start()
    {
        // Fix again on Start to catch any late-created duplicates
        SilentFix();
    }
    
    void SilentFix()
    {
        // Fix AudioListeners - keep first, disable rest
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            for (int i = 1; i < listeners.Length; i++)
            {
                listeners[i].enabled = false;
            }
        }
        
        // Fix EventSystems - keep first, destroy rest  
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        if (eventSystems.Length > 1)
        {
            for (int i = 1; i < eventSystems.Length; i++)
            {
                DestroyImmediate(eventSystems[i].gameObject);
            }
        }
    }
    
    void Update()
    {
        // Continuously fix every 2 seconds
        if (Time.frameCount % 120 == 0) // Every 2 seconds at 60fps
        {
            SilentFix();
        }
    }
}
