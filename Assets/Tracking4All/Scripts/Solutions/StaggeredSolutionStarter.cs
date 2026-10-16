// StaggeredSolutionStarter.cs
// Delays solution startup to avoid simultaneous initialization conflicts

using System.Collections;
using UnityEngine;
using Mediapipe.Unity.Sample;

namespace Tracking4All
{
    /// <summary>
    /// Add this to Remy's MPUPoseTrackingSolution to delay its startup
    /// </summary>
    [DefaultExecutionOrder(-50)]  // Start BEFORE Solution scripts to intercept their Start()
    public class StaggeredSolutionStarter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Delay in seconds before allowing this solution to start")]
        private float startupDelay = 2f;

        private Solution solution;
        private bool hasStarted = false;

        private void Awake()
        {
            // Get the Solution component
            solution = GetComponent<Solution>();
            if (solution == null)
            {
                UnityEngine.Debug.LogError("[StaggeredStarter] No Solution component found!");
                enabled = false;
                return;
            }

            #if UNITY_EDITOR
            UnityEngine.Debug.Log($"[StaggeredStarter] Will delay solution start by {startupDelay}s");
            #endif
        }

        private IEnumerator Start()
        {
            // Disable the Solution to prevent its Start() from running
            if (solution.enabled)
            {
                solution.enabled = false;
                #if UNITY_EDITOR
                UnityEngine.Debug.Log($"[StaggeredStarter] Solution disabled, waiting {startupDelay}s...");
                #endif
            }

            // Wait for the delay
            yield return new WaitForSeconds(startupDelay);

            // Re-enable to trigger the Solution's Start() coroutine
            if (solution != null && !hasStarted)
            {
                hasStarted = true;
                #if UNITY_EDITOR
                UnityEngine.Debug.Log($"[StaggeredStarter] Re-enabling solution now");
                #endif
                var pose = GetComponentInParent<PoseSolution>();
                // A delayed baseline startup must not revive MediaPipe behind an active RTMPose provider.
                if (pose != null && pose.ActiveRTMPose != null && pose.ActiveRTMPose.isActiveAndEnabled) yield break;
                solution.enabled = true;
                #if UNITY_EDITOR
                UnityEngine.Debug.Log($"[StaggeredStarter] Solution started");
                #endif
            }
        }
    }
}
