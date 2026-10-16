using UnityEngine;

/// <summary>
/// Simple script to ensure CameraRig_Adam starts disabled by default
/// Attach this to the CameraRig_Adam GameObject
/// </summary>
public class CameraRigAdamStartup : MonoBehaviour
{
    private void Awake()
    {
        // Ensure this GameObject starts disabled
        if (gameObject.activeInHierarchy)
        {
            Debug.Log("CameraRigAdamStartup: Disabling CameraRig_Adam at startup");
            gameObject.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        Debug.Log("CameraRigAdamStartup: CameraRig_Adam has been enabled");
    }
    
    private void OnDisable()
    {
        Debug.Log("CameraRigAdamStartup: CameraRig_Adam has been disabled");
    }
}
