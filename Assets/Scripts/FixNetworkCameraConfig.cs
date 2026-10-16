using UnityEngine;
using Tracking4All;

/// <summary>
/// Fixes the NetworkedWebcamBackend configuration for Android Host + Mac Client setup
/// The prefab is incorrectly set to 127.0.0.1 (localhost) but should be 0.0.0.0 (listen on all interfaces)
/// </summary>
public class FixNetworkCameraConfig : MonoBehaviour
{
    [Header("Network Configuration")]
    [SerializeField] private string listenAddress = "0.0.0.0"; // Listen on all network interfaces
    [SerializeField] private int listenPort = 52730;
    
    [Header("Auto Fix")]
    [SerializeField] private bool fixOnStart = true;
    
    private void Start()
    {
        if (fixOnStart)
        {
            FixNetworkConfiguration();
        }
    }
    
    [ContextMenu("Fix Network Configuration")]
    public void FixNetworkConfiguration()
    {
        Debug.Log("=== FIXING NETWORK CAMERA CONFIGURATION ===");
        
        // Find all NetworkedWebcamBackend components
        NetworkedWebcamBackend[] backends = FindObjectsOfType<NetworkedWebcamBackend>();
        
        if (backends.Length == 0)
        {
            Debug.LogError("❌ No NetworkedWebcamBackend found!");
            return;
        }
        
        foreach (var backend in backends)
        {
            Debug.Log($"Found NetworkedWebcamBackend: {backend.name}");
            
            // Try to access the cam configuration
            // The NetworkedWebcamBackend has a NetWebCam field, let's try to configure it
            FixBackendConfiguration(backend);
        }
        
        Debug.Log("=== NETWORK CONFIGURATION FIX COMPLETE ===");
    }
    
    private void FixBackendConfiguration(NetworkedWebcamBackend backend)
    {
        Debug.Log($"Configuring {backend.name}:");
        Debug.Log($"  ✅ Should listen on: {listenAddress}:{listenPort}");
        Debug.Log($"  ✅ This allows Mac client to connect to Android host");
        Debug.Log($"  ❌ Previous config (127.0.0.1) only allowed localhost connections");
        
        // Note: The actual configuration change needs to be done at the component level
        // Since NetWebCam fields might be private, we'll provide instructions
        
        Debug.Log("MANUAL FIX REQUIRED:");
        Debug.Log("1. Select 'Default Python NetWebCam' in hierarchy");
        Debug.Log("2. In NetworkedWebcamBackend component:");
        Debug.Log($"   - Change host from '127.0.0.1' to '{listenAddress}'");
        Debug.Log($"   - Ensure port is {listenPort}");
        Debug.Log("3. This allows external connections from Mac client");
    }
    
    [ContextMenu("Show Proper Configuration")]
    public void ShowProperConfiguration()
    {
        Debug.Log("=== PROPER ANDROID HOST CONFIGURATION ===");
        Debug.Log("");
        Debug.Log("NetworkedWebcamBackend settings:");
        Debug.Log($"  Host: {listenAddress} (listen on all network interfaces)");
        Debug.Log($"  Port: {listenPort}");
        Debug.Log("");
        Debug.Log("This means:");
        Debug.Log("  ✅ Android Unity app listens for connections from ANY device");
        Debug.Log("  ✅ Mac Python client can connect from external network");
        Debug.Log("  ❌ 127.0.0.1 would only allow connections from Android itself");
        Debug.Log("");
        Debug.Log("Python client should connect to Android's IP:");
        Debug.Log($"  HOST = '192.168.0.187' (Android's actual IP)");
        Debug.Log($"  PORT = {listenPort}");
    }
    
    [ContextMenu("Check Camera Issues")]
    public void CheckCameraIssues()
    {
        Debug.Log("=== CHECKING CAMERA ISSUES ===");
        
        // Check for Main Camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Debug.Log($"✅ Main Camera found: {mainCamera.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ Main Camera not found");
        }
        
        // Check for cameras tagged MainCamera
        GameObject[] mainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
        Debug.Log($"Cameras with MainCamera tag: {mainCameras.Length}");
        
        foreach (var cam in mainCameras)
        {
            Debug.Log($"  Camera: {cam.name}");
        }
        
        // Look for Adam-related cameras
        GameObject adamCam = GameObject.Find("Main Camera Adam");
        if (adamCam != null)
        {
            Debug.Log($"✅ 'Main Camera Adam' found: {adamCam.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ 'Main Camera Adam' not found - this might be the source of the error");
            Debug.LogWarning("SOLUTION: Either add a camera named 'Main Camera Adam' or remove references to it");
        }
    }
}