using UnityEngine;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking4All
{
    /// <summary>
    /// Streams pose data from local MediaPipe to remote Unity instance via UDP.
    /// Uses the EXACT same format as Python networkedwebcam script.
    /// 
    /// Device 1 (this): Local webcam → MediaPipe → Local avatar + UDP stream
    /// Device 2 (remote): UDP receive → Remote avatar
    /// </summary>
    public class PoseDataUDPStreamer : MonoBehaviour
    {
        [Header("Pose Source")]
        [Tooltip("Drag the GameObject with your pose tracking solution (e.g., the one controlling Bryce)")]
        [SerializeField] private LandmarkProvider<PoseLandmarks> poseProvider;

        [Header("UDP Target")]
        [Tooltip("IP of remote Unity instance (e.g., 192.168.1.200)")]
        public string targetIP = "192.168.1.200";

        [Tooltip("UDP port (must match ServerUDP on remote, default: 52733)")]
        public int targetPort = 52733;

        [Header("Options")]
        [Tooltip("Start streaming automatically when scene starts")]
        public bool autoStart = true;

        [Tooltip("Show debug logs")]
        public bool showDebugLogs = true;

        [Header("Status (Read-Only)")]
        [SerializeField] private bool isStreaming = false;
        [SerializeField] private int packetsSent = 0;
        [SerializeField] private float streamFPS = 0f;

        // Networking
        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
        private Thread sendThread;
        private bool shouldRun = false;

        // Data queue (thread-safe)
        private readonly object queueLock = new object();
        private string latestPoseData = null;
        private bool hasNewData = false;

        // FPS tracking
        private int lastPacketCount = 0;
        private float lastFPSCheck = 0f;

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
                UnityEngine.Debug.Log("Already streaming");
                return;
            }

            // Validate pose provider
            if (poseProvider == null || poseProvider.Null)
            {
                UnityEngine.Debug.Log("ERROR: Pose provider not assigned!");
                return;
            }

            UnityEngine.Debug.Log($"Starting UDP stream to {targetIP}:{targetPort}");

            // Setup UDP
            try
            {
                udpClient = new UdpClient();
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log($"Failed to create UDP client: {e.Message}");
                return;
            }

            // Start send thread
            shouldRun = true;
            sendThread = new Thread(SendLoop);
            sendThread.IsBackground = true;
            sendThread.Start();

            isStreaming = true;
            UnityEngine.Debug.Log("Streaming started!");
        }

        public void StopStreaming()
        {
            if (!isStreaming)
            {
                UnityEngine.Debug.Log("Not streaming");
                return;
            }

            UnityEngine.Debug.Log("Stopping stream...");

            shouldRun = false;
            isStreaming = false;

            // Wait for thread to finish (give it time to exit cleanly)
            if (sendThread != null && sendThread.IsAlive)
            {
                sendThread.Join(1000); // Wait up to 1 second
            }

            // Now safe to close UDP
            if (udpClient != null)
            {
                try
                {
                    udpClient.Close();
                    udpClient = null;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning($"Error closing UDP: {e.Message}");
                }
            }

            UnityEngine.Debug.Log("Streaming stopped");
        }

        //private void Update()
        //{
        //    if (isStreaming && poseProvider != null && !poseProvider.Null)
        //    {
        //        CapturePoseData(0); // Use group 0
        //    }
        //}

        private void CapturePoseData(int group)
        {
            if (!isStreaming || poseProvider == null || poseProvider.Null)
                return;

            // Build pose data string in EXACT Python format
            StringBuilder sb = new StringBuilder();

            // World coordinates (33 pose landmarks)
            for (int i = 0; i < 33; i++)
            {
                var landmark = poseProvider.Get(group, (PoseLandmarks)i);
                if (landmark.Position!= null)
                {
                    Vector3 pos = landmark.Position;
                    sb.AppendLine($"{i}|{pos.x}|{pos.y}|{pos.z}");
                }
                else
                {
                    sb.AppendLine($"{i}|0|0|0");
                }
            }

            // Separator (same as Python)
            sb.Append("*||*");

            // Normalized coordinates (33 landmarks)
            for (int i = 0; i < 33; i++)
            {
                var landmark = poseProvider.Get(group, (PoseLandmarks)i);
                if (landmark.Position!=null)
                {
                    Vector3 pos = landmark.Position;
                    sb.AppendLine($"{i}|{pos.x}|{pos.y}|{pos.z}");
                }
                else
                {
                    sb.AppendLine($"{i}|0|0|0");
                }
            }

            // Queue for sending (thread-safe)
            lock (queueLock)
            {
                latestPoseData = sb.ToString();
                hasNewData = true;
            }
        }
    

        private void SendLoop()
        {
            while (shouldRun)
            {
                string dataToSend = null;

                // Check if we have new data (thread-safe)
                lock (queueLock)
                {
                    if (hasNewData)
                    {
                        dataToSend = latestPoseData;
                        hasNewData = false;
                    }
                }

                // Send if we have data
                if (dataToSend != null && shouldRun) // Double-check shouldRun
                {
                    try
                    {
                        // Check if client is still valid
                        if (udpClient != null)
                        {
                            // Add end-of-message marker (same as Python)
                            string message = dataToSend + "<EOM>";
                            byte[] data = Encoding.UTF8.GetBytes(message);

                            udpClient.Send(data, data.Length, remoteEndPoint);
                            
                            // Thread-safe increment
                            lock (queueLock)
                            {
                                packetsSent++;
                            }
                        }
                        else
                        {
                            // Client is null, exit thread
                            break;
                        }
                    }
                    catch (System.ObjectDisposedException)
                    {
                        // Socket was closed, exit gracefully
                        break;
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        // Network error, exit gracefully
                        break;
                    }
                    catch (System.Exception)
                    {
                        // Other errors, continue trying
                    }
                }
                else
                {
                    // No data, sleep briefly
                    Thread.Sleep(1);
                }
            }
        }

        void Update()
        {
            if (isStreaming && poseProvider != null && !poseProvider.Null)
            {
                CapturePoseData(0); // Use group 0
            }
            
            // Update FPS counter (safe in main thread)
            if (Time.time - lastFPSCheck >= 1f)
            {
                int currentPackets;
                lock (queueLock)
                {
                    currentPackets = packetsSent;
                }
                
                streamFPS = (currentPackets - lastPacketCount) / (Time.time - lastFPSCheck);
                lastPacketCount = currentPackets;
                lastFPSCheck = Time.time;
                
                // Optional debug logging
                if (showDebugLogs)
                {
                    UnityEngine.Debug.Log($"<color=cyan>UDP Stream: {streamFPS:F1} fps, Total: {currentPackets}</color>");
                }
            }
        }

        void OnDestroy()
        {
            StopStreaming();
        }

        void OnApplicationQuit()
        {
            StopStreaming();
        }

        // GUI for debugging
        void OnGUI()
        {
            if (!showDebugLogs) return;

            GUILayout.BeginArea(new Rect(10, 10, 250, 150));
            GUILayout.Label("=== UDP Streamer ===", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"Target: {targetIP}:{targetPort}");
            GUILayout.Label($"Streaming: {isStreaming}");
            GUILayout.Label($"Packets sent: {packetsSent}");
            GUILayout.Label($"FPS: {streamFPS:F1}");

            if (GUILayout.Button(isStreaming ? "Stop Streaming" : "Start Streaming"))
            {
                if (isStreaming)
                    StopStreaming();
                else
                    StartStreaming();
            }
            GUILayout.EndArea();
        }
    }
}
