using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Camera Rigs")]
    [SerializeField] private GameObject mainCameraRig;
    [SerializeField] private GameObject cameraRigAdam;
    
    [Header("Player References")]
    [SerializeField] private GameObject remyAvatar;
    [SerializeField] private GameObject adamAvatar;
    
    [Header("Tracking System")]
    [SerializeField] private TrackingTargetManager trackingTargetManager;
    //[SerializeField] private AdamPoseTrackingManager adamPoseTrackingManager;
    
    private void Start()
    {
        Debug.Log("CameraManager: Starting initialization");
        
        // Find cameras if not assigned
        if (mainCameraRig == null)
        {
            // Try to find main camera or camera rig
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCameraRig = mainCam.gameObject;
                Debug.Log($"CameraManager: Found main camera: {mainCam.name}");
            }
        }
        
        if (cameraRigAdam == null)
        {
            cameraRigAdam = GameObject.Find("CameraRig_Adam");
            if (cameraRigAdam != null)
            {
                Debug.Log("CameraManager: Found CameraRig_Adam");
            }
        }
        
        // Find avatars if not assigned
        if (remyAvatar == null)
        {
            remyAvatar = GameObject.Find("Remy");
            if (remyAvatar != null)
            {
                Debug.Log("CameraManager: Found Remy avatar");
            }
        }
        
        if (adamAvatar == null)
        {
            adamAvatar = GameObject.Find("Adam_Big");
            if (adamAvatar != null)
            {
                Debug.Log("CameraManager: Found Adam_Big avatar");
            }
        }
        
        // Find tracking target manager if not assigned
        if (trackingTargetManager == null)
        {
            trackingTargetManager = FindObjectOfType<TrackingTargetManager>();
            if (trackingTargetManager != null)
            {
                Debug.Log("CameraManager: Found TrackingTargetManager");
            }
            else
            {
                Debug.LogWarning("CameraManager: TrackingTargetManager not found!");
            }
        }
        
        // Initialize with default state (Remy selected, CameraRig_Adam disabled)
        SetRemyView();
        Debug.Log("CameraManager: Initialization complete - Remy view set as default");
    }
    
    public void SetRemyView()
    {
        Debug.Log("CameraManager: Setting Remy view - Main camera active, CameraRig_Adam disabled");
        
        // Enable main camera, disable Adam camera
        if (mainCameraRig != null)
        {
            mainCameraRig.SetActive(true);
            Debug.Log("CameraManager: Main camera enabled");
        }
        else
        {
            Debug.LogWarning("CameraManager: Main camera rig is null!");
        }
        
        if (cameraRigAdam != null)
        {
            cameraRigAdam.SetActive(false);
            Debug.Log("CameraManager: CameraRig_Adam disabled");
        }
        else
        {
            Debug.LogWarning("CameraManager: CameraRig_Adam is null!");
        }
        
        // Set player tags
        if (remyAvatar != null)
        {
            remyAvatar.tag = "Player";
            Debug.Log("CameraManager: Remy tagged as Player");
        }
        if (adamAvatar != null)
        {
            adamAvatar.tag = "Enemy";
            Debug.Log("CameraManager: Adam tagged as Enemy");
        }
        
        // Setup tracking for Remy
        if (trackingTargetManager != null)
        {
            trackingTargetManager.OnRemySelected();
        }
        else
        {
            Debug.LogWarning("CameraManager: TrackingTargetManager not available for Remy setup");
        }
    }
    
    public void SetAdamView()
    {
        Debug.Log("CameraManager: Setting Adam view - CameraRig_Adam active, Main camera disabled");
        
        // Disable main camera, enable Adam camera
        if (mainCameraRig != null)
        {
            mainCameraRig.SetActive(false);
            Debug.Log("CameraManager: Main camera disabled");
        }
        else
        {
            Debug.LogWarning("CameraManager: Main camera rig is null!");
        }
        
        if (cameraRigAdam != null)
        {
            cameraRigAdam.SetActive(true);
            Debug.Log("CameraManager: CameraRig_Adam enabled");
        }
        else
        {
            Debug.LogWarning("CameraManager: CameraRig_Adam is null!");
        }
        
        // Set player tags
        if (adamAvatar != null)
        {
            adamAvatar.tag = "Player";
            Debug.Log("CameraManager: Adam tagged as Player");
        }
        if (remyAvatar != null)
        {
            remyAvatar.tag = "Enemy";
            Debug.Log("CameraManager: Remy tagged as Enemy");
        }
        
        // Setup tracking for Adam
        if (trackingTargetManager != null)
        {
            trackingTargetManager.OnAdamSelected();
        }
        else
        {
            Debug.LogWarning("CameraManager: TrackingTargetManager not available for Adam setup");
        }
    }
    
    // Method to be called by UI buttons
    public void OnAdamButtonPressed()
    {
        Debug.Log("CameraManager: Adam button pressed");
        SetAdamView();
    }
    
    public void OnRemyButtonPressed()
    {
        Debug.Log("CameraManager: Remy button pressed");
        SetRemyView();
    }
    
    // Public method to get current tracking target manager
    public TrackingTargetManager GetTrackingTargetManager()
    {
        return trackingTargetManager;
    }
}
