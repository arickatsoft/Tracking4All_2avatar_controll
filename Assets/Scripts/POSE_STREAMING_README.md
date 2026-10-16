# C# Pose Streaming Scripts - Converted from Python MediaPipe

## Overview
These C# scripts convert the Python MediaPipe pose streaming logic to Unity C# scripts that can:
- Stream from IP-based network webcams
- Process pose data using your existing MediaPipe Unity integration
- Send pose data via UDP to other Unity instances

## Files Created

### 1. NetworkWebcamClient.cs
**Purpose**: Connects to IP-based webcam streams (MJPEG/HTTP streams)

**Key Features**:
- TCP connection to network webcams
- Automatic reconnection on disconnect
- Frame-by-frame data retrieval
- Thread-safe frame access

**Usage**:
```csharp
NetworkWebcamClient client = gameObject.AddComponent<NetworkWebcamClient>();
// Configure in Inspector or via code
client.OnFrameReceived += (frameData) => {
    // Process frame
};
```

### 2. UDPPoseClient.cs
**Purpose**: Sends pose data via UDP to Unity receiver (mirrors Python ClientUDP)

**Key Features**:
- UDP streaming with automatic reconnection
- Message queue for reliable sending
- End-of-message markers ("<EOM>")
- Thread-safe message sending

**Usage**:
```csharp
UDPPoseClient client = gameObject.AddComponent<UDPPoseClient>();
client.SendMessage(poseDataString);
```

### 3. PoseStreamer.cs
**Purpose**: Main streaming component that combines webcam + pose detection + UDP streaming

**Key Features**:
- Works with both local and network webcams
- Integrates with your existing LandmarkProvider<PoseLandmarks>
- FPS monitoring and performance tracking
- Debug UI overlay

**Usage**:
1. Add PoseStreamer to a GameObject
2. Configure webcam settings (local or network)
3. Set UDP target IP/port
4. Ensure MediaPipe pose provider is running in scene

### 4. SimpleIPWebcamPoseStreamer.cs
**Purpose**: Simplified all-in-one solution for IP webcam streaming

**Key Features**:
- Minimal setup required
- Auto-starts on play
- Easy configuration via Inspector
- Good starting point for basic streaming

## Setup Instructions

### Option 1: Using with IP Webcam (Recommended for your use case)

1. **Install an IP Webcam app on your phone or use network camera**
   - Android: "IP Webcam" app (common port: 8080)
   - iOS: "EpocCam" or similar
   - Physical IP cameras

2. **Configure the streamer**:
   ```csharp
   // In Unity Inspector:
   - Webcam IP: 192.168.1.XXX (your phone/camera IP)
   - Webcam Port: 8080 (or your camera's port)
   - Stream Path: /video or /mjpeg
   - Target Unity IP: 127.0.0.1 (or remote PC)
   - Target Unity Port: 52733 (matches Python default)
   ```

3. **Add to scene**:
   - Create empty GameObject: "PoseStreamer"
   - Add SimpleIPWebcamPoseStreamer component
   - Configure IP and ports in Inspector
   - Ensure your MediaPipe solution is active

### Option 2: Using with Local Webcam

1. **Configure PoseStreamer**:
   ```csharp
   - Use Network Webcam: false
   - Local Webcam Index: 0
   - Target FPS: 30
   - Resolution: 640x480
   ```

## Integration with Existing System

### Your Current Setup (Python-based):
```
Python Script → MediaPipe Processing → UDP Send → Unity Receives
```

### New C# Setup (these scripts):
```
IP Webcam → NetworkWebcamClient → MediaPipe (Unity) → UDPPoseClient → Unity Receives
```

### Data Format (matches Python implementation):
```
# World coordinates (33 landmarks)
0|x|y|z
1|x|y|z
...
32|x|y|z
*||*
# Normalized coordinates (33 landmarks)
0|x|y|z
1|x|y|z
...
32|x|y|z
```

## Key Differences from Python Version

1. **Threading**: C# uses Unity's threading with proper marshalling
2. **Webcam Access**: Uses Unity WebCamTexture or network streams
3. **MediaPipe**: Relies on your existing Unity MediaPipe integration
4. **UDP**: Native C# Socket implementation

## Common IP Webcam URLs

- **IP Webcam (Android)**: `http://192.168.1.XXX:8080/video`
- **DroidCam**: `http://192.168.1.XXX:4747/mjpegfeed`
- **EpocCam (iOS)**: `http://192.168.1.XXX:8080/video`
- **MJPEG-Streamer**: `http://192.168.1.XXX:8080/?action=stream`

## Testing Your Setup

### Step 1: Test IP Webcam Connection
```csharp
// Add to test GameObject
var client = gameObject.AddComponent<NetworkWebcamClient>();
client.OnConnected += () => Debug.Log("Webcam connected!");
client.OnFrameReceived += (data) => Debug.Log($"Frame: {data.Length} bytes");
```

### Step 2: Test UDP Sending
```csharp
var udp = gameObject.AddComponent<UDPPoseClient>();
udp.SendMessage("Test message");
```

### Step 3: Full Integration
- Use PoseStreamer or SimpleIPWebcamPoseStreamer
- Monitor Unity Console for connection status
- Check receiving Unity instance for incoming data

## Troubleshooting

### No webcam connection:
- Verify IP address (check phone's WiFi settings)
- Ensure same network/subnet
- Test URL in browser first: `http://192.168.1.XXX:8080/video`
- Check firewall settings

### No pose data:
- Ensure MediaPipe solution is running
- Check LandmarkProvider is in scene
- Verify OnLandmarksUpdated event fires

### No UDP data received:
- Check target IP/port match receiver
- Verify firewall allows UDP on that port
- Use Wireshark to debug UDP packets

## Performance Tips

1. **Reduce resolution** for faster processing
2. **Lower FPS** if network is slow
3. **Use ModelComplexity = 0** for speed
4. **Monitor with Debug Mode** to find bottlenecks

## Example: Minimal Setup

```csharp
// Attach to GameObject with MediaPipe already running
public class QuickPoseStream : MonoBehaviour
{
    void Start()
    {
        var streamer = gameObject.AddComponent<SimpleIPWebcamPoseStreamer>();
        streamer.webcamIP = "192.168.1.100";
        streamer.webcamPort = 8080;
        streamer.targetUnityIP = "127.0.0.1";
        streamer.targetUnityPort = 52733;
        streamer.StartStreaming();
    }
}
```

## Python vs C# Feature Comparison

| Feature | Python (Original) | C# (New) |
|---------|------------------|----------|
| Local Webcam | ✅ cv2.VideoCapture | ✅ WebCamTexture |
| Network Webcam | ✅ HTTP Stream | ✅ NetworkWebcamClient |
| MediaPipe | ✅ Python SDK | ✅ Unity MediaPipe |
| UDP Streaming | ✅ Socket | ✅ UdpClient |
| Threading | ✅ threading.Thread | ✅ System.Threading |
| Auto-reconnect | ✅ | ✅ |
| Debug Mode | ✅ | ✅ |

## Next Steps

1. **Test with your existing receiver**: Your current Python server should receive data
2. **Integrate with PosePuppet**: The data format matches what MPPPoseLandmarkAdapter expects
3. **Add to multiplayer**: Stream poses between networked Unity instances
4. **Optimize**: Tune settings for your network/hardware

## Notes

- These scripts use your **existing MediaPipe Unity integration**
- The UDP format **matches your Python implementation**
- Works with **IP Webcam apps** on phones
- Compatible with your **current receiver scripts** (ServerUDP.cs, MPPPoseLandmarkAdapter.cs)

## Support

For issues:
1. Check Unity Console for errors
2. Verify network connectivity
3. Test components individually
4. Monitor UDP traffic with Wireshark
