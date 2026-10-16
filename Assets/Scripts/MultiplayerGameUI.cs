using UnityEngine;
using UnityEngine.UI;
using Tracking4All.Multiplayer;

namespace Tracking4All
{
    /// <summary>
    /// Simple game mode selection UI - replaces Adam/Remy chooser with Host/Join chooser
    /// This replaces the InstantPlayerSelector UI with multiplayer options
    /// </summary>
    public class MultiplayerGameUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject gameSelectionPanel;
        [SerializeField] private Button hostGameButton;
        [SerializeField] private Button joinGameButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text titleText;
        
        private MultiplayerNetworkManager networkManager;
        
        void Start()
        {
            UnityEngine.Debug.Log("=== MultiplayerGameUI Starting ===");
            
            // Disable old player selector first
            DisableOldPlayerSelector();
            
            CreateUI();
            SetupNetworkManager();
        }
        
        void DisableOldPlayerSelector()
        {
            // Find and disable InstantPlayerSelector
            InstantPlayerSelector oldSelector = FindObjectOfType<InstantPlayerSelector>();
            if (oldSelector != null)
            {
                UnityEngine.Debug.Log("Disabling old InstantPlayerSelector");
                oldSelector.gameObject.SetActive(false);
                Destroy(oldSelector.gameObject);
            }
            
            // Find and disable any existing player selection panels
            GameObject[] oldPanels = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in oldPanels)
            {
                if (obj.name.Contains("PlayerSelection") || obj.name.Contains("PlayerChoose"))
                {
                    UnityEngine.Debug.Log($"Disabling old UI panel: {obj.name}");
                    obj.SetActive(false);
                }
            }
        }        
        void CreateUI()
        {
            // Find or create main canvas
            if (mainCanvas == null)
            {
                GameObject canvasGO = GameObject.Find("Canvas");
                if (canvasGO == null)
                {
                    canvasGO = new GameObject("Canvas");
                    mainCanvas = canvasGO.AddComponent<Canvas>();
                    mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    mainCanvas.sortingOrder = 100; // Make sure it's on top
                    canvasGO.AddComponent<CanvasScaler>();
                    canvasGO.AddComponent<GraphicRaycaster>();
                }
                else
                {
                    mainCanvas = canvasGO.GetComponent<Canvas>();
                }
            }
            
            // Create game selection panel
            if (gameSelectionPanel == null)
            {
                gameSelectionPanel = CreateSelectionPanel();
            }
        }
        
        GameObject CreateSelectionPanel()
        {
            // Main panel
            GameObject panel = new GameObject("MultiplayerGameSelection");
            panel.transform.SetParent(mainCanvas.transform, false);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
            
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            // Create center container
            GameObject container = new GameObject("Container");
            container.transform.SetParent(panel.transform, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(600, 400);
            containerRect.anchoredPosition = Vector2.zero;
            
            Image containerBg = container.AddComponent<Image>();
            containerBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            
            // Title
            CreateTitle(container);
            
            // Host Game Button
            hostGameButton = CreateButton(container.transform, "HOST GAME", new Vector2(-150, -20), 
                new Color(0.2f, 0.6f, 0.2f, 1f));
            
            // Join Game Button
            joinGameButton = CreateButton(container.transform, "JOIN GAME", new Vector2(150, -20), 
                new Color(0.2f, 0.2f, 0.6f, 1f));
            
            // Status Text
            CreateStatusText(container);
            
            return panel;
        }        
        void CreateTitle(GameObject container)
        {
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(container.transform, false);
            
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(500, 80);
            titleRect.anchoredPosition = new Vector2(0, -60);
            
            titleText = titleGO.AddComponent<Text>();
            titleText.text = "MULTIPLAYER TRACKING GAME";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 36;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyle.Bold;
        }
        
        Button CreateButton(Transform parent, string text, Vector2 position, Color color)
        {
            GameObject buttonGO = new GameObject($"{text}Button");
            buttonGO.transform.SetParent(parent, false);
            
            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(200, 80);            
            Button button = buttonGO.AddComponent<Button>();
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = color;
            
            // Add hover effect
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            button.colors = colors;
            
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
            buttonText.fontSize = 24;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
            buttonText.fontStyle = FontStyle.Bold;
            
            return button;
        }        
        void CreateStatusText(GameObject container)
        {
            GameObject statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(container.transform, false);
            
            RectTransform statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0.5f);
            statusRect.anchorMax = new Vector2(0.5f, 0.5f);
            statusRect.anchoredPosition = new Vector2(0, -150);
            statusRect.sizeDelta = new Vector2(550, 40);
            
            statusText = statusGO.AddComponent<Text>();
            statusText.text = "Select game mode to begin";
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 18;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = Color.yellow;
        }
        
        void SetupNetworkManager()
        {
            // Find or create network manager
            networkManager = FindObjectOfType<MultiplayerNetworkManager>();
            if (networkManager == null)
            {
                GameObject networkGO = new GameObject("MultiplayerNetworkManager");
                networkManager = networkGO.AddComponent<MultiplayerNetworkManager>();
                UnityEngine.Debug.Log("Created MultiplayerNetworkManager");
            }
            
            // Setup references
            SetNetworkManagerReferences();
        }        
        void SetNetworkManagerReferences()
        {
            if (networkManager == null) return;
            
            // Set UI references using reflection (since the fields are SerializeField private)
            var networkType = typeof(MultiplayerNetworkManager);
            
            var gameSelectionPanelField = networkType.GetField("gameSelectionPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (gameSelectionPanelField != null)
                gameSelectionPanelField.SetValue(networkManager, gameSelectionPanel);
                
            var hostGameButtonField = networkType.GetField("hostGameButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (hostGameButtonField != null)
                hostGameButtonField.SetValue(networkManager, hostGameButton);
                
            var joinGameButtonField = networkType.GetField("joinGameButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (joinGameButtonField != null)
                joinGameButtonField.SetValue(networkManager, joinGameButton);
                
            var statusTextField = networkType.GetField("statusText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (statusTextField != null)
                statusTextField.SetValue(networkManager, statusText);
            
            // Set avatar and camera references
            SetAvatarReferences();
            SetCameraReferences();
        }        
        void SetAvatarReferences()
        {
            var networkType = typeof(MultiplayerNetworkManager);
            
            // Find avatars in scene (using Remy and Bryce instead of Adam)
            GameObject remy = GameObject.Find("Remy");
            GameObject bryce = GameObject.Find("Bryce");
            
            if (remy != null)
            {
                var remyField = networkType.GetField("remyAvatar", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (remyField != null)
                    remyField.SetValue(networkManager, remy);
                    
                UnityEngine.Debug.Log("Set Remy avatar reference in NetworkManager");
            }
            
            if (bryce != null)
            {
                var bryceField = networkType.GetField("bryceAvatar", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (bryceField != null)
                    bryceField.SetValue(networkManager, bryce);
                    
                UnityEngine.Debug.Log("Set Bryce avatar reference in NetworkManager");
            }
        }        
        void SetCameraReferences()
        {
            var networkType = typeof(MultiplayerNetworkManager);
            
            // Find camera rig
            Transform cameraRig = null;
            GameObject cameraRigGO = GameObject.Find("CameraRig");
            if (cameraRigGO != null)
            {
                cameraRig = cameraRigGO.transform;
            }
            else
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    cameraRig = mainCam.transform;
                }
            }
            
            if (cameraRig != null)
            {
                var cameraRigField = networkType.GetField("cameraRig", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (cameraRigField != null)
                    cameraRigField.SetValue(networkManager, cameraRig);
                    
                UnityEngine.Debug.Log("Set camera rig reference in NetworkManager");
            }
            
            // Set main camera
            Camera mainCamera = Camera.main;            if (mainCamera != null)
            {
                var mainCameraField = networkType.GetField("mainCamera", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (mainCameraField != null)
                    mainCameraField.SetValue(networkManager, mainCamera);
                    
                UnityEngine.Debug.Log("Set main camera reference in NetworkManager");
            }
        }
        
        public void ShowGameModeSelection()
        {
            if (gameSelectionPanel != null)
            {
                gameSelectionPanel.SetActive(true);
            }
            
            if (statusText != null)
            {
                statusText.text = "Select game mode to begin";
            }
        }
        
        public void HideGameModeSelection()
        {
            if (gameSelectionPanel != null)
            {
                gameSelectionPanel.SetActive(false);
            }
        }
    }
}