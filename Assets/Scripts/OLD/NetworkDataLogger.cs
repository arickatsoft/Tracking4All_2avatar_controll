using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Tracking4All;
using Tracking4All.Multiplayer;

namespace Tracking4All
{
    /// <summary>
    /// Logs camera input data for network transmission and simulates network avatar movement
    /// This helps debug and test multiplayer functionality
    /// </summary>
    public class NetworkDataLogger : MonoBehaviour
    {
        [Header("Avatar References")]
        [SerializeField] private GameObject bryceAvatar;
        [SerializeField] private GameObject remyAvatar;
        
        [Header("Logging Settings")]
        [SerializeField] private bool enableDetailedLogging = true;
        [SerializeField] private bool logEveryFrame = false;
        [SerializeField] private float loggingInterval = 0.5f; // Log every 0.5 seconds
        
        [Header("Network Simulation")]
        [SerializeField] private bool simulateNetworkData = false;
        [SerializeField] private float networkSimulationRate = 30f; // 30 Hz
        
        // Internal state
        private float lastLogTime = 0f;
        private float lastNetworkSimTime = 0f;
        private HumanPoseHandler bryceHandler;
        private HumanPoseHandler remyHandler;
        private HumanPose bryceCurrentPose;
        private HumanPose remyCurrentPose;
        
        // Network simulation data
        private List<SimulatedNetworkFrame> networkFrameBuffer = new List<SimulatedNetworkFrame>();
        private bool isHost = false;
        private bool isClient = false;
        
        [System.Serializable]
        public class SimulatedNetworkFrame
        {
            public string avatarName;
            public Vector3 bodyPosition;
            public Quaternion bodyRotation;
            public float[] muscles; // 95 muscle values
            public float timestamp;
            public Vector3[] keyBonePositions; // Head, LeftHand, RightHand positions
            public Quaternion[] keyBoneRotations; // Head, LeftHand, RightHand rotations
        }
        
        void Start()
        {
            InitializeAvatarReferences();
            SetupPoseHandlers();
            
            UnityEngine.Debug.Log("=== NetworkDataLogger Initialized ===");
            UnityEngine.Debug.Log($"Bryce Avatar: {(bryceAvatar != null ? "Found" : "NOT FOUND")}");
            UnityEngine.Debug.Log($"Remy Avatar: {(remyAvatar != null ? "Found" : "NOT FOUND")}");
        }
        
        void InitializeAvatarReferences()
        {
            if (bryceAvatar == null)
                bryceAvatar = GameObject.Find("Bryce");
                
            if (remyAvatar == null)
                remyAvatar = GameObject.Find("Remy");
        }
        
        void SetupPoseHandlers()
        {
            if (bryceAvatar != null)
            {
                Animator bryceAnimator = bryceAvatar.GetComponent<Animator>();
                if (bryceAnimator != null && bryceAnimator.avatar != null && bryceAnimator.avatar.isHuman)
                {
                    bryceHandler = new HumanPoseHandler(bryceAnimator.avatar, bryceAnimator.transform);
                    UnityEngine.Debug.Log("✅ Bryce pose handler setup successfully");
                }
                else
                {
                    UnityEngine.Debug.LogError("❌ Bryce avatar missing Animator or human avatar");
                }
            }
            
            if (remyAvatar != null)
            {
                Animator remyAnimator = remyAvatar.GetComponent<Animator>();
                if (remyAnimator != null && remyAnimator.avatar != null && remyAnimator.avatar.isHuman)
                {
                    remyHandler = new HumanPoseHandler(remyAnimator.avatar, remyAnimator.transform);
                    UnityEngine.Debug.Log("✅ Remy pose handler setup successfully");
                }
                else
                {
                    UnityEngine.Debug.LogError("❌ Remy avatar missing Animator or human avatar");
                }
            }
        }
        
        void Update()
        {
            // Regular pose data logging
            if (enableDetailedLogging && (logEveryFrame || Time.time - lastLogTime >= loggingInterval))
            {
                LogCameraInputData();
                lastLogTime = Time.time;
            }
            
            // Network simulation
            if (simulateNetworkData && Time.time - lastNetworkSimTime >= (1f / networkSimulationRate))
            {
                SimulateNetworkTransmission();
                lastNetworkSimTime = Time.time;
            }
        }
        
        /// <summary>
        /// Logs current camera input data that can be translated to bone movements
        /// This is the data that would normally be sent over the network
        /// </summary>
        void LogCameraInputData()
        {
            LogAvatarPoseData("BRYCE", bryceHandler, ref bryceCurrentPose);
            LogAvatarPoseData("REMY", remyHandler, ref remyCurrentPose);
        }
        
        void LogAvatarPoseData(string avatarName, HumanPoseHandler handler, ref HumanPose currentPose)
        {
            if (handler == null) return;
            
            // Get current pose from Tracking4All
            handler.GetHumanPose(ref currentPose);
            
            // Extract key bone positions for network transmission
            var keyBones = ExtractKeyBoneData(avatarName);
            
            UnityEngine.Debug.Log($"=== CAMERA INPUT DATA - {avatarName} ===");
            UnityEngine.Debug.Log($"[{Time.time:F2}s] Body Position: {currentPose.bodyPosition}");
            UnityEngine.Debug.Log($"[{Time.time:F2}s] Body Rotation: {currentPose.bodyRotation.eulerAngles}");
            
            // Log key bone positions (most important for network)
            if (keyBones.Count > 0)
            {
                UnityEngine.Debug.Log($"[{Time.time:F2}s] === KEY BONE POSITIONS ===");
                foreach (var bone in keyBones)
                {
                    UnityEngine.Debug.Log($"[{Time.time:F2}s]   {bone.Key}: Pos={bone.Value.position} | Rot={bone.Value.rotation.eulerAngles}");
                }
            }
            
            // Log muscle values (for precise network transmission)
            if (currentPose.muscles != null && currentPose.muscles.Length > 0)
            {
                // Log first few muscle values as example
                string muscleLog = $"[{Time.time:F2}s] Muscle Values (first 10): ";
                for (int i = 0; i < Mathf.Min(10, currentPose.muscles.Length); i++)
                {
                    muscleLog += $"M{i}={currentPose.muscles[i]:F3} ";
                }
                UnityEngine.Debug.Log(muscleLog);
                
                UnityEngine.Debug.Log($"[{Time.time:F2}s] Total Muscles: {currentPose.muscles.Length}");
            }
            
            // Create network packet simulation
            var networkPacket = CreateNetworkPacket(avatarName, currentPose, keyBones);
            LogNetworkPacketData(networkPacket);
            
            UnityEngine.Debug.Log($"=== END {avatarName} DATA ===");
        }
        
        /// <summary>
        /// Extract key bone data that's most important for network transmission
        /// </summary>
        Dictionary<string, Transform> ExtractKeyBoneData(string avatarName)
        {
            var keyBones = new Dictionary<string, Transform>();
            
            GameObject avatar = avatarName == "BRYCE" ? bryceAvatar : remyAvatar;
            if (avatar == null) return keyBones;
            
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null) return keyBones;
            
            // Key bones for network transmission
            var importantBones = new[]
            {
                HumanBodyBones.Head,
                HumanBodyBones.LeftHand,
                HumanBodyBones.RightHand,
                HumanBodyBones.LeftFoot,
                HumanBodyBones.RightFoot,
                HumanBodyBones.Hips,
                HumanBodyBones.Spine,
                HumanBodyBones.Chest
            };
            
            foreach (var bone in importantBones)
            {
                Transform boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform != null)
                {
                    keyBones[bone.ToString()] = boneTransform;
                }
            }
            
            return keyBones;
        }
        
        /// <summary>
        /// Create a network packet from current pose data
        /// </summary>
        SimulatedNetworkFrame CreateNetworkPacket(string avatarName, HumanPose pose, Dictionary<string, Transform> keyBones)
        {
            var packet = new SimulatedNetworkFrame
            {
                avatarName = avatarName,
                bodyPosition = pose.bodyPosition,
                bodyRotation = pose.bodyRotation,
                muscles = (float[])pose.muscles.Clone(),
                timestamp = Time.time,
                keyBonePositions = new Vector3[keyBones.Count],
                keyBoneRotations = new Quaternion[keyBones.Count]
            };
            
            // Extract key bone data
            int index = 0;
            foreach (var bone in keyBones)
            {
                if (index < packet.keyBonePositions.Length)
                {
                    packet.keyBonePositions[index] = bone.Value.position;
                    packet.keyBoneRotations[index] = bone.Value.rotation;
                    index++;
                }
            }
            
            return packet;
        }
        
        /// <summary>
        /// Log the network packet data in a format suitable for network transmission
        /// </summary>
        void LogNetworkPacketData(SimulatedNetworkFrame packet)
        {
            UnityEngine.Debug.Log($">>> NETWORK PACKET DATA <<<");
            UnityEngine.Debug.Log($"Avatar: {packet.avatarName} | Timestamp: {packet.timestamp:F3}");
            UnityEngine.Debug.Log($"Packet Size Estimate: {EstimatePacketSize(packet)} bytes");
            
            // This would be the actual JSON data sent over network
            string networkData = JsonUtility.ToJson(packet);
            UnityEngine.Debug.Log($"Network JSON Length: {networkData.Length} chars");
            
            // Log in a copy-pasteable format for testing
            UnityEngine.Debug.Log($"=== COPY-PASTE NETWORK DATA ===");
            UnityEngine.Debug.Log(networkData);
            UnityEngine.Debug.Log($"=== END NETWORK DATA ===");
        }
        
        int EstimatePacketSize(SimulatedNetworkFrame packet)
        {
            // Estimate UDP packet size
            int size = 0;
            size += 12; // Vector3 bodyPosition (3 * 4 bytes)
            size += 16; // Quaternion bodyRotation (4 * 4 bytes)
            size += packet.muscles.Length * 4; // muscle array (95 * 4 bytes typically)
            size += 8; // timestamp (double)
            size += packet.keyBonePositions.Length * 12; // key bone positions
            size += packet.keyBoneRotations.Length * 16; // key bone rotations
            size += 50; // JSON overhead and avatar name
            
            return size;
        }
        
        /// <summary>
        /// Simulate network transmission for testing multiplayer functionality
        /// </summary>
        void SimulateNetworkTransmission()
        {
            if (isHost)
            {
                // Host sends Remy data, receives Bryce data
                SimulateNetworkSend("HOST_SENDING_REMY", remyHandler, ref remyCurrentPose);
                SimulateNetworkReceive("HOST_RECEIVING_BRYCE", bryceHandler);
            }
            else if (isClient)
            {
                // Client sends Bryce data, receives Remy data  
                SimulateNetworkSend("CLIENT_SENDING_BRYCE", bryceHandler, ref bryceCurrentPose);
                SimulateNetworkReceive("CLIENT_RECEIVING_REMY", remyHandler);
            }
        }
        
        void SimulateNetworkSend(string context, HumanPoseHandler handler, ref HumanPose pose)
        {
            if (handler == null) return;
            
            handler.GetHumanPose(ref pose);
            UnityEngine.Debug.Log($"🌐 {context} - Body: {pose.bodyPosition} | Muscles: {pose.muscles?.Length}");
        }
        
        void SimulateNetworkReceive(string context, HumanPoseHandler handler)
        {
            // Simulate received network data with some variation
            var simulatedPose = CreateSimulatedPose();
            
            if (handler != null)
            {
                handler.SetHumanPose(ref simulatedPose);
                UnityEngine.Debug.Log($"🎯 {context} - Applied simulated network pose");
            }
        }
        
        HumanPose CreateSimulatedPose()
        {
            // Create a simple animated pose for testing
            float time = Time.time;
            
            var pose = new HumanPose();
            pose.bodyPosition = new Vector3(
                Mathf.Sin(time * 0.5f) * 0.1f,
                1.0f + Mathf.Sin(time * 2f) * 0.05f,
                0f
            );
            pose.bodyRotation = Quaternion.Euler(0, Mathf.Sin(time * 0.3f) * 5f, 0);
            
            // Initialize muscle array
            pose.muscles = new float[95]; // Standard human muscle count
            
            // Add some simple animations
            for (int i = 0; i < pose.muscles.Length; i++)
            {
                pose.muscles[i] = Mathf.Sin(time + i * 0.1f) * 0.1f; // Small movements
            }
            
            return pose;
        }
        
        /// <summary>
        /// Call this when Host button is clicked to set role
        /// </summary>
        public void SetAsHost()
        {
            isHost = true;
            isClient = false;
            simulateNetworkData = true;
            
            UnityEngine.Debug.Log("🎮 NETWORK ROLE: HOST (Controls Remy locally, receives Bryce from network)");
            UnityEngine.Debug.Log("📊 Network simulation enabled - Remy will be active avatar");
            
            // Log initial state
            LogHostSetup();
        }
        
        /// <summary>
        /// Call this when Join button is clicked to set role
        /// </summary>
        public void SetAsClient()
        {
            isHost = false;
            isClient = true;
            simulateNetworkData = true;
            
            UnityEngine.Debug.Log("🎮 NETWORK ROLE: CLIENT (Controls Bryce locally, receives Remy from network)");
            UnityEngine.Debug.Log("📊 Network simulation enabled - Bryce will be active avatar");
            
            // Log initial state
            LogClientSetup();
        }
        
        void LogHostSetup()
        {
            UnityEngine.Debug.Log("=== HOST SETUP COMPLETE ===");
            UnityEngine.Debug.Log("Local Avatar: Remy (camera input controls this)");
            UnityEngine.Debug.Log("Remote Avatar: Bryce (receives network data)");
            UnityEngine.Debug.Log("Network Data Flow: SEND Remy → RECEIVE Bryce");
            UnityEngine.Debug.Log("===========================");
        }
        
        void LogClientSetup()
        {
            UnityEngine.Debug.Log("=== CLIENT SETUP COMPLETE ===");
            UnityEngine.Debug.Log("Local Avatar: Bryce (camera input controls this)");
            UnityEngine.Debug.Log("Remote Avatar: Remy (receives network data)");
            UnityEngine.Debug.Log("Network Data Flow: SEND Bryce → RECEIVE Remy");
            UnityEngine.Debug.Log("============================");
        }
        
        /// <summary>
        /// Apply network data to simulate remote avatar movement
        /// Call this with network data received from the other device
        /// </summary>
        public void ApplyNetworkDataToAvatar(string jsonData)
        {
            try
            {
                var networkFrame = JsonUtility.FromJson<SimulatedNetworkFrame>(jsonData);
                
                if (networkFrame != null)
                {
                    HumanPoseHandler targetHandler = null;
                    string targetAvatar = "";
                    
                    // Determine which avatar to apply the data to based on role
                    if (isHost && networkFrame.avatarName == "BRYCE")
                    {
                        targetHandler = bryceHandler;
                        targetAvatar = "Bryce";
                    }
                    else if (isClient && networkFrame.avatarName == "REMY")
                    {
                        targetHandler = remyHandler;
                        targetAvatar = "Remy";
                    }
                    
                    if (targetHandler != null)
                    {
                        // Apply the network pose data
                        var pose = new HumanPose
                        {
                            bodyPosition = networkFrame.bodyPosition,
                            bodyRotation = networkFrame.bodyRotation,
                            muscles = networkFrame.muscles
                        };
                        
                        targetHandler.SetHumanPose(ref pose);
                        
                        UnityEngine.Debug.Log($"🌐 NETWORK DATA APPLIED to {targetAvatar}:");
                        UnityEngine.Debug.Log($"   Body Position: {pose.bodyPosition}");
                        UnityEngine.Debug.Log($"   Body Rotation: {pose.bodyRotation.eulerAngles}");
                        UnityEngine.Debug.Log($"   Muscle Count: {pose.muscles?.Length}");
                        UnityEngine.Debug.Log($"   Timestamp: {networkFrame.timestamp:F3}");
                        UnityEngine.Debug.Log($"   Latency: {Time.time - networkFrame.timestamp:F3}s");
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to apply network data: {e.Message}");
            }
        }
        
        /// <summary>
        /// Test method to validate avatar movement with sample data
        /// </summary>
        [ContextMenu("Test Network Movement")]
        public void TestNetworkMovement()
        {
            // Create sample network data
            var testPose = CreateSimulatedPose();
            var testFrame = new SimulatedNetworkFrame
            {
                avatarName = isHost ? "BRYCE" : "REMY",
                bodyPosition = testPose.bodyPosition,
                bodyRotation = testPose.bodyRotation,
                muscles = testPose.muscles,
                timestamp = Time.time
            };
            
            string testJson = JsonUtility.ToJson(testFrame);
            UnityEngine.Debug.Log($"🧪 TESTING with data: {testJson}");
            
            ApplyNetworkDataToAvatar(testJson);
        }
    }
}