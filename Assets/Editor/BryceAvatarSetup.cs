using UnityEngine;
using UnityEditor;

public class BryceAvatarSetup : MonoBehaviour
{
    [ContextMenu("Setup Bryce Avatar")]
    public void SetupAvatar()
    {
        string fbxPath = "Assets/Tracking4All/Models/FitFight/Bryce.fbx";
        
        // Force reimport the FBX with correct settings
        ModelImporter modelImporter = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (modelImporter != null)
        {
            modelImporter.animationType = ModelImporterAnimationType.Human;
            modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            modelImporter.SaveAndReimport();
            Debug.Log("Reimported Bryce.fbx with Human avatar settings.");
        }
        
        // Wait a frame for import to complete, then assign avatar
        EditorApplication.delayCall += () => AssignAvatarToBryce();
    }
    
    private void AssignAvatarToBryce()
    {
        GameObject bryceObj = GameObject.Find("Bryce");
        if (bryceObj == null)
        {
            Debug.LogError("Bryce GameObject not found!");
            return;
        }
        
        Animator animator = bryceObj.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator on Bryce!");
            return;
        }
        
        // Load all assets from the FBX file
        string fbxPath = "Assets/Tracking4All/Models/FitFight/Bryce.fbx";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        Avatar bryceAvatar = null;
        foreach (Object asset in assets)
        {
            if (asset is Avatar avatar)
            {
                bryceAvatar = avatar;
                Debug.Log($"Found avatar: {avatar.name} (valid: {avatar.isValid})");
                break;
            }
        }
        
        if (bryceAvatar != null)
        {
            animator.avatar = bryceAvatar;
            Debug.Log($"Successfully assigned avatar to Bryce! Avatar valid: {bryceAvatar.isValid}");
            
            // Also ensure the animator controller is assigned
            if (animator.runtimeAnimatorController == null)
            {
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Tracking4All/Models/FitFight/BryceAnimController.controller");
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("Assigned BryceAnimController to animator.");
                }
            }
            
            EditorUtility.SetDirty(bryceObj);
        }
        else
        {
            Debug.LogError("No valid Avatar found in Bryce.fbx!");
        }
    }
}