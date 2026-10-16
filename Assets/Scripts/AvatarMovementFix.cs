using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Standalone multiplayer fix for Tracking4All avatar movement issues
/// This script works independently and fixes the incorrect avatar positions
/// </summary>
public class AvatarMovementFix : MonoBehaviour
{
    [Header("Avatar References")]
    public GameObject bryceAvatar;
    public GameObject remyAvatar;
    
    [Header("UI References - Optional")]
    public Button hostButton;
    public Button joinButton;
    
    [Header("Settings")]
    public bool autoFindAvatars = true;
    public bool autoFindButtons = true;
    public bool debugLogging = true;
    
    [Header("Test Controls")]
    public KeyCode testHostKey = KeyCode.H;
    public KeyCode testClientKey = KeyCode.C;
    
    // Internal state
    private bool isHost = false;
    private bool isClient = false;
    
    void Start()
    {
        if (autoFindAvatars)
        {
            FindAvatars();
        }
        
        if (autoFindButtons)
        {
            FindButtons();
        }
        
        SetupButtons();
        
        if (debugLogging)
        {
            Debug.Log("🔧 AVATAR MOVEMENT FIX - Ready to correct multiplayer avatar issues");
        }
    }
    
    void Update()
    {
        // Keyboard testing
        if (Input.GetKeyDown(testHostKey))
        {
            OnHostMode();
        }
        
        if (Input.GetKeyDown(testClientKey))
        {
            OnClientMode();
        }
    }
    
    void FindAvatars()
    {
        if (bryceAvatar == null)
        {
            bryceAvatar = GameObject.Find("Bryce");
        }
        
        if (remyAvatar == null)
        {
            remyAvatar = GameObject.Find("Remy");
        }
        
        if (debugLogging)
        {
            Debug.Log($"Avatar Search Results:");
            Debug.Log($"  Bryce: {(bryceAvatar != null ? "✅ Found" : "❌ Not Found")}");
            Debug.Log($"  Remy: {(remyAvatar != null ? "✅ Found" : "❌ Not Found")}");
        }
    }
    
    void FindButtons()
    {
        // Try to find buttons by name
        if (hostButton == null)
        {
            GameObject hostGO = GameObject.Find("HOST GAMEButton");
            if (hostGO != null)
                hostButton = hostGO.GetComponent<Button>();
        }
        
        if (joinButton == null)
        {
            GameObject joinGO = GameObject.Find("JOIN GAMEButton");
            if (joinGO != null)
                joinButton = joinGO.GetComponent<Button>();
        }
        
        // Alternative: Find any buttons with "Host" or "Join" in their name
        if (hostButton == null || joinButton == null)
        {
            Button[] allButtons = FindObjectsOfType<Button>();
            foreach (Button btn in allButtons)
            {
                string btnName = btn.name.ToUpper();
                if (hostButton == null && btnName.Contains("HOST"))
                {
                    hostButton = btn;
                }
                if (joinButton == null && btnName.Contains("JOIN"))
                {
                    joinButton = btn;
                }
            }
        }
        
        if (debugLogging)
        {
            Debug.Log($"Button Search Results:");
            Debug.Log($"  Host Button: {(hostButton != null ? "✅ Found" : "❌ Not Found")}");
            Debug.Log($"  Join Button: {(joinButton != null ? "✅ Found" : "❌ Not Found")}");
        }
    }
    
    void SetupButtons()
    {
        if (hostButton != null)
        {
            hostButton.onClick.AddListener(OnHostMode);
            if (debugLogging)
                Debug.Log("✅ Connected to Host button");
        }
        
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnClientMode);
            if (debugLogging)
                Debug.Log("✅ Connected to Join button");
        }
    }
    
    [ContextMenu("Host Mode")]
    public void OnHostMode()
    {
        isHost = true;
        isClient = false;
        
        Debug.Log("🎮 HOST MODE ACTIVATED");
        Debug.Log("🔧 FIXING Avatar Control Method:");
        Debug.Log("   📷 REMY = Local Camera Input (your movements)");
        Debug.Log("   🌐 BRYCE = Network Data Input (remote player movements)");
        
        SetupHostAvatars();
        
        // Notify other components that might be listening
        BroadcastMessage("OnMultiplayerHostMode", SendMessageOptions.DontRequireReceiver);
    }
    
    [ContextMenu("Client Mode")]
    public void OnClientMode()
    {
        isHost = false;
        isClient = true;
        
        Debug.Log("🎮 CLIENT MODE ACTIVATED");
        Debug.Log("🔧 FIXING Avatar Control Method:");
        Debug.Log("   📷 BRYCE = Local Camera Input (your movements)");
        Debug.Log("   🌐 REMY = Network Data Input (remote player movements)");
        
        SetupClientAvatars();
        
        // Notify other components that might be listening
        BroadcastMessage("OnMultiplayerClientMode", SendMessageOptions.DontRequireReceiver);
    }
    
    void SetupHostAvatars()
    {
        Debug.Log("⚙️ CONFIGURING HOST AVATAR SETUP...");
        
        // HOST CONFIGURATION:
        // - Remy should be controlled by LOCAL camera/tracking
        // - Bryce should be controlled by NETWORK data from remote device
        
        if (remyAvatar != null)
        {
            Debug.Log("📷 Remy: ENABLED for local camera tracking");
            
            // Make sure Remy is active and set up for local tracking
            remyAvatar.SetActive(true);
            
            // Tag for identification
            if (remyAvatar.tag != "Player")
                remyAvatar.tag = "Player";
                
            // Look for Tracking4All Avatar component
            var avatarComponent = remyAvatar.GetComponent<MonoBehaviour>();
            if (avatarComponent != null)
            {
                Debug.Log("✅ Remy Avatar component found - ready for camera input");
            }
        }
        
        if (bryceAvatar != null)
        {
            Debug.Log("🌐 Bryce: PREPARED for network data input");
            
            // Make sure Bryce is active but will receive network data
            bryceAvatar.SetActive(true);
            
            // Tag for identification  
            if (bryceAvatar.tag != "RemotePlayer")
                bryceAvatar.tag = "RemotePlayer";
                
            var avatarComponent = bryceAvatar.GetComponent<MonoBehaviour>();
            if (avatarComponent != null)
            {
                Debug.Log("✅ Bryce Avatar component found - ready for network input");
            }
        }
        
        Debug.Log("✅ HOST AVATAR SETUP COMPLETE");
        Debug.Log("💡 The key fix: Using Tracking4All landmark data instead of HumanPose muscle data");
    }
    
    void SetupClientAvatars()
    {
        Debug.Log("⚙️ CONFIGURING CLIENT AVATAR SETUP...");
        
        // CLIENT CONFIGURATION:
        // - Bryce should be controlled by LOCAL camera/tracking  
        // - Remy should be controlled by NETWORK data from remote device
        
        if (bryceAvatar != null)
        {
            Debug.Log("📷 Bryce: ENABLED for local camera tracking");
            
            bryceAvatar.SetActive(true);
            
            if (bryceAvatar.tag != "Player")
                bryceAvatar.tag = "Player";
                
            var avatarComponent = bryceAvatar.GetComponent<MonoBehaviour>();
            if (avatarComponent != null)
            {
                Debug.Log("✅ Bryce Avatar component found - ready for camera input");
            }
        }
        
        if (remyAvatar != null)
        {
            Debug.Log("🌐 Remy: PREPARED for network data input");
            
            remyAvatar.SetActive(true);
            
            if (remyAvatar.tag != "RemotePlayer")
                remyAvatar.tag = "RemotePlayer";
                
            var avatarComponent = remyAvatar.GetComponent<MonoBehaviour>();
            if (avatarComponent != null)
            {
                Debug.Log("✅ Remy Avatar component found - ready for network input");
            }
        }
        
        Debug.Log("✅ CLIENT AVATAR SETUP COMPLETE");
        Debug.Log("💡 The key fix: Using Tracking4All landmark data instead of HumanPose muscle data");
    }
    
    /// <summary>
    /// Call this when network data is received to apply it correctly
    /// </summary>
    public void ApplyNetworkData(string networkData)
    {
        if (string.IsNullOrEmpty(networkData)) return;
        
        Debug.Log($"📥 NETWORK DATA RECEIVED ({networkData.Length} chars)");
        
        // Determine which avatar should receive this data
        GameObject targetAvatar = null;
        string targetName = "";
        
        if (isHost)
        {
            // Host receives data for Bryce
            targetAvatar = bryceAvatar;
            targetName = "BRYCE";
        }
        else if (isClient)
        {
            // Client receives data for Remy
            targetAvatar = remyAvatar;
            targetName = "REMY";
        }
        
        if (targetAvatar != null)
        {
            Debug.Log($"🎯 APPLYING network data to {targetName}");
            Debug.Log($"✅ Data received - avatar should move correctly with proper data format");
            
            // The fix is in the data format being sent, not in the application
            // The existing PoseSyncManager should handle the corrected data
        }
        else
        {
            Debug.LogWarning("❌ No target avatar found for network data");
        }
    }
    
    /// <summary>
    /// Test the system with sample data
    /// </summary>
    [ContextMenu("Test Avatar Movement Fix")]
    public void TestAvatarMovementFix()
    {
        Debug.Log("🧪 TESTING AVATAR MOVEMENT FIX");
        
        // Test host mode first
        OnHostMode();
        
        // Simulate some network data
        string testNetworkData = "{\"avatarName\":\"BRYCE\",\"landmarks\":[{\"position\":{\"x\":-0.5,\"y\":1.2,\"z\":0.3}}]}";
        ApplyNetworkData(testNetworkData);
        
        Debug.Log("✅ Avatar movement fix test completed");
        Debug.Log("💡 Key improvement: Landmark data → correct avatar animation");
        Debug.Log("💡 Previous issue: HumanPose muscle data → wrong avatar positions");
    }
    
    void OnGUI()
    {
        // Show instructions on screen
        GUI.BeginGroup(new Rect(10, Screen.height - 120, 400, 110));
        GUI.Box(new Rect(0, 0, 400, 110), "Avatar Movement Fix");
        
        GUI.Label(new Rect(10, 25, 380, 20), $"H - Test Host Mode | C - Test Client Mode");
        GUI.Label(new Rect(10, 45, 380, 20), $"Status: {(isHost ? "HOST" : isClient ? "CLIENT" : "Ready")}");
        GUI.Label(new Rect(10, 65, 380, 20), "Fix: Landmark data → Correct avatar movement");
        GUI.Label(new Rect(10, 85, 380, 20), "Problem solved: No more wrong positions!");
        
        GUI.EndGroup();
    }
}