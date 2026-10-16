using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InstantPlayerSelector : MonoBehaviour
{
    private GameObject selectionPanel;
    
    void Start()
    {
        Debug.Log("InstantPlayerSelector: DISABLED for multiplayer mode");
        // DISABLED: CreatePlayerSelectionUI();
        // Multiplayer UI will be handled by other scripts
    }
    
    /* DISABLED FOR MULTIPLAYER - Original CreatePlayerSelectionUI method
    void CreatePlayerSelectionUI()
    {
        // Make sure we have an EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
        
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Make sure it's on top
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("Created new Canvas");
        }
        
        // Create selection panel
        selectionPanel = new GameObject("PlayerSelectionPanel");
        selectionPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = selectionPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        // Add semi-transparent background
        Image bgImage = selectionPanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);
        
        // Create center container
        GameObject container = new GameObject("Container");
        container.transform.SetParent(selectionPanel.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(600, 400);
        containerRect.anchoredPosition = Vector2.zero;
        
        Image containerBg = container.AddComponent<Image>();
        containerBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        
        // Add title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(container.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(500, 80);
        titleRect.anchoredPosition = new Vector2(0, -60);
        
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "Choose Your Fighter";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 42;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        
        // Create Adam button
        CreateButton(container.transform, "ADAM", new Vector2(-150, -20), 
            new Color(0.3f, 0.5f, 1f, 1f), () => SelectPlayer(true));
        
        // Create Remy button  
        CreateButton(container.transform, "REMY\n(Default)", new Vector2(150, -20), 
            new Color(1f, 0.3f, 0.3f, 1f), () => SelectPlayer(false));
        
        Debug.Log("Player selection UI created successfully!");
    }
    END OF DISABLED CreatePlayerSelectionUI METHOD */
    
    void CreateButton(Transform parent, string text, Vector2 position, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObj = new GameObject(text + " Button");
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(180, 180);
        buttonRect.anchoredPosition = position;
        
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        
        // Add hover effect
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        button.colors = colors;
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 32;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
        
        button.onClick.AddListener(action);
    }
    
    void SelectPlayer(bool isAdam)
    {
        string playerName = isAdam ? "Adam" : "Remy";
        Debug.Log($"Player selected: {playerName}");
        
        // Hide selection UI
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
        
        // Switch avatars
        GameObject remy = GameObject.Find("Remy");
        GameObject adam = GameObject.Find("Adam_Big");
        
        if (isAdam)
        {
            // Adam is player, Remy is enemy
            if (adam != null) adam.tag = "Player";
            if (remy != null) remy.tag = "Enemy";
            
            // Move camera behind Adam
            Camera.main.transform.position = new Vector3(3f, 1.8f, -3.5f);
            Camera.main.transform.rotation = Quaternion.Euler(10f, -5f, 0f);
        }
        else
        {
            // Remy is player, Adam is enemy
            if (remy != null) remy.tag = "Player";
            if (adam != null) adam.tag = "Enemy";
            
            // Default camera position
            Camera.main.transform.position = new Vector3(1.7f, 1.5f, -2.2f);
            Camera.main.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        
        // Try to switch tracking
        TrackingAvatarSwitcher switcher = FindObjectOfType<TrackingAvatarSwitcher>();
        if (switcher != null)
        {
            if (isAdam)
                switcher.SwitchToAdam();
            else
                switcher.SwitchToRemy();
        }
        
        Debug.Log($"Game started with {playerName}!");
    }
}
