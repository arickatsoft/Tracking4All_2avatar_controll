using UnityEngine;
using System.Text;

namespace Tracking4All
{
    /// <summary>
    /// Streams webcam-based pose tracking data via UDP to another Unity instance.
    /// Converts Python MediaPipe body.py logic to C# for Unity.
    /// Can work with local webcam or network webcam (IP-based).
    /// </summary>
    public class PoseStreamer : MonoBehaviour
    {
        [Header("Webcam Source")]
        [SerializeField] private bool useNetworkWebcam = false;
        [SerializeField] private NetworkWebcamClient networkWebcam;
        [SerializeField] private WebCamTexture localWebcam;
        [SerializeField] private int localWebcamIndex = 0;
        [SerializeField] private int targetFPS = 30;
        [SerializeField] private Vector2Int resolution = new Vector2Int(640, 480);
        
        [Header("UDP Streaming")]
        [SerializeField] private UDPPoseClient udpClient;
        [SerializeField] private string targetIP = "127.0.0.1";
        [SerializeField] private int targetPort = 52733;
        
        [Header("MediaPipe Settings")]
        [SerializeField] private int modelComplexity = 1; // 0-2
        [SerializeField] private float minDetectionConfidence = 0.5f;
        [SerializeField] private float minTrackingConfidence = 0.5f;
        
        [Header("Pose Provider")]
        [Tooltip("Assign the GameObject with your pose solution (e.g., MPPPoseSolution or PosePuppet)")]
        [SerializeField] private LandmarkProvider<PoseLandmarks> poseProvider;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showFPS = true;
        
        // Performance tracking
        private float lastFrameTime;
        private int frameCount;
        private float avgFPS;
        
        private void Start()
        {
            SetupWebcam();
            SetupUDP();
            SetupPoseProvider();
        }
        
        private void SetupWebcam()
        {
            if (useNetworkWebcam)
            {
                if (networkWebcam == null)
                {
                    networkWebcam = gameObject.AddComponent<NetworkWebcamClient>();
                }
                networkWebcam.OnFrameReceived += OnNetworkFrameReceived;
            }
            else
            {
                // Setup local webcam
                WebCamDevice[] devices = WebCamTexture.devices;
                if (devices.Length > localWebcamIndex)
                {
                    localWebcam = new WebCamTexture(devices[localWebcamIndex].name, 
                        resolution.x, resolution.y, targetFPS);
                    localWebcam.Play();
                    UnityEngine.Debug.Log($"Started local webcam: {devices[localWebcamIndex].name}");
                }
                else
                {
                    UnityEngine.Debug.LogError("No webcam found at specified index");
                }
            }
        }
        
        private void SetupUDP()
        {
            if (udpClient == null)
            {
                udpClient = gameObject.AddComponent<UDPPoseClient>();
            }
        }
        
        private void SetupPoseProvider()
        {
            if (poseProvider == null || poseProvider.Null)
            {
                UnityEngine.Debug.LogError("Pose Provider not assigned! Please assign a GameObject with ILandmarkProvider<PoseLandmarks> in the Inspector.");
                return;
            }
            
            poseProvider.OnLandmarksUpdated += OnPoseUpdated;
        }
        
        private void OnNetworkFrameReceived(byte[] frameData)
        {
            // Process network frame (decode JPEG/PNG and feed to MediaPipe)
            UnityEngine.Debug.Log($"Received network frame: {frameData.Length} bytes");
            // You would decode and process the frame here
        }
        
        private void OnPoseUpdated(int group)
        {
            if (poseProvider == null || poseProvider.Null || udpClient == null || !udpClient.IsConnected)
                return;
            
            // Build pose data string in same format as Python implementation
            StringBuilder dataBuilder = new StringBuilder();
            
            try
            {
                // Get world landmarks (3D coordinates)
                // MediaPipe provides 33 pose landmarks (0-32)
                for (int i = 0; i < 33; i++)
                {
                    var landmark = poseProvider.Get(group, (PoseLandmarks)i);
                    //if (landmark != null && !landmark.Null)
                    //{
                        Vector3 pos = landmark.Position;
                        dataBuilder.Append($"{i}|{pos.x}|{pos.y}|{pos.z}\n");
                    //}
                    //else
                    //{
                    //    // Send zero position if landmark not available
                    //    dataBuilder.Append($"{i}|0|0|0\n");
                    //}
                }
                
                // Add separator
                dataBuilder.Append("*||*");
                
                // Get screen landmarks (normalized 2D coordinates)
                // For Unity MediaPipe, we use the same Position data
                // The receiver can interpret them as needed
                for (int i = 0; i < 33; i++)
                {
                    var landmark = poseProvider.Get(group, (PoseLandmarks)i);
                    //if (landmark != null && !landmark.Null)
                    //{
                        Vector3 pos = landmark.Position;
                        dataBuilder.Append($"{i}|{pos.x}|{pos.y}|{pos.z}\n");
                    //}
                    //else
                    //{
                    //    // Send zero position if landmark not available
                    //    dataBuilder.Append($"{i}|0|0|0\n");
                    //}
                }
                
                // Send via UDP
                string poseData = dataBuilder.ToString();
                udpClient.SendMessage(poseData);
                
                // Update FPS counter
                UpdateFPS();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error building pose data: {e.Message}");
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
                    UnityEngine.Debug.Log($"Pose Streaming FPS: {avgFPS:F1}");
                }
            }
        }
        
        private void Update()
        {
            // Update local webcam processing if needed
            if (!useNetworkWebcam && localWebcam != null && localWebcam.isPlaying)
            {
                // Feed frames to MediaPipe solution if needed
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup
            if (localWebcam != null && localWebcam.isPlaying)
            {
                localWebcam.Stop();
            }
            
            if (networkWebcam != null)
            {
                networkWebcam.OnFrameReceived -= OnNetworkFrameReceived;
            }
            
            if (poseProvider != null)
            {
                poseProvider.OnLandmarksUpdated -= OnPoseUpdated;
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Pose Streamer Status", new GUIStyle(GUI.skin.label) 
                { fontSize = 16, fontStyle = FontStyle.Bold });
            GUILayout.Label($"Webcam: {(useNetworkWebcam ? "Network" : "Local")}");
            GUILayout.Label($"UDP Connected: {(udpClient != null ? udpClient.IsConnected : false)}");
            GUILayout.Label($"Target: {targetIP}:{targetPort}");
            GUILayout.Label($"FPS: {avgFPS:F1}");
            GUILayout.EndArea();
        }
    }
}
