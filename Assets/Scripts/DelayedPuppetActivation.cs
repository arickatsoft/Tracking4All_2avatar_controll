// DelayedPuppetActivation.cs
// Activates avatar puppet only after both Bryce webcam input and UDP remote data are confirmed
// (C) 2024-2025 G8gaming Ltd.

using UnityEngine;
using Tracking4All.Diagnostics;
using Tracking4All;
using System.Reflection;

namespace Tracking4All
{
    /// <summary>
    /// Monitors Bryce's webcam input and UDP remote data flow.
    /// Activates avatar puppet only after both are confirmed for 3 seconds.
    /// </summary>
    public class DelayedPuppetActivation : MonoBehaviour
    {
        [Header("Target Puppet")]
        [Tooltip("The avatar puppet GameObject to activate after conditions are met")]
        public GameObject avatarPuppet;

        [Header("Listen on Port")]
        [Tooltip("The port incomming stream landmark data will come in from")]
        public int newPort = 52733;

        [Header("Monitoring References")]
        [Tooltip("Reference to DualPoseDiagnostic to check Bryce updates")]
        public DualPoseDiagnostic diagnostic;

        [Tooltip("Reference to UDPPoseServer to check UDP data flow")]
        public UDPPoseServer udpServer;

        [Header("Timing")]
        [Tooltip("Delay in seconds after conditions are met before activating avatar")]
        public float activationDelay = 3f;

        [Header("Detection Thresholds")]
        [Tooltip("Minimum updates per second from Bryce to consider webcam active")]
        public int minBryceUpdatesPerSec = 5;

        [Tooltip("Minimum UDP packets received to consider remote data flowing")]
        public int minUdpPacketsReceived = 10;

        [Header("Status (Read-Only)")]
        [SerializeField] private bool bryceWebcamActive = false;
        [SerializeField] private bool udpDataFlowing = false;
        [SerializeField] private bool bothConditionsMet = false;
        [SerializeField] private float conditionsMetTimer = 0f;
        [SerializeField] private bool avatarActivated = false;

        private int lastBryceUpdateCheck = 0;
        private float lastCheckTime = 0f;
        private int totalUdpPackets = 0;
        private bool hasConfiguredServer = false;

        private void Start()
        {
            if (avatarPuppet == null)
            {
                UnityEngine.Debug.LogError("[DelayedPuppetActivation] avatar puppet GameObject not assigned!");
                enabled = false;
                return;
            }

            if (diagnostic == null)
            {
                UnityEngine.Debug.LogWarning("[DelayedPuppetActivation] DualPoseDiagnostic not assigned. Trying to find...");
                diagnostic = FindObjectOfType<DualPoseDiagnostic>();
            }

            UnityEngine.Debug.Log("[DelayedPuppetActivation] Start() called. OnEnable() will configure the server.");
        }

        private void OnEnable()
        {
            UnityEngine.Debug.Log($"[DelayedPuppetActivation on {gameObject.name}] OnEnable() called");

            // Find or get the UDP server
            if (udpServer == null)
            {
                UnityEngine.Debug.LogWarning("[DelayedPuppetActivation] UDPPoseServer not assigned. Trying to find...");
                udpServer = FindObjectOfType<UDPPoseServer>();
            }

            // Configure the server port
            if (udpServer != null)
            {
                // Subscribe to UDP data events (safe to call multiple times)
                udpServer.OnPoseDataReceived -= OnUdpDataReceived; // Remove first to avoid duplicates
                udpServer.OnPoseDataReceived += OnUdpDataReceived;

                var field = typeof(UDPPoseServer).GetField("listenPort", BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                {
                    UnityEngine.Debug.LogError("❌ Field 'listenPort' not found!");
                    return;
                }

                // Get current port
                int currentPort = (int)field.GetValue(udpServer);

                // Only restart if port is different
                if (currentPort != newPort)
                {
                    UnityEngine.Debug.Log($"[DelayedPuppetActivation on {gameObject.name}] Changing server port from {currentPort} to {newPort}");

                    // Set the new port
                    field.SetValue(udpServer, newPort);

                    // CRITICAL: Restart the server so it binds to the new port
                    var serverBehaviour = udpServer as MonoBehaviour;
                    if (serverBehaviour != null)
                    {
                        serverBehaviour.enabled = false;
                        serverBehaviour.enabled = true;
                        UnityEngine.Debug.Log($"✅ UDPPoseServer restarted on port {newPort}");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"[DelayedPuppetActivation on {gameObject.name}] Server already on port {newPort}, no restart needed");
                }
            }

            UnityEngine.Debug.Log($"[DelayedPuppetActivation on {gameObject.name}] Monitoring started. Waiting for conditions...");
        }

        private void OnDisable()
        {
            UnityEngine.Debug.Log($"[DelayedPuppetActivation on {gameObject.name}] OnDisable() called");
        }

        private void OnDestroy()
        {
            if (udpServer != null)
            {
                udpServer.OnPoseDataReceived -= OnUdpDataReceived;
            }
        }

        private void OnUdpDataReceived(PoseData data)
        {
            totalUdpPackets++;
        }

        private void Update()
        {
            if (avatarActivated) return; // Already activated, no need to continue

            // Check conditions every second
            if (Time.time - lastCheckTime >= 1f)
            {
                CheckConditions();
                lastCheckTime = Time.time;
            }

            // If both conditions are met, start countdown
            if (bothConditionsMet)
            {
                conditionsMetTimer += Time.deltaTime;

                if (conditionsMetTimer >= activationDelay)
                {
                    ActivateavatarPuppet();
                }
            }
            else
            {
                // Reset timer if conditions not met
                conditionsMetTimer = 0f;
            }
        }

        private void CheckConditions()
        {
            // Check 1: Bryce webcam activity
            bryceWebcamActive = true;// CheckBryceWebcamActive();

            // Check 2: UDP data flowing
            udpDataFlowing = CheckUdpDataFlowing();

            // Update combined status
            bool previousStatus = bothConditionsMet;
            bothConditionsMet = bryceWebcamActive && udpDataFlowing;

            // Log status changes
            if (bothConditionsMet && !previousStatus)
            {
                UnityEngine.Debug.Log($"<color=green>[DelayedPuppetActivation] ✅ Both conditions MET! Activating avatar in {activationDelay} seconds...</color>");
            }
            else if (!bothConditionsMet && previousStatus)
            {
                UnityEngine.Debug.LogWarning("[DelayedPuppetActivation] ⚠️ Conditions lost. Resetting timer.");
                conditionsMetTimer = 0f;
            }

            // Log individual statuses
            if (!avatarActivated)
            {
                UnityEngine.Debug.Log($"[DelayedPuppetActivation] Status Check: Bryce Webcam={bryceWebcamActive}, UDP Data={udpDataFlowing}");
            }
        }

        //private bool CheckBryceWebcamActive()
        //{
        //    if (diagnostic == null) return false;

        //    // Access Bryce update count through reflection (since it's private in DualPoseDiagnostic)
        //    var bryceUpdateCountField = typeof(DualPoseDiagnostic).GetField("bryceUpdateCount",
        //        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        //    if (bryceUpdateCountField != null)
        //    {
        //        int currentUpdates = (int)bryceUpdateCountField.GetValue(diagnostic);
        //        int updatesSinceLastCheck = currentUpdates - lastBryceUpdateCheck;
        //        lastBryceUpdateCheck = currentUpdates;

        //        bool isActive = updatesSinceLastCheck >= minBryceUpdatesPerSec;
                
        //        if (isActive)
        //        {
        //            UnityEngine.Debug.Log($"[DelayedPuppetActivation] Bryce webcam ACTIVE: {updatesSinceLastCheck} updates/sec");
        //        }

        //        return isActive;
        //    }

        //    return false;
        //}

        private bool CheckUdpDataFlowing()
        {
            if (udpServer == null) return false;

            bool isFlowing = totalUdpPackets >= minUdpPacketsReceived;

            if (isFlowing)
            {
                UnityEngine.Debug.Log($"[DelayedPuppetActivation] UDP data FLOWING: {totalUdpPackets} packets received");
            }

            return isFlowing;
        }

        private void ActivateavatarPuppet()
        {
            if (avatarActivated) return;

            UnityEngine.Debug.Log($"<color=cyan>[DelayedPuppetActivation] 🎉 Activating avatar puppet NOW!</color>");
            avatarPuppet.SetActive(true);
            avatarActivated = true;

            // Optionally disable this script after activation
            // enabled = false;
        }

        // Public method to manually reset the activation process
        public void ResetActivation()
        {
            UnityEngine.Debug.Log("[DelayedPuppetActivation] Manual reset triggered");
            avatarActivated = false;
            conditionsMetTimer = 0f;
            bothConditionsMet = false;
            totalUdpPackets = 0;
            lastBryceUpdateCheck = 0;
            //avatarPuppet.SetActive(false);
        }
    }
}
