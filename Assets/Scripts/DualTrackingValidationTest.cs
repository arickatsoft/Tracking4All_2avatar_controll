using UnityEngine;
using Unity.Netcode;
using Mediapipe.Unity.Sample.PoseTracking;
using Tracking4All;

/// <summary>
/// Validation and testing script for dual avatar tracking system
/// Monitors system status and provides diagnostic information
/// </summary>
public class DualTrackingValidationTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runContinuousTests = true;
    [SerializeField] private float testInterval = 2f;
    [SerializeField] private bool logDetailedStatus = true;
    
    [Header("Component References")]
    [SerializeField] private DualTrackingSolutionSetup trackingSetup;
    [SerializeField] private NetworkManager networkManager;
    
    private float nextTestTime = 0f;
    private int testCycleCount = 0;
    
    private void Start()
    {
        if (trackingSetup == null)
            trackingSetup = FindObjectOfType<DualTrackingSolutionSetup>();
            
        if (networkManager == null)
            networkManager = FindObjectOfType<NetworkManager>();
            
        Debug.Log("=== DUAL TRACKING VALIDATION TEST STARTED ===");
        RunInitialValidation();
    }
    
    private void Update()
    {
        if (runContinuousTests && Time.time >= nextTestTime)
        {
            RunPeriodicValidation();
            nextTestTime = Time.time + testInterval;
            testCycleCount++;
        }
    }
    
    private void RunInitialValidation()
    {
        Debug.Log("--- Initial System Validation ---");
        
        // Test 1: Check avatar presence
        ValidateAvatarPresence();
        
        // Test 2: Check tracking solutions
        ValidateTrackingSolutions();
        
        // Test 3: Check network components
        ValidateNetworkComponents();
        
        // Test 4: Check Python system readiness
        ValidatePythonSystemReadiness();
        
        Debug.Log("--- Initial Validation Complete ---");
    }
    
    private void RunPeriodicValidation()
    {
        if (logDetailedStatus)
        {
            Debug.Log($"--- Periodic Validation #{testCycleCount} ---");
            
            // Monitor active tracking
            MonitorActiveTracking();
            
            // Monitor network status
            MonitorNetworkStatus();
            
            // Monitor performance
            MonitorPerformance();
        }
    }
    
    private void ValidateAvatarPresence()
    {
        GameObject bryce = GameObject.Find("Bryce") ?? GameObject.FindWithTag("Enemy");
        GameObject remy = GameObject.Find("Remy") ?? GameObject.FindWithTag("Player");
        
        LogTestResult("Bryce Avatar Found", bryce != null, bryce?.name);
        LogTestResult("Remy Avatar Found", remy != null, remy?.name);
        
        if (bryce != null)
        {
            var transformTargets = bryce.GetComponent<TransformTargets>();
            LogTestResult("Bryce has TransformTargets", transformTargets != null);
        }
        
        if (remy != null)
        {
            var transformTargets = remy.GetComponent<TransformTargets>();
            LogTestResult("Remy has TransformTargets", transformTargets != null);
        }
    }
    
    private void ValidateTrackingSolutions()
    {
        var trackingSolutions = FindObjectsOfType<MPUPoseTrackingSolution>();
        
        LogTestResult("Tracking Solutions Found", trackingSolutions.Length >= 1, $"Count: {trackingSolutions.Length}");
        LogTestResult("Dual Tracking Ready", trackingSolutions.Length >= 2, "Need 2 solutions for dual tracking");
        
        foreach (var solution in trackingSolutions)
        {
            bool isActive = solution.gameObject.activeInHierarchy;
            LogTestResult($"Solution '{solution.name}' Active", isActive);
        }
    }
    
    private void ValidateNetworkComponents()
    {
        // Check NetworkManager
        LogTestResult("NetworkManager Present", networkManager != null);
        
        if (networkManager != null)
        {
            LogTestResult("Network Session Active", networkManager.IsListening || networkManager.IsHost || networkManager.IsClient);
            LogTestResult("Is Host", networkManager.IsHost);
            LogTestResult("Is Client", networkManager.IsClient);
        }
        
        // Check NetworkedWebcamBackend
        var networkBackend = FindObjectOfType<NetworkedWebcamBackend>();
        LogTestResult("NetworkedWebcamBackend Found", networkBackend != null);
        
        // Check Network Camera Provider
        var networkProvider = FindObjectOfType<NetworkCameraImageProvider>();
        LogTestResult("NetworkCameraImageProvider Found", networkProvider != null);
        
        if (networkProvider != null)
        {
            LogTestResult("Network Camera Connected", networkProvider.IsConnected());
        }
    }
    
    private void ValidatePythonSystemReadiness()
    {
        // Check for Python system files
        string pythonPath = "Assets/Tracking4All/Third Party/MediaPipePython/PythonSystem/";
        
        bool mainPyExists = System.IO.File.Exists(Application.dataPath.Replace("Assets", "") + pythonPath + "main.py");
        bool networkWebcamExists = System.IO.File.Exists(Application.dataPath.Replace("Assets", "") + pythonPath + "networkedwebcam/entryPoint.py");
        
        LogTestResult("Python main.py exists", mainPyExists);
        LogTestResult("NetworkedWebcam module exists", networkWebcamExists);
        
        // Check Default Python NetWebCam prefab
        GameObject netWebCamPrefab = Resources.Load<GameObject>("Default Python NetWebCam");
        if (netWebCamPrefab == null)
        {
            // Try finding in scene
            GameObject sceneNetCam = GameObject.Find("Default Python NetWebCam");
            LogTestResult("Default Python NetWebCam available", sceneNetCam != null);
        }
        else
        {
            LogTestResult("Default Python NetWebCam prefab found", true);
        }
    }
    
    private void MonitorActiveTracking()
    {
        var trackingSolutions = FindObjectsOfType<MPUPoseTrackingSolution>();
        int activeSolutions = 0;
        
        foreach (var solution in trackingSolutions)
        {
            if (solution.gameObject.activeInHierarchy)
            {
                activeSolutions++;
                // Could check if solution is actually receiving data
            }
        }
        
        Debug.Log($"Active Tracking Solutions: {activeSolutions}");
    }
    
    private void MonitorNetworkStatus()
    {
        if (networkManager != null && trackingSetup != null)
        {
            string status = trackingSetup.GetSetupStatus();
            Debug.Log($"Setup Status: {status}");
        }
        
        var networkProvider = FindObjectOfType<NetworkCameraImageProvider>();
        if (networkProvider != null)
        {
            string connectionStatus = networkProvider.GetConnectionStatus();
            Debug.Log($"Network Camera: {connectionStatus}");
        }
    }
    
    private void MonitorPerformance()
    {
        float fps = 1f / Time.deltaTime;
        Debug.Log($"Current FPS: {fps:F1}");
        
        // Memory usage
        long memoryUsage = System.GC.GetTotalMemory(false) / 1024 / 1024; // MB
        Debug.Log($"Memory Usage: {memoryUsage}MB");
    }
    
    private void LogTestResult(string testName, bool passed, string details = null)
    {
        string status = passed ? "✓ PASS" : "✗ FAIL";
        string message = $"{status} - {testName}";
        
        if (!string.IsNullOrEmpty(details))
        {
            message += $" ({details})";
        }
        
        if (passed)
        {
            Debug.Log(message);
        }
        else
        {
            Debug.LogWarning(message);
        }
    }
    
    // Public methods for manual testing
    [ContextMenu("Run Full Validation")]
    public void RunFullValidation()
    {
        testCycleCount = 0;
        RunInitialValidation();
        RunPeriodicValidation();
    }
    
    [ContextMenu("Test Network Connection")]
    public void TestNetworkConnection()
    {
        var networkProvider = FindObjectOfType<NetworkCameraImageProvider>();
        if (networkProvider != null)
        {
            Debug.Log($"Network Test Result: {networkProvider.GetConnectionStatus()}");
        }
        else
        {
            Debug.LogError("NetworkCameraImageProvider not found for testing!");
        }
    }
    
    [ContextMenu("Show Setup Instructions")]
    public void ShowSetupInstructions()
    {
        var instructions = FindObjectOfType<DualAvatarSetupInstructions>();
        if (instructions != null)
        {
            instructions.ShowSetupInstructions();
        }
        else
        {
            Debug.LogWarning("DualAvatarSetupInstructions component not found!");
        }
    }
}