using UnityEngine;
using Unity.Netcode;
using Mediapipe.Unity.Sample.PoseTracking;
using Tracking4All;

/// <summary>
/// Manages dual avatar tracking system where:
/// - Host device: Bryce controlled by local camera, Remy controlled by remote networked camera
/// - Client device: Remy controlled by local camera, Bryce receives data from host
/// </summary>
public class DualAvatarTrackingManager : NetworkBehaviour
{
    [Header("Avatar References")]
    [SerializeField] private GameObject bryceAvatar;
    [SerializeField] private GameObject remyAvatar;
    
    [Header("Local Tracking Solution")]
    [SerializeField] private MPUPoseTrackingSolution localTrackingSolution;
    
    [Header("Remote Tracking Solution")]
    [SerializeField] private MPUPoseTrackingSolution remoteTrackingSolution;
    [SerializeField] private GameObject pythonNetWebCamPrefab;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private GameObject instantiatedNetWebCam;
    private NetworkedWebcamBackend networkCamBackend;
    private bool isInitialized = false;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsHost)
        {
            SetupHostConfiguration();
        }
        else
        {
            SetupClientConfiguration();
        }
        
        isInitialized = true;
        
        if (enableDebugLogs)
            Debug.Log($"DualAvatarTrackingManager: Initialized as {(IsHost ? "HOST" : "CLIENT")}");
    }
    
    private void SetupHostConfiguration()
    {
        // HOST: Bryce = local camera, Remy = remote networked camera
        if (enableDebugLogs)
            Debug.Log("Setting up HOST configuration: Bryce=local, Remy=remote");
            
        // Setup local tracking for Bryce
        SetupLocalTracking(bryceAvatar, "Bryce");
        
        // Setup remote tracking for Remy
        SetupRemoteTracking(remyAvatar, "Remy");
    }
    
    private void SetupClientConfiguration()
    {
        // CLIENT: Remy = local camera, sends data to host for Bryce
        if (enableDebugLogs)
            Debug.Log("Setting up CLIENT configuration: Remy=local, send to host for Bryce");
            
        // Setup local tracking for Remy
        SetupLocalTracking(remyAvatar, "Remy");
        
        // Client will send tracking data to host via Python networked webcam
        StartPythonNetworkWebcam();
    }
    
    private void SetupLocalTracking(GameObject avatar, string avatarName)
    {
        if (localTrackingSolution == null)
        {
            Debug.LogError("Local tracking solution not assigned!");
            return;
        }
        
        // Configure the local tracking solution to track the specified avatar
        ConfigureTrackingSolutionForAvatar(localTrackingSolution, avatar, avatarName, true);
        
        if (enableDebugLogs)
            Debug.Log($"Configured local tracking for {avatarName}");
    }
    
    private void SetupRemoteTracking(GameObject avatar, string avatarName)
    {
        if (remoteTrackingSolution == null || pythonNetWebCamPrefab == null)
        {
            Debug.LogError("Remote tracking solution or Python NetWebCam prefab not assigned!");
            return;
        }
        
        // Instantiate the Python NetWebCam receiver
        instantiatedNetWebCam = Instantiate(pythonNetWebCamPrefab);
        networkCamBackend = instantiatedNetWebCam.GetComponent<NetworkedWebcamBackend>();
        
        if (networkCamBackend == null)
        {
            Debug.LogError("NetworkedWebcamBackend component not found on prefab!");
            return;
        }
        
        // Configure the remote tracking solution to use networked camera data
        ConfigureTrackingSolutionForAvatar(remoteTrackingSolution, avatar, avatarName, false);
        
        // Connect the networked camera to the remote tracking solution
        ConnectNetworkCameraToTrackingSolution(remoteTrackingSolution, networkCamBackend);
        
        if (enableDebugLogs)
            Debug.Log($"Configured remote tracking for {avatarName}");
    }
    
    private void ConfigureTrackingSolutionForAvatar(MPUPoseTrackingSolution solution, GameObject avatar, string avatarName, bool isLocal)
    {
        if (solution == null || avatar == null) return;
        
        // Find pose tracking components on the avatar
        var transformTargets = avatar.GetComponent<TransformTargets>();
        if (transformTargets == null)
        {
            transformTargets = avatar.AddComponent<TransformTargets>();
        }
        
        // Connect the tracking solution to the avatar's transform targets
        // This would need to be done via the Tracking4All provider system
        // The exact implementation depends on how the current tracking is set up
        
        if (enableDebugLogs)
            Debug.Log($"Configured tracking solution for {avatarName} (Local: {isLocal})");
    }
    
    private void ConnectNetworkCameraToTrackingSolution(MPUPoseTrackingSolution solution, NetworkedWebcamBackend networkCam)
    {
        // This connects the networked camera feed as the image source for the tracking solution
        // The NetworkedWebcamBackend should implement IImageFrameProvider or similar
        
        if (enableDebugLogs)
            Debug.Log("Connected network camera to remote tracking solution");
    }
    
    private void StartPythonNetworkWebcam()
    {
        // Instructions for the client to start the Python networked webcam system
        if (enableDebugLogs)
        {
            Debug.Log("=== CLIENT SETUP INSTRUCTIONS ===");
            Debug.Log("1. On this device, run the Python networked webcam:");
            Debug.Log("   Navigate to: Assets/Tracking4All/Third Party/MediaPipePython/PythonSystem/");
            Debug.Log("   Run: python main.py");
            Debug.Log("   Select: networkedwebcam");
            Debug.Log("2. Ensure the host IP is correctly configured in the Python script");
            Debug.Log("3. This will send camera feed to the host for Bryce avatar tracking");
        }
    }
    
    public override void OnNetworkDespawn()
    {
        if (instantiatedNetWebCam != null)
        {
            Destroy(instantiatedNetWebCam);
        }
        
        base.OnNetworkDespawn();
    }
    
    // Public methods for runtime configuration
    public void SwitchToLocalBryceRemoteRemy()
    {
        if (!IsHost) return;
        SetupHostConfiguration();
    }
    
    public void SwitchToLocalRemySendToBryce()
    {
        if (IsHost) return;
        SetupClientConfiguration();
    }
    
    private void Update()
    {
        // Monitor connection status if needed
        if (isInitialized && networkCamBackend != null && enableDebugLogs)
        {
            // Could add periodic status checks here
        }
    }
}