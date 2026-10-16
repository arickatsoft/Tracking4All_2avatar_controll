using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Quick setup script to ensure all required components are in the scene
/// Run this to automatically add missing components for dual avatar tracking
/// </summary>
public class QuickSceneSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = false;
    
    [Header("Prefab References")]
    [SerializeField] private GameObject netWebCamPrefab;
    
    [ContextMenu("Setup Scene for Dual Avatar Tracking")]
    public void SetupScene()
    {
        Debug.Log("=== SETTING UP SCENE FOR DUAL AVATAR TRACKING ===");
        
        // 1. Check and add NetworkManager
        SetupNetworkManager();
        
        // 2. Check and add Default Python NetWebCam
        SetupNetworkCamera();
        
        // 3. Check avatars
        CheckAvatars();
        
        // 4. Final verification
        VerifySetup();
        
        Debug.Log("=== SCENE SETUP COMPLETE ===");
    }
    
    private void SetupNetworkManager()
    {
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager == null)
        {
            Debug.Log("Adding NetworkManager...");
            GameObject nmGO = new GameObject("NetworkManager");
            networkManager = nmGO.AddComponent<NetworkManager>();
            Debug.Log("✅ NetworkManager added");
        }
        else
        {
            Debug.Log("✅ NetworkManager already exists");
        }
    }
    
    private void SetupNetworkCamera()
    {
        // Check if Default Python NetWebCam already exists
        GameObject existingNetCam = GameObject.Find("Default Python NetWebCam");
        if (existingNetCam != null)
        {
            Debug.Log("✅ Default Python NetWebCam already in scene");
            Debug.Log($"   Active: {existingNetCam.activeInHierarchy}");
            return;
        }
        
        // Try to load the prefab
        if (netWebCamPrefab == null)
        {
            // Try to load from Resources or find in project
            netWebCamPrefab = Resources.Load<GameObject>("Default Python NetWebCam");
            
            if (netWebCamPrefab == null)
            {
                Debug.LogError("❌ Could not find Default Python NetWebCam prefab!");
                Debug.LogError("MANUAL STEP REQUIRED:");
                Debug.LogError("1. Go to Assets/Tracking4All/Prefabs/");
                Debug.LogError("2. Drag 'Default Python NetWebCam.prefab' into scene");
                return;
            }
        }
        
        // Instantiate the prefab
        GameObject netCam = Instantiate(netWebCamPrefab);
        netCam.name = "Default Python NetWebCam";
        Debug.Log("✅ Default Python NetWebCam added to scene");
    }
    
    private void CheckAvatars()
    {
        Debug.Log("--- Avatar Check ---");
        
        GameObject bryce = GameObject.Find("Bryce");
        if (bryce == null) bryce = GameObject.FindWithTag("Enemy");
        Debug.Log($"Bryce Avatar: {(bryce != null ? "✅ Found" : "❌ Missing")}");
        
        GameObject remy = GameObject.Find("Remy");  
        if (remy == null) remy = GameObject.FindWithTag("Player");
        Debug.Log($"Remy Avatar: {(remy != null ? "✅ Found" : "❌ Missing")}");
        
        if (bryce == null || remy == null)
        {
            Debug.LogWarning("⚠️ Missing avatars - add Bryce and Remy to scene for dual tracking");
        }
    }
    
    private void VerifySetup()
    {
        Debug.Log("--- Final Verification ---");
        
        var networkManager = FindObjectOfType<NetworkManager>();
        var netWebCam = GameObject.Find("Default Python NetWebCam");
        
        if (networkManager != null && netWebCam != null)
        {
            Debug.Log("🎉 Scene setup complete!");
            Debug.Log("Next steps:");
            Debug.Log("1. Build to Android");
            Debug.Log("2. Start app and click 'Host Game'");  
            Debug.Log("3. Wait 15 seconds for network initialization");
            Debug.Log("4. Unity should then listen on port 52730");
            Debug.Log("5. Start Python script on Mac");
        }
        else
        {
            Debug.LogError("❌ Setup incomplete - missing required components");
        }
    }
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupScene();
        }
    }
    
    // Manual prefab assignment helper
    [ContextMenu("Find NetWebCam Prefab")]
// Manual prefab assignment helper
    [ContextMenu("Find NetWebCam Prefab")]
    public void FindNetWebCamPrefab()
    {
        #if UNITY_EDITOR
        // This helps find the prefab in the project
        string[] guids = UnityEditor.AssetDatabase.FindAssets("Default Python NetWebCam");
        
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"Found prefab at: {path}");
            
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                netWebCamPrefab = prefab;
                Debug.Log("✅ Prefab reference set");
                break;
            }
        }
        #else
        Debug.LogWarning("FindNetWebCamPrefab only works in Unity Editor");
        #endif
    }
}