# MediaPipe boxing delivery — app 1.13

APK: `Builds/FitFight-punch-priority.apk`; package `com.DefaultCompany.Tracking4AllUnity`; Android version code **6**, minimum API **26**.

SHA-256: `a61cb00b74f1c28bbe2aa6d1764aba1cfe18abc45c1c46ee5db96029c6726b28`

Build succeeded with **0 errors and 748 warnings**. APK v2 signature verification passed. Manifest version/package and bundled MediaPipe native library and Full/Lite model assets were checked. This is ready for a phone test; a new physical-phone result is not claimed.

## Delivered behavior

- After an observed fighting guard, an ambiguous single upward arm stroke favors a punch. Clearly paired overhead raises/lowers remain excluded.
- An isolated landmark jump can recover through two fresh, folded guard observations. The bad frame and recovered guard frame cannot commit. Genuine wrist loss, stale gaps or unrecovered jumps still require guard reacquisition.
- Modest depth/support-hand motions require stronger corroboration. Stationary preparations cancel; cancellation retains the trusted guard and waits for a visible return rather than using an extended endpoint as a new guard.
- Clear image/depth endpoints and unfolding survive misleading initial image direction from a floor view. This preserves slow foreshortened strokes and fast punches without waiting for another rendered frame as evidence.
- **BOXING TEST** replaces the ambiguous RECORD TEST label, forces assistance on, and saves three matching files to PunchSessions. **BODY TRACKING TEST · SAVE RESULTS** explicitly shows assistance OFF and continues saving pose/mirroring diagnostics to MediaPipeSessions.
- The stable authored boxing stance, independently updating preview/REC timer and physical swept fist-to-bag scoring remain in place.

## Verification

`BoxingPriorityValidation`, October 6 replay checks, MediaPipe boxing checks, existing punch/body/tracking checks, engine comparison checks and actual-scene `PunchPlayValidation` pass. Reports are in `Logs/Practice/` and `Logs/engine-comparison-validation.txt`.

The October 7 replays reproduce the old v1.12 totals before comparison. Revised totals are **25 vs 23**, **23 vs 26** and **22 vs 22**, respectively. The first recovers the known tracking-jump miss and accepts a single upward stroke. The second removes the support-hand and settled-hand decisions near 47 seconds while retaining reviewed outgoing strokes. The third retains all 12 requested single-arm and eight observed alternating strokes. These are replay decisions, not labelled accuracy percentages. Partial strokes can still score under the accepted game prior.

The October 6 floor replay preserves outgoing punches. The torso replay preserves outgoing strokes and excludes the repeated first slow-left return. Tests compare stroke timing rather than insisting every old false decision remain.

Actual-scene tests verify one physical contact per test stroke for both legacy arms and both boxing arms, mode ownership/restore, summaries, pause cleanup and home/re-entry. The renamed BOXING TEST button is exercised from mirroring mode and enables assisted recording. The rendered test UI was inspected.

## Next phone test

Use the existing floor setup, MediaPipe Full, per-region filtering ON, stable body correction ON, direct avatar OFF and 60 FPS. Calibrate standing upright, then take a fighting guard. Tap **BOXING TEST** and follow the 75-second spoken sequence. Send the matching JSONL, summary JSON, summary Markdown and screen video.

```sh
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/PunchSessions/ ./
```

See `Docs/MediaPipe-boxing-test-plan.md` for the short procedure and `Docs/MediaPipeSessions/20261009-body-test-and-punch-priority.md` for analysis. The October 9 input was a body-mirroring test with assistance off, so it does not measure hybrid boxing decisions.

Native MediaPipe computation/capture cadence and roughly 100 ms frame-availability-to-pose processing delay are unchanged. This update improves the decision policy and recovery; it does not retrain MediaPipe or establish perfect movement classification.
