using UnityEngine;

public class CameraShakeFix : MonoBehaviour
{
    [Header("Camera References")]
    public Transform cameraRig;
    
    [Header("Reset")]
    public KeyCode resetKey = KeyCode.R;
    
    private Vector3 originalPosition;
    private bool positionSaved = false;
    
    void Start()
    {
        // Find camera rig if not assigned
        if (cameraRig == null)
        {
            GameObject cameraRigObj = GameObject.Find("CameraRig");
            if (cameraRigObj != null)
            {
                cameraRig = cameraRigObj.transform;
            }
        }
        
        // Save the original position
        if (cameraRig != null)
        {
            originalPosition = cameraRig.localPosition;
            positionSaved = true;
            Debug.Log($"CameraShakeFix: Saved original camera position: {originalPosition}");
        }
    }
    
    void Update()
    {
        // Press R to reset camera position
        if (Input.GetKeyDown(resetKey) && cameraRig != null && positionSaved)
        {
            Debug.Log("Resetting camera to original position");
            ResetCameraPosition();
        }
    }
    
    public void ResetCameraPosition()
    {
        if (cameraRig != null && positionSaved)
        {
            cameraRig.localPosition = originalPosition;
            
            // Stop any ongoing HitFeedback effects
            HitFeedback hitFeedback = FindObjectOfType<HitFeedback>();
            if (hitFeedback != null)
            {
                hitFeedback.StopAllCoroutines();
            }
            
            Debug.Log($"Camera position reset to: {originalPosition}");
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 220, 350, 100), "CAMERA SHAKE FIX");
        
        GUI.Label(new Rect(20, 245, 320, 20), $"Original Pos: {originalPosition}");
        if (cameraRig != null)
        {
            GUI.Label(new Rect(20, 265, 320, 20), $"Current Pos: {cameraRig.localPosition}");
        }
        GUI.Label(new Rect(20, 285, 320, 20), $"Press {resetKey} to reset camera");
        
        if (GUI.Button(new Rect(20, 300, 100, 25), "Reset Now"))
        {
            ResetCameraPosition();
        }
    }
}