using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Complete Unity Netcode setup and management for Tracking4All multiplayer
    /// This script handles everything: UI, network setup, and pose synchronization
    /// </summary>
    public class MultiplayerSetupManager : MonoBehaviour
    {
        [Header("UI Controls")]
        public Button hostButton;
        public Button clientButton;
        public Button disconnectButton;
        public Text statusText;
        public InputField ipAddressField;
        
        [Header("Tracking4All References")]
        public GameObject poseSolution;
        public GameObject avatar;
        public GameObject puppet;
        
        [Header("Network Settings")]
        public float poseUpdateRate = 30f;
        public bool debugLogs = true;
        
        private NetworkManager networkManager;
        private bool isNetworkActive = false;
        
        private void Start()
        {
            InitializeUI();
            FindNetworkManager();
            AutoAssignTracking4AllComponents();
        }
        
        private void InitializeUI()
        {
            if (hostButton != null)
                hostButton.onClick.AddListener(StartHost);
                
            if (clientButton != null) 
                clientButton.onClick.AddListener(StartClient);
                
            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(Disconnect);
                disconnectButton.interactable = false;
            }
            
            if (ipAddressField != null)
                ipAddressField.text = "127.0.0.1"; // Default localhost
                
            UpdateStatusText("Ready to connect");
        }
        
        private void FindNetworkManager()
        {
            networkManager = FindObjectOfType<NetworkManager>();
            
            if (networkManager == null)
            {
                UnityEngine.Debug.LogError("NetworkManager not found! Please add a NetworkManager to the scene.");
                UpdateStatusText("ERROR: NetworkManager not found!");
                return;
            }
            
            // Subscribe to network events
            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        private void AutoAssignTracking4AllComponents()
        {
            if (poseSolution == null)
                poseSolution = GameObject.Find("Pose Solution");
                
            if (avatar == null)
            {
                var player = GameObject.FindWithTag("Player");
                avatar = player ?? GameObject.Find("Adam_Big") ?? GameObject.Find("Remy");
            }
            
            if (puppet == null)
                puppet = GameObject.Find("Puppet-StandardT");
                
            if (debugLogs)
            {
                UnityEngine.Debug.Log("Tracking4All Components Found:");
                UnityEngine.Debug.Log($"- Pose Solution: {poseSolution?.name ?? "NOT FOUND"}");
                UnityEngine.Debug.Log($"- Avatar: {avatar?.name ?? "NOT FOUND"}");  
                UnityEngine.Debug.Log($"- Puppet: {puppet?.name ?? "NOT FOUND"}");
            }
        }
        
        public void StartHost()
        {
            if (networkManager == null) return;
            
            UpdateStatusText("Starting Host...");
            
            // Setup host-specific Tracking4All configuration
            SetupHostMode();
            
            // Start as host
            networkManager.StartHost();
            
            if (debugLogs)
                UnityEngine.Debug.Log("Starting as HOST - Camera tracking enabled");
        }
        
        public void StartClient()
        {
            if (networkManager == null) return;
            
            // Set connection address if provided
            if (ipAddressField != null && !string.IsNullOrEmpty(ipAddressField.text))
            {
                var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Address = ipAddressField.text;
                }
            }
            
            UpdateStatusText("Connecting to Host...");
            
            // Setup client-specific Tracking4All configuration  
            SetupClientMode();
            
            // Start as client
            networkManager.StartClient();
            
            if (debugLogs)
                UnityEngine.Debug.Log("Starting as CLIENT - Camera tracking disabled");
        }
        
        public void Disconnect()
        {
            if (networkManager == null) return;
            
            UpdateStatusText("Disconnecting...");
            networkManager.Shutdown();
            
            // Reset Tracking4All components
            ResetTracking4AllComponents();
            
            isNetworkActive = false;
            UpdateUIForDisconnected();
        }
        
        private void SetupHostMode()
        {
            // Host: Enable camera-based pose tracking
            if (poseSolution != null)
                poseSolution.SetActive(true);
                
            // Host: Keep avatar and puppet active
            if (avatar != null) avatar.SetActive(true);
            if (puppet != null) puppet.SetActive(true);
            
            UpdateStatusText("HOST: Camera tracking enabled");
        }
        
        private void SetupClientMode()
        {
            // Client: Disable camera-based tracking to save resources
            if (poseSolution != null)
                poseSolution.SetActive(false);
                
            // Client: Keep avatar and puppet active for network animation
            if (avatar != null) avatar.SetActive(true);
            if (puppet != null) puppet.SetActive(true);
            
            UpdateStatusText("CLIENT: Camera tracking disabled");
        }
        
        private void ResetTracking4AllComponents()
        {
            // Reset all components to default state
            if (poseSolution != null) poseSolution.SetActive(true);
            if (avatar != null) avatar.SetActive(true);
            if (puppet != null) puppet.SetActive(true);
        }
        
        private void OnServerStarted()
        {
            isNetworkActive = true;
            UpdateStatusText("HOST: Server started - Waiting for clients");
            UpdateUIForConnected();
            
            if (debugLogs)
                UnityEngine.Debug.Log("Host server started successfully");
        }
        
        private void OnClientConnected(ulong clientId)
        {
            if (networkManager.IsHost)
            {
                UpdateStatusText($"HOST: Client {clientId} connected");
                
                if (debugLogs)
                    UnityEngine.Debug.Log($"Client {clientId} connected to host");
            }
            else if (clientId == networkManager.LocalClientId)
            {
                isNetworkActive = true;
                UpdateStatusText("CLIENT: Connected to host");
                UpdateUIForConnected();
                
                if (debugLogs)
                    UnityEngine.Debug.Log("Successfully connected to host as client");
            }
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            if (networkManager.IsHost)
            {
                UpdateStatusText($"HOST: Client {clientId} disconnected");
            }
            else if (clientId == networkManager.LocalClientId)
            {
                isNetworkActive = false;
                UpdateStatusText("CLIENT: Disconnected from host");
                UpdateUIForDisconnected();
                ResetTracking4AllComponents();
            }
        }
        
        private void UpdateUIForConnected()
        {
            if (hostButton != null) hostButton.interactable = false;
            if (clientButton != null) clientButton.interactable = false;
            if (disconnectButton != null) disconnectButton.interactable = true;
            if (ipAddressField != null) ipAddressField.interactable = false;
        }
        
        private void UpdateUIForDisconnected()
        {
            if (hostButton != null) hostButton.interactable = true;
            if (clientButton != null) clientButton.interactable = true;
            if (disconnectButton != null) disconnectButton.interactable = false;
            if (ipAddressField != null) ipAddressField.interactable = true;
        }
        
        private void UpdateStatusText(string message)
        {
            if (statusText != null)
                statusText.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
                
            if (debugLogs)
                UnityEngine.Debug.Log($"MultiplayerSetup: {message}");
        }
        
        private void Update()
        {
            UpdateNetworkStatus();
        }
        
        private void UpdateNetworkStatus()
        {
            if (!isNetworkActive) return;
            
            if (networkManager != null && networkManager.IsHost)
            {
                int connectedClients = networkManager.ConnectedClients.Count;
                UpdateStatusText($"HOST: Running - {connectedClients} client(s) connected");
            }
            else if (networkManager != null && networkManager.IsClient)
            {
                UpdateStatusText("CLIENT: Connected - Receiving network data");
            }
        }
        
        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnServerStarted -= OnServerStarted;
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }
        
        // Public methods for external monitoring
        public bool IsNetworkActive() => isNetworkActive;
        public bool IsHost() => networkManager != null && networkManager.IsHost;
        public bool IsClient() => networkManager != null && networkManager.IsClient && !networkManager.IsHost;
        public int GetConnectedClientCount() => networkManager?.ConnectedClients?.Count ?? 0;
    }
}