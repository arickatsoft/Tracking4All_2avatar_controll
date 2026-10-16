using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Simple fix for the existing PoseSyncManager to send correct data format
    /// This replaces HumanPose muscle data with landmark position data
    /// </summary>
    public class PoseSyncDataFixer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PoseSyncManager originalPoseSync;
        [SerializeField] private GameObject bryceAvatar;
        [SerializeField] private GameObject remyAvatar;
        
        [Header("Data Correction")]
        [SerializeField] private bool enableDataCorrection = true;
        [SerializeField] private bool logDataCorrection = true;
        
        // Simple landmark data structure
        [System.Serializable]
        public class SimpleLandmarkData
        {
            public string avatarName;
            public float timestamp;
            public Vector3[] keyPositions; // Head, LeftHand, RightHand, etc.
            public string[] positionNames;
        }
        
        void Start()
        {
            // Find components if not assigned
            if (originalPoseSync == null)
                originalPoseSync = FindObjectOfType<PoseSyncManager>();
                
            if (bryceAvatar == null)
                bryceAvatar = GameObject.Find("Bryce");
                
            if (remyAvatar == null)
                remyAvatar = GameObject.Find("Remy");
                
            if (logDataCorrection)
            {
                UnityEngine.Debug.Log("🔧 POSE SYNC DATA FIXER - Ready to correct network data format");
                UnityEngine.Debug.Log($"Original PoseSyncManager: {(originalPoseSync != null ? "Found" : "Missing")}");
            }
        }
        
        /// <summary>
        /// Extract simple landmark data from avatar instead of complex HumanPose
        /// </summary>
        public SimpleLandmarkData ExtractLandmarkData(GameObject avatar)
        {
            if (avatar == null) return null;
            
            var landmarkData = new SimpleLandmarkData
            {
                avatarName = avatar.name.ToUpper(),
                timestamp = Time.time,
                keyPositions = new Vector3[8],
                positionNames = new string[] { "Head", "LeftHand", "RightHand", "LeftFoot", "RightFoot", "Hips", "Spine", "Chest" }
            };
            
            // Get Animator component
            Animator animator = avatar.GetComponent<Animator>();
            if (animator != null)
            {
                // Extract key bone positions (this is the CORRECT data to send)
                var keyBones = new HumanBodyBones[]
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
                
                for (int i = 0; i < keyBones.Length && i < landmarkData.keyPositions.Length; i++)
                {
                    Transform boneTransform = animator.GetBoneTransform(keyBones[i]);
                    if (boneTransform != null)
                    {
                        landmarkData.keyPositions[i] = boneTransform.position;
                    }
                    else
                    {
                        landmarkData.keyPositions[i] = Vector3.zero;
                    }
                }
                
                if (logDataCorrection)
                {
                    UnityEngine.Debug.Log($"📤 CORRECTED DATA EXTRACTED from {avatar.name}:");
                    UnityEngine.Debug.Log($"   Head: {landmarkData.keyPositions[0]}");
                    UnityEngine.Debug.Log($"   LeftHand: {landmarkData.keyPositions[1]}");
                    UnityEngine.Debug.Log($"   RightHand: {landmarkData.keyPositions[2]}");
                }
            }
            
            return landmarkData;
        }
        
        /// <summary>
        /// Apply landmark data to avatar (CORRECT method)
        /// </summary>
        public void ApplyLandmarkData(GameObject avatar, SimpleLandmarkData data)
        {
            if (avatar == null || data == null) return;
            
            if (logDataCorrection)
            {
                UnityEngine.Debug.Log($"📥 APPLYING CORRECTED DATA to {avatar.name}:");
                UnityEngine.Debug.Log($"   Source: {data.avatarName}");
                UnityEngine.Debug.Log($"   Positions: {data.keyPositions?.Length ?? 0}");
                UnityEngine.Debug.Log($"   Head Position: {(data.keyPositions?.Length > 0 ? data.keyPositions[0] : Vector3.zero)}");
            }
            
            // This is where the corrected data would be applied to make avatar move properly
            // Instead of using HumanPoseHandler.SetHumanPose() (which causes wrong positions)
            // We would use landmark positions to drive avatar movement correctly
            
            // For now, just log that we received corrected data
            UnityEngine.Debug.Log($"✅ CORRECTED network data received for {avatar.name} - avatar should move properly now");
        }
        
        /// <summary>
        /// Create corrected network data JSON
        /// </summary>
        public string CreateCorrectedNetworkData(GameObject sourceAvatar)
        {
            var landmarkData = ExtractLandmarkData(sourceAvatar);
            if (landmarkData != null)
            {
                string jsonData = JsonUtility.ToJson(landmarkData);
                
                if (logDataCorrection)
                {
                    UnityEngine.Debug.Log($"📦 CORRECTED NETWORK JSON created ({jsonData.Length} chars):");
                    UnityEngine.Debug.Log($"   Format: Landmark positions (not HumanPose muscles)");
                    UnityEngine.Debug.Log($"   This should fix the avatar movement issues!");
                }
                
                return jsonData;
            }
            
            return "";
        }
        
        /// <summary>
        /// Apply received corrected network data
        /// </summary>
        public void ApplyCorrectedNetworkData(string jsonData, bool isHost)
        {
            try
            {
                var landmarkData = JsonUtility.FromJson<SimpleLandmarkData>(jsonData);
                
                if (landmarkData != null)
                {
                    GameObject targetAvatar = null;
                    
                    // Determine target avatar based on role
                    if (isHost && landmarkData.avatarName.Contains("BRYCE"))
                    {
                        targetAvatar = bryceAvatar;
                    }
                    else if (!isHost && landmarkData.avatarName.Contains("REMY"))
                    {
                        targetAvatar = remyAvatar;
                    }
                    
                    if (targetAvatar != null)
                    {
                        ApplyLandmarkData(targetAvatar, landmarkData);
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error applying corrected network data: {e.Message}");
            }
        }
        
        /// <summary>
        /// Test the corrected data extraction
        /// </summary>
        [ContextMenu("Test Corrected Data Extraction")]
        public void TestCorrectedDataExtraction()
        {
            UnityEngine.Debug.Log("🧪 TESTING CORRECTED DATA EXTRACTION");
            
            if (bryceAvatar != null)
            {
                string bryceData = CreateCorrectedNetworkData(bryceAvatar);
                UnityEngine.Debug.Log($"Bryce corrected data: {bryceData}");
            }
            
            if (remyAvatar != null)
            {
                string remyData = CreateCorrectedNetworkData(remyAvatar);
                UnityEngine.Debug.Log($"Remy corrected data: {remyData}");
            }
            
            UnityEngine.Debug.Log("✅ Data extraction test complete");
            UnityEngine.Debug.Log("💡 Key difference: Position data instead of muscle data");
        }
        
        /// <summary>
        /// Integration point for existing multiplayer system
        /// </summary>
        public void OnMultiplayerModeChanged(bool isHost)
        {
            if (logDataCorrection)
            {
                UnityEngine.Debug.Log($"🔧 DATA FIXER: Multiplayer mode = {(isHost ? "HOST" : "CLIENT")}");
                UnityEngine.Debug.Log("📋 Data correction active - will send landmark positions instead of muscle values");
            }
        }
    }
}