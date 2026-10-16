// NetworkDiagnostics.cs
// Diagnoses UDP socket issues in Unity
// (C) 2024-2025 G8gaming Ltd.

using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Tracking4All
{
    /// <summary>
    /// Diagnostic tool to test raw UDP socket functionality
    /// Helps identify if Unity's UDP implementation has issues
    /// </summary>
    public class NetworkDiagnostics : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("Port to test (should match your UDPPoseServer port)")]
        public int testPort = 52733;

        [Header("Status")]
        [SerializeField] private bool isTestRunning = false;
        [SerializeField] private int packetsReceived = 0;
        [SerializeField] private string lastReceivedMessage = "";
        [SerializeField] private string lastError = "";

        private UdpClient testClient;
        private Thread receiveThread;
        private bool shouldRun = false;

        private void Start()
        {
            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log("[NetworkDiagnostics] Ready to test UDP");
            UnityEngine.Debug.Log("Press 'D' key to start/stop diagnostic test");
            UnityEngine.Debug.Log("========================================");
        }

        private void Update()
        {
            // Press 'D' to toggle diagnostics
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (isTestRunning)
                {
                    StopDiagnosticTest();
                }
                else
                {
                    StartDiagnosticTest();
                }
            }
        }

        public void StartDiagnosticTest()
        {
            if (isTestRunning)
            {
                UnityEngine.Debug.LogWarning("[NetworkDiagnostics] Test already running!");
                return;
            }

            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log($"[NetworkDiagnostics] 🔍 Starting RAW UDP test on port {testPort}");
            UnityEngine.Debug.Log($"[NetworkDiagnostics] This bypasses UDPPoseServer to test socket functionality");
            UnityEngine.Debug.Log("========================================");

            try
            {
                // Create UDP client with same settings as UDPPoseServer
                testClient = new UdpClient(testPort);
                UnityEngine.Debug.Log($"<color=green>[NetworkDiagnostics] ✅ UDP socket bound to port {testPort}</color>");

                // Start receive thread
                shouldRun = true;
                receiveThread = new Thread(DiagnosticReceiveThread);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                isTestRunning = true;
                packetsReceived = 0;

                UnityEngine.Debug.Log($"[NetworkDiagnostics] 📡 Listening for UDP packets...");
                UnityEngine.Debug.Log($"[NetworkDiagnostics] Send a PING from the other device to test");
            }
            catch (SocketException e)
            {
                lastError = e.Message;
                UnityEngine.Debug.LogError($"[NetworkDiagnostics] ❌ Socket binding FAILED: {e.Message}");
                UnityEngine.Debug.LogError($"[NetworkDiagnostics] Error code: {e.ErrorCode}");

                if (e.ErrorCode == 10048 || e.Message.Contains("already in use"))
                {
                    UnityEngine.Debug.LogError($"[NetworkDiagnostics] 🔥 PORT {testPort} IS ALREADY IN USE!");
                    UnityEngine.Debug.LogError($"[NetworkDiagnostics] Another socket is bound to this port (possibly UDPPoseServer)");
                    UnityEngine.Debug.LogError($"[NetworkDiagnostics] Solution: Stop UDPPoseServer first, then run diagnostics");
                }
            }
            catch (System.Exception e)
            {
                lastError = e.Message;
                UnityEngine.Debug.LogError($"[NetworkDiagnostics] ❌ Unexpected error: {e.Message}");
            }
        }

        public void StopDiagnosticTest()
        {
            if (!isTestRunning)
            {
                UnityEngine.Debug.LogWarning("[NetworkDiagnostics] No test running!");
                return;
            }

            UnityEngine.Debug.Log("[NetworkDiagnostics] 🛑 Stopping diagnostic test...");

            shouldRun = false;

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(1000);
            }

            if (testClient != null)
            {
                testClient.Close();
                testClient = null;
            }

            isTestRunning = false;

            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log($"[NetworkDiagnostics] ✅ Test complete. Packets received: {packetsReceived}");
            UnityEngine.Debug.Log("========================================");
        }

        private void DiagnosticReceiveThread()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, testPort);

            UnityEngine.Debug.Log($"[NetworkDiagnostics Thread] 🧵 Receive thread started");

            while (shouldRun)
            {
                try
                {
                    // Blocking receive (will wait for data)
                    byte[] data = testClient.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data);

                    packetsReceived++;
                    lastReceivedMessage = message;

                    UnityEngine.Debug.Log("========================================");
                    UnityEngine.Debug.Log($"<color=cyan>[NetworkDiagnostics] 📦 PACKET RECEIVED #{packetsReceived}</color>");
                    UnityEngine.Debug.Log($"<color=cyan>[NetworkDiagnostics] From: {remoteEndPoint.Address}:{remoteEndPoint.Port}</color>");
                    UnityEngine.Debug.Log($"<color=cyan>[NetworkDiagnostics] Message: '{message}'</color>");
                    UnityEngine.Debug.Log($"<color=cyan>[NetworkDiagnostics] Size: {data.Length} bytes</color>");
                    UnityEngine.Debug.Log("========================================");

                    // If it's a PING, send PONG
                    if (message == "PING")
                    {
                        byte[] pongData = Encoding.UTF8.GetBytes("PONG");
                        testClient.Send(pongData, pongData.Length, remoteEndPoint);
                        UnityEngine.Debug.Log($"<color=green>[NetworkDiagnostics] ✅ Sent PONG back to {remoteEndPoint}</color>");
                    }
                }
                catch (SocketException e)
                {
                    if (shouldRun) // Only log if we're still supposed to be running
                    {
                        lastError = e.Message;
                        UnityEngine.Debug.LogError($"[NetworkDiagnostics Thread] Socket error: {e.Message}");
                    }
                    break;
                }
                catch (System.Exception e)
                {
                    lastError = e.Message;
                    UnityEngine.Debug.LogError($"[NetworkDiagnostics Thread] Error: {e.Message}");
                }
            }

            UnityEngine.Debug.Log($"[NetworkDiagnostics Thread] 🧵 Receive thread exiting");
        }

        private void OnDestroy()
        {
            if (isTestRunning)
            {
                StopDiagnosticTest();
            }
        }

        // OnGUI disabled - UI hidden for production builds
        /*
        private void OnGUI()
        {
            // Diagnostic panel in bottom-left
            GUILayout.BeginArea(new Rect(10, Screen.height - 180, 400, 170));

            GUILayout.Label("Network Diagnostics", new GUIStyle(GUI.skin.label)
                { fontSize = 16, fontStyle = FontStyle.Bold });

            GUILayout.Space(5);
            GUILayout.Label($"Test Port: {testPort}");

            if (isTestRunning)
            {
                GUI.backgroundColor = Color.green;
                GUILayout.Label($"Status: LISTENING", new GUIStyle(GUI.skin.label)
                    { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Color.green } });
                GUI.backgroundColor = Color.white;

                GUILayout.Label($"Packets Received: {packetsReceived}");
                GUILayout.Label($"Last Message: {lastReceivedMessage}");

                if (GUILayout.Button("🛑 Stop Diagnostic Test", GUILayout.Height(35)))
                {
                    StopDiagnosticTest();
                }
            }
            else
            {
                GUI.backgroundColor = Color.yellow;
                GUILayout.Label($"Status: IDLE", new GUIStyle(GUI.skin.label)
                    { fontSize = 14, fontStyle = FontStyle.Bold });
                GUI.backgroundColor = Color.white;

                if (!string.IsNullOrEmpty(lastError))
                {
                    GUILayout.Label($"Last Error: {lastError}", new GUIStyle(GUI.skin.label)
                        { normal = { textColor = Color.red } });
                }

                if (GUILayout.Button("🔍 Start Diagnostic Test", GUILayout.Height(35)))
                {
                    StartDiagnosticTest();
                }
            }

            GUILayout.Space(5);
            GUILayout.Label("Press 'D' key to toggle test", new GUIStyle(GUI.skin.label)
                { fontSize = 10, fontStyle = FontStyle.Italic });

            GUILayout.EndArea();
        }
        */
    }
}
