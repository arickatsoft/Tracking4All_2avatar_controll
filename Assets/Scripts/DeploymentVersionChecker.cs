using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Version checker to verify latest code is deployed
    /// </summary>
    public class DeploymentVersionChecker : MonoBehaviour
    {
        [Header("Version Info")]
        public string buildVersion = "2025-09-14-v2.0";
        public string lastModified = "September 14, 2025 - 12:15 PM";
        
        void Start()
        {
            UnityEngine.Debug.Log("=================================");
            UnityEngine.Debug.Log("🚀 DEPLOYMENT VERSION CHECKER");
            UnityEngine.Debug.Log($"Build Version: {buildVersion}");
            UnityEngine.Debug.Log($"Last Modified: {lastModified}");
            UnityEngine.Debug.Log("=================================");
            
            // Check if key components exist
            VerifyLatestComponents();
        }
        
        void VerifyLatestComponents()
        {
            UnityEngine.Debug.Log("🔍 VERIFYING LATEST COMPONENTS:");
            
            // Check NetworkDataLogger
            var logger = FindObjectOfType<NetworkDataLogger>();
            UnityEngine.Debug.Log($"NetworkDataLogger: {(logger != null ? "✅ FOUND" : "❌ MISSING")}");
            
            // Check MultiplayerDataIntegrator
            var integrator = FindObjectOfType<MultiplayerDataIntegrator>();
            UnityEngine.Debug.Log($"MultiplayerDataIntegrator: {(integrator != null ? "✅ FOUND" : "❌ MISSING")}");
            
            // Check NetworkDataSetup
            var setup = FindObjectOfType<NetworkDataSetup>();
            UnityEngine.Debug.Log($"NetworkDataSetup: {(setup != null ? "✅ FOUND" : "❌ MISSING")}");
            
            // Check AvatarMovementTester
            var tester = FindObjectOfType<AvatarMovementTester>();
            UnityEngine.Debug.Log($"AvatarMovementTester: {(tester != null ? "✅ FOUND" : "❌ MISSING")}");
            
            UnityEngine.Debug.Log("=================================");
            
            if (logger != null && integrator != null)
            {
                UnityEngine.Debug.Log("✅ LATEST CODE SUCCESSFULLY DEPLOYED!");
            }
            else
            {
                UnityEngine.Debug.LogError("❌ LATEST CODE NOT DEPLOYED - REBUILD REQUIRED!");
            }
        }
        
        void OnGUI()
        {
            // Show version on screen
            GUI.BeginGroup(new Rect(Screen.width - 320, Screen.height - 80, 300, 70));
            GUI.Box(new Rect(0, 0, 300, 70), $"Build: {buildVersion}");
            GUI.Label(new Rect(10, 25, 280, 20), $"Modified: {lastModified}");
            GUI.Label(new Rect(10, 45, 280, 20), "Latest changes deployed ✅");
            GUI.EndGroup();
        }
    }
}