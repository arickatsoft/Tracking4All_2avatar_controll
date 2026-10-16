using UnityEngine;
using UnityEditor;

public class DirectPuppetFixer : EditorWindow
{
    [MenuItem("Tools/Direct Puppet Connection Fix")]
    public static void ShowWindow()
    {
        GetWindow<DirectPuppetFixer>("Direct Puppet Fix");
    }

    void OnGUI()
    {
        GUILayout.Label("Direct Puppet Connection Fix", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Fix Puppet-StandardT Connection"))
        {
            FixPuppetConnection();
        }
    }

    void FixPuppetConnection()
    {
        // Find the gameobjects
        GameObject puppetStandardT = GameObject.Find("Puppet-StandardT");
        GameObject poseSolution = GameObject.Find("Pose Solution");
        
        if (puppetStandardT == null)
        {
            Debug.LogError("Puppet-StandardT not found!");
            return;
        }
        
        if (poseSolution == null)
        {
            Debug.LogError("Pose Solution not found!");
            return;
        }
        
        // Get the PosePuppet component on Puppet-StandardT
        var posePuppet = puppetStandardT.GetComponent<MonoBehaviour>();
        if (posePuppet == null)
        {
            Debug.LogError("PosePuppet component not found on Puppet-StandardT!");
            return;
        }
        
        // Get the PoseSolution component on Pose Solution
        var poseSolutionComponent = poseSolution.GetComponent<MonoBehaviour>();
        if (poseSolutionComponent == null)
        {
            Debug.LogError("PoseSolution component not found on Pose Solution!");
            return;
        }
        
        // Use reflection to set the provider field
        var type = posePuppet.GetType();
        var providerField = type.GetField("provider");
        
        if (providerField != null)
        {
            // Try to set the provider directly to the pose solution gameobject
            providerField.SetValue(posePuppet, poseSolution);
            Debug.Log("Set provider directly to Pose Solution GameObject");
        }
        else
        {
            // Look for a nested provider structure
            var poseProviderField = type.GetField("poseProvider");
            if (poseProviderField != null)
            {
                var poseProvider = poseProviderField.GetValue(posePuppet);
                if (poseProvider != null)
                {
                    var nestedProviderField = poseProvider.GetType().GetField("provider");
                    if (nestedProviderField != null)
                    {
                        nestedProviderField.SetValue(poseProvider, poseSolution);
                        Debug.Log("Set nested poseProvider.provider to Pose Solution GameObject");
                    }
                }
            }
        }
        
        Debug.Log("Direct Puppet Connection Fix Complete!");
        EditorUtility.SetDirty(puppetStandardT);
        EditorUtility.SetDirty(poseSolution);
    }
}