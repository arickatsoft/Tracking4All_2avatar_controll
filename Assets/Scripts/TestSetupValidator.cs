using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using Unity.Netcode;
using Mediapipe.Unity.Sample.PoseTracking;
using Tracking4All;

/// <summary>
/// Validates that the Unity project is ready for Android Host + Mac Client testing
/// Checks all required components and configurations
/// </summary>
public class TestSetupValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [SerializeField] private bool runValidationOnStart = true;
    [SerializeField] private bool showDetailedResults = true;
    
    [Header("UI Display")]
    [SerializeField] private UnityEngine.UI.Text validationResultsText;
    [SerializeField] private UnityEngine.UI.Button runValidationButton;
    
    private int totalTests = 0;
    private int passedTests = 0;
    
    private void Start()
    {
        if (runValidationOnStart)
        {
            RunFullValidation();
        }
        
        if (runValidationButton != null)
        {
            runValidationButton.onClick.AddListener(RunFullValidation);
        }
    }
    
    [ContextMenu("Run Full Validation")]
    public void RunFullValidation()
    {
        totalTests = 0;
        passedTests = 0;
        
        Debug.Log("=== ANDROID-MAC TEST SETUP VALIDATION ===");
        
        // Core Unity components
        ValidateUnityComponents();
        
        // Tracking4All system
        ValidateTracking4AllSystem();
        
        // Networking components  
        ValidateNetworkingSystem();
        
        // Python system files
        ValidatePythonSystem();
        
        // Avatar setup
        ValidateAvatarSetup();
        
        // Show final results
        ShowValidationResults();
    }
    
    private void ValidateUnityComponents()
    {
        Debug.Log("--- Unity Components Validation ---");
        
        // Check Unity version compatibility
        ValidateTest("Unity Version Compatible", 
            Application.unityVersion.StartsWith("6000") || Application.unityVersion.StartsWith("2023"),
            $"Version: {Application.unityVersion}");
        
        // Check platform settings
        #if UNITY_ANDROID && !UNITY_EDITOR
        ValidateTest("Running on Android", true, "Android build detected");
        #elif UNITY_EDITOR
        ValidateTest("Unity Editor", true, "Editor - can test Mac client");
        #else
        ValidateTest("Supported Platform", true, "Platform detected");
        #endif
        
        // Check camera permissions
        #if UNITY_ANDROID && !UNITY_EDITOR
        bool cameraPermission = Permission.HasUserAuthorizedPermission(Permission.Camera);
        ValidateTest("Camera Permission", cameraPermission, 
            cameraPermission ? "Granted" : "Need to grant camera permission");
        #endif
    }
    
    private void ValidateTracking4AllSystem()
    {
        Debug.Log("--- Tracking4All System Validation ---");
        
        // Check core Tracking4All singleton
        var tracking4All = FindObjectOfType<Tracking4All.Tracking4All>();
        ValidateTest("Tracking4All Manager", tracking4All != null, 
            tracking4All ? tracking4All.name : "Missing Tracking4All manager in scene");
        
        // Check MPU Tracking Solutions
        var trackingSolutions = FindObjectsOfType<MPUPoseTrackingSolution>();
        ValidateTest("Pose Tracking Solutions", trackingSolutions.Length > 0, 
            $"Found: {trackingSolutions.Length} solutions");
        
        ValidateTest("Dual Tracking Ready", trackingSolutions.Length >= 2,
            trackingSolutions.Length >= 2 ? "Ready for dual tracking" : "Need 2 solutions for dual avatars");
        
        // Check NetworkedWebcamBackend
        var networkBackend = FindObjectOfType<NetworkedWebcamBackend>();
        ValidateTest("NetworkedWebcamBackend", networkBackend != null,
            networkBackend ? "Found in scene" : "Missing - check if Default Python NetWebCam prefab is instantiated");
    }
    
    private void ValidateNetworkingSystem()
    {
        Debug.Log("--- Networking System Validation ---");
        
        // Check Unity Netcode
        var networkManager = FindObjectOfType<NetworkManager>();
        ValidateTest("Unity NetworkManager", networkManager != null,
            networkManager ? "Found in scene" : "Missing NetworkManager");
        
        // Check our dual tracking components
        var dualSetup = FindObjectOfType<DualTrackingSolutionSetup>();
        ValidateTest("Dual Tracking Setup", dualSetup != null,
            dualSetup ? "Component ready" : "Missing DualTrackingSolutionSetup component");
        
        // Check Android-Mac test setup
        var testSetup = FindObjectOfType<AndroidMacTestSetup>();
        ValidateTest("Android-Mac Test Setup", testSetup != null,
            testSetup ? "Test helper available" : "AndroidMacTestSetup not found");
    }
    
    private void ValidatePythonSystem()
    {
        Debug.Log("--- Python System Validation ---");
        
        string basePath = Application.dataPath.Replace("/Assets", "") + "/Assets/Tracking4All/Third Party/MediaPipePython/PythonSystem/";
        
        // Check main.py
        bool mainPyExists = System.IO.File.Exists(basePath + "main.py");
        ValidateTest("Python main.py", mainPyExists, 
            mainPyExists ? "Found" : "Missing - Python system not available");
        
        // Check networkedwebcam module
        bool networkWebcamExists = System.IO.File.Exists(basePath + "networkedwebcam/entryPoint.py");
        ValidateTest("NetworkedWebcam Module", networkWebcamExists,
            networkWebcamExists ? "Available" : "Missing networkedwebcam module");
        
        // Check global_vars.py
        bool globalVarsExists = System.IO.File.Exists(basePath + "networkedwebcam/global_vars.py");
        ValidateTest("Global Variables Config", globalVarsExists,
            globalVarsExists ? "Ready for configuration" : "Missing global_vars.py");
        
        // Check if we can read the global vars file
        if (globalVarsExists)
        {
            try
            {
                string content = System.IO.File.ReadAllText(basePath + "networkedwebcam/global_vars.py");
                bool hasHostConfig = content.Contains("HOST") && content.Contains("PORT");
                ValidateTest("Python Config Structure", hasHostConfig,
                    hasHostConfig ? "HOST and PORT variables found" : "Missing HOST/PORT configuration");
            }
            catch (System.Exception e)
            {
                ValidateTest("Python Config Readable", false, $"Error: {e.Message}");
            }
        }
    }
    
    private void ValidateAvatarSetup()
    {
        Debug.Log("--- Avatar Setup Validation ---");
        
        // Find Bryce avatar
        GameObject bryce = GameObject.Find("Bryce");
        if (bryce == null) bryce = GameObject.FindWithTag("Enemy");
        ValidateTest("Bryce Avatar", bryce != null, 
            bryce ? $"Found: {bryce.name}" : "Missing Bryce avatar");
        
        // Find Remy avatar  
        GameObject remy = GameObject.Find("Remy");
        if (remy == null) remy = GameObject.FindWithTag("Player");
        ValidateTest("Remy Avatar", remy != null,
            remy ? $"Found: {remy.name}" : "Missing Remy avatar");
        
        // Check TransformTargets components
        if (bryce != null)
        {
            var bryceTargets = bryce.GetComponent<TransformTargets>();
            ValidateTest("Bryce TransformTargets", bryceTargets != null,
                bryceTargets ? "Component attached" : "Missing TransformTargets component");
        }
        
        if (remy != null)
        {
            var remyTargets = remy.GetComponent<TransformTargets>();
            ValidateTest("Remy TransformTargets", remyTargets != null,
                remyTargets ? "Component attached" : "Missing TransformTargets component");
        }
        
        // Check avatar tags
        if (bryce != null)
        {
            bool correctTag = bryce.tag == "Enemy";
            ValidateTest("Bryce Tag", correctTag, $"Tag: {bryce.tag} {(correctTag ? "(correct)" : "(should be Enemy)")}");
        }
        
        if (remy != null)
        {
            bool correctTag = remy.tag == "Player";  
            ValidateTest("Remy Tag", correctTag, $"Tag: {remy.tag} {(correctTag ? "(correct)" : "(should be Player)")}");
        }
    }
    
    private void ValidateTest(string testName, bool passed, string details)
    {
        totalTests++;
        if (passed) passedTests++;
        
        string status = passed ? "✓ PASS" : "✗ FAIL";
        string message = $"{status} - {testName}";
        
        if (!string.IsNullOrEmpty(details))
        {
            message += $" ({details})";
        }
        
        if (showDetailedResults)
        {
            if (passed)
                Debug.Log(message);
            else
                Debug.LogWarning(message);
        }
    }
    
    private void ShowValidationResults()
    {
        Debug.Log("--- VALIDATION SUMMARY ---");
        
        float successRate = totalTests > 0 ? (float)passedTests / totalTests * 100f : 0f;
        
        Debug.Log($"Tests: {passedTests}/{totalTests} passed ({successRate:F1}%)");
        
        if (passedTests == totalTests)
        {
            Debug.Log("🎉 ALL TESTS PASSED - Ready for Android-Mac testing!");
            Debug.Log("Next steps:");
            Debug.Log("1. Build to Android device");
            Debug.Log("2. Configure Python script with Android IP");  
            Debug.Log("3. Start host on Android, run Python on Mac");
        }
        else
        {
            Debug.LogWarning($"⚠️ {totalTests - passedTests} issues found - fix before testing");
            
            if (FindObjectOfType<NetworkManager>() == null)
                Debug.LogWarning("→ Add NetworkManager to scene");
            if (FindObjectsOfType<MPUPoseTrackingSolution>().Length < 2)
                Debug.LogWarning("→ Need 2 MPUPoseTrackingSolution instances");
            if (GameObject.Find("Bryce") == null && GameObject.FindWithTag("Enemy") == null)
                Debug.LogWarning("→ Add Bryce avatar to scene");
            if (GameObject.Find("Remy") == null && GameObject.FindWithTag("Player") == null)
                Debug.LogWarning("→ Add Remy avatar to scene");
        }
        
        // Update UI if available
        if (validationResultsText != null)
        {
            string uiText = $"Validation: {passedTests}/{totalTests} passed\\n";
            uiText += successRate == 100f ? "✅ Ready for testing!" : "⚠️ Issues found";
            validationResultsText.text = uiText;
        }
    }
    
    public bool IsReadyForTesting()
    {
        // Quick check for critical components
        var networkManager = FindObjectOfType<NetworkManager>();
        var trackingSolutions = FindObjectsOfType<MPUPoseTrackingSolution>();
        var bryce = GameObject.Find("Bryce") ?? GameObject.FindWithTag("Enemy");
        var remy = GameObject.Find("Remy") ?? GameObject.FindWithTag("Player");
        
        return networkManager != null && 
               trackingSolutions.Length >= 2 && 
               bryce != null && 
               remy != null;
    }
    
    public string GetQuickStatus()
    {
        return IsReadyForTesting() ? 
            "Ready for Android-Mac testing" : 
            "Setup incomplete - run validation";
    }
}