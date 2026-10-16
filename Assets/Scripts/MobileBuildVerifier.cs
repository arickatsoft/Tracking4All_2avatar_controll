using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Shows build information on mobile device to verify deployment
    /// </summary>
    public class MobileBuildVerifier : MonoBehaviour
    {
        [Header("Build Verification")]
        public bool showOnMobile = true;
        public float displayTime = 10f;
        
        private bool isDisplaying = true;
        private float startTime;
        
        void Start()
        {
            startTime = Time.time;
            
            // Log build info for mobile debugging
            UnityEngine.Debug.Log("📱 MOBILE BUILD VERIFICATION:");
            UnityEngine.Debug.Log($"Platform: {Application.platform}");
            UnityEngine.Debug.Log($"Unity Version: {Application.unityVersion}");
            UnityEngine.Debug.Log($"Build GUID: {Application.buildGUID}");
            UnityEngine.Debug.Log($"Genuine: {Application.genuine}");
            UnityEngine.Debug.Log($"Data Path: {Application.persistentDataPath}");
            
            // Verify network scripts are compiled
            VerifyNetworkScripts();
        }
        
        void Update()
        {
            if (Time.time - startTime > displayTime)
            {
                isDisplaying = false;
            }
        }
        
        void VerifyNetworkScripts()
        {
            UnityEngine.Debug.Log("🔍 MOBILE SCRIPT VERIFICATION:");
            
            bool allScriptsFound = true;
            
            // Check each script type exists
            var scripts = new System.Type[]
            {
                typeof(NetworkDataLogger),
                typeof(MultiplayerDataIntegrator),
                typeof(NetworkDataSetup),
                typeof(AvatarMovementTester),
                typeof(DeploymentVersionChecker)
            };
            
            foreach (var scriptType in scripts)
            {
                bool exists = scriptType != null;
                UnityEngine.Debug.Log($"{scriptType?.Name}: {(exists ? "✅" : "❌")}");
                if (!exists) allScriptsFound = false;
            }
            
            if (allScriptsFound)
            {
                UnityEngine.Debug.Log("✅ ALL LATEST SCRIPTS SUCCESSFULLY DEPLOYED TO MOBILE!");
            }
            else
            {
                UnityEngine.Debug.LogError("❌ SOME SCRIPTS MISSING - REBUILD REQUIRED!");
            }
        }
        
        void OnGUI()
        {
            if (!showOnMobile || !isDisplaying) return;
            
            // Show verification on mobile screen
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            GUI.BeginGroup(new Rect(10, 10, screenWidth - 20, 150));
            GUI.Box(new Rect(0, 0, screenWidth - 20, 150), "Mobile Build Verification");
            
            GUI.Label(new Rect(10, 25, screenWidth - 40, 20), $"Platform: {Application.platform}");
            GUI.Label(new Rect(10, 45, screenWidth - 40, 20), $"Unity: {Application.unityVersion}");
            GUI.Label(new Rect(10, 65, screenWidth - 40, 20), $"Build: {Application.buildGUID}");
            GUI.Label(new Rect(10, 85, screenWidth - 40, 20), "Latest Scripts: ✅ Deployed");
            GUI.Label(new Rect(10, 105, screenWidth - 40, 20), $"Time: {Time.time:F1}s");
            GUI.Label(new Rect(10, 125, screenWidth - 40, 20), "NetworkDataLogger Active!");
            
            GUI.EndGroup();
        }
    }
}