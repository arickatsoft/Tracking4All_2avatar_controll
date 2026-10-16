# MediaPipe idle fix delivery — 2026-10-02

APK: `Builds/FitFight-mediapipe-idle-fix.apk`

App version **1.10**, Android version code **3**, package `com.DefaultCompany.Tracking4AllUnity`, minimum Android API **26**.

SHA-256: `6f03879a6f99bc5ce8a4e6698091eef8d246e6f1a7208645cfb47479f6576f32`

Android build succeeded with **0 errors** and **748 warnings**, including Unity/Sentis shader variant warnings. APK signature verification passed (v2). Manifest version and package were checked. Physical-device verification of this new APK remains the next step.

## What changed

Per-region filtering now retains shared smoothing at rest and increases responsiveness when confident image movement is visible. Inferred depth noise alone cannot trigger the faster wrist/elbow/leg presets. New records identify this as `region-motion-aware`. Model weights and upstream capture/inference latency are unchanged.

Replaying the supplied recording through the actual filter reduced initial idle RMS from **2.319 to 2.040 cm**, and final idle RMS from **1.392 to 1.037 cm**. Recorded fast-punch deviation from the raw model trajectory was lower than legacy filtering. These are replay measurements, not new phone results or human ground-truth accuracy.

## Validation

- 11 focused idle/motion/loss/reset checks passed.
- Same-input replay reproduced the old phone-filter output to within 0.0000301 cm and passed revised idle/fast-punch checks.
- MediaPipe improvement, native engine comparison, pose, punch, full-body guide and responsive movement checks passed.
- Actual Unity scene checks passed for native MediaPipe calibration/avatar movement, engine switching, spoken test guide, recording/summary persistence and cleanup.

## Phone test

Install the APK, confirm app **1.10**, and select **MediaPipe Full**. Set:

- MediaPipe per-region filtering: **ON**.
- Stable body motion / floor camera correction: **ON**.
- Direct avatar: **ON**.
- Camera FPS: **60**.
- Adaptive filtering: **ON**; direct bone smoothing and speed limit: **OFF**.

Keep the previous camera position and tuning values. Calibrate, then tap **MEDIAPIPE TEST · SAVE RESULTS**. Follow the spoken instructions, including both stillness intervals. Pull the same `PunchSessions` directory and send the matching JSONL, summary JSON and screen recording. The next run should identify its filter profile as `region-motion-aware`.

Detailed evidence: `Docs/MediaPipeSessions/20261002-110117-region-and-idle-fix.md`.

MediaPipe native binaries still target ARM64; the APK also includes ARMv7 libraries for the other engines. The user's SM-S928B is ARM64. Some raw-model drift may remain; the approximately 100 ms processing pipeline was not changed by this correction.
