using UnityEngine;
using Tracking4All;
using System;
using Mediapipe.Unity;

/// <summary>
/// Image provider that receives data from NetworkedWebcamBackend for use with Tracking4All solutions
/// This bridges the networked camera data to the Tracking4All image provider system
/// </summary>
public class NetworkCameraImageProvider : MonoBehaviour, IImageFrameProvider, IProviderLifecycle
{
    [Header("Network Camera Configuration")]
    [SerializeField] private NetworkedWebcamBackend networkCamBackend;
    [SerializeField] private bool autoFindNetworkCamera = true;
    [SerializeField] private string uniqueProviderId; // Optional: Assign unique ID to prevent conflicts
    
    [Header("Image Settings")]
    [SerializeField] private int imageWidth = 640;
    [SerializeField] private int imageHeight = 480;
    
    private ImageFrame currentImageFrame;
    private bool hasNewFrame = false;
    private float lastUpdateTime = 0f;
    private bool isReady = false;
    private bool isDisposed = false;
    
    // Track if this provider has claimed the backend
    private static NetworkedWebcamBackend claimedBackend = null;
    private static string claimedById = null;
    
    // IProviderLifecycle events - using GroupEvent delegate
    public event GroupEvent OnReady;
    public event GroupEvent OnDisposed;
    public event GroupEvent OnResumed;
    public event GroupEvent OnUpdated;
    public event GroupEvent OnTimedOut;
    
    // IProvider properties
    public IProviderLifecycle Lifecycle => this;
    public int GroupSize => 1;
    public int DataCount => 1;
    
    private void Start()
    {
        // Generate unique ID if not set
        if (string.IsNullOrEmpty(uniqueProviderId))
        {
            uniqueProviderId = $"Provider_{gameObject.name}_{GetEntityId()}";
        }
        
        if (autoFindNetworkCamera && networkCamBackend == null)
        {
            networkCamBackend = FindObjectOfType<NetworkedWebcamBackend>();
        }
        
        if (networkCamBackend == null)
        {
            Debug.LogError($"NetworkCameraImageProvider ({uniqueProviderId}): NetworkedWebcamBackend not found!");
            return;
        }
        
        // Check if another provider has already claimed this backend
        if (claimedBackend == networkCamBackend && claimedById != uniqueProviderId)
        {
            Debug.LogError($"NetworkCameraImageProvider ({uniqueProviderId}): NetworkedWebcamBackend is already in use by {claimedById}! " +
                          "Either disable autoFindNetworkCamera and assign a separate backend, or ensure only one provider uses this backend.");
            enabled = false; // Disable this provider to prevent conflicts
            return;
        }
        
        // Claim the backend
        claimedBackend = networkCamBackend;
        claimedById = uniqueProviderId;
        
        // Initialize the image provider
        InitializeProvider();
        
        Debug.Log($"NetworkCameraImageProvider ({uniqueProviderId}): Claimed and initialized network backend");
    }
    
    private void InitializeProvider()
    {
        // Create empty image frame
        currentImageFrame = new ImageFrame();
        isReady = true;
        
        OnReady?.Invoke(0);
        
        Debug.Log("NetworkCameraImageProvider: Initialized and ready");
    }
    
    private void Update()
    {
        // Check if we have new frame data from the network camera
        if (networkCamBackend != null && !isDisposed)
        {
            // Try to get the latest frame from the network backend
            TryUpdateFrame();
        }
        
        // Update timing information
        lastUpdateTime = Time.time;
    }
    
    private void TryUpdateFrame()
    {
        try
        {
            // Get the latest frame from NetworkedWebcamBackend
            if (networkCamBackend != null && networkCamBackend.Lifecycle.IsReady(0))
            {
                // Try to get image frame from the network backend
                var networkFrame = networkCamBackend.Get(0, ImageFrameIndex.DEFAULT);
                if (networkFrame != null && networkFrame.Texture != null)
                {
                    // Hook the network frame to our current frame
                    currentImageFrame.Hook(networkFrame.Texture, networkFrame.SourceProperties);
                    hasNewFrame = true;
                    OnUpdated?.Invoke(0);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"NetworkCameraImageProvider: Error updating frame: {e.Message}");
        }
    }
    
    // IProvider<ImageFrameIndex, ImageFrame> implementation
    public ImageFrame Get(int group, ImageFrameIndex index)
    {
        if (group == 0 && index == ImageFrameIndex.DEFAULT && !isDisposed)
        {
            return currentImageFrame;
        }
        return null;
    }
    
    public ImageFrame Get(int group, int index)
    {
        if (group == 0 && index == 0 && !isDisposed)
        {
            return currentImageFrame;
        }
        return null;
    }
    
    // IProviderLifecycle implementation
    public bool IsReady(int group)
    {
        return group == 0 && isReady && !isDisposed;
    }
    
    public bool IsDisposed(int group)
    {
        return group == 0 && isDisposed;
    }
    
    public bool IsAlive(int group)
    {
        if (group != 0) return false;
        
        // Check if we're receiving data from network backend
        if (networkCamBackend == null) return false;
        if (isDisposed) return false;
        
        // Consider alive if we've received recent updates
        float timeSinceUpdate = Time.time - lastUpdateTime;
        return timeSinceUpdate < 5f; // 5 second threshold
    }
    
    public float GetTimeSinceLastUpdate(int group = 0)
    {
        return Time.time - lastUpdateTime;
    }
    
    public void Dispose(int group)
    {
        if (group == 0 && !isDisposed)
        {
            isDisposed = true;
            isReady = false;
            
            OnDisposed?.Invoke(group);
            
            if (currentImageFrame != null)
            {
                currentImageFrame.Dispose();
                currentImageFrame = null;
            }
        }
    }
    
    private void OnDestroy()
    {
        // Release the backend claim
        if (claimedBackend == networkCamBackend && claimedById == uniqueProviderId)
        {
            claimedBackend = null;
            claimedById = null;
            Debug.Log($"NetworkCameraImageProvider ({uniqueProviderId}): Released network backend");
        }
        
        for (int i = 0; i < GroupSize; i++)
        {
            Dispose(i);
        }
    }
    
    // Public methods for external configuration
    public void SetNetworkCamera(NetworkedWebcamBackend networkCamera)
    {
        networkCamBackend = networkCamera;
        if (networkCamera != null)
        {
            Debug.Log("NetworkCameraImageProvider: Network camera backend set");
        }
    }
    
    public void SetImageDimensions(int width, int height)
    {
        imageWidth = width;
        imageHeight = height;
    }
    
    public bool IsConnected()
    {
        return networkCamBackend != null && networkCamBackend.Lifecycle.IsReady(0) && IsAlive(0);
    }
    
    public bool HasNewFrame()
    {
        return hasNewFrame && !isDisposed;
    }
    
    public string GetConnectionStatus()
    {
        if (isDisposed)
            return "Provider disposed";
            
        if (networkCamBackend == null)
            return "No network camera backend";
            
        if (!networkCamBackend.Lifecycle.IsReady(0))
            return "Network camera disconnected";
            
        if (!IsAlive(0))
            return "Connection timed out";
            
        if (!hasNewFrame)
            return "Connected but no frames received";
            
        return $"Connected and receiving frames ({imageWidth}x{imageHeight})";
    }
    
    // Additional helper methods for network camera integration
    public void Resume(int group = 0)
    {
        if (group == 0 && isDisposed)
        {
            isDisposed = false;
            isReady = true;
            OnResumed?.Invoke(group);
        }
    }
    
    public void MarkTimedOut(int group = 0)
    {
        if (group == 0)
        {
            OnTimedOut?.Invoke(group);
        }
    }
}