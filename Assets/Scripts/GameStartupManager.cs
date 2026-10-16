using UnityEngine;

/// <summary>
/// Main startup script that ensures proper initialization of camera rigs and player selection UI
/// This should be attached to a GameObject that's active at startup (like GameManager or similar)
/// </summary>
public class GameStartupManager : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private PlayerSelectionUI playerSelectionUI;
    [SerializeField] private GameObject cameraRigAdam;
    [SerializeField] private GameObject mainCamera;
    
    private void Awake()
    {
        Debug.Log("GameStartupManager: Initializing game startup...");
        
        // Find required components if not assigned
        FindRequiredComponents();
        
        // Ensure CameraRig_Adam starts disabled
        EnsureCameraRigAdamDisabled();
        
        // Ensure main camera is active
        EnsureMainCameraActive();
        
        Debug.Log("GameStartupManager: Initialization complete");
    }
    
    private void FindRequiredComponents()
    {
        if (cameraManager == null)
        {
            cameraManager = FindObjectOfType<CameraManager>();
            if (cameraManager != null)
            {
                Debug.Log("GameStartupManager: Found CameraManager");
            }
            else
            {
                Debug.LogWarning("GameStartupManager: CameraManager not found");
            }
        }
        
        if (playerSelectionUI == null)
        {
            playerSelectionUI = FindObjectOfType<PlayerSelectionUI>();
            if (playerSelectionUI != null)
            {
                Debug.Log("GameStartupManager: Found PlayerSelectionUI");
            }
            else
            {
                Debug.LogWarning("GameStartupManager: PlayerSelectionUI not found");
            }
        }
        
        if (cameraRigAdam == null)
        {
            cameraRigAdam = GameObject.Find("CameraRig_Adam");
            if (cameraRigAdam != null)
            {
                Debug.Log("GameStartupManager: Found CameraRig_Adam");
            }
            else
            {
                Debug.LogWarning("GameStartupManager: CameraRig_Adam not found");
            }
        }
        
        if (mainCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                mainCamera = cam.gameObject;
                Debug.Log($"GameStartupManager: Found main camera: {mainCamera.name}");
            }
            else
            {
                Debug.LogWarning("GameStartupManager: Main camera not found");
            }
        }
    }
    
    private void EnsureCameraRigAdamDisabled()
    {
        if (cameraRigAdam != null && cameraRigAdam.activeInHierarchy)
        {
            Debug.Log("GameStartupManager: Disabling CameraRig_Adam (it should start disabled)");
            cameraRigAdam.SetActive(false);
        }
        else if (cameraRigAdam != null)
        {
            Debug.Log("GameStartupManager: CameraRig_Adam is already properly disabled");
        }
    }
    
    private void EnsureMainCameraActive()
    {
        if (mainCamera != null && !mainCamera.activeInHierarchy)
        {
            Debug.Log("GameStartupManager: Enabling main camera");
            mainCamera.SetActive(true);
        }
        else if (mainCamera != null)
        {
            Debug.Log("GameStartupManager: Main camera is properly active");
        }
    }
    
    // Method to be called when Adam button is pressed (alternative to CameraManager)
    public void OnAdamButtonPressed()
    {
        Debug.Log("GameStartupManager: Adam button pressed - enabling CameraRig_Adam");
        
        if (cameraManager != null)
        {
            cameraManager.OnAdamButtonPressed();
        }
        else
        {
            // Fallback if CameraManager is not available
            if (mainCamera != null)
            {
                mainCamera.SetActive(false);
            }
            
            if (cameraRigAdam != null)
            {
                cameraRigAdam.SetActive(true);
            }
            
            Debug.Log("GameStartupManager: Direct camera switch completed");
        }
    }
    
    // Method to be called when Remy button is pressed (alternative to CameraManager)
    public void OnRemyButtonPressed()
    {
        Debug.Log("GameStartupManager: Remy button pressed - disabling CameraRig_Adam");
        
        if (cameraManager != null)
        {
            cameraManager.OnRemyButtonPressed();
        }
        else
        {
            // Fallback if CameraManager is not available
            if (cameraRigAdam != null)
            {
                cameraRigAdam.SetActive(false);
            }
            
            if (mainCamera != null)
            {
                mainCamera.SetActive(true);
            }
            
            Debug.Log("GameStartupManager: Direct camera switch completed");
        }
    }
}
