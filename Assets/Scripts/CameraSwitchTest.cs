using UnityEngine;

public class CameraSwitchTest : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject cameraRigAdam;
    
    [Header("Player References")]
    [SerializeField] private GameObject remy;
    [SerializeField] private GameObject adam;
    
    private bool isUsingAdamCamera = false;
    
    void Start()
    {
        // Find objects if not assigned
        if (mainCamera == null)
            mainCamera = GameObject.Find("Main Camera");
        
        if (cameraRigAdam == null)
            cameraRigAdam = GameObject.Find("CameraRig_Adam");
            
        if (remy == null)
            remy = GameObject.Find("Remy");
            
        if (adam == null)
            adam = GameObject.Find("Adam_Big");
        
        // Initial state - Remy camera
        SetRemyCamera();
        
        Debug.Log("CameraSwitchTest initialized. Press 'A' to switch to Adam camera, 'R' for Remy camera");
    }
    
    void Update()
    {
        // Test controls
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("A pressed - Switching to Adam camera");
            SetAdamCamera();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R pressed - Switching to Remy camera");
            SetRemyCamera();
        }
    }
    
    public void SetAdamCamera()
    {
        Debug.Log("=== Setting Adam Camera ===");
        
        // Disable main camera
        if (mainCamera != null)
        {
            mainCamera.SetActive(false);
            Debug.Log("Main camera disabled");
        }
        else
        {
            Debug.LogError("Main camera not found!");
        }
        
        // Enable Adam camera
        if (cameraRigAdam != null)
        {
            cameraRigAdam.SetActive(true);
            Debug.Log("CameraRig_Adam enabled");
            Debug.Log($"CameraRig_Adam active state: {cameraRigAdam.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("CameraRig_Adam not found!");
        }
        
        // Set player tags
        if (adam != null)
        {
            adam.tag = "Player";
            Debug.Log("Adam set as Player");
        }
        
        if (remy != null)
        {
            remy.tag = "Enemy";
            Debug.Log("Remy set as Enemy");
        }
        
        isUsingAdamCamera = true;
        Debug.Log("=== Adam Camera Setup Complete ===");
    }
    
    public void SetRemyCamera()
    {
        Debug.Log("=== Setting Remy Camera ===");
        
        // Enable main camera
        if (mainCamera != null)
        {
            mainCamera.SetActive(true);
            Debug.Log("Main camera enabled");
        }
        else
        {
            Debug.LogError("Main camera not found!");
        }
        
        // Disable Adam camera
        if (cameraRigAdam != null)
        {
            cameraRigAdam.SetActive(false);
            Debug.Log("CameraRig_Adam disabled");
        }
        else
        {
            Debug.LogError("CameraRig_Adam not found!");
        }
        
        // Set player tags
        if (remy != null)
        {
            remy.tag = "Player";
            Debug.Log("Remy set as Player");
        }
        
        if (adam != null)
        {
            adam.tag = "Enemy";
            Debug.Log("Adam set as Enemy");
        }
        
        isUsingAdamCamera = false;
        Debug.Log("=== Remy Camera Setup Complete ===");
    }
    
    // Public methods that can be called by UI buttons
    public void OnAdamButtonPressed()
    {
        SetAdamCamera();
    }
    
    public void OnRemyButtonPressed()
    {
        SetRemyCamera();
    }
}
