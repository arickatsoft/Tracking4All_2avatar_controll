using UnityEngine;
using UnityEditor;

public class Tracking4AllSetupWindow : EditorWindow
{
    [MenuItem("Tracking4All/Setup Player Selection")]
    public static void ShowWindow()
    {
        GetWindow<Tracking4AllSetupWindow>("Tracking4All Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Tracking4All Player Selection Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Complete Scene Setup", GUILayout.Height(40)))
        {
            SetupCompleteScene();
        }
        
        EditorGUILayout.Space();
        GUILayout.Label("Individual Setup Steps:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1. Create Game Manager"))
        {
            CreateGameManager();
        }
        
        if (GUILayout.Button("2. Setup Player Selection"))
        {
            SetupPlayerSelection();
        }
        
        if (GUILayout.Button("3. Setup Tracking Switcher"))
        {
            SetupTrackingSwitcher();
        }
        
        if (GUILayout.Button("4. Setup Camera Controller"))
        {
            SetupCameraController();
        }
        
        if (GUILayout.Button("5. Configure Avatar Tags"))
        {
            ConfigureAvatarTags();
        }
    }
    
    void SetupCompleteScene()
    {
        CreateGameManager();
        SetupPlayerSelection();
        SetupTrackingSwitcher();
        SetupCameraController();
        ConfigureAvatarTags();
        
        Debug.Log("Complete scene setup finished!");
        EditorUtility.DisplayDialog("Setup Complete", 
            "The scene has been configured for player selection.\n\n" +
            "Make sure to:\n" +
            "1. Enter Play Mode to test\n" +
            "2. Check that both avatars are visible\n" +
            "3. Verify the Tracking4All system is active", 
            "OK");
    }
    
    void CreateGameManager()
    {
        GameManager existing = FindObjectOfType<GameManager>();
        if (existing == null)
        {
            GameObject gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();
            Debug.Log("Created GameManager");
        }
        else
        {
            Debug.Log("GameManager already exists");
        }
    }
    
    void SetupPlayerSelection()
    {
        PlayerSelectionManager existing = FindObjectOfType<PlayerSelectionManager>();
        if (existing == null)
        {
            GameObject psGO = new GameObject("PlayerSelectionManager");
            PlayerSelectionManager psm = psGO.AddComponent<PlayerSelectionManager>();
            
            // Auto-find references
            GameObject remy = GameObject.Find("Remy");
            GameObject adam = GameObject.Find("Adam_Big");
            GameObject cameraRig = GameObject.Find("CameraRig");
            GameObject tracking = GameObject.Find("Tracking4All");
            
            Debug.Log($"Found: Remy={remy != null}, Adam={adam != null}, Camera={cameraRig != null}, Tracking={tracking != null}");
        }
        else
        {
            Debug.Log("PlayerSelectionManager already exists");
        }
    }
    
    void SetupTrackingSwitcher()
    {
        TrackingAvatarSwitcher existing = FindObjectOfType<TrackingAvatarSwitcher>();
        if (existing == null)
        {
            GameObject tsGO = new GameObject("TrackingAvatarSwitcher");
            tsGO.AddComponent<TrackingAvatarSwitcher>();
            Debug.Log("Created TrackingAvatarSwitcher");
        }
        else
        {
            Debug.Log("TrackingAvatarSwitcher already exists");
        }
    }
    
    void SetupCameraController()
    {
        DynamicCameraController existing = FindObjectOfType<DynamicCameraController>();
        if (existing == null)
        {
            GameObject ccGO = new GameObject("DynamicCameraController");
            ccGO.AddComponent<DynamicCameraController>();
            Debug.Log("Created DynamicCameraController");
        }
        else
        {
            Debug.Log("DynamicCameraController already exists");
        }
    }
    
    void ConfigureAvatarTags()
    {
        // Setup tags
        GameObject remy = GameObject.Find("Remy");
        GameObject adam = GameObject.Find("Adam_Big");
        
        if (remy != null)
        {
            remy.tag = "Player";
            Debug.Log("Set Remy tag to Player");
        }
        
        if (adam != null)
        {
            adam.tag = "Enemy";
            Debug.Log("Set Adam tag to Enemy");
        }
        
        // Ensure tags exist in project
        EnsureTagExists("Player");
        EnsureTagExists("Enemy");
    }
    
    void EnsureTagExists(string tagName)
    {
        try
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tagName))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTag.stringValue = tagName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"Added tag: {tagName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not add tag {tagName}: {e.Message}");
        }
    }
}
