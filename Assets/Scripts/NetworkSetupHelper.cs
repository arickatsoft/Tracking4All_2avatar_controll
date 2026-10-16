using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Automatically sets up the scene for proper network communication logging.
    /// Ensures all required components are present and configured correctly.
    /// </summary>
    public class NetworkSetupHelper : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private bool createNetworkLogDisplay = true;
        [SerializeField] private bool setupNetworkManager = true;
        [SerializeField] private bool setupAvatarControllers = true;
        
        [Header("Network Settings")]
        [SerializeField] private string defaultIP = "127.0.0.1";
        [SerializeField] private ushort defaultPort = 7777;
        
        private void Awake()
        {
            if (setupOnAwake)
            {
                SetupNetworkComponents();
            }
        }
        
        [ContextMenu("Setup Network Components")]
        public void SetupNetworkComponents()
        {
            UnityEngine.Debug.Log("[NetworkSetupHelper] Starting network setup...");
            
            if (createNetworkLogDisplay)
            {
                SetupNetworkLogDisplay();
            }
            
            if (setupNetworkManager)
            {
                SetupNetworkManagerComponent();
            }
            
            if (setupAvatarControllers)
            {
                SetupAvatarNetworkControllers();
            }
            
            UnityEngine.Debug.Log("[NetworkSetupHelper] Network setup complete!");
        }
        
        private void SetupNetworkLogDisplay()
        {
            // Check if NetworkLogDisplay already exists
            NetworkLogDisplay existingDisplay = FindObjectOfType<NetworkLogDisplay>();
            if (existingDisplay != null)
            {
                UnityEngine.Debug.Log("[NetworkSetupHelper] NetworkLogDisplay already exists");
                return;
            }
            
            // Create NetworkLogDisplay GameObject
            GameObject logDisplayObj = new GameObject("NetworkLogDisplay");
            NetworkLogDisplay logDisplay = logDisplayObj.AddComponent<NetworkLogDisplay>();
            
            // Make it persistent across scenes
            DontDestroyOnLoad(logDisplayObj);
            
            UnityEngine.Debug.Log("[NetworkSetupHelper] NetworkLogDisplay created successfully");
        }
        
        private void SetupNetworkManagerComponent()
        {
            // Find or create NetworkManager
            NetworkManager networkManager = FindObjectOfType<NetworkManager>();
            
            if (networkManager == null)
            {
                GameObject networkObj = new GameObject("NetworkManager");
                networkManager = networkObj.AddComponent<NetworkManager>();
                DontDestroyOnLoad(networkObj);
                UnityEngine.Debug.Log("[NetworkSetupHelper] NetworkManager created");
            }
            
            // Add LoggingNetworkManager if not present
            LoggingNetworkManager loggingManager = networkManager.GetComponent<LoggingNetworkManager>();
            if (loggingManager == null)
            {
                loggingManager = networkManager.gameObject.AddComponent<LoggingNetworkManager>();
                UnityEngine.Debug.Log("[NetworkSetupHelper] LoggingNetworkManager added");
            }
            
            // Set up Unity Transport
            UnityTransport transport = networkManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                transport = networkManager.gameObject.AddComponent<UnityTransport>();
                networkManager.NetworkConfig.NetworkTransport = transport;
            }
            
            // Configure transport settings
            transport.ConnectionData.Address = defaultIP;
            transport.ConnectionData.Port = defaultPort;
            transport.ConnectionData.ServerListenAddress = "0.0.0.0";
            
            UnityEngine.Debug.Log($"[NetworkSetupHelper] Network transport configured: {defaultIP}:{defaultPort}");
        }
        
        private void SetupAvatarNetworkControllers()
        {
            // Find Adam and Remy avatars
            GameObject adamAvatar = GameObject.FindGameObjectWithTag("Enemy"); // Adam has Enemy tag
            GameObject remyAvatar = GameObject.FindGameObjectWithTag("Player"); // Remy has Player tag
            
            if (adamAvatar == null)
            {
                adamAvatar = GameObject.Find("Adam_Big");
            }
            
            if (remyAvatar == null)
            {
                remyAvatar = GameObject.Find("Remy");
            }
            
            // Setup Adam avatar controller
            if (adamAvatar != null)
            {
                SetupAvatarController(adamAvatar, "Adam", false); // Adam is remote for most clients
            }
            else
            {
                UnityEngine.Debug.LogWarning("[NetworkSetupHelper] Adam avatar not found!");
            }
            
            // Setup Remy avatar controller
            if (remyAvatar != null)
            {
                SetupAvatarController(remyAvatar, "Remy", true); // Remy is usually local
            }
            else
            {
                UnityEngine.Debug.LogWarning("[NetworkSetupHelper] Remy avatar not found!");
            }
        }
        
        private void SetupAvatarController(GameObject avatar, string avatarName, bool isLocalByDefault)
        {
            // Add NetworkObject if not present
            NetworkObject networkObject = avatar.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = avatar.AddComponent<NetworkObject>();
            }
            
            // Add AvatarNetworkController if not present
            AvatarNetworkController controller = avatar.GetComponent<AvatarNetworkController>();
            if (controller == null)
            {
                controller = avatar.AddComponent<AvatarNetworkController>();
            }
            
            UnityEngine.Debug.Log($"[NetworkSetupHelper] {avatarName} avatar controller setup complete");
        }
        
        /// <summary>
        /// Manual setup method for testing
        /// </summary>
        [ContextMenu("Force Complete Setup")]
        public void ForceCompleteSetup()
        {
            SetupNetworkComponents();
            
            // Additional validation
            ValidateSetup();
        }
        
        private void ValidateSetup()
        {
            UnityEngine.Debug.Log("=== Network Setup Validation ===");
            
            // Check NetworkLogDisplay
            NetworkLogDisplay logDisplay = FindObjectOfType<NetworkLogDisplay>();
            UnityEngine.Debug.Log($"NetworkLogDisplay: {(logDisplay != null ? "✅ Found" : "❌ Missing")}");
            
            // Check NetworkManager
            NetworkManager networkManager = FindObjectOfType<NetworkManager>();
            UnityEngine.Debug.Log($"NetworkManager: {(networkManager != null ? "✅ Found" : "❌ Missing")}");
            
            if (networkManager != null)
            {
                LoggingNetworkManager loggingManager = networkManager.GetComponent<LoggingNetworkManager>();
                UnityEngine.Debug.Log($"LoggingNetworkManager: {(loggingManager != null ? "✅ Found" : "❌ Missing")}");
                
                UnityTransport transport = networkManager.GetComponent<UnityTransport>();
                UnityEngine.Debug.Log($"UnityTransport: {(transport != null ? "✅ Found" : "❌ Missing")}");
            }
            
            // Check avatars
            GameObject adamAvatar = GameObject.FindGameObjectWithTag("Enemy");
            GameObject remyAvatar = GameObject.FindGameObjectWithTag("Player");
            
            UnityEngine.Debug.Log($"Adam Avatar: {(adamAvatar != null ? "✅ Found" : "❌ Missing")}");
            UnityEngine.Debug.Log($"Remy Avatar: {(remyAvatar != null ? "✅ Found" : "❌ Missing")}");
            
            UnityEngine.Debug.Log("=== Validation Complete ===");
        }
    }
}