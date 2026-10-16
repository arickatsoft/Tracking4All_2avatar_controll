using UnityEngine;
using UnityEngine.EventSystems;

public class FixDuplicateSystems : MonoBehaviour
{
    [Header("🔇 FIX DUPLICATE SYSTEMS")]
    [Space(10)]
    
    [Header("Auto Fix Settings")]
    public bool fixOnStart = true;
    public bool enableDebugLogs = false; // Set to false to reduce log spam
    
    void Start()
    {
        if (fixOnStart)
        {
            FixAllDuplicates();
        }
    }
    
    [ContextMenu("🔧 Fix All Duplicate Systems")]
    public void FixAllDuplicates()
    {
        if (enableDebugLogs)
        {
            Debug.Log("🔧 FIXING DUPLICATE SYSTEMS...");
        }
        
        FixDuplicateAudioListeners();
        FixDuplicateEventSystems();
        
        if (enableDebugLogs)
        {
            Debug.Log("✅ Duplicate system fix complete!");
        }
    }
    
    void FixDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        
        if (listeners.Length <= 1)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"✅ Audio Listeners: {listeners.Length} (OK)");
            }
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"⚠️ Found {listeners.Length} Audio Listeners, fixing...");
        }
        
        // Keep the first one (usually Main Camera), disable the rest
        for (int i = 1; i < listeners.Length; i++)
        {
            listeners[i].enabled = false;
            
            if (enableDebugLogs)
            {
                Debug.Log($"   ❌ Disabled AudioListener on {listeners[i].gameObject.name}");
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ Audio Listeners fixed: Kept 1, disabled {listeners.Length - 1}");
        }
    }
    
    void FixDuplicateEventSystems()
    {
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        
        if (eventSystems.Length <= 1)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"✅ Event Systems: {eventSystems.Length} (OK)");
            }
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"⚠️ Found {eventSystems.Length} Event Systems, fixing...");
        }
        
        // Keep the first one, destroy the rest
        for (int i = 1; i < eventSystems.Length; i++)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"   🗑️ Destroying duplicate EventSystem on {eventSystems[i].gameObject.name}");
            }
            
            Destroy(eventSystems[i].gameObject);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ Event Systems fixed: Kept 1, destroyed {eventSystems.Length - 1}");
        }
    }
    
    [ContextMenu("📊 Check System Status")]
    public void CheckSystemStatus()
    {
        Debug.Log("📊 CHECKING SYSTEM STATUS...");
        
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        Debug.Log($"🔊 Audio Listeners: {listeners.Length}");
        
        foreach (AudioListener listener in listeners)
        {
            Debug.Log($"   - {listener.gameObject.name}: {(listener.enabled ? "✅ Enabled" : "❌ Disabled")}");
        }
        
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        Debug.Log($"🎮 Event Systems: {eventSystems.Length}");
        
        foreach (EventSystem eventSystem in eventSystems)
        {
            Debug.Log($"   - {eventSystem.gameObject.name}: {(eventSystem.enabled ? "✅ Enabled" : "❌ Disabled")}");
        }
        
        if (listeners.Length == 1 && eventSystems.Length == 1)
        {
            Debug.Log("✅ ALL SYSTEMS GOOD - No more warning messages!");
        }
        else
        {
            Debug.Log("⚠️ Issues detected - run Fix All Duplicate Systems");
        }
    }
    
    // Continuously monitor and fix (optional)
    void Update()
    {
        // Only check every 60 frames to avoid performance impact
        if (Time.frameCount % 60 == 0)
        {
            // Silent fix without logs
            bool oldLogState = enableDebugLogs;
            enableDebugLogs = false;
            
            FixAllDuplicates();
            
            enableDebugLogs = oldLogState;
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(420, 200, 350, 100), "🔇 FIX DUPLICATE SYSTEMS");
        
        if (GUI.Button(new Rect(430, 225, 120, 25), "🔧 Fix Systems"))
        {
            FixAllDuplicates();
        }
        
        if (GUI.Button(new Rect(560, 225, 120, 25), "📊 Check Status"))
        {
            CheckSystemStatus();
        }
        
        GUI.Label(new Rect(430, 260, 330, 20), "🔇 Removes: Audio Listener & Event System warnings");
        GUI.Label(new Rect(430, 280, 330, 20), "✅ Auto-fixes continuously in background");
    }
}
