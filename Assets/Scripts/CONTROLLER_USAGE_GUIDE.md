# IPWebcamStreamController - Quick Start Guide

## Simple Setup (5 Steps)

### 1. Setup Your IP Webcam
- Install "IP Webcam" app on your Android phone (or any IP camera app)
- Start the server in the app
- Note the IP address shown (e.g., 192.168.1.100:8080)

### 2. Add Controller to Scene
1. Create an empty GameObject: `GameObject > Create Empty`
2. Rename it to "WebcamStreamController"
3. Add the script: `Add Component > IPWebcamStreamController`

### 3. Configure in Inspector
Fill in these fields:
```
Webcam Configuration:
  - Webcam IP: 192.168.1.100  (your phone's IP)
  - Webcam Port: 8080
  - Stream Path: /video

Target Unity Receiver:
  - Target IP: 127.0.0.1  (same machine) or IP of receiving Unity
  - Target Port: 52733  (must match your receiver)

Pose Provider:
  - Drag your pose tracking GameObject here 
    (the one with MPPPoseSolution or your tracking solution)

Options:
  - Auto Start On Play: ✓ (check for automatic start)
  - Show Debug Logs: ✓ (check to see status messages)
```

### 4. Verify Your Receiver is Ready
Make sure you have:
- A GameObject with your pose tracking solution running
- ServerUDP listening on port 52733 (or your configured port)
- Avatar with PosePuppet ready to receive data

### 5. Start Streaming
**Option A - Automatic:**
- Check "Auto Start On Play"
- Press Play in Unity

**Option B - Manual/Scripted:**
```csharp
// From another script:
IPWebcamStreamController controller = FindObjectOfType<IPWebcamStreamController>();
controller.StartStream();
```

**Option C - From UI Button:**
- Create a UI Button
- Add OnClick event
- Drag the WebcamStreamController GameObject
- Select `IPWebcamStreamController.StartStream`

## Usage Examples

### Basic Setup
```csharp
using Tracking4All;

public class MyGameManager : MonoBehaviour
{
    private IPWebcamStreamController streamController;
    
    void Start()
    {
        streamController = FindObjectOfType<IPWebcamStreamController>();
        
        // Subscribe to events (optional)
        streamController.OnStreamStarted += () => Debug.Log("Stream started!");
        streamController.OnConnectionEstablished += () => Debug.Log("Connected!");
    }
    
    void Update()
    {
        // Press space to toggle streaming
        if (Input.GetKeyDown(KeyCode.Space))
        {
            streamController.ToggleStream();
        }
    }
}
```

### UI Integration
```csharp
using UnityEngine;
using UnityEngine.UI;
using Tracking4All;

public class StreamUI : MonoBehaviour
{
    public Button startButton;
    public Button stopButton;
    public Text statusText;
    
    private IPWebcamStreamController controller;
    
    void Start()
    {
        controller = FindObjectOfType<IPWebcamStreamController>();
        
        startButton.onClick.AddListener(() => controller.StartStream());
        stopButton.onClick.AddListener(() => controller.StopStream());
        
        controller.OnStreamStarted += () => statusText.text = "Streaming...";
        controller.OnStreamStopped += () => statusText.text = "Stopped";
        controller.OnConnectionEstablished += () => statusText.text = "Connected!";
    }
}
```

### Dynamic Configuration
```csharp
// Change settings at runtime
controller.webcamIP = "192.168.1.150";
controller.webcamPort = 8080;
controller.targetIP = "192.168.1.200";
controller.targetPort = 52733;
controller.StartStream();
```

## Public Methods

| Method | Description |
|--------|-------------|
| `StartStream()` | Start streaming from IP webcam to avatar |
| `StopStream()` | Stop the current stream |
| `ToggleStream()` | Toggle streaming on/off |

## Public Properties

| Property | Description |
|----------|-------------|
| `bool IsStreaming` | Is currently streaming |
| `bool IsConnected` | Is UDP connection active |
| `string WebcamURL` | Full webcam URL |
| `string TargetURL` | Target UDP endpoint |

## Events You Can Subscribe To

```csharp
controller.OnStreamStarted += YourMethod;
controller.OnStreamStopped += YourMethod;
controller.OnConnectionEstablished += YourMethod;
controller.OnConnectionLost += YourMethod;
```

## Troubleshooting

### "Pose Provider not assigned" Error
**Fix:** Drag the GameObject with your pose tracking solution into the "Pose Provider" field in the Inspector.

### No connection to webcam
1. Check phone and PC are on same WiFi network
2. Test webcam URL in browser: `http://192.168.1.100:8080/video`
3. Check firewall isn't blocking connections
4. Verify IP address is correct

### Pose data not reaching avatar
1. Check "Target Port" matches your receiver (default: 52733)
2. Verify your MPPPoseSolution or tracking solution is running
3. Check Console for "UDP connection established!" message
4. Verify ServerUDP is active and listening

### Stream starts but avatar doesn't move
1. Verify pose provider GameObject is correct
2. Check your avatar's PosePuppet is connected to the receiver
3. Look for pose data in Console (if debug logs enabled)
4. Ensure your tracking solution is processing incoming data

## Testing Checklist

- [ ] Phone/camera is on same network as PC
- [ ] Can access webcam URL in browser
- [ ] Pose provider GameObject assigned in Inspector
- [ ] Target IP/port matches your receiver
- [ ] Receiver (ServerUDP + MPPPoseLandmarkAdapter) is running
- [ ] Avatar with PosePuppet is in scene
- [ ] Press Play and see "Stream started successfully!" in Console

## Example Scene Setup

```
Hierarchy:
├── WebcamStreamController (has IPWebcamStreamController script)
├── PoseTrackingSolution (has MPPPoseSolution - assign this to controller)
├── Avatar (has PosePuppet + skeleton)
└── Camera
```

## Advanced: Multiple Cameras

To stream from multiple webcams:
```csharp
// Create multiple controllers
GameObject controller1 = new GameObject("Webcam1");
var stream1 = controller1.AddComponent<IPWebcamStreamController>();
stream1.webcamIP = "192.168.1.100";
stream1.targetPort = 52733;

GameObject controller2 = new GameObject("Webcam2");
var stream2 = controller2.AddComponent<IPWebcamStreamController>();
stream2.webcamIP = "192.168.1.101";
stream2.targetPort = 52734;  // Different port!
```

## Need Help?

Check Console for error messages - the controller provides detailed logging when "Show Debug Logs" is enabled.
