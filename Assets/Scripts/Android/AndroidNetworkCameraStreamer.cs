using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using Unity.Netcode;
using System;
using System.Collections;

/// <summary>
/// Android-native camera streaming solution for dual avatar tracking
/// Replaces Python networked webcam with Unity-based implementation
/// Works directly between two Android smartphones
/// </summary>
public class AndroidNetworkCameraStreamer : NetworkBehaviour
{
    [Header("Camera Configuration")]
    [SerializeField] private int cameraWidth = 640;
    [SerializeField] private int cameraHeight = 480;
    [SerializeField] private int cameraFPS = 30;
    [SerializeField] private int preferredCameraIndex = 0;
    
    [Header("Network Streaming")]
    [SerializeField] private float streamingRate = 15f; // FPS for network streaming
    [SerializeField] private int compressionQuality = 75; // JPEG quality 0-100
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Performance Settings")]
    [SerializeField] private int maxPacketSize = 65000; // Max UDP packet size
    [SerializeField] private bool useCompression = true;
    
    private WebCamTexture webCamTexture;
    private Texture2D captureTexture;
    private byte[] imageData;
    private bool isStreaming = false;
    private float lastStreamTime = 0f;
    
    // Network variables for camera stream
    private NetworkVariable<int> networkCameraWidth = new NetworkVariable<int>(640);
    private NetworkVariable<int> networkCameraHeight = new NetworkVariable<int>(480);
    
    public event Action<Texture2D> OnFrameReceived;
    public bool IsReceivingFrames { get; private set; }
    public bool IsSendingFrames { get; private set; }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsClient && !IsHost)
        {
            // CLIENT: Start camera and stream to host
            StartCoroutine(InitializeCameraStreaming());
        }
        else if (IsHost)
        {
            // HOST: Prepare to receive camera stream
            PrepareForReceiving();
        }
    }
    
    private IEnumerator InitializeCameraStreaming()
    {
        if (enableDebugLogs)
            Debug.Log("CLIENT: Initializing Android camera for streaming...");
        
        // Request camera permission on Android
        yield return StartCoroutine(RequestCameraPermission());
        
        // Initialize camera
        yield return StartCoroutine(InitializeCamera());
        
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            StartCameraStreaming();
        }
        else
        {
            Debug.LogError("Failed to initialize camera for streaming!");
        }
    }
    
private IEnumerator RequestCameraPermission()
    {
        #if PLATFORM_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            
            // Wait for permission response
            float timeout = 10f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.LogError("Camera permission denied!");
                yield break;
            }
        }
        #endif
        
        if (enableDebugLogs)
            Debug.Log("Camera permission granted");
            
        yield return null;
    }
    
    private IEnumerator InitializeCamera()
    {
        // Find available cameras
        WebCamDevice[] devices = WebCamTexture.devices;
        
        if (devices.Length == 0)
        {
            Debug.LogError("No camera devices found!");
            yield break;
        }
        
        // Select camera (prefer back camera on mobile)
        string selectedDevice = "";
        for (int i = 0; i < devices.Length; i++)
        {
            if (enableDebugLogs)
                Debug.Log($"Camera {i}: {devices[i].name} (Front: {devices[i].isFrontFacing})");
            
            // Prefer back camera for pose tracking
            if (!devices[i].isFrontFacing)
            {
                selectedDevice = devices[i].name;
                break;
            }
        }
        
        // Fallback to any available camera
        if (string.IsNullOrEmpty(selectedDevice) && devices.Length > 0)
        {
            selectedDevice = devices[0].name;
        }
        
        if (string.IsNullOrEmpty(selectedDevice))
        {
            Debug.LogError("No suitable camera found!");
            yield break;
        }
        
        // Initialize WebCamTexture
        webCamTexture = new WebCamTexture(selectedDevice, cameraWidth, cameraHeight, cameraFPS);
        webCamTexture.Play();
        
        // Wait for camera to start
        float timeout = 5f;
        while (!webCamTexture.isPlaying && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        
        if (!webCamTexture.isPlaying)
        {
            Debug.LogError("Camera failed to start!");
            yield break;
        }
        
        // Wait for first frame
        yield return new WaitUntil(() => webCamTexture.width > 16);
        
        // Update actual camera dimensions
        cameraWidth = webCamTexture.width;
        cameraHeight = webCamTexture.height;
        
        // Create capture texture
        captureTexture = new Texture2D(cameraWidth, cameraHeight, TextureFormat.RGB24, false);
        
        if (enableDebugLogs)
            Debug.Log($"Camera initialized: {selectedDevice} ({cameraWidth}x{cameraHeight})");
    }
    
    private void StartCameraStreaming()
    {
        if (enableDebugLogs)
            Debug.Log("Starting camera streaming to host...");
        
        isStreaming = true;
        IsSendingFrames = true;
        
        // Send camera specs to host
        SendCameraSpecsServerRpc(cameraWidth, cameraHeight);
    }
    
    private void PrepareForReceiving()
    {
        if (enableDebugLogs)
            Debug.Log("HOST: Prepared to receive camera stream from client");
        
        IsReceivingFrames = false; // Will be set to true when first frame arrives
    }
    
    private void Update()
    {
        if (isStreaming && webCamTexture != null && webCamTexture.isPlaying)
        {
            // Stream frames at specified rate
            if (Time.time - lastStreamTime >= (1f / streamingRate))
            {
                CaptureAndStreamFrame();
                lastStreamTime = Time.time;
            }
        }
    }
    
    private void CaptureAndStreamFrame()
    {
        try
        {
            // Capture current camera frame
            captureTexture.SetPixels(webCamTexture.GetPixels());
            captureTexture.Apply();
            
            // Convert to compressed bytes
            byte[] frameData;
            if (useCompression)
            {
                frameData = captureTexture.EncodeToJPG(compressionQuality);
            }
            else
            {
                frameData = captureTexture.EncodeToPNG();
            }
            
            // Send frame data to host (split into chunks if necessary)
            SendFrameDataToHost(frameData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error capturing frame: {e.Message}");
        }
    }
    
    private void SendFrameDataToHost(byte[] frameData)
    {
        if (frameData.Length <= maxPacketSize)
        {
            // Send in single packet
            SendFrameChunkServerRpc(frameData, 0, 1, frameData.Length);
        }
        else
        {
            // Split into multiple chunks
            int chunkCount = Mathf.CeilToInt((float)frameData.Length / maxPacketSize);
            
            for (int i = 0; i < chunkCount; i++)
            {
                int offset = i * maxPacketSize;
                int chunkSize = Mathf.Min(maxPacketSize, frameData.Length - offset);
                
                byte[] chunk = new byte[chunkSize];
                Array.Copy(frameData, offset, chunk, 0, chunkSize);
                
                SendFrameChunkServerRpc(chunk, i, chunkCount, frameData.Length);
            }
        }
    }
    
    // Network RPCs for camera streaming
    [ServerRpc]
    private void SendCameraSpecsServerRpc(int width, int height)
    {
        networkCameraWidth.Value = width;
        networkCameraHeight.Value = height;
        
        if (enableDebugLogs)
            Debug.Log($"HOST: Received camera specs from client: {width}x{height}");
    }
    
    private byte[] receivedFrameBuffer;
    private int expectedFrameSize;
    private int receivedChunks;
    private int totalChunks;
    
    [ServerRpc]
    private void SendFrameChunkServerRpc(byte[] chunkData, int chunkIndex, int totalChunkCount, int totalFrameSize)
    {
        // Initialize frame buffer for new frame
        if (chunkIndex == 0)
        {
            receivedFrameBuffer = new byte[totalFrameSize];
            expectedFrameSize = totalFrameSize;
            receivedChunks = 0;
            totalChunks = totalChunkCount;
        }
        
        // Copy chunk data to buffer
        int offset = chunkIndex * maxPacketSize;
        Array.Copy(chunkData, 0, receivedFrameBuffer, offset, chunkData.Length);
        receivedChunks++;
        
        // Process complete frame
        if (receivedChunks >= totalChunks)
        {
            ProcessReceivedFrame(receivedFrameBuffer);
        }
    }
    
    private void ProcessReceivedFrame(byte[] frameData)
    {
        try
        {
            // Create texture from received data
            Texture2D receivedTexture = new Texture2D(2, 2);
            
            if (useCompression)
            {
                receivedTexture.LoadImage(frameData); // JPG
            }
            else
            {
                receivedTexture.LoadImage(frameData); // PNG
            }
            
            IsReceivingFrames = true;
            OnFrameReceived?.Invoke(receivedTexture);
            
            // Clean up previous texture to avoid memory leaks
            // Note: The receiving component should handle texture disposal
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing received frame: {e.Message}");
        }
    }
    
    public override void OnNetworkDespawn()
    {
        StopCameraStreaming();
        base.OnNetworkDespawn();
    }
    
    private void StopCameraStreaming()
    {
        isStreaming = false;
        IsSendingFrames = false;
        
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            Destroy(webCamTexture);
            webCamTexture = null;
        }
        
        if (captureTexture != null)
        {
            Destroy(captureTexture);
            captureTexture = null;
        }
    }
    
    private void OnDestroy()
    {
        StopCameraStreaming();
    }
    
    // Public methods for external integration
    public Texture2D GetCurrentLocalFrame()
    {
        return captureTexture;
    }
    
    public bool IsLocalCameraActive()
    {
        return webCamTexture != null && webCamTexture.isPlaying;
    }
    
    public Vector2 GetCameraDimensions()
    {
        if (IsHost)
            return new Vector2(networkCameraWidth.Value, networkCameraHeight.Value);
        else
            return new Vector2(cameraWidth, cameraHeight);
    }
    
    public string GetStreamingStatus()
    {
        if (IsClient && !IsHost)
        {
            if (!IsLocalCameraActive())
                return "Camera not initialized";
            if (!IsSendingFrames)
                return "Camera ready but not streaming";
            return $"Streaming to host ({streamingRate}fps, {compressionQuality}% quality)";
        }
        else if (IsHost)
        {
            if (!IsReceivingFrames)
                return "Waiting for client camera stream";
            return $"Receiving client stream ({networkCameraWidth.Value}x{networkCameraHeight.Value})";
        }
        
        return "Not connected";
    }
}