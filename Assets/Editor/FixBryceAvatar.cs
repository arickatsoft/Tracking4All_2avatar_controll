using UnityEngine;
using UnityEditor;

public class FixBryceAvatar : EditorWindow
{
    [MenuItem("Tools/Fix Bryce Avatar")]
    public static void FixAvatar()
    {
        // Find the Bryce GameObject in the scene
        GameObject bryceObj = GameObject.Find("Bryce");
        if (bryceObj == null)
        {
            Debug.LogError("Bryce GameObject not found in scene!");
            return;
        }

        // Get the Animator component
        Animator animator = bryceObj.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator component found on Bryce!");
            return;
        }

        // Try to load the Bryce.fbx asset and find its avatar
        string fbxPath = "Assets/Tracking4All/Models/FitFight/Bryce.fbx";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        Avatar bryceAvatar = null;
        foreach (Object asset in assets)
        {
            if (asset is Avatar)
            {
                bryceAvatar = asset as Avatar;
                Debug.Log($"Found avatar: {bryceAvatar.name}");
                break;
            }
        }

        if (bryceAvatar == null)
        {
            Debug.LogError("No Avatar found in Bryce.fbx! The FBX might need to be reimported with proper avatar settings.");
            
            // Try to reimport the FBX with correct settings
            ModelImporter modelImporter = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (modelImporter != null)
            {
                modelImporter.animationType = ModelImporterAnimationType.Human;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modelImporter.SaveAndReimport();
                
                Debug.Log("Reimported Bryce.fbx with human animation type. Please run this tool again.");
                return;
            }
        }
        else
        {
            // Assign the avatar to the animator
            animator.avatar = bryceAvatar;
            Debug.Log($"Successfully assigned avatar '{bryceAvatar.name}' to Bryce animator!");
            
            // Also set the runtime animator controller if not set
            if (animator.runtimeAnimatorController == null)
            {
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Tracking4All/Models/FitFight/BryceAnimController.controller");
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("Assigned BryceAnimController to Bryce animator!");
                }
            }
        }

        // Mark the scene as dirty so it gets saved
        EditorUtility.SetDirty(bryceObj);
    }
}