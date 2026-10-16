# MediaPipe Full improvement test and labeling plan

Current build: **FitFight-mediapipe-idle-fix.apk**, app version **1.10**. The original 1.09 legacy/region comparison is recorded separately in the session analysis. Focus on MediaPipe Full. This build changes the processing around the model; it does not retrain the model or upgrade the Unity plugin. Plugin 0.14.4 / upstream MediaPipe 0.10.14 and Full weights are held fixed to isolate processing changes. Other engines remain available.

## What changed

MediaPipe now acquires a still, straight 3D standing reference without the RTMPose 2D reconstruction solver. After calibration, torso projection/visibility no longer rejects an entire otherwise usable MediaPipe pose. Each joint still needs usable confidence, finite coordinates, a fresh frame and the existing motion safeguards. No-person frames clear observed joints immediately. The existing avatar rig supplies fixed bone lengths through bone rotations.

**MediaPipe per-region filtering (A/B)** now retains legacy dead zones/cutoffs at rest and blends toward faster wrist/elbow/leg settings when confident image motion is visible. The original static 1.09 region preset passed more idle depth noise; the 1.10 motion-aware preset corrects that regression. This remains a processing preset, not a proven accuracy increase. Turning it off keeps the previous shared joint-filter settings, while retaining the calibration/acceptance fixes. Direct bone smoothing and speed limits remain separate; leave both off for the first comparison to avoid another variable.

## First test: two short runs

1. Install the new APK over the existing app. Select **MediaPipe Full** and enter **Practice**. Keep the same avatar for both runs.
2. Use a fixed camera at waist/torso height, screen vertical and lens roughly level. Start about 2.5–3 m away and adjust until head, feet and fully raised hands are visible. Keep lighting and phone placement unchanged. Floor placement is also acceptable if it gives better framing; do not change placement between these runs.
3. Open **Tracking**. Set your height. Keep adaptive filtering on, direct avatar on, direct bone smoothing off and direct speed limit off. Use the same requested camera FPS in both runs (start with 60; actual FPS is measured). Leave prediction and confidence at the same values.
4. Turn **MediaPipe per-region filtering (A/B)** **off** for the legacy-filter run. Tap **MEDIAPIPE TEST · SAVE RESULTS** in Practice or Tracking. It selects Full, turns punch assistance off, recalibrates and saves diagnostics. Stand still with arms down until calibration finishes. The guide waits up to 60 seconds; failure produces an incomplete summary.
5. Follow the spoken cues, approximately **112 seconds after calibration**. Perform only comfortable movements. The current cue is also displayed. Tapping the test button again stops early and saves a partial result. Changing settings, recalibrating, rotating the camera, switching engines, starting another recording, pausing, or leaving Practice also stops the test; repeat it with a fixed setup.
6. Open **Tracking → Show MediaPipe results**. Note the profile and whether the test completed. On 1.10, the enabled preset is labeled **region-motion-aware** in saved results. The screen shows per-movement accepted FPS, pose p95 delay, and wrist/knee availability. Full median/p95 timing, rejection counts and stillness statistics are in the saved report.
7. Turn the per-region option **on**, then repeat with the same setup. For a stronger comparison, do a second pair in reverse order (region, then legacy) after a short rest; heating and familiarity can affect results.

The guide covers standing still; arms sideways and overhead; left/right elbows with upper arm raised; forward and sideways torso bends; left/right knee lifts; shallow squats; slow and fast punches; brief left-wrist occlusion; and a final still period. A cue is a requested action, not verified ground truth.

## What to send back

For the first pair, send the two **.summary.json** files and their matching **.jsonl** files. Optional readable **.summary.md** reports are alongside them. The Tracking results panel displays exact file paths. Files are in:

`Application.persistentDataPath/MediaPipeSessions/`

Typical Android location for this package:

`/storage/emulated/0/Android/data/com.DefaultCompany.Tracking4AllUnity/files/MediaPipeSessions/`

Use the actual path shown by the app. For a computer with Android platform tools and an authorized USB-debugging connection:

```sh
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/MediaPipeSessions ./MediaPipeSessions
```

The app does **not encode video**. If you see missed or delayed movements, make a matching phone screen recording with the camera preview visible. Keep the on-screen cue in the recording so we can align it. A side-view recording from another device is useful specifically for forward/backward depth errors, but is not required for the first pair. Screen recording adds load, so either record both runs or neither when comparing timings.

Also send a short note per run: worst body region, noticeable delay, repeated misses, steadiness while still, and any incorrectly performed cue. A screenshot of results is a useful quick report but cannot replace the JSONL when investigating a failure.

## How the recorded evidence is used

Each completed inference is logged, including no-person, calibration, stale and rejected results. Diagnostics contain raw MediaPipe33 image/world landmarks and confidence, calibrated/filtered Halpe26 joints, capture/receive timestamps, acceptance reason and active cue. Avatar records contain humanoid bone rotations/positions and source/applied sequence IDs. Cue timestamps are speech enqueue times, not audible onset. The stream is written on a bounded background writer; dropped-record counts and errors are reported.

Calibration is excluded from movement statistics. Tracking availability counts accepted, observed wrists/knees against **all evaluated movement frames**, including detection loss. It is not accuracy. Median/p95 pose delay uses accepted frames; rejection counts expose slow frames that fail the 200 ms gate. Avatar timing uses the first observed application of each pose and reports its sample count. Timings exclude exposure, camera buffering before Unity receives a frame, and display latency. Stillness RMS uses at least two accepted frames, after a two-second cue transition, across wrists/knees; raw positions are pelvis-relative and scaled to the same calibration units as filtered positions. Intentional motion invalidates a stillness comparison.

## Labeling and improvement cycle

Generate an editable movement table on the computer:

```sh
python3 Tools/MediaPipe/prepare_labels.py path/to/session.jsonl
```

Review the matching video. Mark `performed_correctly` as **yes**, **no** or **uncertain**. Record body region and failure type (**missed**, **late**, **jitter**, **depth**, **occlusion**, or **none**) and severity 0–3. Split a row into shorter intervals when a failure occupies only part of a cue. Align `video_start_s` manually using visible cues; file start is not the same as video start. Unreviewed/no/uncertain intervals are not correctness ground truth.

For detailed accuracy evaluation, annotate visible shoulder/elbow/wrist/hip/knee/ankle locations on selected frames, including occluded/out-of-frame flags. Use approximately 5–10 frames around each repeated failure rather than labeling entire videos. 2D annotations evaluate visible joint position and movement timing; one camera cannot establish true 3D accuracy.

The earlier full-body recording's **2:22–2:32 incorrect left-knee action** remains excluded from correctness evaluation. Its old JSONL is not MediaPipe evidence; the video can be reprocessed later with a fixed MediaPipe model if needed.

Use one correctly performed session for tuning and a separate repeat for validation. Change one stage at a time: confidence/acceptance, wrist/elbow filtering, torso mapping, leg mapping, then bounded occlusion handling. Accept a change only if it reduces the labeled failure without increasing tail delay, rejection rate, jitter, or failures elsewhere. Keep raw MediaPipe outputs as the reference and use held-out clips, not only the examples used to tune.

After this comparison, test the Unity MediaPipe runtime upgrade in isolation, using the same weights/settings and regression checks for the existing body/face integrations. Consider a second detector only if labeled examples show a repeatable MediaPipe weakness that it can fix within the phone's processing budget. Collecting labels here evaluates and tunes the application; it does **not automatically train MediaPipe**. Custom model training would require a separate dataset and compatible training/export pipeline.
