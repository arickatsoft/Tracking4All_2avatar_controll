// IndependentPoseSettingsController.cs
// Controls mirror and perspective flip settings independently for each avatar
// (C) 2024-2025 G8gaming Ltd.

using UnityEngine;
using System.Reflection;

/// <summary>
/// Allows independent control of Mirror and PerspectiveFlip settings for Adam and Remy
/// Automatically switches which settings are being controlled based on active avatar
/// </summary>
public class IndependentPoseSettingsController : MonoBehaviour
{
    [Header("Pose Solution References")]
    [SerializeField] private GameObject remyPoseSolution;
    [SerializeField] private GameObject adamPoseSolution;
    
    [Header("Current Active Avatar")]
    [SerializeField] private string currentActiveAvatar = "Remy"; // "Remy" or "Adam"
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showDebugUI = true;
    
    private object remyModelSettings;
    private object adamModelSettings;
    private MonoBehaviour remyRunner;
    private MonoBehaviour adamRunner;
    
    private void Start()
    {
        FindPoseSolutions();
        InitializeSettings();
    }
    
    private void FindPoseSolutions()
    {
        // Find pose solutions if not assigned
        if (remyPoseSolution == null)
        {
            remyPoseSolution = GameObject.Find("MPUPoseSolutionRemy");
            if (remyPoseSolution == null)
            {
                remyPoseSolution = GameObject.Find("Pose Tracking Solution");
            }
        }
        
        if (adamPoseSolution == null)
        {
            adamPoseSolution = GameObject.Find("Pose Tracking Solution Adam");
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[IndependentSettings] Remy Solution: {(remyPoseSolution != null ? remyPoseSolution.name : "NOT FOUND")}");
            Debug.Log($"[IndependentSettings] Adam Solution: {(adamPoseSolution != null ? adamPoseSolution.name : "NOT FOUND")}");
        }
    }
    
    private void InitializeSettings()
    {
        if (remyPoseSolution != null)
        {
            remyRunner = FindRunnerComponent(remyPoseSolution);
            if (remyRunner != null)
            {
                remyModelSettings = GetModelSettings(remyRunner);
                if (enableDebugLogs && remyModelSettings != null)
                {
                    Debug.Log("[IndependentSettings] ✅ Found Remy's modelSettings");
                }
            }
        }
        
        if (adamPoseSolution != null)
        {
            adamRunner = FindRunnerComponent(adamPoseSolution);
            if (adamRunner != null)
            {
                adamModelSettings = GetModelSettings(adamRunner);
                if (enableDebugLogs && adamModelSettings != null)
                {
                    Debug.Log("[IndependentSettings] ✅ Found Adam's modelSettings");
                }
            }
        }
        
        // Verify independence
        if (remyModelSettings != null && adamModelSettings != null)
        {
            bool areIndependent = !ReferenceEquals(remyModelSettings, adamModelSettings);
            if (enableDebugLogs)
            {
                Debug.Log($"[IndependentSettings] Settings are independent: {areIndependent}");
            }
            
            if (!areIndependent)
            {
                Debug.LogWarning("[IndependentSettings] ⚠️ WARNING: Settings are SHARED! Both avatars will have same mirror/flip settings!");
            }
        }
    }
    
    private MonoBehaviour FindRunnerComponent(GameObject solution)
    {
        var components = solution.GetComponentsInChildren<MonoBehaviour>(true);
        
        foreach (var component in components)
        {
            var modelSettingsField = component.GetType().GetField("modelSettings",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (modelSettingsField != null)
            {
                return component;
            }
        }
        
        return null;
    }
    
    private object GetModelSettings(MonoBehaviour runner)
    {
        if (runner == null) return null;
        
        var modelSettingsField = runner.GetType().GetField("modelSettings",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        return modelSettingsField?.GetValue(runner);
    }
    
    // Public methods to control current active avatar
    public void SetActiveAvatar(string avatarName)
    {
        currentActiveAvatar = avatarName;
        if (enableDebugLogs)
        {
            Debug.Log($"[IndependentSettings] Active avatar changed to: {avatarName}");
        }
    }
    
    public void SetRemyActive()
    {
        SetActiveAvatar("Remy");
    }
    
    public void SetAdamActive()
    {
        SetActiveAvatar("Adam");
    }
    
    // Get/Set Mirror for current active avatar
    public bool GetMirror()
    {
        var settings = GetCurrentSettings();
        return GetBooleanSetting(settings, "mirror");
    }
    
    public void SetMirror(bool value)
    {
        var settings = GetCurrentSettings();
        SetBooleanSetting(settings, "mirror", value);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[IndependentSettings] Set {currentActiveAvatar} Mirror = {value}");
        }
    }
    
    // Get/Set PerspectiveFlip for current active avatar
    public bool GetPerspectiveFlip()
    {
        var settings = GetCurrentSettings();
        return GetBooleanSetting(settings, "perspectiveFlip");
    }
    
    public void SetPerspectiveFlip(bool value)
    {
        var settings = GetCurrentSettings();
        SetBooleanSetting(settings, "perspectiveFlip", value);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[IndependentSettings] Set {currentActiveAvatar} PerspectiveFlip = {value}");
        }
    }
    
    // Direct control for Remy
    public bool GetRemyMirror()
    {
        return GetBooleanSetting(remyModelSettings, "mirror");
    }
    
    public void SetRemyMirror(bool value)
    {
        SetBooleanSetting(remyModelSettings, "mirror", value);
        if (enableDebugLogs)
        {
            Debug.Log($"[IndependentSettings] Set Remy Mirror = {value}");
        }
    }
    
    public bool GetRemyPerspectiveFlip()
    {
        return GetBooleanSetting(remyModelSettings, "perspectiveFlip");
    }
    
    public void SetRemyPerspectiveFlip(bool value)
    {
        SetBooleanSetting(remyModelSettings, "perspectiveFlip", value);
        if (enableDebugLogs)
        {
            Debug.Log($"[IndependentSettings] Set Remy PerspectiveFlip = {value}");
        }
    }
    
    // Direct control for Adam
    public bool GetAdamMirror()
    {
        return GetBooleanSetting(adamModelSettings, "mirror");
    }
    
    public void SetAdamMirror(bool value)
    {
        SetBooleanSetting(adamModelSettings, "mirror", value);
        if (enableDebugLogs)
        {
            Debug.Log($"[IndependentSettings] Set Adam Mirror = {value}");
        }
    }
    
    public bool GetAdamPerspectiveFlip()
    {
        return GetBooleanSetting(adamModelSettings, "perspectiveFlip");
    }
    
    public void SetAdamPerspectiveFlip(bool value)
    {
        SetBooleanSetting(adamModelSettings, "perspectiveFlip", value);
        if (enableDebugLogs)
        {
            Debug.Log($"[IndependentSettings] Set Adam PerspectiveFlip = {value}");
        }
    }
    
    // Helper methods
    private object GetCurrentSettings()
    {
        return currentActiveAvatar == "Adam" ? adamModelSettings : remyModelSettings;
    }
    
    private bool GetBooleanSetting(object settings, string fieldName)
    {
        if (settings == null) return false;
        
        var settingsType = settings.GetType();
        var field = settingsType.GetField(fieldName);
        
        if (field != null)
        {
            var settingObject = field.GetValue(settings);
            if (settingObject != null)
            {
                var valueProperty = settingObject.GetType().GetProperty("Value");
                if (valueProperty != null)
                {
                    var value = valueProperty.GetValue(settingObject);
                    return value is bool boolValue && boolValue;
                }
            }
        }
        
        return false;
    }
    
    private void SetBooleanSetting(object settings, string fieldName, bool value)
    {
        if (settings == null) return;
        
        var settingsType = settings.GetType();
        var field = settingsType.GetField(fieldName);
        
        if (field != null)
        {
            var settingObject = field.GetValue(settings);
            if (settingObject != null)
            {
                var valueProperty = settingObject.GetType().GetProperty("Value");
                if (valueProperty != null && valueProperty.CanWrite)
                {
                    valueProperty.SetValue(settingObject, value);
                }
            }
        }
    }
    
    // Debug UI
    private void OnGUI()
    {
        if (!showDebugUI) return;
        
        GUILayout.BeginArea(new Rect(10, 450, 400, 300));
        GUILayout.Label($"=== INDEPENDENT POSE SETTINGS ===", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
        GUILayout.Label($"Current Active: {currentActiveAvatar}");
        GUILayout.Space(10);
        
        // Remy Controls
        GUILayout.Label("--- REMY ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        bool remyMirror = GetRemyMirror();
        bool newRemyMirror = GUILayout.Toggle(remyMirror, $"Remy Mirror: {remyMirror}");
        if (newRemyMirror != remyMirror) SetRemyMirror(newRemyMirror);
        
        bool remyFlip = GetRemyPerspectiveFlip();
        bool newRemyFlip = GUILayout.Toggle(remyFlip, $"Remy PerspectiveFlip: {remyFlip}");
        if (newRemyFlip != remyFlip) SetRemyPerspectiveFlip(newRemyFlip);
        
        GUILayout.Space(10);
        
        // Adam Controls
        GUILayout.Label("--- ADAM ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        bool adamMirror = GetAdamMirror();
        bool newAdamMirror = GUILayout.Toggle(adamMirror, $"Adam Mirror: {adamMirror}");
        if (newAdamMirror != adamMirror) SetAdamMirror(newAdamMirror);
        
        bool adamFlip = GetAdamPerspectiveFlip();
        bool newAdamFlip = GUILayout.Toggle(adamFlip, $"Adam PerspectiveFlip: {adamFlip}");
        if (newAdamFlip != adamFlip) SetAdamPerspectiveFlip(newAdamFlip);
        
        GUILayout.Space(10);
        
        // Quick switch buttons
        if (GUILayout.Button("Switch to Remy"))
        {
            SetRemyActive();
        }
        if (GUILayout.Button("Switch to Adam"))
        {
            SetAdamActive();
        }
        
        GUILayout.EndArea();
    }
    
    // Context menu for testing
    [ContextMenu("Log Current Settings")]
    public void LogCurrentSettings()
    {
        Debug.Log("=== CURRENT SETTINGS ===");
        Debug.Log($"Active Avatar: {currentActiveAvatar}");
        Debug.Log($"Remy Mirror: {GetRemyMirror()}");
        Debug.Log($"Remy PerspectiveFlip: {GetRemyPerspectiveFlip()}");
        Debug.Log($"Adam Mirror: {GetAdamMirror()}");
        Debug.Log($"Adam PerspectiveFlip: {GetAdamPerspectiveFlip()}");
    }
    
    [ContextMenu("Toggle Remy Mirror")]
    public void ToggleRemyMirror()
    {
        SetRemyMirror(!GetRemyMirror());
    }
    
    [ContextMenu("Toggle Adam Mirror")]
    public void ToggleAdamMirror()
    {
        SetAdamMirror(!GetAdamMirror());
    }
}
