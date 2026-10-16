using UnityEngine;
using Tracking4All;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Quick debug script to check if Unity is properly listening for network connections
/// Add this to a GameObject in your scene to debug the connection issue
/// </summary>
public class QuickNetworkDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool debugOnStart = true;
    [SerializeField] private bool continuousDebug = false;
    [SerializeField] private float debugInterval = 5f;
    
    private float lastDebugTime = 0f;
    
    private void Start()
    {
        if (debugOnStart)
        {
            Invoke(nameof(RunNetworkDebug), 2f); // Wait 2 seconds for initialization
        }
    }
    
    private void Update()
    {
        if (continuousDebug && Time.time - lastDebugTime >= debugInterval)
        {
            RunNetworkDebug();
            lastDebugTime = Time.time;
        }
    }
    
    [ContextMenu("Run Network Debug")]
    public void RunNetworkDebug()
    {
        Debug.Log("=== UNITY NETWORK DEBUG ===");
        
        // 1. Check if NetworkedWebcamBackend exists
        CheckNetworkBackend();
        
        // 2. Check our IP address
        CheckDeviceIP();
        
        // 3. Try to test port 52730
        TestPortBinding();
        
        // 4. Check scene setup
        CheckSceneSetup();
        
        Debug.Log("=== END NETWORK DEBUG ===");
    }
    
    private void CheckNetworkBackend()
    {
        Debug.Log("--- NetworkedWebcamBackend Check ---");
        
        NetworkedWebcamBackend[] backends = FindObjectsOfType<NetworkedWebcamBackend>();
        
        if (backends.Length == 0)
        {
            Debug.LogError("❌ NO NetworkedWebcamBackend found in scene!");
            Debug.LogError("SOLUTION: Add 'Default Python NetWebCam' prefab to your scene");
            Debug.LogError("Location: Assets/Tracking4All/Prefabs/Default Python NetWebCam.prefab");
            return;
        }
        
        Debug.Log($"✅ Found {backends.Length} NetworkedWebcamBackend(s)");
        
        foreach (var backend in backends)
        {
            Debug.Log($"Backend: {backend.name}");
            Debug.Log($"  GameObject Active: {backend.gameObject.activeInHierarchy}");
            Debug.Log($"  Component Enabled: {backend.enabled}");
            
            if (backend.Lifecycle != null)
            {
                Debug.Log($"  Lifecycle Ready: {backend.Lifecycle.IsReady(0)}");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ Lifecycle is null - backend not properly initialized");
            }
        }
    }
    
    private void CheckDeviceIP()
    {
        Debug.Log("--- Device IP Check ---");
        
        try
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            
            Debug.Log($"Device hostname: {hostName}");
            
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                {
                    Debug.Log($"✅ Device IP: {address} (Use this in Python script)");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Could not get device IP: {e.Message}");
        }
    }
    
    private void TestPortBinding()
    {
        Debug.Log("--- Port 52730 Test ---");
        
        try
        {
            // Try to create a listener on the port
            TcpListener testListener = new TcpListener(IPAddress.Any, 52730);
            testListener.Start();
            
            Debug.Log("✅ Port 52730 is available");
            Debug.Log("❌ But this means Unity ISN'T using it yet!");
            Debug.Log("PROBLEM: NetworkedWebcamBackend should be listening on this port");
            
            testListener.Stop();
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Debug.Log("✅ Port 52730 is IN USE (good - Unity is listening)");
                Debug.Log("If Python still can't connect, check firewall/network settings");
            }
            else
            {
                Debug.LogError($"❌ Port test failed: {e.Message}");
            }
        }
    }
    
    private void CheckSceneSetup()
    {
        Debug.Log("--- Scene Setup Check ---");
        
        // Check for essential multiplayer components
        var networkManager = FindObjectOfType<Unity.Netcode.NetworkManager>();
        Debug.Log($"NetworkManager: {(networkManager != null ? "✅ Found" : "❌ Missing")}");
        
        var dualSetup = FindObjectOfType<DualTrackingSolutionSetup>();
        Debug.Log($"DualTrackingSolutionSetup: {(dualSetup != null ? "✅ Found" : "❌ Missing")}");
        
        // Check if we're actually hosting
        if (networkManager != null)
        {
            Debug.Log($"Network Status: IsHost={networkManager.IsHost}, IsClient={networkManager.IsClient}");
            if (!networkManager.IsHost && !networkManager.IsClient)
            {
                Debug.LogWarning("⚠️ NetworkManager not started - click 'Host Game' first!");
            }
        }
    }
    
    // Public method to show instructions
    [ContextMenu("Show Setup Instructions")]
    public void ShowSetupInstructions()
    {
        Debug.Log("=== SETUP INSTRUCTIONS ===");
        Debug.Log("1. Add 'Default Python NetWebCam' prefab to scene");
        Debug.Log("2. Start Unity app on Android");
        Debug.Log("3. Click 'Host Game' button");
        Debug.Log("4. Wait 10-15 seconds for full initialization");
        Debug.Log("5. Run this debug script to verify Unity is listening");
        Debug.Log("6. Only then start Python script on Mac");
    }
}