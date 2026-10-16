using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Mediapipe.Unity.Sample.PoseTracking;
using Tracking4All;

/// <summary>
/// Editor wizard to automatically set up dual avatar tracking system
/// Creates the necessary GameObjects and configurations for Bryce/Remy dual tracking
/// </summary>
public class DualAvatarTrackingSetupWizard : EditorWindow
{
    private GameObject bryceAvatar;
    private GameObject remyAvatar;
    private GameObject existingTrackingSolution;
    private GameObject networkCameraPrefab;
    
    private bool autoFindAvatars = true;
    private bool createNetworkManager = true;
    private bool setupPrefabs = true;
    
    [MenuItem("Tracking4All/Setup Dual Avatar Tracking")]
    public static void ShowWindow()
    {
        GetWindow<DualAvatarTrackingSetupWizard>("Dual Avatar Tracking Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Dual Avatar Tracking Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("This wizard sets up dual avatar tracking where:", EditorStyles.helpBox);
        GUILayout.Label("• HOST: Bryce = local camera, Remy = remote network camera", EditorStyles.helpBox);
        GUILayout.Label("• CLIENT: Remy = local camera, sends data to host", EditorStyles.helpBox);
        GUILayout.Space(10);
        
        // Avatar References
        GUILayout.Label("Avatar References", EditorStyles.boldLabel);
        bryceAvatar = EditorGUILayout.ObjectField("Bryce Avatar", bryceAvatar, typeof(GameObject), true) as GameObject;
        remyAvatar = EditorGUILayout.ObjectField("Remy Avatar", remyAvatar, typeof(GameObject), true) as GameObject;
        
        GUILayout.Space(10);
        
        // Tracking Solution
        GUILayout.Label("Existing Tracking Solution", EditorStyles.boldLabel);
        existingTrackingSolution = EditorGUILayout.ObjectField("Current Tracking Solution", existingTrackingSolution, typeof(GameObject), true) as GameObject;
        
        GUILayout.Space(10);
        
        // Network Camera
        GUILayout.Label("Network Camera Setup", EditorStyles.boldLabel);
        networkCameraPrefab = EditorGUILayout.ObjectField("Network Camera Prefab", networkCameraPrefab, typeof(GameObject), true) as GameObject;
        
        GUILayout.Space(10);
        
        // Options
        GUILayout.Label("Setup Options", EditorStyles.boldLabel);
        autoFindAvatars = EditorGUILayout.Toggle("Auto-find Avatars", autoFindAvatars);
        createNetworkManager = EditorGUILayout.Toggle("Create Network Manager", createNetworkManager);
        setupPrefabs = EditorGUILayout.Toggle("Setup Required Prefabs", setupPrefabs);
        
        GUILayout.Space(20);
        
        // Auto-find button
        if (GUILayout.Button("Auto-Find Components"))
        {
            AutoFindComponents();
        }
        
        GUILayout.Space(10);
        
        // Setup button
        GUI.enabled = CanPerformSetup();
        if (GUILayout.Button("Setup Dual Avatar Tracking", GUILayout.Height(40)))
        {
            PerformSetup();
        }
        GUI.enabled = true;
        
        GUILayout.Space(10);
        
        // Status
        ShowSetupStatus();
    }
    
    private void AutoFindComponents()
    {
        // Find avatars
        if (bryceAvatar == null)
        {
            GameObject found = GameObject.Find("Bryce");
            if (found == null) found = GameObject.Find("Bryce_Big");
            if (found == null) found = GameObject.FindWithTag("Enemy"); // Bryce might be tagged as Enemy
            bryceAvatar = found;
        }
        
        if (remyAvatar == null)
        {
            GameObject found = GameObject.Find("Remy");
            if (found == null) found = GameObject.Find("Remy_Big");
            if (found == null) found = GameObject.FindWithTag("Player"); // Remy might be tagged as Player
            remyAvatar = found;
        }
        
        // Find existing tracking solution
        if (existingTrackingSolution == null)
        {
            MPUPoseTrackingSolution solution = FindObjectOfType<MPUPoseTrackingSolution>();
            if (solution != null)
            {
                existingTrackingSolution = solution.gameObject;
            }
        }
        
        // Find network camera prefab
        if (networkCameraPrefab == null)
        {
            string[] guids = AssetDatabase.FindAssets("Default Python NetWebCam");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                networkCameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
        
        Debug.Log($"Auto-find results: Bryce={bryceAvatar?.name}, Remy={remyAvatar?.name}, " +
                 $"Solution={existingTrackingSolution?.name}, NetCam={networkCameraPrefab?.name}");
    }
    
    private bool CanPerformSetup()
    {
        return bryceAvatar != null && remyAvatar != null && existingTrackingSolution != null;
    }
    
    private void PerformSetup()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Dual Avatar Setup", "Setting up dual avatar tracking...", 0f);
            
            // 1. Create Network Manager if needed
            if (createNetworkManager)
            {
                CreateNetworkManagerSetup();
                EditorUtility.DisplayProgressBar("Dual Avatar Setup", "Creating network manager...", 0.2f);
            }
            
            // 2. Create second tracking solution
            GameObject secondSolution = CreateSecondTrackingSolution();
            EditorUtility.DisplayProgressBar("Dual Avatar Setup", "Creating second tracking solution...", 0.4f);
            
            // 3. Setup network camera
            GameObject networkCameraInstance = null;
            if (networkCameraPrefab != null)
            {
                networkCameraInstance = SetupNetworkCamera();
                EditorUtility.DisplayProgressBar("Dual Avatar Setup", "Setting up network camera...", 0.6f);
            }
            
            // 4. Create dual tracking manager
            GameObject managerObject = CreateDualTrackingManager(secondSolution, networkCameraInstance);
            EditorUtility.DisplayProgressBar("Dual Avatar Setup", "Creating tracking manager...", 0.8f);
            
            // 5. Configure avatar components
            ConfigureAvatarComponents();
            EditorUtility.DisplayProgressBar("Dual Avatar Setup", "Configuring avatars...", 1.0f);
            
            EditorUtility.ClearProgressBar();
            
            EditorUtility.DisplayDialog("Setup Complete", 
                "Dual avatar tracking system has been set up successfully!\\n\\n" +
                "Next steps:\\n" +
                "1. Test HOST mode: Bryce=local camera, Remy=network camera\\n" +
                "2. Test CLIENT mode: Start Python networked webcam\\n" +
                "3. Verify both avatars track correctly", "OK");
                
            // Select the manager in hierarchy
            Selection.activeGameObject = managerObject;
            EditorGUIUtility.PingObject(managerObject);
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Setup Error", $"Error during setup: {e.Message}", "OK");
            Debug.LogError($"Dual avatar setup error: {e}");
        }
    }
    
    private void CreateNetworkManagerSetup()
    {
        // Find or create NetworkManager
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager == null)
        {
            GameObject nmObject = new GameObject("NetworkManager");
            networkManager = nmObject.AddComponent<NetworkManager>();
            
            // Basic network manager configuration
            // This would need proper configuration for the specific networking setup
            Debug.Log("Created NetworkManager for dual avatar tracking");
        }
    }
    
    private GameObject CreateSecondTrackingSolution()
    {
        // Duplicate the existing tracking solution
        GameObject secondSolution = Instantiate(existingTrackingSolution);
        secondSolution.name = "Remote Pose Tracking Solution";
        
        // Position it next to the original
        secondSolution.transform.position = existingTrackingSolution.transform.position + Vector3.right * 2f;
        
        // Mark as remote solution
        MPUPoseTrackingSolution solutionComponent = secondSolution.GetComponent<MPUPoseTrackingSolution>();
        if (solutionComponent != null)
        {
            // Configure for remote use
            // This might need specific configuration based on the solution setup
        }
        
        return secondSolution;
    }
    
    private GameObject SetupNetworkCamera()
    {
        // Instantiate network camera prefab
        GameObject networkCamera = PrefabUtility.InstantiatePrefab(networkCameraPrefab) as GameObject;
        networkCamera.name = "Network Camera (Remote)";
        
        // Position appropriately
        networkCamera.transform.position = new Vector3(0, 2, 0);
        
        return networkCamera;
    }
    
    private GameObject CreateDualTrackingManager(GameObject secondSolution, GameObject networkCamera)
    {
        // Create the main dual tracking manager
        GameObject managerObject = new GameObject("Dual Avatar Tracking Manager");
        
        // Add network behavior
        NetworkObject netObject = managerObject.AddComponent<NetworkObject>();
        
        // Add the dual tracking setup component
        DualTrackingSolutionSetup setupComponent = managerObject.AddComponent<DualTrackingSolutionSetup>();
        
        // Configure the setup component
        var bryceConfig = new AvatarTrackingConfiguration
        {
            avatar = bryceAvatar,
            avatarName = "Bryce",
            trackingSolution = existingTrackingSolution.GetComponent<MPUPoseTrackingSolution>(),
            useNetworkCamera = false
        };
        
        var remyConfig = new AvatarTrackingConfiguration
        {
            avatar = remyAvatar,
            avatarName = "Remy", 
            trackingSolution = secondSolution.GetComponent<MPUPoseTrackingSolution>(),
            useNetworkCamera = true
        };
        
        // These would be set via SerializedObject since the fields are private
        SerializedObject serializedObject = new SerializedObject(setupComponent);
        serializedObject.FindProperty("networkCameraPrefab").objectReferenceValue = networkCameraPrefab;
        serializedObject.FindProperty("enableDebugLogs").boolValue = true;
        serializedObject.ApplyModifiedProperties();
        
        return managerObject;
    }
    
    private void ConfigureAvatarComponents()
    {
        // Ensure avatars have required components
        ConfigureAvatar(bryceAvatar, "Bryce");
        ConfigureAvatar(remyAvatar, "Remy");
    }
    
    private void ConfigureAvatar(GameObject avatar, string avatarName)
    {
        if (avatar == null) return;
        
        // Add TransformTargets if not present
        if (avatar.GetComponent<TransformTargets>() == null)
        {
            avatar.AddComponent<TransformTargets>();
        }
        
        // Ensure proper tags
        if (avatarName == "Bryce" && avatar.tag != "Enemy")
        {
            avatar.tag = "Enemy";
        }
        else if (avatarName == "Remy" && avatar.tag != "Player")
        {
            avatar.tag = "Player";
        }
        
        Debug.Log($"Configured {avatarName} avatar components");
    }
    
    private void ShowSetupStatus()
    {
        GUILayout.Label("Setup Status:", EditorStyles.boldLabel);
        
        ShowComponentStatus("Bryce Avatar", bryceAvatar);
        ShowComponentStatus("Remy Avatar", remyAvatar);
        ShowComponentStatus("Tracking Solution", existingTrackingSolution);
        ShowComponentStatus("Network Camera Prefab", networkCameraPrefab);
        
        if (!CanPerformSetup())
        {
            EditorGUILayout.HelpBox("Missing required components. Use 'Auto-Find Components' or assign manually.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("Ready to setup dual avatar tracking!", MessageType.Info);
        }
    }
    
    private void ShowComponentStatus(string name, GameObject obj)
    {
        GUIStyle style = obj != null ? EditorStyles.label : EditorStyles.helpBox;
        Color originalColor = GUI.color;
        if (obj == null) GUI.color = Color.yellow;
        
        GUILayout.Label($"  {name}: {(obj != null ? obj.name : "NOT FOUND")}", style);
        
        GUI.color = originalColor;
    }
}