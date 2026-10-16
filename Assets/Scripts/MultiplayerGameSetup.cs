using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Main multiplayer game setup - replaces single player setup
    /// This script should be added to a GameObject in the scene to initialize multiplayer
    /// It prevents camera issues and ensures clean multiplayer initialization
    /// </summary>
    [DefaultExecutionOrder(-50)] // Run after system disable but before UI creation
    public class MultiplayerGameSetup : MonoBehaviour
    {
        [Header("Setup Options")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool preserveExistingAvatars = true;
        [SerializeField] private bool preventCameraMovement = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        private MultiplayerGameUI gameUI;
        private bool isSetupComplete = false;
        
        void Start()
        {
            if (autoInitialize)
            {
                InitializeMultiplayerGame();
            }
        }
        
        public void InitializeMultiplayerGame()
        {
            if (isSetupComplete)
            {
                LogDebug("Multiplayer already setup, skipping...");
                return;
            }
            
            LogDebug("=== Initializing Multiplayer Game ===");
            
            // Ensure old systems are disabled
            EnsureOldSystemsDisabled();
            
            // Setup camera position prevention
            if (preventCameraMovement)
            {
                PreventCameraIssues();
            }
            
            // Verify avatars are ready
            VerifyAvatarsReady();
            
            // Create multiplayer UI
            CreateMultiplayerUI();
            
            isSetupComplete = true;
            LogDebug("=== Multiplayer Initialization Complete ===");
        }
        
        void EnsureOldSystemsDisabled()
        {
            // Make sure DisableOldSinglePlayerSystems ran
            DisableOldSinglePlayerSystems disabler = FindObjectOfType<DisableOldSinglePlayerSystems>();
            if (disabler == null)
            {
                // Create it if it doesn't exist
                GameObject disablerGO = new GameObject("DisableOldSinglePlayerSystems");
                disabler = disablerGO.AddComponent<DisableOldSinglePlayerSystems>();
                LogDebug("Created DisableOldSinglePlayerSystems");
            }
        }        
        void PreventCameraIssues()
        {
            // Store the initial camera position to prevent unwanted changes
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 initialPosition = mainCamera.transform.position;
                Quaternion initialRotation = mainCamera.transform.rotation;
                
                LogDebug($"Stored initial camera position: {initialPosition}, rotation: {initialRotation}");
                
                // Add a script to monitor and log camera changes but don't prevent them
                CameraPositionMonitor monitor = mainCamera.gameObject.GetComponent<CameraPositionMonitor>();
                if (monitor == null)
                {
                    monitor = mainCamera.gameObject.AddComponent<CameraPositionMonitor>();
                    monitor.Initialize(initialPosition, initialRotation);
                }
            }
        }
        
        void VerifyAvatarsReady()
        {
            GameObject remy = GameObject.Find("Remy");
            GameObject bryce = GameObject.Find("Bryce");
            
            if (remy == null)
            {
                LogDebug("WARNING: Remy avatar not found in scene!");
            }
            else
            {
                LogDebug($"Found Remy avatar: {remy.name} (Active: {remy.activeInHierarchy})");
                
                // Ensure both avatars are active initially
                if (preserveExistingAvatars)
                {
                    remy.SetActive(true);
                }
            }
            
            if (bryce == null)
            {
                LogDebug("WARNING: Bryce avatar not found in scene!");
            }
            else
            {
                LogDebug($"Found Bryce avatar: {bryce.name} (Active: {bryce.activeInHierarchy})");
                
                // Ensure both avatars are active initially  
                if (preserveExistingAvatars)
                {
                    bryce.SetActive(true);
                }
            }
        }
        
        void CreateMultiplayerUI()
        {
            // Find existing MultiplayerGameUI or create new one
            gameUI = FindObjectOfType<MultiplayerGameUI>();
            if (gameUI == null)
            {
                GameObject uiGO = new GameObject("MultiplayerGameUI");
                gameUI = uiGO.AddComponent<MultiplayerGameUI>();
                LogDebug("Created MultiplayerGameUI");
            }
            else
            {
                LogDebug("Found existing MultiplayerGameUI");
            }
        }
        
        void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                UnityEngine.Debug.Log($"[MultiplayerSetup] {message}");
            }
        }
        
        // Public methods for manual control
        public void ShowMultiplayerUI()
        {
            if (gameUI != null)
            {
                gameUI.ShowGameModeSelection();
            }
        }
        
        public void HideMultiplayerUI()
        {
            if (gameUI != null)
            {
                gameUI.HideGameModeSelection();
            }
        }
    }
    
    /// <summary>
    /// Monitors camera position changes for debugging camera issues
    /// Helps identify what's causing unwanted camera movements
    /// </summary>
    public class CameraPositionMonitor : MonoBehaviour
    {
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        
        public void Initialize(Vector3 startPos, Quaternion startRot)
        {
            initialPosition = startPos;
            initialRotation = startRot;
            lastPosition = startPos;
            lastRotation = startRot;
        }
        
        void Update()
        {
            Vector3 currentPos = transform.position;
            Quaternion currentRot = transform.rotation;
            
            // Log significant position changes
            if (Vector3.Distance(currentPos, lastPosition) > 0.1f ||
                Quaternion.Angle(currentRot, lastRotation) > 1f)
            {
                UnityEngine.Debug.Log($"[CameraMonitor] Position changed from {lastPosition} to {currentPos}");
                UnityEngine.Debug.Log($"[CameraMonitor] Rotation changed from {lastRotation.eulerAngles} to {currentRot.eulerAngles}");
                
                lastPosition = currentPos;
                lastRotation = currentRot;
            }
        }
    }
}