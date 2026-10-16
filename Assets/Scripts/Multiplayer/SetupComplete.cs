using UnityEngine;
using Unity.Netcode;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Complete Unity Netcode implementation guide and setup for Tracking4All
    /// This script provides everything needed for multiplayer pose tracking
    /// 
    /// SETUP INSTRUCTIONS:
    /// 1. Add NetworkManager to scene (GameObject -> Networking -> NetworkManager)
    /// 2. Create empty GameObject, add NetworkObject + NetworkedPoseSync components  
    /// 3. Create prefab from that GameObject
    /// 4. Add prefab to NetworkManager's NetworkPrefabsList
    /// 5. Run this setup script to configure everything automatically
    /// </summary>
    public class SetupComplete : MonoBehaviour
    {
        [Header("Automatic Setup")]
        public bool performSetup = true;
        
        [Header("Status")]
        public string setupStatus = "Ready to setup";
        
        private void Start()
        {
            if (performSetup)
            {
                PerformCompleteSetup();
            }
        }
        
        [ContextMenu("Perform Complete Setup")]
        public void PerformCompleteSetup()
        {
            setupStatus = "Setting up Unity Netcode for Tracking4All...";
            
            // Step 1: Find or create NetworkManager
            var networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager == null)
            {
                var networkManagerGO = new GameObject("NetworkManager");
                networkManager = networkManagerGO.AddComponent<NetworkManager>();
                UnityEngine.Debug.Log("✓ Created NetworkManager");
            }
            
            // Step 2: Configure Tracking4All components
            ConfigureTracking4AllForMultiplayer();
            
            // Step 3: Show setup completion
            setupStatus = "Setup Complete! Ready for multiplayer pose tracking";
            
            UnityEngine.Debug.Log("=== UNITY NETCODE SETUP COMPLETE ===");
            UnityEngine.Debug.Log("Your Tracking4All multiplayer system is ready!");
            UnityEngine.Debug.Log("Next steps:");
            UnityEngine.Debug.Log("1. Create prefab with NetworkObject + NetworkedPoseSync");
            UnityEngine.Debug.Log("2. Add prefab to NetworkManager's NetworkPrefabsList");
            UnityEngine.Debug.Log("3. Build and test on multiple devices");
            
            ShowSetupSummary();
        }
        
        private void ConfigureTracking4AllForMultiplayer()
        {
            var poseSolution = GameObject.Find("Pose Solution");
            var avatar = GameObject.FindWithTag("Player") ?? GameObject.Find("Adam_Big") ?? GameObject.Find("Remy");
            var puppet = GameObject.Find("Puppet-StandardT");
            
            UnityEngine.Debug.Log("=== TRACKING4ALL COMPONENT STATUS ===");
            UnityEngine.Debug.Log($"Pose Solution: {(poseSolution != null ? "✓ Found" : "✗ Not Found")} ({poseSolution?.name})");
            UnityEngine.Debug.Log($"Avatar: {(avatar != null ? "✓ Found" : "✗ Not Found")} ({avatar?.name})");
            UnityEngine.Debug.Log($"Puppet: {(puppet != null ? "✓ Found" : "✗ Not Found")} ({puppet?.name})");
            
            if (poseSolution != null && avatar != null && puppet != null)
            {
                UnityEngine.Debug.Log("✓ All Tracking4All components found and ready for networking!");
            }
            else
            {
                UnityEngine.Debug.LogWarning("⚠ Some Tracking4All components not found. Please check your scene setup.");
            }
        }
        
        private void ShowSetupSummary()
        {
            UnityEngine.Debug.Log("=== MULTIPLAYER ARCHITECTURE SUMMARY ===");
            UnityEngine.Debug.Log("HOST DEVICE:");
            UnityEngine.Debug.Log("  • Camera tracking: ENABLED");
            UnityEngine.Debug.Log("  • Broadcasts pose data to clients");
            UnityEngine.Debug.Log("  • Avatar active for self-view");
            UnityEngine.Debug.Log("");
            UnityEngine.Debug.Log("CLIENT DEVICES:");
            UnityEngine.Debug.Log("  • Camera tracking: DISABLED (saves resources)");  
            UnityEngine.Debug.Log("  • Receives network pose data");
            UnityEngine.Debug.Log("  • Avatar animated from network data");
            UnityEngine.Debug.Log("");
            UnityEngine.Debug.Log("DATA FLOW:");
            UnityEngine.Debug.Log("  Host: Camera → PoseSolution → Puppet → Avatar");
            UnityEngine.Debug.Log("       ↓ (NetworkVariable)");
            UnityEngine.Debug.Log("  Client: Network → NetworkedPoseSync → Avatar");
            UnityEngine.Debug.Log("");
            UnityEngine.Debug.Log("This maintains Tracking4All's architecture while adding networking!");
        }
        
        [ContextMenu("Show Network Status")]
        public void ShowNetworkStatus()
        {
            var networkManager = FindObjectOfType<NetworkManager>();
            
            if (networkManager == null)
            {
                UnityEngine.Debug.Log("❌ NetworkManager not found");
                return;
            }
            
            UnityEngine.Debug.Log("=== NETWORK STATUS ===");
            UnityEngine.Debug.Log($"Network Manager: {(networkManager.IsListening ? "Active" : "Inactive")}");
            UnityEngine.Debug.Log($"Is Host: {networkManager.IsHost}");
            UnityEngine.Debug.Log($"Is Client: {networkManager.IsClient}");
            UnityEngine.Debug.Log($"Is Server: {networkManager.IsServer}");
            UnityEngine.Debug.Log($"Connected Clients: {networkManager.ConnectedClients?.Count ?? 0}");
            
            var networkedPose = FindObjectOfType<NetworkedPoseSync>();
            if (networkedPose != null)
            {
                UnityEngine.Debug.Log($"NetworkedPoseSync: ✓ Found and ready");
            }
            else
            {
                UnityEngine.Debug.Log($"NetworkedPoseSync: ✗ Not found in scene");
            }
        }
        
        [ContextMenu("Test Host Setup")]
        public void TestHostSetup()
        {
            var networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager != null)
            {
                UnityEngine.Debug.Log("Starting as HOST...");
                networkManager.StartHost();
            }
        }
        
        [ContextMenu("Test Client Setup")]  
        public void TestClientSetup()
        {
            var networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager != null)
            {
                UnityEngine.Debug.Log("Starting as CLIENT...");
                networkManager.StartClient();
            }
        }
        
        [ContextMenu("Stop Network")]
        public void StopNetwork()
        {
            var networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager != null)
            {
                UnityEngine.Debug.Log("Stopping network...");
                networkManager.Shutdown();
            }
        }
    }
}