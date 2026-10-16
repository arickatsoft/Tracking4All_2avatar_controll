// SimpleAvatarActivator.cs - TOGGLE VERSION
// Toggles Avatar puppet off/on after UDP data is flowing
// (C) 2024-2025 G8gaming Ltd.

using UnityEngine;
using System.Collections;

namespace Tracking4All
{
    /// <summary>
    /// Waits for UDP data, then after 3 seconds deactivates Avatar for 1 second, then reactivates.
    /// This reinitializes the puppet with UDP data properly.
    /// </summary>
    public class SimpleAvatarActivator : MonoBehaviour
    {
        [Header("Target Puppet")]
        [Tooltip("The Avatar puppet GameObject to toggle")]
        [SerializeField] public GameObject avatarPuppet;

        [Header("Timing")]
        [Tooltip("Seconds to wait after UDP data starts before toggling")]
        public float waitBeforeToggle = 3f;
        
        [Tooltip("Seconds to keep puppet inactive during toggle")]
        public float deactivationDuration = 1f;

        [Header("Status (Read-Only)")]
        [SerializeField] private bool udpDataReceived = false;
        [SerializeField] private float dataFlowTimer = 0f;
        [SerializeField] private bool toggleCompleted = false;
        [SerializeField] private int totalPacketsReceived = 0;
        [SerializeField] private string currentState = "Waiting for UDP";

        private UDPPoseServer udpServer;
        private float lastLogTime = 0f;

        private void Start()
        {
            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log("[SimpleAvatarActivator] START - Toggle version");
            UnityEngine.Debug.Log("========================================");

            if (avatarPuppet == null)
            {
                UnityEngine.Debug.LogError("[SimpleAvatarActivator] ❌ ERROR: Avatar puppet GameObject not assigned in Inspector!");
                enabled = false;
                return;
            }

            UnityEngine.Debug.Log($"[SimpleAvatarActivator] ✅ Avatar puppet assigned: {avatarPuppet.name}");

            // Ensure Avatar starts ACTIVE
            if (!avatarPuppet.activeSelf)
            {
                UnityEngine.Debug.Log("[SimpleAvatarActivator] Activating Avatar puppet at start (was inactive)");
                avatarPuppet.SetActive(true);
            }
            else
            {
                UnityEngine.Debug.Log("[SimpleAvatarActivator] ✅ Avatar puppet already active (correct)");
            }

            // Find UDPPoseServer (it's added from code)
            udpServer = FindObjectOfType<UDPPoseServer>();
            
            if (udpServer == null)
            {
                UnityEngine.Debug.LogWarning("[SimpleAvatarActivator] ⚠️ UDPPoseServer not found yet. Will keep searching every 0.5 seconds...");
                InvokeRepeating(nameof(SearchForUdpServer), 0.5f, 0.5f);
            }
            else
            {
                SubscribeToUdpServer();
            }

            UnityEngine.Debug.Log($"[SimpleAvatarActivator] 🎯 Waiting for UDP data to start flowing...");
            UnityEngine.Debug.Log($"[SimpleAvatarActivator] Logic: UDP data → Wait {waitBeforeToggle}s → Deactivate {deactivationDuration}s → Reactivate");
            UnityEngine.Debug.Log("========================================");
        }

        private void SearchForUdpServer()
        {
            UnityEngine.Debug.Log("[SimpleAvatarActivator] 🔍 Searching for UDPPoseServer...");
            udpServer = FindObjectOfType<UDPPoseServer>();
            
            if (udpServer != null)
            {
                UnityEngine.Debug.Log("[SimpleAvatarActivator] ✅ Found UDPPoseServer!");
                CancelInvoke(nameof(SearchForUdpServer));
                SubscribeToUdpServer();
            }
        }

        private void SubscribeToUdpServer()
        {
            if (udpServer != null)
            {
                udpServer.OnPoseDataReceived += OnUdpDataReceived;
                UnityEngine.Debug.Log("[SimpleAvatarActivator] ✅ Subscribed to UDP data events - ready to receive!");
            }
        }

        private void OnDestroy()
        {
            if (udpServer != null)
            {
                udpServer.OnPoseDataReceived -= OnUdpDataReceived;
            }
            UnityEngine.Debug.Log("[SimpleAvatarActivator] Script destroyed/stopped");
        }

        private void OnUdpDataReceived(PoseData data)
        {
            totalPacketsReceived++;
            
            if (!udpDataReceived)
            {
                udpDataReceived = true;
                currentState = "Counting down to toggle";
                UnityEngine.Debug.Log("========================================");
                UnityEngine.Debug.Log($"<color=green>[SimpleAvatarActivator] ✅ FIRST UDP PACKET RECEIVED!</color>");
                UnityEngine.Debug.Log($"<color=green>[SimpleAvatarActivator] 🎯 Will toggle Avatar in {waitBeforeToggle} seconds...</color>");
                UnityEngine.Debug.Log("========================================");
            }
        }

        private void Update()
        {
            if (toggleCompleted) return;

            // Log status every 2 seconds even if no data
            if (Time.time - lastLogTime >= 2f)
            {
                lastLogTime = Time.time;
                
                if (!udpDataReceived)
                {
                    UnityEngine.Debug.Log($"[SimpleAvatarActivator] Status: Still waiting for UDP data... (Total packets: {totalPacketsReceived})");
                    
                    if (udpServer == null)
                    {
                        UnityEngine.Debug.LogWarning("[SimpleAvatarActivator] ⚠️ UDPPoseServer still not found!");
                    }
                }
            }

            // Once UDP data is flowing, count down
            if (udpDataReceived && !toggleCompleted)
            {
                dataFlowTimer += Time.deltaTime;

                // Log progress every second
                int currentSecond = Mathf.FloorToInt(dataFlowTimer);
                int lastSecond = Mathf.FloorToInt(dataFlowTimer - Time.deltaTime);
                
                if (currentSecond != lastSecond)
                {
                    float remaining = waitBeforeToggle - dataFlowTimer;
                    if (remaining > 0)
                    {
                        UnityEngine.Debug.Log($"<color=yellow>[SimpleAvatarActivator] ⏱️ Toggling in {Mathf.CeilToInt(remaining)} seconds... (Total packets: {totalPacketsReceived})</color>");
                    }
                }

                // Toggle when time is up
                if (dataFlowTimer >= waitBeforeToggle)
                {
                    StartCoroutine(ToggleavatarPuppet());
                }
            }
        }

        private IEnumerator ToggleavatarPuppet()
        {
            toggleCompleted = true; // Prevent multiple toggles
            
            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log($"<color=cyan>[SimpleAvatarActivator] 🔄 STARTING TOGGLE SEQUENCE</color>");
            UnityEngine.Debug.Log($"<color=cyan>[SimpleAvatarActivator] Total UDP packets received: {totalPacketsReceived}</color>");
            UnityEngine.Debug.Log("========================================");

            // Step 1: Deactivate
            currentState = "Deactivating puppet";
            UnityEngine.Debug.Log($"<color=orange>[SimpleAvatarActivator] ⏸️ DEACTIVATING Avatar puppet for {deactivationDuration} second(s)...</color>");
            avatarPuppet.SetActive(false);
            UnityEngine.Debug.Log($"[SimpleAvatarActivator] ✅ Avatar puppet is now inactive: {avatarPuppet.activeSelf}");

            // Step 2: Wait
            currentState = $"Waiting {deactivationDuration}s";
            yield return new WaitForSeconds(deactivationDuration);

            // Step 3: Reactivate
            currentState = "Reactivating puppet";
            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log($"<color=green>[SimpleAvatarActivator] ▶️ REACTIVATING Avatar puppet NOW!</color>");
            avatarPuppet.SetActive(true);
            UnityEngine.Debug.Log($"[SimpleAvatarActivator] ✅ Avatar puppet is now active: {avatarPuppet.activeSelf}");
            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log($"<color=green>[SimpleAvatarActivator] 🎉 TOGGLE SEQUENCE COMPLETE!</color>");
            UnityEngine.Debug.Log($"<color=green>[SimpleAvatarActivator] Avatar should now be receiving UDP data properly.</color>");
            UnityEngine.Debug.Log("========================================");

            currentState = "Toggle complete";
        }

        // Public method to manually reset and toggle again
        public void ResetAndToggle()
        {
            UnityEngine.Debug.Log("[SimpleAvatarActivator] 🔄 Manual reset triggered");
            StopAllCoroutines();
            toggleCompleted = false;
            udpDataReceived = false;
            dataFlowTimer = 0f;
            totalPacketsReceived = 0;
            currentState = "Reset - waiting for UDP";
            
            // Ensure puppet is active to receive data
            avatarPuppet.SetActive(true);
            UnityEngine.Debug.Log("[SimpleAvatarActivator] Reset complete - waiting for UDP data again");
        }
    }
}
