using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuickSetupPlayerSelection : MonoBehaviour
{
    // DISABLED FOR MULTIPLAYER MODE
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void SetupOnSceneLoad()
    {
        Debug.Log("QuickSetupPlayerSelection: DISABLED for multiplayer mode");
        return; // Exit immediately
        
        // All the original setup code is disabled below:
        // Check if PlayerSelectionManager exists
        PlayerSelectionManager manager = GameObject.FindObjectOfType<PlayerSelectionManager>();
        
        if (manager == null)
        {
            Debug.Log("Creating PlayerSelectionManager...");
            
            // Create the manager
            GameObject managerGO = new GameObject("PlayerSelectionManager");
            manager = managerGO.AddComponent<PlayerSelectionManager>();
            
            Debug.Log("PlayerSelectionManager created and will show UI on Start");
        }
        else
        {
            Debug.Log("PlayerSelectionManager already exists in scene");
        }
        
        // Ensure EventSystem exists for UI
        if (GameObject.FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("Created EventSystem for UI interaction");
        }
    }
}

// Alternative manual setup component
[AddComponentMenu("Tracking4All/Quick Player Selection Setup")]
public class ManualPlayerSelectionSetup : MonoBehaviour
{
    [Header("Quick Setup")]
    [SerializeField] private bool setupOnAwake = true;
    
    void Awake()
    {
        if (setupOnAwake)
        {
            SetupPlayerSelection();
        }
    }
    
    public void SetupPlayerSelection()
    {
        // Find or create PlayerSelectionManager
        PlayerSelectionManager manager = GameObject.FindObjectOfType<PlayerSelectionManager>();
        
        if (manager == null)
        {
            GameObject managerGO = new GameObject("PlayerSelectionManager");
            manager = managerGO.AddComponent<PlayerSelectionManager>();
            Debug.Log("Created PlayerSelectionManager");
        }
        
        // Create UI immediately for testing
        CreateTestUI();
    }
    
    void CreateTestUI()
    {
        // Ensure Canvas exists
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create a simple test panel
        GameObject panel = new GameObject("TestPlayerSelection");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(500, 400);
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.85f);
        titleRect.anchorMax = new Vector2(0.5f, 0.85f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(400, 60);
        
        Text title = titleGO.AddComponent<Text>();
        title.text = "SELECT YOUR PLAYER";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 36;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        
        // Adam Button
        CreatePlayerButton(panel.transform, "ADAM", new Vector2(-120, 0), 
            new Color(0.2f, 0.4f, 0.8f, 1f), true);
        
        // Remy Button  
        CreatePlayerButton(panel.transform, "REMY", new Vector2(120, 0), 
            new Color(0.8f, 0.2f, 0.2f, 1f), false);
        
        Debug.Log("Test UI Created - You should see player selection buttons now!");
    }
    
    void CreatePlayerButton(Transform parent, string playerName, Vector2 position, Color color, bool isAdam)
    {
        GameObject buttonGO = new GameObject(playerName + "Button");
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(150, 150);
        
        Button button = buttonGO.AddComponent<Button>();
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = color;
        
        // Add text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text text = textGO.AddComponent<Text>();
        text.text = playerName;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        
        // Add click handler
        button.onClick.AddListener(() => OnPlayerButtonClicked(isAdam));
    }
    
    void OnPlayerButtonClicked(bool isAdam)
    {
        Debug.Log($"Player selected: {(isAdam ? "Adam" : "Remy")}");
        
        // Find the manager and trigger selection
        PlayerSelectionManager manager = GameObject.FindObjectOfType<PlayerSelectionManager>();
        if (manager != null)
        {
            // Hide this test UI
            transform.GetChild(0).gameObject.SetActive(false);
            
            // You can trigger the manager's selection here
            // For now just log
            Debug.Log("Player selection triggered!");
        }
        
        // Also try to find and update tracking
        TrackingAvatarSwitcher switcher = GameObject.FindObjectOfType<TrackingAvatarSwitcher>();
        if (switcher != null)
        {
            if (isAdam)
                switcher.SwitchToAdam();
            else
                switcher.SwitchToRemy();
        }
    }
}
