using UnityEngine;
using System;

namespace Tracking4All
{
    /// <summary>
    /// Provides pose landmarks from UDP data, bypassing MediaPipe.
    /// This acts as a direct landmark provider that puppets can consume.
    /// </summary>
    public class UDPPoseLandmarkProvider : MonoBehaviour, ILandmarkProvider<PoseLandmarks>
    {
        [Header("UDP Configuration")]
        [SerializeField] public int listenPort = 52733;
        
        [Header("Status")]
        [SerializeField] private bool isReceivingData = false;
        [SerializeField] private float lastUpdateTime = 0f;
        [SerializeField, Min(0.1f)] private float timeoutSeconds = 0.5f;
        public bool IsReceivingData => isReceivingData;
        
        // Landmark storage
        private Landmark[] landmarks = new Landmark[33];
        
        // UDP Server component
        private UDPPoseServer udpServer;
        
        // Internal lifecycle manager
        private SimpleLifecycleManager lifecycleManager;
        
        // Events from ILandmarkProvider
        public event GroupEvent OnLandmarksUpdated
        {
            add { if (lifecycleManager != null) lifecycleManager.OnUpdated += value; }
            remove { if (lifecycleManager != null) lifecycleManager.OnUpdated -= value; }
        }
        
        public event GroupEvent OnLandmarksReady
        {
            add { if (lifecycleManager != null) lifecycleManager.OnReady += value; }
            remove { if (lifecycleManager != null) lifecycleManager.OnReady -= value; }
        }
        
        public event GroupEvent OnLandmarksDisposed
        {
            add { if (lifecycleManager != null) lifecycleManager.OnDisposed += value; }
            remove { if (lifecycleManager != null) lifecycleManager.OnDisposed -= value; }
        }
        
        public event GroupEvent OnLandmarksResumed
        {
            add { if (lifecycleManager != null) lifecycleManager.OnResumed += value; }
            remove { if (lifecycleManager != null) lifecycleManager.OnResumed -= value; }
        }
        
        public event GroupEvent OnLandmarksTimedOut
        {
            add { if (lifecycleManager != null) lifecycleManager.OnTimedOut += value; }
            remove { if (lifecycleManager != null) lifecycleManager.OnTimedOut -= value; }
        }
        
        public IProviderLifecycle Lifecycle => lifecycleManager;
        public int GroupSize => 1;
        public int DataCount => 33;
        
        private void Awake()
        {
            // Initialize lifecycle manager
            lifecycleManager = new SimpleLifecycleManager(1);
            
            // Initialize all landmarks
            for (int i = 0; i < 33; i++)
            {
                landmarks[i] = new Landmark();
            }
            
            // Get or add UDP server component
            udpServer = GetComponent<UDPPoseServer>();
            if (udpServer == null)
            {
                udpServer = gameObject.AddComponent<UDPPoseServer>();
            }
        }
        
        private void Start()
        {
            // Subscribe to UDP server events
            if (udpServer != null)
            {
                udpServer.OnPoseDataReceived += HandlePoseDataReceived;
            }
            
            UnityEngine.Debug.Log($"[UDPPoseLandmarkProvider] Waiting for UDP data from Device 2...");
            // Don't signal ready yet - wait for UDP data to arrive!
        }
        
        private void CheckConnection()
        {
            var subscriberCount = lifecycleManager.GetSubscriberCount();
            if (subscriberCount == 0)
            {
                UnityEngine.Debug.LogWarning("[UDPPoseLandmarkProvider] ⚠️ No subscribers! Puppet may not be connected properly.");
                UnityEngine.Debug.LogWarning("[UDPPoseLandmarkProvider] Try disabling and re-enabling the Puppet-StandardT-Remy GameObject.");
            }
            else
            {
                UnityEngine.Debug.Log($"[UDPPoseLandmarkProvider] ✅ {subscriberCount} subscriber(s) connected!");
            }
        }
        
        private void HandlePoseDataReceived(PoseData poseData)
        {
            if (!isActiveAndEnabled || poseData == null || poseData.worldLandmarks == null
                || poseData.worldLandmarks.Length != 33) return;
            
            // Update landmarks
            for (int i = 0; i < 33 && i < poseData.worldLandmarks.Length; i++)
            {
                Vector3 position = poseData.worldLandmarks[i];
                // Legacy protocol uses all-zero coordinates for missing landmarks.
                float available = position == Vector3.zero ? 0f : 1f;
                landmarks[i] = new Landmark(position, available, available);
            }
            
            isReceivingData = true;
            lastUpdateTime = Time.unscaledTime;
            lifecycleManager.SetReady(0);
            lifecycleManager.NotifyUpdated(0);
        }

        private void Update()
        {
            if (isReceivingData && Time.unscaledTime - lastUpdateTime > timeoutSeconds)
            {
                isReceivingData = false;
                for (int i = 0; i < landmarks.Length; i++) landmarks[i].Dispose();
                lifecycleManager.SetTimedOut(0);
            }
        }

        public Landmark Get(int group, PoseLandmarks index)
        {
            if (group != 0)
            {
                UnityEngine.Debug.LogWarning($"[UDPPoseLandmarkProvider] Invalid group {group}, only group 0 is supported");
                return new Landmark();
            }
            
            int idx = (int)index;
            if (idx < 0 || idx >= 33)
            {
                UnityEngine.Debug.LogWarning($"[UDPPoseLandmarkProvider] Invalid landmark index {idx}");
                return new Landmark();
            }
            
            return landmarks[idx];
        }
        
        public Landmark Get(int group, int index)
        {
            return Get(group, (PoseLandmarks)index);
        }
        
        private void OnDisable()
        {
            isReceivingData = false;
            for (int i = 0; i < landmarks.Length; i++) landmarks[i].Dispose();
            lifecycleManager?.SetDisposed(0);
        }

        private void OnDestroy()
        {
            if (udpServer != null)
            {
                udpServer.OnPoseDataReceived -= HandlePoseDataReceived;
            }
            
            lifecycleManager?.SetDisposed(0);
        }
    }
    
    /// <summary>
    /// Simple lifecycle manager for providers that don't use adapters
    /// </summary>
    internal class SimpleLifecycleManager : IProviderLifecycle
    {
        private bool[] isReady;
        private bool[] timedOut;
        
        public event GroupEvent OnReady;
        public event GroupEvent OnDisposed;
        public event GroupEvent OnResumed;
        public event GroupEvent OnUpdated;
        public event GroupEvent OnTimedOut;
        
        public int GroupSize { get; private set; }
        
        public SimpleLifecycleManager(int groupSize)
        {
            GroupSize = groupSize;
            isReady = new bool[groupSize];
            timedOut = new bool[groupSize];
        }
        
        public void SetReady(int group)
        {
            if (!isReady[group])
            {
                isReady[group] = true;
                timedOut[group] = false;
                OnReady?.Invoke(group);
            }
            else if (timedOut[group])
            {
                timedOut[group] = false;
                OnResumed?.Invoke(group);
            }
        }
        
        public void SetTimedOut(int group)
        {
            if (isReady[group] && !timedOut[group])
            {
                timedOut[group] = true;
                OnTimedOut?.Invoke(group);
            }
        }

        public void SetDisposed(int group)
        {
            if (isReady[group])
            {
                isReady[group] = false;
                timedOut[group] = false;
                OnDisposed?.Invoke(group);
            }
        }
        
        public void NotifyUpdated(int group)
        {
            OnUpdated?.Invoke(group);
        }
        
        public int GetSubscriberCount()
        {
            return OnUpdated?.GetInvocationList()?.Length ?? 0;
        }
        
        public bool IsReady(int group) => group >= 0 && group < GroupSize && isReady[group];
        public bool IsDisposed(int group) => !IsReady(group);
        public bool IsAlive(int group) => IsReady(group) && !timedOut[group];
        public bool IsTimedOut(int group) => group >= 0 && group < GroupSize && timedOut[group];
    }
}
