# External review adaptation — app 1.19

The recommendation to optimize capture, conversion and scheduling before changing the pose model is supported by the code and the last phone session. The suggested numerical improvements are hypotheses, not measured results. This release keeps MediaPipe Full, automatic exposure, 512 maximum input dimension and the existing game behavior as the baseline.

## Evidence and corrections

The accepted native frames from `20261009-192549-873` have median / p95 availability-to-result age **72.69 / 93.23 ms**, compute **43.66 / 55.92 ms**, camera conversion **11.79 / 15.96 ms**, reported camera cadence median **49.05 Hz**, and inference cadence median **19.45 Hz**. The slightly different values in the external review reflect sample selection. Recomputed values are saved in `Logs/native-pipeline-baseline-timing.json`.

The old `droppedFrames=8266` is **not 8,266 distinct lost camera images**: the provider increments it repeatedly when a render tick sees an unprocessed camera frame while inference is busy. The old native `readbackToConsumeMs` also includes compute; it is not solely result-pickup delay. Stage medians cannot simply be added or subtracted to recover the median of their sum. New explicit counters and timestamps remove these ambiguities. The legacy fallback retains its historical busy-poll counter and its freshest-frame scheduling behavior; use the new camera/worker counters for the native path.

Actual sensor-to-availability delay and display latency were not measured in that session. Estimates of 130–170 ms posture latency or 200+ ms punch latency remain unverified. A requested `[60,60]` range alone does not establish 60 delivered frames per second.

## Decisions

| Recommendation | Adaptation |
|---|---|
| 1. Frame-driven camera → inference | Implemented for native Android MediaPipe. Camera callback keeps one latest YUV image and wakes the inference worker. The worker consumes the freshest image immediately after completing the previous inference, independently of Unity render ticks. Unity takes the latest completed result in Update / LateUpdate so existing game update order remains intact. |
| Lock-free triple buffer | Three owned pose buffers with a short lock only for reference exchange. Camera conversion, inference and game processing occur outside this result lock. Correct lifetime and bounded storage matter more than claiming lock-free behavior. |
| 2. libyuv and reusable buffers | Implemented. Official source pinned to `eeb06a6451ba2e21f8ff272c7b42fc62b9faecc5`; BSD license ships in the APK. Production Android enables runtime-dispatched SIMD kernels; universal macOS validation uses portable Arm kernels. Conversion handles planar, NV12, NV21, padded rows and other pixel-stride layouts, rotation, scaling and RGBA byte order. Large buffers and scratch planes are reused. No per-pixel Java conversion or camera-sized array allocation in the camera callback. |
| 3. 288×384 input | Added 512 / 384 maximum-dimension controls. Native capture now respects the selected size; 4:3 portrait input becomes 384×512 or 288×384. Default stays 512. Smaller input can lose small/blurred wrists and feet; “no measurable loss” needs identical-input comparison and phone validation. |
| 4. Remove copies / preview upload | MediaPipe receives the worker's direct RGBA buffer pointer synchronously. Camera images close immediately after conversion. Preview copies/uploads at at most 15 Hz, independently of inference. Preview pickup is nonblocking if conversion is busy. Small metadata/JNI objects still exist; this is not a claim of zero allocations everywhere. |
| 5. Camera controls | Capability-checked EIS OFF, noise reduction FAST and edge FAST; preserve automatic exposure and supported FPS selection. Capture results now log matched sensor timestamp, exposure, frame duration, ISO, AE state and applied FPS range when available. Missing metadata stays unknown. Do not force fixed ISO/6–8 ms exposure before checking illumination and noise. TEMPLATE_RECORD is a later measured A/B, not a default assumption about Samsung. |
| 6. Priority / affinity | Camera handler uses DISPLAY priority. No URGENT_DISPLAY priority or hard-coded CPU affinity for inference. Core IDs, scheduling and thermal effects are not established by compute outliers. Profile with Perfetto before changing inference priorities or pinning cores. |
| 7. 120 Hz render | Deferred; preserve 60 render baseline. Requesting 120 does not prove 120 presentation or a fixed latency saving, and can change thermal/GPU load. Test separately after the capture change is measured. |
| 8. GPU delegate | Existing experimental selector now uses the new native capture path too. Keep CPU default. Re-test Full CPU versus Full GPU with actual delegate/fallback logged; suggested Adreno timings are not a device guarantee. Do this separately from the standard Game Body Test, which deliberately restores CPU settings. |
| 9. Lite versus Full | Exact input archiving and CPU Editor replay now support identical-input comparison. Confidence/landmark disagreement are not labelled accuracy. Keep Full until reviewed punches, feet and quiet posture justify a change. |
| 10. MediaPipe upgrade | Deferred pending a measured reason. Updating the Unity native runtime can require wrapper/ABI changes. This release does not claim to use the latest runtime or gain speed from an upgrade. |
| 11. Record inference inputs | Implemented as an opt-in **10-second lossless RGBA archive**, with camera and inference sequence IDs and timestamps, plus a final saved/skipped-frame stats file. Four reusable writer slots; full queues skip archive frames instead of blocking inference on disk. Gzip runs on a separate writer. This deliberately differs from lossy H.264 and whole-session recording: it preserves exact model inputs and limits storage/measurement interference. H.264 can follow if longer clips are needed. |
| 12. Motion-to-photon | Optional protocol below. New capture metadata can reveal some upstream camera delay when the sensor clock is comparable, but cannot measure display presentation. No end-to-end latency claim from host tests. |
| 13. TTS onset | Android utterance enqueue/start/done/stop/error callbacks are logged with utterance IDs and mapped to Unity realtime. Cues carry the same ID. `onStart` is an engine callback, not a microphone measurement of audible onset. iOS retains its existing speech implementation without claiming these Android callbacks. |

## Animation and detector proposals

- App 1.18 already added a **140 ms / 10 cm bounded moving-bag lead** and an **8 cm pelvis shift** for foot strikes. These remain, with the existing fresh-observation and physical-contact scoring gates. The review's foot gaps came from app 1.17. Re-test 1.19 on the phone before adding further prediction or a shin capsule that changes the scoring surface.
- Keep provisional leg-chamber visuals before classification. Changing knee/kick classification to image-only apex decisions requires labelled kick/held-lift sequences, especially at reduced cadence. Existing torso, head, squat and arm tracking remain available.
- The proposed overhead-gate relaxation is **not applied**. Earlier replay work showed that a simpler relaxation changed older combination timing. Preserve frozen punch and held-lift regressions; any future change needs the exact overhead interval plus older punch combinations.
- Wrist-directed punch variation (height, hook/uppercut direction) is promising, but changing authored trajectories also changes contact timing. It is queued after capture latency is validated, using exact inputs and existing physical-contact checks.
- Lucas–Kanade wrist tracking is deferred until camera timing is measured. It needs occlusion/blur/reacquisition confidence and ownership tests; a 2D patch is not evidence of hand depth or a scored strike. A future experiment may drive visual preparation only.
- Temporal lifters, learned action classifiers and multi-model fusion remain later research. They require labels and measured latency; they do not remove the capture/scheduling overhead addressed here.

## Lifetime and recovery

The camera keeps at most one waiting image. Four ImageReader slots allow one consumed image, one retained image and the two-slot margin required by `acquireLatestImage`. Conversion closes the consumed image before MediaPipe begins. Close/retry waits for the conversion section, releases retained images, and preserves existing bounded open/recovery logic.

Every new inference consumer gets a separate direct buffer generation. This is essential because model switching keeps the camera open while the previous inference worker finishes. Pixel buffers and matching metadata stay together; a previous worker cannot overwrite the new model's input. JNI threads attach/detach explicitly. Native model initialization, inference and destruction remain on the same worker, including GPU fallback.

Calibration resets reject captures from before the reset, and camera/orientation changes reset reconstruction. No new prediction may score an unobserved strike. Latest completed pose replacement keeps storage bounded if Unity stalls.

## Phone test — first validate the pipeline

1. Install `Builds/FitFight-frame-driven.apk` (1.19 / code 12), fully close/reopen once.
2. Use your preferred **floor-level front camera**, with head and both feet in frame and good light. Keep its placement/tilt/distance matched to your prior run. Changing angle at the same time would obscure the pipeline comparison.
3. Select **MediaPipe Full**, native camera ON, GPU OFF, input **512**, 60 camera FPS. Use the existing **Game body test · 2:44**; it enables the established game settings, assistance, per-region filtering and stable body motion, and turns direct-avatar mode off.
4. Keep input archiving **off** during this baseline timing run. Record the screen/audio as before. A 15 FPS preview is intentional; a multi-second frozen preview or avatar is not.
5. Pull the new full test JSONL and summaries from **PunchSessions**. Input archives, if separately requested, are under **MediaPipeSessions**:

```sh
adb install -r Builds/FitFight-frame-driven.apk
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/PunchSessions/ ./
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/MediaPipeSessions/ ./
```

Compare conversion median/p95, camera interval/frame duration (JSONL interval histogram bins: <10, 10–18, 18–25, 25–35, 35–50, 50–100, ≥100 ms), inference FPS, pose age median/p95, result-pickup delay, native fallback/runtime errors, fast-punch misses and quiet-posture stability. **≥22 Hz inference and ~50 ms pose age are experimental targets, not release promises.** More inference FPS alone does not establish better punch accuracy.

If capture is stable, make one short separate input archive: Tracking → **Capture model inputs · 10 seconds**, then alternate a few punches, a knee lift and a front kick. It writes `.ffr.gz`, `.ffr.gz.json` and `.ffr.gz.stats.json`; capture ends automatically. Archive copying/compression can affect timing, so do not use that clip as the clean latency benchmark. The clip is roughly 16 MB/s uncompressed at 20 Hz for 384×512; gzip size depends on the scene. Four slots bound in-memory pixel storage to 4 MiB; only ten seconds are collected.

Optional later A/B: use 384 input at the same placement/model, then Full GPU through the experimental selector. Change one option per run. The standard Game Body Test restores CPU, so GPU checks should use the existing MediaPipe diagnostic test or sanity check. Full/Lite offline comparison should reuse the same archive; head/torso placement does not need changing to test this pipeline.

## Exact-input tools

```sh
python3 NativePose/tools/read_input_archive.py /absolute/path/clip.ffr.gz --export /tmp/pose-input-frames
FF_POSE_INPUT_ARCHIVE=/absolute/path/clip.ffr.gz /Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "$PWD" -executeMethod PoseInputReplay.RunBatch -logFile "$PWD/Logs/pose-input-replay.log"
```

The Editor comparison runs Full/512, Full/384 and Lite/512 over the same ordered clip and timestamps; it saves per-input CSVs and a comparison in `Logs/PoseInputReplay/`. Reported compute is host compute. Smaller-input landmark differences compare against Full/512, **not ground truth**. PNG exports are lossless. The archive records evaluated inputs, including no-person frames; writer drops can leave sequence gaps.

## Optional motion-to-photon check

If a second phone is available, film the player and the game display in the same high-frame-rate shot, with both clearly visible. Use isolated punches with a pause between them. Count recorded frames from the first visible hand movement to avatar-arm onset and then to the bag-hit flash. At 240 FPS one frame is about 4.17 ms; camera shutter and screen scanout still limit precision. Repeat both hands at least five times and report median/range, separate posture onset from game contact. This is optional and does not block the regular floor-camera run.

## Primary references

- [libyuv source and license](https://chromium.googlesource.com/libyuv/libyuv/+/eeb06a6451ba2e21f8ff272c7b42fc62b9faecc5/README.chromium).
- [Android ImageReader ownership / latest-image margin](https://developer.android.com/reference/android/media/ImageReader#acquireLatestImage()).
- [Camera2 request controls](https://developer.android.com/reference/android/hardware/camera2/CaptureRequest) and [capture results](https://developer.android.com/reference/android/hardware/camera2/CaptureResult).
- [Unity JNI thread attachment](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AndroidJNI.AttachCurrentThread.html) and [direct-buffer pointers](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AndroidJNI.GetDirectBufferAddress.html).
- [Android utterance progress callbacks](https://developer.android.com/reference/android/speech/tts/UtteranceProgressListener).
