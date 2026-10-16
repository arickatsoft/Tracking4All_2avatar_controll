using UnityEngine;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Quick test script to verify Tracking4All component detection works correctly.
    /// This will run automatically and report what components are found.
    /// </summary>
    public class Tracking4AllComponentTester : MonoBehaviour
    {
        private void Start()
        {
            TestComponentDetection();
        }
        
        [ContextMenu("Test Component Detection")]
        public void TestComponentDetection()
        {
            UnityEngine.Debug.Log("🔍 Testing Tracking4All component detection...");
            
            // Search for all MonoBehaviour components
            var allComponents = FindObjectsOfType<MonoBehaviour>();
            
            UnityEngine.Debug.Log($"Total MonoBehaviour components in scene: {allComponents.Length}");
            
            int posePuppetCount = 0;
            int tracking4AllCount = 0;
            int otherTrackingComponents = 0;
            
            foreach (var comp in allComponents)
            {
                var typeName = comp.GetType().Name;
                var fullTypeName = comp.GetType().FullName;
                
                // Check for PosePuppet
                if (typeName == "PosePuppet" || fullTypeName.Contains("PosePuppet"))
                {
                    posePuppetCount++;
                    UnityEngine.Debug.Log($"✅ Found PosePuppet: {comp.name} ({fullTypeName})");
                }
                // Check for main Tracking4All component
                else if (typeName == "Tracking4All" || fullTypeName.Contains("Tracking4All.Tracking4All"))
                {
                    tracking4AllCount++;
                    UnityEngine.Debug.Log($"✅ Found Tracking4All: {comp.name} ({fullTypeName})");
                }
                // Check for other Tracking4All related components
                else if (fullTypeName.Contains("Tracking4All") || typeName.Contains("Puppet") || typeName.Contains("Joint"))
                {
                    otherTrackingComponents++;
                    UnityEngine.Debug.Log($"🔍 Found related component: {comp.name} ({fullTypeName})");
                }
            }
            
            UnityEngine.Debug.Log($"📊 Detection Results:");
            UnityEngine.Debug.Log($"   PosePuppet components: {posePuppetCount}");
            UnityEngine.Debug.Log($"   Tracking4All components: {tracking4AllCount}");
            UnityEngine.Debug.Log($"   Other Tracking4All related: {otherTrackingComponents}");
            
            if (posePuppetCount > 0 || tracking4AllCount > 0)
            {
                UnityEngine.Debug.Log("✅ Tracking4All components detected successfully!");
            }
            else
            {
                UnityEngine.Debug.LogWarning("❌ No main Tracking4All components found!");
            }
        }
    }
}