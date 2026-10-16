// NETWORKING SCRIPT - DISABLED FOR LOCAL PLAY
// Uncomment and use this when you add Unity Netcode packages back

/*
using Unity.Netcode;
using UnityEngine;

public class BasicNetworkManager : MonoBehaviour
{
    [Header("Network Settings")]
    public NetworkManager networkManager;
    
    [Header("UI References")]
    public GameObject hostButton;
    public GameObject clientButton;
    public GameObject statusText;
    
    void Start()
    {
        // Find network manager if not assigned
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();
        }
        
        // Subscribe to network events
        if (networkManager != null)
        {
            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }
    
    public void StartHost()
    {
        if (networkManager != null)
        {
            networkManager.StartHost();
            Debug.Log("Starting as Host...");
        }
    }
    
    public void StartClient()
    {
        if (networkManager != null)
        {
            networkManager.StartClient();
            Debug.Log("Starting as Client...");
        }
    }
    
    public void StartServer()
    {
        if (networkManager != null)
        {
            networkManager.StartServer();
            Debug.Log("Starting as Server...");
        }
    }
    
    private void OnServerStarted()
    {
        Debug.Log("Server started successfully!");
        UpdateUI("Server Started - Waiting for players...");
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected!");
        UpdateUI($"Player {clientId} joined the game!");
        
        // Check if we have 2 players to start the match
        if (networkManager.ConnectedClients.Count >= 2)
        {
            UpdateUI("Match Ready! Both players connected.");
            // You can add match start logic here
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected!");
        UpdateUI($"Player {clientId} left the game.");
    }
    
    private void UpdateUI(string message)
    {
        if (statusText != null)
        {
            var textComponent = statusText.GetComponent<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                textComponent.text = message;
            }
        }
        Debug.Log(message);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (networkManager != null)
        {
            networkManager.OnServerStarted -= OnServerStarted;
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
*/

// FOR NOW: Use LocalHitSystemSetup.cs instead!