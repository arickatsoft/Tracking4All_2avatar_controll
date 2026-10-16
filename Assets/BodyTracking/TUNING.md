# Tracking setup and tuning

## Stable motion update — 15 September 2026

The old rigid torso/head lock has been replaced by calibrated, bounded movement. **Stable body motion / floor camera correction** keeps camera perspective correction while allowing forward bends, side leans and head turns. Squats lower the pelvis through bent legs and the existing shoe contact correction. RTMPose Body26 is still a 2D model: forward bend and limb depth are estimates, not recovered 3D measurements; a frontal view cannot reliably distinguish every bend, twist or motion toward the lens.

Start with **Tracking → Apply stable / fast preset**, then calibrate in the phone's final position. Stand still, upright, looking forward, with both feet down and your face, hands and feet visible. Wait for calibration before exercising. The preset enables direct avatar control to avoid the puppet retargeter’s additional limb twist; you can still switch back for comparison. Existing saved tuning is retained until you apply the preset; press **Save responsiveness settings** to keep adjustments.

The movement filter uses an anchored spatial dead zone followed by speed-adaptive One Euro smoothing. Small noise does not accumulate velocity; large movements raise the cutoff in the current observation. Bone vectors are filtered *before* foreshortening is converted to depth. Missing joints stop extrapolating immediately, hold visually for a short grace period, and then blend toward a guard. Confidence hysteresis requires a stronger detection to reacquire a lost joint. Implausible teleports are quarantined until another consistent sample arrives. Display holds are excluded from hit detection.

| New control | Default | Adjustment |
|---|---:|---|
| Ignore small joint noise | 1.8 cm | Increase toward 2–3 cm for stationary jitter; reduce if deliberate small movements disappear. Large movements escape the dead zone; small movements accumulate against the held position. |
| Lost joint reacquisition margin | 0.08 | New/lost joints require the confidence gate plus this margin; already tracked joints use the normal gate. Raise for repeated false reacquisition, lower if visible joints recover poorly. |
| Body / head noise | 2.5° | Angular dead zone before adaptive filtering. Raise for shaky head/torso, lower for finer motion. |
| Maximum forward bend | 65° | Bounds the ambiguous monocular bend estimate; it does not lock the torso. |

Tune rest noise first with prediction at zero and motion response low. Then perform fast punches/kicks and increase motion response until catch-up is satisfactory. Finish with slow squats/bends to check that the dead zone is not too large. Only then add modest prediction. Smoothing cannot remove the camera/inference delay; avoid increasing prediction to compensate for a slow model, because it amplifies errors and overshoot.

Research basis: the [One Euro filter authors](https://gery.casiez.net/1euro/) describe tuning the rest cutoff first, then increasing speed response to reduce lag. [Microsoft's skeletal tracking guidance](https://learn.microsoft.com/en-us/archive/msdn-magazine/2012/may/kinect-multimodal-communication-with-kinect) distinguishes smoothing, prediction, jitter radius and maximum deviation, and notes the latency cost of stronger smoothing. [MediaPipe's pose documentation](https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker) treats detection, presence and tracking confidence as separate controls. Our RTMPose score gates and angular limits are project-specific, not parameters copied from MediaPipe or calibrated probabilities.

Validation: `Logs/RTMPose-validation.txt` includes synthetic camera projections at 0°, 20° and 40°, slow motion, missing joints, and rest/noise/onset checks at 10/30/60 Hz. `Logs/Practice/floor-camera-avatar-validation.txt` records rendered humanoid checks for both retargeting modes. These are synthetic regression checks, not a measurement of phone end-to-end latency.

## Calibrate

In **RTMPose**, set your actual height, place the phone in its final position, then press **Calibrate floor camera** (or CALIBRATE in practice). On a phone, static gravity estimates the upward camera tilt; the solver resets its crop, history and body scale, then averages the next **20 confident shoulder/hip observations within a short window**. Stand upright, face the camera and keep both feet and hands visible until “Calibration captured.” It is an approximate monocular scale/tilt calibration, not measured 3D depth, a floor-plane scan, action learning or latency calibration.

For a manually chosen tilt, use **Apply manual tilt / reset crop**; the floor-camera button overwrites manual tilt with the gravity estimate on mobile. Recalibrate after moving/rotating the phone or changing height. Calibration is per session. Do not throw a kick while scale is being captured.

In **Tracking4All**, practice CALIBRATE resets the bag and retries the camera if missing. It does not apply RTMPose height/tilt calibration. **RESET BAG** only restores the bag suspension/contact state; it does not recalibrate pose tracking.

## Controls

These RTMPose settings belong to our Unity tracking/retargeting layer; they do not change the pretrained network's weights, resolution or neural inference speed.

| Control | What it changes | Starting value / adjustment |
|---|---|---|
| Adaptive filtering | Smooths pose coordinates before retargeting | On. Off is useful only for comparing raw noise. Prediction remains a separate control. |
| Filter cutoff | Rest/slow-motion smoothing | 1.5 Hz. Lower is steadier but delays slow motion; higher follows jitter more closely. |
| Motion response | Raises cutoff as joint velocity grows | 12. Raise if fast movements lag; lower if moving limbs shake. |
| Velocity cutoff | Smoothing of the velocity estimate used for response/prediction | 12 Hz. Lower reduces noisy velocity; higher reacts faster. |
| Prediction | Additional velocity lead beyond estimated processing delay | 10 ms. Start at 0 when tuning, then try 5–15 ms. Total extrapolation is capped at 40 ms; this does not complete an action. |
| Joint confidence | Minimum keypoint score for accepted measurements; initial torso acquisition requires at least 0.50 for 20 accepted frames in a cadence-adjusted window (2.5–8 seconds) | 0.30. Raise toward 0.4–0.5 for false detections; lower cautiously if clearly visible joints drop out. Scores are not calibrated probabilities. |
| Leg movement threshold | Fraction of leg length that must change before the puppet updates a leg | 0%. Increase only slightly (0.5–1%) for planted-leg noise; larger values suppress subtle kicks. Does not apply to direct-avatar mode. |
| Occlusion grace | Brief hold after joint loss | 80 ms. Runtime uses at least 1.5 measured observation intervals, capped at 200 ms for that cadence allowance. Whole observations expire at 250 ms from capture. Increasing this does not make inference faster. |
| Depth response | Base speed limit for estimated limb depth changes | 6 m/s. Higher follows kicks faster but can amplify monocular depth errors. |
| Depth motion gain | Raises the depth limit when projected image motion is faster | 2. Lower if knee depth pops; raise cautiously if depth response trails fast kicks. |
| Prediction distance cap | Maximum extrapolated joint offset | 12 cm. Reduce if hands/feet overshoot. It limits extrapolation, not the physical bag contact radius. |
| 30 / 60 FPS | Requested camera rate | Try 60, then inspect **actual** camera/pose FPS. A request is not a guarantee. |
| Direct avatar | Bypasses the puppet and rotates humanoid bones directly | Experimental A/B comparison; does not speed up camera capture or neural inference. |
| Height / tilt | Body-scale prior and approximate camera elevation correction | Your actual height and measured/static phone angle. Recalibrate after changes. |
| Save responsiveness settings | Persists RTMPose controls | Existing saved preferences are retained; use Reset balanced defaults to load the revised 2 Hz / 15 ms defaults, then Save. |
| Record benchmark CSV | Records rates, software timing and tracking state | Use identical camera/lighting/movements to compare settings. Timing excludes camera exposure and display scanout. |
| Retry camera | Reopens the local camera | Use after granting camera permission or connecting a camera. No-device and 10-second startup failures terminate cleanly. |

**Tracking4All controls** are separate session settings. **Rest cutoff** (start 1.5 Hz) smooths small position errors; lower reduces shake. **Movement catch-up** (start 20) increases response as the measured pose moves farther from the displayed pose; higher reduces movement lag. They configure the local scene's existing SimpleSmooth stage. Other MediaPipe/internal graph smoothing can still contribute latency. The legacy Inspector SimpleSmooth `speed` applies where adaptive body mode is off; higher is faster/noisier. EuroSmooth is a separate legacy modifier and is not the scene's active SimpleSmooth implementation.

## Tune in this order

1. Fix capture first: stable phone, bright even light, full body with margin for both extended arms and kicks. Use the smallest upward angle that fits the whole body. A floor view creates foreshortening and self-occlusion; filters cannot recover unseen joints. Confirm the preview is the intended camera.
2. Check pipeline status. **Camera FPS = 0** means a capture/permission/device problem. Camera running but **inference = 0** means an inference/backend problem. Inference running but **pose = 0** means rejected torso/old observations; inspect status and joint scores before changing smoothing.
3. Set height/tilt and calibrate upright. Wait for calibration captured. A partial body or empty room is not a valid calibration target.
4. Tune standing still: set prediction and motion response to 0, leg threshold to 0, filtering on. Lower cutoff until rest noise is acceptable (start 1.5 Hz).
5. Test slow then fast movements on both sides. Increase motion response (start 8) to reduce lag while preserving rest stability. This follows the original One Euro tuning procedure: https://gery.casiez.net/1euro/ .
6. Add only a little prediction. Check abrupt stops and direction reversals; reduce prediction/distance cap if a limb continues after you stop. Do not use long prediction or automated move completion to manufacture hits.
7. Adjust confidence only after confirming visibility. Check left and right knees/ankles separately. Tune depth last, since it is inferred rather than measured. Compare direct-avatar mode on the same movements.
8. Compare actual FPS/timing at 30 and 60 requested FPS, warm up, record the same sequence, and repeat after several minutes to expose device heating. Save the best settings. No camera system has literal zero physical latency.

## Changes and validation (September 13, 2026)

- Valid RTMPose results no longer expire immediately simply because processing exceeded the configured occlusion grace. Processing delay and time since delivery are separate; freshness and prediction remain bounded.
- The confidence slider now also controls torso acquisition and puppet shoulder acceptance. Status reports no camera, rejected torso scores, slow inference, or live poses.
- Tiny velocity reversals no longer bypass position smoothing. Balanced reset defaults are 2 Hz and 15 ms (saved user settings are preserved).
- Tracking4All's local smoothing responds more strongly to larger motion. Toe/heel loss no longer invalidates a visible knee/ankle chain. Confidence hysteresis and a 100 ms display hold reduce intermittent leg fallback; display-only held legs cannot score practice hits.
- The legacy ground-lock script defers to FitFight's existing per-leg guard to prevent competing bone writers.
- Confirmed bag contacts emit a green splash and one matching hit sound. Misses, held contact, reacquisition and teleports do not score.

Unity reports: `Logs/RTMPose-validation.txt`, `Logs/Practice/validation.txt`. Recorded-person inference is a regression fixture, not a measurement of live phone accuracy. Live motion quality and physical end-to-end latency still need a person in frame on the target phone.

The avatar starts following RTMPose only after initial calibration succeeds. This reduces isolated background detections; a pose-only neural model can still produce false positives, so a dedicated person detector remains a possible future improvement.

Startup preview: detected RTMPose landmarks appear in amber before calibration, then green once calibrated. Short confidence dips pause calibration instead of restarting it; gaps beyond the measured cadence allowance (250–750 ms) without a confident torso reset acquisition. Avatar motion remains disabled until calibration completes. Both the practice HUD and Tracking panel display progress. No separate model download or direct-avatar toggle is required.


## Phone startup repair (September 14, 2026)

Practice starts camera capture automatically. The backend waits for the Android permission result, serializes competing local camera owners, retries failed/stalled capture, and resumes after backgrounding. Denied permission is reported separately and is not bypassed. Connecting a camera or granting permission no longer requires using Calibrate as a restart button.

RTMPose resolves the camera into a standard GPU texture before compute preprocessing. This avoids directly binding a phone's external camera texture to the model's compute input and keeps a stable frame during inference. The maximum snapshot width is 960 pixels; the model input remains 192×256. Camera orientation/mirroring still follow Unity's camera properties, and require verification on the target phone.

Initial calibration now measures inference cadence before any avatar poses are published. At 5 FPS, 20 confident frames can take four seconds, which previously could never fit the fixed 2.5-second acquisition window. The allowance now follows cadence, with a maximum eight-second window and a maximum 750 ms gap; confidence requirements and live-pose expiry remain in force.

The floor-camera Calibrate button now starts a **five-second step-back countdown**. Return to your final full-body position during that countdown, then stand upright. Amber detections remain available while waiting. The practice screen shows camera/inference FPS while calibrating, and reports inference over 200 ms as too slow instead of asking repeatedly for calibration. Detailed tracking controls also show camera texture dimensions, rotation, vertical flip and inference duration. Development builds log this health information every three seconds under `[FitFight tracking]` for targeted device diagnosis.

Unity regression tests cover the resolved camera texture input, calibrated reference-person knees, calibration at 5 FPS and confidence-gap expiry. Editor camera startup was measured at approximately 30 FPS without pressing Calibrate. These checks do not establish the target phone's live accuracy or inference speed.


### Phone diagnostic revision 2026-09-14b

The Tracking panel displays this revision. Raw detections from inference taking 200–1000 ms remain visible in amber for diagnosis; they cannot calibrate or drive the avatar. Raw previews expire independently of live-pose freshness. Phone release builds now log camera/inference/render FPS, inference duration, orientation and torso scores every three seconds, without recording camera images. This distinguishes slow inference from bad image orientation or weak detection. The diagnostic APK uses normal release optimization so timing is comparable to the regular phone build. The 200 ms live-pose acceptance limit has not been loosened.


### Android camera frame scheduling repair, revision 2026-09-14c

Live SM-S928B logs showed 30–60 camera FPS with zero sustained inference FPS: individual results appeared after Calibrate, then stopped changing. RTMPose's duplicate-frame guard relied on `WebCamTexture.updateCount`. It now uses the backend's own monotonically increasing sequence, advanced on `didUpdateThisFrame`, instead. The model can consume each newly delivered frame even when the native texture counter stays unchanged. The diagnostic log includes both the native texture counter and completed inference count to verify this on the phone.


Verified on SM-S928B after installing revision 2026-09-14c: the native texture update counter stays at **0**, but the backend frame sequence now drives continuous inference. Observed inference was roughly 9–13 FPS at 69–109 ms frame-available-to-result time; calibration completed, green landmarks appeared, and the avatar moved with the user while the bag hit counter advanced. This confirms the frame-counter gate caused the phone's one-frame-after-Calibrate behavior. Logs: `Logs/phone-rtmpose-verification.txt`; screenshot: `Logs/Practice/phone-tracking-fixed.png`. This is not a zero-latency or exhaustive movement-accuracy certification.
