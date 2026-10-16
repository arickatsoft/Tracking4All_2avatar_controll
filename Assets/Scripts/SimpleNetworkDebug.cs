using UnityEngine;
using Unity.Netcode;
using Tracking4All;

/// <summary>
/// Simple debug script to check network status
/// </summary>
public class SimpleNetworkDebug : MonoBehaviour
{
    private void Start()
    {
        // Check every 5 seconds
        InvokeRepeating(nameof(LogNetworkStatus), 2f, 5f);
    }
    
    private void LogNetworkStatus()
    {
        Debug.Log("=== NETWORK STATUS ===");
        
        // Check Unity Netcode
        var networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            Debug.Log($"Unity Netcode - IsHost: {networkManager.IsHost}, IsClient: {networkManager.IsClient}");
        }
        else
        {
            Debug.Log("❌ Unity NetworkManager not found");
        }
        
        // Check MultiplayerNetworkManager
        var multiplayerManager = FindObjectOfType<Tracking4All.Multiplayer.MultiplayerNetworkManager>();
        if (multiplayerManager != null)
        {
            Debug.Log("✅ MultiplayerNetworkManager found");
        }
        else
        {
            Debug.Log("❌ MultiplayerNetworkManager not found");
        }
        
        // Check NetworkedWebcamBackend
        var webcamBackend = FindObjectOfType<NetworkedWebcamBackend>();
        if (webcamBackend != null)
        {
            Debug.Log($"NetworkedWebcamBackend - Ready: {webcamBackend.Lifecycle?.IsReady(0)}");
        }
        else
        {
            Debug.Log("❌ NetworkedWebcamBackend not found");
        }
    }
    
    [ContextMenu("Check Now")]
    public void CheckNow()
    {
        LogNetworkStatus();
    }
}