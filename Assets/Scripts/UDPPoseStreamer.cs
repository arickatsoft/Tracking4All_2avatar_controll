using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;

namespace Tracking4All
{
    /// <summary>
    /// Streams MediaPipe pose data from local avatar to another Unity instance via UDP.
    /// Mimics the exact format used by the Python MediaPipe streaming script.
    /// </summary>
    public class UDPPoseStreamer : MonoBehaviour
    {
        [Header("Network Settings")]
        [Tooltip("Target IP address (e.g., 192.168.1.100 for another device, or 127.0.0.1 for same device)")]
        [SerializeField] private string targetIP = "127.0.0.1";
        [SerializeField] private int targetPort = 52733;
        
        [Header("Source Avatar")]
        [Tooltip("The pose landmark provider to stream from (e.g., Bryce's provider)")]
        [SerializeField] private LandmarkProvider<PoseLandmarks> sourcePoseProvider;
        [SerializeField] private NormalizedLandmarkProvider<PoseLandmarks> sourceNormalizedProvider;
        [SerializeField] private int sourceGroup = 0;
        
        [Header("Streaming Settings")]
        [Tooltip("Enable/disable streaming at runtime")]
        [SerializeField] private bool isStreaming = true;
        [Tooltip("Max frames per second to stream (0 = unlimited)")]
        [SerializeField] private float maxFPS = 30f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showFPS = true;
        [SerializeField] private bool logSentData = false;
        
        [Header("Connection Settings")]
        [Tooltip("Number of PING attempts before giving up (each attempt is ~1 second)")]
        [SerializeField] private int maxPingAttempts = 10;
        
        // UDP client
        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
        private volatile bool isConnected = false;
        private volatile bool isReceiverReady = false;

        // Cache for thread-safe logging
        private string cachedGameObjectName;
        
        // Connection handshake
        private Thread handshakeThread;
        private volatile bool isAttemptingConnection = false;
        
        // Threading for non-blocking sends
        private Thread sendThread;
        private volatile bool shouldRun = true;

        // Performance tracking
        private float lastSendTime;
        private float sendInterval;
        private float avgFPS;
        private int totalPacketsSent;
        private float lastFPSUpdateTime;
        private int lastPacketCountForFPS;
        private float lastStatusLogTime;

        // Performance optimizations
        private StringBuilder cachedStringBuilder = new StringBuilder(4096);
        private AutoResetEvent sendEvent = new AutoResetEvent(false);
        private string pendingPose; // Latest pose wins; never replay a backlog after a stall.

        // Events
        public event Action<string> OnDataSent;
        
        private void Start()
        {
            // CRITICAL: Cache gameObject.name for thread-safe logging
            cachedGameObjectName = gameObject.name;

            sendInterval = maxFPS > 0 ? 1f / maxFPS : 0;
            ConnectUDP();
        }
        
        private void ConnectUDP()
        {
            try
            {
                UnityEngine.Debug.Log($"🔧 [{gameObject.name}] Creating new UDP client for {targetIP}:{targetPort}...");

                udpClient = new UdpClient();
                udpClient.Client.SendTimeout = 100;
                udpClient.Client.ReceiveTimeout = 100; // 100ms timeout for receiving PONG

                // ========================================
                // Android-specific UDP socket configuration
                // ========================================
                #if UNITY_ANDROID || UNITY_IOS
                try
                {
                    // Enable address reuse - helps with reconnection on mobile
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    UnityEngine.Debug.Log($"[UDPPoseStreamer on {gameObject.name}] 📱 Mobile: Enabled SO_REUSEADDR");

                    // Disable linger - prevents hanging connections on mobile
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                    UnityEngine.Debug.Log($"[UDPPoseStreamer on {gameObject.name}] 📱 Mobile: Disabled socket linger");

                    // Set larger send buffer for mobile - helps prevent packet loss
                    udpClient.Client.SendBufferSize = 256 * 1024; // 256KB
                    UnityEngine.Debug.Log($"[UDPPoseStreamer on {gameObject.name}] 📱 Mobile: Set send buffer to 256KB");

                    // Enable broadcast (helps with local network discovery)
                    udpClient.EnableBroadcast = true;
                    UnityEngine.Debug.Log($"[UDPPoseStreamer on {gameObject.name}] 📱 Mobile: Enabled broadcast");
                }
                catch (Exception mobileEx)
                {
                    UnityEngine.Debug.LogWarning($"[UDPPoseStreamer on {gameObject.name}] ⚠️ Mobile socket configuration warning: {mobileEx.Message}");
                    UnityEngine.Debug.LogWarning($"[UDPPoseStreamer on {gameObject.name}] This is not critical - socket may still work");
                }
                #endif

                remoteEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
                udpClient.Connect(remoteEndPoint);
                isConnected = true;

                UnityEngine.Debug.Log($"<color=yellow>📡 [{gameObject.name}] UDP Client created for TARGET: {targetIP}:{targetPort}</color>");
                UnityEngine.Debug.Log($"<color=yellow>📡 [{gameObject.name}] Will send PING packets to this address...</color>");
                
                // Start handshake thread to detect receiver
                isAttemptingConnection = true;
                isReceiverReady = false;
                
                shouldRun = true;
                handshakeThread = new Thread(HandshakeThreadWorker);
                handshakeThread.IsBackground = true;

                UnityEngine.Debug.Log($"<color=yellow>🔧 [{gameObject.name}] About to start handshake thread...</color>");
                handshakeThread.Start();
                UnityEngine.Debug.Log($"<color=green>✅ [{gameObject.name}] Handshake thread started (ThreadID will be logged from thread)</color>");

                // Start send thread (will only send once receiver is ready)
                sendThread = new Thread(SendThreadWorker);
                sendThread.IsBackground = true;
                sendThread.Start();
                UnityEngine.Debug.Log($"<color=green>✅ [{gameObject.name}] Send thread started</color>");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"❌ Failed to create UDP client: {e.Message}");
                isConnected = false;
            }
        }
        
        private void HandshakeThreadWorker()
        {
            UnityEngine.Debug.Log($"<color=yellow>🤝 [{cachedGameObjectName}] Handshake thread ENTERED (ThreadID: {Thread.CurrentThread.ManagedThreadId})</color>");
            UnityEngine.Debug.Log($"<color=yellow>🤝 [{cachedGameObjectName}] Target: {targetIP}:{targetPort}, MaxAttempts: {maxPingAttempts}</color>");
            int attemptCount = 0;
            
            while (shouldRun && !isReceiverReady && isAttemptingConnection && attemptCount < maxPingAttempts)
            {
                attemptCount++;
                
                // Check if UDP client is still valid
                if (udpClient == null)
                {
                    UnityEngine.Debug.LogError("❌ UDP client is null! Cannot send PING.");
                    break;
                }
                
                try
                {
                    // Send PING message
                    string pingMessage = "PING";
                    byte[] pingData = Encoding.UTF8.GetBytes(pingMessage);
                    int bytesSent = udpClient.Send(pingData, pingData.Length);

                    UnityEngine.Debug.Log($"🔄 [{cachedGameObjectName}] Attempt {attemptCount}/{maxPingAttempts}: Sent {bytesSent} bytes PING to {targetIP}:{targetPort}");
                    UnityEngine.Debug.Log($"📤 [{cachedGameObjectName}] PING destination: {remoteEndPoint.Address}:{remoteEndPoint.Port}");
                    
                    // Try to receive PONG (non-blocking with timeout)
                    try
                    {
                        IPEndPoint receiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        byte[] receivedData = udpClient.Receive(ref receiveEndPoint);
                        string response = Encoding.UTF8.GetString(receivedData);
                        
                        if (response == "PONG")
                        {
                            isReceiverReady = true;
                            isAttemptingConnection = false;
                            UnityEngine.Debug.Log($"<color=green>✅ PONG received! Receiver is ready. Starting pose streaming...</color>");
                            break;
                        }
                    }
                    catch (SocketException)
                    {
                        // Timeout or no response - continue trying
                    }
                }
                catch (ObjectDisposedException)
                {
                    UnityEngine.Debug.LogWarning("⚠️ UDP client was disposed during handshake");
                    break;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"Handshake error: {e.Message}");
                }
                
                // Wait 1 second before retry
                for (int wait = 0; wait < 20 && shouldRun; wait++) Thread.Sleep(50);
            }
            
            isAttemptingConnection = false;
            
            if (!isReceiverReady)
            {
                UnityEngine.Debug.LogWarning($"⚠️ No response after {attemptCount} attempts. Click 'Retry Connection' button or check if receiver is running.");
            }
            
            UnityEngine.Debug.Log($"🤝 Handshake thread: Exiting. ReceiverReady={isReceiverReady}");
        }
        
        private void Update()
        {
            // Periodic status logging (every 5 seconds)
            #if UNITY_EDITOR
            if (Time.time - lastStatusLogTime >= 5f)
            {
                lastStatusLogTime = Time.time;
                UnityEngine.Debug.Log($"[UDPPoseStreamer] Status: ReceiverReady={isReceiverReady}, Attempting={isAttemptingConnection}, Connected={isConnected}, Streaming={isStreaming}, FPS={avgFPS:F1}");
            }
            #endif

            if (!isStreaming || !isConnected || !isReceiverReady)
            {
                #if UNITY_EDITOR
                if (Time.frameCount % 300 == 0) // Every 5 seconds
                {
                    UnityEngine.Debug.LogWarning($"⚠️ Not streaming: isStreaming={isStreaming}, isConnected={isConnected}, isReceiverReady={isReceiverReady}");
                }
                #endif
                return;
            }

            if (sourcePoseProvider == null || sourcePoseProvider.Null)
            {
                #if UNITY_EDITOR
                if (Time.frameCount % 300 == 0)
                {
                    UnityEngine.Debug.LogWarning("⚠️ sourcePoseProvider is null or invalid!");
                }
                #endif
                return;
            }

            if (sourceNormalizedProvider == null || sourceNormalizedProvider.Null)
            {
                #if UNITY_EDITOR
                if (Time.frameCount % 300 == 0)
                {
                    UnityEngine.Debug.LogWarning("⚠️ sourceNormalizedProvider is null or invalid!");
                }
                #endif
                return;
            }

            // Throttle based on maxFPS
            if (maxFPS > 0 && Time.unscaledTime - lastSendTime < sendInterval)
                return;

            lastSendTime = Time.unscaledTime;

            // Build and queue message
            string message = BuildPoseMessage();
            if (!string.IsNullOrEmpty(message))
            {
                Interlocked.Exchange(ref pendingPose, message);
                sendEvent.Set(); // Wake thread only when needed

                #if UNITY_EDITOR
                // Debug: Log first few characters to verify data format
                if (showDebugInfo && Time.frameCount % 60 == 0) // Once per second
                {
                    int previewLength = Mathf.Min(100, message.Length);
                    UnityEngine.Debug.Log($"📤 Sending data (preview): {message.Substring(0, previewLength)}...");
                }
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                if (Time.frameCount % 60 == 0) // Once per second
                {
                    UnityEngine.Debug.LogWarning("⚠️ BuildPoseMessage returned null or empty!");
                }
                #endif
            }

            // Update FPS tracking (safe in main thread)
            UpdateFPS();
        }
        
        private string BuildPoseMessage()
        {
            try
            {
                cachedStringBuilder.Clear(); // Reuse instead of new

                // PART 1: World Landmarks (3D coordinates)
                for (int i = 0; i < 33; i++)
                {
                    var landmark = sourcePoseProvider.Get(sourceGroup, (PoseLandmarks)i);
                    // Check if landmark has valid data (visibility/presence check)
                    if (landmark.Visibility > 0f || landmark.Presence > 0f)
                    {
                        Vector3 pos = landmark.Position;
                        cachedStringBuilder.Append(i).Append('|')
                                        .Append(pos.x.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                                        .Append(pos.y.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                                        .Append(pos.z.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                    }
                    else
                    {
                        // Send zeros for missing landmarks
                        cachedStringBuilder.Append(i).Append('|')
                                        .Append(0).Append('|')
                                        .Append(0).Append('|')
                                        .Append(0).Append('\n');
                    }
                }

                // Separator between world and normalized landmarks
                cachedStringBuilder.Append("*||*");

                // PART 2: Normalized Screen Landmarks (2D/3D normalized)
                for (int i = 0; i < 33; i++)
                {
                    var landmark = sourceNormalizedProvider.Get(sourceGroup, (PoseLandmarks)i);
                    // Check if landmark has valid data
                    if (landmark.Visibility > 0f || landmark.Presence > 0f)
                    {
                        Vector3 pos = landmark.Position;
                        cachedStringBuilder.Append(i).Append('|')
                                        .Append(pos.x.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                                        .Append(pos.y.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                                        .Append(pos.z.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                    }
                    else
                    {
                        // Send zeros for missing landmarks
                        cachedStringBuilder.Append(i).Append('|')
                                        .Append(0).Append('|')
                                        .Append(0).Append('|')
                                        .Append(0).Append('\n');
                    }
                }

                return cachedStringBuilder.ToString();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error building pose message: {e.Message}");
                return null;
            }
        }
        
        private void SendThreadWorker()
        {
            while (shouldRun)
            {
                sendEvent.WaitOne(100); // Sleep until signaled or 100ms timeout

                string message = Interlocked.Exchange(ref pendingPose, null);
                if (message != null)
                {
                    try
                    {
                        if (udpClient != null && shouldRun)
                        {
                            // Send the newest available pose.
                            byte[] data = Encoding.UTF8.GetBytes(message + "<EOM>");
                            udpClient.Send(data, data.Length);

                            // Thread-safe increment
                            System.Threading.Interlocked.Increment(ref totalPacketsSent);
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Socket was closed, exit gracefully
                        break;
                    }
                    catch (SocketException)
                    {
                        // Network error, exit gracefully
                        break;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning($"Send error: {e.Message}");
                    }
                }
            }
        }
        
        private void UpdateFPS()
        {
            // Get thread-safe packet count (Interlocked operations are atomic)
            int currentPacketCount = totalPacketsSent;

            // Calculate FPS every second
            if (Time.time - lastFPSUpdateTime >= 1f)
            {
                float deltaTime = Time.time - lastFPSUpdateTime;
                int packetsSinceLastUpdate = currentPacketCount - lastPacketCountForFPS;

                avgFPS = packetsSinceLastUpdate / deltaTime;
                lastPacketCountForFPS = currentPacketCount;
                lastFPSUpdateTime = Time.time;

                #if UNITY_EDITOR
                if (showFPS)
                {
                    UnityEngine.Debug.Log($"<color=cyan>UDP Stream FPS: {avgFPS:F1} (Total sent: {currentPacketCount})</color>");
                }
                #endif
            }
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
        
        private void OnApplicationQuit()
        {
            Disconnect();
        }
        
        private void Disconnect()
        {
            UnityEngine.Debug.Log("🔌 Disconnecting UDP streamer...");
            
            // Stop all threads
            shouldRun = false;
            isAttemptingConnection = false;
            sendEvent.Set();
            Interlocked.Exchange(ref pendingPose, null);
            udpClient?.Close(); // Unblock socket receive before joining workers.
            
            // Wait for handshake thread to finish
            if (handshakeThread != null && handshakeThread.IsAlive)
            {
                UnityEngine.Debug.Log("⏳ Waiting for handshake thread to stop...");
                handshakeThread.Join(1000);
            }
            
            // Wait for send thread to finish
            if (sendThread != null && sendThread.IsAlive)
            {
                UnityEngine.Debug.Log("⏳ Waiting for send thread to stop...");
                sendThread.Join(1000);
            }
            
            // Now it's safe to close socket
            if (udpClient != null)
            {
                try
                {
                    udpClient.Close();
                    udpClient = null;
                    isConnected = false;
                    isReceiverReady = false;
                    UnityEngine.Debug.Log("✅ UDP Streamer disconnected cleanly");
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"Error closing UDP client: {e.Message}");
                }
            }
            
            // Note: We DON'T reset isStreaming - that's a user preference
        }

        // OnGUI disabled - UI hidden for production builds
        /*
        private void OnGUI()
        {
            // Always show reconnect buttons in top-right corner, regardless of connection state
            GUILayout.BeginArea(new Rect(Screen.width - 200, 10, 190, 130));

            // Color code based on connection state
            if (isReceiverReady)
            {
                GUI.backgroundColor = Color.green;
            }
            else if (isAttemptingConnection)
            {
                GUI.backgroundColor = Color.yellow;
            }
            else
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0f); // Orange when disconnected
            }

            string buttonText = isReceiverReady ? "🔄 RECONNECT\n(Connected)" :
                               isAttemptingConnection ? "🔄 CONNECTING...\n(Please Wait)" :
                               "🔄 RETRY CONNECTION\n(Not Connected)";

            if (GUILayout.Button(buttonText, GUILayout.Height(50)))
            {
                if (!isAttemptingConnection) // Don't allow clicks while connecting
                {
                    Reconnect();
                }
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndArea();

            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 350, 280));
            GUILayout.Label($"UDP Pose Streamer", new GUIStyle(GUI.skin.label)
            { fontSize = 16, fontStyle = FontStyle.Bold });

            GUILayout.Space(5);
            GUILayout.Label($"Target: {targetIP}:{targetPort}");

            // Connection status with color coding
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };

            if (isReceiverReady)
            {
                statusStyle.normal.textColor = Color.green;
                GUILayout.Label("✅ Connected & Streaming", statusStyle);
            }
            else if (isAttemptingConnection)
            {
                statusStyle.normal.textColor = Color.yellow;
                GUILayout.Label("🔄 Connecting...", statusStyle);
            }
            else
            {
                statusStyle.normal.textColor = Color.red;
                GUILayout.Label("❌ Not Connected", statusStyle);
            }

            GUILayout.Space(5);
            GUILayout.Label($"Receiver Ready: {isReceiverReady}");
            GUILayout.Label($"Streaming Active: {isStreaming && isReceiverReady}");
            GUILayout.Label($"Attempting Connection: {isAttemptingConnection}");
            GUILayout.Label($"Queue Size: {(pendingPose == null ? 0 : 1)}");
            GUILayout.Label($"Packets Sent: {totalPacketsSent}");
            GUILayout.Label($"FPS: {avgFPS:F1}");

            GUILayout.Space(10);

            // Action buttons - always show SOMETHING to keep GUI layout stable
            if (isStreaming && isReceiverReady)
            {
                if (GUILayout.Button("⏸ Stop Streaming", GUILayout.Height(30)))
                {
                    isStreaming = false;
                }
            }
            else if (isReceiverReady)
            {
                if (GUILayout.Button("▶ Start Streaming", GUILayout.Height(30)))
                {
                    isStreaming = true;
                }
            }
            else
            {
                // Show disabled button to maintain GUI layout consistency
                GUI.enabled = false;
                GUILayout.Button("⏸ Waiting for Connection", GUILayout.Height(30));
                GUI.enabled = true;
            }

            GUILayout.EndArea();
        }
        */

        // Public API
        public bool IsConnected => isConnected && isReceiverReady;
        public bool NeedsRetry => !isReceiverReady && !isAttemptingConnection;
        public bool IsStreaming
        {
            get => isStreaming;
            set => isStreaming = value;
        }
        public float CurrentFPS => avgFPS;
        public int TotalPacketsSent => totalPacketsSent;
        
        public void Reconnect()
        {
            UnityEngine.Debug.Log($"<color=cyan>🔄 [{gameObject.name}] Reconnect() called</color>");
            UnityEngine.Debug.Log($"<color=cyan>🔄 [{gameObject.name}] Current state: isConnected={isConnected}, isReceiverReady={isReceiverReady}, isAttemptingConnection={isAttemptingConnection}</color>");
            UnityEngine.Debug.Log($"<color=cyan>🔄 [{gameObject.name}] Will reconnect to: {targetIP}:{targetPort}</color>");

            try
            {
                // CRITICAL: Reset ALL connection state flags
                isReceiverReady = false;
                isAttemptingConnection = false;
                isConnected = false;

                // Clean up existing connection
                UnityEngine.Debug.Log($"<color=cyan>🔄 [{gameObject.name}] Calling Disconnect()...</color>");
                Disconnect();

                // Workers have exited and the socket is closed; no main-thread sleep is needed.

                // Start fresh connection attempt
                UnityEngine.Debug.Log($"<color=cyan>🔄 [{gameObject.name}] Calling ConnectUDP() with new target {targetIP}:{targetPort}...</color>");
                ConnectUDP();
                UnityEngine.Debug.Log($"<color=cyan>🔄 [{gameObject.name}] Reconnect() complete successfully</color>");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"<color=red>❌ [{gameObject.name}] Reconnect() FAILED with exception: {e.Message}</color>");
                UnityEngine.Debug.LogError($"<color=red>❌ [{gameObject.name}] Stack trace: {e.StackTrace}</color>");
            }
        }
        
        public void SetTarget(string ip, int port)
        {
            UnityEngine.Debug.Log($"<color=cyan>🎯 [{gameObject.name}] SetTarget() called: {ip}:{port}</color>");
            UnityEngine.Debug.Log($"<color=cyan>🎯 [{gameObject.name}] Previous target was: {targetIP}:{targetPort}</color>");

            targetIP = ip;
            targetPort = port;

            UnityEngine.Debug.Log($"<color=cyan>🎯 [{gameObject.name}] New target set to: {targetIP}:{targetPort}</color>");

            if (isConnected)
            {
                UnityEngine.Debug.Log($"<color=cyan>🎯 [{gameObject.name}] Already connected, will reconnect with new target...</color>");
                Reconnect();
            }
            else
            {
                UnityEngine.Debug.Log($"<color=cyan>🎯 [{gameObject.name}] Not connected yet, target will be used on next connection</color>");
            }
        }

        /// <summary>
        /// Resets ALL network connections on this device (all streamers + server).
        /// Call this from the second device after both devices have selected avatars.
        /// </summary>
        private void ResetAllNetworkConnections()
        {
            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log("🔄 RESET BOTH DEVICES - Restarting ALL network connections");
            UnityEngine.Debug.Log("========================================");

            // Find the AvatarSelectionManager to call its reset method
            var avatarManager = FindObjectOfType<AvatarSelectionManager>();
            if (avatarManager != null)
            {
                UnityEngine.Debug.Log("✅ Found AvatarSelectionManager, calling ResetAllNetworkConnections()");
                avatarManager.ResetAllNetworkConnections();
            }
            else
            {
                // Fallback: manually reset everything if manager not found
                UnityEngine.Debug.LogWarning("⚠️ AvatarSelectionManager not found, performing manual reset");

                // Find all streamers
                UDPPoseStreamer[] streamers = FindObjectsOfType<UDPPoseStreamer>();
                foreach (var streamer in streamers)
                {
                    UnityEngine.Debug.Log($"🔄 Reconnecting streamer on {streamer.gameObject.name}");
                    streamer.Reconnect();
                }

                // Restart the server
                UDPPoseServer server = FindObjectOfType<UDPPoseServer>();
                if (server != null)
                {
                    UnityEngine.Debug.Log("🔄 Restarting UDPPoseServer...");
                    var serverBehaviour = server as MonoBehaviour;
                    if (serverBehaviour != null)
                    {
                        serverBehaviour.enabled = false;
                        serverBehaviour.enabled = true;
                        UnityEngine.Debug.Log("✅ UDPPoseServer restarted");
                    }
                }
            }

            UnityEngine.Debug.Log("✅ Network reset complete!");
            UnityEngine.Debug.Log("========================================");
        }
    }
}
