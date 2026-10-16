// SimpleAvatarSwitcher.cs
// Simple avatar switching - treats Remy and Adam equally
// No special logic, no managers, just enable/disable
// (C) 2024-2025 G8gaming Ltd.

using UnityEngine;

/// <summary>
/// Simple switcher that treats all avatars equally.
/// Each avatar has its own pose solution - just enable the pair you want active.
/// </summary>
public class SimpleAvatarSwitcher : MonoBehaviour
{
    [Header("Remy Setup")]
    [SerializeField] private GameObject remyAvatar;
    [SerializeField] private GameObject remySolution;
    
    [Header("Adam Setup")]
    [SerializeField] private GameObject adamAvatar;
    [SerializeField] private GameObject adamSolution;
    
    [Header("Camera Rigs (Optional)")]
    [SerializeField] private GameObject remyCameraRig;
    [SerializeField] private GameObject adamCameraRig;
    
    [Header("Current Active")]
    [SerializeField] private string currentActive = "Remy";
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Start()
    {
        // Auto-find if not assigned
        FindAvatars();
        FindSolutions();
        
        // Start with Remy active (or whoever you prefer)
        if (currentActive == "Remy")
        {
            SwitchToRemy();
        }
        else if (currentActive == "Adam")
        {
            SwitchToAdam();
        }
    }
    
    private void FindAvatars()
    {
        if (remyAvatar == null)
        {
            remyAvatar = GameObject.Find("Remy");
        }
        
        if (adamAvatar == null)
        {
            adamAvatar = GameObject.Find("Adam_Big");
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[SimpleAvatarSwitcher] Remy Avatar: {(remyAvatar != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"[SimpleAvatarSwitcher] Adam Avatar: {(adamAvatar != null ? "Found" : "NOT FOUND")}");
        }
    }
    
    private void FindSolutions()
    {
        if (remySolution == null)
        {
            remySolution = GameObject.Find("MPUPoseSolutionRemy");
            if (remySolution == null)
            {
                remySolution = GameObject.Find("Pose Tracking Solution");
            }
        }
        
        if (adamSolution == null)
        {
            adamSolution = GameObject.Find("MPUPoseSolutionAdam");
            if (adamSolution == null)
            {
                adamSolution = GameObject.Find("Pose Tracking Solution Adam");
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[SimpleAvatarSwitcher] Remy Solution: {(remySolution != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"[SimpleAvatarSwitcher] Adam Solution: {(adamSolution != null ? "Found" : "NOT FOUND")}");
        }
    }
    
    [ContextMenu("Switch to Remy")]
    public void SwitchToRemy()
    {
        if (enableDebugLogs) Debug.Log("[SimpleAvatarSwitcher] Switching to Remy");
        
        currentActive = "Remy";
        
        // Enable Remy's tracking
        if (remySolution != null) remySolution.SetActive(true);
        if (remyAvatar != null)
        {
            var avatar = remyAvatar.GetComponent<Tracking4All.Avatar>();
            if (avatar != null) avatar.enabled = true;
            remyAvatar.tag = "Player";
        }
        if (remyCameraRig != null) remyCameraRig.SetActive(true);
        
        // Disable Adam's tracking
        if (adamSolution != null) adamSolution.SetActive(false);
        if (adamAvatar != null)
        {
            var avatar = adamAvatar.GetComponent<Tracking4All.Avatar>();
            if (avatar != null) avatar.enabled = false;
            adamAvatar.tag = "Enemy";
        }
        if (adamCameraRig != null) adamCameraRig.SetActive(false);
        
        if (enableDebugLogs) Debug.Log("[SimpleAvatarSwitcher] ✅ Remy is now active");
    }
    
    [ContextMenu("Switch to Adam")]
    public void SwitchToAdam()
    {
        if (enableDebugLogs) Debug.Log("[SimpleAvatarSwitcher] Switching to Adam");
        
        currentActive = "Adam";
        
        // Enable Adam's tracking
        if (adamSolution != null) adamSolution.SetActive(true);
        if (adamAvatar != null)
        {
            var avatar = adamAvatar.GetComponent<Tracking4All.Avatar>();
            if (avatar != null) avatar.enabled = true;
            adamAvatar.tag = "Player";
        }
        if (adamCameraRig != null) adamCameraRig.SetActive(true);
        
        // Disable Remy's tracking
        if (remySolution != null) remySolution.SetActive(false);
        if (remyAvatar != null)
        {
            var avatar = remyAvatar.GetComponent<Tracking4All.Avatar>();
            if (avatar != null) avatar.enabled = false;
            remyAvatar.tag = "Enemy";
        }
        if (remyCameraRig != null) remyCameraRig.SetActive(false);
        
        if (enableDebugLogs) Debug.Log("[SimpleAvatarSwitcher] ✅ Adam is now active");
    }
    
    [ContextMenu("Toggle Active Avatar")]
    public void ToggleActiveAvatar()
    {
        if (currentActive == "Remy")
        {
            SwitchToAdam();
        }
        else
        {
            SwitchToRemy();
        }
    }
    
    // Debug UI
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 150));
        GUILayout.Label("=== AVATAR SWITCHER ===", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
        GUILayout.Label($"Current: {currentActive}");
        GUILayout.Space(10);
        
        if (GUILayout.Button("Switch to Remy"))
        {
            SwitchToRemy();
        }
        
        if (GUILayout.Button("Switch to Adam"))
        {
            SwitchToAdam();
        }
        
        if (GUILayout.Button("Toggle"))
        {
            ToggleActiveAvatar();
        }
        
        GUILayout.EndArea();
    }
}
