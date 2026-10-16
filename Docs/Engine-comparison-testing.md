# Live pose-engine comparison

Install `Builds/FitFight-engine-comparison.apk` over the previous FitFight build. Choose the engine on the home screen or in Practice → Tracking:

1. Tracking4All baseline.
2. RTMPose · Unity GPU (the existing path).
3. RTMPose · native ncnn (CPU, two threads).
4. MediaPipe Pose Landmarker Lite (CPU).
5. MediaPipe Pose Landmarker Full (CPU).

Enter Practice and tap **SANITY CHECK · NO RECORDING**. This turns punch assistance off, enables direct avatar tracking for the alternative engines, recalibrates, and starts a roughly one-minute spoken guide. Stand still with your whole body visible until calibration completes. The guide asks for arms to the sides, overhead arms, each elbow, torso bends/leans, each knee lift and quick punches. It waits for calibration before starting movement cues. Returning home, selecting another engine, or starting a recording stops this guide. The sanity check stops an existing session/benchmark-CSV recording and does not start session, video or benchmark-CSV recording. Existing optional recording buttons remain available.

Keep the camera and your distance unchanged across engines. Recommended starting setup: camera at waist height, phone screen vertical, lens level, about 2.5–3 m away; keep feet, head and fully raised hands inside the frame. Keep lighting constant. After changing engines, tap Sanity Check again and hold still for its fresh calibration.

The HUD shows the actual engine, camera/AI/accepted-pose FPS, frame-availability-to-pose delay, backend time and left/right leg status. Compare visible delay, repeated missed movements, jitter at rest, bends, hidden wrists and knee motion. Faster processing is useful only if the movements still follow correctly. Backend times differ in scope between CPU native calls and Unity GPU schedule/readback; use frame-to-pose for the shared comparison. Timings exclude exposure, camera buffering before Unity sees a frame, and display latency.

If a backend fails to load, Tracking shows the failure and identifies the active fallback. **Retry selected engine** retries loading. Models are bundled and do not need downloads on the phone. Performance on your Samsung is not inferred from the Editor tests; your live check is the next comparison.

Implementation/build/validation details are in `NativePose/README.md` and the reports under `Logs`.
