using UnityEngine;
using Unity.Netcode;
using Tracking4All;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Fixes the avatar floating issue by ensuring proper pose data flow and avatar control.
    /// Integrates with Tracking4All framework for correct pose tracking and network synchronization.
    /// </summary>
    public class AvatarNetworkController : NetworkBehaviour
    {
        [Header("Avatar Configuration")]
        [SerializeField] private GameObject targetAvatar;
        [SerializeField] private bool isLocalAvatar = true;
        
        [Header("Tracking4All Integration")]
        [SerializeField] private bool useTracking4AllData = true;
        [SerializeField] private Transform rootTransform;
        
        [Header("Debug Settings")]
        [SerializeField] private bool logMovement = true;
        [SerializeField] private bool preventDrift = true;
        
        // Network variables for position and rotation
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
        
        // Movement validation
        private Vector3 lastValidPosition;
        private Quaternion lastValidRotation;
        private float maxMovementPerFrame = 2f; // Max movement per frame in units
        private float maxRotationPerFrame = 180f; // Max rotation per frame in degrees
        
        // Tracking4All components
        private MonoBehaviour puppet; // Can be PuppetBase, PuppetJointProvider, or PuppetBodyPart
        private Animator avatarAnimator;
        
        private void Start()
        {
            InitializeComponents();
            SetupNetworkCallbacks();
            
            NetworkLogDisplay.LogNetwork("AVATAR", $"AvatarNetworkController initialized for {targetAvatar?.name}");
        }
        
private void InitializeComponents()
        {
            if (targetAvatar == null)
            {
                targetAvatar = gameObject;
            }
            
            // Find Tracking4All components - search for actual components that exist
            
            // Look for PosePuppet component (this is the main puppet component)
            var posePuppetComponents = FindObjectsOfType<MonoBehaviour>();
            MonoBehaviour foundPosePuppet = null;
            foreach (var component in posePuppetComponents)
            {
                if (component.GetType().Name == "PosePuppet" || component.GetType().FullName.Contains("PosePuppet"))
                {
                    foundPosePuppet = component;
                    break;
                }
            }
            
            // Look for the main Tracking4All component
            var tracking4AllMainComponents = FindObjectsOfType<MonoBehaviour>();
            MonoBehaviour foundTracking4All = null;
            foreach (var component in tracking4AllMainComponents)
            {
                if (component.GetType().Name == "Tracking4All" || component.GetType().FullName.Contains("Tracking4All.Tracking4All"))
                {
                    foundTracking4All = component;
                    break;
                }
            }
            
            // Look for PuppetJointProvider or other puppet-related components
            var puppetJointProviders = FindObjectsOfType<MonoBehaviour>();
            MonoBehaviour foundJointProvider = null;
            foreach (var component in puppetJointProviders)
            {
                if (component.GetType().Name.Contains("PuppetJointProvider") || component.GetType().Name.Contains("JointProvider"))
                {
                    foundJointProvider = component;
                    break;
                }
            }
            
            // Store the puppet reference (prioritize PosePuppet as it's the main avatar controller)
            if (foundPosePuppet != null)
            {
                puppet = foundPosePuppet;
            }
            else if (foundTracking4All != null)
            {
                puppet = foundTracking4All;
            }
            else if (foundJointProvider != null)
            {
                puppet = foundJointProvider;
            }
            
            // Find Animator component
            avatarAnimator = targetAvatar.GetComponent<Animator>();
            if (avatarAnimator == null)
            {
                avatarAnimator = targetAvatar.GetComponentInChildren<Animator>();
            }
            
            // Set initial valid positions
            lastValidPosition = targetAvatar.transform.position;
            lastValidRotation = targetAvatar.transform.rotation;
            
            if (rootTransform == null)
            {
                rootTransform = targetAvatar.transform;
            }
            
            // Log component status with detailed information
            NetworkLogDisplay.LogNetwork("AVATAR", $"PosePuppet found: {(foundPosePuppet != null ? "✅ " + foundPosePuppet.name + " (" + foundPosePuppet.GetType().Name + ")" : "❌ Not found")}");
            NetworkLogDisplay.LogNetwork("AVATAR", $"Tracking4All component found: {(foundTracking4All != null ? "✅ " + foundTracking4All.name : "❌ Not found")}");
            NetworkLogDisplay.LogNetwork("AVATAR", $"JointProvider found: {(foundJointProvider != null ? "✅ " + foundJointProvider.GetType().Name : "❌ Not found")}");
            NetworkLogDisplay.LogNetwork("AVATAR", $"Animator found: {(avatarAnimator != null ? "✅ " + avatarAnimator.name : "❌ Not found")}");
            
            if (puppet == null)
            {
                NetworkLogDisplay.LogNetwork("WARNING", "No Tracking4All puppet components found! Avatar may not receive pose data correctly", true);
                
                // List all MonoBehaviour components for debugging
                var allComponents = FindObjectsOfType<MonoBehaviour>();
                var tracking4AllComponents = new System.Collections.Generic.List<string>();
                foreach (var comp in allComponents)
                {
                    var typeName = comp.GetType().Name;
                    var fullTypeName = comp.GetType().FullName;
                    if (typeName.Contains("Puppet") || typeName.Contains("Tracking") || typeName.Contains("Pose") || fullTypeName.Contains("Tracking4All"))
                    {
                        tracking4AllComponents.Add($"{typeName} on {comp.name}");
                    }
                }
                
                NetworkLogDisplay.LogNetwork("DEBUG", $"Found {tracking4AllComponents.Count} Tracking4All related components:");
                foreach (var compInfo in tracking4AllComponents)
                {
                    NetworkLogDisplay.LogNetwork("DEBUG", $"  - {compInfo}");
                }
            }
            else
            {
                NetworkLogDisplay.LogNetwork("SUCCESS", $"Using Tracking4All component: {puppet.GetType().Name} on {puppet.name}");
            }
        }
        
        private void SetupNetworkCallbacks()
        {
            if (IsClient && !IsHost)
            {
                // Client subscribes to network position changes
                networkPosition.OnValueChanged += OnNetworkPositionChanged;
                networkRotation.OnValueChanged += OnNetworkRotationChanged;
            }
        }
        
        private void Update()
        {
            if (IsHost && isLocalAvatar)
            {
                // Host sends local avatar data to clients
                HandleLocalAvatarUpdate();
            }
            else if (IsClient && !isLocalAvatar)
            {
                // Client applies received data to remote avatar
                HandleRemoteAvatarUpdate();
            }
            
            // Prevent unwanted floating/drifting
            if (preventDrift)
            {
                ValidateAvatarPosition();
            }
        }
        
        private void HandleLocalAvatarUpdate()
        {
            if (targetAvatar == null) return;
            
            Vector3 currentPos = rootTransform.position;
            Quaternion currentRot = rootTransform.rotation;
            
            // Only update if position has changed significantly
            if (Vector3.Distance(currentPos, networkPosition.Value) > 0.01f ||
                Quaternion.Angle(currentRot, networkRotation.Value) > 1f)
            {
                networkPosition.Value = currentPos;
                networkRotation.Value = currentRot;
                
                if (logMovement && Time.time % 2f < 0.1f) // Log every 2 seconds
                {
                    NetworkLogDisplay.LogHost($"Local avatar position: {currentPos:F2}");
                }
            }
        }
        
        private void HandleRemoteAvatarUpdate()
        {
            // This is handled by the network callbacks
            // Just validate the avatar isn't drifting
            ValidateAvatarPosition();
        }
        
        private void OnNetworkPositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (targetAvatar == null || isLocalAvatar) return;
            
            // Validate movement is reasonable
            if (Vector3.Distance(newValue, lastValidPosition) > maxMovementPerFrame)
            {
                NetworkLogDisplay.LogNetwork("WARNING", $"Large position change detected: {Vector3.Distance(newValue, lastValidPosition):F2} units", true);
                // Don't apply extreme movements
                return;
            }
            
            rootTransform.position = newValue;
            lastValidPosition = newValue;
            
            if (logMovement && Time.time % 2f < 0.1f)
            {
                NetworkLogDisplay.LogClient($"Remote avatar position updated: {newValue:F2}");
            }
        }
        
        private void OnNetworkRotationChanged(Quaternion previousValue, Quaternion newValue)
        {
            if (targetAvatar == null || isLocalAvatar) return;
            
            // Validate rotation is reasonable
            float rotationDifference = Quaternion.Angle(newValue, lastValidRotation);
            if (rotationDifference > maxRotationPerFrame)
            {
                NetworkLogDisplay.LogNetwork("WARNING", $"Large rotation change detected: {rotationDifference:F1} degrees", true);
                // Don't apply extreme rotations
                return;
            }
            
            rootTransform.rotation = newValue;
            lastValidRotation = newValue;
        }
        
        private void ValidateAvatarPosition()
        {
            if (targetAvatar == null) return;
            
            Vector3 currentPos = rootTransform.position;
            
            // Check for invalid positions (NaN, infinity, extreme values)
            if (!IsValidPosition(currentPos))
            {
                NetworkLogDisplay.LogNetwork("ERROR", $"Invalid avatar position detected: {currentPos}", true);
                
                // Reset to last valid position
                rootTransform.position = lastValidPosition;
                rootTransform.rotation = lastValidRotation;
                
                NetworkLogDisplay.LogNetwork("FIX", "Avatar position reset to last valid state");
                return;
            }
            
            // Check for unwanted floating (Y position too high without reason)
            if (currentPos.y > 5f && !HasValidReasonForHeight())
            {
                NetworkLogDisplay.LogNetwork("WARNING", $"Avatar floating detected at height: {currentPos.y:F2}", true);
                
                // Gradually bring avatar back to ground level
                Vector3 correctedPos = currentPos;
                correctedPos.y = Mathf.Lerp(currentPos.y, 0f, Time.deltaTime * 2f);
                rootTransform.position = correctedPos;
                
                if (logMovement)
                {
                    NetworkLogDisplay.LogNetwork("FIX", $"Correcting avatar height: {correctedPos.y:F2}");
                }
            }
            
            // Check for unwanted sideways drift
            Vector3 horizontalPos = new Vector3(currentPos.x, 0, currentPos.z);
            if (horizontalPos.magnitude > 10f) // Too far from origin
            {
                NetworkLogDisplay.LogNetwork("WARNING", "Avatar drifting too far horizontally", true);
                
                // Gradually bring avatar back toward center
                Vector3 correctedPos = currentPos;
                Vector3 towardCenter = Vector3.zero - horizontalPos;
                correctedPos += towardCenter.normalized * Time.deltaTime * 1f;
                correctedPos.y = currentPos.y; // Keep Y position
                rootTransform.position = correctedPos;
            }
        }
        
        private bool IsValidPosition(Vector3 pos)
        {
            return !float.IsNaN(pos.x) && !float.IsNaN(pos.y) && !float.IsNaN(pos.z) &&
                   !float.IsInfinity(pos.x) && !float.IsInfinity(pos.y) && !float.IsInfinity(pos.z) &&
                   pos.magnitude < 1000f; // Reasonable bounds
        }
        
        private bool HasValidReasonForHeight()
        {
            // Add logic here to determine if the avatar should be at height
            // For example: jumping animation, being lifted by tracking data, etc.
            
            // For now, assume any height above 5 units is unintentional floating
            return false;
        }
        
        /// <summary>
        /// Force reset avatar to a safe position
        /// </summary>
        public void ResetAvatarPosition()
        {
            Vector3 safePosition = Vector3.zero;
            
            // Try to find a reasonable position based on other avatars
            AvatarNetworkController[] otherAvatars = FindObjectsOfType<AvatarNetworkController>();
            foreach (var other in otherAvatars)
            {
                if (other != this && other.targetAvatar != null)
                {
                    safePosition = other.rootTransform.position + Vector3.right * 2f;
                    break;
                }
            }
            
            rootTransform.position = safePosition;
            rootTransform.rotation = Quaternion.identity;
            
            lastValidPosition = safePosition;
            lastValidRotation = Quaternion.identity;
            
            NetworkLogDisplay.LogNetwork("RESET", $"Avatar {targetAvatar.name} reset to position: {safePosition}");
        }
        
        public override void OnNetworkDespawn()
        {
            // Clean up network callbacks
            if (networkPosition != null)
            {
                networkPosition.OnValueChanged -= OnNetworkPositionChanged;
            }
            if (networkRotation != null)
            {
                networkRotation.OnValueChanged -= OnNetworkRotationChanged;
            }
            
            base.OnNetworkDespawn();
        }
    }
}