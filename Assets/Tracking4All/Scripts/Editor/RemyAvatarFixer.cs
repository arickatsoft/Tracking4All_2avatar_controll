using UnityEngine;
using UnityEditor;

public class RemyAvatarFixer : EditorWindow
{
    [MenuItem("Tools/Fix Remy Avatar Connection")]
    public static void ShowWindow()
    {
        GetWindow<RemyAvatarFixer>("Remy Avatar Fixer");
    }

    void OnGUI()
    {
        GUILayout.Label("Remy Avatar Connection Fixer", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Fix Remy Avatar Connections"))
        {
            FixRemyConnections();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Manual Steps:", EditorStyles.boldLabel);
        GUILayout.Label("1. Ensure Remy.fbx is set to Humanoid");
        GUILayout.Label("2. Check Remy has Animator with RemyAnimController");
        GUILayout.Label("3. Run the Auto-Fix above");
        GUILayout.Label("4. Test in Play mode");
    }

    void FixRemyConnections()
    {
        // Find required gameobjects
        GameObject remy = GameObject.Find("Remy");
        GameObject puppetStandardT = GameObject.Find("Puppet-StandardT");
        GameObject poseTrackingSolution = GameObject.Find("Pose Tracking Solution");
        
        if (remy == null)
        {
            Debug.LogError("Remy gameobject not found in scene!");
            return;
        }
        
        if (puppetStandardT == null)
        {
            Debug.LogError("Puppet-StandardT gameobject not found in scene!");
            return;
        }
        
        if (poseTrackingSolution == null)
        {
            Debug.LogError("Pose Tracking Solution gameobject not found in scene!");
            return;
        }
        
        // Fix Remy's tracking component connections
        MonoBehaviour[] remyComponents = remy.GetComponents<MonoBehaviour>();
        foreach (var component in remyComponents)
        {
            // Use reflection to find and set pose-related fields
            var type = component.GetType();
            
            // Look for pose.avatarSkeleton field
            var poseField = type.GetField("pose");
            if (poseField != null)
            {
                var poseValue = poseField.GetValue(component);
                if (poseValue != null)
                {
                    var avatarSkeletonField = poseValue.GetType().GetField("avatarSkeleton");
                    if (avatarSkeletonField != null)
                    {
                        avatarSkeletonField.SetValue(poseValue, remy.GetComponent<MonoBehaviour>());
                        Debug.Log("Fixed pose.avatarSkeleton connection");
                    }
                    
                    var useAnyPuppetField = poseValue.GetType().GetField("useAnyPuppet");
                    if (useAnyPuppetField != null)
                    {
                        useAnyPuppetField.SetValue(poseValue, true);
                        Debug.Log("Enabled pose.useAnyPuppet");
                    }
                    
                    var puppetProviderField = poseValue.GetType().GetField("puppetProvider");
                    if (puppetProviderField != null)
                    {
                        var puppetProvider = puppetProviderField.GetValue(poseValue);
                        if (puppetProvider != null)
                        {
                            var providerField = puppetProvider.GetType().GetField("provider");
                            if (providerField != null)
                            {
                                providerField.SetValue(puppetProvider, puppetStandardT.GetComponent<MonoBehaviour>());
                                Debug.Log("Fixed pose.puppetProvider.provider connection");
                            }
                        }
                    }
                }
            }
        }
        
        // Fix Puppet-StandardT connections
        MonoBehaviour[] puppetComponents = puppetStandardT.GetComponents<MonoBehaviour>();
        foreach (var component in puppetComponents)
        {
            var type = component.GetType();
            
            // Look for provider fields
            var providerField = type.GetField("provider");
            if (providerField != null)
            {
                var provider = providerField.GetValue(component);
                if (provider != null)
                {
                    var providerProviderField = provider.GetType().GetField("provider");
                    if (providerProviderField != null)
                    {
                        providerProviderField.SetValue(provider, poseTrackingSolution.GetComponent<MonoBehaviour>());
                        Debug.Log("Fixed puppet provider connection");
                    }
                }
            }
        }
        
        Debug.Log("Remy Avatar Fix Complete! Test in Play mode.");
        EditorUtility.SetDirty(remy);
        EditorUtility.SetDirty(puppetStandardT);
    }
}
