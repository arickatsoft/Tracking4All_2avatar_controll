using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Initializes the multiplayer tracking scene with proper logging and network setup.
    /// Run this script to ensure all components are working correctly.
    /// </summary>
    public class MultiplayerSceneInitializer : MonoBehaviour
    {
        [Header("Scene Setup")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool showDetailedLogs = true;
        
        private void Start()
        {
            if (initializeOnStart)
            {
                InitializeScene();
            }
        }
        
        [ContextMenu("Initialize Multiplayer Scene")]
public void InitializeScene()
        {
            UnityEngine.Debug.Log("🚀 [MultiplayerSceneInitializer] Starting scene initialization...");
            
            // Step 1: Initialize NetworkLogDisplay first (for logging other steps)
            InitializeNetworkLogDisplay();
            
            // Step 2: Initialize AvatarTagManager
            InitializeAvatarTagManager();
            
            // Step 3: Log scene information
            LogSceneInfo();
            
            // Step 4: Find and log avatar information
            FindAndLogAvatars();
            
            // Step 5: Check network components
            CheckNetworkComponents();
            
            // Step 6: Check tracking components  
            CheckTrackingComponents();
            
            // Step 7: Final validation
            ValidateSceneSetup();
            
            NetworkLogDisplay.Log("🎉 Scene initialization complete!", Color.green);
            UnityEngine.Debug.Log("✅ [MultiplayerSceneInitializer] Scene initialization finished successfully!");
        }
        
        private void InitializeNetworkLogDisplay()
        {
            NetworkLogDisplay existingDisplay = FindObjectOfType<NetworkLogDisplay>();
            if (existingDisplay == null)
            {
                GameObject logDisplayObj = new GameObject("NetworkLogDisplay");
                NetworkLogDisplay logDisplay = logDisplayObj.AddComponent<NetworkLogDisplay>();
                DontDestroyOnLoad(logDisplayObj);
                
                UnityEngine.Debug.Log("✅ Created NetworkLogDisplay");
            }
            else
            {
                UnityEngine.Debug.Log("✅ NetworkLogDisplay already exists");
            }
            
            // Wait a frame for the display to initialize
            Invoke(nameof(ContinueInitialization), 0.1f);
        }

private void InitializeAvatarTagManager()
        {
            AvatarTagManager existingManager = FindObjectOfType<AvatarTagManager>();
            if (existingManager == null)
            {
                GameObject tagManagerObj = new GameObject("AvatarTagManager");
                AvatarTagManager tagManager = tagManagerObj.AddComponent<AvatarTagManager>();
                
                UnityEngine.Debug.Log("✅ Created AvatarTagManager");
            }
            else
            {
                UnityEngine.Debug.Log("✅ AvatarTagManager already exists");
            }
            
            // Wait a frame for the manager to initialize
            Invoke(nameof(ContinueWithTagManagerSetup), 0.1f);
        }
        
        private void ContinueWithTagManagerSetup()
        {
            NetworkLogDisplay.LogNetwork("AVATAR_TAGS", "AvatarTagManager initialized and ready");
        }

        
        private void ContinueInitialization()
        {
            NetworkLogDisplay.Log("🔧 MultiplayerSceneInitializer starting...", Color.cyan);
        }
        
        private void LogSceneInfo()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            int objectCount = FindObjectsOfType<GameObject>().Length;
            
            NetworkLogDisplay.LogNetwork("SCENE", $"Active scene: {sceneName}");
            NetworkLogDisplay.LogNetwork("SCENE", $"Total GameObjects: {objectCount}");
            
            if (showDetailedLogs)
            {
                NetworkLogDisplay.LogNetwork("SCENE", $"Scene path: {SceneManager.GetActiveScene().path}");
                NetworkLogDisplay.LogNetwork("SCENE", $"Build index: {SceneManager.GetActiveScene().buildIndex}");
            }
        }
        
        private void FindAndLogAvatars()
        {
            NetworkLogDisplay.LogNetwork("AVATARS", "Searching for avatars...");
            
            // Find Adam avatar
            GameObject adamAvatar = GameObject.FindGameObjectWithTag("Enemy");
            if (adamAvatar == null) adamAvatar = GameObject.Find("Adam_Big");
            
            if (adamAvatar != null)
            {
                NetworkLogDisplay.LogNetwork("AVATAR", $"✅ Adam found: {adamAvatar.name}");
                NetworkLogDisplay.LogNetwork("AVATAR", $"   Position: {adamAvatar.transform.position}");
                NetworkLogDisplay.LogNetwork("AVATAR", $"   Active: {adamAvatar.activeInHierarchy}");
                
                // Check for Animator
                Animator adamAnimator = adamAvatar.GetComponent<Animator>();
                NetworkLogDisplay.LogNetwork("AVATAR", $"   Animator: {(adamAnimator != null ? "✅" : "❌")}");
            }
            else
            {
                NetworkLogDisplay.LogNetwork("ERROR", "❌ Adam avatar not found!", true);
            }
            
            // Find Remy avatar
            GameObject remyAvatar = GameObject.FindGameObjectWithTag("Player");
            if (remyAvatar == null) remyAvatar = GameObject.Find("Remy");
            
            if (remyAvatar != null)
            {
                NetworkLogDisplay.LogNetwork("AVATAR", $"✅ Remy found: {remyAvatar.name}");
                NetworkLogDisplay.LogNetwork("AVATAR", $"   Position: {remyAvatar.transform.position}");
                NetworkLogDisplay.LogNetwork("AVATAR", $"   Active: {remyAvatar.activeInHierarchy}");
                
                // Check for Animator
                Animator remyAnimator = remyAvatar.GetComponent<Animator>();
                NetworkLogDisplay.LogNetwork("AVATAR", $"   Animator: {(remyAnimator != null ? "✅" : "❌")}");
                
                // Log the Remy floating issue status
                if (remyAvatar.transform.position.y > 1f)
                {
                    NetworkLogDisplay.LogNetwork("WARNING", $"⚠️ Remy appears to be floating at Y: {remyAvatar.transform.position.y:F2}", true);
                }
            }
            else
            {
                NetworkLogDisplay.LogNetwork("ERROR", "❌ Remy avatar not found!", true);
            }
        }
        
        private void CheckNetworkComponents()
        {
            NetworkLogDisplay.LogNetwork("NETWORK", "Checking network components...");
            
            // Check NetworkManager
            Unity.Netcode.NetworkManager networkManager = FindObjectOfType<Unity.Netcode.NetworkManager>();
            if (networkManager != null)
            {
                NetworkLogDisplay.LogNetwork("NETWORK", "✅ NetworkManager found");
                
                // Check transport
                var transport = networkManager.NetworkConfig?.NetworkTransport;
                if (transport != null)
                {
                    NetworkLogDisplay.LogNetwork("NETWORK", $"✅ Transport: {transport.GetType().Name}");
                }
                else
                {
                    NetworkLogDisplay.LogNetwork("WARNING", "❌ No transport configured", true);
                }
                
                // Check LoggingNetworkManager
                LoggingNetworkManager loggingManager = networkManager.GetComponent<LoggingNetworkManager>();
                NetworkLogDisplay.LogNetwork("NETWORK", $"LoggingNetworkManager: {(loggingManager != null ? "✅" : "❌")}");
            }
            else
            {
                NetworkLogDisplay.LogNetwork("ERROR", "❌ NetworkManager not found!", true);
            }
            
            // Check MultiplayerManager
            MultiplayerManager multiplayerManager = FindObjectOfType<MultiplayerManager>();
            NetworkLogDisplay.LogNetwork("NETWORK", $"MultiplayerManager: {(multiplayerManager != null ? "✅" : "❌")}");
            
            // Check NetworkPoseTracker
            NetworkPoseTracker poseTracker = FindObjectOfType<NetworkPoseTracker>();
            NetworkLogDisplay.LogNetwork("NETWORK", $"NetworkPoseTracker: {(poseTracker != null ? "✅" : "❌")}");
        }
        
private void CheckTrackingComponents()
        {
            NetworkLogDisplay.LogNetwork("TRACKING", "Checking pose tracking components...");
            
            // Check for MediaPipe components
            var poseGraphs = FindObjectsOfType<Mediapipe.Unity.Sample.PoseTracking.PoseTrackingGraph>();
            NetworkLogDisplay.LogNetwork("TRACKING", $"PoseTrackingGraphs found: {poseGraphs.Length}");
            
            // Check for cameras
            Camera[] cameras = FindObjectsOfType<Camera>();
            NetworkLogDisplay.LogNetwork("TRACKING", $"Cameras found: {cameras.Length}");
            
            foreach (Camera cam in cameras)
            {
                if (cam.name.Contains("Main"))
                {
                    NetworkLogDisplay.LogNetwork("TRACKING", $"✅ Main Camera: {cam.name} at {cam.transform.position}");
                }
            }
            
            // Check for Tracking4All components (using the correct types)
            var allComponents = FindObjectsOfType<MonoBehaviour>();
            int posePuppetCount = 0;
            int tracking4AllCount = 0;
            
            foreach (var comp in allComponents)
            {
                var typeName = comp.GetType().Name;
                var fullTypeName = comp.GetType().FullName;
                
                if (typeName == "PosePuppet" || fullTypeName.Contains("PosePuppet"))
                {
                    posePuppetCount++;
                    NetworkLogDisplay.LogNetwork("TRACKING", $"✅ PosePuppet: {comp.name}");
                }
                else if (typeName == "Tracking4All" || fullTypeName.Contains("Tracking4All.Tracking4All"))
                {
                    tracking4AllCount++;
                    NetworkLogDisplay.LogNetwork("TRACKING", $"✅ Tracking4All: {comp.name}");
                }
            }
            
            NetworkLogDisplay.LogNetwork("TRACKING", $"PosePuppet components found: {posePuppetCount}");
            NetworkLogDisplay.LogNetwork("TRACKING", $"Tracking4All components found: {tracking4AllCount}");
            
            if (posePuppetCount == 0 && tracking4AllCount == 0)
            {
                NetworkLogDisplay.LogNetwork("WARNING", "No Tracking4All puppet components found!", true);
            }
        }
        
        private void ValidateSceneSetup()
        {
            NetworkLogDisplay.LogNetwork("VALIDATION", "Running final validation...");
            
            bool isSetupValid = true;
            
            // Must-have components
            if (FindObjectOfType<NetworkLogDisplay>() == null)
            {
                NetworkLogDisplay.LogNetwork("ERROR", "❌ NetworkLogDisplay missing", true);
                isSetupValid = false;
            }
            
            if (FindObjectOfType<Unity.Netcode.NetworkManager>() == null)
            {
                NetworkLogDisplay.LogNetwork("ERROR", "❌ NetworkManager missing", true);
                isSetupValid = false;
            }
            
            if (GameObject.FindGameObjectWithTag("Player") == null && GameObject.Find("Remy") == null)
            {
                NetworkLogDisplay.LogNetwork("ERROR", "❌ Player avatar (Remy) missing", true);
                isSetupValid = false;
            }
            
            if (GameObject.FindGameObjectWithTag("Enemy") == null && GameObject.Find("Adam_Big") == null)
            {
                NetworkLogDisplay.LogNetwork("ERROR", "❌ Enemy avatar (Adam) missing", true);
                isSetupValid = false;
            }
            
            // Report validation result
            if (isSetupValid)
            {
                NetworkLogDisplay.LogNetwork("VALIDATION", "✅ Scene setup validation passed!");
            }
            else
            {
                NetworkLogDisplay.LogNetwork("VALIDATION", "❌ Scene setup validation failed!", true);
            }
        }
        
        /// <summary>
        /// Quick fix for common issues
        /// </summary>
        [ContextMenu("Apply Quick Fixes")]
        public void ApplyQuickFixes()
        {
            NetworkLogDisplay.LogNetwork("FIX", "Applying quick fixes...");
            
            // Fix Remy floating issue
            GameObject remyAvatar = GameObject.FindGameObjectWithTag("Player");
            if (remyAvatar == null) remyAvatar = GameObject.Find("Remy");
            
            if (remyAvatar != null && remyAvatar.transform.position.y > 1f)
            {
                Vector3 fixedPosition = remyAvatar.transform.position;
                fixedPosition.y = 0f; // Ground level
                remyAvatar.transform.position = fixedPosition;
                
                NetworkLogDisplay.LogNetwork("FIX", $"✅ Remy position corrected to ground level");
            }
            
            NetworkLogDisplay.LogNetwork("FIX", "Quick fixes applied");
        }
    }
}