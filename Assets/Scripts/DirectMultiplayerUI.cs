using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Direct UI replacement script - instantly creates Host/Join buttons and removes old UI
/// Add this script to any GameObject in the scene and it will immediately take effect
/// </summary>
public class DirectMultiplayerUI : MonoBehaviour
{
    [Header("Auto-Setup")]
    public bool autoSetupOnStart = true;
    
    private GameObject multiplayerUI;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            Debug.Log("=== DirectMultiplayerUI: Starting UI replacement ===");
            SetupMultiplayerUI();
        }
    }
    
    [ContextMenu("Setup Multiplayer UI")]
    public void SetupMultiplayerUI()
    {
        Debug.Log("Setting up multiplayer UI...");
        
        // Step 1: Remove old UI
        RemoveOldUI();
        
        // Step 2: Create new Host/Join UI
        CreateHostJoinUI();
        
        Debug.Log("Multiplayer UI setup complete!");
    }
    
    void RemoveOldUI()
    {
        // Find and destroy any existing player selection UI
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        
        foreach (GameObject obj in allObjects)
        {
            // Look for objects that might be the old UI
            if (obj.name.Contains("PlayerSelection") || 
                obj.name.Contains("Adam") || 
                obj.name.Contains("Remy") ||
                obj.name.Contains("Choose") ||
                obj.name.Contains("Fighter"))
            {
                Debug.Log($"Removing old UI object: {obj.name}");
                obj.SetActive(false);
            }
        }
        
        // Also disable InstantPlayerSelector component if it exists
        MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>(true);
        foreach (MonoBehaviour comp in allComponents)
        {
            if (comp.GetType().Name == "InstantPlayerSelector")
            {
                Debug.Log("Disabling InstantPlayerSelector component");
                comp.enabled = false;
                comp.gameObject.SetActive(false);
            }
        }
    }
    
    void CreateHostJoinUI()
    {
        // Find main canvas or create one
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.Log("Creating new Canvas");
            GameObject canvasGO = new GameObject("MultiplayerCanvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 1000; // Make sure it's on top
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create multiplayer UI panel
        multiplayerUI = new GameObject("MultiplayerUI");
        multiplayerUI.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform panelRect = multiplayerUI.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        // Semi-transparent background
        Image bgImage = multiplayerUI.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);
        
        // Create container
        GameObject container = new GameObject("ButtonContainer");
        container.transform.SetParent(multiplayerUI.transform, false);
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(600, 400);
        containerRect.anchoredPosition = Vector2.zero;
        
        // Title
        CreateTitle(container);
        
        // Host button
        CreateButton(container, "HOST GAME", new Vector2(-150, 0), Color.green, OnHostClicked);
        
        // Join button  
        CreateButton(container, "JOIN GAME", new Vector2(150, 0), Color.blue, OnJoinClicked);
        
        // Status text
        CreateStatusText(container);
        
        Debug.Log("Host/Join UI created successfully!");
    }    
    void CreateTitle(GameObject container)
    {
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(container.transform, false);
        
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(500, 80);
        titleRect.anchoredPosition = new Vector2(0, 100);
        
        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "MULTIPLAYER TRACKING GAME";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 28;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyle.Bold;
    }
    
    void CreateButton(GameObject parent, string text, Vector2 position, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonGO = new GameObject($"{text}Button");
        buttonGO.transform.SetParent(parent.transform, false);
        
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(200, 80);
        buttonRect.anchoredPosition = position;
        
        Button button = buttonGO.AddComponent<Button>();
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = color;
        
        // Button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text buttonText = textGO.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 20;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
        buttonText.fontStyle = FontStyle.Bold;
        
        button.onClick.AddListener(action);
    }
    
    void CreateStatusText(GameObject container)
    {
        GameObject statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(container.transform, false);
        
        RectTransform statusRect = statusGO.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.sizeDelta = new Vector2(400, 40);
        statusRect.anchoredPosition = new Vector2(0, -100);
        
        Text statusText = statusGO.AddComponent<Text>();
        statusText.text = "Choose your mode to start the game";
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 16;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.yellow;
    }    
    void OnHostClicked()
    {
        Debug.Log("HOST GAME button clicked!");
        
        // Hide the UI
        if (multiplayerUI != null)
        {
            multiplayerUI.SetActive(false);
        }
        
        // TODO: Initialize host functionality
        Debug.Log("Starting host mode - Remy will be the local player");
        ShowMessage("HOST MODE ACTIVATED - Remy is your player");
    }
    
    void OnJoinClicked()
    {
        Debug.Log("JOIN GAME button clicked!");
        
        // Hide the UI
        if (multiplayerUI != null)
        {
            multiplayerUI.SetActive(false);
        }
        
        // Switch to Remy's view (client should be Remy)
        CameraManager cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager != null)
        {
            Debug.Log("Switching to Remy view for client mode");
            cameraManager.OnRemyButtonPressed();
        }
        else
        {
            Debug.LogWarning("CameraManager not found! Cannot switch to Remy view.");
        }
        
        // Initialize client functionality
        Debug.Log("Starting join mode - Remy will be the local player");
        ShowMessage("JOIN MODE ACTIVATED - Remy is your player");
    }
    
    void ShowMessage(string message)
    {
        // Create a temporary message display
        GameObject messageGO = new GameObject("TempMessage");
        messageGO.transform.SetParent(FindObjectOfType<Canvas>().transform, false);
        
        RectTransform messageRect = messageGO.AddComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.sizeDelta = new Vector2(600, 100);
        messageRect.anchoredPosition = Vector2.zero;
        
        Image messageBg = messageGO.AddComponent<Image>();
        messageBg.color = new Color(0, 0.8f, 0, 0.9f);
        
        GameObject textGO = new GameObject("MessageText");
        textGO.transform.SetParent(messageGO.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text messageText = textGO.AddComponent<Text>();
        messageText.text = message;
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 24;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = Color.white;
        messageText.fontStyle = FontStyle.Bold;
        
        // Auto-destroy after 3 seconds
        Destroy(messageGO, 3f);
    }
    
    void OnDestroy()
    {
        if (multiplayerUI != null)
        {
            Destroy(multiplayerUI);
        }
    }
}