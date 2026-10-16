using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Tracking4All.Multiplayer;
using Tracking4All.Multiplayer;

/// <summary>
/// Clean multiplayer buttons script with all camera/display logic removed
/// Ready for fresh implementation
/// </summary>
public class BasicMultiplayerButtons : MonoBehaviour
{
    [System.Serializable]
    public class PoseLandmarkData
    {
        public float[] landmarkPositions; // MediaPipe landmarks (33 landmarks * 3 coords = 99 floats)
        public long timestamp;
        
        public PoseLandmarkData(float[] landmarks, long time)
        {
            landmarkPositions = landmarks;
            timestamp = time;
        }
        
        public string ToNetworkMessage()
        {
            string landmarkString = string.Join(",", landmarkPositions);
            return $"POSE_LANDMARKS|{landmarkString}|{timestamp}";
        }
        
        public static PoseLandmarkData FromNetworkMessage(string message)
        {
            string[] parts = message.Split('|');
            if (parts.Length == 3 && parts[0] == "POSE_LANDMARKS")
            {
                string[] landmarkStrings = parts[1].Split(',');
                float[] landmarks = new float[landmarkStrings.Length];
                for (int i = 0; i < landmarkStrings.Length; i++)
                {
                    float.TryParse(landmarkStrings[i], out landmarks[i]);
                }
                long.TryParse(parts[2], out long timestamp);
                return new PoseLandmarkData(landmarks, timestamp);
            }
            return null;
        }
    }

    // Network state
    private bool isHost = false;
    private bool isClient = false;
    
    // UI Components
    private GameObject selectionUI;
    private Button hostButton;
    private Button joinButton;
    private Text statusText;
    
    // Communication logging
    private GameObject logPanel;
    private Text logText;
    private string logData = "";
    
    // Data processing
    private Queue<PoseLandmarkData> incomingPoseData = new Queue<PoseLandmarkData>();
    private System.Object logLock = new System.Object();
    private Queue<string> logQueue = new Queue<string>();

    void Awake()
    {
        Debug.Log("BasicMultiplayerButtons: Creating multiplayer UI programmatically...");
        Debug.Log("BasicMultiplayerButtons: Script is running!");
        
        try
        {
            Debug.Log("Starting SetupInitialPuppetConfiguration...");
            // IMPORTANT: Set puppet auto-seek flags BEFORE Avatar system initializes
            SetupInitialPuppetConfiguration();
            
            Debug.Log("Starting CreateMultiplayerUI...");
            // Create the multiplayer UI (Host/Join buttons and log panel)
            CreateMultiplayerUI();
            Debug.Log("=== UI CREATION COMPLETED IN AWAKE ===");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ERROR IN AWAKE: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }
    
    void Start()
    {
        try
        {
            Debug.Log("=== BASICMULTIPLAYERBUTTONS START STARTED ===");
            
            // Start the logging system
            InvokeRepeating("ProcessLogQueue", 0.1f, 0.1f);
            
            // Start the network data processing
            InvokeRepeating("ProcessIncomingPoseData", 0.033f, 0.033f); // 30 FPS
            
            // Start sending pose data from player avatar
            InvokeRepeating("SendPlayerPoseData", 0.033f, 0.033f); // 30 FPS
            
            Debug.Log("=== BASICMULTIPLAYERBUTTONS START COMPLETED SUCCESSFULLY ===");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"=== ERROR IN BASICMULTIPLAYERBUTTONS START: {e.Message} ===");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }
    
    // DEBUG method to test log visibility
    void TestLogVisibility()
    {
        AddLogEntry($"🔍 TEST LOG VISIBILITY - {System.DateTime.Now:HH:mm:ss.fff}");
        AddLogEntry($"📱 App is running and logging system is active!");
    }
    
    void SetupInitialPuppetConfiguration()
    {
        AddLogEntry("🏗️ Setting up initial puppet configuration before Avatar initialization");
        
        GameObject remyPuppet = GameObject.Find("Remy");
        GameObject adamPuppet = GameObject.Find("Adam_Big");
        
        if (remyPuppet == null && adamPuppet == null)
        {
            AddLogEntry("❌ No avatar puppets found in scene! Looking for alternative names...");
            
            // Try alternative names
            remyPuppet = GameObject.Find("remy") ?? GameObject.Find("REMY");
            adamPuppet = GameObject.Find("adam") ?? GameObject.Find("ADAM") ?? GameObject.Find("Adam");
            
            if (remyPuppet == null && adamPuppet == null)
            {
                AddLogEntry("❌ Still no puppets found! Check scene setup.");
                return;
            }
        }
        
        AddLogEntry($"✅ Found puppets: Remy={remyPuppet?.name}, Adam={adamPuppet?.name}");
        
        // Test log immediately to see if it's working
        TestLogVisibility();
    }
    
    void ProcessLogQueue()
    {
        lock (logLock)
        {
            while (logQueue.Count > 0)
            {
                string message = logQueue.Dequeue();
                string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
                string logEntry = $"[{timestamp}] {message}\n";
                logData += logEntry;
                
                if (logText != null)
                {
                    logText.text = logData;
                }
                
                // Keep only last 120 lines to prevent memory issues
                string[] lines = logData.Split('\n');
                if (lines.Length > 120)
                {
                    logData = string.Join("\n", lines, lines.Length - 120, 120);
                    if (logText != null) logText.text = logData;
                }
                
                Debug.Log($"NetworkLog: {message}"); // Also log to Unity console
            }
        }
    }
    
    void ProcessIncomingPoseData()
    {
        lock (logLock)
        {
            while (incomingPoseData.Count > 0)
            {
                PoseLandmarkData poseData = incomingPoseData.Dequeue();
                
                // Find the target avatar (enemy = receives network data)
                GameObject targetAvatar = FindTargetEnemyAvatar();
                if (targetAvatar != null)
                {
                    // Apply pose data to the enemy avatar
                    ApplyPoseDataToAvatar(targetAvatar, poseData);
                    AddLogEntry($"📥 Applied pose data to {targetAvatar.name}");
                }
            }
        }
    }
    
    // Public method that MultiplayerNetworkManager can call
    public void ProcessNetworkPoseData(string networkMessage)
    {
        AddLogEntry($"📥 Received network message: {networkMessage.Substring(0, Mathf.Min(50, networkMessage.Length))}...");
        
        PoseLandmarkData poseData = PoseLandmarkData.FromNetworkMessage(networkMessage);
        if (poseData != null)
        {
            lock (logLock)
            {
                incomingPoseData.Enqueue(poseData);
            }
            AddLogEntry($"✅ Parsed pose data with {poseData.landmarkPositions?.Length ?? 0} landmarks");
        }
        else
        {
            AddLogEntry($"❌ Failed to parse pose data from network message");
        }
    }
    
    void ApplyPoseDataToAvatar(GameObject avatar, PoseLandmarkData poseData)
    {
        // This method applies received network data to the enemy avatar
        // Implementation depends on your avatar animation system
        
        Animator animator = avatar.GetComponent<Animator>();
        if (animator != null)
        {
            // Convert pose data to avatar movements
            // This is a simplified example - you may need more complex mapping
            if (poseData.landmarkPositions != null && poseData.landmarkPositions.Length >= 9)
            {
                // Example: Use landmark data to move avatar
                // This would need to be adapted based on your specific avatar system
                Vector3 centerPosition = new Vector3(
                    poseData.landmarkPositions[0],
                    poseData.landmarkPositions[1], 
                    poseData.landmarkPositions[2]
                );
                
                // Apply position with smoothing
                avatar.transform.position = Vector3.Lerp(
                    avatar.transform.position, 
                    centerPosition, 
                    Time.deltaTime * 5f
                );
            }
        }
    }
    
    void SendPlayerPoseData()
    {
        // Only send if we're in multiplayer mode
        if (!isHost && !isClient) return;
        
        // Find the player avatar (the one getting camera input)
        GameObject playerAvatar = GameObject.FindGameObjectWithTag("Player");
        if (playerAvatar == null) return;
        
        // Capture pose data from player avatar
        PoseLandmarkData poseData = CapturePoseDataFromAvatar(playerAvatar);
        if (poseData != null)
        {
            // Send to network (via existing MultiplayerNetworkManager)
            SendPoseDataToNetwork(poseData);
        }
    }
    
    PoseLandmarkData CapturePoseDataFromAvatar(GameObject avatar)
    {
        // This captures the current pose of the player avatar
        // Implementation depends on your avatar tracking system
        
        Animator animator = avatar.GetComponent<Animator>();
        if (animator != null)
        {
            // Create a simplified pose data from avatar position and key joints
            float[] landmarks = new float[99]; // MediaPipe has 33 landmarks * 3 coords
            
            // Fill with avatar position and basic joint positions
            Vector3 position = avatar.transform.position;
            landmarks[0] = position.x;
            landmarks[1] = position.y;
            landmarks[2] = position.z;
            
            // Add more landmark data from animator bones if available
            // This is simplified - you may want to use more detailed bone tracking
            
            return new PoseLandmarkData(landmarks, System.DateTime.Now.Ticks);
        }
        
        return null;
    }
    
    void SendPoseDataToNetwork(PoseLandmarkData poseData)
    {
        // Send pose data through existing networking system
        string networkMessage = poseData.ToNetworkMessage();
        
        // Find the MultiplayerNetworkManager and send data
        MultiplayerNetworkManager networkManager = FindObjectOfType<MultiplayerNetworkManager>();
        if (networkManager != null)
        {
            // Use reflection to access the send method
            var method = networkManager.GetType().GetMethod("SendDataToConnectedPeer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(networkManager, new object[] { networkMessage });
            }
        }
    }
    
    GameObject FindTargetEnemyAvatar()
    {
        // Find avatar with "Enemy" tag (set by Host/Join button clicks)
        GameObject enemyAvatar = GameObject.FindGameObjectWithTag("Enemy");
        if (enemyAvatar != null)
        {
            AddLogEntry($"🎯 Found enemy avatar by tag: {enemyAvatar.name}");
            return enemyAvatar;
        }
        
        AddLogEntry("❌ No enemy avatar found! Make sure to click Host or Join first.");
        return null;
    }
    
    void AddLogEntry(string message)
    {
        // Always log to Unity console
        Debug.Log($"NetworkLog: {message}");
        
        // Queue for UI display if system is ready
        if (logLock != null)
        {
            lock (logLock)
            {
                logQueue.Enqueue(message);
            }
        }
    }
    
    // ==================== UI CREATION ====================
    
    void CreateMultiplayerUI()
    {
        AddLogEntry("🎨 Creating multiplayer UI...");
        
        // Find the main Canvas specifically (not Preview Canvas)
        Canvas canvas = null;
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in canvases)
        {
            if (c.gameObject.name == "Canvas")
            {
                canvas = c;
                break;
            }
        }
        
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            AddLogEntry("✅ Created new Main Canvas");
        }
        else
        {
            AddLogEntry($"✅ Found existing Main Canvas: {canvas.gameObject.name}");
        }
        
        // Create main selection UI panel
        selectionUI = new GameObject("MultiplayerSelectionUI");
        selectionUI.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = selectionUI.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(150, 0); // Move right since log is on left
        panelRect.sizeDelta = new Vector2(400, 300);
        
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
        
        // Create communication log panel
        CreateCommunicationLog();
        
        AddLogEntry("✅ Multiplayer UI created successfully");
    }
    
    void CreateTitle()
    {
        if (selectionUI == null)
        {
            Debug.LogError("CreateTitle: selectionUI is null!");
            return;
        }
        
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(selectionUI.transform, false);
        
        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "Multiplayer Game Mode";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 24;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
    }
    
    void CreateHostButton()
    {
        if (selectionUI == null)
        {
            Debug.LogError("CreateHostButton: selectionUI is null!");
            return;
        }
        
        GameObject hostButtonGO = new GameObject("HostButton");
        hostButtonGO.transform.SetParent(selectionUI.transform, false);
        
        hostButton = hostButtonGO.AddComponent<Button>();
        Image hostButtonImage = hostButtonGO.AddComponent<Image>();
        hostButtonImage.color = new Color(0.2f, 0.7f, 0.2f, 0.8f); // Green
        
        RectTransform hostRect = hostButtonGO.GetComponent<RectTransform>();
        hostRect.anchorMin = new Vector2(0.1f, 0.5f);
        hostRect.anchorMax = new Vector2(0.9f, 0.7f);
        hostRect.offsetMin = Vector2.zero;
        hostRect.offsetMax = Vector2.zero;
        
        GameObject hostTextGO = new GameObject("HostText");
        hostTextGO.transform.SetParent(hostButtonGO.transform, false);
        Text hostText = hostTextGO.AddComponent<Text>();
        hostText.text = "🎮 HOST GAME";
        hostText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hostText.fontSize = 18;
        hostText.color = Color.white;
        hostText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform hostTextRect = hostTextGO.GetComponent<RectTransform>();
        hostTextRect.anchorMin = Vector2.zero;
        hostTextRect.anchorMax = Vector2.one;
        hostTextRect.offsetMin = Vector2.zero;
        hostTextRect.offsetMax = Vector2.zero;
        
        hostButton.onClick.AddListener(OnHostClicked);
    }
    
    void CreateJoinButton()
    {
        if (selectionUI == null)
        {
            Debug.LogError("CreateJoinButton: selectionUI is null!");
            return;
        }
        
        GameObject joinButtonGO = new GameObject("JoinButton");
        joinButtonGO.transform.SetParent(selectionUI.transform, false);
        
        joinButton = joinButtonGO.AddComponent<Button>();
        Image joinButtonImage = joinButtonGO.AddComponent<Image>();
        joinButtonImage.color = new Color(0.2f, 0.2f, 0.7f, 0.8f); // Blue
        
        RectTransform joinRect = joinButtonGO.GetComponent<RectTransform>();
        joinRect.anchorMin = new Vector2(0.1f, 0.3f);
        joinRect.anchorMax = new Vector2(0.9f, 0.5f);
        joinRect.offsetMin = Vector2.zero;
        joinRect.offsetMax = Vector2.zero;
        
        GameObject joinTextGO = new GameObject("JoinText");
        joinTextGO.transform.SetParent(joinButtonGO.transform, false);
        Text joinText = joinTextGO.AddComponent<Text>();
        joinText.text = "🔗 JOIN GAME";
        joinText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        joinText.fontSize = 18;
        joinText.color = Color.white;
        joinText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform joinTextRect = joinTextGO.GetComponent<RectTransform>();
        joinTextRect.anchorMin = Vector2.zero;
        joinTextRect.anchorMax = Vector2.one;
        joinTextRect.offsetMin = Vector2.zero;
        joinTextRect.offsetMax = Vector2.zero;
        
        joinButton.onClick.AddListener(OnJoinClicked);
    }
    
    void CreateStatusText()
    {
        if (selectionUI == null)
        {
            Debug.LogError("CreateStatusText: selectionUI is null!");
            return;
        }
        
        GameObject statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(selectionUI.transform, false);
        
        statusText = statusGO.AddComponent<Text>();
        statusText.text = "Select your game mode";
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 14;
        statusText.color = Color.yellow;
        statusText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.1f);
        statusRect.anchorMax = new Vector2(1, 0.3f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
    }
    
    void CreateCommunicationLog()
    {
        AddLogEntry("📋 Creating communication log panel...");
        
        // Find the main Canvas specifically (not Preview Canvas)
        Canvas canvas = null;
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in canvases)
        {
            if (c.gameObject.name == "Canvas")
            {
                canvas = c;
                break;
            }
        }
        
        if (canvas == null)
        {
            AddLogEntry("❌ ERROR: Could not find main Canvas for log panel!");
            return;
        }
        
        logPanel = new GameObject("CommunicationLogPanel");
        logPanel.transform.SetParent(canvas.transform, false);
        
        // Position in TOP LEFT corner with transparency
        RectTransform logRect = logPanel.AddComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0f, 0.5f);  // Top left anchor
        logRect.anchorMax = new Vector2(0.45f, 1f); // Extend to middle-left area
        logRect.offsetMin = new Vector2(10, 10);    // Small margins
        logRect.offsetMax = new Vector2(-10, -10);
        
        // Semi-transparent dark background (lower opacity to see through)
        Image logBackground = logPanel.AddComponent<Image>();
        logBackground.color = new Color(0.05f, 0.05f, 0.05f, 0.7f); // Reduced alpha from 0.95f to 0.7f
        
        // Create scroll area
        GameObject scrollArea = new GameObject("ScrollArea");
        scrollArea.transform.SetParent(logPanel.transform, false);
        
        RectTransform scrollRect = scrollArea.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(10, 10);
        scrollRect.offsetMax = new Vector2(-10, -10);
        
        logText = scrollArea.AddComponent<Text>();
        logText.text = "📡 Communication Log\n" +
                      "===================\n" + 
                      "System initializing...\n";
        logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? 
                      Resources.GetBuiltinResource<Font>("Arial.ttf");
        logText.fontSize = 10;
        logText.alignment = TextAnchor.UpperLeft;
        logText.color = Color.white;
        logText.verticalOverflow = VerticalWrapMode.Overflow;
        
        // Show the log panel
        logPanel.SetActive(true);
        
        // Start logging system status immediately
        AddLogEntry("📡 Communication system initialized");
        AddLogEntry("🎮 Waiting for mode selection...");
        AddLogEntry($"📍 System ready at {System.DateTime.Now:HH:mm:ss}");
    }
    
    // ==================== BUTTON CLICK HANDLERS ====================
    
    void OnHostClicked()
    {
        Debug.Log("🎮 HOST GAME CLICKED! Starting host mode...");
        
        isHost = true;
        isClient = false;
        
        // Update status text
        if (statusText != null)
        {
            statusText.text = "HOST MODE: Bryce is camera-controlled!";
            statusText.color = Color.green;
        }
        
        // Add immediate log entries
        AddLogEntry("🟢 HOST MODE ACTIVATED");
        AddLogEntry("🎯 Bryce will be camera-controlled (Player)");
        AddLogEntry("🎭 Remy will be network-controlled (Enemy)");
        AddLogEntry("📡 Starting host advertisement...");
        
        // Setup avatars for HOST mode
        SetupAvatarsForHost();
        
        // Start network advertising
        StartHostNetworking();
    }
    
    void OnJoinClicked()
    {
        Debug.Log("🎮 JOIN GAME CLICKED! Starting client mode...");
        
        isHost = false;
        isClient = true;
        
        // Update status text
        if (statusText != null)
        {
            statusText.text = "CLIENT MODE: Remy is camera-controlled!";
            statusText.color = Color.blue;
        }
        
        // Add immediate log entries
        AddLogEntry("🔵 CLIENT MODE ACTIVATED");
        AddLogEntry("🎯 Remy will be camera-controlled (Player)");
        AddLogEntry("🎭 Bryce will be network-controlled (Enemy)");
        AddLogEntry("🔍 Searching for host...");
        
        // Setup avatars for CLIENT mode
        SetupAvatarsForClient();
        
        // Start network discovery
        StartClientNetworking();
    }
    
    void TestBoneDataProcessing()
    {
        AddLogEntry("🧪 Testing bone data processing system...");
        
        // Simulate receiving bone data (76 floats = 19 joints * 4 quaternion components)
        List<float> testBoneData = new List<float>();
        
        // Generate test rotation data for 19 joints
        for (int i = 0; i < 19; i++)
        {
            // Create slight random rotations for testing
            float x = UnityEngine.Random.Range(-0.1f, 0.1f);
            float y = UnityEngine.Random.Range(-0.1f, 0.1f);
            float z = UnityEngine.Random.Range(-0.1f, 0.1f);
            float w = Mathf.Sqrt(1 - (x*x + y*y + z*z)); // Normalize quaternion
            
            testBoneData.Add(x);
            testBoneData.Add(y);
            testBoneData.Add(z);
            testBoneData.Add(w);
        }
        
        // Create test pose data
        PoseLandmarkData testPoseData = new PoseLandmarkData(testBoneData.ToArray(), System.DateTime.Now.Ticks);
        
        AddLogEntry($"📥 SIMULATED: Received {testBoneData.Count} bone rotation values");
        
        // Add to incoming data queue to test the processing system
        lock (logLock)
        {
            incomingPoseData.Enqueue(testPoseData);
        }
        
        AddLogEntry("🔄 Test data added to processing queue");
    }
    
    // ==================== AVATAR CONTROL LOGIC ====================
    
    void SetupAvatarsForHost()
    {
        AddLogEntry("🏠 HOST: Setting up avatar controls...");
        
        // Find avatars in scene
        GameObject remy = GameObject.Find("Remy");
        GameObject bryce = GameObject.Find("Adam_Big"); // Bryce is Adam_Big
        
        if (remy == null || bryce == null)
        {
            AddLogEntry("❌ ERROR: Could not find avatars in scene!");
            return;
        }
        
        // HOST: Bryce gets camera input (Player), Remy gets network input (Enemy)
        if (bryce != null)
        {
            bryce.SetActive(true);
            bryce.tag = "Player";
            
            // Enable pose tracking for Bryce (camera input)
            var bryceTracking = bryce.GetComponent<Tracking4All.Avatar>();
            if (bryceTracking != null)
            {
                bryceTracking.enabled = true;
                AddLogEntry("✅ Bryce Avatar tracking ENABLED (camera input)");
            }
            
            AddLogEntry("🎯 HOST: Bryce set as Player (camera-controlled)");
        }
        
        if (remy != null)
        {
            remy.SetActive(true);
            remy.tag = "Enemy";
            
            // Disable pose tracking for Remy (will receive network data)
            var remyTracking = remy.GetComponent<Tracking4All.Avatar>();
            if (remyTracking != null)
            {
                remyTracking.enabled = false;
                AddLogEntry("🔌 Remy Avatar tracking DISABLED (network input only)");
            }
            
            AddLogEntry("🎭 HOST: Remy set as Enemy (network-controlled)");
        }
        
        // Position camera for Bryce view (like InstantPlayerSelector)
        SetupCameraForPlayer(true); // true = Adam/Bryce
        
        AddLogEntry("✅ HOST avatar setup complete");
    }
    
    void SetupAvatarsForClient()
    {
        AddLogEntry("👥 CLIENT: Setting up avatar controls...");
        
        // Find avatars in scene
        GameObject remy = GameObject.Find("Remy");
        GameObject bryce = GameObject.Find("Adam_Big"); // Bryce is Adam_Big
        
        if (remy == null || bryce == null)
        {
            AddLogEntry("❌ ERROR: Could not find avatars in scene!");
            return;
        }
        
        // CLIENT: Remy gets camera input (Player), Bryce gets network input (Enemy)
        if (remy != null)
        {
            remy.SetActive(true);
            remy.tag = "Player";
            
            // Enable pose tracking for Remy (camera input)
            var remyTracking = remy.GetComponent<Tracking4All.Avatar>();
            if (remyTracking != null)
            {
                remyTracking.enabled = true;
                AddLogEntry("✅ Remy Avatar tracking ENABLED (camera input)");
            }
            
            AddLogEntry("🎯 CLIENT: Remy set as Player (camera-controlled)");
        }
        
        if (bryce != null)
        {
            bryce.SetActive(true);
            bryce.tag = "Enemy";
            
            // Disable pose tracking for Bryce (will receive network data)
            var bryceTracking = bryce.GetComponent<Tracking4All.Avatar>();
            if (bryceTracking != null)
            {
                bryceTracking.enabled = false;
                AddLogEntry("🔌 Bryce Avatar tracking DISABLED (network input only)");
            }
            
            AddLogEntry("🎭 CLIENT: Bryce set as Enemy (network-controlled)");
        }
        
        // Position camera for Remy view (like InstantPlayerSelector)
        SetupCameraForPlayer(false); // false = Remy
        
        AddLogEntry("✅ CLIENT avatar setup complete");
    }
    
    void SetupCameraForPlayer(bool isAdam)
    {
        AddLogEntry($"📷 Setting up camera for {(isAdam ? "Bryce/Adam" : "Remy")}...");
        
        // Find the CameraRig_Adam
        GameObject cameraRigAdam = GameObject.Find("CameraRig_Adam");
        if (cameraRigAdam == null)
        {
            AddLogEntry("❌ CameraRig_Adam not found!");
            return;
        }
        
        if (isAdam)
        {
            // HOST MODE: Bryce/Adam is player - use CameraRig_Adam
            
            // Find the Main Camera Adam inside CameraRig_Adam
            Transform mainCameraAdam = cameraRigAdam.transform.Find("Main Camera Adam");
            if (mainCameraAdam == null)
            {
                AddLogEntry("❌ Main Camera Adam not found inside CameraRig_Adam!");
                return;
            }
            
            // Set HOST camera settings
            mainCameraAdam.localPosition = new Vector3(-2.5f, 0f, 4f);
            mainCameraAdam.localRotation = Quaternion.Euler(25f, 180f, 0f);
            
            // Enable CameraRig_Adam for HOST mode
            cameraRigAdam.SetActive(true);
            AddLogEntry("✅ CameraRig_Adam ENABLED for HOST mode");
            AddLogEntry("📹 Camera set to HOST position: (-2.5, 0, 4) rotation: (25, 180, 0)");
        }
        else
        {
            // CLIENT MODE: Remy is player - disable CameraRig_Adam
            cameraRigAdam.SetActive(false);
            AddLogEntry("🔌 CameraRig_Adam DISABLED for CLIENT mode");
            AddLogEntry("📹 Using main scene camera for Remy view");
        }
    }
    
    // ==================== NETWORKING LOGIC ====================
    
    void StartHostNetworking()
    {
        AddLogEntry("📡 Starting HOST networking...");
        
        // Find and use existing MultiplayerNetworkManager
        MultiplayerNetworkManager networkManager = FindObjectOfType<MultiplayerNetworkManager>();
        if (networkManager != null)
        {
            // Use reflection to call the private StartHostMode method
            var method = networkManager.GetType().GetMethod("StartHostMode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(networkManager, null);
                AddLogEntry("✅ HOST networking started via MultiplayerNetworkManager");
                AddLogEntry("📻 Broadcasting for clients to connect...");
                AddLogEntry("📤 Bryce movements will be sent to clients");
            }
            else
            {
                AddLogEntry("❌ Could not find StartHostMode method");
            }
        }
        else
        {
            AddLogEntry("❌ MultiplayerNetworkManager not found!");
            AddLogEntry("📋 Please ensure MultiplayerNetworkManager is in the scene");
        }
    }
    
    void StartClientNetworking()
    {
        AddLogEntry("🔍 Starting CLIENT networking...");
        
        // Find and use existing MultiplayerNetworkManager
        MultiplayerNetworkManager networkManager = FindObjectOfType<MultiplayerNetworkManager>();
        if (networkManager != null)
        {
            // Use reflection to call the private StartClientMode method
            var method = networkManager.GetType().GetMethod("StartClientMode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(networkManager, null);
                AddLogEntry("✅ CLIENT networking started via MultiplayerNetworkManager");
                AddLogEntry("🔍 Scanning for host devices...");
                AddLogEntry("📤 Remy movements will be sent to host");
            }
            else
            {
                AddLogEntry("❌ Could not find StartClientMode method");
            }
        }
        else
        {
            AddLogEntry("❌ MultiplayerNetworkManager not found!");
            AddLogEntry("📋 Please ensure MultiplayerNetworkManager is in the scene");
        }
    }
}
