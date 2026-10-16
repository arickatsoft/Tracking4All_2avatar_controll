using UnityEngine;
using UnityEditor;

namespace Tracking4All
{
    public class SimpleAvatarFixer : EditorWindow
    {
        [MenuItem("Tools/Simple Avatar Connection Fix")]
        public static void ShowWindow()
        {
            GetWindow<SimpleAvatarFixer>("Simple Avatar Fixer");
        }

        void OnGUI()
        {
            GUILayout.Label("Simple Avatar Connection Fixer", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Fix Basic Connections"))
            {
                FixBasicConnections();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Instructions:", EditorStyles.boldLabel);
            GUILayout.Label("1. Run this to add missing components");
            GUILayout.Label("2. Manually set connections in Inspector:");
            GUILayout.Label("   - Select Puppet-StandardT");
            GUILayout.Label("   - Drag 'Pose Solution' to poseProvider.provider");
            GUILayout.Label("   - Select Remy");
            GUILayout.Label("   - Set Avatar component connections");
        }

        void FixBasicConnections()
        {
            UnityEngine.Debug.Log("=== Starting Simple Avatar Connection Fix ===");
            
            // Find required GameObjects
            GameObject remy = GameObject.Find("Remy");
            GameObject puppetStandardT = GameObject.Find("Puppet-StandardT");
            GameObject poseSolution = GameObject.Find("Pose Solution");
            
            if (remy == null)
            {
                UnityEngine.Debug.LogError("Remy not found in scene!");
                return;
            }
            
            if (puppetStandardT == null)
            {
                UnityEngine.Debug.LogError("Puppet-StandardT not found in scene!");
                return;
            }
            
            if (poseSolution == null)
            {
                UnityEngine.Debug.LogError("Pose Solution not found in scene!");
                return;
            }
            
            UnityEngine.Debug.Log("Found all required GameObjects");
            
            // Add missing components to Remy
            AddRemyComponents(remy);
            
            UnityEngine.Debug.Log("=== Simple Avatar Connection Fix DONE! ===");
            UnityEngine.Debug.Log("Now manually set up connections in Inspector:");
            UnityEngine.Debug.Log("1. Select Puppet-StandardT in hierarchy");
            UnityEngine.Debug.Log("2. In PosePuppet component, expand 'poseProvider'");
            UnityEngine.Debug.Log("3. Drag 'Pose Solution' to the 'provider' field");
            UnityEngine.Debug.Log("4. Select Remy in hierarchy");
            UnityEngine.Debug.Log("5. Set up Avatar component connections to Puppet-StandardT");
        }
        
        void AddRemyComponents(GameObject remy)
        {
            UnityEngine.Debug.Log("--- Adding Components to Remy ---");
            
            // Check if Remy has Avatar component
            var avatar = remy.GetComponent<Avatar>();
            if (avatar == null)
            {
                UnityEngine.Debug.Log("Adding Avatar component to Remy...");
                avatar = remy.AddComponent<Avatar>();
            }
            else
            {
                UnityEngine.Debug.Log("Avatar component already exists on Remy");
            }
            
            // Check if Remy has AvatarSkeleton component (required by Avatar)
            var avatarSkeleton = remy.GetComponent<AvatarSkeleton>();
            if (avatarSkeleton == null)
            {
                UnityEngine.Debug.Log("Adding AvatarSkeleton component to Remy...");
                avatarSkeleton = remy.AddComponent<AvatarSkeleton>();
            }
            else
            {
                UnityEngine.Debug.Log("AvatarSkeleton component already exists on Remy");
            }
            
            UnityEngine.Debug.Log("✓ Remy components setup complete");
            EditorUtility.SetDirty(remy);
        }
    }
}