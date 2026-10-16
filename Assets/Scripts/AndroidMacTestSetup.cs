using UnityEngine;
using Unity.Netcode;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Test setup configuration for Android Host + Mac Client dual avatar tracking
/// Android Host: Bryce (local camera) + Remy (Mac networked camera)
/// Mac Client: Runs Python networked webcam, sends camera data to Android
/// </summary>
public class AndroidMacTestSetup : MonoBehaviour
{
    [Header("Network Configuration")]
    [SerializeField] private int networkPort = 52730;
    [SerializeField] private bool autoDetectAndroidIP = true;
    [SerializeField] private string manualAndroidIP = "192.168.1.100";
    
    [Header("Test Settings")]
    [SerializeField] private bool showDetailedLogs = true;
    [SerializeField] private bool runNetworkTests = true;
    
    [Header("UI Display")]
    [SerializeField] private UnityEngine.UI.Text statusText;
    [SerializeField] private UnityEngine.UI.Button testConnectionButton;
    
    private string detectedAndroidIP = "";
    private bool isAndroidDevice = false;
    
    private void Start()
    {
        DetectPlatform();
        DetectNetworkConfiguration();
        SetupUI();
        ShowSetupInstructions();
    }
    
    private void DetectPlatform()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        isAndroidDevice = true;
        #else
        isAndroidDevice = false;
        #endif
        
        if (showDetailedLogs)
        {
            string platform = isAndroidDevice ? "ANDROID DEVICE" : "MAC/PC";
            Debug.Log($"Platform detected: {platform}");
        }
    }
    
    private void DetectNetworkConfiguration()
    {
        if (autoDetectAndroidIP)
        {
            detectedAndroidIP = GetLocalIPAddress();
            
            if (showDetailedLogs)
                Debug.Log($"Detected local IP: {detectedAndroidIP}");
        }
    }
    
    private void SetupUI()
    {
        if (statusText != null)
        {
            UpdateStatusDisplay();
        }
        
        if (testConnectionButton != null)
        {
            testConnectionButton.onClick.AddListener(TestNetworkConnection);
        }
    }
    
    private void ShowSetupInstructions()
    {
        if (isAndroidDevice)
        {
            ShowAndroidHostInstructions();
        }
        else
        {
            ShowMacClientInstructions();
        }
    }
    
    private void ShowAndroidHostInstructions()
    {
        Debug.Log("=== ANDROID HOST SETUP ===");
        Debug.Log("This Android device will be the HOST:");
        Debug.Log("• Bryce avatar = controlled by this device's camera");
        Debug.Log("• Remy avatar = controlled by Mac's camera (via network)");
        Debug.Log("");
        
        Debug.Log("Steps:");
        Debug.Log("1. Ensure Android and Mac are on same WiFi network");
        Debug.Log($"2. Note this device's IP address: {detectedAndroidIP}");
        Debug.Log("3. Start Unity multiplayer as HOST");
        Debug.Log("4. Wait for Mac client to connect with Python webcam");
        Debug.Log("");
        
        Debug.Log("Expected behavior:");
        Debug.Log("• Bryce moves with your Android camera");
        Debug.Log("• Remy moves when Mac connects and sends camera data");
        Debug.Log("• Both avatars visible simultaneously on Android screen");
        Debug.Log("");
        
        if (showDetailedLogs)
        {
            Debug.Log("Network Details:");
            Debug.Log($"• Android IP: {detectedAndroidIP}");
            Debug.Log($"• Network Port: {networkPort}");
            Debug.Log($"• Expected connection from Mac client");
        }
    }
    
    private void ShowMacClientInstructions()
    {
        Debug.Log("=== MAC CLIENT SETUP ===");
        Debug.Log("This Mac will be the CLIENT:");
        Debug.Log("• Sends camera data to Android host");
        Debug.Log("• Android will use your camera data to control Remy avatar");
        Debug.Log("");
        
        string androidIP = !string.IsNullOrEmpty(detectedAndroidIP) ? detectedAndroidIP : manualAndroidIP;
        
        Debug.Log("Setup Steps:");
        Debug.Log("1. Configure Python script with Android IP:");
        Debug.Log($"   Edit: Assets/Tracking4All/Third Party/MediaPipePython/PythonSystem/networkedwebcam/global_vars.py");
        Debug.Log($"   Set: HOST = '{androidIP}'");
        Debug.Log($"   Set: PORT = {networkPort}");
        Debug.Log("");
        
        Debug.Log("2. Start Python networked webcam:");
        Debug.Log("   cd Assets/Tracking4All/Third Party/MediaPipePython/PythonSystem/");
        Debug.Log("   python main.py");
        Debug.Log("   Select: networkedwebcam");
        Debug.Log("");
        
        Debug.Log("3. Verify connection:");
        Debug.Log("   Python should show 'Connected' or similar message");
        Debug.Log("   Android should receive camera frames");
        Debug.Log("");
        
        Debug.Log("Alternative Unity Client Setup:");
        Debug.Log("• You can also run Unity on Mac as client");
        Debug.Log("• Join the Android host's multiplayer session");
        Debug.Log("• Remy will track your local pose on Mac");
        Debug.Log("");
    }
    
    public void TestNetworkConnection()
    {
        if (isAndroidDevice)
        {
            TestAndroidHostConnectivity();
        }
        else
        {
            TestMacClientConnectivity();
        }
    }
    
    private void TestAndroidHostConnectivity()
    {
        Debug.Log("=== ANDROID HOST NETWORK TEST ===");
        
        // Test if we can bind to the network port
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Any, networkPort);
            listener.Start();
            listener.Stop();
            
            Debug.Log($"✓ SUCCESS: Can bind to port {networkPort}");
            Debug.Log($"✓ Android IP: {detectedAndroidIP}");
            Debug.Log("✓ Ready to receive camera data from Mac client");
            
            UpdateStatusDisplay("Ready for Mac connection");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ FAILED: Cannot bind to port {networkPort}");
            Debug.LogError($"Error: {e.Message}");
            Debug.LogError("Check if port is already in use or blocked");
            
            UpdateStatusDisplay("Network test failed");
        }
    }
    
    private void TestMacClientConnectivity()
    {
        Debug.Log("=== MAC CLIENT NETWORK TEST ===");
        
        string targetIP = !string.IsNullOrEmpty(detectedAndroidIP) ? detectedAndroidIP : manualAndroidIP;
        
        // Test if we can reach the Android host
        try
        {
            TcpClient client = new TcpClient();
            var result = client.BeginConnect(targetIP, networkPort, null, null);
            bool connected = result.AsyncWaitHandle.WaitOne(3000); // 3 second timeout
            
            if (connected && client.Connected)
            {
                Debug.Log($"✓ SUCCESS: Can connect to Android host at {targetIP}:{networkPort}");
                Debug.Log("✓ Network path is clear for Python webcam");
                client.Close();
                
                UpdateStatusDisplay("Can reach Android host");
            }
            else
            {
                Debug.LogWarning($"✗ TIMEOUT: Cannot connect to Android host at {targetIP}:{networkPort}");
                Debug.LogWarning("Possible issues:");
                Debug.LogWarning("• Android host not running");
                Debug.LogWarning("• Wrong IP address");
                Debug.LogWarning("• Firewall blocking connection");
                Debug.LogWarning("• Devices on different networks");
                
                UpdateStatusDisplay("Cannot reach Android host");
                client.Close();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ FAILED: Network connection test failed");
            Debug.LogError($"Error: {e.Message}");
            
            UpdateStatusDisplay("Network connection failed");
        }
    }
    
    private void UpdateStatusDisplay(string status = null)
    {
        if (statusText == null) return;
        
        string displayText = "";
        
        if (isAndroidDevice)
        {
            displayText = $"ANDROID HOST\\n";
            displayText += $"IP: {detectedAndroidIP}\\n";
            displayText += $"Port: {networkPort}\\n";
            displayText += $"Status: {status ?? "Waiting for setup"}";
        }
        else
        {
            string targetIP = !string.IsNullOrEmpty(detectedAndroidIP) ? detectedAndroidIP : manualAndroidIP;
            displayText = $"MAC CLIENT\\n";
            displayText += $"Target: {targetIP}:{networkPort}\\n";
            displayText += $"Status: {status ?? "Ready to connect"}";
        }
        
        statusText.text = displayText;
    }
    
    private string GetLocalIPAddress()
    {
        try
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork && 
                    !IPAddress.IsLoopback(address))
                {
                    return address.ToString();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not detect IP address: {e.Message}");
        }
        
        return "IP_NOT_DETECTED";
    }
    
    // Public methods for external control
    public string GetAndroidIP()
    {
        return !string.IsNullOrEmpty(detectedAndroidIP) ? detectedAndroidIP : manualAndroidIP;
    }
    
    public int GetNetworkPort()
    {
        return networkPort;
    }
    
    public bool IsAndroidHost()
    {
        return isAndroidDevice;
    }
    
    public string GetPythonConfigInstructions()
    {
        string androidIP = GetAndroidIP();
        
        return $"Python Configuration:\\n" +
               $"File: networkedwebcam/global_vars.py\\n" +
               $"HOST = '{androidIP}'\\n" +
               $"PORT = {networkPort}\\n\\n" +
               $"Command: python main.py\\n" +
               $"Select: networkedwebcam";
    }
    
    [ContextMenu("Show Setup Instructions")]
    public void ShowInstructions()
    {
        ShowSetupInstructions();
    }
    
    [ContextMenu("Test Network")]
    public void RunNetworkTest()
    {
        TestNetworkConnection();
    }
}