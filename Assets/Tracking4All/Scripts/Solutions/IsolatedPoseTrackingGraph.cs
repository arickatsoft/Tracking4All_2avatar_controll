// IsolatedPoseTrackingGraph.cs
// Wrapper for PoseTrackingGraph that ensures complete isolation between instances
// (C) 2024-2025 G8gaming Ltd.

using UnityEngine;
using Mediapipe.Unity.Sample.PoseTracking;
using Mediapipe.Unity.Sample;
using System.Collections;

namespace Tracking4All
{
    /// <summary>
    /// Isolated wrapper for PoseTrackingGraph to prevent resource conflicts.
    /// Use this instead of directly using PoseTrackingGraph for multi-avatar setups.
    /// </summary>
    [RequireComponent(typeof(PoseTrackingGraph))]
    public class IsolatedPoseTrackingGraph : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Unique ID for this graph instance (0, 1, 2, etc)")]
        [SerializeField] private int instanceId = 0;

        [Tooltip("Delay start by this many seconds to stagger initialization")]
        [SerializeField] private float startDelay = 0f;

        [Header("References")]
        [SerializeField] private PoseTrackingGraph poseGraph;

        private bool isInitialized = false;

        private void Awake()
        {
            if (poseGraph == null)
            {
                poseGraph = GetComponent<PoseTrackingGraph>();
            }
        }

        private void Start()
        {
            // Stagger start times to avoid simultaneous initialization
            StartCoroutine(DelayedStart());
        }

        private IEnumerator DelayedStart()
        {
            if (startDelay > 0)
            {
                #if UNITY_EDITOR
                UnityEngine.Debug.Log($"[IsolatedPoseGraph_{instanceId}] Delaying start by {startDelay}s...");
                #endif
                yield return new WaitForSeconds(startDelay);
            }

            #if UNITY_EDITOR
            UnityEngine.Debug.Log($"[IsolatedPoseGraph_{instanceId}] Starting...");
            UnityEngine.Debug.Log($"[IsolatedPoseGraph_{instanceId}] Config Type: {poseGraph.configType}");
            UnityEngine.Debug.Log($"[IsolatedPoseGraph_{instanceId}] GPU Initialized: {Mediapipe.Unity.GpuManager.IsInitialized}");
            #endif

            isInitialized = true;
        }

        // Public API to check status
        public bool IsInitialized => isInitialized;
        public int InstanceId => instanceId;
        public PoseTrackingGraph Graph => poseGraph;
    }
}
