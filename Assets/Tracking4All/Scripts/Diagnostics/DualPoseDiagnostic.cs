// DualPoseDiagnostic.cs
// Diagnostic script to debug dual-avatar MediaPipe issue
// (C) 2024-2025 G8gaming Ltd.

using UnityEngine;
using Mediapipe.Unity.Sample.PoseTracking;
using Mediapipe.Unity.Sample;

namespace Tracking4All.Diagnostics
{
    /// <summary>
    /// Attach this to a GameObject in your scene to diagnose why both PoseTrackingGraphs aren't working simultaneously.
    /// </summary>
    public class DualPoseDiagnostic : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Drag Remy's PoseTrackingGraph here")]
        public PoseTrackingGraph remyGraph;

        [Tooltip("Drag Bryce's PoseTrackingGraph here")]
        public PoseTrackingGraph bryceGraph;

        [Header("Diagnostic Info")]
        [SerializeField] private bool remyIsRunning = false;
        [SerializeField] private bool bryceIsRunning = false;
        [SerializeField] private string remyConfigType = "";
        [SerializeField] private string bryceConfigType = "";
        [SerializeField] private bool gpuManagerInitialized = false;
        [SerializeField] private int activeGraphCount = 0;

        private float lastUpdateTime = 0f;
        private int remyUpdateCount = 0;
        private int bryceUpdateCount = 0;
        private int lastRemyCount = 0;
        private int lastBryceCount = 0;

        private void Start()
        {
            if (remyGraph == null || bryceGraph == null)
            {
                UnityEngine.Debug.LogError("[DualPoseDiagnostic] Please assign both Remy and Bryce PoseTrackingGraph references in the Inspector!");
                return;
            }

            InvokeRepeating(nameof(UpdateDiagnostics), 1f, 1f);
        }

        private void UpdateDiagnostics()
        {
            // Check GPU Manager state
            gpuManagerInitialized = Mediapipe.Unity.GpuManager.IsInitialized;

            // Check config types
            remyConfigType = remyGraph.configType.ToString();
            bryceConfigType = bryceGraph.configType.ToString();

            // Check if graphs are running (reflection needed for private field)
            remyIsRunning = CheckIfRunning(remyGraph);
            bryceIsRunning = CheckIfRunning(bryceGraph);

            // Count active graphs
            activeGraphCount = (remyIsRunning ? 1 : 0) + (bryceIsRunning ? 1 : 0);

            // Log status every 2 seconds
            if (Time.time - lastUpdateTime >= 2f)
            {
                lastUpdateTime = Time.time;
                LogStatus();
            }
        }

        private bool CheckIfRunning(GraphRunner graph)
        {
            // Use reflection to access private _isRunning field
            var field = typeof(GraphRunner).GetField("_isRunning",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                return (bool)field.GetValue(graph);
            }

            return false;
        }

        private void LogStatus()
        {
            UnityEngine.Debug.Log($"[DualPoseDiagnostic] === STATUS REPORT ===");
            UnityEngine.Debug.Log($"  GPU Manager Initialized: {gpuManagerInitialized}");
            UnityEngine.Debug.Log($"  Remy Config: {remyConfigType}, Running: {remyIsRunning}");
            UnityEngine.Debug.Log($"  Bryce Config: {bryceConfigType}, Running: {bryceIsRunning}");
            UnityEngine.Debug.Log($"  Active Graphs: {activeGraphCount}/2");

            // Check for updates per second
            int remyUPS = remyUpdateCount - lastRemyCount;
            int bryceUPS = bryceUpdateCount - lastBryceCount;
            lastRemyCount = remyUpdateCount;
            lastBryceCount = bryceUpdateCount;

            UnityEngine.Debug.Log($"  Remy Updates/sec: {remyUPS}");
            UnityEngine.Debug.Log($"  Bryce Updates/sec: {bryceUPS}");

            // Diagnose issues
            if (!remyIsRunning && !bryceIsRunning)
            {
                UnityEngine.Debug.LogError("[DualPoseDiagnostic] ❌ BOTH graphs are NOT running!");
            }
            else if (remyIsRunning && !bryceIsRunning)
            {
                UnityEngine.Debug.LogWarning("[DualPoseDiagnostic] ⚠️ Only REMY is running!");
            }
            else if (!remyIsRunning && bryceIsRunning)
            {
                UnityEngine.Debug.LogWarning("[DualPoseDiagnostic] ⚠️ Only BRYCE is running!");
            }
            else
            {
                if (remyUPS == 0 && bryceUPS == 0)
                {
                    UnityEngine.Debug.LogWarning("[DualPoseDiagnostic] ⚠️ Both graphs running but NO UPDATES!");
                }
                else if (remyUPS == 0)
                {
                    UnityEngine.Debug.LogWarning("[DualPoseDiagnostic] ⚠️ Remy running but NOT UPDATING!");
                }
                else if (bryceUPS == 0)
                {
                    UnityEngine.Debug.LogWarning("[DualPoseDiagnostic] ⚠️ Bryce running but NOT UPDATING!");
                }
                else
                {
                    UnityEngine.Debug.Log("[DualPoseDiagnostic] ✅ Both graphs running and updating!");
                }
            }
            UnityEngine.Debug.Log($"[DualPoseDiagnostic] ==================");
        }

        // Track updates (call this from your pose solution when it receives data)
        public void NotifyRemyUpdate()
        {
            remyUpdateCount++;
        }

        public void NotifyBryceUpdate()
        {
            bryceUpdateCount++;
        }

        // Alternative: Subscribe to pose landmark events if available
        private void OnEnable()
        {
            // You can subscribe to events here if your PoseSolution exposes them
        }

        private void OnDisable()
        {
            // Unsubscribe from events
        }
    }
}
