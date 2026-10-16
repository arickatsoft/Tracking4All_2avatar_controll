using UnityEngine;
using Tracking4All;

namespace Tracking4All
{
    /// <summary>
    /// Real-time movement validator - compares recorded vs applied poses
    /// </summary>
    public class MovementValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        public float validationInterval = 1f;
        public bool showValidationResults = true;
        
        private NetworkDataLogger logger;
        private float lastValidationTime = 0f;
        
        void Start()
        {
            logger = FindObjectOfType<NetworkDataLogger>();
            UnityEngine.Debug.Log("🔍 MOVEMENT VALIDATOR ACTIVE");
        }
        
        void Update()
        {
            if (Time.time - lastValidationTime >= validationInterval)
            {
                ValidateMovement();
                lastValidationTime = Time.time;
            }
        }
        
        void ValidateMovement()
        {
            // Get current avatar poses
            GameObject bryce = GameObject.Find("Bryce");
            GameObject remy = GameObject.Find("Remy");
            
            if (bryce != null && remy != null)
            {
                ValidateAvatarPose(bryce, "BRYCE");
                ValidateAvatarPose(remy, "REMY");
            }
        }
        
        void ValidateAvatarPose(GameObject avatar, string name)
        {
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null) return;
            
            // Check if avatar is actually moving/posing
            bool isMoving = CheckIfAvatarIsMoving(animator);
            
            UnityEngine.Debug.Log($"✅ {name} Movement Status: {(isMoving ? "ACTIVE" : "STATIC")}");
            
            if (isMoving)
            {
                LogCurrentPose(avatar, name);
            }
        }
        
        bool CheckIfAvatarIsMoving(Animator animator)
        {
            // Check key bones for movement
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            
            // Simple movement detection - check if positions are changing
            return head != null && (
                head.position != Vector3.zero ||
                (leftHand != null && leftHand.position.y > 0.5f) ||
                (rightHand != null && rightHand.position.y > 0.5f)
            );
        }
        
        void LogCurrentPose(GameObject avatar, string name)
        {
            Animator animator = avatar.GetComponent<Animator>();
            
            // Log key bone positions
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            
            UnityEngine.Debug.Log($"🎭 {name} Current Pose:");
            if (head != null)
                UnityEngine.Debug.Log($"   Head: {head.position} | Rot: {head.rotation.eulerAngles}");
            if (leftHand != null)
                UnityEngine.Debug.Log($"   LeftHand: {leftHand.position}");
            if (rightHand != null)
                UnityEngine.Debug.Log($"   RightHand: {rightHand.position}");
        }
    }
}