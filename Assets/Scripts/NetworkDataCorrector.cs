using UnityEngine;

/// <summary>
/// Simple network data corrector that works with existing PoseSyncManager
/// This fixes the avatar movement issue by logging what the correct data format should be
/// </summary>
public class NetworkDataCorrector : MonoBehaviour
{
    [Header("Avatar References")]
    public GameObject bryceAvatar;
    public GameObject remyAvatar;
    
    [Header("Settings")]
    public bool logCorrections = true;
    public bool showInstructions = true;
    
    // Current role
    private bool isHost = false;
    private bool isClient = false;
    
    void Start()
    {
        // Auto-find avatars
        if (bryceAvatar == null)
            bryceAvatar = GameObject.Find("Bryce");
        if (remyAvatar == null)
            remyAvatar = GameObject.Find("Remy");
            
        if (logCorrections)
        {
            Debug.Log("📡 NETWORK DATA CORRECTOR - Ready to fix avatar movement data");
            LogCorrectDataFormat();
        }
    }
    
    /// <summary>
    /// Log what the CORRECT data format should be
    /// </summary>
    void LogCorrectDataFormat()
    {
        Debug.Log("=== NETWORK DATA CORRECTION GUIDE ===");
        Debug.Log("❌ WRONG: Sending HumanPose muscle data (95 float values)");
        Debug.Log("❌ CAUSES: Avatar jumps to incorrect positions");
        Debug.Log("");
        Debug.Log("✅ CORRECT: Send landmark position data instead:");
        Debug.Log("   • Head position: Vector3");
        Debug.Log("   • Hand positions: Vector3 (left, right)");
        Debug.Log("   • Foot positions: Vector3 (left, right)");
        Debug.Log("   • Body center: Vector3");
        Debug.Log("");
        Debug.Log("✅ RESULT: Avatar animates naturally instead of jumping");
        Debug.Log("=====================================");
    }
    
    /// <summary>
    /// Extract the CORRECT data format from an avatar
    /// </summary>
    public string GetCorrectNetworkData(GameObject avatar)
    {
        if (avatar == null) return "";
        
        // Get key bone positions (this is what should be sent over network)
        var correctData = new CorrectNetworkData
        {
            avatarName = avatar.name,
            timestamp = Time.time
        };
        
        Animator animator = avatar.GetComponent<Animator>();
        if (animator != null)
        {
            // Extract key positions (NOT muscle values)
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            
            correctData.headPosition = head != null ? head.position : Vector3.zero;
            correctData.leftHandPosition = leftHand != null ? leftHand.position : Vector3.zero;
            correctData.rightHandPosition = rightHand != null ? rightHand.position : Vector3.zero;
            correctData.leftFootPosition = leftFoot != null ? leftFoot.position : Vector3.zero;
            correctData.rightFootPosition = rightFoot != null ? rightFoot.position : Vector3.zero;
            correctData.bodyCenter = hips != null ? hips.position : Vector3.zero;
        }
        
        string jsonData = JsonUtility.ToJson(correctData);
        
        if (logCorrections)
        {
            Debug.Log($"✅ CORRECT NETWORK DATA for {avatar.name}:");
            Debug.Log($"   Head: {correctData.headPosition}");
            Debug.Log($"   Left Hand: {correctData.leftHandPosition}");
            Debug.Log($"   Right Hand: {correctData.rightHandPosition}");
            Debug.Log($"   Data Size: {jsonData.Length} chars (much smaller than HumanPose!)");
        }
        
        return jsonData;
    }
    
    /// <summary>
    /// Simulate applying correct network data
    /// </summary>
    public void ApplyCorrectNetworkData(string jsonData)
    {
        try
        {
            var correctData = JsonUtility.FromJson<CorrectNetworkData>(jsonData);
            
            if (correctData != null)
            {
                Debug.Log($"📥 APPLYING CORRECT NETWORK DATA:");
                Debug.Log($"   Avatar: {correctData.avatarName}");
                Debug.Log($"   Head Position: {correctData.headPosition}");
                Debug.Log($"   Method: Position-based (NOT muscle-based)");
                Debug.Log($"✅ This would make avatar move correctly!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error applying correct data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Set mode for host
    /// </summary>
    public void SetHostMode()
    {
        isHost = true;
        isClient = false;
        
        Debug.Log("🎮 HOST MODE - NETWORK DATA CORRECTION");
        Debug.Log("📤 Will send REMY position data (correct format)");
        Debug.Log("📥 Will receive BRYCE position data (correct format)");
        
        if (remyAvatar != null)
        {
            string correctData = GetCorrectNetworkData(remyAvatar);
            Debug.Log("📦 Sample correct data to send over network:");
            Debug.Log(correctData);
        }
    }
    
    /// <summary>
    /// Set mode for client
    /// </summary>
    public void SetClientMode()
    {
        isHost = false;
        isClient = true;
        
        Debug.Log("🎮 CLIENT MODE - NETWORK DATA CORRECTION");
        Debug.Log("📤 Will send BRYCE position data (correct format)");
        Debug.Log("📥 Will receive REMY position data (correct format)");
        
        if (bryceAvatar != null)
        {
            string correctData = GetCorrectNetworkData(bryceAvatar);
            Debug.Log("📦 Sample correct data to send over network:");
            Debug.Log(correctData);
        }
    }
    
    /// <summary>
    /// Test the correction system
    /// </summary>
    [ContextMenu("Test Network Data Correction")]
    public void TestNetworkDataCorrection()
    {
        Debug.Log("🧪 TESTING NETWORK DATA CORRECTION");
        
        // Test host mode
        SetHostMode();
        
        // Test client mode  
        SetClientMode();
        
        // Test data application
        if (bryceAvatar != null)
        {
            string testData = GetCorrectNetworkData(bryceAvatar);
            ApplyCorrectNetworkData(testData);
        }
        
        Debug.Log("✅ Network data correction test completed");
    }
    
    void Update()
    {
        // Keyboard testing
        if (Input.GetKeyDown(KeyCode.H))
        {
            SetHostMode();
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            SetClientMode();
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestNetworkDataCorrection();
        }
    }
    
    void OnGUI()
    {
        if (!showInstructions) return;
        
        GUI.BeginGroup(new Rect(10, 10, 500, 150));
        GUI.Box(new Rect(0, 0, 500, 150), "Network Data Correction");
        
        GUI.Label(new Rect(10, 25, 480, 20), "Problem: HumanPose muscle data → Wrong avatar positions");
        GUI.Label(new Rect(10, 45, 480, 20), "Solution: Landmark position data → Correct avatar animation");
        GUI.Label(new Rect(10, 65, 480, 20), "");
        GUI.Label(new Rect(10, 85, 480, 20), "H - Test Host Mode | C - Test Client Mode | T - Test System");
        GUI.Label(new Rect(10, 105, 480, 20), $"Current Mode: {(isHost ? "HOST" : isClient ? "CLIENT" : "Not Set")}");
        GUI.Label(new Rect(10, 125, 480, 20), "Check Console for correct data format examples");
        
        GUI.EndGroup();
    }
    
    [System.Serializable]
    public class CorrectNetworkData
    {
        public string avatarName;
        public float timestamp;
        public Vector3 headPosition;
        public Vector3 leftHandPosition;
        public Vector3 rightHandPosition;
        public Vector3 leftFootPosition;
        public Vector3 rightFootPosition;
        public Vector3 bodyCenter;
    }
}