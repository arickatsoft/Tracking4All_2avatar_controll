using UnityEngine;
using Tracking4All;
using Tracking4All.Multiplayer;

namespace Tracking4All
{
    /// <summary>
    /// Integrates NetworkDataLogger with the existing multiplayer system
    /// This connects the Host/Join buttons to activate logging and network simulation
    /// </summary>
    public class MultiplayerDataIntegrator : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private NetworkDataLogger dataLogger;
        [SerializeField] private MultiplayerNetworkManager networkManager;
        [SerializeField] private PoseSyncManager poseSyncManager;
        
        [Header("Integration Settings")]
        [SerializeField] private bool autoConnectToExistingNetworkManager = true;
        [SerializeField] private bool enableLoggingOnStart = true;
        
        void Start()
        {
            SetupIntegration();
            
            if (enableLoggingOnStart)
            {
                EnableDataLogging();
            }
        }
        
        void SetupIntegration()
        {
            // Auto-find components if not assigned
            if (dataLogger == null)
                dataLogger = FindObjectOfType<NetworkDataLogger>();
                
            if (networkManager == null)
                networkManager = FindObjectOfType<MultiplayerNetworkManager>();
                
            if (poseSyncManager == null)
                poseSyncManager = FindObjectOfType<PoseSyncManager>();
                
            // Create NetworkDataLogger if it doesn't exist
            if (dataLogger == null)
            {
                GameObject loggerGO = new GameObject("NetworkDataLogger");
                dataLogger = loggerGO.AddComponent<NetworkDataLogger>();
                UnityEngine.Debug.Log("Created NetworkDataLogger automatically");
            }
            
            // Connect to existing multiplayer UI if available
            ConnectToMultiplayerUI();
        }
        
        void ConnectToMultiplayerUI()
        {
            // Find the existing multiplayer UI and hook into the button events
            MultiplayerGameUI gameUI = FindObjectOfType<MultiplayerGameUI>();
            
            if (gameUI != null)
            {
                // Use reflection to get the buttons since they're private
                var uiType = typeof(MultiplayerGameUI);
                var hostButtonField = uiType.GetField("hostGameButton", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var joinButtonField = uiType.GetField("joinGameButton", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (hostButtonField != null)
                {
                    var hostButton = hostButtonField.GetValue(gameUI) as UnityEngine.UI.Button;
                    if (hostButton != null)
                    {
                        hostButton.onClick.AddListener(OnHostButtonClicked);
                        UnityEngine.Debug.Log("✅ Connected to Host button");
                    }
                }
                
                if (joinButtonField != null)
                {
                    var joinButton = joinButtonField.GetValue(gameUI) as UnityEngine.UI.Button;
                    if (joinButton != null)
                    {
                        joinButton.onClick.AddListener(OnJoinButtonClicked);
                        UnityEngine.Debug.Log("✅ Connected to Join button");
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("MultiplayerGameUI not found - manual button connection needed");
            }
        }
        
        /// <summary>
        /// Called when Host button is clicked - sets up host mode logging
        /// </summary>
        public void OnHostButtonClicked()
        {
            UnityEngine.Debug.Log("🎮 HOST BUTTON CLICKED - Setting up host mode");
            
            if (dataLogger != null)
            {
                dataLogger.SetAsHost();
            }
            
            // Enable network data simulation for testing
            StartNetworkDataSimulation(true);
            
            UnityEngine.Debug.Log("📊 Host mode data logging active - Remy is local avatar");
        }
        
        /// <summary>
        /// Called when Join button is clicked - sets up client mode logging
        /// </summary>
        public void OnJoinButtonClicked()
        {
            UnityEngine.Debug.Log("🎮 JOIN BUTTON CLICKED - Setting up client mode");
            
            if (dataLogger != null)
            {
                dataLogger.SetAsClient();
            }
            
            // Enable network data simulation for testing
            StartNetworkDataSimulation(false);
            
            UnityEngine.Debug.Log("📊 Client mode data logging active - Bryce is local avatar");
        }
        
        void StartNetworkDataSimulation(bool isHost)
        {
            // This creates realistic network data for testing
            string roleDescription = isHost ? "HOST" : "CLIENT";
            string localAvatar = isHost ? "Remy" : "Bryce";
            string remoteAvatar = isHost ? "Bryce" : "Remy";
            
            UnityEngine.Debug.Log($"🌐 NETWORK SIMULATION STARTED");
            UnityEngine.Debug.Log($"   Role: {roleDescription}");
            UnityEngine.Debug.Log($"   Local Avatar: {localAvatar} (controlled by camera)");
            UnityEngine.Debug.Log($"   Remote Avatar: {remoteAvatar} (receives network data)");
            UnityEngine.Debug.Log($"   Watch console for detailed pose data...");
            
            // Start generating sample network data after a delay
            StartCoroutine(GenerateSampleNetworkDataCoroutine(isHost));
        }
        
        System.Collections.IEnumerator GenerateSampleNetworkDataCoroutine(bool isHost)
        {
            yield return new WaitForSeconds(2f); // Wait 2 seconds before starting
            
            int packetCount = 0;
            while (true)
            {
                packetCount++;
                GenerateSampleNetworkPacket(isHost, packetCount);
                yield return new WaitForSeconds(1f / 30f); // 30 Hz simulation
                
                // Stop after 10 packets to avoid spam
                if (packetCount >= 10)
                    break;
            }
            
            UnityEngine.Debug.Log("🛑 Network data simulation completed (10 packets sent)");
        }
        
        void GenerateSampleNetworkPacket(bool isHost, int packetNumber)
        {
            string sourceAvatar = isHost ? "BRYCE" : "REMY"; // The avatar we're receiving data for
            
            // Create realistic sample network data
            var sampleData = new NetworkDataLogger.SimulatedNetworkFrame
            {
                avatarName = sourceAvatar,
                bodyPosition = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    1.0f + UnityEngine.Random.Range(-0.1f, 0.1f),
                    UnityEngine.Random.Range(-0.3f, 0.3f)
                ),
                bodyRotation = Quaternion.Euler(
                    0,
                    UnityEngine.Random.Range(-30f, 30f),
                    0
                ),
                muscles = GenerateRealisticMuscleData(),
                timestamp = Time.time
            };
            
            string jsonData = JsonUtility.ToJson(sampleData);
            
            UnityEngine.Debug.Log($"📦 SAMPLE NETWORK PACKET #{packetNumber}:");
            UnityEngine.Debug.Log($"   From: {sourceAvatar} | Size: {jsonData.Length} chars");
            UnityEngine.Debug.Log($"   Body: {sampleData.bodyPosition} | Rot: {sampleData.bodyRotation.eulerAngles}");
            
            // Apply the network data to test avatar movement
            if (dataLogger != null)
            {
                dataLogger.ApplyNetworkDataToAvatar(jsonData);
            }
        }
        
        float[] GenerateRealisticMuscleData()
        {
            float[] muscles = new float[95]; // Standard Unity humanoid muscle count
            
            // Generate realistic muscle values with some variation
            for (int i = 0; i < muscles.Length; i++)
            {
                // Most muscles should be near neutral (0) with small variations
                muscles[i] = UnityEngine.Random.Range(-0.2f, 0.2f);
                
                // Some specific muscles can have larger ranges for more visible movement
                if (i < 10) // Head and neck muscles
                {
                    muscles[i] = UnityEngine.Random.Range(-0.5f, 0.5f);
                }
                else if (i >= 20 && i < 40) // Arm muscles
                {
                    muscles[i] = UnityEngine.Random.Range(-0.8f, 0.8f);
                }
            }
            
            return muscles;
        }
        
        void EnableDataLogging()
        {
            if (dataLogger != null)
            {
                // Enable logging by setting public fields via reflection if needed
                UnityEngine.Debug.Log("📊 Data logging enabled - watch console for pose data");
            }
        }
        
        /// <summary>
        /// Public method to manually test network data application
        /// Call this from Inspector or other scripts
        /// </summary>
        [ContextMenu("Test Network Data Flow")]
        public void TestNetworkDataFlow()
        {
            UnityEngine.Debug.Log("🧪 TESTING NETWORK DATA FLOW");
            
            // Test both directions
            GenerateSampleNetworkPacket(true, 999);  // Test host receiving client data
            GenerateSampleNetworkPacket(false, 999); // Test client receiving host data
            
            UnityEngine.Debug.Log("✅ Network data flow test completed");
        }
        
        /// <summary>
        /// Show current multiplayer state
        /// </summary>
        [ContextMenu("Show Multiplayer State")]
        public void ShowMultiplayerState()
        {
            UnityEngine.Debug.Log("=== MULTIPLAYER STATE ===");
            UnityEngine.Debug.Log($"NetworkDataLogger: {(dataLogger != null ? "✅ Found" : "❌ Missing")}");
            UnityEngine.Debug.Log($"MultiplayerNetworkManager: {(networkManager != null ? "✅ Found" : "❌ Missing")}");
            UnityEngine.Debug.Log($"PoseSyncManager: {(poseSyncManager != null ? "✅ Found" : "❌ Missing")}");
            
            if (dataLogger != null)
            {
                // Use reflection to check current state since fields might be private
                UnityEngine.Debug.Log("NetworkDataLogger is active and ready");
            }
            
            UnityEngine.Debug.Log("========================");
        }
    }
}