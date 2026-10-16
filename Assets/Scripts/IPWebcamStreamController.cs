using UnityEngine;
using System;

namespace Tracking4All
{
    /// <summary>
    /// Simple controller to start/stop IP webcam streaming to avatar.
    /// Attach this to any GameObject in your scene and call StartStream() to begin.
    /// </summary>
    public class IPWebcamStreamController : MonoBehaviour
    {
        [Header("Webcam Configuration")]
        [Tooltip("IP address of the webcam (e.g., your phone's IP: 192.168.1.100)")]
        public string webcamIP = "192.168.1.100";
        
        [Tooltip("Webcam port (IP Webcam app default: 8080)")]
        public int webcamPort = 8080;
        
        [Tooltip("Stream path (usually /video or /mjpeg)")]
        public string streamPath = "/video";
        
        [Header("Target Unity Receiver")]
        [Tooltip("IP of the Unity instance receiving pose data (127.0.0.1 for same machine)")]
        public string targetIP = "127.0.0.1";
        
        [Tooltip("UDP port that your pose tracking solution is listening on")]
        public int targetPort = 52733;
        
        [Header("Pose Provider")]
        [Tooltip("Drag the GameObject with your pose tracking solution here (e.g., MPPPoseSolution)")]
        [SerializeField] public LandmarkProvider<PoseLandmarks> poseProvider;
        
        [Header("Options")]
        [Tooltip("Automatically start streaming when scene starts")]
        public bool autoStartOnPlay = false;
        
        [Tooltip("Show debug messages in console")]
        public bool showDebugLogs = true;
        
        [Header("Status (Read-Only)")]
        [SerializeField] private bool isStreaming = false;
        [SerializeField] private bool isConnected = false;
        
        // Internal components
        private NetworkWebcamClient webcamClient;
        private UDPPoseClient udpClient;
        
        // Events you can subscribe to
        public event Action OnStreamStarted;
        public event Action OnStreamStopped;
        public event Action OnConnectionEstablished;
        public event Action OnConnectionLost;
        
        void Start()
        {
            if (autoStartOnPlay)
            {
                StartStream();
            }
        }
        
        /// <summary>
        /// Start streaming from IP webcam to avatar.
        /// Call this method to begin the stream.
        /// </summary>
        public void StartStream()
        {
            if (isStreaming)
            {
                LogMessage("Stream already running!", true);
                return;
            }
            
            // Validate pose provider
            if (poseProvider == null || poseProvider.Null)
            {
                LogMessage("ERROR: Pose Provider not assigned! Please assign it in the Inspector.", true);
                return;
            }
            
            LogMessage($"Starting stream from {webcamIP}:{webcamPort} to {targetIP}:{targetPort}");
            
            // Setup webcam client
            SetupWebcamClient();
            
            // Setup UDP client
            SetupUDPClient();
            
            // Connect to pose provider events
            poseProvider.OnLandmarksUpdated += OnPoseDataReceived;
            
            isStreaming = true;
            OnStreamStarted?.Invoke();
            
            LogMessage("Stream started successfully!");
        }
        
        /// <summary>
        /// Stop the current stream.
        /// Call this method to stop streaming.
        /// </summary>
        public void StopStream()
        {
            if (!isStreaming)
            {
                LogMessage("No stream to stop.", true);
                return;
            }
            
            LogMessage("Stopping stream...");
            
            // Disconnect from pose provider
            if (poseProvider != null && !poseProvider.Null)
            {
                poseProvider.OnLandmarksUpdated -= OnPoseDataReceived;
            }
            
            // Cleanup webcam
            if (webcamClient != null)
            {
                webcamClient.OnConnected -= OnWebcamConnected;
                webcamClient.OnDisconnected -= OnWebcamDisconnected;
                Destroy(webcamClient);
                webcamClient = null;
            }
            
            // Cleanup UDP
            if (udpClient != null)
            {
                udpClient.OnConnected -= OnUDPConnected;
                udpClient.OnDisconnected -= OnUDPDisconnected;
                Destroy(udpClient);
                udpClient = null;
            }
            
            isStreaming = false;
            isConnected = false;
            OnStreamStopped?.Invoke();
            
            LogMessage("Stream stopped.");
        }
        
        /// <summary>
        /// Toggle streaming on/off.
        /// Useful for UI buttons.
        /// </summary>
        public void ToggleStream()
        {
            if (isStreaming)
                StopStream();
            else
                StartStream();
        }
        
        private void SetupWebcamClient()
        {
            if (webcamClient == null)
            {
                webcamClient = gameObject.AddComponent<NetworkWebcamClient>();
            }
            
            // Configure webcam settings
            webcamClient.WebcamIP = webcamIP;
            webcamClient.WebcamPort = webcamPort;
            webcamClient.StreamPath = streamPath;
            webcamClient.AutoConnect = false; // We'll connect manually
            
            webcamClient.OnConnected += OnWebcamConnected;
            webcamClient.OnDisconnected += OnWebcamDisconnected;
            
            // Start connection
            webcamClient.Connect();
        }
        
        private void SetupUDPClient()
        {
            if (udpClient == null)
            {
                udpClient = gameObject.AddComponent<UDPPoseClient>();
            }
            
            // Configure UDP target
            udpClient.TargetIP = targetIP;
            udpClient.TargetPort = targetPort;
            
            udpClient.OnConnected += OnUDPConnected;
            udpClient.OnDisconnected += OnUDPDisconnected;
        }
        
        private void OnPoseDataReceived(int group)
        {
            if (!isStreaming || udpClient == null || !udpClient.IsConnected)
                return;
            
            if (poseProvider == null || poseProvider.Null)
                return;
            
            // Build pose data string in same format as Python implementation
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            // World coordinates (33 pose landmarks)
            for (int i = 0; i < 33; i++)
            {
                var landmark = poseProvider.Get(group, (PoseLandmarks)i);
                Vector3 pos = landmark.Position;
                sb.AppendLine($"{i}|{pos.x}|{pos.y}|{pos.z}");
            }
            
            sb.Append("*||*");
            
            // Normalized coordinates
            for (int i = 0; i < 33; i++)
            {
                var landmark = poseProvider.Get(group, (PoseLandmarks)i);
                Vector3 pos = landmark.Position;
                sb.AppendLine($"{i}|{pos.x}|{pos.y}|{pos.z}");
            }
            
            // Send via UDP
            udpClient.SendMessage(sb.ToString());
        }
        
        // Event callbacks
        private void OnWebcamConnected()
        {
            LogMessage("Webcam connected!");
        }
        
        private void OnWebcamDisconnected()
        {
            LogMessage("Webcam disconnected!", true);
            OnConnectionLost?.Invoke();
        }
        
        private void OnUDPConnected()
        {
            isConnected = true;
            LogMessage("UDP connection established!");
            OnConnectionEstablished?.Invoke();
        }
        
        private void OnUDPDisconnected()
        {
            isConnected = false;
            LogMessage("UDP connection lost!", true);
            OnConnectionLost?.Invoke();
        }
        
        private void LogMessage(string message, bool isWarning = false)
        {
            if (!showDebugLogs)
                return;
            
            string prefix = "[IPWebcamStreamController] ";
            
            if (isWarning)
                UnityEngine.Debug.LogWarning(prefix + message);
            else
                UnityEngine.Debug.Log(prefix + message);
        }
        
        void OnDestroy()
        {
            if (isStreaming)
            {
                StopStream();
            }
        }
        
        // Public getters for status
        public bool IsStreaming => isStreaming;
        public bool IsConnected => isConnected;
        public string WebcamURL => $"http://{webcamIP}:{webcamPort}{streamPath}";
        public string TargetURL => $"{targetIP}:{targetPort}";
    }
}
