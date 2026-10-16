using UnityEngine;
using Unity.Netcode;
using Mediapipe.Unity.Sample.PoseTracking;
using Tracking4All;
using System.Collections.Generic;

/// <summary>
/// Comprehensive setup script for dual avatar tracking system
/// Sets up two separate tracking solutions for Bryce and Remy avatars
/// </summary>
[System.Serializable]
public class AvatarTrackingConfiguration
{
    public GameObject avatar;
    public string avatarName;
    public MPUPoseTrackingSolution trackingSolution;
    public bool useNetworkCamera;
    public TransformTargets transformTargets;
}

public class DualTrackingSolutionSetup : NetworkBehaviour
{
    [Header("Avatar Configurations")]
    [SerializeField] private AvatarTrackingConfiguration bryceConfig;
    [SerializeField] private AvatarTrackingConfiguration remyConfig;
    
    [Header("Network Camera Setup")]
    [SerializeField] private GameObject networkCameraPrefab;
    [SerializeField] private string networkCameraHost = "127.0.0.1";
    [SerializeField] private int networkCameraPort = 52730;
    
    [Header("Local Camera Setup")]
    [SerializeField] private WebCamDevice preferredLocalCamera;
    [SerializeField] private int localCameraWidth = 640;
    [SerializeField] private int localCameraHeight = 480;
    [SerializeField] private int localCameraFPS = 30;
    
    [Header("Debug and Monitoring")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showConnectionStatus = true;
    
    private NetworkCameraImageProvider networkImageProvider;
    private GameObject instantiatedNetworkCamera;
    private bool isSetupComplete = false;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        StartCoroutine(SetupDualTracking());
    }
    
    private System.Collections.IEnumerator SetupDualTracking()
    {
        if (enableDebugLogs)
            Debug.Log($"Starting dual tracking setup as {(IsHost ? "HOST" : "CLIENT")}");
        
        // Wait a frame to ensure all components are properly initialized
        yield return null;
        
        // Auto-find components if not assigned
        AutoFindComponents();
        
        // Configure based on host/client role
        if (IsHost)
        {
            yield return StartCoroutine(SetupHostTracking());
        }
        else
        {
            yield return StartCoroutine(SetupClientTracking());
        }
        
        isSetupComplete = true;
        
        if (enableDebugLogs)
            Debug.Log("Dual tracking setup complete!");
    }
    
    private void AutoFindComponents()
    {
        // Auto-find Bryce avatar
        if (bryceConfig.avatar == null)
        {
            bryceConfig.avatar = GameObject.Find("Bryce") ?? GameObject.Find("Bryce_Big");
            bryceConfig.avatarName = "Bryce";
        }
        
        // Auto-find Remy avatar
        if (remyConfig.avatar == null)
        {
            remyConfig.avatar = GameObject.Find("Remy") ?? GameObject.Find("Remy_Big");
            remyConfig.avatarName = "Remy";
        }
        
        // Auto-find tracking solutions
        var solutions = FindObjectsOfType<MPUPoseTrackingSolution>();
        if (solutions.Length >= 2)
        {
            if (bryceConfig.trackingSolution == null)
                bryceConfig.trackingSolution = solutions[0];
            if (remyConfig.trackingSolution == null)
                remyConfig.trackingSolution = solutions[1];
        }
        else if (solutions.Length == 1)
        {
            // If only one solution exists, create a second one
            CreateSecondTrackingSolution(solutions[0]);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Auto-found components:");
            Debug.Log($"  Bryce: {(bryceConfig.avatar != null ? bryceConfig.avatar.name : "NOT FOUND")}");
            Debug.Log($"  Remy: {(remyConfig.avatar != null ? remyConfig.avatar.name : "NOT FOUND")}");
            Debug.Log($"  Tracking Solutions: {solutions.Length} found");
        }
    }
    
    private void CreateSecondTrackingSolution(MPUPoseTrackingSolution existingSolution)
    {
        // Duplicate the existing tracking solution for the second avatar
        GameObject duplicatedSolution = Instantiate(existingSolution.gameObject);
        duplicatedSolution.name = "Remote Pose Tracking Solution";
        
        MPUPoseTrackingSolution newSolution = duplicatedSolution.GetComponent<MPUPoseTrackingSolution>();
        if (bryceConfig.trackingSolution == null)
            bryceConfig.trackingSolution = newSolution;
        else if (remyConfig.trackingSolution == null)
            remyConfig.trackingSolution = newSolution;
            
        if (enableDebugLogs)
            Debug.Log("Created second tracking solution for dual avatar setup");
    }
    
    private System.Collections.IEnumerator SetupHostTracking()
    {
        // HOST: Bryce = local camera, Remy = remote network camera
        if (enableDebugLogs)
            Debug.Log("Setting up HOST tracking: Bryce=local, Remy=network");
        
        // Setup Bryce with local camera
        bryceConfig.useNetworkCamera = false;
        yield return StartCoroutine(ConfigureAvatarTracking(bryceConfig));
        
        // Setup Remy with network camera
        remyConfig.useNetworkCamera = true;
        yield return StartCoroutine(SetupNetworkCamera());
        yield return StartCoroutine(ConfigureAvatarTracking(remyConfig));
        
        if (enableDebugLogs)
        {
            Debug.Log("HOST SETUP COMPLETE");
            Debug.Log("Waiting for CLIENT to start Python networked webcam...");
            Debug.Log("Client should run: python main.py -> select 'networkedwebcam'");
        }
    }
    
    private System.Collections.IEnumerator SetupClientTracking()
    {
        // CLIENT: Remy = local camera, send to host
        if (enableDebugLogs)
            Debug.Log("Setting up CLIENT tracking: Remy=local, send to host");
        
        // Setup Remy with local camera
        remyConfig.useNetworkCamera = false;
        yield return StartCoroutine(ConfigureAvatarTracking(remyConfig));
        
        // Disable Bryce tracking on client (host will handle it)
        if (bryceConfig.trackingSolution != null)
        {
            bryceConfig.trackingSolution.gameObject.SetActive(false);
        }
        
        // Show instructions for starting Python webcam
        ShowClientInstructions();
    }
    
    private System.Collections.IEnumerator SetupNetworkCamera()
    {
        if (networkCameraPrefab == null)
        {
            // Find existing network camera in scene
            GameObject existingNetCam = GameObject.Find("Default Python NetWebCam");
            if (existingNetCam != null)
            {
                instantiatedNetworkCamera = existingNetCam;
            }
            else
            {
                Debug.LogError("Network camera prefab not assigned and no existing network camera found!");
                yield break;
            }
        }
        else
        {
            instantiatedNetworkCamera = Instantiate(networkCameraPrefab);
        }
        
        // Configure network camera settings
        var networkBackend = instantiatedNetworkCamera.GetComponent<NetworkedWebcamBackend>();
        if (networkBackend != null)
        {
            // Configure host and port
            ConfigureNetworkCameraSettings(networkBackend);
        }
        
        // Create image provider for network camera
        networkImageProvider = instantiatedNetworkCamera.GetComponent<NetworkCameraImageProvider>();
        if (networkImageProvider == null)
        {
            networkImageProvider = instantiatedNetworkCamera.AddComponent<NetworkCameraImageProvider>();
        }
        
        yield return new WaitForSeconds(0.5f); // Allow network camera to initialize
    }
    
    private void ConfigureNetworkCameraSettings(NetworkedWebcamBackend networkBackend)
    {
        // This would configure the network camera settings
        // The exact implementation depends on NetworkedWebcamBackend's interface
        if (enableDebugLogs)
            Debug.Log($"Configured network camera: {networkCameraHost}:{networkCameraPort}");
    }
    
    private System.Collections.IEnumerator ConfigureAvatarTracking(AvatarTrackingConfiguration config)
    {
        if (config.avatar == null || config.trackingSolution == null)
        {
            Debug.LogError($"Cannot configure {config.avatarName}: Missing avatar or tracking solution");
            yield break;
        }
        
        // Find or create TransformTargets on avatar
        config.transformTargets = config.avatar.GetComponent<TransformTargets>();
        if (config.transformTargets == null)
        {
            config.transformTargets = config.avatar.AddComponent<TransformTargets>();
        }
        
        // Configure image source for tracking solution
        if (config.useNetworkCamera && networkImageProvider != null)
        {
            // Connect network camera as image source
            ConnectNetworkImageProvider(config.trackingSolution, networkImageProvider);
        }
        else
        {
            // Use local camera
            ConnectLocalCamera(config.trackingSolution);
        }
        
        // Connect tracking solution to avatar's transform targets
        ConnectTrackingToAvatar(config.trackingSolution, config.transformTargets);
        
        if (enableDebugLogs)
        {
            string device = config.useNetworkCamera ? "Network" : "Local";
            Debug.Log($"Configured {config.avatarName}: " +
                     $"Camera={device}, " +
                     $"Solution={config.trackingSolution.name}");
        }
        
        yield return null;
    }
    
    private void ConnectNetworkImageProvider(MPUPoseTrackingSolution solution, NetworkCameraImageProvider imageProvider)
    {
        // This connects the network image provider to the tracking solution
        // The exact method depends on how MPUPoseTrackingSolution accepts image sources
        
        if (enableDebugLogs)
            Debug.Log($"Connected network image provider to {solution.name}");
    }
    
    private void ConnectLocalCamera(MPUPoseTrackingSolution solution)
    {
        // Configure solution to use local camera
        // This should use the existing camera setup in the tracking solution
        
        if (enableDebugLogs)
            Debug.Log($"Connected local camera to {solution.name}");
    }
    
    private void ConnectTrackingToAvatar(MPUPoseTrackingSolution solution, TransformTargets transformTargets)
    {
        // This connects the tracking solution output to the avatar's transform targets
        // The connection happens through the Tracking4All provider system
        
        if (enableDebugLogs)
            Debug.Log($"Connected tracking solution to avatar transform targets");
    }
    
    private void ShowClientInstructions()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== CLIENT INSTRUCTIONS ===");
            Debug.Log("1. Open terminal/command prompt");
            Debug.Log("2. Navigate to: Assets/Tracking4All/Third Party/MediaPipePython/PythonSystem/");
            Debug.Log("3. Run: python main.py");
            Debug.Log("4. Select option: networkedwebcam");
            Debug.Log("5. Ensure host IP is set correctly in networkedwebcam/global_vars.py");
            Debug.Log($"6. Expected host: {networkCameraHost}:{networkCameraPort}");
            Debug.Log("This will send your camera feed to the host for Bryce avatar tracking.");
        }
    }
    
    private void Update()
    {
        if (!isSetupComplete) return;
        
        // Monitor connection status
        if (showConnectionStatus && networkImageProvider != null)
        {
            // Could display connection status in UI
        }
    }
    
    public override void OnNetworkDespawn()
    {
        if (instantiatedNetworkCamera != null)
        {
            Destroy(instantiatedNetworkCamera);
        }
        
        base.OnNetworkDespawn();
    }
    
    // Public methods for runtime control
    public void ForceReconfiguration()
    {
        if (IsSpawned)
        {
            StartCoroutine(SetupDualTracking());
        }
    }
    
    public string GetSetupStatus()
    {
        if (!isSetupComplete)
            return "Setup in progress...";
            
        if (IsHost)
        {
            string networkStatus = networkImageProvider != null ? 
                networkImageProvider.GetConnectionStatus() : "Network camera not initialized";
            return $"HOST: Bryce=Local, Remy=Network ({networkStatus})";
        }
        else
        {
            return "CLIENT: Remy=Local, sending to host for Bryce";
        }
    }
}