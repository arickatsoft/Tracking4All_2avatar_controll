using UnityEngine;

public class CameraRigForceActivator : MonoBehaviour
{
    [Header("Force Activate CameraRig_Adam")]
    [Tooltip("This script forcefully enables CameraRig_Adam even when disabled in hierarchy")]
    public bool enableDebugLogging = true;
    
    void Start()
    {
        Debug.Log("🔧 CameraRigForceActivator started");
        
        // Find and log initial state
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        if (cameraRigAdam != null)
        {
            Debug.Log($"📷 Found CameraRig_Adam:");
            Debug.Log($"   - activeSelf: {cameraRigAdam.activeSelf}");
            Debug.Log($"   - activeInHierarchy: {cameraRigAdam.activeInHierarchy}");
            Debug.Log($"   - Position: {cameraRigAdam.transform.position}");
        }
        else
        {
            Debug.LogError("❌ CameraRig_Adam not found!");
        }
    }
    
    public void ForceEnableCameraRigAdam()
    {
        Debug.Log("🚀 === FORCE ENABLING CAMERARIG_ADAM ===");
        
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        GameObject mainCamera = GameObject.Find("Main Camera");
        
        if (cameraRigAdam == null)
        {
            Debug.LogError("❌ CameraRig_Adam not found!");
            return;
        }
        
        // Log current state
        Debug.Log($"📷 CameraRig_Adam current state:");
        Debug.Log($"   - activeSelf: {cameraRigAdam.activeSelf}");
        Debug.Log($"   - activeInHierarchy: {cameraRigAdam.activeInHierarchy}");
        
        // Force enable it multiple ways
        if (!cameraRigAdam.activeSelf)
        {
            Debug.Log("🔧 CameraRig_Adam activeSelf is FALSE - forcing to TRUE");
            cameraRigAdam.SetActive(true);
        }
        
        // Double check and try again if needed
        if (!cameraRigAdam.activeInHierarchy)
        {
            Debug.Log("🔧 CameraRig_Adam still not active in hierarchy - trying again");
            cameraRigAdam.SetActive(false); // Force off first
            cameraRigAdam.SetActive(true);  // Then force on
        }
        
        // Verify final state
        Debug.Log($"📷 CameraRig_Adam after force enable:");
        Debug.Log($"   - activeSelf: {cameraRigAdam.activeSelf}");
        Debug.Log($"   - activeInHierarchy: {cameraRigAdam.activeInHierarchy}");
        
        // Also disable main camera
        if (mainCamera != null)
        {
            mainCamera.SetActive(false);
            Debug.Log("📷 Main Camera disabled");
        }
        
        // Check camera component
        Camera cam = cameraRigAdam.GetComponentInChildren<Camera>();
        if (cam != null)
        {
            Debug.Log($"✅ Camera component found: {cam.gameObject.name}");
            Debug.Log($"   - Camera enabled: {cam.enabled}");
            Debug.Log($"   - Camera gameObject active: {cam.gameObject.activeInHierarchy}");
            
            // Force enable the camera component too
            if (!cam.enabled)
            {
                cam.enabled = true;
                Debug.Log("🔧 Camera component was disabled - force enabled");
            }
        }
        else
        {
            Debug.LogError("❌ No Camera component found in CameraRig_Adam children!");
        }
        
        // Set player tags
        SetPlayerTags();
        
        Debug.Log("🚀 === FORCE ENABLE COMPLETE ===");
        
        // Final verification
        StartCoroutine(VerifyAfterFrame());
    }
    
    System.Collections.IEnumerator VerifyAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        
        Debug.Log("🔍 === FINAL VERIFICATION ===");
        
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        GameObject mainCamera = GameObject.Find("Main Camera");
        
        if (cameraRigAdam != null)
        {
            Debug.Log($"✅ CameraRig_Adam final state: {(cameraRigAdam.activeInHierarchy ? "ACTIVE" : "INACTIVE")}");
        }
        
        if (mainCamera != null)
        {
            Debug.Log($"✅ Main Camera final state: {(mainCamera.activeInHierarchy ? "ACTIVE" : "INACTIVE")}");
        }
        
        // Check which cameras are actually rendering
        Camera[] allCameras = FindObjectsOfType<Camera>();
        Debug.Log($"🎥 Active cameras:");
        foreach (Camera cam in allCameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                Debug.Log($"   - {cam.gameObject.name} (Parent: {cam.transform.parent?.name ?? "Root"})");
            }
        }
    }
    
    void SetPlayerTags()
    {
        GameObject adam = GameObject.Find("Adam_Big");
        GameObject remy = GameObject.Find("Remy");
        
        if (adam != null)
        {
            adam.tag = "Player";
            Debug.Log("✅ Adam tagged as Player");
        }
        
        if (remy != null)
        {
            remy.tag = "Enemy";
            Debug.Log("✅ Remy tagged as Enemy");
        }
    }
    
    // Keyboard shortcut for testing
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("🎹 'F' Key pressed - Force enabling CameraRig_Adam");
            ForceEnableCameraRigAdam();
        }
    }
    
    // Method to be called by buttons
    public void OnAdamButtonPressed()
    {
        Debug.Log("🔥 Adam Button Pressed - Force Enabling Camera");
        ForceEnableCameraRigAdam();
    }
}
