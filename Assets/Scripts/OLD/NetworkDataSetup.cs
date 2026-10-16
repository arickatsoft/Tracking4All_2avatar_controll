using UnityEngine;
using Tracking4All;

namespace Tracking4All
{
    /// <summary>
    /// Easy setup script for network data logging and testing
    /// Add this to any GameObject in your scene to activate the networking logging system
    /// </summary>
    public class NetworkDataSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;
        
        [Header("Manual Controls")]
        [SerializeField] private KeyCode testHostModeKey = KeyCode.H;
        [SerializeField] private KeyCode testClientModeKey = KeyCode.C;
        [SerializeField] private KeyCode generateTestDataKey = KeyCode.T;
        
        void Start()
        {
            if (setupOnStart)
            {
                SetupNetworkingComponents();
            }
        }
        
        void Update()
        {
            // Manual testing controls
            if (Input.GetKeyDown(testHostModeKey))
            {
                TestHostMode();
            }
            
            if (Input.GetKeyDown(testClientModeKey))
            {
                TestClientMode();
            }
            
            if (Input.GetKeyDown(generateTestDataKey))
            {
                GenerateTestNetworkData();
            }
        }
        
        /// <summary>
        /// Sets up all necessary components for network data logging
        /// </summary>
        [ContextMenu("Setup Network Components")]
        public void SetupNetworkingComponents()
        {
            UnityEngine.Debug.Log("🚀 SETTING UP NETWORK DATA LOGGING SYSTEM");
            
            // Find or create NetworkDataLogger
            NetworkDataLogger logger = FindObjectOfType<NetworkDataLogger>();
            if (logger == null)
            {
                GameObject loggerObj = new GameObject("NetworkDataLogger");
                logger = loggerObj.AddComponent<NetworkDataLogger>();
                UnityEngine.Debug.Log("✅ Created NetworkDataLogger");
            }
            else
            {
                UnityEngine.Debug.Log("✅ Found existing NetworkDataLogger");
            }
            
            // Find or create MultiplayerDataIntegrator
            MultiplayerDataIntegrator integrator = FindObjectOfType<MultiplayerDataIntegrator>();
            if (integrator == null)
            {
                GameObject integratorObj = new GameObject("MultiplayerDataIntegrator");
                integrator = integratorObj.AddComponent<MultiplayerDataIntegrator>();
                UnityEngine.Debug.Log("✅ Created MultiplayerDataIntegrator");
            }
            else
            {
                UnityEngine.Debug.Log("✅ Found existing MultiplayerDataIntegrator");
            }
            
            // Verify avatars are in scene
            VerifyAvatars();
            
            UnityEngine.Debug.Log("✅ NETWORK SETUP COMPLETE!");
            UnityEngine.Debug.Log("📋 Instructions:");
            UnityEngine.Debug.Log($"   • Press '{testHostModeKey}' to test Host mode");
            UnityEngine.Debug.Log($"   • Press '{testClientModeKey}' to test Client mode");
            UnityEngine.Debug.Log($"   • Press '{generateTestDataKey}' to generate test network data");
            UnityEngine.Debug.Log("   • Click Host/Join buttons in game for actual multiplayer");
        }
        
        void VerifyAvatars()
        {
            GameObject bryce = GameObject.Find("Bryce");
            GameObject remy = GameObject.Find("Remy");
            
            UnityEngine.Debug.Log($"🎭 Avatar Check:");
            UnityEngine.Debug.Log($"   Bryce: {(bryce != null ? "✅ Found" : "❌ Not Found")}");
            UnityEngine.Debug.Log($"   Remy: {(remy != null ? "✅ Found" : "❌ Not Found")}");
            
            if (bryce != null)
            {
                Animator bryceAnimator = bryce.GetComponent<Animator>();
                UnityEngine.Debug.Log($"   Bryce Animator: {(bryceAnimator != null ? "✅" : "❌")}");
                if (bryceAnimator != null && bryceAnimator.avatar != null)
                {
                    UnityEngine.Debug.Log($"   Bryce Human: {(bryceAnimator.avatar.isHuman ? "✅" : "❌")}");
                }
            }
            
            if (remy != null)
            {
                Animator remyAnimator = remy.GetComponent<Animator>();
                UnityEngine.Debug.Log($"   Remy Animator: {(remyAnimator != null ? "✅" : "❌")}");
                if (remyAnimator != null && remyAnimator.avatar != null)
                {
                    UnityEngine.Debug.Log($"   Remy Human: {(remyAnimator.avatar.isHuman ? "✅" : "❌")}");
                }
            }
        }
        
        void TestHostMode()
        {
            UnityEngine.Debug.Log("🎮 TESTING HOST MODE (H key pressed)");
            
            MultiplayerDataIntegrator integrator = FindObjectOfType<MultiplayerDataIntegrator>();
            if (integrator != null)
            {
                integrator.OnHostButtonClicked();
            }
            else
            {
                UnityEngine.Debug.LogError("MultiplayerDataIntegrator not found! Run Setup first.");
            }
        }
        
        void TestClientMode()
        {
            UnityEngine.Debug.Log("🎮 TESTING CLIENT MODE (C key pressed)");
            
            MultiplayerDataIntegrator integrator = FindObjectOfType<MultiplayerDataIntegrator>();
            if (integrator != null)
            {
                integrator.OnJoinButtonClicked();
            }
            else
            {
                UnityEngine.Debug.LogError("MultiplayerDataIntegrator not found! Run Setup first.");
            }
        }
        
        void GenerateTestNetworkData()
        {
            UnityEngine.Debug.Log("📦 GENERATING TEST NETWORK DATA (T key pressed)");
            
            MultiplayerDataIntegrator integrator = FindObjectOfType<MultiplayerDataIntegrator>();
            if (integrator != null)
            {
                integrator.TestNetworkDataFlow();
            }
            else
            {
                UnityEngine.Debug.LogError("MultiplayerDataIntegrator not found! Run Setup first.");
            }
        }
        
        /// <summary>
        /// Complete system test - simulates the full multiplayer flow
        /// </summary>
        [ContextMenu("Run Full System Test")]
        public void RunFullSystemTest()
        {
            UnityEngine.Debug.Log("🧪 RUNNING FULL SYSTEM TEST");
            
            // Setup if not done
            SetupNetworkingComponents();
            
            // Wait a bit then test host mode
            StartCoroutine(DelayedSystemTest());
        }
        
        System.Collections.IEnumerator DelayedSystemTest()
        {
            yield return new WaitForSeconds(1f);
            
            UnityEngine.Debug.Log("🎭 Testing Host Mode...");
            TestHostMode();
            
            yield return new WaitForSeconds(3f);
            
            UnityEngine.Debug.Log("🎭 Testing Client Mode...");
            TestClientMode();
            
            yield return new WaitForSeconds(3f);
            
            UnityEngine.Debug.Log("📦 Testing Network Data...");
            GenerateTestNetworkData();
            
            yield return new WaitForSeconds(2f);
            
            UnityEngine.Debug.Log("✅ FULL SYSTEM TEST COMPLETED");
        }
        
        void OnGUI()
        {
            // Simple on-screen instructions
            GUI.BeginGroup(new Rect(10, 10, 300, 120));
            GUI.Box(new Rect(0, 0, 300, 120), "Network Data Logger Controls");
            
            GUI.Label(new Rect(10, 25, 280, 20), $"H - Test Host Mode");
            GUI.Label(new Rect(10, 45, 280, 20), $"C - Test Client Mode");
            GUI.Label(new Rect(10, 65, 280, 20), $"T - Generate Test Data");
            GUI.Label(new Rect(10, 85, 280, 20), $"Or use Host/Join buttons");
            
            GUI.EndGroup();
        }
    }
}