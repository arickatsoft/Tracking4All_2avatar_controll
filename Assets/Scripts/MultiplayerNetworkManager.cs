using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Tracking4All;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Clean multiplayer network manager replicating the working logic
    /// Host: Broadcasts availability like the working version
    /// Client: Listens for host broadcasts like the working version  
    /// </summary>
    public class MultiplayerNetworkManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject gameSelectionPanel;
        [SerializeField] private Button hostGameButton;
        [SerializeField] private Button joinGameButton;
        [SerializeField] private Text statusText;
        
        [Header("Avatar References")]
        [SerializeField] private GameObject remyAvatar;
        [SerializeField] private GameObject bryceAvatar;        
        [Header("Camera References")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Camera mainCamera;
        
        [Header("Network Settings")]
        [SerializeField] private int discoveryPort = 47777;
        [SerializeField] private string gameIdentifier = "TRACKGAME_V1";
        [SerializeField] private string deviceName = "Mobile Device";
        
        // Network state
        private bool isHost = false;
        private bool isClient = false;
        
        // Host broadcasting (like the working version)
        private UdpClient hostBroadcaster;
        private Thread hostBroadcastThread;
        private bool isBroadcasting = false;
        private string hostIP = "";
        
        // Client listening (like the working version)
        private UdpClient clientListener;
        private Thread clientListenThread;
        private bool isListening = false;
        private List<DiscoveredHost> discoveredHosts = new List<DiscoveredHost>();
        
        [System.Serializable]
        public class DiscoveredHost
        {
            public string name;
            public string ip;
            public int port;
            public float lastSeen;
        }        
        void Start()
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = SystemInfo.deviceName;
            }
            
            SetupUI();
            UpdateStatus("Select game mode:");
        }
        
        void SetupUI()
        {
            if (hostGameButton != null)
            {
                hostGameButton.onClick.AddListener(OnHostGameClicked);
            }
            
            if (joinGameButton != null)  
            {
                joinGameButton.onClick.AddListener(OnJoinGameClicked);
            }
        }
        
        void OnHostGameClicked()
        {
            UnityEngine.Debug.Log("=== HOST GAME CLICKED ===");
            
            // Hide selection panel
            if (gameSelectionPanel != null)
            {
                gameSelectionPanel.SetActive(false);
            }
            
            StartHostMode();
        }        
        void OnJoinGameClicked()
        {
            UnityEngine.Debug.Log("=== JOIN GAME CLICKED ===");
            
            // Hide selection panel
            if (gameSelectionPanel != null)
            {
                gameSelectionPanel.SetActive(false);
            }
            
            StartClientMode();
        }
        
        void StartHostMode()
        {
            isHost = true;
            isClient = false;
            
            UpdateStatus("Starting host mode...");
            UnityEngine.Debug.Log("Host mode: Setting up Bryce as local player");
            
            // Setup avatars: Host controls Bryce
            SetupAvatarsForHost();
            
            // Start broadcasting like the working version
            StartHostBroadcast();
        }
        
        void StartClientMode()
        {
            isClient = true;
            isHost = false;
            
            UpdateStatus("Starting client mode...");
            UnityEngine.Debug.Log("Client mode: Setting up Remy as local player");
            
            // Setup avatars: Client controls Remy
            SetupAvatarsForClient();
            
            // Start listening for hosts like the working version
            StartClientListen();
        }        
        void SetupAvatarsForHost()
        {
            // Host: Bryce gets camera input, Remy gets network input (consistent with BasicMultiplayerButtons)
            if (bryceAvatar != null)
            {
                bryceAvatar.SetActive(true);
                bryceAvatar.tag = "Player";
                
                // Enable pose tracking for Bryce
                var trackingComponent = bryceAvatar.GetComponent<Avatar>();
                if (trackingComponent != null)
                {
                    trackingComponent.enabled = true;
                }
                
                UnityEngine.Debug.Log("Host: Bryce is now the active tracked avatar");
            }
            
            if (remyAvatar != null)
            {
                remyAvatar.SetActive(true);
                remyAvatar.tag = "RemotePlayer";
                
                // Disable pose tracking for Remy (will receive network data)
                var trackingComponent = remyAvatar.GetComponent<Avatar>();
                if (trackingComponent != null)
                {
                    trackingComponent.enabled = false;
                }
                
                UnityEngine.Debug.Log("Host: Remy will receive network pose data");
            }
        }
        
        void SetupAvatarsForClient()
        {
            // Client: Remy gets camera input, Bryce gets network input
            if (remyAvatar != null)
            {
                remyAvatar.SetActive(true);
                remyAvatar.tag = "Player";                
                // Enable pose tracking for Remy
                var trackingComponent = remyAvatar.GetComponent<Avatar>();
                if (trackingComponent != null)
                {
                    trackingComponent.enabled = true;
                }
                
                UnityEngine.Debug.Log("Client: Remy is now the active tracked avatar");
            }
            
            if (bryceAvatar != null)
            {
                bryceAvatar.SetActive(true);
                bryceAvatar.tag = "RemotePlayer";
                
                // Disable pose tracking for Bryce (will receive network data)
                var trackingComponent = bryceAvatar.GetComponent<Avatar>();
                if (trackingComponent != null)
                {
                    trackingComponent.enabled = false;
                }
                
                UnityEngine.Debug.Log("Client: Bryce will receive network pose data");
            }
        }
        
        void StartHostBroadcast()
        {
            try
            {
                // Get local IP
                hostIP = GetLocalIPAddress();
                UpdateStatus($"Broadcasting from {hostIP}...");
                UnityEngine.Debug.Log($"Host starting broadcast on IP: {hostIP}");
                
                // Setup UDP broadcaster
                hostBroadcaster = new UdpClient();
                hostBroadcaster.EnableBroadcast = true;                
                // Start broadcast thread
                isBroadcasting = true;
                hostBroadcastThread = new Thread(HostBroadcastLoop);
                hostBroadcastThread.Start();
                
                UpdateStatus("Waiting for players to join...");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to start host broadcast: {e.Message}");
                UpdateStatus($"Host failed: {e.Message}");
            }
        }
        
        void HostBroadcastLoop()
        {
            UnityEngine.Debug.Log("Host broadcast loop started");
            
            while (isBroadcasting && hostBroadcaster != null)
            {
                try
                {
                    // Create broadcast message like the working version
                    string message = $"HOST_AVAILABLE|{gameIdentifier}|{deviceName}|{hostIP}|7777|{System.DateTime.Now.Ticks}";
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    
                    // Broadcast to multiple subnets like the working version
                    var broadcastAddresses = new[]
                    {
                        IPAddress.Parse("255.255.255.255"),  // Global broadcast
                        IPAddress.Parse("192.168.0.255"),    // Common subnet 1
                        IPAddress.Parse("192.168.1.255")     // Common subnet 2
                    };
                    
                    foreach (var broadcastAddr in broadcastAddresses)
                    {
                        IPEndPoint endPoint = new IPEndPoint(broadcastAddr, discoveryPort);
                        hostBroadcaster.Send(data, data.Length, endPoint);
                    }
                    
                    UnityEngine.Debug.Log($"Host broadcast sent: {message}");
                    
                    Thread.Sleep(2000); // Broadcast every 2 seconds
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"Host broadcast error: {e.Message}");
                    break;
                }
            }
            
            UnityEngine.Debug.Log("Host broadcast loop ended");
        }        
        void StartClientListen()
        {
            try
            {
                string localIP = GetLocalIPAddress();
                UpdateStatus($"Scanning from {localIP}...");
                UnityEngine.Debug.Log($"Client starting listen on IP: {localIP}");
                
                // Setup UDP listener exactly like the working version
                clientListener = new UdpClient(discoveryPort);
                clientListener.EnableBroadcast = true;
                
                // Start listen thread
                isListening = true;
                clientListenThread = new Thread(ClientListenLoop);
                clientListenThread.Start();
                
                UpdateStatus("Looking for available games...");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to start client listen: {e.Message}");
                UpdateStatus($"Join failed: {e.Message}");
            }
        }
        
        void ClientListenLoop()
        {
            UnityEngine.Debug.Log("Client listen loop started");
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, discoveryPort);
            
            while (isListening && clientListener != null)
            {
                try
                {
                    byte[] data = clientListener.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data);
                    string senderIP = remoteEndPoint.Address.ToString();
                    
                    UnityEngine.Debug.Log($"Client received: {message} from {senderIP}");
                    
                    // Process host broadcast like the working version
                    ProcessHostBroadcast(message, senderIP);
                }
                catch (System.Exception e)
                {
                    if (isListening)
                    {
                        UnityEngine.Debug.LogError($"Client listen error: {e.Message}");
                    }
                }
            }
            
            UnityEngine.Debug.Log("Client listen loop ended");
        }        
        void ProcessHostBroadcast(string message, string senderIP)
        {
            try
            {
                UnityEngine.Debug.Log($"Processing broadcast from {senderIP}: {message}");
                
                var parts = message.Split('|');
                if (parts.Length >= 5 && parts[0] == "HOST_AVAILABLE" && parts[1] == gameIdentifier)
                {
                    string hostName = parts[2];
                    string hostIP = parts[3];
                    string portStr = parts[4];
                    
                    if (int.TryParse(portStr, out int gamePort))
                    {
                        UnityEngine.Debug.Log($"Found valid host: {hostName} at {hostIP}:{gamePort}");
                        
                        // Update or add discovered host
                        var existingHost = discoveredHosts.Find(h => h.ip == hostIP);
                        if (existingHost != null)
                        {
                            existingHost.lastSeen = Time.time;
                        }
                        else
                        {
                            discoveredHosts.Add(new DiscoveredHost
                            {
                                name = hostName,
                                ip = hostIP,
                                port = gamePort,
                                lastSeen = Time.time
                            });
                        }
                        
                        // Auto-connect to first found host
                        if (discoveredHosts.Count == 1)
                        {
                            ConnectToHost(discoveredHosts[0]);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error processing host broadcast: {e.Message}");
            }
        }        
        void ConnectToHost(DiscoveredHost host)
        {
            UnityEngine.Debug.Log($"Connecting to host: {host.name} at {host.ip}:{host.port}");
            UpdateStatus($"Connecting to {host.name}...");
            
            // Stop listening for more hosts
            StopClientListen();
            
            // Show connection success
            UpdateStatus($"Connected to {host.name}!");
            
            // Start actual multiplayer game session
            StartMultiplayerGame();
        }
        
        void StartMultiplayerGame()
        {
            UnityEngine.Debug.Log("=== MULTIPLAYER GAME STARTED ===");
            
            // Initialize pose synchronization
            InitializePoseSync();
            
            if (isHost)
            {
                UnityEngine.Debug.Log("Host: Game session active - Bryce tracking, sending to Remy");
                UpdateStatus("Game active! Tracking your movements...");
            }
            else if (isClient)
            {
                UnityEngine.Debug.Log("Client: Game session active - Remy tracking, sending to Bryce");
                UpdateStatus("Game active! Connected to host...");
            }
        }
        
        void InitializePoseSync()
        {
            // Find or create pose sync manager
            PoseSyncManager poseSyncManager = FindObjectOfType<PoseSyncManager>();
            if (poseSyncManager == null)
            {
                GameObject poseSyncGO = new GameObject("PoseSyncManager");
                poseSyncManager = poseSyncGO.AddComponent<PoseSyncManager>();
            }
            
            if (isHost)
            {
                // Host: Bryce is local, Remy is remote
                poseSyncManager.InitializeAsHost();
            }
            else if (isClient)
            {
                // Client: Remy is local, Bryce is remote
                poseSyncManager.InitializeAsClient();
            }
        }        

        // ==================== DATA TRANSMISSION METHODS ====================
        
        private void SendDataToConnectedPeer(string data)
        {
            if (!isHost && !isClient)
            {
                UnityEngine.Debug.LogWarning("SendDataToConnectedPeer: Not in multiplayer mode");
                return;
            }
            
            UnityEngine.Debug.Log($"SendDataToConnectedPeer: {data.Substring(0, Mathf.Min(50, data.Length))}...");
            
            // TODO: Implement actual data transmission over TCP connection
            // This is a placeholder that logs the data that would be sent
            // In a complete implementation, this would send over an established TCP connection
        }
        
        // Method to receive data (placeholder)
        private void OnDataReceived(string data)
        {
            UnityEngine.Debug.Log($"OnDataReceived: {data.Substring(0, Mathf.Min(50, data.Length))}...");
            
            // TODO: Process received pose data and apply to appropriate avatar
            // This would parse the received pose data and update the remote avatar
        }
        
        string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to get local IP: {e.Message}");
            }
            
            return "127.0.0.1";
        }
        
        void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
            
            UnityEngine.Debug.Log($"Status: {status}");
        }
        
        void StopHostBroadcast()
        {
            isBroadcasting = false;
            
            if (hostBroadcastThread != null)
            {
                hostBroadcastThread.Join(1000);
                hostBroadcastThread = null;
            }
            
            if (hostBroadcaster != null)
            {
                hostBroadcaster.Close();
                hostBroadcaster = null;
            }
        }        
        void StopClientListen()
        {
            isListening = false;
            
            if (clientListenThread != null)
            {
                clientListenThread.Join(1000);
                clientListenThread = null;
            }
            
            if (clientListener != null)
            {
                clientListener.Close();
                clientListener = null;
            }
        }
        
        void OnDestroy()
        {
            StopHostBroadcast();
            StopClientListen();
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StopHostBroadcast();
                StopClientListen();
            }
        }
        
        public void ShowGameSelection()
        {
            if (gameSelectionPanel != null)
            {
                gameSelectionPanel.SetActive(true);
            }
            
            StopHostBroadcast();
            StopClientListen();
            
            isHost = false;
            isClient = false;
            
            UpdateStatus("Select game mode:");
        }
    }
}
