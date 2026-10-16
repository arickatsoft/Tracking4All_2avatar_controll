using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking4All
{
    /// <summary>
    /// Bridges IP Webcam to NetworkedWebcamBackend without Python.
    /// Captures frames from IP webcam and sends them via TCP to NetworkedWebcamBackend
    /// in the same format Python networkedwebcam uses.
    /// </summary>
    public class IPWebcamToTCPBridge : MonoBehaviour
    {
        [Header("IP Webcam Source")]
        [Tooltip("IP address of your phone/IP webcam")]
        public string webcamIP = "192.168.1.100";
        public int webcamPort = 8080;
        public string streamPath = "/video";
        
        [Header("TCP Target (NetworkedWebcamBackend)")]
        [Tooltip("Leave as 127.0.0.1 for same device")]
        public string tcpServerIP = "127.0.0.1";
        [Tooltip("Must match NetworkedWebcamBackend port")]
        public int tcpServerPort = 52730;
        
        [Header("Options")]
        public bool autoStart = true;
        public bool showDebugLogs = true;
        
        [Header("Status")]
        [SerializeField] private bool isRunning = false;
        [SerializeField] private int framesSent = 0;
        [SerializeField] private float fps = 0f;
        
        // Networking
        private Thread webcamThread;
        private TcpClient tcpClient;
        private NetworkStream tcpStream;
        private bool shouldRun = false;
        
        // Frame tracking
        private int lastFrameCount = 0;
        private float lastFPSCheck = 0f;
        
        void Start()
        {
            if (autoStart)
            {
                StartBridge();
            }
        }
        
        public void StartBridge()
        {
            if (isRunning)
            {
                Log("Bridge already running", true);
                return;
            }
            
            shouldRun = true;
            webcamThread = new Thread(BridgeLoop);
            webcamThread.IsBackground = true;
            webcamThread.Start();
            
            isRunning = true;
            Log($"Started bridge: {webcamIP}:{webcamPort} → {tcpServerIP}:{tcpServerPort}");
        }
        
        public void StopBridge()
        {
            shouldRun = false;
            isRunning = false;
            
            try
            {
                tcpStream?.Close();
                tcpClient?.Close();
            }
            catch (Exception e)
            {
                Log($"Error stopping: {e.Message}", true);
            }
            
            if (webcamThread != null && webcamThread.IsAlive)
            {
                webcamThread.Join(1000);
            }
            
            Log("Bridge stopped");
        }
        
        private void BridgeLoop()
        {
            TcpClient webcamConnection = null;
            NetworkStream webcamStream = null;
            
            try
            {
                // Connect to TCP server (NetworkedWebcamBackend)
                tcpClient = new TcpClient();
                tcpClient.Connect(tcpServerIP, tcpServerPort);
                tcpStream = tcpClient.GetStream();
                Log("Connected to TCP server");
                
                // Send initial camera settings (message type 117)
                SendCameraSettings(0, 640, 480, 30); // Default settings
                
                // Connect to IP webcam
                webcamConnection = new TcpClient();
                webcamConnection.Connect(webcamIP, webcamPort);
                webcamStream = webcamConnection.GetStream();
                
                // Send HTTP request
                string request = $"GET {streamPath} HTTP/1.1\r\n" +
                               $"Host: {webcamIP}:{webcamPort}\r\n" +
                               "Connection: keep-alive\r\n\r\n";
                byte[] requestBytes = System.Text.Encoding.ASCII.GetBytes(request);
                webcamStream.Write(requestBytes, 0, requestBytes.Length);
                
                Log("Connected to webcam, streaming...");
                
                // Stream frames
                while (shouldRun)
                {
                    byte[] frameData = ReadFrameFromWebcam(webcamStream);
                    
                    if (frameData != null && frameData.Length > 0)
                    {
                        // Send frame data (message type 118)
                        SendFrameData(frameData);
                        framesSent++;
                    }
                }
            }
            catch (Exception e)
            {
                Log($"Bridge error: {e.Message}", true);
            }
            finally
            {
                webcamStream?.Close();
                webcamConnection?.Close();
                tcpStream?.Close();
                tcpClient?.Close();
            }
        }
        
        private byte[] ReadFrameFromWebcam(NetworkStream stream)
        {
            try
            {
                // Read HTTP headers
                string headers = "";
                int c;
                while ((c = stream.ReadByte()) != -1)
                {
                    headers += (char)c;
                    if (headers.EndsWith("\r\n\r\n"))
                        break;
                }
                
                // Parse content length
                int contentLength = 0;
                string[] lines = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        string lengthStr = line.Substring(15).Trim();
                        int.TryParse(lengthStr, out contentLength);
                        break;
                    }
                }
                
                if (contentLength > 0)
                {
                    // Read frame data
                    byte[] frameData = new byte[contentLength];
                    int bytesRead = 0;
                    while (bytesRead < contentLength)
                    {
                        int read = stream.Read(frameData, bytesRead, contentLength - bytesRead);
                        if (read <= 0) break;
                        bytesRead += read;
                    }
                    
                    return frameData;
                }
            }
            catch (Exception e)
            {
                Log($"Error reading frame: {e.Message}", true);
            }
            
            return null;
        }
        
        private void SendCameraSettings(int index, int width, int height, float fps)
        {
            try
            {
                // Message format: [length][type][data]
                // Type 117 = Camera initialization
                byte[] data = new byte[16];
                
                // Convert to network byte order (big-endian)
                byte[] indexBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(index));
                byte[] widthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(width));
                byte[] heightBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(height));
                
                // FPS as float (need to reverse for network order)
                byte[] fpsBytes = BitConverter.GetBytes(fps);
                Array.Reverse(fpsBytes);
                
                Array.Copy(indexBytes, 0, data, 0, 4);
                Array.Copy(widthBytes, 0, data, 4, 4);
                Array.Copy(heightBytes, 0, data, 8, 4);
                Array.Copy(fpsBytes, 0, data, 12, 4);
                
                SendTCPMessage(117, data);
                Log($"Sent camera settings: {width}x{height} @ {fps}fps");
            }
            catch (Exception e)
            {
                Log($"Error sending camera settings: {e.Message}", true);
            }
        }
        
        private void SendFrameData(byte[] frameData)
        {
            try
            {
                // Type 118 = Frame data
                SendTCPMessage(118, frameData);
            }
            catch (Exception e)
            {
                Log($"Error sending frame: {e.Message}", true);
            }
        }
        
        private void SendTCPMessage(uint messageType, byte[] data)
        {
            if (tcpStream == null || !tcpStream.CanWrite)
                return;
            
            try
            {
                // Format: [length (4 bytes)][type (4 bytes)][data]
                int dataLength = data.Length;
                byte[] lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(dataLength));
                byte[] typeBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)messageType));
                
                // Send header
                tcpStream.Write(lengthBytes, 0, 4);
                tcpStream.Write(typeBytes, 0, 4);
                
                // Send data
                tcpStream.Write(data, 0, data.Length);
                tcpStream.Flush();
            }
            catch (Exception e)
            {
                Log($"TCP send error: {e.Message}", true);
                shouldRun = false;
            }
        }
        
        private void Log(string message, bool isWarning = false)
        {
            if (!showDebugLogs) return;
            
            string prefix = "[IPWebcamToTCPBridge] ";
            if (isWarning)
                UnityEngine.Debug.LogWarning(prefix + message);
            else
                UnityEngine.Debug.Log(prefix + message);
        }
        
        void Update()
        {
            // Update FPS counter
            if (Time.time - lastFPSCheck >= 1f)
            {
                fps = (framesSent - lastFrameCount) / (Time.time - lastFPSCheck);
                lastFrameCount = framesSent;
                lastFPSCheck = Time.time;
            }
        }
        
        void OnDestroy()
        {
            StopBridge();
        }
    }
}
