// DetailedStartupLogger.cs
// Logs detailed startup sequence for debugging dual-avatar issue

using UnityEngine;
using Mediapipe.Unity.Sample;

namespace Tracking4All.Debug
{
    /// <summary>
    /// Add this component to both MPUPoseTrackingSolution GameObjects to track startup sequence
    /// </summary>
    public class DetailedStartupLogger : MonoBehaviour
    {
        [SerializeField] private string solutionName = "Unknown";

        private ImageSourceSolution<Mediapipe.Unity.Sample.PoseTracking.PoseTrackingGraph> solution;

        private void Awake()
        {
            solution = GetComponent<ImageSourceSolution<Mediapipe.Unity.Sample.PoseTracking.PoseTrackingGraph>>();
            UnityEngine.Debug.Log($"[{solutionName}] Awake");
        }

        private void Start()
        {
            UnityEngine.Debug.Log($"[{solutionName}] Start");
            InvokeRepeating(nameof(CheckStatus), 0.5f, 0.5f);
        }

        private void OnEnable()
        {
            UnityEngine.Debug.Log($"[{solutionName}] OnEnable");
        }

        private void CheckStatus()
        {
            if (solution != null)
            {
                var bootstrap = solution.bootstrap;

                UnityEngine.Debug.Log($"[{solutionName}] Status Check - Bootstrap: {(bootstrap != null ? (bootstrap.isFinished ? "Ready" : "Waiting") : "Null")}");
            }
        }
    }
}
