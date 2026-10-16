using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MultiplayerUIManager : MonoBehaviour
{
    [Header("UI References")]
    private GameObject selectionUI;
    private Button hostButton;
    private Button joinButton;
    private Text statusText;
    
    void Start()
    {
        Debug.Log("MultiplayerUIManager: Creating multiplayer selection UI...");
        CreateMultiplayerUI();
    }
    
    void CreateMultiplayerUI()
    {
        // Find existing Canvas
        GameObject existingCanvas = GameObject.Find("Canvas");
        Canvas canvas;
        
        if (existingCanvas != null)
        {
            canvas = existingCanvas.GetComponent<Canvas>();
            Debug.Log("Using existing Canvas for multiplayer UI");
        }
        else
        {
            // Create new Canvas if doesn't exist
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("Created new Canvas for multiplayer UI");
        }
        
        // Ensure EventSystem exists
        if (GameObject.FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("Created EventSystem for UI interaction");
        }
        
        // Create main selection panel
        selectionUI = new GameObject("MultiplayerSelectionPanel");
        selectionUI.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = selectionUI.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(500, 350);
        
        // Add semi-transparent dark background
        Image panelImage = selectionUI.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Create title
        CreateTitle();
        
        // Create Host button
        CreateHostButton();
        
        // Create Join button  
        CreateJoinButton();
        
        // Create status text
        CreateStatusText();
        
        Debug.Log("Multiplayer UI created successfully!");
    }
    
    void CreateTitle()
    {
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(selectionUI.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.8f);
        titleRect.anchorMax = new Vector2(0.5f, 0.8f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(450, 50);
        
        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "MULTIPLAYER TRACKING GAME";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
    }
    
    void CreateHostButton()
    {
        GameObject hostButtonGO = new GameObject("HostButton");
        hostButtonGO.transform.SetParent(selectionUI.transform, false);
        RectTransform hostRect = hostButtonGO.AddComponent<RectTransform>();
        hostRect.anchorMin = new Vector2(0.25f, 0.5f);
        hostRect.anchorMax = new Vector2(0.25f, 0.5f);
        hostRect.anchoredPosition = Vector2.zero;
        hostRect.sizeDelta = new Vector2(150, 80);
        
        hostButton = hostButtonGO.AddComponent<Button>();
        Image hostImage = hostButtonGO.AddComponent<Image>();
        hostImage.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        
        // Add button text
        GameObject hostTextGO = new GameObject("Text");
        hostTextGO.transform.SetParent(hostButtonGO.transform, false);
        RectTransform hostTextRect = hostTextGO.AddComponent<RectTransform>();
        hostTextRect.anchorMin = Vector2.zero;
        hostTextRect.anchorMax = Vector2.one;
        hostTextRect.sizeDelta = Vector2.zero;
        hostTextRect.offsetMin = Vector2.zero;
        hostTextRect.offsetMax = Vector2.zero;
        
        Text hostText = hostTextGO.AddComponent<Text>();
        hostText.text = "HOST GAME";
        hostText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hostText.fontSize = 20;
        hostText.fontStyle = FontStyle.Bold;
        hostText.alignment = TextAnchor.MiddleCenter;
        hostText.color = Color.white;
        
        // Add button click listener
        hostButton.onClick.AddListener(OnHostButtonClicked);
    }
    
    void CreateJoinButton()
    {
        GameObject joinButtonGO = new GameObject("JoinButton");
        joinButtonGO.transform.SetParent(selectionUI.transform, false);
        RectTransform joinRect = joinButtonGO.AddComponent<RectTransform>();
        joinRect.anchorMin = new Vector2(0.75f, 0.5f);
        joinRect.anchorMax = new Vector2(0.75f, 0.5f);
        joinRect.anchoredPosition = Vector2.zero;
        joinRect.sizeDelta = new Vector2(150, 80);
        
        joinButton = joinButtonGO.AddComponent<Button>();
        Image joinImage = joinButtonGO.AddComponent<Image>();
        joinImage.color = new Color(0.2f, 0.2f, 0.8f, 1f); // Blue
        
        // Add button text
        GameObject joinTextGO = new GameObject("Text");
        joinTextGO.transform.SetParent(joinButtonGO.transform, false);
        RectTransform joinTextRect = joinTextGO.AddComponent<RectTransform>();
        joinTextRect.anchorMin = Vector2.zero;
        joinTextRect.anchorMax = Vector2.one;
        joinTextRect.sizeDelta = Vector2.zero;
        joinTextRect.offsetMin = Vector2.zero;
        joinTextRect.offsetMax = Vector2.zero;
        
        Text joinText = joinTextGO.AddComponent<Text>();
        joinText.text = "JOIN GAME";
        joinText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        joinText.fontSize = 20;
        joinText.fontStyle = FontStyle.Bold;
        joinText.alignment = TextAnchor.MiddleCenter;
        joinText.color = Color.white;
        
        // Add button click listener
        joinButton.onClick.AddListener(OnJoinButtonClicked);
    }
    
    void CreateStatusText()
    {
        GameObject statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(selectionUI.transform, false);
        RectTransform statusRect = statusGO.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.25f);
        statusRect.anchorMax = new Vector2(0.5f, 0.25f);
        statusRect.anchoredPosition = Vector2.zero;
        statusRect.sizeDelta = new Vector2(400, 40);
        
        statusText = statusGO.AddComponent<Text>();
        statusText.text = "Choose your multiplayer mode";
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 18;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
    }
    
    void OnHostButtonClicked()
    {
        Debug.Log("HOST GAME CLICKED! Starting host mode...");
        
        // Update status text
        if (statusText != null)
        {
            statusText.text = "HOST MODE: Remy is your local player!";
            statusText.color = Color.green;
        }
        
        // Call existing camera management methods if they exist
        CallCameraMethod("OnAdamButtonPressed");
        
        // Hide UI after delay
        StartCoroutine(HideUIAfterDelay(3f));
        
        Debug.Log("Host mode activated - Remy tracking enabled");
    }
    
    void OnJoinButtonClicked()
    {
        Debug.Log("JOIN GAME CLICKED! Starting client mode...");
        
        // Update status text
        if (statusText != null)
        {
            statusText.text = "CLIENT MODE: Bryce is your local player!";
            statusText.color = Color.blue;
        }
        
        // Call existing camera management methods if they exist  
        CallCameraMethod("OnRemyButtonPressed");
        
        // Hide UI after delay
        StartCoroutine(HideUIAfterDelay(3f));
        
        Debug.Log("Join mode activated - Bryce tracking enabled");
    }
    
    void CallCameraMethod(string methodName)
    {
        // Try to find CameraManager and call appropriate method
        GameObject cameraManager = GameObject.Find("CameraManager");
        if (cameraManager != null)
        {
            Component cameraComp = cameraManager.GetComponent("CameraManager");
            if (cameraComp != null)
            {
                try
                {
                    cameraComp.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
                    Debug.Log($"Called {methodName} on CameraManager");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not call {methodName}: {e.Message}");
                }
            }
        }
        else
        {
            Debug.LogWarning("CameraManager not found - camera positioning may not work");
        }
    }
    
    System.Collections.IEnumerator HideUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (selectionUI != null)
        {
            selectionUI.SetActive(false);
            Debug.Log("Multiplayer UI hidden - ready for gameplay");
        }
    }
}
