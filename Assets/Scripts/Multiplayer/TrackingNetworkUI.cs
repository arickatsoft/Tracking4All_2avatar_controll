using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Complete UI and setup manager for your Tracking4All Unity Netcode multiplayer system
    /// This handles all UI interactions, network setup, and status monitoring
    /// </summary>
    public class TrackingNetworkUI : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private InputField ipAddressField;
        [SerializeField] private Text statusText;
        [SerializeField] private Text networkInfoText;
        
        [Header("Network Configuration")]
        [SerializeField] private GameObject networkManagerPrefab; // Optional: prefab with NetworkManager
        [SerializeField] private string defaultIP = "127.0.0.1";
        [SerializeField] private int defaultPort = 7777;
        
        [Header("Auto-Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool debugMode = true;
        
        private NetworkManager networkManager;
        private CompleteTrackingNetworkSync trackingSync;
        private bool isNetworkActive = false;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                StartCoroutine(AutoSetupDelay());
            }
        }
        
        private IEnumerator AutoSetupDelay()
        {
            // Wait for Unity to finish initializing
            yield return new WaitForSeconds(0.5f);
            
            InitializeUI();
            SetupNetworkManager();
            FindOrCreateNetworkObjects();
            
            UpdateStatusText("Ready for multiplayer");
        }
        
        private void InitializeUI()
        {
            // Setup button listeners
            if (hostButton != null)
            {
                hostButton.onClick.RemoveAllListeners();
                hostButton.onClick.AddListener(StartHost);
            }
                
            if (clientButton != null)
            {
                clientButton.onClick.RemoveAllListeners();
                clientButton.onClick.AddListener(StartClient);
            }
                
            if (disconnectButton != null)
            {
                disconnectButton.onClick.RemoveAllListeners();
                disconnectButton.onClick.AddListener(Disconnect);
                disconnectButton.interactable = false;
            }
            
            // Setup IP address field
            if (ipAddressField != null)
            {
                ipAddressField.text = defaultIP;
            }
            
            if (debugMode)
                UnityEngine.Debug.Log("TrackingNetworkUI: UI initialized");
        }
        
        private void SetupNetworkManager()
        {
            // Find existing NetworkManager or create one
            networkManager = FindObjectOfType<NetworkManager>();
            
            if (networkManager == null)
            {
                if (networkManagerPrefab != null)
                {
                    var networkManagerGO = Instantiate(networkManagerPrefab);
                    networkManager = networkManagerGO.GetComponent<NetworkManager>();
                }
                else
                {
                    // Create basic NetworkManager
                    var networkManagerGO = new GameObject("NetworkManager");
                    networkManager = networkManagerGO.AddComponent<NetworkManager>();
                }
                
                if (debugMode)
                    UnityEngine.Debug.Log("TrackingNetworkUI: Created NetworkManager");
            }
            
            // Configure transport if available
            var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Port = (ushort)defaultPort;
            }
            
            // Subscribe to network events
            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        private void FindOrCreateNetworkObjects()
        {
            trackingSync = FindObjectOfType<CompleteTrackingNetworkSync>();
            
            if (trackingSync != null)
            {
                if (debugMode)
                    UnityEngine.Debug.Log($"TrackingNetworkUI: Found existing CompleteTrackingNetworkSync");
            }
            else
            {
                if (debugMode)
                    UnityEngine.Debug.LogWarning("TrackingNetworkUI: No CompleteTrackingNetworkSync found. Please create NetworkObject prefab with this component.");
            }
        }
        
        public void StartHost()
        {
            if (networkManager == null) return;
            
            UpdateStatusText("Starting Host...");
            UpdateButtonStates(false, false, false);
            
            if (debugMode)
                UnityEngine.Debug.Log("TrackingNetworkUI: Starting as Host");
                
            networkManager.StartHost();
        }
        
        public void StartClient()
        {
            if (networkManager == null) return;
            
            // Configure connection address
            if (ipAddressField != null && !string.IsNullOrEmpty(ipAddressField.text))
            {
                var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Address = ipAddressField.text;
                    
                    if (debugMode)
                        UnityEngine.Debug.Log($"TrackingNetworkUI: Connecting to {ipAddressField.text}");
                }
            }
            
            UpdateStatusText($"Connecting to {ipAddressField?.text ?? defaultIP}...");
            UpdateButtonStates(false, false, false);
            
            networkManager.StartClient();
        }
        
        public void Disconnect()
        {
            if (networkManager == null) return;
            
            UpdateStatusText("Disconnecting...");
            
            if (debugMode)
                UnityEngine.Debug.Log("TrackingNetworkUI: Disconnecting from network");
                
            networkManager.Shutdown();
            
            isNetworkActive = false;
            UpdateButtonStates(true, true, false);
        }
        
        private void OnServerStarted()
        {
            isNetworkActive = true;
            UpdateStatusText("HOST: Server started - Waiting for clients");
            UpdateButtonStates(false, false, true);
            
            if (debugMode)
                UnityEngine.Debug.Log("TrackingNetworkUI: Host server started successfully");
        }
        
        private void OnClientConnected(ulong clientId)
        {
            if (networkManager.IsHost)
            {
                UpdateStatusText($"HOST: Client {clientId} connected");
                
                if (debugMode)
                    UnityEngine.Debug.Log($"TrackingNetworkUI: Client {clientId} connected to host");
            }
            else if (clientId == networkManager.LocalClientId)
            {
                isNetworkActive = true;
                UpdateStatusText("CLIENT: Connected to host");
                UpdateButtonStates(false, false, true);
                
                if (debugMode)
                    UnityEngine.Debug.Log("TrackingNetworkUI: Successfully connected as client");
            }
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            if (networkManager.IsHost)
            {
                UpdateStatusText($"HOST: Client {clientId} disconnected");
                
                if (debugMode)
                    UnityEngine.Debug.Log($"TrackingNetworkUI: Client {clientId} disconnected from host");
            }
            else if (clientId == networkManager.LocalClientId)
            {
                isNetworkActive = false;
                UpdateStatusText("CLIENT: Disconnected from host");
                UpdateButtonStates(true, true, false);
                
                if (debugMode)
                    UnityEngine.Debug.Log("TrackingNetworkUI: Disconnected from host as client");
            }
        }
        
        private void UpdateButtonStates(bool hostEnabled, bool clientEnabled, bool disconnectEnabled)
        {
            if (hostButton != null) hostButton.interactable = hostEnabled;
            if (clientButton != null) clientButton.interactable = clientEnabled;
            if (disconnectButton != null) disconnectButton.interactable = disconnectEnabled;
            if (ipAddressField != null) ipAddressField.interactable = hostEnabled || clientEnabled;
        }
        
        private void UpdateStatusText(string message)
        {
            string timestampedMessage = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            
            if (statusText != null)
                statusText.text = timestampedMessage;
                
            if (debugMode)
                UnityEngine.Debug.Log($"TrackingNetworkUI: {message}");
        }
        
        private void Update()
        {
            UpdateNetworkInfo();
        }
        
        private void UpdateNetworkInfo()
        {
            if (networkInfoText == null || networkManager == null) return;
            
            if (!isNetworkActive)
            {
                networkInfoText.text = "Network: Inactive";
                return;
            }
            
            string info = "";
            
            if (networkManager.IsHost)
            {
                int clientCount = networkManager.ConnectedClients?.Count ?? 0;
                info = $"HOST Mode\\nClients: {clientCount}";
                
                if (trackingSync != null)
                {
                    info += $"\\nTracking Status: {trackingSync.GetDetailedStatus()}";
                }
            }
            else if (networkManager.IsClient)
            {
                info = "CLIENT Mode\\nConnected to Host";
                
                if (trackingSync != null)
                {
                    bool receiving = trackingSync.IsReceivingData();
                    info += $"\\nReceiving Data: {(receiving ? "Yes" : "No")}";
                    info += $"\\nTracking Status: {trackingSync.GetDetailedStatus()}";
                }
            }
            
            networkInfoText.text = info;
        }
        
        // Public API for external monitoring
        public bool IsNetworkActive() => isNetworkActive;
        public bool IsHost() => networkManager != null && networkManager.IsHost;
        public bool IsClient() => networkManager != null && networkManager.IsClient && !networkManager.IsHost;
        public int GetConnectedClientCount() => networkManager?.ConnectedClients?.Count ?? 0;
        
        // Context menu for testing
        [ContextMenu("Test Host")]
        public void TestHost() => StartHost();
        
        [ContextMenu("Test Client")]  
        public void TestClient() => StartClient();
        
        [ContextMenu("Test Disconnect")]
        public void TestDisconnect() => Disconnect();
        
        [ContextMenu("Show Network Status")]
        public void ShowNetworkStatus()
        {
            UnityEngine.Debug.Log("=== NETWORK STATUS ===");
            UnityEngine.Debug.Log($"Network Active: {IsNetworkActive()}");
            UnityEngine.Debug.Log($"Role: {(IsHost() ? "HOST" : IsClient() ? "CLIENT" : "NONE")}");
            UnityEngine.Debug.Log($"Connected Clients: {GetConnectedClientCount()}");
            
            if (trackingSync != null)
            {
                UnityEngine.Debug.Log($"Tracking Sync: {trackingSync.GetDetailedStatus()}");
                UnityEngine.Debug.Log($"Receiving Data: {trackingSync.IsReceivingData()}");
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup event subscriptions
            if (networkManager != null)
            {
                networkManager.OnServerStarted -= OnServerStarted;
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }
    }
}
