using UnityEngine;
using System;
using System.Text;

namespace Tracking4All
{
    /// <summary>
    /// Simplified webcam to pose streaming client.
    /// Captures from IP webcam and streams to MediaPipe Python backend,
    /// or directly processes and sends pose data.
    /// </summary>
    public class SimpleIPWebcamPoseStreamer : MonoBehaviour
    {
        [Header("IP Webcam Settings")]
        [Tooltip("IP address of the webcam (e.g., 192.168.1.100)")]
        public string webcamIP = "192.168.1.100";
        
        [Tooltip("Port of the webcam stream (common: 8080, 4747)")]
        public int webcamPort = 8080;
        
        [Tooltip("Stream path (e.g., /video, /mjpeg, /shot.jpg)")]
        public string streamPath = "/video";
        
        [Header("Pose Provider")]
        [Tooltip("Assign the GameObject with your pose solution (e.g., MPPPoseSolution or PosePuppet)")]
        [SerializeField] private LandmarkProvider<PoseLandmarks> poseProvider;
        
        [Header("Pose Streaming Settings")]
        [Tooltip("Target Unity instance IP to send pose data")]
        public string targetUnityIP = "127.0.0.1";
        
        [Tooltip("Target Unity UDP port (must match receiver)")]
        public int targetUnityPort = 52733;
        
        [Header("Options")]
        public bool autoStart = true;
        public bool debugMode = true;
        
        private NetworkWebcamClient webcamClient;
        private UDPPoseClient udpClient;
        
        private bool isStreaming = false;
        
        void Start()
        {
            if (autoStart)
            {
                StartStreaming();
            }
        }
        
        public void StartStreaming()
        {
            if (isStreaming)
            {
                UnityEngine.Debug.LogWarning("Already streaming");
                return;
            }
            
            // Validate pose provider
            if (poseProvider == null || poseProvider.Null)
            {
                UnityEngine.Debug.LogError("Pose Provider not assigned! Please assign a GameObject with ILandmarkProvider<PoseLandmarks> in the Inspector.");
                return;
            }
            
            // Setup webcam client
            webcamClient = gameObject.AddComponent<NetworkWebcamClient>();
            // Note: You'll need to expose these as public properties in NetworkWebcamClient
            // or use reflection/SendMessage to set them
            
            // Setup UDP client
            udpClient = gameObject.AddComponent<UDPPoseClient>();
            
            // Connect to pose provider
            poseProvider.OnLandmarksUpdated += OnPoseDetected;
            
            isStreaming = true;
            UnityEngine.Debug.Log("Started streaming from IP webcam");
        }
        
        public void StopStreaming()
        {
            isStreaming = false;
            
            if (poseProvider != null && !poseProvider.Null)
            {
                poseProvider.OnLandmarksUpdated -= OnPoseDetected;
            }
            
            if (webcamClient != null)
            {
                Destroy(webcamClient);
            }
            
            if (udpClient != null)
            {
                Destroy(udpClient);
            }
            
            UnityEngine.Debug.Log("Stopped streaming");
        }
        
        private void OnPoseDetected(int group)
        {
            if (!isStreaming || udpClient == null || !udpClient.IsConnected)
                return;
            
            if (poseProvider == null || poseProvider.Null)
                return;
            
            // Format and send pose data
            StringBuilder sb = new StringBuilder();
            
            // Send world coordinates (33 pose landmarks)
            for (int i = 0; i < 33; i++)
            {
                var landmark = poseProvider.Get(group, (PoseLandmarks)i);
                Vector3 pos = landmark.Position;
                sb.AppendLine($"{i}|{pos.x}|{pos.y}|{pos.z}");
            }
            
            sb.Append("*||*");
            
            // Send normalized coordinates
            for (int i = 0; i < 33; i++)
            {
                var landmark = poseProvider.Get(group, (PoseLandmarks)i);
                Vector3 pos = landmark.Position;
                sb.AppendLine($"{i}|{pos.x}|{pos.y}|{pos.z}");
            }
            
            udpClient.SendMessage(sb.ToString());
        }
        
        void OnDestroy()
        {
            StopStreaming();
        }
    }
}
