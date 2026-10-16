# Full-body test delivery — 30 September 2026

Install `Builds/FitFight-full-body-test.apk`. In RTMPose practice, keep head, hands and feet visible, press Calibrate and wait for ready tracking while standing upright. Turn up media volume, optionally start screen recording, then press FULL BODY TEST. Instructions and completion are vocal; no further taps are needed. The guide lasts four minutes. It temporarily selects direct body mirroring and restores both previous mode selections. STOP RECORDING, Home and app pause end capture.

## Spoken schedule

| Time | Movement |
| --- | --- |
| 0:00–0:10 | FULL BODY · STEP BACK |
| 0:10–0:16 | HOLD STILL · ARMS DOWN |
| 0:16–0:26 | ARMS FORWARD · HOLD · LOWER |
| 0:26–0:38 | ARMS OVERHEAD · HOLD · LOWER |
| 0:38–0:50 | ARMS TO THE SIDES · T POSE |
| 0:50–1:02 | BEND AND STRAIGHTEN ELBOWS |
| 1:02–1:16 | TORSO FORWARD BEND · RETURN |
| 1:16–1:30 | TORSO SIDE BENDS · LEFT / RIGHT |
| 1:30–1:44 | TORSO TWIST · LEFT / RIGHT |
| 1:44–1:54 | HEAD TURN · LEFT / RIGHT |
| 1:54–2:06 | LEFT KNEE LIFT · HOLD · LOWER |
| 2:06–2:18 | RIGHT KNEE LIFT · HOLD · LOWER |
| 2:18–2:32 | LEFT LEG · GENTLE FRONT EXTENSION |
| 2:32–2:46 | RIGHT LEG · GENTLE FRONT EXTENSION |
| 2:46–2:58 | LEFT LEG · SIDE LIFT |
| 2:58–3:10 | RIGHT LEG · SIDE LIFT |
| 3:10–3:24 | SQUAT · HOLD · STAND |
| 3:24–3:36 | HEEL RAISES · KEEP TOES DOWN |
| 3:36–3:44 | RETURN TO NEUTRAL · HOLD STILL |
| 3:44–4:00 | STEP OUT · RETURN · HOLD |

## Fixes and validation

- Downward return no longer starts the reviewed false preparation. Uncommitted cancellation can recover from a folded arm; completed strikes retain their rearm lock. Brief bilateral inhibition preserves a folded-arm reference, with fresh outgoing evidence still required on release.
- 157 punch checks passed, including both 756- and 703-observation phone fixtures. Both previously missed reviewed windows now commit exactly once, and the actual two-hand overhead segment remains negative.
- 69 full-body guide/snapshot/schema checks passed. 170 RTMPose checks passed, including 57 new arm/elbow, left/right leg and torso side-bend/recovery checks at 0/20/40° plus existing squat/forward-bend/head-motion and real GPU/model checks. The optional reference-person inference fixture was unavailable and skipped.
- Actual Play mode passed observation-to-animation-to-one-contact for both fists, direct retargeter binding/restoration from both prior selections, recording without assistance, unique observations versus repeated rendered poses, timeout/manual stop/pause/Home and reentry.
- Reviewed the actual scene/HUD capture at `Logs/Practice/full-body-guide-ui.png`.

## Recorded evidence

Schema 2 preserves old punch fields and adds native full-body image joint arrays/scores, camera geometry/mirroring/ROI, effective settings/calibration, cue schedule and speech enqueue timestamps, final feet/torso/head/owner and bag/target/contact diagnostics. Only accepted RTMPose observations are captured; missing or unaccepted frames cause gaps. MediaPipe33 values are adapter landmarks and visibility. Final rotations, root and foot positions use the existing Unity/rig convention. This revision does not encode video. Use the visible REC time/frame to align a phone screen recording.

Cue times are weak labels and enqueue timestamps do not establish audible or movement onset. Contact rejection reasons explain actual scoring; a classifier commitment alone does not produce a hit. The supplied two no-hit commitments still need the new contact instrumentation on a fresh run.

## Scope and remaining gates

M1 rotation options and M2 collection are available. Full-body recording replay/resampling and A/B metrics remain pending. Assisted kicks, jumps and new trained detectors have not been added. Torso yaw/depth remain ambiguous in the existing monocular estimator; shoulder twists and heel raises are diagnostic collection stages, not proof of accurate reconstruction. Target-phone speech audibility, movement quality and sustained performance require a fresh recording. No 70° camera-pitch claim is made.

Original edited files are preserved under `Docs/ImplementationBackups/20260930-full-body-tests/`. This folder has no Git metadata; no commit was created.

## Reports

- `Logs/Practice/punch-validation.txt`
- `Logs/Practice/full-body-validation.txt`
- `Logs/RTMPose-validation.txt`
- `Logs/Practice/punch-play-validation.txt`
- `Logs/full-body-play-validation.log`
- `Logs/full-body-android-build.log` and `Logs/full-body-test-build.txt` (build result)

## Android delivery

Unity 6000.6.0f1 built `Builds/FitFight-full-body-test.apk` successfully: zero errors, 731 reported project/compiler warnings. Build size: 261,412,720 bytes. SHA-256: `0f4a149b0938054875f6f3ab55d4986af30c5d004d52dd871c7aa138c28c0b45`. Existing APKs were retained. The installed app has not been replaced or launched on the phone by this task; use the new APK for the next guided recording.
