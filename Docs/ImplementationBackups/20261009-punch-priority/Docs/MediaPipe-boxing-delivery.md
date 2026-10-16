# MediaPipe boxing delivery — app 1.12

APK: `Builds/FitFight-preview-boxing-fix.apk`, package `com.DefaultCompany.Tracking4AllUnity`, Android version code **5**, minimum API **26**.

SHA-256: `9bb5a076f3c70733f894c877911748413df028c3fc3c196cea8093e1741faba4`

Build succeeded with **0 errors**, **748 warnings**, including existing Unity/Sentis shader variant warnings. APK v2 signature verification passed. Manifest package/version and bundled native MediaPipe library/model assets were checked. No new physical-phone result is claimed.

## Delivered changes

- Independently updating camera preview and REC timer, separated from contact sampling/final-pose recording.
- Assisted boxing avoids inactive mirroring leg-confidence reads. Practice contact, pose-recording and preview exceptions are captured separately; console logging is throttled.
- Completed native inference diagnostics are saved independently of the avatar-render path. Session summary includes final-pose/preview counts and runtime-error details.
- Early coordinated two-hand raise/lower detection retains the single-hand boxing prior and stable assisted stance.

## Verification

- October 6 replays reproduce saved app 1.11 decisions exactly. Revised gesture gating preserves every punch-section decision, while raise/lower-section decisions fall from five to one across both recordings. The remaining decision lies at a cue transition. These are replay decisions, not labelled accuracy scores.
- Native MediaPipe Full, actual scene, direct avatar OFF: 40 arm observations, 602 final poses and 50 model diagnostics. During an injected contact-collider failure, the REC timer continued, 121 poses and ten inference diagnostics were saved in the last second, and the summary captured the error. The Android root exception was not reproduced.
- Actual scene: both legacy arms and both boxing arms each produce one physical contact per stroke. Gameplay recording persists summaries; mirroring ownership, pause/stop and home/reentry checks pass.
- Existing 166 punch assertions, 70 full-body checks, 170 RTMPose checks, 24 MediaPipe boxing checks and 54 engine checks pass. `Logs/Practice/` and `Logs/engine-comparison-validation.txt` hold the validation reports.

## Phone next step

Follow `Docs/MediaPipe-boxing-test-plan.md`. Install with `adb install -r Builds/FitFight-preview-boxing-fix.apk`, or use your normal APK installation method.

First check the live preview for 15–20 seconds at torso/waist height. Then perform one **RECORD TEST** with MediaPipe Full, per-region filtering ON, stable body motion ON, direct avatar ON and 60 FPS. Calibrate standing; keep fighting guard through the spoken countdown. Check continuous REC time/frame updates through punches and paired raises.

The same pull command retrieves matching `.jsonl`, `.summary.json` and `.summary.md` files:

```sh
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/PunchSessions/ ./
```

Send the matched video and three files. If the preview freezes again, promptly save `adb logcat -d -v threadtime > fitfight-logcat.txt` too. Compare floor placement after the preview works. The original paired recordings and detailed findings are in `Docs/MediaPipeSessions/20261006-floor-torso-preview-analysis.md`.

Native model/runtime and approximately 100 ms capture-to-decision processing delay are unchanged. Capture/readback/inference cadence remains the next latency work item after phone verification.
