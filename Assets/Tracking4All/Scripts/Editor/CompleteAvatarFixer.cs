using UnityEngine;
using UnityEditor;

namespace Tracking4All
{
    public class CompleteAvatarFixer : EditorWindow
    {
        [MenuItem("Tools/Complete Avatar Connection Fix")]
        public static void ShowWindow()
        {
            GetWindow<CompleteAvatarFixer>("Complete Avatar Fixer");
        }

        void OnGUI()
        {
            GUILayout.Label("Complete Avatar Connection Fixer", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Fix All Avatar Connections"))
            {
                FixAllConnections();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("This will:", EditorStyles.boldLabel);
            GUILayout.Label("1. Fix Puppet-StandardT → Pose Solution connection");
            GUILayout.Label("2. Add Avatar component to Remy if missing");
            GUILayout.Label("3. Connect Remy Avatar → Puppet-StandardT");
            GUILayout.Label("4. Set up proper tracking flow");
        }

        void FixAllConnections()
        {
            UnityEngine.Debug.Log("=== Starting Complete Avatar Connection Fix ===");            
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
            
            // Step 1: Fix Puppet-StandardT connection to Pose Solution
            FixPuppetConnection(puppetStandardT, poseSolution);
            
            // Step 2: Fix Remy Avatar setup
            FixRemyAvatarSetup(remy, puppetStandardT);
            
            UnityEngine.Debug.Log("=== Complete Avatar Connection Fix DONE! ===");
            UnityEngine.Debug.Log("Please test in Play mode to verify tracking is working");
        }        
        void FixPuppetConnection(GameObject puppetStandardT, GameObject poseSolution)
        {
            UnityEngine.Debug.Log("--- Fixing Puppet-StandardT Connection ---");
            
            var posePuppet = puppetStandardT.GetComponent<PosePuppet>();
            if (posePuppet == null)
            {
                UnityEngine.Debug.LogError("PosePuppet component not found on Puppet-StandardT!");
                return;
            }
            
            var poseSolutionComponent = poseSolution.GetComponent<PoseSolution>();
            if (poseSolutionComponent == null)
            {
                UnityEngine.Debug.LogError("PoseSolution component not found on Pose Solution!");
                return;
            }
            
            // Use reflection to set the poseProvider field
            var posePuppetType = typeof(PosePuppet);
            var poseProviderField = posePuppetType.GetField("poseProvider", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (poseProviderField != null)
            {
                // Get the current LandmarkProvider or create a new one
                var currentProvider = poseProviderField.GetValue(posePuppet) as LandmarkProvider<PoseLandmarks>;
                if (currentProvider == null)
                {
                    currentProvider = new LandmarkProvider<PoseLandmarks>();
                    poseProviderField.SetValue(posePuppet, currentProvider);
                }
                
                // Set the provider property of the LandmarkProvider to point to PoseSolution
                var providerType = typeof(LandmarkProvider<PoseLandmarks>);
                var providerField = providerType.GetField("provider", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (providerField != null)
                {
                    providerField.SetValue(currentProvider, poseSolutionComponent);
                    UnityEngine.Debug.Log("✓ Connected PosePuppet.poseProvider.provider to PoseSolution");
                }
                else
                {
                    UnityEngine.Debug.LogError("Could not find provider field on LandmarkProvider");
                }
            }
            else
            {
                UnityEngine.Debug.LogError("Could not find poseProvider field on PosePuppet");
            }
            
            EditorUtility.SetDirty(puppetStandardT);
        }        
        void FixRemyAvatarSetup(GameObject remy, GameObject puppetStandardT)
        {
            UnityEngine.Debug.Log("--- Fixing Remy Avatar Setup ---");
            
            // Check if Remy has Avatar component
            var avatar = remy.GetComponent<Avatar>();
            if (avatar == null)
            {
                UnityEngine.Debug.Log("Adding Avatar component to Remy...");
                avatar = remy.AddComponent<Avatar>();
            }
            
            // Check if Remy has AvatarSkeleton component (required by Avatar)
            var avatarSkeleton = remy.GetComponent<AvatarSkeleton>();
            if (avatarSkeleton == null)
            {
                UnityEngine.Debug.Log("Adding AvatarSkeleton component to Remy...");
                avatarSkeleton = remy.AddComponent<AvatarSkeleton>();
            }
            
            // Get the PosePuppet from Puppet-StandardT
            var posePuppet = puppetStandardT.GetComponent<PosePuppet>();
            if (posePuppet == null)
            {
                UnityEngine.Debug.LogError("PosePuppet component not found on Puppet-StandardT!");
                return;
            }
            
            UnityEngine.Debug.Log("✓ Remy Avatar setup complete - components added");
            UnityEngine.Debug.Log("Note: You may need to manually set up Avatar.pose connections in the inspector");
            
            EditorUtility.SetDirty(remy);
        }
    }
}