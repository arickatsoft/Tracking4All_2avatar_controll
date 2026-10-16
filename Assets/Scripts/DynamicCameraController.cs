using UnityEngine;

public class DynamicCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float rotationSpeed = 3f;
    
    [Header("Player References")]
    [SerializeField] private Transform remyTransform;
    [SerializeField] private Transform adamTransform;
    
    [Header("Camera Positions")]
    [SerializeField] private Vector3 remyOffset = new Vector3(0.3f, 1.5f, -2.5f);
    [SerializeField] private Vector3 adamOffset = new Vector3(0f, 1.8f, 3.5f);
    [SerializeField] private float lookAtHeight = 1.2f;
    
    private bool isFollowingAdam = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
            
        if (remyTransform == null)
        {
            GameObject remy = GameObject.Find("Remy");
            if (remy != null)
                remyTransform = remy.transform;
        }
        
        if (adamTransform == null)
        {
            GameObject adam = GameObject.Find("Adam_Big");
            if (adam != null)
                adamTransform = adam.transform;
        }
    }
    
    void LateUpdate()
    {
        if (isFollowingAdam)
        {
            FollowAdam();
        }
        else
        {
            FollowRemy();
        }
        
        // Smooth camera movement
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPosition, smoothSpeed * Time.deltaTime);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
    
    void FollowRemy()
    {
        if (remyTransform == null) return;
        
        // Position camera behind Remy
        targetPosition = remyTransform.position + remyOffset;
        
        // Look at Remy
        Vector3 lookTarget = remyTransform.position + Vector3.up * lookAtHeight;
        targetRotation = Quaternion.LookRotation(lookTarget - targetPosition);
    }
    
    void FollowAdam()
    {
        if (adamTransform == null || remyTransform == null) return;
        
        // Position camera behind Adam looking at Remy
        targetPosition = adamTransform.position + adamTransform.TransformDirection(adamOffset);
        
        // Look at Remy from Adam's position
        Vector3 lookTarget = remyTransform.position + Vector3.up * lookAtHeight;
        targetRotation = Quaternion.LookRotation(lookTarget - targetPosition);
    }
    
    public void SetFollowAdam(bool followAdam)
    {
        isFollowingAdam = followAdam;
    }
    
    public void SetCameraOffset(Vector3 offset, bool forAdam = false)
    {
        if (forAdam)
            adamOffset = offset;
        else
            remyOffset = offset;
    }
    
    public bool IsFollowingAdam()
    {
        return isFollowingAdam;
    }
}
