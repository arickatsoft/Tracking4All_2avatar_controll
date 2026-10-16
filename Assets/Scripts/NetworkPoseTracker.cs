using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Mediapipe.Unity.Sample.PoseTracking;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Clean, optimized NetworkBehaviour-based pose tracker for MediaPipe integration.
    /// Properly handles the data flow: Camera → MediaPipe → Avatar Bones → Network Sync
    /// </summary>
    public class NetworkPoseTracker : NetworkBehaviour
    {
        [Header("Avatar Configuration")]
        [SerializeField] private GameObject localAvatar;   // Adam_Big for host
        [SerializeField] private GameObject remoteAvatar;  // Remy for remote
        
        [Header("MediaPipe Components")]
        [SerializeField] private Camera cameraProvider;
        [SerializeField] private PoseTrackingGraph poseTrackingGraph;
        
        [Header("Network Settings")]
        [SerializeField] private float sendRate = 30f; // Hz
        [SerializeField] private bool logNetworkData = true;
        
        // Network Variables for essential pose data
        private NetworkVariable<PoseDataNetwork> networkPoseData = new NetworkVariable<PoseDataNetwork>();
        
        // Local tracking state
        private float lastSendTime = 0f;
        private Camera mainCamera;
        
        // Pose data structure optimized for network transmission
        [System.Serializable]
        public struct PoseDataNetwork : INetworkSerializable
        {
            public Vector3 hipPosition;
            public Quaternion hipRotation;
            public Vector3 chestPosition;
            public Quaternion chestRotation;
            public Vector3 headPosition;
            public Quaternion headRotation;
            public Vector3 leftHandPos;
            public Quaternion leftHandRot;
            public Vector3 rightHandPos;
            public Quaternion rightHandRot;
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref hipPosition);
                serializer.SerializeValue(ref hipRotation);
                serializer.SerializeValue(ref chestPosition);
                serializer.SerializeValue(ref chestRotation);
                serializer.SerializeValue(ref headPosition);
                serializer.SerializeValue(ref headRotation);
                serializer.SerializeValue(ref leftHandPos);
                serializer.SerializeValue(ref leftHandRot);
                serializer.SerializeValue(ref rightHandPos);
                serializer.SerializeValue(ref rightHandRot);
            }
        }
        
private void Start()
        {
            InitializeComponents();
            SetupAvatars();
            
            // Subscribe to network data changes
            networkPoseData.OnValueChanged += OnNetworkPoseDataChanged;
            
            // Add network logging
            NetworkLogDisplay.LogNetwork("INIT", $"NetworkPoseTracker starting - IsHost: {IsHost}, IsClient: {IsClient}");
            
            // Log avatar configuration
            NetworkLogDisplay.LogNetwork("SETUP", $"Local Avatar: {(localAvatar != null ? localAvatar.name : "null")}");
            NetworkLogDisplay.LogNetwork("SETUP", $"Remote Avatar: {(remoteAvatar != null ? remoteAvatar.name : "null")}");
        }
        
        private void InitializeComponents()
        {
            mainCamera = Camera.main;
            
            // Find MediaPipe components if not assigned
            if (poseTrackingGraph == null)
                poseTrackingGraph = FindObjectOfType<PoseTrackingGraph>();
                
            if (cameraProvider == null)
                cameraProvider = Camera.main;
                
            if (logNetworkData)
                UnityEngine.Debug.Log($"[NetworkPoseTracker] Initialized - IsHost: {IsHost}, IsClient: {IsClient}");
        }
        
private void SetupAvatars()
        {
            // Use AvatarTagManager to get the correct avatar assignments
            localAvatar = AvatarTagManager.GetLocalPlayerAvatar();
            remoteAvatar = AvatarTagManager.GetRemotePlayerAvatar();
            
            if (IsHost)
            {
                // Host configuration
                if (localAvatar != null) localAvatar.SetActive(true);
                if (remoteAvatar != null) remoteAvatar.SetActive(true);
                
                // Position camera to show both avatars
                if (mainCamera != null)
                {
                    mainCamera.transform.position = new Vector3(1.2f, 1.5f, -1.3f);
                    mainCamera.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
                }
                
                NetworkLogDisplay.LogHost($"Host avatar setup - Local: {(localAvatar ? localAvatar.name : "null")}, Remote: {(remoteAvatar ? remoteAvatar.name : "null")}");
                NetworkLogDisplay.LogNetwork("CAMERA", "Camera positioned for Host view");
            }
            else
            {
                // Client configuration
                if (localAvatar != null) localAvatar.SetActive(true);
                if (remoteAvatar != null) remoteAvatar.SetActive(true);
                
                // Focus camera on local avatar for client
                if (mainCamera != null && localAvatar != null)
                {
                    Vector3 localPos = localAvatar.transform.position;
                    mainCamera.transform.position = localPos + new Vector3(0f, 1.5f, -2f);
                    mainCamera.transform.LookAt(localPos + Vector3.up);
                }
                
                NetworkLogDisplay.LogClient($"Client avatar setup - Local: {(localAvatar ? localAvatar.name : "null")}, Remote: {(remoteAvatar ? remoteAvatar.name : "null")}");
                NetworkLogDisplay.LogNetwork("CAMERA", "Camera focused on local avatar for Client view");
            }
        }
        
        private void Update()
        {
            // Only send pose data if we're tracking locally
            if (IsHost && ShouldSendPoseData())
            {
                SendLocalPoseData();
            }
        }
        
        private bool ShouldSendPoseData()
        {
            return Time.time - lastSendTime >= (1f / sendRate);
        }
        
private void SendLocalPoseData()
        {
            if (poseTrackingGraph == null) 
            {
                NetworkLogDisplay.LogNetwork("ERROR", "PoseTrackingGraph is null, cannot send pose data", true);
                return;
            }
            
            // Get pose data from MediaPipe processing
            var poseData = ExtractPoseDataFromAvatar();
            
            // Update network variable (automatically syncs to clients)
            networkPoseData.Value = poseData;
            
            lastSendTime = Time.time;
            
            // Log data transmission with details
            NetworkLogDisplay.LogData("SENT", "PoseData", System.Runtime.InteropServices.Marshal.SizeOf(poseData));
            
            if (logNetworkData && Time.time % 2f < 0.1f) // Log every 2 seconds to avoid spam
            {
                NetworkLogDisplay.LogHost($"Sending pose data - Hip: {poseData.hipPosition:F2}, Head: {poseData.headPosition:F2}");
            }
        }
        
        private PoseDataNetwork ExtractPoseDataFromAvatar()
        {
            var poseData = new PoseDataNetwork();
            
            // Extract key joint data from the local avatar
            // MediaPipe drives the avatar bones directly
            
            if (localAvatar != null)
            {
                var animatorComponent = localAvatar.GetComponent<Animator>();
                if (animatorComponent != null)
                {
                    // Get key bone transforms
                    var hipBone = animatorComponent.GetBoneTransform(HumanBodyBones.Hips);
                    var chestBone = animatorComponent.GetBoneTransform(HumanBodyBones.Chest);
                    var headBone = animatorComponent.GetBoneTransform(HumanBodyBones.Head);
                    var leftHandBone = animatorComponent.GetBoneTransform(HumanBodyBones.LeftHand);
                    var rightHandBone = animatorComponent.GetBoneTransform(HumanBodyBones.RightHand);
                    
                    if (hipBone != null)
                    {
                        poseData.hipPosition = hipBone.position;
                        poseData.hipRotation = hipBone.rotation;
                    }
                    
                    if (chestBone != null)
                    {
                        poseData.chestPosition = chestBone.position;
                        poseData.chestRotation = chestBone.rotation;
                    }
                    
                    if (headBone != null)
                    {
                        poseData.headPosition = headBone.position;
                        poseData.headRotation = headBone.rotation;
                    }
                    
                    if (leftHandBone != null)
                    {
                        poseData.leftHandPos = leftHandBone.position;
                        poseData.leftHandRot = leftHandBone.rotation;
                    }
                    
                    if (rightHandBone != null)
                    {
                        poseData.rightHandPos = rightHandBone.position;
                        poseData.rightHandRot = rightHandBone.rotation;
                    }
                }
            }
            
            return poseData;
        }
        
private void OnNetworkPoseDataChanged(PoseDataNetwork previousValue, PoseDataNetwork newValue)
        {
            if (!IsClient) return;
            
            // Log data reception with details
            NetworkLogDisplay.LogData("RECEIVED", "PoseData", System.Runtime.InteropServices.Marshal.SizeOf(newValue));
            
            // Apply received pose data to remote avatar
            ApplyPoseDataToRemoteAvatar(newValue);
            
            if (logNetworkData && Time.time % 2f < 0.1f) // Log every 2 seconds to avoid spam
            {
                NetworkLogDisplay.LogClient($"Received pose data - Hip: {newValue.hipPosition:F2}, Head: {newValue.headPosition:F2}");
                NetworkLogDisplay.LogClient($"Applied to remote avatar: {(remoteAvatar != null ? remoteAvatar.name : "null")}");
            }
        }
        
        private void ApplyPoseDataToRemoteAvatar(PoseDataNetwork poseData)
        {
            if (remoteAvatar == null) return;
            
            // Apply pose data to the remote avatar's animator
            var remoteAnimator = remoteAvatar.GetComponent<Animator>();
            if (remoteAnimator != null)
            {
                // Apply the received pose data to key bones
                ApplyBoneTransform(remoteAnimator, HumanBodyBones.Hips, poseData.hipPosition, poseData.hipRotation);
                ApplyBoneTransform(remoteAnimator, HumanBodyBones.Chest, poseData.chestPosition, poseData.chestRotation);
                ApplyBoneTransform(remoteAnimator, HumanBodyBones.Head, poseData.headPosition, poseData.headRotation);
                ApplyBoneTransform(remoteAnimator, HumanBodyBones.LeftHand, poseData.leftHandPos, poseData.leftHandRot);
                ApplyBoneTransform(remoteAnimator, HumanBodyBones.RightHand, poseData.rightHandPos, poseData.rightHandRot);
            }
        }
        
        private void ApplyBoneTransform(Animator animator, HumanBodyBones bone, Vector3 position, Quaternion rotation)
        {
            var boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform != null)
            {
                boneTransform.position = position;
                boneTransform.rotation = rotation;
            }
        }
        
public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            NetworkLogDisplay.LogNetwork("SPAWN", $"Network spawned - IsHost: {IsHost}, IsClient: {IsClient}");
            
            if (IsHost)
            {
                NetworkLogDisplay.LogHost("Starting as HOST - Will send pose data to clients");
            }
            else if (IsClient)
            {
                NetworkLogDisplay.LogClient("Starting as CLIENT - Will receive pose data from host");
            }
        }
        
        public override void OnNetworkDespawn()
        {
            networkPoseData.OnValueChanged -= OnNetworkPoseDataChanged;
            base.OnNetworkDespawn();
        }
        
        private void OnDestroy()
        {
            if (networkPoseData != null)
            {
                networkPoseData.OnValueChanged -= OnNetworkPoseDataChanged;
            }
        }
    }
}