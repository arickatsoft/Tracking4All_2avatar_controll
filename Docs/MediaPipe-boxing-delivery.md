# MediaPipe boxing delivery — app 1.14

APK: `Builds/FitFight-punch-handoff.apk`; package `com.DefaultCompany.Tracking4AllUnity`; version code **7**; minimum API **26**.

SHA-256: `0f7da0b24d8b63d7f8aa927f750d33d6ff97bcc612b40567fc5e96eb3b05fb8f`

Build succeeded with **0 errors and 748 warnings**. APK v2 signature verification passed. Manifest version/package and bundled native MediaPipe library/Full/Lite models were checked. Physical-phone behavior of 1.14 still needs validation.

## Change

A committed opposite-hand punch now immediately takes ownership from an uncommitted preparation. Previously it could wait through preparation and recovery until its 180 ms queue window expired, causing a detected punch to disappear without an avatar strike or bag hit. Queueing behind an already active strike remains intact. This fixes the mechanism found in the October 9 13:01 session's third fast-right decision at REC 48.119 seconds.

MediaPipe, detector thresholds, floor-camera calibration, boxing stance and single-arm upward-stroke prior are unchanged from 1.13. The remaining support-hand false decision around REC 48.618 seconds is not claimed fixed. Partial outward strokes may still score under the accepted early boxing policy. The roughly 100 ms pose-processing pipeline is unchanged.

## Evidence and checks

The latest recording has 24 detector decisions and 23 physical hits. Manual preview review identifies 19 main strokes with matching decisions, one lost avatar strike, one extra support-hand hit and four partial-motion hits. No idle/overhead/step-out hits occur. The preview remains live, with zero recorded runtime errors and dropped records.

An actual-scene regression starts tentative left preparation, then confirms a right punch while that left candidate is pending. It fails on 1.13 with `started=False, hits=0`; with this fix it starts the right strike within the 80 ms test bound and produces exactly one physical contact. Existing legacy/boxing contacts, mode restore, recorder summary, pause and home/re-entry checks pass. Saved-session boxing replays and engine checks also pass.

Reports: `Logs/Practice/punch-handoff-before-validation.txt`, `punch-play-validation.txt`, `boxing-priority-validation.txt`, `october-replay-validation.txt`, `mediapipe-boxing-validation.txt`; build report `Logs/punch-handoff-build.txt`.

See `Docs/MediaPipeSessions/20261009-130157-assisted-boxing-analysis.md` for the timing/decision analysis and `Docs/MediaPipe-boxing-test-plan.md` for the next phone procedure.

## Phone test

Keep the same floor placement, MediaPipe Full, region filtering ON, stable correction ON, direct avatar OFF, 60 FPS and **punch assistance ON**. Calibrate upright, take fighting guard, then use **BOXING TEST** for one spoken sequence. Focus on fast-right/alternating punches and wrong-hand extra hits.

Pull the three matching files from PunchSessions and send them with the screen video:

```sh
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/PunchSessions/ ./
```
