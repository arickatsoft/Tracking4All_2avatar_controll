using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace Tracking4All
{
    /// <summary>
    /// UDP Server for receiving pose data from another Unity instance.
    /// Parses Python-style MediaPipe format: world_landmarks + screen_landmarks with <EOM> delimiter.
    /// </summary>
    public class UDPPoseServer : MonoBehaviour
    {
        [Header("Network Settings")]
        [Tooltip("Listen port (auto-detected based on active avatar, or manually set)")]
        [SerializeField] public int listenPort = 52733; // Default fallback, will be auto-detected

        [Header("Target Avatar")]
        [Tooltip("Assign the avatar's pose landmark provider to apply received data")]
        [SerializeField] private LandmarkProvider<PoseLandmarks> targetPoseProvider;
        [SerializeField] private int targetGroup = 0;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showFPS = true;
        [SerializeField] private bool logReceivedData = false;

        // CRITICAL: Static port override that survives component re-initialization
        private static int? portOverride = null;

        // UDP components
        private UdpClient udpServer;
        private Thread receiveThread;
        private volatile bool shouldRun = true;

        // Cache for thread-safe logging
        private string gameObjectName; // Cached to avoid accessing gameObject.name from background thread

        // Data buffering
        private readonly object dataLock = new object();
        private PoseData latestPoseData;
        private bool hasNewData = false;
        
        // Performance tracking
        private float lastFrameTime;
        private int frameCount;
        private float avgFPS;
        private int totalPacketsReceived;
        
        // Events
        public event Action<PoseData> OnPoseDataReceived;
        
        private void Start()
        {
            // CRITICAL: Cache gameObject.name for thread-safe logging
            gameObjectName = gameObject.name;
            UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] Start() called (runs once on creation)");
        }

        private void OnEnable()
        {
            // Cache gameObject.name if not already done
            if (string.IsNullOrEmpty(gameObjectName))
            {
                gameObjectName = gameObject.name;
            }

            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] OnEnable() called");
            UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] Initial serialized listenPort value: {listenPort}");
            UnityEngine.Debug.Log("========================================");

            // AUTOMATIC PORT DETECTION: Check which avatar is camera-controlled
            // This detection happens EVERY time the component is enabled
            int? detectedPort = DetectCorrectPort();

            if (detectedPort.HasValue)
            {
                UnityEngine.Debug.Log($"<color=green>[UDPPoseServer on {gameObjectName}] ✅ AUTO-DETECTED port: {detectedPort.Value}</color>");
                UnityEngine.Debug.Log($"<color=green>[UDPPoseServer on {gameObjectName}] Overriding serialized value {listenPort} with detected port {detectedPort.Value}</color>");
                listenPort = detectedPort.Value;
            }
            // Fallback: Check for static override (legacy support)
            else if (portOverride.HasValue)
            {
                UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] 🔧 Using static port override: {portOverride.Value}");
                UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] Overriding serialized value {listenPort} with static override {portOverride.Value}");
                listenPort = portOverride.Value;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[UDPPoseServer on {gameObjectName}] ⚠️ NO port detected or overridden!");
                UnityEngine.Debug.LogWarning($"[UDPPoseServer on {gameObjectName}] Using serialized field value: {listenPort}");
            }

            UnityEngine.Debug.Log($"<color=cyan>[UDPPoseServer on {gameObjectName}] FINAL listen port: {listenPort}</color>");

            // DIAGNOSTIC: Check if another UDPPoseServer is already running
            UDPPoseServer[] allServers = FindObjectsOfType<UDPPoseServer>();
            if (allServers.Length > 1)
            {
                UnityEngine.Debug.LogWarning($"⚠️ [UDPPoseServer] Found {allServers.Length} UDPPoseServer instances:");
                foreach (var server in allServers)
                {
                    UnityEngine.Debug.LogWarning($"  - {server.gameObject.name} (port {server.listenPort}, enabled={server.enabled})");
                }
                UnityEngine.Debug.LogWarning($"⚠️ Multiple servers may cause port binding conflicts!");
            }

            lock (dataLock) { latestPoseData = null; hasNewData = false; }
            StartServer();
        }

        /// <summary>
        /// Automatically detects which avatar is camera-controlled (has active UDPPoseStreamer)
        /// and returns the correct port to RECEIVE the OTHER avatar's data.
        /// Logic:
        /// - If Remy is camera-controlled → receive Adam's data on port 52734
        /// - If Adam is camera-controlled → receive Remy's data on port 52733
        /// </summary>
        private int? DetectCorrectPort()
        {
            UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] 🔍 Detecting which avatar is camera-controlled...");

            // Find all UDPPoseStreamers in the scene (including inactive ones)
            UDPPoseStreamer[] allStreamers = FindObjectsOfType<UDPPoseStreamer>(true); // true = include inactive
            UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] Found {allStreamers.Length} UDPPoseStreamer(s) in scene (including inactive)");

            // Track what we find
            UDPPoseStreamer remyStreamer = null;
            UDPPoseStreamer adamStreamer = null;

            foreach (var streamer in allStreamers)
            {
                string avatarName = streamer.gameObject.name;
                bool isActive = streamer.gameObject.activeInHierarchy && streamer.enabled;

                UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}]   - {avatarName}: activeInHierarchy={streamer.gameObject.activeInHierarchy}, enabled={streamer.enabled}, isActive={isActive}");

                if (avatarName.Contains("Remy"))
                {
                    remyStreamer = streamer;
                }
                else if (avatarName.Contains("Adam"))
                {
                    adamStreamer = streamer;
                }
            }

            // Now check which one is enabled
            if (remyStreamer != null && remyStreamer.enabled)
            {
                UnityEngine.Debug.Log($"<color=yellow>[UDPPoseServer on {gameObjectName}] 🎯 Remy's streamer is ENABLED → Remy is camera-controlled → Need to receive Adam's data on port 52734</color>");
                return 52734;
            }
            else if (adamStreamer != null && adamStreamer.enabled)
            {
                UnityEngine.Debug.Log($"<color=yellow>[UDPPoseServer on {gameObjectName}] 🎯 Adam's streamer is ENABLED → Adam is camera-controlled → Need to receive Remy's data on port 52733</color>");
                return 52733;
            }

            UnityEngine.Debug.LogWarning($"[UDPPoseServer on {gameObjectName}] ⚠️ Could not detect enabled streamer!");
            if (remyStreamer != null)
            {
                UnityEngine.Debug.LogWarning($"  Remy streamer found but enabled={remyStreamer.enabled}");
            }
            if (adamStreamer != null)
            {
                UnityEngine.Debug.LogWarning($"  Adam streamer found but enabled={adamStreamer.enabled}");
            }

            return null;
        }
        
        private void StartServer()
        {
            try
            {
                UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] Attempting to create UDP server on port {listenPort}");
                udpServer = new UdpClient(listenPort);

                // ========================================
                // Android-specific UDP socket configuration
                // ========================================
                #if UNITY_ANDROID || UNITY_IOS
                try
                {
                    // Enable address reuse - helps with reconnection on mobile
                    udpServer.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] 📱 Mobile: Enabled SO_REUSEADDR");

                    // Disable linger - prevents hanging connections on mobile
                    udpServer.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                    UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] 📱 Mobile: Disabled socket linger");

                    // Set larger receive buffer for mobile - helps prevent packet loss
                    udpServer.Client.ReceiveBufferSize = 256 * 1024; // 256KB
                    UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] 📱 Mobile: Set receive buffer to 256KB");

                    // Enable broadcast (helps with local network discovery)
                    udpServer.EnableBroadcast = true;
                    UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName}] 📱 Mobile: Enabled broadcast");
                }
                catch (Exception mobileEx)
                {
                    UnityEngine.Debug.LogWarning($"[UDPPoseServer on {gameObjectName}] ⚠️ Mobile socket configuration warning: {mobileEx.Message}");
                    UnityEngine.Debug.LogWarning($"[UDPPoseServer on {gameObjectName}] This is not critical - socket may still work");
                }
                #endif

                UnityEngine.Debug.Log($"<color=green>[UDPPoseServer on {gameObjectName}] ✅ UDP Server successfully bound to port {listenPort}</color>");
                UnityEngine.Debug.Log($"<color=green>[UDPPoseServer on {gameObjectName}] ✅ Socket is listening on 0.0.0.0:{listenPort} (all interfaces)</color>");

                shouldRun = true;
                receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start();
                UnityEngine.Debug.Log($"<color=green>[UDPPoseServer on {gameObjectName}] ✅ Receive thread started</color>");
            }
            catch (SocketException e)
            {
                UnityEngine.Debug.LogError($"[UDPPoseServer on {gameObjectName}] ❌ SOCKET ERROR: {e.Message}");
                UnityEngine.Debug.LogError($"[UDPPoseServer on {gameObjectName}] Error code: {e.ErrorCode}");

                if (e.ErrorCode == 10048 || e.Message.Contains("already in use"))
                {
                    UnityEngine.Debug.LogError($"[UDPPoseServer on {gameObjectName}] 🔥 PORT {listenPort} IS ALREADY IN USE!");
                    UnityEngine.Debug.LogError($"[UDPPoseServer on {gameObjectName}] Another socket is bound to this port");
                    UnityEngine.Debug.LogError($"[UDPPoseServer on {gameObjectName}] Check for duplicate UDPPoseServer components or external programs");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[UDPPoseServer on {gameObjectName}] ❌ FAILED to start UDP server: {e.Message}");
                UnityEngine.Debug.LogError($"[UDPPoseServer on {gameObjectName}] Full error: {e.ToString()}");
            }
        }
        
        private void ReceiveData()
        {
            // NOTE: listenPort was already set in Start() before StartServer() was called
            // The UDP socket is already bound to the correct port at this point
            UnityEngine.Debug.Log($"<color=yellow>[UDPPoseServer on {gameObjectName}] 🧵 Receive thread ENTERED</color>");
            UnityEngine.Debug.Log($"<color=yellow>[UDPPoseServer on {gameObjectName}] 🧵 Thread ID: {Thread.CurrentThread.ManagedThreadId}</color>");
            UnityEngine.Debug.Log($"<color=yellow>[UDPPoseServer on {gameObjectName}] 🧵 Waiting for UDP packets on port {listenPort}...</color>");

            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
            while (shouldRun)
            {
                try
                {
                    byte[] data = udpServer.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data);
                    
                    // Handle handshake PING message
                    if (message == "PING")
                    {
                        UnityEngine.Debug.Log($"<color=cyan>📡 PING received from {remoteEndPoint.Address}:{remoteEndPoint.Port}. Sending PONG...</color>");
                        
                        // Send PONG response
                        byte[] pongData = Encoding.UTF8.GetBytes("PONG");
                        udpServer.Send(pongData, pongData.Length, remoteEndPoint);
                        continue; // Don't process as pose data
                    }
                    
                    // UDP preserves datagram boundaries. Never join unrelated/incomplete packets.
                    if (!message.EndsWith("<EOM>", StringComparison.Ordinal)) continue;
                    PoseData poseData = ParsePoseData(message.Substring(0, message.Length - 5));
                    if (poseData == null) continue;
                    lock (dataLock)
                    {
                        latestPoseData = poseData;
                        hasNewData = true;
                        totalPacketsReceived++;
                    }

                }
                catch (SocketException)
                {
                    // Socket closed, exit gracefully
                    break;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Error receiving data: {e.Message}");
                }
            }
        }
        
        private PoseData ParsePoseData(string message)
        {
            Vector3[] world, screen;
            if (!PoseWireFormat.TryParse(message, out world, out screen)) return null;
            return new PoseData { worldLandmarks = world, screenLandmarks = screen };
        }

        private void Update()
        {
            PoseData poseData;
            lock (dataLock)
            {
                if (!hasNewData) return;
                poseData = latestPoseData;
                hasNewData = false;
            }
            ApplyPoseData(poseData);
            OnPoseDataReceived?.Invoke(poseData);
            UpdateFPS();
        }

        private void ApplyPoseData(PoseData poseData)
        {
            if (targetPoseProvider == null || targetPoseProvider.Null || poseData == null)
            {
                if (showDebugInfo && Time.frameCount % 120 == 0) // Log every ~2 seconds
                {
                    UnityEngine.Debug.LogWarning($"[UDPPoseServer] Cannot apply data: Provider={targetPoseProvider != null}, ProviderNull={targetPoseProvider?.Null}, Data={poseData != null}");
                }
                return;
            }
            
            try
            {
                // Apply world landmarks to the pose provider
                for (int i = 0; i < 33 && i < poseData.worldLandmarks.Length; i++)
                {
                    var landmark = targetPoseProvider.Get(targetGroup, (PoseLandmarks)i);
                    landmark.Position = poseData.worldLandmarks[i];
                }
                
                // CRITICAL: Trigger the OnUpdated event to notify subscribers (like the puppet)
                TriggerProviderUpdate();
                
                if (showDebugInfo && Time.frameCount % 120 == 0)
                {
                    UnityEngine.Debug.Log($"[UDPPoseServer] ✅ Applied pose data to group {targetGroup} + Triggered update");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error applying pose data: {e.Message}");
            }
        }
        
        private void TriggerProviderUpdate()
        {
            try
            {
                // Access the underlying provider's lifecycle
                var lifecycle = targetPoseProvider?.Provider?.Lifecycle;
                if (lifecycle == null) return;
                
                // Use reflection to invoke the OnUpdated event
                // The event is stored in a private field that backs the public event property
                var lifecycleType = lifecycle.GetType();
                
                // Try to find the OnUpdated event backing field
                // Event backing fields are usually named "OnUpdated" or with a prefix
                var eventField = lifecycleType.GetField("OnUpdated", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public);
                
                if (eventField == null)
                {
                    // Try lowercase with underscores (common pattern)
                    eventField = lifecycleType.GetField("onUpdated", 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.NonPublic);
                }
                
                if (eventField != null)
                {
                    var eventDelegate = eventField.GetValue(lifecycle) as GroupEvent;
                    if (eventDelegate != null)
                    {
                        // Invoke all subscribers
                        eventDelegate.Invoke(targetGroup);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[UDPPoseServer] Could not trigger provider update: {e.Message}");
            }
        }
        
        private void UpdateFPS()
        {
            frameCount++;
            float currentTime = Time.time;
            float deltaTime = currentTime - lastFrameTime;
            
            if (deltaTime >= 1f)
            {
                avgFPS = frameCount / deltaTime;
                frameCount = 0;
                lastFrameTime = currentTime;
                
                if (showFPS)
                {
                    UnityEngine.Debug.Log($"UDP Pose Receive FPS: {avgFPS:F1} (Total packets: {totalPacketsReceived})");
                }
            }
        }
        
        private void OnDisable()
        {
            UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName ?? gameObject.name}] OnDisable() called - cleaning up UDP resources...");

            shouldRun = false;
            udpServer?.Close(); // Release blocking Receive before waiting for the worker.

            if (receiveThread != null && receiveThread.IsAlive)
            {
                UnityEngine.Debug.Log($"[UDPPoseServer] Stopping receive thread...");
                receiveThread.Join(500);
            }

            if (udpServer != null)
            {
                UnityEngine.Debug.Log($"[UDPPoseServer] Closing UDP socket...");
                udpServer.Close();
                udpServer = null;
            }

            UnityEngine.Debug.Log($"[UDPPoseServer] Cleanup complete");
        }

        private void OnDestroy()
        {
            UnityEngine.Debug.Log($"[UDPPoseServer on {gameObjectName ?? gameObject.name}] OnDestroy() called");

            shouldRun = false;
            udpServer?.Close(); // Release blocking Receive before waiting for the worker.

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(500);
            }

            if (udpServer != null)
            {
                udpServer.Close();
                udpServer = null;
            }
        }
        
        // OnGUI disabled - UI hidden for production builds
        /*
        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 220, 300, 200));
            GUILayout.Label($"UDP Pose Server Status", new GUIStyle(GUI.skin.label)
                { fontSize = 16, fontStyle = FontStyle.Bold });
            GUILayout.Label($"Listening on port: {listenPort}");
            GUILayout.Label($"Has data: {hasNewData}");
            GUILayout.Label($"Total packets: {totalPacketsReceived}");
            GUILayout.Label($"FPS: {avgFPS:F1}");
            GUILayout.EndArea();
        }
        */
        
        // Public accessors
        public int ListenPort => listenPort;
        public float CurrentFPS => avgFPS;
        public int TotalPacketsReceived => totalPacketsReceived;

        /// <summary>
        /// Sets a static port override that survives component re-initialization.
        /// This is necessary because Unity resets serialized fields when SetActive is toggled.
        /// </summary>
        public static void SetPortOverride(int port)
        {
            UnityEngine.Debug.Log($"[UDPPoseServer] 🔧 Setting STATIC port override to {port}");
            UnityEngine.Debug.Log($"[UDPPoseServer] 🔍 DIAGNOSTIC: Before set - portOverride.HasValue = {portOverride.HasValue}");
            portOverride = port;
            UnityEngine.Debug.Log($"[UDPPoseServer] 🔍 DIAGNOSTIC: After set - portOverride.HasValue = {portOverride.HasValue}, Value = {portOverride.Value}");
        }

        /// <summary>
        /// Gets the current static port override value (for diagnostics).
        /// </summary>
        public static int? GetPortOverride()
        {
            return portOverride;
        }

        /// <summary>
        /// Clears the static port override.
        /// </summary>
        public static void ClearPortOverride()
        {
            UnityEngine.Debug.Log($"[UDPPoseServer] 🔧 Clearing STATIC port override");
            portOverride = null;
        }
    }
    
    /// <summary>
    /// Holds parsed pose data from MediaPipe format
    /// </summary>
    [Serializable]
    public class PoseData
    {
        public Vector3[] worldLandmarks;    // 3D world coordinates (33 landmarks)
        public Vector3[] screenLandmarks;   // Normalized 2D coordinates (33 landmarks)
    }
}
