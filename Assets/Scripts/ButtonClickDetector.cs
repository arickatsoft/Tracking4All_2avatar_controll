using UnityEngine;
using UnityEngine.UI;

public class ButtonClickDetector : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebugLogging = true;
    
    void Start()
    {
        if (enableDebugLogging)
        {
            Debug.Log("=== ButtonClickDetector Started ===");
            Debug.Log($"Time: {System.DateTime.Now}");
        }
        
        // Find ALL buttons in the scene and add click detection
        DetectAllButtons();
        
        // Look specifically for Adam/Remy buttons
        FindPlayerSelectionButtons();
    }
    
    void DetectAllButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>();
        Debug.Log($"Found {allButtons.Length} buttons in the scene:");
        
        for (int i = 0; i < allButtons.Length; i++)
        {
            Button button = allButtons[i];
            string buttonName = button.gameObject.name;
            
            Debug.Log($"Button {i + 1}: '{buttonName}' - Active: {button.gameObject.activeInHierarchy}");
            
            // Add a universal click detector to every button
            int buttonIndex = i; // Capture for closure
            button.onClick.AddListener(() => {
                Debug.Log($"*** BUTTON CLICKED *** '{buttonName}' at {System.DateTime.Now:HH:mm:ss.fff}");
                LogButtonDetails(button);
            });
        }
    }
    
    void FindPlayerSelectionButtons()
    {
        Debug.Log("=== Looking for Player Selection Buttons ===");
        
        // Look for Adam button
        Button adamButton = FindButtonByName("adam");
        if (adamButton != null)
        {
            Debug.Log($"FOUND Adam button: '{adamButton.gameObject.name}'");
            SetupAdamButton(adamButton);
        }
        else
        {
            Debug.LogWarning("Adam button NOT FOUND. Looking for alternatives...");
            SearchForAdamButtonAlternatives();
        }
        
        // Look for Remy button
        Button remyButton = FindButtonByName("remy");
        if (remyButton != null)
        {
            Debug.Log($"FOUND Remy button: '{remyButton.gameObject.name}'");
            SetupRemyButton(remyButton);
        }
        else
        {
            Debug.LogWarning("Remy button NOT FOUND. Looking for alternatives...");
            SearchForRemyButtonAlternatives();
        }
    }
    
    Button FindButtonByName(string searchTerm)
    {
        Button[] allButtons = FindObjectsOfType<Button>();
        
        foreach (Button button in allButtons)
        {
            if (button.gameObject.name.ToLower().Contains(searchTerm.ToLower()))
            {
                return button;
            }
            
            // Also check button text
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null && buttonText.text.ToLower().Contains(searchTerm.ToLower()))
            {
                return button;
            }
        }
        
        return null;
    }
    
    void SearchForAdamButtonAlternatives()
    {
        string[] adamKeywords = {"adam", "ADAM", "player1", "big", "enemy"};
        
        foreach (string keyword in adamKeywords)
        {
            Button found = FindButtonByName(keyword);
            if (found != null)
            {
                Debug.Log($"Found potential Adam button by keyword '{keyword}': {found.gameObject.name}");
            }
        }
    }
    
    void SearchForRemyButtonAlternatives()
    {
        string[] remyKeywords = {"remy", "REMY", "player2", "small", "default"};
        
        foreach (string keyword in remyKeywords)
        {
            Button found = FindButtonByName(keyword);
            if (found != null)
            {
                Debug.Log($"Found potential Remy button by keyword '{keyword}': {found.gameObject.name}");
            }
        }
    }
    
    void SetupAdamButton(Button adamButton)
    {
        // Add our camera switching logic
        adamButton.onClick.AddListener(() => {
            Debug.Log("🔥 ADAM BUTTON CLICKED - SWITCHING TO ADAM CAMERA 🔥");
            SwitchToAdamCamera();
        });
    }
    
    void SetupRemyButton(Button remyButton)
    {
        // Add our camera switching logic
        remyButton.onClick.AddListener(() => {
            Debug.Log("🔥 REMY BUTTON CLICKED - SWITCHING TO REMY CAMERA 🔥");
            SwitchToRemyCamera();
        });
    }
    
    void SwitchToAdamCamera()
    {
        Debug.Log("=== EXECUTING ADAM CAMERA SWITCH ===");
        
        GameObject mainCamera = GameObject.Find("Main Camera");
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        GameObject adam = GameObject.Find("Adam_Big");
        GameObject remy = GameObject.Find("Remy");
        
        Debug.Log($"Main Camera found: {mainCamera != null}");
        Debug.Log($"CameraRig_Adam found: {cameraRigAdam != null}");
        Debug.Log($"Adam found: {adam != null}");
        Debug.Log($"Remy found: {remy != null}");
        
        if (mainCamera != null)
        {
            mainCamera.SetActive(false);
            Debug.Log("✅ Main Camera DISABLED");
        }
        
        if (cameraRigAdam != null)
        {
            cameraRigAdam.SetActive(true);
            Debug.Log($"✅ CameraRig_Adam ENABLED - Active: {cameraRigAdam.activeInHierarchy}");
            LogCameraRigDetails(cameraRigAdam);
        }
        else
        {
            Debug.LogError("❌ CameraRig_Adam NOT FOUND!");
        }
        
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
        
        Debug.Log("=== ADAM CAMERA SWITCH COMPLETE ===");
    }
    
    void SwitchToRemyCamera()
    {
        Debug.Log("=== EXECUTING REMY CAMERA SWITCH ===");
        
        GameObject mainCamera = GameObject.Find("Main Camera");
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        GameObject adam = GameObject.Find("Adam_Big");
        GameObject remy = GameObject.Find("Remy");
        
        if (mainCamera != null)
        {
            mainCamera.SetActive(true);
            Debug.Log("✅ Main Camera ENABLED");
        }
        
        if (cameraRigAdam != null)
        {
            cameraRigAdam.SetActive(false);
            Debug.Log("✅ CameraRig_Adam DISABLED");
        }
        
        if (remy != null)
        {
            remy.tag = "Player";
            Debug.Log("✅ Remy tagged as Player");
        }
        
        if (adam != null)
        {
            adam.tag = "Enemy";
            Debug.Log("✅ Adam tagged as Enemy");
        }
        
        Debug.Log("=== REMY CAMERA SWITCH COMPLETE ===");
    }
    
    void LogButtonDetails(Button button)
    {
        Debug.Log($"Button Details for '{button.gameObject.name}':");
        Debug.Log($"  - Interactable: {button.interactable}");
        Debug.Log($"  - Active in Hierarchy: {button.gameObject.activeInHierarchy}");
        Debug.Log($"  - OnClick Persistent Calls: {button.onClick.GetPersistentEventCount()}");
        
        // Check for text
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            Debug.Log($"  - Button Text: '{buttonText.text}'");
        }
    }
    
    void LogCameraRigDetails(GameObject cameraRig)
    {
        Debug.Log($"CameraRig_Adam Details:");
        Debug.Log($"  - Position: {cameraRig.transform.position}");
        Debug.Log($"  - Rotation: {cameraRig.transform.rotation.eulerAngles}");
        Debug.Log($"  - Active Self: {cameraRig.activeSelf}");
        Debug.Log($"  - Active in Hierarchy: {cameraRig.activeInHierarchy}");
        
        // Check children
        Camera cam = cameraRig.GetComponentInChildren<Camera>();
        if (cam != null)
        {
            Debug.Log($"  - Child Camera found: {cam.gameObject.name}");
            Debug.Log($"  - Child Camera enabled: {cam.enabled}");
        }
        else
        {
            Debug.LogWarning("  - No Camera component found in children!");
        }
    }
    
    void Update()
    {
        // Keyboard shortcuts for testing
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("🎹 'A' Key pressed - Testing Adam camera");
            SwitchToAdamCamera();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("🎹 'R' Key pressed - Testing Remy camera");
            SwitchToRemyCamera();
        }
        
        // Log current camera state every 5 seconds (only once per 5-second period)
        if (Mathf.FloorToInt(Time.time) % 5 == 0 && Mathf.FloorToInt(Time.time) != Mathf.FloorToInt(Time.time - Time.deltaTime) && Time.time > 1f)
        {
            LogCurrentCameraState();
        }
    }
    
    void LogCurrentCameraState()
    {
        GameObject mainCamera = GameObject.Find("Main Camera");
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        
        string mainCameraState = mainCamera != null ? (mainCamera.activeInHierarchy ? "ACTIVE" : "INACTIVE") : "NOT FOUND";
        string adamCameraState = cameraRigAdam != null ? (cameraRigAdam.activeInHierarchy ? "ACTIVE" : "INACTIVE") : "NOT FOUND";
        
        Debug.Log($"📊 Camera Status - Main: {mainCameraState} | Adam: {adamCameraState}");
    }
}
