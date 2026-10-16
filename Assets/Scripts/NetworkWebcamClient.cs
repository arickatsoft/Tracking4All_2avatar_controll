using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Tracking4All
{
    /// <summary>
    /// Network webcam client that connects to an IP-based webcam stream
    /// and forwards frames for processing. Similar to Python networkedwebcam solution.
    /// </summary>
    public class NetworkWebcamClient : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private string webcamIP = "192.168.1.100";
        [SerializeField] private int webcamPort = 8080;
        [SerializeField] private string streamPath = "/video";
        
        [Header("Connection")]
        [SerializeField] private bool autoConnect = true;
        [SerializeField] private float reconnectDelay = 2f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Public properties for configuration
        public string WebcamIP { get => webcamIP; set => webcamIP = value; }
        public int WebcamPort { get => webcamPort; set => webcamPort = value; }
        public string StreamPath { get => streamPath; set => streamPath = value; }
        public bool AutoConnect { get => autoConnect; set => autoConnect = value; }
        
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private Thread receiveThread;
        private bool isConnected = false;
        private bool shouldRun = true;
        
        // Frame data
        private byte[] latestFrameData;
        private bool hasNewFrame = false;
        private readonly object frameLock = new object();
        
        // Events
        public event Action<byte[]> OnFrameReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;
        
        private void Start()
        {
            if (autoConnect)
            {
                Connect();
            }
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
        
        public void Connect()
        {
            if (isConnected)
            {
                UnityEngine.Debug.LogWarning("Already connected to webcam");
                return;
            }
            
            shouldRun = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        
        public void Disconnect()
        {
            shouldRun = false;
            isConnected = false;
            
            try
            {
                networkStream?.Close();
                tcpClient?.Close();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error disconnecting: {e.Message}");
            }
            
            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(1000);
            }
        }
        
        private void ReceiveData()
        {
            while (shouldRun)
            {
                try
                {
                    // Connect to webcam
                    tcpClient = new TcpClient();
                    tcpClient.Connect(webcamIP, webcamPort);
                    networkStream = tcpClient.GetStream();
                    
                    // Send HTTP request for stream
                    string request = $"GET {streamPath} HTTP/1.1\r\n" +
                                   $"Host: {webcamIP}:{webcamPort}\r\n" +
                                   "Connection: keep-alive\r\n\r\n";
                    byte[] requestBytes = Encoding.ASCII.GetBytes(request);
                    networkStream.Write(requestBytes, 0, requestBytes.Length);
                    
                    isConnected = true;
                    UnityEngine.Debug.Log($"Connected to webcam at {webcamIP}:{webcamPort}");
                    OnConnected?.Invoke();
                    
                    // Read stream data
                    while (shouldRun && isConnected)
                    {
                        byte[] frameData = ReadFrame();
                        
                        if (frameData != null && frameData.Length > 0)
                        {
                            lock (frameLock)
                            {
                                latestFrameData = frameData;
                                hasNewFrame = true;
                            }
                            
                            OnFrameReceived?.Invoke(frameData);
                        }
                    }
                }
                catch (SocketException e)
                {
                    UnityEngine.Debug.LogError($"Socket error: {e.Message}");
                    isConnected = false;
                    OnDisconnected?.Invoke();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Connection error: {e.Message}");
                    isConnected = false;
                    OnDisconnected?.Invoke();
                }
                
                if (shouldRun)
                {
                    Thread.Sleep((int)(reconnectDelay * 1000));
                }
            }
        }
        
        private byte[] ReadFrame()
        {
            try
            {
                string headers = "";
                int c;
                while ((c = networkStream.ReadByte()) != -1)
                {
                    headers += (char)c;
                    if (headers.EndsWith("\r\n\r\n"))
                        break;
                }
                
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
                    byte[] frameData = new byte[contentLength];
                    int bytesRead = 0;
                    while (bytesRead < contentLength)
                    {
                        int read = networkStream.Read(frameData, bytesRead, contentLength - bytesRead);
                        if (read <= 0) break;
                        bytesRead += read;
                    }
                    return frameData;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error reading frame: {e.Message}");
            }
            
            return null;
        }
        
        public byte[] GetLatestFrame()
        {
            lock (frameLock)
            {
                if (hasNewFrame)
                {
                    hasNewFrame = false;
                    return latestFrameData;
                }
            }
            return null;
        }
        
        public bool IsConnected => isConnected;
    }
}
