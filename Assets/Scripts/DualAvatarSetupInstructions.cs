using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Runtime instructions and helper for setting up dual avatar tracking
/// Provides step-by-step guidance for both host and client setup
/// </summary>
public class DualAvatarSetupInstructions : MonoBehaviour
{
    [Header("Setup Instructions Display")]
    [SerializeField] private bool showInstructionsOnStart = true;
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private bool showInUI = false;
    
    [Header("Python System Paths")]
    [TextArea(2, 3)]
    [SerializeField] private string pythonSystemPath = "Assets/Tracking4All/Third Party/MediaPipePython/PythonSystem/";
    [SerializeField] private string hostIP = "192.168.1.100"; // Update with actual host IP
    [SerializeField] private int networkPort = 52730;
    
    private void Start()
    {
        if (showInstructionsOnStart)
        {
            ShowSetupInstructions();
        }
    }
    
    public void ShowSetupInstructions()
    {
        if (logToConsole)
        {
            LogHostInstructions();
            LogClientInstructions();
            LogPythonSystemSetup();
            LogTroubleshootingTips();
        }
    }
    
    private void LogHostInstructions()
    {
        Debug.Log("=== HOST DEVICE SETUP ===");
        Debug.Log("1. This device will control Bryce with its local camera");
        Debug.Log("2. Remy will be controlled by the remote client's camera data");
        Debug.Log("3. Start the Unity multiplayer session as HOST");
        Debug.Log("4. Wait for client to connect and start sending camera data");
        Debug.Log("5. Both avatars should be tracking simultaneously");
        Debug.Log("");
        
        Debug.Log("HOST - Expected Behavior:");
        Debug.Log("• Bryce avatar moves with your local camera/pose");
        Debug.Log("• Remy avatar moves when client connects and sends camera data");
        Debug.Log("• You can see both players in the same scene");
        Debug.Log("");
    }
    
    private void LogClientInstructions()
    {
        Debug.Log("=== CLIENT DEVICE SETUP ===");
        Debug.Log("1. This device will control Remy with its local camera");
        Debug.Log("2. Camera data will be sent to host for Bryce avatar");
        Debug.Log("3. Update Python script with correct host IP");
        Debug.Log("4. Start Python networked webcam BEFORE joining Unity session");
        Debug.Log("5. Join the Unity multiplayer session");
        Debug.Log("");
        
        Debug.Log("CLIENT - Steps to Follow:");
        Debug.Log("Step 1: Configure Host IP");
        Debug.Log($"  - Edit: {pythonSystemPath}networkedwebcam/global_vars.py");
        Debug.Log($"  - Set HOST = '{hostIP}' (replace with actual host IP)");
        Debug.Log($"  - Set PORT = {networkPort}");
        Debug.Log("");
        
        Debug.Log("Step 2: Start Python System");
        Debug.Log($"  - Open terminal/command prompt");
        Debug.Log($"  - Navigate to: {pythonSystemPath}");
        Debug.Log("  - Run: python main.py");
        Debug.Log("  - Select: networkedwebcam (option number)");
        Debug.Log("  - Camera should start and connect to host");
        Debug.Log("");
        
        Debug.Log("Step 3: Join Unity Session");
        Debug.Log("  - Start Unity on client device");
        Debug.Log("  - Click 'Join Game' button");
        Debug.Log("  - Remy should start tracking your local pose");
        Debug.Log("");
    }
    
    private void LogPythonSystemSetup()
    {
        Debug.Log("=== PYTHON SYSTEM CONFIGURATION ===");
        Debug.Log("");
        
        Debug.Log("Required Python Dependencies:");
        Debug.Log("• opencv-python (cv2)");
        Debug.Log("• mediapipe");
        Debug.Log("• numpy");
        Debug.Log("Install with: pip install opencv-python mediapipe numpy");
        Debug.Log("");
        
        Debug.Log("Network Configuration Files:");
        Debug.Log($"1. {pythonSystemPath}networkedwebcam/global_vars.py");
        Debug.Log("   - HOST: IP address of the host Unity device");
        Debug.Log("   - PORT: Network port (default 52730)");
        Debug.Log("   - KILL_THREADS: Threading control variable");
        Debug.Log("");
        
        Debug.Log($"2. {pythonSystemPath}common/clientTCP.py");
        Debug.Log("   - TCP client implementation");
        Debug.Log("   - Handles connection and data transmission");
        Debug.Log("");
        
        Debug.Log($"3. {pythonSystemPath}networkedwebcam/networkwebcam.py");
        Debug.Log("   - Main webcam capture and network sending");
        Debug.Log("   - Converts camera frames to PNG and sends via TCP");
        Debug.Log("");
    }
    
    private void LogTroubleshootingTips()
    {
        Debug.Log("=== TROUBLESHOOTING TIPS ===");
        Debug.Log("");
        
        Debug.Log("Connection Issues:");
        Debug.Log("• Ensure both devices are on the same network");
        Debug.Log("• Check firewall settings (allow port 52730)");
        Debug.Log("• Verify host IP address is correct");
        Debug.Log("• Try pinging host from client device");
        Debug.Log("");
        
        Debug.Log("Python Script Issues:");
        Debug.Log("• Check camera permissions on client device");
        Debug.Log("• Ensure all Python dependencies are installed");
        Debug.Log("• Try running python script with administrator privileges");
        Debug.Log("• Check for antivirus blocking Python network access");
        Debug.Log("");
        
        Debug.Log("Unity Tracking Issues:");
        Debug.Log("• Verify both tracking solutions are properly configured");
        Debug.Log("• Check that avatars have TransformTargets components");
        Debug.Log("• Ensure Network Camera prefab is assigned and active");
        Debug.Log("• Look for error messages in Unity console");
        Debug.Log("");
        
        Debug.Log("Performance Issues:");
        Debug.Log("• Lower camera resolution in Python script if needed");
        Debug.Log("• Reduce tracking solution quality settings");
        Debug.Log("• Check network bandwidth usage");
        Debug.Log("• Monitor CPU usage on both devices");
        Debug.Log("");
    }
    
    // Public methods for UI integration
    public void ShowHostSetup() => LogHostInstructions();
    public void ShowClientSetup() => LogClientInstructions();
    public void ShowPythonSetup() => LogPythonSystemSetup();
    public void ShowTroubleshooting() => LogTroubleshootingTips();
    
    public string GetQuickSetupSummary()
    {
        return $"HOST: Bryce=local, Remy=network | CLIENT: python main.py -> networkedwebcam -> Unity join\\n" +
               $"Host IP: {hostIP}:{networkPort}\\n" +
               $"Python path: {pythonSystemPath}";
    }
    
    [ContextMenu("Update Host IP")]
    public void UpdateHostIP()
    {
        // Get the local IP for convenience
        string localIP = GetLocalIPAddress();
        if (!string.IsNullOrEmpty(localIP))
        {
            hostIP = localIP;
            Debug.Log($"Updated host IP to: {hostIP}");
        }
    }
    
    private string GetLocalIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not get local IP: {e.Message}");
        }
        return "";
    }
}