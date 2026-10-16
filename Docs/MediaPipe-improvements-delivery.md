# MediaPipe improvements delivery — 2026-10-01

APK: `Builds/FitFight-mediapipe-improvements.apk`, version **1.09**, Android version code **2**, package `com.DefaultCompany.Tracking4AllUnity`. Size 286,524,132 bytes (273.25 MiB). Android minimum API 26.

SHA-256: `1c29e2afeac91f295b364b7510f790ed3d5c337997a0400033b36f81efe8ebea`.

Final build succeeded with **zero errors / 748 warnings**. APK v2 signature verified. Bundled Full/Lite models, ARM64 MediaPipe native runtime and both ncnn ABI libraries verified. The APK also contains ARMv7 paths for existing engines; this bundled MediaPipe native runtime is ARM64, so MediaPipe device acceptance is for ARM64 phones. A failed engine retains the existing fallback behavior.

MediaPipe integration/model weights are unchanged in this iteration. This intentionally isolates calibration, acceptance, filtering and measurement changes before the runtime upgrade. No detector fusion or automatic model training was added.

## Changes

- MediaPipe-specific standing 3D calibration, fixed neutral orientation/scale reference and independent joint evaluation. No RTMPose 2D reconstruction/torso projection gate on calibrated MediaPipe observations.
- Existing confidence, finite-coordinate, freshness and bounded-prediction safeguards retained. No-person results immediately clear observed joints. Missing optional world visibility no longer counts as zero when image visibility/presence are available.
- Selectable MediaPipe region filter preset: smaller wrist/elbow dead zones and stronger motion response; moderate leg smoothing. Legacy mode retains the former joint filter settings, with the new calibration/acceptance fixes in both modes.
- Practice / Tracking: **MediaPipe test · save results**. Selects Full, disables punch assistance, waits for calibration and provides a 14-movement, approximately 112-second spoken protocol.
- Background JSONL logging of raw MediaPipe33, calibrated/filtered joints, rejected detections, cue timestamps and synchronized avatar rotations/positions. Summary JSON and readable Markdown, plus an on-phone results view.
- Calibration excluded from movement stats; median/p95 accepted pose and avatar timing, compute timing, rejected outcomes, actual accepted FPS, measured camera FPS, wrist/knee availability and stillness RMS. Availability is not accuracy; timestamps exclude camera/display delays outside Unity.
- Tests finalize on settings/height changes, calibration reset, engine switch, pause, other recording or exit. Partial/failed sessions are marked; files and dropped-record counts are reported.
- Standard-library label exporter creates unreviewed movement intervals for manual video review; never promotes a requested cue into correctness ground truth.

## Verification

- 18 new calibration, partial-visibility, invalid-joint, loss/recovery, percentile, availability, stillness-units and protocol checks passed.
- Real native Lite/Full inference checks and existing RTMPose, punch, full-body guide and recorded-motion regressions passed. Optional older reference-person fixture remains unavailable; real native recorded-person fixture ran.
- Actual scene: repeated switching across all five engines, native inference → calibration → direct avatar, sanity guide, saved raw/filtered/avatar JSONL, partial summary and shutdown marker verified. Height changes and engine switches finalize sessions; home exit cleans up.
- Label exporter: 2 tests passed (clock alignment/unreviewed labels and malformed input), plus a successful export from the real Editor diagnostic stream.
- Final APK build and signature/package/model checks passed. Logs: `Logs/mediapipe-improvement-validation.log`, `Logs/mediapipe-improvement-validation.txt`, `Logs/mediapipe-improvement-play.log`, `Logs/engine-comparison-play.txt`, `Logs/mediapipe-improvements-android-build.log`.

These establish implementation correctness in the Editor, not improved phone accuracy or perceived latency. The user's legacy/region pair is the next acceptance step; neither recorded phone comparison nor model training is claimed.

Test/label plan: `Docs/MediaPipe-improvement-test-plan.md`. Source backups: `Docs/ImplementationBackups/20261001-mediapipe/`.
