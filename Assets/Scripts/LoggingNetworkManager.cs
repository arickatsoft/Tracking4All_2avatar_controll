using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Enhanced Network Manager that provides comprehensive logging for network operations.
    /// Handles connection events, network status, and integration with the logging system.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class LoggingNetworkManager : MonoBehaviour
    {
        [Header("Network Configuration")]
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private ushort serverPort = 7777;
        [SerializeField] private bool autoStartLogging = true;
        
        [Header("Connection Settings")]
        [SerializeField] private float connectionTimeout = 10f;
        [SerializeField] private int maxConnections = 4;
        
        private NetworkManager networkManager;
        private UnityTransport unityTransport;
        
        private void Awake()
        {
            InitializeNetworkComponents();
            SetupNetworkCallbacks();
        }
        
        private void Start()
        {
            if (autoStartLogging)
            {
                NetworkLogDisplay.LogNetwork("NETWORK", "LoggingNetworkManager initialized");
                LogNetworkConfiguration();
            }
        }
        
        private void InitializeNetworkComponents()
        {
            networkManager = GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                UnityEngine.Debug.LogError("[LoggingNetworkManager] NetworkManager component not found!");
                return;
            }
            
            // Get or add Unity Transport
            unityTransport = networkManager.GetComponent<UnityTransport>();
            if (unityTransport == null)
            {
                unityTransport = networkManager.gameObject.AddComponent<UnityTransport>();
                networkManager.NetworkConfig.NetworkTransport = unityTransport;
            }
            
            // Configure transport settings
            unityTransport.ConnectionData.Address = serverIP;
            unityTransport.ConnectionData.Port = serverPort;
            unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0"; // Listen on all interfaces
        }
        
        private void SetupNetworkCallbacks()
        {
            if (networkManager == null) return;
            
            // Subscribe to network events
            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.OnTransportFailure += OnTransportFailure;
        }
        
        private void LogNetworkConfiguration()
        {
            if (networkManager == null) return;
            
            NetworkLogDisplay.LogNetwork("CONFIG", $"Server IP: {serverIP}:{serverPort}");
            NetworkLogDisplay.LogNetwork("CONFIG", $"Max Connections: {maxConnections}");
            NetworkLogDisplay.LogNetwork("CONFIG", $"Connection Timeout: {connectionTimeout}s");
            NetworkLogDisplay.LogNetwork("CONFIG", $"Transport Type: {unityTransport?.GetType().Name ?? "Unknown"}");
        }
        
        private void OnServerStarted()
        {
            NetworkLogDisplay.LogHost("🎉 Server started successfully!");
            NetworkLogDisplay.LogNetwork("STATUS", $"Listening on {serverIP}:{serverPort}");
            NetworkLogDisplay.LogNetwork("STATUS", $"Ready to accept {maxConnections} clients");
            
            // Start periodic status logging
            InvokeRepeating(nameof(LogServerStatus), 5f, 10f);
        }
        
        private void OnClientConnected(ulong clientId)
        {
            if (IsHost)
            {
                NetworkLogDisplay.LogHost($"✅ Client {clientId} connected");
                NetworkLogDisplay.LogNetwork("CONNECTION", $"Active clients: {networkManager.ConnectedClients.Count}");
            }
            else
            {
                NetworkLogDisplay.LogClient($"✅ Connected to server as client {clientId}");
                NetworkLogDisplay.LogNetwork("STATUS", "Connection established successfully");
            }
            
            // Cancel connection timeout if this is our first connection as client
            if (!IsHost && clientId == networkManager.LocalClientId)
            {
                CancelInvoke(nameof(LogConnectionAttempt));
            }
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            if (IsHost)
            {
                NetworkLogDisplay.LogHost($"❌ Client {clientId} disconnected");
                NetworkLogDisplay.LogNetwork("CONNECTION", $"Remaining clients: {networkManager.ConnectedClients.Count - 1}");
            }
            else
            {
                NetworkLogDisplay.LogClient("❌ Disconnected from server");
                NetworkLogDisplay.LogNetwork("STATUS", "Connection lost");
            }
        }
        
        private void OnTransportFailure()
        {
            NetworkLogDisplay.LogNetwork("ERROR", "Transport failure detected!", true);
            
            if (IsHost)
            {
                NetworkLogDisplay.LogHost("Server transport failed - attempting restart...");
            }
            else
            {
                NetworkLogDisplay.LogClient("Client transport failed - connection lost");
            }
        }
        
        private void LogServerStatus()
        {
            if (!IsHost) return;
            
            int connectedClients = networkManager.ConnectedClients.Count;
            NetworkLogDisplay.LogHost($"Server Status: {connectedClients} clients connected");
        }
        
        private void LogConnectionAttempt()
        {
            if (IsClient && !networkManager.IsConnectedClient)
            {
                NetworkLogDisplay.LogClient("Still attempting to connect...");
            }
        }
        
        public bool StartHost()
        {
            NetworkLogDisplay.LogHost("Attempting to start host...");
            
            try
            {
                bool success = networkManager.StartHost();
                
                if (success)
                {
                    NetworkLogDisplay.LogHost("Host start initiated successfully");
                }
                else
                {
                    NetworkLogDisplay.LogNetwork("ERROR", "Failed to start host", true);
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                NetworkLogDisplay.LogNetwork("ERROR", $"Exception starting host: {ex.Message}", true);
                return false;
            }
        }
        
        public bool StartClient()
        {
            NetworkLogDisplay.LogClient($"Attempting to connect to {serverIP}:{serverPort}...");
            
            try
            {
                bool success = networkManager.StartClient();
                
                if (success)
                {
                    NetworkLogDisplay.LogClient("Client start initiated successfully");
                    
                    // Start connection timeout monitoring
                    InvokeRepeating(nameof(LogConnectionAttempt), 1f, 2f);
                    Invoke(nameof(CheckConnectionTimeout), connectionTimeout);
                }
                else
                {
                    NetworkLogDisplay.LogNetwork("ERROR", "Failed to start client", true);
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                NetworkLogDisplay.LogNetwork("ERROR", $"Exception starting client: {ex.Message}", true);
                return false;
            }
        }
        
        public void Shutdown()
        {
            NetworkLogDisplay.LogNetwork("SHUTDOWN", "Shutting down network...");
            
            CancelInvoke(); // Cancel all periodic logging
            
            if (networkManager.IsHost)
            {
                NetworkLogDisplay.LogHost("Shutting down host");
            }
            else if (networkManager.IsClient)
            {
                NetworkLogDisplay.LogClient("Disconnecting from server");
            }
            
            networkManager.Shutdown();
            NetworkLogDisplay.LogNetwork("SHUTDOWN", "Network shutdown complete");
        }
        
        private void CheckConnectionTimeout()
        {
            if (IsClient && !networkManager.IsConnectedClient)
            {
                NetworkLogDisplay.LogNetwork("ERROR", $"Connection timeout after {connectionTimeout}s", true);
                NetworkLogDisplay.LogClient("Failed to connect - timeout");
                
                CancelInvoke(nameof(LogConnectionAttempt));
            }
        }
        
        public bool IsHost => networkManager != null && networkManager.IsHost;
        public bool IsClient => networkManager != null && networkManager.IsClient;
        public bool IsConnected => networkManager != null && (networkManager.IsHost || networkManager.IsConnectedClient);
        
        private void OnDestroy()
        {
            // Clean up callbacks
            if (networkManager != null)
            {
                networkManager.OnServerStarted -= OnServerStarted;
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                networkManager.OnTransportFailure -= OnTransportFailure;
            }
            
            CancelInvoke(); // Cancel all periodic operations
        }
    }
}