# Strike contact and camera handoff — app 1.18

APK: `Builds/FitFight-strike-contact.apk` — **app 1.18, version code 11**, package `com.DefaultCompany.Tracking4AllUnity`. Build succeeded with **0 errors and 750 warnings**. Size: **286,658,336 bytes** (273 MiB). Android minimum API 26 / target API 36.

SHA-256: `ccb78d988cd678f8a0d7c3a8e7cdf37bc17b1b067ed832336f40251bbb895f7e`.

Install as an update from the project directory:

```sh
adb install -r Builds/FitFight-strike-contact.apk
```

This update follows the app 1.17 floor-camera session `20261009-192549-873`. The input and review remain under `Logs/MediaPipe/20261009-192549-873/`. No new phone recording was available while implementing this update.

## Changes

- A confirmed foot strike now transfers the pelvis up to 8 cm forward while constraining the supporting foot. The prior 10 cm retreat reduced usable leg reach. This changes the animated stance, not the detected action.
- The foot goal captures a short lead from the bag's measured velocity at commitment: 140 ms, capped at 10 cm. It remains fixed for that action. The original surface offset remains. Knee targeting and confirmation are unchanged.
- Guard posture gets a stable pelvis-height reference before observed squat and balance offsets. This prevents offsets accumulating across manual zero-time animation evaluations. Active punch clips retain their authored vertical motion and existing detection/gating. Ambiguous single-arm upward strokes retain boxing priority.
- Model changes and explicit inference retries retain camera ownership while suspending the baseline graph. Capture mode and frame rate are configured together, avoiding intermediate camera opens. Model failures still restore the baseline and its original frame-rate preference.

The punch gesture gate remains unchanged in this release. A candidate lowering-rule adjustment changed older combination timing and was deferred pending a separate regression-safe experiment.

All scoring still requires a fresh verified source, the original action window, and an actual approaching swept limb contact. Captured targets and held display poses do not grant impact permission. Each action scores at most once.

## Test design and limits

The old four-knee-drive scene check proved sensitive to pendulum phase and host timing. A moving bag can enter the chambered knee's space (preventing rearming) or leave thigh reach. Original-controller and updated-controller diagnostic reports are preserved; neither speech cues nor a detector decision imply a guaranteed physical hit. The isolated 1.15 leg/rig checks now hold the bag fixed and retain their original required contacts. Full 1.16 and 1.17 recordings independently exercise the freely moving bag.

The latest session's quiet head target itself changes about 8.4 degrees in pitch. Its scene check allows up to 12 degrees of rendered world-head change and a maximum 2.5 cm planted-foot frame step. The older, quieter session retains its original 8-degree limit. This is a stability guard, not an accuracy label.

The complete latest replay with the contact changes produced 7 physical foot hits versus 5 in the pre-change host replay, while retaining all 13 main punch hits and zero held-knee hits. This does not establish phone accuracy: host bag dynamics differ from the recorded phone, and even the baseline misses different individual attempts. Phone confirmation is required. The update does not reduce MediaPipe inference cost or establish a new motion-to-photon latency.

## Phone test

Install `Builds/FitFight-strike-contact.apk` as an update, fully close/reopen once, and check that the preview moves. Keep the intended floor position and use **GAME BODY TEST · 2:44**. The button selects the recommended configuration and includes calibration. Keep media audio in the screen recording. Comfortable low kicks and non-expert technique are acceptable; do not repeat a test solely for an extra or imperfect movement.

Send the newest matching JSONL, summary JSON/Markdown, and screen recording. Data stays in `PunchSessions`:

```sh
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/PunchSessions/ ./
```

See `Docs/Game-body-test-plan.md` for the spoken sequence and capture/fallback details. Cold Camera2 startup, perceived smoothness and contact behavior remain device checks.

## Scene validation

The final full-body scene suite passes. Eight synthetic front/side/knee/generic strikes contact once each; the older recorded knee drives and foot extensions all retain their required stationary-bag contacts, with zero held-lift hits. The full 1.16 moving-bag replay retains all 14 main punch hits and both non-scoring holds. Quiet world-head deviation is 2.58 degrees; maximum planted-foot frame displacement is 0.95 cm. The prior candidate run exceeded the 2.5 cm foot-step guard at 2.8 cm. This comparison is a host check rather than a phone smoothness measure.

The held-squat regression checks rendered pelvis height against the observed offset over repeated frames after settling: maximum error rounds to 0.00 cm, below the 1.5 cm limit. Torso/head movement, bilateral overhead arms, automatic spoken-test completion with three saved files, restoration of prior mode and home cleanup also pass. No recorded observations are replaced by slow host frames.

The final latest-session moving-bag replay passes with all 13 main punch hits, zero held-knee hits, 30 total physical contacts and 7 feet. Quiet rendered world-head deviation is 9.01 degrees (the saved target itself varies), and maximum planted-foot frame displacement rounds to 0.00 cm. The earlier contact-only candidate produced 8 feet; the final guard-height-stable version produced 7. One remaining reach/contact miss is retained for further phone validation rather than bypassing scoring gates.

## Build and regression verification

- The final Android build reruns all 10 new target/capture-request checks, 107 synthetic full-body checks, the 1.15 and 1.16 recorded detector replays, 56 smoothing/quality/head checks, earlier boxing-priority/October replays, MediaPipe boxing checks, real native model comparison and full-body diagnostics. All pass.
- The separate punch scene passes both legacy and fast boxing paths, both fists, tentative-left to committed-right handover, recording/mirroring mode transitions, summaries, home cleanup and reentry.
- Android API 36 compilation, 26 camera ownership/recovery checks and 52 YUV/rotation checks pass. These do not simulate the phone camera service. The new inactive-backend FPS tests verify combined request idempotence and original-preference restoration; Android native-mode behavior still requires the phone.
- APK v2 signature and package/version are verified. Full/Lite model assets, ARM64 inference libraries, defined camera Java classes and startup probe are present. Final IL2CPP metadata contains the new capture-configuration and leg-target methods. App 1.16 and 1.17 APK hashes remain unchanged.

Reports: `Logs/strike-contact-build.txt`, `Logs/strike-contact-apk-verification.txt`, `Logs/Practice/strike-contact-validation.txt`, `Logs/Practice/strike-contact-camera-validation.txt`, `Logs/Practice/strike-contact-body-scene-final.txt`, `Logs/Practice/strike-contact-latest-scene-final.txt`, and `Logs/Practice/punch-play-validation.txt`. The baseline/candidate scene diagnostics are preserved alongside them. Original source files are backed up in `Docs/ImplementationBackups/20261010-strike-contact/`; this is not a complete repository snapshot.

The final latest-session miss is the left front attack around REC 138.6: the foot reaches within about 9.6 cm of the moving surface, just outside the 9.5 cm contact radius. Its goal is within leg reach. This remains a contact/target-motion case; the detector already committed it. No radius or freshness gate was loosened to force a score.
