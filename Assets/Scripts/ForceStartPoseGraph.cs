using UnityEngine;
using Mediapipe.Unity.Sample.PoseTracking;
using System.Reflection;
using System.Linq;

namespace Tracking4All
{
    /// <summary>
    /// Forces a PoseTrackingGraph to start and run without requiring a camera source.
    /// Use this when pose data comes from UDP instead of a camera.
    /// </summary>
    public class ForceStartPoseGraph : MonoBehaviour
    {
        [Header("Target Graph")]
        [Tooltip("Drag Remy's PoseTrackingGraph GameObject here")]
        [SerializeField] private PoseTrackingGraph poseGraph;
        
        [Header("Options")]
        [Tooltip("Automatically start the graph on Start()")]
        [SerializeField] private bool autoStart = true;
        
        [Tooltip("Keep trying to start if it fails initially")]
        [SerializeField] private bool retryOnFailure = true;
        
        [SerializeField] private float retryInterval = 1f;
        
        [Header("Status")]
        [SerializeField] private bool isRunning = false;
        [SerializeField] private int startAttempts = 0;
        
        private void Start()
        {
            if (autoStart)
            {
                StartGraph();
            }
        }
        
        public void StartGraph()
        {
            if (poseGraph == null)
            {
                UnityEngine.Debug.LogError("[ForceStartPoseGraph] PoseTrackingGraph not assigned!");
                return;
            }
            
            startAttempts++;
            
            try
            {
                // Try to start the graph (using reflection since API varies)
                TryStartGraph();
                
                // Wait a moment for it to initialize
                Invoke(nameof(CheckStartSuccess), 0.5f);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[ForceStartPoseGraph] ❌ Failed to start graph: {e.Message}");
                
                if (retryOnFailure && startAttempts < 5)
                {
                    Invoke(nameof(StartGraph), retryInterval);
                }
            }
        }
        
        private void TryStartGraph()
        {
            // Try different methods to start the graph
            var graphType = poseGraph.GetType();
            
            // Method 1: Try all Play() overloads
            var playMethods = graphType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "Play").ToArray();
            
            foreach (var method in playMethods)
            {
                var parameters = method.GetParameters();
                UnityEngine.Debug.Log($"[ForceStartPoseGraph] Found Play() with {parameters.Length} parameters");
                
                try
                {
                    if (parameters.Length == 0)
                    {
                        UnityEngine.Debug.Log("[ForceStartPoseGraph] Calling Play() with no parameters");
                        method.Invoke(poseGraph, null);
                        return;
                    }
                    else if (parameters.Length == 1)
                    {
                        // Try with null parameter
                        UnityEngine.Debug.Log($"[ForceStartPoseGraph] Calling Play() with 1 parameter (type: {parameters[0].ParameterType.Name})");
                        object[] args = new object[] { null };
                        method.Invoke(poseGraph, args);
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[ForceStartPoseGraph] Play() failed: {e.Message}");
                }
            }
            
            // Method 2: Try all Run() overloads
            var runMethods = graphType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "Run").ToArray();
            
            foreach (var method in runMethods)
            {
                var parameters = method.GetParameters();
                UnityEngine.Debug.Log($"[ForceStartPoseGraph] Found Run() with {parameters.Length} parameters");
                
                try
                {
                    if (parameters.Length == 0)
                    {
                        UnityEngine.Debug.Log("[ForceStartPoseGraph] Calling Run() with no parameters");
                        method.Invoke(poseGraph, null);
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[ForceStartPoseGraph] Run() failed: {e.Message}");
                }
            }
            
            // Method 3: Try StartRun()
            var startRunMethods = graphType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "StartRun").ToArray();
            
            foreach (var method in startRunMethods)
            {
                var parameters = method.GetParameters();
                
                try
                {
                    if (parameters.Length == 0)
                    {
                        UnityEngine.Debug.Log("[ForceStartPoseGraph] Calling StartRun()");
                        method.Invoke(poseGraph, null);
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[ForceStartPoseGraph] StartRun() failed: {e.Message}");
                }
            }
            
            // Method 4: Try enabling the component
            try
            {
                var enabledProp = graphType.GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
                if (enabledProp != null)
                {
                    UnityEngine.Debug.Log("[ForceStartPoseGraph] Enabling component");
                    enabledProp.SetValue(poseGraph, true);
                    return;
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[ForceStartPoseGraph] Enable failed: {e.Message}");
            }
            
            UnityEngine.Debug.LogError("[ForceStartPoseGraph] Could not find a working method to start the graph!");
        }
        
        private void CheckStartSuccess()
        {
            try { 
            // Check if it actually started
            isRunning = CheckIfRunning();
                
                if (isRunning)
                {
                    UnityEngine.Debug.Log($"<color=green>[ForceStartPoseGraph] ✅ Successfully started pose graph '{poseGraph.name}'</color>");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[ForceStartPoseGraph] ⚠️ Called Play() but graph not running yet (attempt {startAttempts})");
                    
                    if (retryOnFailure)
                    {
                        Invoke(nameof(StartGraph), retryInterval);
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[ForceStartPoseGraph] ❌ Failed to start graph: {e.Message}");
                
                if (retryOnFailure && startAttempts < 5)
                {
                    Invoke(nameof(StartGraph), retryInterval);
                }
            }
        }
        
        public void StopGraph()
        {
            if (poseGraph == null) return;
            
            try
            {
                var graphType = poseGraph.GetType();
                
                // Try Stop() method
                var stopMethod = graphType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
                if (stopMethod != null)
                {
                    stopMethod.Invoke(poseGraph, null);
                    isRunning = false;
                    UnityEngine.Debug.Log("[ForceStartPoseGraph] Stopped pose graph");
                    return;
                }
                
                // Try disabling the component
                var enabledProp = graphType.GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
                if (enabledProp != null)
                {
                    enabledProp.SetValue(poseGraph, false);
                    isRunning = false;
                    UnityEngine.Debug.Log("[ForceStartPoseGraph] Disabled pose graph");
                }
            }
            catch (System.Exception e)
            {
               UnityEngine.Debug.LogError($"[ForceStartPoseGraph] Error stopping graph: {e.Message}");
            }
        }
        
        private bool CheckIfRunning()
        {
            if (poseGraph == null) return false;
            
            // Use reflection to check the private _isRunning field
            var field = typeof(Mediapipe.Unity.Sample.GraphRunner).GetField("_isRunning",
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (field != null)
            {
                return (bool)field.GetValue(poseGraph);
            }
            
            return false;
        }
        
        private void Update()
        {
            // Update running status
            if (poseGraph != null)
            {
                isRunning = CheckIfRunning();
            }
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 550, 300, 100));
            GUILayout.Label("<b>Force Start Pose Graph</b>");
            GUILayout.Label($"Graph: {(poseGraph != null ? poseGraph.name : "Not assigned")}");
            GUILayout.Label($"Running: {isRunning}");
            GUILayout.Label($"Start attempts: {startAttempts}");
            
            if (!isRunning && GUILayout.Button("Try Start Now"))
            {
                StartGraph();
            }
            
            if (isRunning && GUILayout.Button("Stop"))
            {
                StopGraph();
            }
            
            GUILayout.EndArea();
        }
    }
}
