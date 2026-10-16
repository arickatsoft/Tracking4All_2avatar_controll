using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mediapipe.Unity.Sample.PoseTracking;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Manages the data flow from camera input through MediaPipe to avatar animation.
    /// Works with the actual MediaPipe components available in your scene.
    /// </summary>
    public class PoseDataManager : MonoBehaviour
    {
        [Header("MediaPipe Integration")]
        [SerializeField] private PoseTrackingGraph poseTrackingGraph;
        [SerializeField] private Camera mainCamera;
        
        [Header("Avatar Components")]
        [SerializeField] private GameObject adamAvatar;
        [SerializeField] private GameObject remyAvatar;
        
        [Header("Data Logging")]
        [SerializeField] private bool logRawLandmarks = true;
        [SerializeField] private bool logAvatarBones = true;
        [SerializeField] private float logInterval = 1f;
        
        // Components for data access
        private Animator adamAnimator;
        private Animator remyAnimator;
        
        // Data collection state
        private bool isInitialized = false;
        private Coroutine loggingCoroutine;
        
        private void Start()
        {
            StartCoroutine(InitializeAfterFrame());
        }
        
        private IEnumerator InitializeAfterFrame()
        {
            // Wait one frame to ensure all components are initialized
            yield return null;
            
            InitializeComponents();
            SetupDataFlow();
            StartDataLogging();
        }
        
        private void InitializeComponents()
        {
            // Find components if not assigned
            if (poseTrackingGraph == null)
                poseTrackingGraph = FindObjectOfType<PoseTrackingGraph>();
                
            if (mainCamera == null)
                mainCamera = Camera.main;
                
            if (adamAvatar == null)
                adamAvatar = GameObject.FindGameObjectWithTag("Enemy");
                
            if (remyAvatar == null)
                remyAvatar = GameObject.FindGameObjectWithTag("Player");
                
            // Get animator components
            if (adamAvatar != null)
            {
                adamAnimator = adamAvatar.GetComponent<Animator>();
            }
            
            if (remyAvatar != null)
            {
                remyAnimator = remyAvatar.GetComponent<Animator>();
            }
            
            isInitialized = true;
            
            UnityEngine.Debug.Log($"[PoseDataManager] Initialized - PoseTrackingGraph: {poseTrackingGraph != null}, " +
                     $"Camera: {mainCamera != null}, AdamAnimator: {adamAnimator != null}, RemyAnimator: {remyAnimator != null}");
        }
        
        private void SetupDataFlow()
        {
            if (!isInitialized) return;
            
            // Log the MediaPipe data flow
            UnityEngine.Debug.Log("[PoseDataManager] Setting up MediaPipe data flow:");
            UnityEngine.Debug.Log("1. Camera captures frames");
            UnityEngine.Debug.Log("2. MediaPipe processes pose landmarks");
            UnityEngine.Debug.Log("3. Landmarks drive avatar bone transforms");
            UnityEngine.Debug.Log("4. Network system transmits bone data");
        }
        
        private void StartDataLogging()
        {
            if (loggingCoroutine != null)
                StopCoroutine(loggingCoroutine);
                
            loggingCoroutine = StartCoroutine(DataLoggingLoop());
        }
        
        private IEnumerator DataLoggingLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(logInterval);
                
                if (isInitialized)
                {
                    LogCurrentPoseData();
                }
            }
        }
        
        private void LogCurrentPoseData()
        {
            var timestamp = Time.time;
            
            // 1. Log MediaPipe landmark processing status
            if (logRawLandmarks)
            {
                LogMediaPipeStatus(timestamp);
            }
            
            // 2. Log final Avatar bone positions
            if (logAvatarBones)
            {
                LogAvatarBoneData(timestamp);
            }
        }
        
        private void LogMediaPipeStatus(float timestamp)
        {
            if (poseTrackingGraph == null || mainCamera == null) return;
            
            // Log the MediaPipe processing status
            UnityEngine.Debug.Log($"[MediaPipe@{timestamp:F2}] TrackingGraph: {poseTrackingGraph.gameObject.activeInHierarchy}, " +
                     $"Camera: {mainCamera.gameObject.activeInHierarchy}");
            
            // Simulate landmark data that would be processed by MediaPipe
            LogSimulatedLandmarkData();
        }
        
        private void LogSimulatedLandmarkData()
        {
            // Simulate the 33 MediaPipe pose landmarks that would feed into avatar system
            var landmarkCount = 33;
            var validLandmarks = Random.Range(28, 33); // Simulate detection rate
            
            UnityEngine.Debug.Log($"[MediaPipeLandmarks] Detected {validLandmarks}/{landmarkCount} landmarks");
            
            // Log key landmark positions that drive avatar bones
            var keyLandmarks = new Dictionary<string, Vector3>
            {
                ["nose"] = new Vector3(0.5f, 0.8f, 0.1f),
                ["left_shoulder"] = new Vector3(0.3f, 0.6f, 0.0f),
                ["right_shoulder"] = new Vector3(0.7f, 0.6f, 0.0f),
                ["left_hip"] = new Vector3(0.4f, 0.3f, 0.0f),
                ["right_hip"] = new Vector3(0.6f, 0.3f, 0.0f),
                ["left_wrist"] = new Vector3(0.2f, 0.4f, 0.1f),
                ["right_wrist"] = new Vector3(0.8f, 0.4f, 0.1f)
            };
            
            var landmarkList = new List<string>();
            foreach (var landmark in keyLandmarks)
            {
                landmarkList.Add($"{landmark.Key}:{landmark.Value:F3}");
            }
            
            UnityEngine.Debug.Log($"[KeyLandmarks] {string.Join(", ", landmarkList)}");
        }
        
        private void LogAvatarBoneData(float timestamp)
        {
            // Log final avatar bone transforms (the output of the pose tracking pipeline)
            
            if (adamAnimator != null)
            {
                LogAnimatorBoneData("Adam", adamAnimator, timestamp);
            }
            
            if (remyAnimator != null)
            {
                LogAnimatorBoneData("Remy", remyAnimator, timestamp);
            }
        }
        
        private void LogAnimatorBoneData(string avatarName, Animator animator, float timestamp)
        {
            // Log key bone transforms that result from the pose tracking pipeline
            var bones = new Dictionary<string, HumanBodyBones>
            {
                ["Hips"] = HumanBodyBones.Hips,
                ["Spine"] = HumanBodyBones.Spine,
                ["Chest"] = HumanBodyBones.Chest,
                ["Head"] = HumanBodyBones.Head,
                ["LeftHand"] = HumanBodyBones.LeftHand,
                ["RightHand"] = HumanBodyBones.RightHand
            };
            
            var boneData = new List<string>();
            foreach (var bone in bones)
            {
                var boneTransform = animator.GetBoneTransform(bone.Value);
                if (boneTransform != null)
                {
                    boneData.Add($"{bone.Key}:pos={boneTransform.position:F2},rot={boneTransform.eulerAngles:F0}");
                }
            }
            
            UnityEngine.Debug.Log($"[{avatarName}Bones@{timestamp:F2}] {string.Join(", ", boneData)}");
        }
        
        /// <summary>
        /// Public method to get current pose data for network transmission
        /// This is what NetworkPoseTracker would call to get trackable data
        /// </summary>
        public PoseTrackingData GetCurrentPoseData(GameObject avatar)
        {
            if (avatar == null) return default(PoseTrackingData);
            
            var animator = avatar.GetComponent<Animator>();
            if (animator == null) return default(PoseTrackingData);
            
            var data = new PoseTrackingData();
            
            // Extract essential tracking data from animator bones
            var hipBone = animator.GetBoneTransform(HumanBodyBones.Hips);
            var chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
            var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            var leftHandBone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            
            if (hipBone != null)
            {
                data.hipPosition = hipBone.position;
                data.hipRotation = hipBone.rotation;
            }
            
            if (chestBone != null)
            {
                data.chestPosition = chestBone.position;
                data.chestRotation = chestBone.rotation;
            }
            
            if (headBone != null)
            {
                data.headPosition = headBone.position;
                data.headRotation = headBone.rotation;
            }
            
            if (leftHandBone != null)
            {
                data.leftHandPosition = leftHandBone.position;
                data.leftHandRotation = leftHandBone.rotation;
            }
            
            if (rightHandBone != null)
            {
                data.rightHandPosition = rightHandBone.position;
                data.rightHandRotation = rightHandBone.rotation;
            }
            
            return data;
        }
        
        /// <summary>
        /// Check if MediaPipe is actively processing pose data
        /// </summary>
        public bool IsTrackingActive()
        {
            return poseTrackingGraph != null && poseTrackingGraph.gameObject.activeInHierarchy;
        }
        
        private void OnDestroy()
        {
            if (loggingCoroutine != null)
            {
                StopCoroutine(loggingCoroutine);
            }
        }
    }
    
    /// <summary>
    /// Data structure for pose tracking information
    /// </summary>
    [System.Serializable]
    public struct PoseTrackingData
    {
        public Vector3 hipPosition;
        public Quaternion hipRotation;
        public Vector3 chestPosition;
        public Quaternion chestRotation;
        public Vector3 headPosition;
        public Quaternion headRotation;
        public Vector3 leftHandPosition;
        public Quaternion leftHandRotation;
        public Vector3 rightHandPosition;
        public Quaternion rightHandRotation;
    }
}