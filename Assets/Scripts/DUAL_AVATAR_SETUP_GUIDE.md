 Press Play, see "Connected to TCP server" in Console
- [ ] Remy shows "Running: True" in DualPoseDiagnostic
- [ ] Both avatars moving independently

### Two Device Deployment:
- [ ] Device 1 has phone IP webcam OR local webcam
- [ ] IPWebcamToTCPBridge pointing to Device 2's IP
- [ ] Device 2 firewall allows TCP 52730
- [ ] NetworkedWebcamBackend listening on 0.0.0.0:52730
- [ ] Can telnet from Device 1 to Device 2:52730
- [ ] Both devices on same network/subnet

---

## Architecture Diagrams

### Current Python-Based (What you had):
```
Phone → Python Script → TCP → NetworkedWebcamBackend → Remy
                        ↓
                  (Port 52730)
```

### New C# Solution (What you have now):
```
Phone → IPWebcamToTCPBridge → NetworkedWebcamBackend → Remy
                     ↓
               (Port 52730)
```

### Full Dual Avatar Setup:
```
┌─────────────────────────────────────┐
│         Unity Application           │
│                                     │
│  Local Webcam                       │
│       ↓                             │
│  UnityWebCamBackend → Bryce Avatar  │
│                                     │
│  Phone IP Webcam                    │
│       ↓                             │
│  IPWebcamToTCPBridge                │
│       ↓                             │
│  NetworkedWebcamBackend → Remy Avatar│
│                                     │
└─────────────────────────────────────┘
```

---

## Key Differences From Python Solution

| Feature | Python (Old) | C# Bridge (New) |
|---------|-------------|-----------------|
| Language | Python script | Pure C# in Unity |
| Deployment | Separate process | Single Unity build |
| Dependencies | cv2, mediapipe | None |
| Setup | Run Python script first | Auto-start in Unity |
| Debugging | Python console | Unity Console |
| Platform | Python required | Unity only |

---

## Advanced: Multiple IP Webcams

To control multiple avatars from different IP webcams:

```csharp
// Avatar 1: Local webcam (Bryce)
// Already using UnityWebCamBackend

// Avatar 2: Phone 1 (Remy)
GameObject bridge1 = new GameObject("RemyBridge");
var remyBridge = bridge1.AddComponent<IPWebcamToTCPBridge>();
remyBridge.webcamIP = "192.168.1.100";  // Phone 1
remyBridge.tcpServerPort = 52730;        // Remy's port

// Avatar 3: Phone 2 (Adam)
GameObject bridge2 = new GameObject("AdamBridge");
var adamBridge = bridge2.AddComponent<IPWebcamToTCPBridge>();
adamBridge.webcamIP = "192.168.1.101";  // Phone 2
adamBridge.tcpServerPort = 52731;        // Adam's port (different!)
```

**Important:** Each NetworkedWebcamBackend needs a unique port!

---

## Performance Optimization

### If experiencing lag:

1. **Reduce IP Webcam resolution:**
   - In IP Webcam app: Settings → Video Resolution → 640x480

2. **Lower frame rate:**
   - IP Webcam app: Settings → Video FPS → 15 or 20

3. **Use Lite models:**
   - MediaPipe Model Complexity: Lite (not Full)

4. **Network optimization:**
   - Use 5GHz WiFi instead of 2.4GHz
   - Place phone closer to router
   - Reduce other network traffic

5. **Unity settings:**
   - Lower detection confidence (0.3 instead of 0.5)
   - Disable unnecessary visual effects
   - Use CPU mode instead of GPU (if GPU struggling)

---

## Common Errors and Solutions

### "Connection refused" error
**Cause:** NetworkedWebcamBackend not listening yet
**Fix:** Make sure NetworkedWebcamBackend GameObject is active and enabled

### "No route to host"
**Cause:** Devices not on same network or wrong IP
**Fix:** Verify IP with `ipconfig` (Windows) or `ifconfig` (Mac/Linux)

### Bridge connects but no frames
**Cause:** Webcam stream format mismatch
**Fix:** Try different stream paths: `/video`, `/mjpeg`, `/shot.jpg`

### Remy avatar not moving
**Cause:** PosePuppet not connected to NetworkedWebcamBackend
**Fix:** Check Remy's PosePuppet → Pose Provider field points to correct solution

### Phone keeps disconnecting
**Cause:** Power saving mode or weak WiFi
**Fix:** 
- Disable battery optimization for IP Webcam app
- Keep phone plugged in
- Use shorter stream path like `/shot.jpg` instead of `/video`

---

## Testing Without Phone

Don't have a phone handy? Test with OBS Virtual Camera:

1. Install OBS Studio
2. Start Virtual Camera
3. Modify IPWebcamToTCPBridge:
   ```csharp
   // Instead of IP webcam, capture from second webcam
   webcamIP = "127.0.0.1";
   webcamPort = 8081; // OBS MJPEG output
   ```

Or use VLC to stream:
```bash
vlc v4l2:///dev/video1 --sout '#transcode{vcodec=MJPG}:standard{access=http,mux=mpjpeg,dst=:8081/video}'
```

---

## Final Workflow

### Development/Testing (1 Device):
```
1. Start Unity
2. IPWebcamToTCPBridge auto-starts
3. Both avatars animate from different sources
4. Test, iterate, debug
```

### Production/Demo (2 Devices):
```
Device 1 (Sender):
1. Start Unity with IPWebcamToTCPBridge
2. Configure Device 2's IP
3. Phone webcam ready

Device 2 (Receiver):
1. Start Unity with NetworkedWebcamBackend
2. Listening for incoming stream
3. Avatar animates from remote data
```

---

## What You've Built

You now have:
- ✅ **Pure C# solution** (no Python needed)
- ✅ **Single device testing** (both avatars, one PC)
- ✅ **Network streaming ready** (deploy to multiple PCs)
- ✅ **IP webcam support** (any phone/camera)
- ✅ **Compatible with existing code** (uses NetworkedWebcamBackend)

## Next Steps

1. **Test single device:** Get Remy working with phone IP webcam
2. **Verify both avatars:** Both moving independently
3. **Deploy to two devices:** Split sender/receiver
4. **Add UDP pose streaming:** Use IPWebcamStreamController for final step

Need help with any step? Check Console for detailed logs!
