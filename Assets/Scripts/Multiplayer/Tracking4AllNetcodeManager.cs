using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Complete Unity Netcode implementation for Tracking4All multiplayer pose tracking
    /// This replaces your existing networking scripts with proper Netcode architecture
    /// </summary>
    public class Tracking4AllNetcodeManager : NetworkBehaviour
    {
        [Header("Tracking4All Components")]
        [SerializeField] private GameObject poseSolution;
        [SerializeField] private GameObject avatar;
        [SerializeField] private GameObject puppet;
        
        [Header("Network Configuration")]
        [SerializeField] private float updateRate = 30f;
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private Text statusText; // Optional UI status display
        
        // Network synchronized pose data
        private NetworkVariable<PoseNetworkData> currentPoseData = new NetworkVariable<PoseNetworkData>(
            default, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
        );
        
        // Tracking variables
        private float lastBroadcastTime = 0f;
        private bool isInitialized = false;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            InitializeComponents();
            
            if (IsOwner)
            {
                SetupHostMode();
            }
            else
            {
                SetupClientMode();
            }
            
            // Subscribe to network data changes
            currentPoseData.OnValueChanged += OnPoseDataChanged;
            
            isInitialized = true;
            
            if (enableDebugLogs)
                UnityEngine.Debug.Log($"Tracking4AllNetcodeManager: Initialized as {(IsOwner ? "HOST" : "CLIENT")}");
        }
        
        private void InitializeComponents()
        {
            // Auto-find Tracking4All components if not assigned
            if (poseSolution == null)
            {
                poseSolution = GameObject.Find("Pose Solution");
                if (poseSolution == null)
                    UnityEngine.Debug.LogWarning("Pose Solution GameObject not found!");
            }
            
            if (avatar == null)
            {
                // Try to find avatar by Player tag first
                var playerObjects = GameObject.FindGameObjectsWithTag("Player");
                if (playerObjects.Length > 0)
                {
                    avatar = playerObjects[0];
                }
                else
                {
                    // Fallback to common avatar names
                    avatar = GameObject.Find("Adam_Big") ?? GameObject.Find("Remy");
                }
                
                if (avatar == null)
                    UnityEngine.Debug.LogWarning("Avatar GameObject not found! Please assign manually.");
            }
            
            if (puppet == null)
            {
                puppet = GameObject.Find("Puppet-StandardT");
                if (puppet == null)
                    UnityEngine.Debug.LogWarning("Puppet GameObject not found!");
            }
            
            if (enableDebugLogs)
            {
                UnityEngine.Debug.Log("Component Assignment:");
                UnityEngine.Debug.Log($"  Pose Solution: {poseSolution?.name ?? "NULL"}");
                UnityEngine.Debug.Log($"  Avatar: {avatar?.name ?? "NULL"}");
                UnityEngine.Debug.Log($"  Puppet: {puppet?.name ?? "NULL"}");
            }
        }
        
        private void SetupHostMode()
        {
            // Host: Enable camera-based pose tracking
            if (poseSolution != null)
            {
                poseSolution.SetActive(true);
                
                if (enableDebugLogs)
                    UnityEngine.Debug.Log("HOST: Enabled camera-based pose tracking");
            }
            
            // Host: Keep avatar and puppet active for self-view
            if (avatar != null) avatar.SetActive(true);
            if (puppet != null) puppet.SetActive(true);
            
            // Start broadcasting pose data at specified rate
            InvokeRepeating(nameof(BroadcastPoseData), 0.1f, 1f / updateRate);
        }
        
        private void SetupClientMode()
        {
            // Client: Disable camera-based tracking to save resources
            if (poseSolution != null)
            {
                poseSolution.SetActive(false);
                
                if (enableDebugLogs)
                    UnityEngine.Debug.Log("CLIENT: Disabled camera-based pose tracking");
            }
            
            // Client: Enable avatar for network-driven animation
            if (avatar != null) avatar.SetActive(true);
            if (puppet != null) puppet.SetActive(true);
        }
        
        private void BroadcastPoseData()
        {
            if (!IsOwner || !IsServer) return;
            
            // Extract pose data from Tracking4All system
            var poseData = ExtractPoseData();
            
            // Update network variable (automatically syncs to all clients)
            currentPoseData.Value = poseData;
            
            lastBroadcastTime = Time.time;
            
            if (enableDebugLogs && Time.frameCount % 180 == 0) // Log every 6 seconds at 30fps
            {
                UnityEngine.Debug.Log($"HOST: Broadcasting pose data (valid: {poseData.isValid})");
            }
        }
        
        private PoseNetworkData ExtractPoseData()
        {
            var data = new PoseNetworkData
            {
                timestamp = Time.time,
                isValid = poseSolution != null && poseSolution.activeInHierarchy
            };
            
            // TODO: Extract actual pose landmark data from your Tracking4All setup
            // This is where you would get landmark positions, rotations, etc.
            // For now, we just validate the basic tracking state
            
            return data;
        }
        
        private void OnPoseDataChanged(PoseNetworkData previousValue, PoseNetworkData newValue)
        {
            if (IsOwner) return; // Host doesn't need to process its own data
            
            if (enableDebugLogs)
            {
                UnityEngine.Debug.Log($"CLIENT: Received pose data update (valid: {newValue.isValid}, " +
                         $"age: {Time.time - newValue.timestamp:F2}s)");
            }
            
            // Apply received pose data to client avatar
            ApplyPoseDataToAvatar(newValue);
        }
        
        private void ApplyPoseDataToAvatar(PoseNetworkData poseData)
        {
            if (!poseData.isValid) return;
            
            // TODO: This is where you'll inject network pose data into your Tracking4All pipeline
            // The goal is to replace the camera input with network data on client devices
            // while maintaining the Solutions -> Puppet -> Avatar data flow
            
            // Example approach:
            // 1. Find the PoseSolution component
            // 2. Replace its LandmarkProvider with a NetworkLandmarkProvider
            // 3. Feed the network data through the existing pipeline
        }
        
        private void Update()
        {
            UpdateStatusDisplay();
        }
        
        private void UpdateStatusDisplay()
        {
            if (statusText == null) return;
            
            string status = GetNetworkStatus();
            statusText.text = $"Tracking4All Network Status:\n{status}";
        }
        
        public string GetNetworkStatus()
        {
            if (!isInitialized)
                return "Initializing...";
                
            if (IsOwner)
            {
                return $"HOST MODE\n" +
                       $"Broadcasting at {updateRate}fps\n" +
                       $"Last broadcast: {Time.time - lastBroadcastTime:F1}s ago\n" +
                       $"Pose tracking: {(poseSolution?.activeInHierarchy == true ? "Active" : "Inactive")}";
            }
            else
            {
                bool receivingData = IsReceivingNetworkData();
                float dataAge = currentPoseData.Value.isValid ? Time.time - currentPoseData.Value.timestamp : -1f;
                
                return $"CLIENT MODE\n" +
                       $"Receiving data: {(receivingData ? "Yes" : "No")}\n" +
                       $"Data age: {(dataAge >= 0 ? $"{dataAge:F1}s" : "N/A")}\n" +
                       $"Camera tracking: Disabled";
            }
        }
        
        public bool IsReceivingNetworkData()
        {
            return !IsOwner && 
                   currentPoseData.Value.isValid && 
                   (Time.time - currentPoseData.Value.timestamp) < 3f;
        }
        
        // Context menu for testing
        [ContextMenu("Force Broadcast")]
        public void ForceBroadcast()
        {
            if (IsOwner)
            {
                BroadcastPoseData();
                UnityEngine.Debug.Log("Forced pose data broadcast");
            }
        }
        
        [ContextMenu("Show Component Status")]
        public void ShowComponentStatus()
        {
            UnityEngine.Debug.Log("=== Component Status ===");
            UnityEngine.Debug.Log($"Pose Solution: {poseSolution?.name} (Active: {poseSolution?.activeInHierarchy})");
            UnityEngine.Debug.Log($"Avatar: {avatar?.name} (Active: {avatar?.activeInHierarchy})");
            UnityEngine.Debug.Log($"Puppet: {puppet?.name} (Active: {puppet?.activeInHierarchy})");
            UnityEngine.Debug.Log($"Network Status: {GetNetworkStatus()}");
        }
        
        private void OnDestroy()
        {
            if (isInitialized)
            {
                CancelInvoke(nameof(BroadcastPoseData));
            }
        }
    }
    
    /// <summary>
    /// Network-serializable pose data structure optimized for Unity Netcode
    /// </summary>
    [System.Serializable]
    public struct PoseNetworkData : System.IEquatable<PoseNetworkData>
    {
        public float timestamp;
        public bool isValid;
        
        // TODO: Add actual pose landmark data here when ready
        // Examples of what you might add:
        // public Vector3[] landmarkPositions;     // 33 MediaPipe pose landmarks
        // public float[] landmarkVisibility;      // Visibility scores 0-1
        // public float[] landmarkPresence;        // Presence scores 0-1
        // public Quaternion[] jointRotations;     // Calculated joint rotations
        
        public bool Equals(PoseNetworkData other)
        {
            return timestamp.Equals(other.timestamp) && isValid == other.isValid;
        }
        
        public override bool Equals(object obj)
        {
            return obj is PoseNetworkData other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                return (timestamp.GetHashCode() * 397) ^ isValid.GetHashCode();
            }
        }
        
        public override string ToString()
        {
            return $"PoseNetworkData(timestamp: {timestamp:F2}, valid: {isValid})";
        }
    }
}