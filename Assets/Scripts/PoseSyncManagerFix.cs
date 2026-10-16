using UnityEngine;

/// <summary>
/// Integration fix for PoseSyncManager to use correct bone position data
/// This fixes the floating avatar issue by replacing HumanPose.muscles with direct bone positions
/// </summary>
public class PoseSyncManagerFix : MonoBehaviour
{
    [Header("Fix Settings")]
    public bool enableFix = true;
    public bool showDebugInfo = true;
    
    private FixAvatarMovement avatarMovementFix;
    
    void Start()
    {
        // Find or create the avatar movement fix component
        avatarMovementFix = FindObjectOfType<FixAvatarMovement>();
        if (avatarMovementFix == null)
        {
            Debug.LogWarning("⚠️ FixAvatarMovement component not found. Please add it to use the correct data format.");
        }
        
        Debug.Log("🔧 PoseSyncManager Fix ready");
        Debug.Log("💡 This will help your PoseSyncManager use correct bone position data");
    }
    
    /// <summary>
    /// Call this method from PoseSyncManager.SendLocalPoseData() instead of getting HumanPose
    /// </summary>
    public string GetCorrectPoseDataForNetwork()
    {
        if (!enableFix || avatarMovementFix == null)
        {
            return "";
        }
        
        string correctData = avatarMovementFix.GetCorrectAvatarData();
        
        if (showDebugInfo && !string.IsNullOrEmpty(correctData))
        {
            Debug.Log($"🔄 PROVIDING CORRECT DATA to PoseSyncManager: {correctData.Length} chars");
        }
        
        return correctData;
    }
    
    /// <summary>
    /// Call this method from PoseSyncManager.ApplyBufferedPoseData() instead of SetHumanPose
    /// </summary>
    public void ApplyCorrectPoseDataFromNetwork(string jsonData)
    {
        if (!enableFix || avatarMovementFix == null || string.IsNullOrEmpty(jsonData))
        {
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"🔄 APPLYING CORRECT DATA from network: {jsonData.Length} chars");
        }
        
        avatarMovementFix.ApplyCorrectAvatarData(jsonData);
    }
    
    /// <summary>
    /// Set the role for the avatar movement fix component
    /// </summary>
    public void SetRole(bool isHost)
    {
        if (avatarMovementFix != null)
        {
            if (isHost)
            {
                avatarMovementFix.SetHost();
            }
            else
            {
                avatarMovementFix.SetClient();
            }
        }
        
        Debug.Log($"🎮 Role set: {(isHost ? "HOST" : "CLIENT")}");
    }
}