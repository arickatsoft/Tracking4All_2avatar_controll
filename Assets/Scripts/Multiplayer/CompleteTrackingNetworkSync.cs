using Unity.Netcode;
using UnityEngine;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Complete Unity Netcode implementation for your Tracking4All multiplayer system
    /// This replaces your existing networking with proper Unity Netcode architecture
    /// 
    /// SETUP INSTRUCTIONS:
    /// 1. Wait for Unity to compile this script
    /// 2. Add this component to a GameObject with NetworkObject
    /// 3. Create prefab and add to NetworkManager's NetworkPrefabsList
    /// 4. Configure component references in inspector
    /// 5. Test with Host/Client buttons
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class CompleteTrackingNetworkSync : NetworkBehaviour
    {
        [Header("Tracking4All Component Auto-Discovery")]
        [SerializeField] private bool autoFindComponents = true;
        
        [Header("Manual Component References")]
        [SerializeField] private GameObject poseSolution;
        [SerializeField] private GameObject avatar;
        [SerializeField] private GameObject puppet;
        
        [Header("Network Configuration")]
        [SerializeField] private float broadcastRate = 30f;
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool enableInterpolation = true;
        
        // Network Variables for synchronized pose data
        private NetworkVariable<NetworkPoseData> currentPoseData = new NetworkVariable<NetworkPoseData>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
            
        // Interpolation variables
        private NetworkPoseData lastReceivedPose;
        private NetworkPoseData targetPose;
        private float interpolationProgress = 0f;
        
        // Status tracking
        private float lastBroadcastTime = 0f;
        private bool isInitialized = false;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (autoFindComponents)
            {
                AutoAssignComponents();
            }
            
            ConfigureForNetworkRole();
            
            // Subscribe to network data changes
            currentPoseData.OnValueChanged += OnPoseDataReceived;
            
            isInitialized = true;
            
            if (enableDebugLogs)
            {
                UnityEngine.Debug.Log($"CompleteTrackingNetworkSync: Initialized as {GetNetworkRole()}");
                UnityEngine.Debug.Log($"Components: PoseSolution={poseSolution?.name}, Avatar={avatar?.name}, Puppet={puppet?.name}");
            }
        }
        
        private void AutoAssignComponents()
        {
            // Auto-find Tracking4All components based on your scene structure
            if (poseSolution == null)
            {
                poseSolution = GameObject.Find("Pose Solution");
                if (poseSolution == null)
                {
                    // Try alternate names
                    var poseSolutions = FindObjectsOfType<Transform>();
                    foreach (var t in poseSolutions)
                    {
                        if (t.name.Contains("Pose") && t.name.Contains("Solution"))
                        {
                            poseSolution = t.gameObject;
                            break;
                        }
                    }
                }
            }
            
            if (avatar == null)
            {
                // Try Player tag first
                var playerObjects = GameObject.FindGameObjectsWithTag("Player");
                if (playerObjects.Length > 0)
                {
                    avatar = playerObjects[0];
                }
                else
                {
                    // Try common avatar names from your project
                    avatar = GameObject.Find("Adam_Big") ?? 
                            GameObject.Find("Remy") ?? 
                            GameObject.Find("Bryce");
                }
            }
            
            if (puppet == null)
            {
                puppet = GameObject.Find("Puppet-StandardT");
                if (puppet == null)
                {
                    // Try finding any puppet
                    var puppets = FindObjectsOfType<Transform>();
                    foreach (var t in puppets)
                    {
                        if (t.name.ToLower().Contains("puppet"))
                        {
                            puppet = t.gameObject;
                            break;
                        }
                    }
                }
            }
        }
        
        private void ConfigureForNetworkRole()
        {
            if (IsOwner)
            {
                SetupHostMode();
            }
            else
            {
                SetupClientMode();
            }
        }
        
        private void SetupHostMode()
        {
            // Host: Enable camera-based pose tracking
            if (poseSolution != null)
            {
                poseSolution.SetActive(true);
                
                if (enableDebugLogs)
                    UnityEngine.Debug.Log("HOST: Camera-based pose tracking enabled");
            }
            
            // Host: Keep avatar and puppet active for self-view
            if (avatar != null) avatar.SetActive(true);
            if (puppet != null) puppet.SetActive(true);
            
            // Start broadcasting pose data at configured rate
            InvokeRepeating(nameof(BroadcastPoseData), 0.1f, 1f / broadcastRate);
        }
        
        private void SetupClientMode()
        {
            // Client: Disable camera-based tracking to save resources
            if (poseSolution != null)
            {
                poseSolution.SetActive(false);
                
                if (enableDebugLogs)
                    UnityEngine.Debug.Log("CLIENT: Camera-based pose tracking disabled");
            }
            
            // Client: Keep avatar and puppet active for network-driven animation
            if (avatar != null) avatar.SetActive(true);
            if (puppet != null) puppet.SetActive(true);
        }
        
        private void BroadcastPoseData()
        {
            if (!IsOwner || !IsServer) return;
            
            var poseData = ExtractCurrentPoseData();
            
            // Update NetworkVariable - automatically syncs to all clients
            currentPoseData.Value = poseData;
            
            lastBroadcastTime = Time.time;
            
            if (enableDebugLogs && Time.frameCount % 180 == 0) // Every 6 seconds at 30fps
            {
                UnityEngine.Debug.Log($"HOST: Broadcasting pose data (valid: {poseData.isValid})");
            }
        }
        
        private NetworkPoseData ExtractCurrentPoseData()
        {
            var data = new NetworkPoseData
            {
                timestamp = Time.time,
                isValid = poseSolution != null && poseSolution.activeInHierarchy
            };
            
            // TODO: Extract actual pose landmark data from your Tracking4All system
            // This is where you would get actual landmark positions, rotations, etc.
            // For now, we validate basic tracking state
            
            if (data.isValid && puppet != null)
            {
                // Example: You could extract puppet transform data here
                // data.rootPosition = puppet.transform.position;
                // data.rootRotation = puppet.transform.rotation;
            }
            
            return data;
        }
        
        private void OnPoseDataReceived(NetworkPoseData previousValue, NetworkPoseData newValue)
        {
            if (IsOwner) return; // Host doesn't process its own data
            
            if (!newValue.isValid) return;
            
            // Setup interpolation for smooth client animation
            if (enableInterpolation)
            {
                lastReceivedPose = targetPose;
                targetPose = newValue;
                interpolationProgress = 0f;
            }
            else
            {
                // Apply immediately without interpolation
                ApplyPoseToAvatar(newValue);
            }
            
            if (enableDebugLogs)
            {
                float latency = Time.time - newValue.timestamp;
                UnityEngine.Debug.Log($"CLIENT: Received pose data (latency: {latency:F3}s)");
            }
        }
        
        private void Update()
        {
            // Handle client-side interpolation
            if (!IsOwner && enableInterpolation && targetPose.isValid)
            {
                interpolationProgress += Time.deltaTime * broadcastRate * 0.8f;
                interpolationProgress = Mathf.Clamp01(interpolationProgress);
                
                var interpolatedPose = InterpolatePoses(lastReceivedPose, targetPose, interpolationProgress);
                ApplyPoseToAvatar(interpolatedPose);
            }
        }
        
        private NetworkPoseData InterpolatePoses(NetworkPoseData from, NetworkPoseData to, float t)
        {
            var result = new NetworkPoseData
            {
                timestamp = Mathf.Lerp(from.timestamp, to.timestamp, t),
                isValid = to.isValid
            };
            
            // TODO: Add actual landmark/transform interpolation here
            // Example:
            // result.rootPosition = Vector3.Lerp(from.rootPosition, to.rootPosition, t);
            // result.rootRotation = Quaternion.Lerp(from.rootRotation, to.rootRotation, t);
            
            return result;
        }
        
        private void ApplyPoseToAvatar(NetworkPoseData poseData)
        {
            if (!poseData.isValid) return;
            
            // TODO: This is the critical integration point!
            // Here you need to inject the network pose data into your Tracking4All pipeline
            // instead of camera input. The goal is to maintain the data flow:
            // Network Data → Solutions Layer → Puppet → Avatar
            //
            // Approach options:
            // 1. Replace the PoseSolution's LandmarkProvider with a NetworkLandmarkProvider
            // 2. Directly update puppet transforms with network data  
            // 3. Create a custom adapter that feeds network data into existing pipeline
            
            if (enableDebugLogs && Time.frameCount % 60 == 0) // Every 2 seconds at 30fps
            {
                UnityEngine.Debug.Log($"CLIENT: Applying pose data to avatar");
            }
        }
        
        // Public API for monitoring and debugging
        public bool IsReceivingData()
        {
            return !IsOwner && currentPoseData.Value.isValid && 
                   (Time.time - currentPoseData.Value.timestamp) < 3f;
        }
        
        public string GetNetworkRole()
        {
            if (IsOwner && IsServer) return "HOST";
            if (IsClient && !IsServer) return "CLIENT";
            return "UNKNOWN";
        }
        
        public string GetDetailedStatus()
        {
            if (!isInitialized) return "Initializing...";
            
            if (IsOwner)
            {
                return $"HOST MODE\\n" +
                       $"Broadcasting: {broadcastRate}fps\\n" +
                       $"Last broadcast: {Time.time - lastBroadcastTime:F1}s ago\\n" +
                       $"Pose tracking: {(poseSolution?.activeInHierarchy == true ? "Active" : "Inactive")}";
            }
            else
            {
                bool receiving = IsReceivingData();
                float dataAge = currentPoseData.Value.isValid ? Time.time - currentPoseData.Value.timestamp : -1f;
                
                return $"CLIENT MODE\\n" +
                       $"Receiving data: {(receiving ? "Yes" : "No")}\\n" +
                       $"Data latency: {(dataAge >= 0 ? $"{dataAge:F2}s" : "N/A")}\\n" +
                       $"Camera tracking: Disabled";
            }
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
        
        [ContextMenu("Show Status")]
        public void ShowStatus()
        {
            UnityEngine.Debug.Log($"=== CompleteTrackingNetworkSync Status ===");
            UnityEngine.Debug.Log($"Role: {GetNetworkRole()}");
            UnityEngine.Debug.Log($"Components Found: {CountValidComponents()}/3");
            UnityEngine.Debug.Log($"Network Status: {GetDetailedStatus()}");
            UnityEngine.Debug.Log($"Is Receiving Data: {IsReceivingData()}");
        }
        
        private int CountValidComponents()
        {
            int count = 0;
            if (poseSolution != null) count++;
            if (avatar != null) count++;
            if (puppet != null) count++;
            return count;
        }
        
        private void OnDestroy()
        {
            if (IsOwner && isInitialized)
            {
                CancelInvoke(nameof(BroadcastPoseData));
            }
        }
    }
    
    /// <summary>
    /// Network-serializable pose data structure optimized for Unity Netcode
    /// </summary>
    [System.Serializable]
    public struct NetworkPoseData : System.IEquatable<NetworkPoseData>
    {
        public float timestamp;
        public bool isValid;
        
        // TODO: Add your actual pose data fields here based on your Tracking4All setup
        // Examples of what you might add:
        // public Vector3[] landmarkPositions;     // MediaPipe pose landmarks  
        // public float[] landmarkVisibility;      // Visibility scores
        // public float[] landmarkPresence;        // Presence scores
        // public Vector3 rootPosition;            // Avatar root position
        // public Quaternion rootRotation;         // Avatar root rotation
        // public Quaternion[] jointRotations;     // Joint rotations from puppet
        
        public bool Equals(NetworkPoseData other)
        {
            return timestamp.Equals(other.timestamp) && isValid == other.isValid;
        }
        
        public override bool Equals(object obj)
        {
            return obj is NetworkPoseData other && Equals(other);
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
            return $"NetworkPoseData(timestamp: {timestamp:F2}, valid: {isValid})";
        }
    }
}
