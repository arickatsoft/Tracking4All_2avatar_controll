# Frame-driven MediaPipe delivery — app 1.19

**Ready for a floor-camera phone test.** APK: `Builds/FitFight-frame-driven.apk`; package `com.DefaultCompany.Tracking4AllUnity`, version **1.19**, code **12**.

The camera now hands latest YUV images to an autonomous inference worker. Native libyuv converts only consumed frames; MediaPipe reads the worker-owned direct RGBA buffer. Main-thread preview pickup is nonblocking and uploads at up to 15 FPS. Pose result ownership uses three buffers with a short exchange lock. Model changes receive separate pixel-buffer generations so a finishing worker cannot overwrite the next model's input.

New evidence tools include per-stage timing, actual camera interval bins/capture metadata, separate replaced-image/result counters, Android TTS callbacks linked to cue IDs, input-size controls, ten-second lossless input archives, and identical-input Full/Lite/resolution replay. The native MediaPipe runtime, detector policy, hybrid animation, contact radii and fresh-observation scoring gates remain the established baseline.

Read `Docs/External-recommendations-adaptation.md` for the review of every recommendation, the deferred experiments and detailed test instructions. `Docs/Game-body-test-plan.md` is updated for this APK.

## Verification

- Final Android build: **succeeded, zero errors**; 751 warnings reported by the Unity build. See `Logs/frame-driven-build.txt` and `.log`.
- **19 pipeline/archive checks**: autonomous real inference without render ticks; latest-result replacement; immutable consumed results; camera replacement and teardown; failure propagation; exact input bytes/timestamps/identities; bounded recording/draining. `Logs/frame-driven-pipeline-validation.txt`.
- **432 native conversion checks**: planar and interleaved YUV, padded rows, buffer offsets, RGBA byte order, all right-angle rotations, odd dimensions, invalid capacities and retained scratch-buffer reuse. `Logs/native-camera-conversion-validation.txt`.
- **8 production Java/JNI host checks**: independent model buffer generations, stale consumer cancellation, nonblocking preview, direct-buffer slices, capacity rejection and repeated close. `Logs/native-camera-lease-validation.txt`. This uses a host converter shim; it does not emulate a physical Camera2 device.
- Android API 36 Java camera/speech compilation succeeded; existing **26 ownership/recovery** and **52 pixel/orientation** checks passed.
- Existing real-engine comparisons passed for native RTMPose, MediaPipe Lite and Full, including blank-frame and in-flight disposal checks. `Logs/engine-comparison-validation.txt`.
- **108 synthetic full-body checks**, frozen October boxing/priority/reconstruction replays, body smoothing and strike-contact checks passed in the final build batch.
- Full scene replay of the latest recorded session passed: **all 13 main punch decisions reached the bag**, both held knee lifts remained non-scoring, quiet head deviation **9.01°**, planted-foot frame step **0.00 cm**, no host observation replacement. The moving-bag simulation produced **33 contacts / 8 feet** this run; bag dynamics and host step timing vary, so this count is not a detection-accuracy improvement. `Logs/Practice/frame-driven-latest-scene.txt` and `Logs/frame-driven-latest-scene.log`.
- The identical-input tool replayed six real standing-fixture inputs through Full/512, Full/384 and Lite/512, all detecting a person. This proves the archive/replay path, not fast-punch accuracy. `Logs/PoseInputReplay/20261010-071419/comparison.md`.
- APK signature, version, defined Java classes, final IL2CPP entry points, both rebuilt native ABIs, dynamically exported JNI conversion, converter 16 KiB segment alignment and shipped libyuv license verified. Packaged executable code matches the rebuilt converter despite debug-symbol stripping. MediaPipe JNI is byte-identical to 1.18. `Logs/frame-driven-apk-verification.txt`.
- Earlier 1.16, 1.17 and 1.18 APK hashes remain unchanged.

APK bytes: **286,723,147**. SHA-256:

`dfcf340e2908ebe3662e8d67c471b1eb63f935801484e4a871f7fe04f4bef081`

## Next phone run

Use the same floor/front-camera placement and lighting, select MediaPipe Full / native camera / CPU / input 512 / 60 camera FPS, then run **GAME BODY TEST · 2:44**. Keep optional input archiving off for this timing baseline; the test applies assistance, adaptive region filtering, stable body motion and direct-avatar off itself. Fully close/reopen after installation.

Send the new screen video and the new JSONL, summary JSON and summary Markdown from **PunchSessions**. The separate optional input archive is under **MediaPipeSessions** and includes metadata and final saved/skipped-frame stats. The review document includes both `adb pull` commands.

Phone camera behavior, real speech callbacks, GPU behavior, actual cadence, thermals and end-to-end display latency remain unverified. The 72→50 ms estimate and ≥22 Hz target are hypotheses to test, not measured release results. Existing generic posture tracking remains enabled.

## Reproduction / source

Source backups: `Docs/ImplementationBackups/20261010-native-pipeline/`. No Git repository is present in this workspace. libyuv revision is recorded in `NativePose/sdk/libyuv-revision.txt`; source hashes are in `NativePose/libyuv-source.sha256`. `NativePose/build.sh` rebuilds native libraries and ad-hoc signs the universal macOS validation library. The new Editor build command is `MobileTrackingBuild.BuildFrameDrivenBatch`; APK verification script is `Logs/verify-frame-driven-apk.py`.

Legacy diagnostic field names are retained for compatibility. On the new native path `scheduledToReadbackMs` spans conversion, not GPU readback, and `readbackToConsumeMs` includes inference. Use the explicit `cameraConversionMs`, `cameraQueueMs`, `workerQueueMs`, `resultPickupMs`, inference start/end and sensor clock flags for stage analysis. Histogram bins are documented in the adaptation review. Camera counters cover the lifetime of that camera instance; they are not a direct count of sensor exposures lost.
