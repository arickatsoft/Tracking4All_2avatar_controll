using UnityEngine;

public class AdamButtonHandler : MonoBehaviour
{
    [Header("Debug Info")]
    [Tooltip("Attach this script directly to the Adam button GameObject")]
    public bool enableDebugLogging = true;
    
    [Header("Mirror Settings")]
    [Tooltip("Enable mirror mode when Adam is selected (he faces opposite to Remy)")]
    public bool enableMirrorForAdam = true;
    
    private AdamTrackingMirrorController mirrorController;
    
    void Start()
    {
        Debug.Log($"🎯 AdamButtonHandler attached to: {gameObject.name}");
        Debug.Log($"🎯 GameObject active: {gameObject.activeInHierarchy}");
        
        // Find or create the mirror controller
        mirrorController = FindObjectOfType<AdamTrackingMirrorController>();
        if (mirrorController == null)
        {
            // Create a GameObject to hold the mirror controller
            GameObject mirrorControllerObj = new GameObject("AdamMirrorController");
            mirrorController = mirrorControllerObj.AddComponent<AdamTrackingMirrorController>();
            Debug.Log("🪞 Created AdamTrackingMirrorController");
        }
        
        // Check if this is actually a button
        UnityEngine.UI.Button button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            Debug.Log("✅ Button component found!");
            Debug.Log($"✅ Button interactable: {button.interactable}");
            
            // Clear existing listeners and add ours
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnAdamButtonClicked);
            
            Debug.Log("✅ Adam button listener added successfully!");
        }
        else
        {
            Debug.LogError("❌ No Button component found! This script should be attached to a Button.");
        }
    }
    
    public void OnAdamButtonClicked()
    {
        Debug.Log("🔥🔥🔥 ADAM BUTTON CLICKED! 🔥🔥🔥");
        Debug.Log($"Time: {System.DateTime.Now:HH:mm:ss.fff}");
        Debug.Log($"Button: {gameObject.name}");
        
        SwitchToAdamCamera();
    }
    
    void SwitchToAdamCamera()
    {
        Debug.Log("🎬 === SWITCHING TO ADAM CAMERA === 🎬");
        
        // Find cameras
        GameObject mainCamera = GameObject.Find("Main Camera");
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        
        if (mainCamera == null)
        {
            Debug.LogError("❌ Main Camera not found!");
        }
        else
        {
            Debug.Log($"📷 Main Camera found: {mainCamera.name}");
        }
        
        if (cameraRigAdam == null)
        {
            Debug.LogError("❌ CameraRig_Adam not found!");
            return;
        }
        else
        {
            Debug.Log($"📷 CameraRig_Adam found: {cameraRigAdam.name}");
            Debug.Log($"📷 CameraRig_Adam current activeSelf: {cameraRigAdam.activeSelf}");
            Debug.Log($"📷 CameraRig_Adam current activeInHierarchy: {cameraRigAdam.activeInHierarchy}");
        }
        
        // Disable main camera first
        if (mainCamera != null)
        {
            bool wasActive = mainCamera.activeInHierarchy;
            mainCamera.SetActive(false);
            Debug.Log($"📷 Main Camera disabled (was {(wasActive ? "active" : "inactive")})");
        }
        
        // FORCE enable Adam camera - handle disabled state properly
        Debug.Log("🔧 Force enabling CameraRig_Adam...");
        
        // Method 1: Direct activation
        cameraRigAdam.SetActive(true);
        Debug.Log($"📷 After SetActive(true) - activeInHierarchy: {cameraRigAdam.activeInHierarchy}");
        
        // Method 2: If still not active, try toggle approach
        if (!cameraRigAdam.activeInHierarchy)
        {
            Debug.Log("🔧 Direct activation failed, trying toggle approach...");
            cameraRigAdam.SetActive(false);
            System.Threading.Thread.Sleep(10); // Brief pause
            cameraRigAdam.SetActive(true);
            Debug.Log($"📷 After toggle - activeInHierarchy: {cameraRigAdam.activeInHierarchy}");
        }
        
        // Method 3: Check parent hierarchy
        if (!cameraRigAdam.activeInHierarchy)
        {
            Debug.Log("🔧 Checking parent hierarchy...");
            Transform parent = cameraRigAdam.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"⚠️ Parent '{parent.name}' is inactive!");
                }
                parent = parent.parent;
            }
        }
        
        // Verify activation success
        if (cameraRigAdam.activeInHierarchy)
        {
            Debug.Log("✅ CameraRig_Adam is now ACTIVE!");
        }
        else
        {
            Debug.LogError("❌ CameraRig_Adam failed to activate!");
            Debug.LogError("❌ This might be because it was manually disabled in the Inspector");
            Debug.LogError("❌ Try manually checking the checkbox next to CameraRig_Adam in hierarchy");
        }
        
        // Check for camera component and force enable it
        Camera cam = cameraRigAdam.GetComponentInChildren<Camera>();
        if (cam != null)
        {
            Debug.Log($"✅ Found camera in children: {cam.gameObject.name}");
            Debug.Log($"✅ Camera enabled: {cam.enabled}");
            Debug.Log($"✅ Camera gameObject active: {cam.gameObject.activeInHierarchy}");
            Debug.Log($"✅ Camera position: {cam.transform.position}");
            
            // Force enable camera component if disabled
            if (!cam.enabled)
            {
                cam.enabled = true;
                Debug.Log("🔧 Camera component was disabled - force enabled");
            }
            
            // Force enable camera GameObject if disabled
            if (!cam.gameObject.activeInHierarchy)
            {
                cam.gameObject.SetActive(true);
                Debug.Log("🔧 Camera GameObject was inactive - force enabled");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No Camera component found in CameraRig_Adam children");
        }
        
        // Update player tags
        GameObject adam = GameObject.Find("Adam_Big");
        GameObject remy = GameObject.Find("Remy");
        
        if (adam != null)
        {
            string oldTag = adam.tag;
            adam.tag = "Player";
            Debug.Log($"👤 Adam tag changed: {oldTag} → {adam.tag}");
        }
        else
        {
            Debug.LogWarning("⚠️ Adam_Big not found for tag update");
        }
        
        if (remy != null)
        {
            string oldTag = remy.tag;
            remy.tag = "Enemy";
            Debug.Log($"👤 Remy tag changed: {oldTag} → {remy.tag}");
        }
        else
        {
            Debug.LogWarning("⚠️ Remy not found for tag update");
        }
        
        // Enable mirror mode for Adam since he faces opposite direction to Remy
        if (enableMirrorForAdam && mirrorController != null)
        {
            Debug.Log("🪞 === ENABLING MIRROR MODE FOR ADAM === 🪞");
            mirrorController.EnableMirrorForAdam();
            Debug.Log("🪞 Mirror mode enabled - Adam's tracking will be mirrored");
        }
        
        Debug.Log("🎬 === ADAM CAMERA SWITCH COMPLETE === 🎬");
        
        // Final verification
        VerifySwitch();
    }
    
    void VerifySwitch()
    {
        Debug.Log("🔍 === VERIFYING CAMERA SWITCH === 🔍");
        
        GameObject mainCamera = GameObject.Find("Main Camera");
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        
        if (mainCamera != null)
        {
            Debug.Log($"📷 Main Camera active: {mainCamera.activeInHierarchy}");
        }
        
        if (cameraRigAdam != null)
        {
            Debug.Log($"📷 CameraRig_Adam active: {cameraRigAdam.activeInHierarchy}");
        }
        
        // Check which camera is currently rendering
        Camera[] allCameras = FindObjectsOfType<Camera>();
        Debug.Log($"🎥 Total cameras found: {allCameras.Length}");
        
        foreach (Camera cam in allCameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                Debug.Log($"🎥 ACTIVE CAMERA: {cam.gameObject.name} (Parent: {cam.transform.parent?.name ?? "None"})");
            }
        }
    }
}
