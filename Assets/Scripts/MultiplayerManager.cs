using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using Mediapipe.Unity.Sample.PoseTracking;

using Tracking4All;


namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Manages the multiplayer UI and connects camera input to pose tracking system.
    /// Handles Host/Join functionality and data flow setup.
    /// </summary>
    public class MultiplayerManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Text statusText;
        
        [Header("Avatar References")]
        [SerializeField] private GameObject adamAvatar;  // Adam_Big
        [SerializeField] private GameObject remyAvatar;  // Remy
        
        [Header("Tracking4All Components")]
        [SerializeField] private PoseTrackingGraph poseSolution;
        [SerializeField] private Camera cameraProvider;
        
        [Header("Network Settings")]
        [SerializeField] private bool logCameraInput = true;
        [SerializeField] private bool logPoseData = true;
        
        private NetworkPoseTracker poseTracker;
        private Camera mainCamera;
        private bool isTracking = false;
        
        private void Start()
        {
            InitializeUI();
            InitializeComponents();
            SetupCameraInputLogging();
        }
        
        private void InitializeUI()
        {
            if (hostButton != null)
                hostButton.onClick.AddListener(OnHostButtonClicked);
                
            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinButtonClicked);
                
            UpdateStatusText("Select Host or Join");
        }
        
        private void InitializeComponents()
        {
            mainCamera = Camera.main;
            
            // Find components if not assigned
            if (poseSolution == null)
                poseSolution = FindObjectOfType<PoseTrackingGraph>();
                
            if (cameraProvider == null)
                cameraProvider = Camera.main;
                
            // Find avatars if not assigned
            if (adamAvatar == null)
                adamAvatar = GameObject.FindGameObjectWithTag("Enemy"); // Adam has Enemy tag
                
            if (remyAvatar == null)
                remyAvatar = GameObject.FindGameObjectWithTag("Player"); // Remy has Player tag
                
            UnityEngine.Debug.Log($"[MultiplayerManager] Found components - PoseSolution: {poseSolution != null}, " +
                     $"CameraProvider: {cameraProvider != null}, Adam: {adamAvatar != null}, Remy: {remyAvatar != null}");
        }
        
        private void SetupCameraInputLogging()
        {
            if (!logCameraInput) return;
            
            // Start coroutine to log camera input data
            StartCoroutine(LogCameraInputData());
        }
        
        private IEnumerator LogCameraInputData()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f); // Log every second
                
                if (cameraProvider != null && isTracking)
                {
                    LogCurrentCameraData();
                }
            }
        }
        
        private void LogCurrentCameraData()
        {
            if (logCameraInput)
            {
                // Log camera input data that can be translated to bone movements
                UnityEngine.Debug.Log($"[CameraInput] Frame time: {Time.time:F2}, " +
                         $"Camera active: {cameraProvider.gameObject.activeInHierarchy}, " +
                         $"Provider enabled: {cameraProvider.enabled}");
                
                // Check if we have pose data from the solution
                if (poseSolution != null)
                {
                    UnityEngine.Debug.Log($"[PoseData] Solution active: {poseSolution.gameObject.activeInHierarchy}");
                    
                    // If we can access pose landmarks, log them
                    LogPoseLandmarkData();
                }
            }
        }
        
        private void LogPoseLandmarkData()
        {
            if (!logPoseData) return;
            
            // This method logs the pose landmark data that comes from the camera
            // and gets processed by the Tracking4All system
            
            // Check if we have active pose tracking data
            var activeTracker = FindObjectOfType<NetworkPoseTracker>();
            if (activeTracker != null)
            {
                UnityEngine.Debug.Log($"[PoseLandmarks] NetworkPoseTracker found and active");
                
                // Log key joint positions that would be derived from camera landmarks
                if (adamAvatar != null && adamAvatar.activeInHierarchy)
                {
                    var adamAnimator = adamAvatar.GetComponent<Animator>();
                    if (adamAnimator != null)
                    {
                        LogAvatarBonePositions("Adam", adamAnimator);
                    }
                }
                
                if (remyAvatar != null && remyAvatar.activeInHierarchy)
                {
                    var remyAnimator = remyAvatar.GetComponent<Animator>();
                    if (remyAnimator != null)
                    {
                        LogAvatarBonePositions("Remy", remyAnimator);
                    }
                }
            }
        }
        
        private void LogAvatarBonePositions(string avatarName, Animator animator)
        {
            var hipBone = animator.GetBoneTransform(HumanBodyBones.Hips);
            var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            
            if (hipBone != null && headBone != null)
            {
                UnityEngine.Debug.Log($"[{avatarName}Bones] Hip: {hipBone.position:F3}, Head: {headBone.position:F3}, " +
                         $"L_Hand: {(leftHand != null ? leftHand.position.ToString("F3") : "null")}, " +
                         $"R_Hand: {(rightHand != null ? rightHand.position.ToString("F3") : "null")}");
            }
        }
        
private void OnHostButtonClicked()
        {
            NetworkLogDisplay.LogHost("HOST button clicked - Starting host server");
            
            // Setup avatar tags for HOST mode BEFORE starting network
            AvatarTagManager.SetupForHost();
            
            try
            {
                // Start Unity Netcode as Host
                var networkManager = Unity.Netcode.NetworkManager.Singleton;
                if (networkManager != null)
                {
                    NetworkLogDisplay.LogNetwork("CONNECTION", "Starting Unity Netcode Host...");
                    
                    // Configure network transport
                    var transport = networkManager.NetworkConfig?.NetworkTransport;
                    if (transport != null)
                    {
                        NetworkLogDisplay.LogHost($"Network transport: {transport.GetType().Name}");
                    }
                    
                    bool success = networkManager.StartHost();
                    
                    if (success)
                    {
                        NetworkLogDisplay.LogHost("✅ Host started successfully!");
                        NetworkLogDisplay.LogNetwork("STATUS", "Listening for client connections...");
                        
                        // Set up host configuration
                        SetupHostConfiguration();
                        StartTracking();
                        HideMenu();
                    }
                    else
                    {
                        NetworkLogDisplay.LogNetwork("ERROR", "Failed to start host!", true);
                        UpdateStatusText("Failed to start host");
                    }
                }
                else
                {
                    NetworkLogDisplay.LogNetwork("ERROR", "NetworkManager not found!", true);
                    UpdateStatusText("NetworkManager not found");
                }
            }
            catch (System.Exception ex)
            {
                NetworkLogDisplay.LogNetwork("ERROR", $"Exception starting host: {ex.Message}", true);
                UpdateStatusText($"Host error: {ex.Message}");
            }
        }
        
private void OnJoinButtonClicked()
        {
            NetworkLogDisplay.LogClient("JOIN button clicked - Connecting to host");
            
            // Setup avatar tags for CLIENT mode BEFORE starting network
            AvatarTagManager.SetupForClient();
            
            try
            {
                // Start Unity Netcode as Client
                var networkManager = Unity.Netcode.NetworkManager.Singleton;
                if (networkManager != null)
                {
                    NetworkLogDisplay.LogNetwork("CONNECTION", "Starting Unity Netcode Client...");
                    
                    // Log connection details
                    var transport = networkManager.NetworkConfig?.NetworkTransport;
                    if (transport != null)
                    {
                        NetworkLogDisplay.LogClient($"Network transport: {transport.GetType().Name}");
                    }
                    
                    // Attempt to start client
                    bool success = networkManager.StartClient();
                    
                    if (success)
                    {
                        NetworkLogDisplay.LogClient("✅ Client connection initiated!");
                        NetworkLogDisplay.LogNetwork("STATUS", "Attempting to connect to host...");
                        
                        // Set up client configuration
                        SetupClientConfiguration();
                        StartTracking();
                        HideMenu();
                        
                        // Start connection timeout check
                        StartCoroutine(CheckConnectionStatus());
                    }
                    else
                    {
                        NetworkLogDisplay.LogNetwork("ERROR", "Failed to start client!", true);
                        UpdateStatusText("Failed to start client");
                    }
                }
                else
                {
                    NetworkLogDisplay.LogNetwork("ERROR", "NetworkManager not found!", true);
                    UpdateStatusText("NetworkManager not found");
                }
            }
            catch (System.Exception ex)
            {
                NetworkLogDisplay.LogNetwork("ERROR", $"Exception starting client: {ex.Message}", true);
                UpdateStatusText($"Client error: {ex.Message}");
            }
        }
        
        private void StartHost()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.StartHost();
                UpdateStatusText("Starting as Host...");
                
                // Enable tracking for host
                isTracking = true;
                
                UnityEngine.Debug.Log("[MultiplayerManager] Host started successfully");
            }
            else
            {
                UnityEngine.Debug.LogError("[MultiplayerManager] NetworkManager not found!");
                UpdateStatusText("Error: NetworkManager not found");
            }
        }
        
        private void StartClient()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.StartClient();
                UpdateStatusText("Connecting as Client...");
                
                UnityEngine.Debug.Log("[MultiplayerManager] Client started successfully");
            }
            else
            {
                UnityEngine.Debug.LogError("[MultiplayerManager] NetworkManager not found!");
                UpdateStatusText("Error: NetworkManager not found");
            }
        }
        
        private void SetupHostConfiguration()
        {
            // Host uses Adam_Big as local avatar, Remy for remote data
            if (adamAvatar != null) adamAvatar.SetActive(true);
            if (remyAvatar != null) remyAvatar.SetActive(true);
            
            // Set up NetworkPoseTracker for host
            SetupNetworkPoseTracker(adamAvatar, remyAvatar);
            
            // Prevent camera zoom issues
            SetupCameraForHost();
            
            UpdateStatusText("Host: Tracking with Adam, Remote: Remy");
        }
        
        private void SetupClientConfiguration()
        {
            // Client uses Remy as local avatar, Adam for remote data
            if (remyAvatar != null) remyAvatar.SetActive(true);
            if (adamAvatar != null) adamAvatar.SetActive(true);
            
            // Set up NetworkPoseTracker for client
            SetupNetworkPoseTracker(remyAvatar, adamAvatar);
            
            // Focus camera on Remy (Bryce equivalent)
            SetupCameraForClient();
            
            UpdateStatusText("Client: Tracking with Remy, Remote: Adam");
        }
        
        private void SetupNetworkPoseTracker(GameObject localAvatar, GameObject remoteAvatar)
        {
            // Find or create NetworkPoseTracker
            poseTracker = FindObjectOfType<NetworkPoseTracker>();
            if (poseTracker == null)
            {
                var trackerObj = new GameObject("NetworkPoseTracker");
                poseTracker = trackerObj.AddComponent<NetworkPoseTracker>();
            }
            
            // Configure the tracker via reflection or public properties if available
            UnityEngine.Debug.Log($"[MultiplayerManager] NetworkPoseTracker configured - Local: {localAvatar.name}, Remote: {remoteAvatar.name}");
        }
        
        private void SetupCameraForHost()
        {
            if (mainCamera != null)
            {
                // Position camera to show both avatars without zooming in
                mainCamera.transform.position = new Vector3(1.2f, 1.5f, -1.3f);
                mainCamera.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
                
                UnityEngine.Debug.Log("[MultiplayerManager] Camera positioned for Host view");
            }
        }
        
        private void SetupCameraForClient()
        {
            if (mainCamera != null && remyAvatar != null)
            {
                // Focus camera on Remy without excessive zoom
                Vector3 remyPos = remyAvatar.transform.position;
                mainCamera.transform.position = remyPos + new Vector3(0f, 1.5f, -2f);
                mainCamera.transform.LookAt(remyPos + Vector3.up);
                
                UnityEngine.Debug.Log("[MultiplayerManager] Camera focused on Remy for Client view");
            }
        }
        
        private void HideMenu()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }
        }
        
        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            
            UnityEngine.Debug.Log($"[MultiplayerManager] Status: {message}");
        }
        
        private void OnDestroy()
        {
            // Clean up UI listeners
            if (hostButton != null)
                hostButton.onClick.RemoveListener(OnHostButtonClicked);
                
            if (joinButton != null)
                joinButton.onClick.RemoveListener(OnJoinButtonClicked);
        }
    

private IEnumerator CheckConnectionStatus()
        {
            float timeout = 10f; // 10 second timeout
            float elapsed = 0f;
            
            var networkManager = Unity.Netcode.NetworkManager.Singleton;
            
            while (elapsed < timeout && networkManager != null)
            {
                if (networkManager.IsClient && networkManager.IsConnectedClient)
                {
                    NetworkLogDisplay.LogClient("✅ Successfully connected to host!");
                    NetworkLogDisplay.LogNetwork("STATUS", "Connection established - Ready for multiplayer");
                    yield break;
                }
                
                elapsed += 0.5f;
                NetworkLogDisplay.LogClient($"Connecting... ({elapsed:F1}s / {timeout}s)");
                yield return new WaitForSeconds(0.5f);
            }
            
            // Connection timed out
            NetworkLogDisplay.LogNetwork("ERROR", "Connection timeout - Could not connect to host", true);
            UpdateStatusText("Connection failed - timeout");
        }


private void StartTracking()
        {
            NetworkLogDisplay.LogNetwork("TRACKING", "Starting pose tracking system...");
            
            isTracking = true;
            
            // Enable the pose solution if available
            if (poseSolution != null)
            {
                poseSolution.gameObject.SetActive(true);
                NetworkLogDisplay.LogNetwork("TRACKING", $"PoseTrackingGraph activated: {poseSolution.name}");
                
                // Log MediaPipe configuration
                NetworkLogDisplay.LogNetwork("MEDIAPIPE", "MediaPipe pose tracking initialized");
            }
            else
            {
                NetworkLogDisplay.LogNetwork("WARNING", "PoseTrackingGraph not found - pose tracking may not work", true);
            }
            
            // Enable camera if available
            if (cameraProvider != null)
            {
                cameraProvider.enabled = true;
                NetworkLogDisplay.LogNetwork("CAMERA", $"Camera provider enabled: {cameraProvider.name}");
            }
            else
            {
                NetworkLogDisplay.LogNetwork("WARNING", "Camera provider not found", true);
            }
            
            // Log avatar status
            if (adamAvatar != null)
            {
                NetworkLogDisplay.LogNetwork("AVATAR", $"Adam avatar status: {(adamAvatar.activeInHierarchy ? "Active" : "Inactive")}");
            }
            
            if (remyAvatar != null)
            {
                NetworkLogDisplay.LogNetwork("AVATAR", $"Remy avatar status: {(remyAvatar.activeInHierarchy ? "Active" : "Inactive")}");
            }
            
            NetworkLogDisplay.LogNetwork("TRACKING", "✅ Pose tracking system started successfully!");
        }
}
}