using UnityEngine;

/// <summary>
/// Quick setup helper for avatar selection UI
/// Add this to an empty GameObject and press "Setup Avatar Selection" button in Inspector
/// </summary>
public class QuickAvatarSelectionSetup : MonoBehaviour
{
    [ContextMenu("Setup Avatar Selection")]
    public void SetupAvatarSelection()
    {
        Debug.Log("=== Quick Avatar Selection Setup ===");
        
        // Find or create PlayerSelectionManager
        PlayerSelectionManager manager = FindObjectOfType<PlayerSelectionManager>();
        
        if (manager == null)
        {
            GameObject managerObj = new GameObject("PlayerSelectionManager");
            manager = managerObj.AddComponent<PlayerSelectionManager>();
            Debug.Log("✅ Created PlayerSelectionManager");
        }
        else
        {
            Debug.Log("✅ Found existing PlayerSelectionManager");
        }
        
        // Auto-find and assign references
        manager.AutoFindReferences();
        
        Debug.Log("=== Setup Complete! ===");
        Debug.Log("Run the scene to see avatar selection UI");
    }
}

// Extension for PlayerSelectionManager to auto-find references
public static class PlayerSelectionManagerExtensions
{
    public static void AutoFindReferences(this PlayerSelectionManager manager)
    {
        var type = manager.GetType();
        
        // Use reflection to set private fields
        var remyField = type.GetField("remyAvatar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var adamField = type.GetField("adamAvatar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cameraRigField = type.GetField("cameraRig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cameraRigAdamField = type.GetField("cameraRigAdam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mainCameraField = type.GetField("mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tracking4AllField = type.GetField("tracking4AllObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Find avatars
        GameObject remy = GameObject.Find("Remy");
        GameObject adam = GameObject.Find("Adam_Big");
        
        if (remy != null)
        {
            remyField?.SetValue(manager, remy);
            Debug.Log($"✅ Found Remy: {remy.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ Remy avatar not found");
        }
        
        if (adam != null)
        {
            adamField?.SetValue(manager, adam);
            Debug.Log($"✅ Found Adam: {adam.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ Adam_Big avatar not found");
        }
        
        // Find camera rigs
        GameObject mainCameraRig = GameObject.Find("CameraRig");
        GameObject adamCameraRig = GameObject.Find("CameraRig_Adam");
        
        if (mainCameraRig != null)
        {
            cameraRigField?.SetValue(manager, mainCameraRig.transform);
            Debug.Log($"✅ Found CameraRig: {mainCameraRig.name}");
        }
        
        if (adamCameraRig != null)
        {
            cameraRigAdamField?.SetValue(manager, adamCameraRig);
            Debug.Log($"✅ Found CameraRig_Adam: {adamCameraRig.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ CameraRig_Adam not found - will be needed for Adam selection");
        }
        
        // Find main camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            mainCameraField?.SetValue(manager, cam);
            Debug.Log($"✅ Found Main Camera");
        }
        
        // Find tracking system
        GameObject tracking = GameObject.Find("Tracking4All");
        if (tracking == null)
        {
            // Try to find pose tracking solutions
            var poseSolutions = GameObject.FindObjectsOfType<UnityEngine.MonoBehaviour>();
            foreach (var solution in poseSolutions)
            {
                if (solution.name.Contains("Pose") && solution.name.Contains("Solution"))
                {
                    tracking = solution.gameObject;
                    break;
                }
            }
        }
        
        if (tracking != null)
        {
            tracking4AllField?.SetValue(manager, tracking);
            Debug.Log($"✅ Found Tracking System: {tracking.name}");
        }
        
        // Check pose tracking solutions
        CheckPoseTrackingSolutions();
    }
    
    private static void CheckPoseTrackingSolutions()
    {
        GameObject adamSolution = GameObject.Find("Pose Tracking Solution Adam");
        GameObject remySolution = GameObject.Find("Pose Solution Remy");
        
        if (adamSolution != null)
        {
            if (!adamSolution.activeSelf)
            {
                Debug.LogWarning("⚠️ 'Pose Tracking Solution Adam' is INACTIVE - activate it for Adam to work!");
            }
            else
            {
                Debug.Log("✅ 'Pose Tracking Solution Adam' is active");
            }
        }
        
        if (remySolution != null)
        {
            if (!remySolution.activeSelf)
            {
                Debug.LogWarning("⚠️ 'Pose Solution Remy' is INACTIVE - activate it for Remy to work!");
            }
            else
            {
                Debug.Log("✅ 'Pose Solution Remy' is active");
            }
        }
    }
}
