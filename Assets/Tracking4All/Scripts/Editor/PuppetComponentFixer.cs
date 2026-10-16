using UnityEngine;
using UnityEditor;

namespace Tracking4All
{
    public class PuppetComponentFixer : EditorWindow
    {
        [MenuItem("Tools/Fix Puppet Components")]
        public static void ShowWindow()
        {
            GetWindow<PuppetComponentFixer>("Puppet Component Fixer");
        }

        void OnGUI()
        {
            GUILayout.Label("Puppet Component Fixer", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Add Missing PosePuppet Component"))
            {
                FixPuppetComponents();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("This will:", EditorStyles.boldLabel);
            GUILayout.Label("1. Add PosePuppet component to Puppet-StandardT");
            GUILayout.Label("2. Check what components are currently on it");
            GUILayout.Label("3. Provide instructions for manual setup");
        }

        void FixPuppetComponents()
        {
            UnityEngine.Debug.Log("=== Fixing Puppet Components ===");
            
            // Find Puppet-StandardT
            GameObject puppetStandardT = GameObject.Find("Puppet-StandardT");
            
            if (puppetStandardT == null)
            {
                UnityEngine.Debug.LogError("Puppet-StandardT not found in scene!");
                return;
            }
            
            UnityEngine.Debug.Log("Found Puppet-StandardT");
            
            // List current components
            var components = puppetStandardT.GetComponents<Component>();
            UnityEngine.Debug.Log($"Current components on Puppet-StandardT: {components.Length}");
            foreach (var comp in components)
            {
                if (comp != null)
                    UnityEngine.Debug.Log($"  - {comp.GetType().Name}");
                else
                    UnityEngine.Debug.Log("  - Missing Script (null)");
            }
            
            // Check if PosePuppet exists
            var posePuppet = puppetStandardT.GetComponent<PosePuppet>();
            if (posePuppet == null)
            {
                UnityEngine.Debug.Log("Adding PosePuppet component...");
                posePuppet = puppetStandardT.AddComponent<PosePuppet>();
                UnityEngine.Debug.Log("✓ PosePuppet component added");
            }
            else
            {
                UnityEngine.Debug.Log("PosePuppet component already exists");
            }
            
            EditorUtility.SetDirty(puppetStandardT);
            
            UnityEngine.Debug.Log("=== Puppet Components Fix Complete ===");
            UnityEngine.Debug.Log("Next steps:");
            UnityEngine.Debug.Log("1. Select Puppet-StandardT in hierarchy");
            UnityEngine.Debug.Log("2. In PosePuppet component, expand 'poseProvider'");
            UnityEngine.Debug.Log("3. Drag 'Pose Solution' to the 'provider' field");
        }
    }
}