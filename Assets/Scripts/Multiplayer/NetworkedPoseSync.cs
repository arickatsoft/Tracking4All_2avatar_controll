using UnityEngine;
using Unity.Netcode;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Simple NetworkBehaviour component for Tracking4All pose synchronization
    /// Add this to a GameObject with NetworkObject to enable multiplayer pose tracking
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedPoseSync : NetworkBehaviour
    {
        [Header("Auto-Setup")]
        public bool autoFindComponents = true;
        
        [Header("Manual Component Assignment")]
        public GameObject poseSolutionObject;
        public GameObject avatarObject;
        public GameObject puppetObject;
        
        [Header("Network Settings")]
        public float broadcastRate = 30f;
        
        // Network synchronized data
        private NetworkVariable<PoseSync> poseData = new NetworkVariable<PoseSync>();
        
        private void Start()
        {
            if (autoFindComponents)
            {
                AutoAssignComponents();
            }
        }
        
        private void AutoAssignComponents()
        {
            if (poseSolutionObject == null)
                poseSolutionObject = GameObject.Find("Pose Solution");
                
            if (avatarObject == null)
            {
                var player = GameObject.FindWithTag("Player");
                avatarObject = player ?? GameObject.Find("Adam_Big") ?? GameObject.Find("Remy");
            }
            
            if (puppetObject == null)
                puppetObject = GameObject.Find("Puppet-StandardT");
        }
        
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // Host setup
                if (poseSolutionObject != null) poseSolutionObject.SetActive(true);
                InvokeRepeating(nameof(SyncPose), 0.1f, 1f / broadcastRate);
                UnityEngine.Debug.Log("NetworkedPoseSync: HOST started");
            }
            else
            {
                // Client setup  
                if (poseSolutionObject != null) poseSolutionObject.SetActive(false);
                poseData.OnValueChanged += OnPoseReceived;
                UnityEngine.Debug.Log("NetworkedPoseSync: CLIENT connected");
            }
            
            // Keep avatar and puppet active for both
            if (avatarObject != null) avatarObject.SetActive(true);
            if (puppetObject != null) puppetObject.SetActive(true);
        }
        
        private void SyncPose()
        {
            if (!IsOwner) return;
            
            var sync = new PoseSync
            {
                timestamp = Time.time,
                isActive = poseSolutionObject != null && poseSolutionObject.activeInHierarchy
            };
            
            poseData.Value = sync;
        }
        
        private void OnPoseReceived(PoseSync previous, PoseSync current)
        {
            UnityEngine.Debug.Log($"Client received pose data: {current.isActive}");
            // TODO: Apply pose data to client avatar
        }
    }
    
    [System.Serializable]
    public struct PoseSync : System.IEquatable<PoseSync>
    {
        public float timestamp;
        public bool isActive;
        
        public bool Equals(PoseSync other)
        {
            return timestamp.Equals(other.timestamp) && isActive == other.isActive;
        }
        
        public override bool Equals(object obj)
        {
            return obj is PoseSync other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return (timestamp.GetHashCode() * 397) ^ isActive.GetHashCode();
        }
    }
}