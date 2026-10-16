using UnityEngine;
using Tracking4All;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

/// <summary>
/// Network troubleshooting helper for Android Host + Mac Client connection issues
/// Helps diagnose and resolve "Connection refused" errors
/// </summary>
public class NetworkTroubleshootingHelper : MonoBehaviour
{
    [Header("Network Configuration")]
    [SerializeField] private int targetPort = 52730;
    [SerializeField] private bool autoRunDiagnostics = true;
    [SerializeField] private bool showDetailedLogs = true;
    
    [Header("UI Display")]
    [SerializeField] private UnityEngine.UI.Text diagnosticsText;
    [SerializeField] private UnityEngine.UI.Button runDiagnosticsButton;
    
    private string deviceIP = "";
    
    private void Start()
    {
        if (runDiagnosticsButton != null)
        {
            runDiagnosticsButton.onClick.AddListener(RunFullDiagnostics);
        }
        
        if (autoRunDiagnostics)
        {
            Invoke(nameof(RunFullDiagnostics), 1f); // Wait a moment for UI to initialize
        }
    }
    
    [ContextMenu("Run Network Diagnostics")]
    public void RunFullDiagnostics()
    {
        Debug.Log("=== NETWORK TROUBLESHOOTING DIAGNOSTICS ===");
        
        // Step 1: Get device IP
        DetectDeviceIP();
        
        // Step 2: Check if Unity is actually listening
        CheckUnityNetworkListener();
        
        // Step 3: Test port binding
        TestPortBinding();
        
        // Step 4: Show network interface info
        ShowNetworkInterfaces();
        
        // Step 5: Provide specific solutions
        ProvideTroubleshootingSolutions();
    }
    
    private void DetectDeviceIP()
    {
        Debug.Log("--- Device IP Detection ---");
        
        try
        {
            // Method 1: Get local IP addresses
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork && 
                    !IPAddress.IsLoopback(address))
                {
                    deviceIP = address.ToString();
                    Debug.Log($"✓ Found device IP: {deviceIP}");
                    break;
                }
            }
            
            // Method 2: Alternative IP detection
            if (string.IsNullOrEmpty(deviceIP))
            {
                deviceIP = GetAlternativeIP();
                Debug.Log($"✓ Alternative IP detection: {deviceIP}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ IP detection failed: {e.Message}");
        }
        
        UpdateDiagnosticsDisplay($"Device IP: {deviceIP}");
    }
    
    private string GetAlternativeIP()
    {
        try
        {
            // Connect to a remote address to determine local IP
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "Unknown";
            }
        }
        catch
        {
            return "IP_Detection_Failed";
        }
    }
    
    private void CheckUnityNetworkListener()
    {
        Debug.Log("--- Unity Network Listener Status ---");
        
        // Check if NetworkedWebcamBackend is active and listening
        var networkBackend = FindObjectOfType<NetworkedWebcamBackend>();
        if (networkBackend == null)
        {
            Debug.LogError("✗ NetworkedWebcamBackend not found in scene!");
            Debug.LogError("SOLUTION: Add 'Default Python NetWebCam' prefab to scene");
            return;
        }
        
        Debug.Log($"✓ NetworkedWebcamBackend found: {networkBackend.name}");
        
        // Check if it's properly initialized
        bool isReady = networkBackend.Lifecycle?.IsReady(0) ?? false;
        Debug.Log($"Network backend ready: {isReady}");
        
        if (!isReady)
        {
            Debug.LogWarning("✗ NetworkedWebcamBackend not ready - may not be listening yet");
            Debug.LogWarning("SOLUTION: Wait for Unity to fully initialize, then try Python connection");
        }
    }
    
    private void TestPortBinding()
    {
        Debug.Log("--- Port Binding Test ---");
        
        try
        {
            // Try to bind to the port to see if it's available
            TcpListener testListener = new TcpListener(IPAddress.Any, targetPort);
            testListener.Start();
            testListener.Stop();
            
            Debug.Log($"✓ Port {targetPort} is available for binding");
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Debug.Log($"✓ Port {targetPort} is already in use (likely by Unity NetworkedWebcamBackend)");
            }
            else
            {
                Debug.LogError($"✗ Port binding error: {e.Message}");
            }
        }
    }
    
    private void ShowNetworkInterfaces()
    {
        Debug.Log("--- Network Interfaces ---");
        
        try
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            
            foreach (NetworkInterface ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up && 
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    Debug.Log($"Interface: {ni.Name} ({ni.NetworkInterfaceType})");
                    
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            Debug.Log($"  IP: {ip.Address}");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not enumerate network interfaces: {e.Message}");
        }
    }
    
    private void ProvideTroubleshootingSolutions()
    {
        Debug.Log("=== TROUBLESHOOTING SOLUTIONS ===");
        
        Debug.Log("Most Common Solutions for 'Connection refused':");
        Debug.Log("");
        
        Debug.Log("1. TIMING ISSUE:");
        Debug.Log("   • Unity host must be fully loaded before Python connects");
        Debug.Log("   • Wait 10-15 seconds after starting Unity app");
        Debug.Log("   • Look for 'NetworkedWebcamBackend ready' message");
        Debug.Log("");
        
        Debug.Log("2. WRONG IP ADDRESS:");
        Debug.Log($"   • Mac should connect to: {deviceIP}:{targetPort}");
        Debug.Log("   • Update Python global_vars.py:");
        Debug.Log($"     HOST = '{deviceIP}'");
        Debug.Log($"     PORT = {targetPort}");
        Debug.Log("");
        
        Debug.Log("3. NETWORK RESTRICTIONS:");
        Debug.Log("   • Both devices must be on same WiFi network");
        Debug.Log("   • Check router's AP isolation settings");
        Debug.Log("   • Try mobile hotspot if WiFi doesn't work");
        Debug.Log("");
        
        Debug.Log("4. FIREWALL/SECURITY:");
        Debug.Log("   • Android: Check if firewall apps are blocking");
        Debug.Log("   • Router: Ensure devices can communicate");
        Debug.Log("   • Try different port if 52730 is blocked");
        Debug.Log("");
        
        Debug.Log("5. UNITY SETUP:");
        Debug.Log("   • Ensure 'Default Python NetWebCam' prefab is in scene");
        Debug.Log("   • Check that NetworkManager is configured as Host");
        Debug.Log("   • Verify dual tracking setup is complete");
        Debug.Log("");
        
        // Provide specific command for Mac
        Debug.Log("=== MAC CLIENT COMMANDS ===");
        Debug.Log($"Test connection from Mac Terminal:");
        Debug.Log($"telnet {deviceIP} {targetPort}");
        Debug.Log("(If it connects, Unity is listening correctly)");
        Debug.Log("");
        Debug.Log("Python configuration file:");
        Debug.Log("nano networkedwebcam/global_vars.py");
        Debug.Log($"Set: HOST = '{deviceIP}'");
        Debug.Log($"Set: PORT = {targetPort}");
    }
    
    private void UpdateDiagnosticsDisplay(string info)
    {
        if (diagnosticsText != null)
        {
            diagnosticsText.text += info + "\\n";
        }
    }
    
    // Quick test methods
    [ContextMenu("Get Device IP")]
    public void ShowDeviceIP()
    {
        DetectDeviceIP();
        Debug.Log($"Device IP for Python client: {deviceIP}:{targetPort}");
    }
    
    [ContextMenu("Test Port Availability")]
    public void TestPort()
    {
        TestPortBinding();
    }
    
    public string GetDeviceIPForPython()
    {
        if (string.IsNullOrEmpty(deviceIP))
        {
            DetectDeviceIP();
        }
        return deviceIP;
    }
    
    public string GetPythonConfigString()
    {
        if (string.IsNullOrEmpty(deviceIP))
        {
            DetectDeviceIP();
        }
        
        return $"Python Config:\\n" +
               $"File: networkedwebcam/global_vars.py\\n" +
               $"HOST = '{deviceIP}'\\n" +
               $"PORT = {targetPort}\\n\\n" +
               $"Test connection: telnet {deviceIP} {targetPort}";
    }
}