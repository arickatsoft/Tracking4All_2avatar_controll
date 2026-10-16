using UnityEngine;
using UnityEngine.UI;

public class PlayerSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button adamButton;
    [SerializeField] private Button remyButton;
    [SerializeField] private GameObject selectionPanel;
    
    [Header("Camera Manager")]
    [SerializeField] private CameraManager cameraManager;
    
    [Header("Avatar References")]
    [SerializeField] private GameObject adamAvatar;
    [SerializeField] private GameObject remyAvatar;
    
    private void Start()
    {
        // Find camera manager if not assigned
        if (cameraManager == null)
        {
            cameraManager = FindObjectOfType<CameraManager>();
        }
        
        // Auto-find avatars if not assigned
        if (adamAvatar == null)
        {
            adamAvatar = GameObject.Find("Adam_Big");
        }
        if (remyAvatar == null)
        {
            remyAvatar = GameObject.Find("Remy");
        }
        
        // Setup button listeners
        SetupButtons();
        
        // Show selection UI at start
        ShowSelectionUI();
    }
    
    private void SetupButtons()
    {
        if (adamButton != null)
        {
            adamButton.onClick.RemoveAllListeners();
            adamButton.onClick.AddListener(OnAdamSelected);
        }
        
        if (remyButton != null)
        {
            remyButton.onClick.RemoveAllListeners();
            remyButton.onClick.AddListener(OnRemySelected);
        }
    }
    
    private void OnAdamSelected()
    {
        Debug.Log("Adam selected - switching to CameraRig_Adam");
        
        if (cameraManager != null)
        {
            cameraManager.OnAdamButtonPressed();
        }
        
        HideSelectionUI();

        // Apply rotation with delay to let tracking system initialize
        // Preserve the existing X rotation from Inspector
        if (adamAvatar != null)
        {
            float currentXRotation = adamAvatar.transform.localEulerAngles.x;
            StartCoroutine(ApplyRotationDelayed(adamAvatar, new Vector3(currentXRotation, 180, 0), 0.5f));
        }
    }

    private void OnRemySelected()
    {
        Debug.Log("Remy selected - using main camera");

        if (cameraManager != null)
        {
            cameraManager.OnRemyButtonPressed();
        }

        HideSelectionUI();

        // Apply rotation with delay to let tracking system initialize
        // Preserve the existing X rotation from Inspector
        if (adamAvatar != null)
        {
            float currentXRotation = adamAvatar.transform.localEulerAngles.x;
            StartCoroutine(ApplyRotationDelayed(adamAvatar, new Vector3(currentXRotation, 180, 0), 0.5f));
        }
    }
    
    public void ShowSelectionUI()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }
    }
    
    public void HideSelectionUI()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
    }
    
    // Coroutine to apply rotation with delay (lets tracking system initialize first)
    private System.Collections.IEnumerator ApplyRotationDelayed(GameObject avatar, Vector3 eulerAngles, float delay)
    {
        Debug.Log($"Waiting {delay} seconds before applying rotation to {avatar.name}...");
        
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);
        
        // Apply the rotation
        avatar.transform.rotation = Quaternion.Euler(eulerAngles);
        Debug.Log($"Applied rotation {eulerAngles} to {avatar.name}");
    }
    
    // Method to show selection again (e.g., from pause menu)
    public void ShowPlayerSelection()
    {
        ShowSelectionUI();
    }
}
