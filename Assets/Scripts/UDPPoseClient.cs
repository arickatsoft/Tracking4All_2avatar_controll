using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Tracking4All
{
    /// <summary>
    /// UDP Client for sending pose data to Unity receiver.
    /// Similar to Python ClientUDP implementation.
    /// </summary>
    public class UDPPoseClient : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private string targetIP = "127.0.0.1";
        [SerializeField] private int targetPort = 52733;
        
        [Header("Options")]
        [SerializeField] private bool autoReconnect = true;
        [SerializeField] private int reconnectDelayMs = 1000;
        
        // Public properties for configuration
        public string TargetIP { get => targetIP; set => targetIP = value; }
        public int TargetPort { get => targetPort; set => targetPort = value; }
        
        private UdpClient udpClient;
        private IPEndPoint endPoint;
        private bool isConnected = false;
        private Thread sendThread;
        private bool shouldRun = true;
        
        // Message queue
        private System.Collections.Concurrent.ConcurrentQueue<string> messageQueue;
        
        // Events
        public event Action OnConnected;
        public event Action OnDisconnected;
        
        private void Awake()
        {
            messageQueue = new System.Collections.Concurrent.ConcurrentQueue<string>();
        }
        
        private void Start()
        {
            Connect();
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
        
        public void Connect()
        {
            if (isConnected)
            {
                UnityEngine.Debug.LogWarning("Already connected");
                return;
            }
            
            try
            {
                udpClient = new UdpClient();
                endPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
                udpClient.Connect(endPoint);
                
                isConnected = true;
                UnityEngine.Debug.Log($"UDP Client connected to {targetIP}:{targetPort}");
                OnConnected?.Invoke();
                
                // Start send thread
                shouldRun = true;
                sendThread = new Thread(ProcessMessageQueue);
                sendThread.IsBackground = true;
                sendThread.Start();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Connection failed: {e.Message}");
                if (autoReconnect)
                {
                    Invoke(nameof(Connect), reconnectDelayMs / 1000f);
                }
            }
        }
        
        public void Disconnect()
        {
            shouldRun = false;
            isConnected = false;
            
            try
            {
                udpClient?.Close();
                OnDisconnected?.Invoke();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error disconnecting: {e.Message}");
            }
            
            if (sendThread != null && sendThread.IsAlive)
            {
                sendThread.Join(500);
            }
        }
        
        public void SendMessage(string message)
        {
            if (!isConnected)
            {
                UnityEngine.Debug.LogWarning("Not connected. Message not sent.");
                return;
            }
            
            messageQueue.Enqueue(message);
        }
        
        private void ProcessMessageQueue()
        {
            while (shouldRun)
            {
                try
                {
                    if (messageQueue.TryDequeue(out string message))
                    {
                        string fullMessage = message + "<EOM>";
                        byte[] data = Encoding.UTF8.GetBytes(fullMessage);
                        
                        udpClient.Send(data, data.Length);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                catch (SocketException e)
                {
                    UnityEngine.Debug.LogError($"Socket error: {e.Message}");
                    isConnected = false;
                    OnDisconnected?.Invoke();
                    
                    if (autoReconnect)
                    {
                        Thread.Sleep(reconnectDelayMs);
                        Connect();
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Error sending message: {e.Message}");
                }
            }
        }
        
        public bool IsConnected => isConnected;
    }
}
