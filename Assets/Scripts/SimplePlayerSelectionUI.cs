using UnityEngine;
using UnityEngine.UI;

public class SimplePlayerSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    public Button adamButton;
    public Button remyButton;
    public GameObject selectionPanel;
    
    [Header("Camera Switch Test")]
    public CameraSwitchTest cameraSwitcher;
    
    void Start()
    {
        Debug.Log("SimplePlayerSelectionUI: DISABLED for multiplayer mode");
        // All UI creation disabled for multiplayer
        return;
        // Find camera switcher if not assigned
        if (cameraSwitcher == null)
        {
            cameraSwitcher = FindObjectOfType<CameraSwitchTest>();
        }
        
        // Try to find existing buttons or create them
        SetupButtons();
        
        Debug.Log("SimplePlayerSelectionUI initialized");
    }
    
    void SetupButtons()
    {
        // Try to find existing buttons in the scene
        if (adamButton == null)
        {
            // Look for button with "Adam" in the name
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("adam") && obj.GetComponent<Button>() != null)
                {
                    adamButton = obj.GetComponent<Button>();
                    Debug.Log($"Found Adam button: {obj.name}");
                    break;
                }
            }
        }
        
        if (remyButton == null)
        {
            // Look for button with "Remy" in the name
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("remy") && obj.GetComponent<Button>() != null)
                {
                    remyButton = obj.GetComponent<Button>();
                    Debug.Log($"Found Remy button: {obj.name}");
                    break;
                }
            }
        }
        
        // Setup button listeners
        if (adamButton != null && cameraSwitcher != null)
        {
            adamButton.onClick.RemoveAllListeners();
            adamButton.onClick.AddListener(() => {
                Debug.Log("Adam button clicked via UI");
                cameraSwitcher.OnAdamButtonPressed();
                HideSelectionUI();
            });
            Debug.Log("Adam button listener added");
        }
        else if (adamButton == null)
        {
            Debug.LogWarning("Adam button not found! You can still use 'A' key to test.");
        }
        
        if (remyButton != null && cameraSwitcher != null)
        {
            remyButton.onClick.RemoveAllListeners();
            remyButton.onClick.AddListener(() => {
                Debug.Log("Remy button clicked via UI");
                cameraSwitcher.OnRemyButtonPressed();
                HideSelectionUI();
            });
            Debug.Log("Remy button listener added");
        }
        else if (remyButton == null)
        {
            Debug.LogWarning("Remy button not found! You can still use 'R' key to test.");
        }
    }
    
    public void ShowSelectionUI()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }
        Debug.Log("Selection UI shown");
    }
    
    public void HideSelectionUI()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
        Debug.Log("Selection UI hidden");
    }
    
    // Method to be called from external systems
    public void OnAdamSelected()
    {
        if (cameraSwitcher != null)
        {
            cameraSwitcher.OnAdamButtonPressed();
        }
        HideSelectionUI();
    }
    
    public void OnRemySelected()
    {
        if (cameraSwitcher != null)
        {
            cameraSwitcher.OnRemyButtonPressed();
        }
        HideSelectionUI();
    }
}
