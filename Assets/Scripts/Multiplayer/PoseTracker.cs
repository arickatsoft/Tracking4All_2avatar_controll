using Unity.Netcode;
using UnityEngine;

namespace Tracking4All.Multiplayer
{
    public class NetcodePoseTracker : NetworkBehaviour
    {
        [Header("Tracking4All Integration")]
        public GameObject hostPoseSolution;
        public GameObject clientAvatar;
        public GameObject puppet;
        
        [Header("Network Settings")]
        public float networkUpdateRate = 30f;
        public bool debugNetworking = true;
        
        private NetworkVariable<PoseData> poseData = new NetworkVariable<PoseData>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            AutoAssignComponents();
            
            if (IsOwner)
            {
                SetupHost();
            }
            else
            {
                SetupClient();
            }
            
            poseData.OnValueChanged += OnPoseDataReceived;
            UnityEngine.Debug.Log($"NetcodePoseTracker spawned as {(IsOwner ? "HOST" : "CLIENT")}");
        }
        
        private void AutoAssignComponents()
        {
            if (hostPoseSolution == null)
                hostPoseSolution = GameObject.Find("Pose Solution");
                
            if (clientAvatar == null)
            {
                var player = GameObject.FindWithTag("Player");
                clientAvatar = player ?? GameObject.Find("Adam_Big") ?? GameObject.Find("Remy");
            }
            
            if (puppet == null)
                puppet = GameObject.Find("Puppet-StandardT");
        }
        
        private void SetupHost()
        {
            if (hostPoseSolution != null) hostPoseSolution.SetActive(true);
            if (clientAvatar != null) clientAvatar.SetActive(true);
            if (puppet != null) puppet.SetActive(true);
            
            InvokeRepeating(nameof(BroadcastPose), 0.1f, 1f / networkUpdateRate);
            UnityEngine.Debug.Log("HOST: Camera tracking enabled");
        }
        
        private void SetupClient()
        {
            if (hostPoseSolution != null) hostPoseSolution.SetActive(false);
            if (clientAvatar != null) clientAvatar.SetActive(true);
            if (puppet != null) puppet.SetActive(true);
            
            UnityEngine.Debug.Log("CLIENT: Camera tracking disabled");
        }
        
        private void BroadcastPose()
        {
            if (!IsOwner) return;
            
            var data = new PoseData
            {
                timestamp = Time.time,
                isValid = hostPoseSolution != null && hostPoseSolution.activeInHierarchy
            };
            
            poseData.Value = data;
        }
        
        private void OnPoseDataReceived(PoseData previous, PoseData current)
        {
            if (IsOwner) return;
            
            if (debugNetworking)
                UnityEngine.Debug.Log($"CLIENT: Received pose data (valid: {current.isValid})");
                
            ApplyPoseToAvatar(current);
        }
        
        private void ApplyPoseToAvatar(PoseData data)
        {
            if (!data.isValid) return;
            // TODO: Apply network pose data to avatar
        }
        
        public bool IsReceivingData()
        {
            return !IsOwner && poseData.Value.isValid;
        }
    }
    
    [System.Serializable]
    public struct PoseData : System.IEquatable<PoseData>
    {
        public float timestamp;
        public bool isValid;
        
        public bool Equals(PoseData other)
        {
            return timestamp.Equals(other.timestamp) && isValid == other.isValid;
        }
        
        public override bool Equals(object obj)
        {
            return obj is PoseData other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return (timestamp.GetHashCode() * 397) ^ isValid.GetHashCode();
        }
    }
}