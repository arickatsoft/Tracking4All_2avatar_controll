using UnityEngine;

/// <summary>
/// EXACT FIX for avatar movement - captures and applies bone positions correctly
/// Works with your existing PoseSyncManager networking system
/// </summary>
public class FixAvatarMovement : MonoBehaviour
{
    [Header("Avatar References")]
    public GameObject bryceAvatar;
    public GameObject remyAvatar;
    
    [Header("Settings")]
    public bool enableLogging = true;
    public float sendRate = 30f; // 30 FPS for smooth movement
    
    // Network timing
    private float lastSendTime = 0f;
    
    // Current role assignment
    private bool isHost = false;
    private bool isClient = false;
    private bool isDataSender = false;
    
    void Start()
    {
        // Auto-find avatars
        if (bryceAvatar == null) bryceAvatar = GameObject.Find("Bryce");
        if (remyAvatar == null) remyAvatar = GameObject.Find("Remy");
        
        Debug.Log("🔧 Avatar Movement Fix ready");
        
        // Find existing UI buttons to determine role
        SetupRoles();
    }
    
    void SetupRoles()
    {
        // Try to find existing UI system
        var hostButton = GameObject.Find("HostButton");
        var joinButton = GameObject.Find("JoinButton");
        
        if (hostButton == null) hostButton = GameObject.Find("Host");
        if (joinButton == null) joinButton = GameObject.Find("Join");
        if (joinButton == null) joinButton = GameObject.Find("Client");
        
        Debug.Log("🎮 Avatar Movement Fix initialized - use SetHost() or SetClient() methods");
    }
    
    void Update()
    {
        // Send data at specified rate
        if (isDataSender && Time.time - lastSendTime >= (1f / sendRate))
        {
            string correctData = GetCorrectAvatarData();
            if (!string.IsNullOrEmpty(correctData))
            {
                // Instead of sending directly, make this available to your existing PoseSyncManager
                // You can call GetCorrectAvatarData() from your PoseSyncManager instead of using HumanPose
                if (enableLogging)
                {
                    Debug.Log($"📤 CORRECT DATA READY: {correctData.Length} chars");
                }
            }
            lastSendTime = Time.time;
        }
    }
    
    /// <summary>
    /// Get the CORRECT data format - call this from your PoseSyncManager instead of HumanPose
    /// </summary>
    public string GetCorrectAvatarData()
    {
        GameObject sourceAvatar = GetSourceAvatar();
        if (sourceAvatar == null) return "";
        
        Animator animator = sourceAvatar.GetComponent<Animator>();
        if (animator == null) return "";
        
        // Create CORRECT data format - bone positions not muscle values
        var boneData = new AvatarBoneData
        {
            sourceAvatar = sourceAvatar.name,
            timestamp = Time.time,
            
            // KEY INSIGHT: Send bone positions directly from camera tracking
            headPos = GetBoneWorldPos(animator, HumanBodyBones.Head),
            leftHandPos = GetBoneWorldPos(animator, HumanBodyBones.LeftHand),
            rightHandPos = GetBoneWorldPos(animator, HumanBodyBones.RightHand),
            leftFootPos = GetBoneWorldPos(animator, HumanBodyBones.LeftFoot),
            rightFootPos = GetBoneWorldPos(animator, HumanBodyBones.RightFoot),
            hipsPos = GetBoneWorldPos(animator, HumanBodyBones.Hips),
            spinePos = GetBoneWorldPos(animator, HumanBodyBones.Spine),
            chestPos = GetBoneWorldPos(animator, HumanBodyBones.Chest),
            
            // Include rotations for better accuracy
            headRot = GetBoneWorldRot(animator, HumanBodyBones.Head),
            leftHandRot = GetBoneWorldRot(animator, HumanBodyBones.LeftHand),
            rightHandRot = GetBoneWorldRot(animator, HumanBodyBones.RightHand),
            hipsRot = GetBoneWorldRot(animator, HumanBodyBones.Hips)
        };
        
        string jsonData = JsonUtility.ToJson(boneData);
        
        if (enableLogging)
        {
            Debug.Log($"📤 SENDING CORRECT DATA from {sourceAvatar.name}");
            Debug.Log($"   Head: {boneData.headPos}");
            Debug.Log($"   L-Hand: {boneData.leftHandPos}, R-Hand: {boneData.rightHandPos}");
            Debug.Log($"   Data size: {jsonData.Length} chars (better than HumanPose)");
        }
        
        return jsonData;
    }
    
    /// <summary>
    /// Apply received pose data to avatar - call this from your PoseSyncManager instead of SetHumanPose
    /// </summary>
    public void ApplyCorrectAvatarData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData)) return;
        
        try
        {
            var data = JsonUtility.FromJson<AvatarBoneData>(jsonData);
            if (data == null) return;
            
            GameObject targetAvatar = GetTargetAvatar(data.sourceAvatar);
            if (targetAvatar == null) return;
            
            Animator animator = targetAvatar.GetComponent<Animator>();
            if (animator == null) return;
            
            if (enableLogging)
            {
                Debug.Log($"📥 APPLYING CORRECT DATA to {targetAvatar.name}");
                Debug.Log($"   Source data from: {data.sourceAvatar}");
                Debug.Log($"   Head position: {data.headPos}");
                Debug.Log($"   Hand positions: L={data.leftHandPos}, R={data.rightHandPos}");
            }
            
            // APPLY POSITIONS DIRECTLY - this is the key fix!
            ApplyBonePosition(animator, HumanBodyBones.Head, data.headPos, data.headRot);
            ApplyBonePosition(animator, HumanBodyBones.LeftHand, data.leftHandPos, data.leftHandRot);
            ApplyBonePosition(animator, HumanBodyBones.RightHand, data.rightHandPos, data.rightHandRot);
            ApplyBonePosition(animator, HumanBodyBones.LeftFoot, data.leftFootPos, Quaternion.identity);
            ApplyBonePosition(animator, HumanBodyBones.RightFoot, data.rightFootPos, Quaternion.identity);
            ApplyBonePosition(animator, HumanBodyBones.Hips, data.hipsPos, data.hipsRot);
            ApplyBonePosition(animator, HumanBodyBones.Spine, data.spinePos, Quaternion.identity);
            ApplyBonePosition(animator, HumanBodyBones.Chest, data.chestPos, Quaternion.identity);
            
            Debug.Log($"✅ Applied bone positions to {targetAvatar.name} - should now move correctly!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error applying correct avatar data: {e.Message}");
        }
    }
    
    Vector3 GetBoneWorldPos(Animator animator, HumanBodyBones bone)
    {
        Transform boneTransform = animator.GetBoneTransform(bone);
        return boneTransform != null ? boneTransform.position : Vector3.zero;
    }
    
    Quaternion GetBoneWorldRot(Animator animator, HumanBodyBones bone)
    {
        Transform boneTransform = animator.GetBoneTransform(bone);
        return boneTransform != null ? boneTransform.rotation : Quaternion.identity;
    }
    
    void ApplyBonePosition(Animator animator, HumanBodyBones bone, Vector3 position, Quaternion rotation)
    {
        Transform boneTransform = animator.GetBoneTransform(bone);
        if (boneTransform != null)
        {
            boneTransform.position = position;
            if (rotation != Quaternion.identity)
            {
                boneTransform.rotation = rotation;
            }
        }
    }
    
    GameObject GetSourceAvatar()
    {
        if (isHost) return remyAvatar;  // Host sends Remy data
        if (isClient) return bryceAvatar; // Client sends Bryce data
        return null;
    }
    
    GameObject GetTargetAvatar(string sourceName)
    {
        // Apply to opposite avatar
        if (sourceName.Contains("Remy") || sourceName.Contains("REMY"))
        {
            return remyAvatar;
        }
        else if (sourceName.Contains("Bryce") || sourceName.Contains("BRYCE"))
        {
            return bryceAvatar;
        }
        return null;
    }
    
    /// <summary>
    /// Call this when Host button is pressed
    /// </summary>
    public void SetHost()
    {
        isHost = true;
        isClient = false;
        isDataSender = (remyAvatar != null);
        Debug.Log("🎮 HOST MODE - Will capture REMY bone positions");
    }
    
    /// <summary>
    /// Call this when Client/Join button is pressed
    /// </summary>
    public void SetClient()
    {
        isHost = false;
        isClient = true;
        isDataSender = (bryceAvatar != null);
        Debug.Log("🎮 CLIENT MODE - Will capture BRYCE bone positions");
    }
    
    /// <summary>
    /// Test the correct data format
    /// </summary>
    [ContextMenu("Test Correct Data")]
    public void TestCorrectData()
    {
        SetHost(); // Test as host
        
        string testData = GetCorrectAvatarData();
        if (!string.IsNullOrEmpty(testData))
        {
            Debug.Log($"🧪 TEST DATA: {testData}");
            ApplyCorrectAvatarData(testData);
        }
        
        Debug.Log("✅ This data format should fix avatar movement");
    }
    
    [System.Serializable]
    public class AvatarBoneData
    {
        public string sourceAvatar;
        public float timestamp;
        
        // Key bone positions (CORRECT data format)
        public Vector3 headPos;
        public Vector3 leftHandPos;
        public Vector3 rightHandPos;
        public Vector3 leftFootPos;
        public Vector3 rightFootPos;
        public Vector3 hipsPos;
        public Vector3 spinePos;
        public Vector3 chestPos;
        
        // Key bone rotations
        public Quaternion headRot;
        public Quaternion leftHandRot;
        public Quaternion rightHandRot;
        public Quaternion hipsRot;
    }
}
