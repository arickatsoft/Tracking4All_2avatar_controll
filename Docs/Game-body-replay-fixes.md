# Recorded full-body fixes — app 1.16

APK: `Builds/FitFight-game-body-replay-fix.apk` (273 MiB). Package `com.DefaultCompany.Tracking4AllUnity`, version code **9**, minimum API **26**, target API **36**. Android build succeeded with **0 errors and 752 warnings**; APK v2 signature, defined camera classes, ARM64 native libraries and Full/Lite models were verified. Build report: `Logs/game-body-replay-build.txt`; package verification: `Logs/game-body-replay-apk-verification.txt`.

SHA-256: `1ed93cc40be2acf75f55153ede6163a1297002c6fceade972938b447d19174f7`. The earlier 1.15 APK remains at its original path.

This update addresses the first app 1.15 full-body recording, `20261009-142232-221`. The original inputs and review remain under `Logs/MediaPipe/20261009-142232-221/`.

## Resulting behavior

- High kicks are bounded by calibrated thigh/shin lengths, plausible geometry and speed rather than a fixed 95 cm inter-ankle height ceiling. Tracking loss, stale frames, missing support joints, impossible geometry and large jumps still block scoring.
- A leg lift remains visible and provisional. Foot extension resolves to a front kick, a body-relative side kick, or a generic foot strike when direction is unclear. A bent-knee drive commits on a short observed retraction, using accumulated peak motion evidence. This avoids scoring the two held lifts and preserves all four recorded knee drives. The visual lift begins immediately; confirmed knee contact is later than its initial rise. Rapid intentional lift-and-return movements remain inherently ambiguous.
- Each resolved strike retains one action ID and the existing physical contact gate. Kicks are not selected from the spoken cue; no cue labels enter the runtime classifier.
- Native Camera2 startup waits for Android camera availability after Unity releases the old source. Recovery waits for `onClosed`, ignores stale callback generations, limits opening to three attempts, and retries at 30 FPS when necessary. Closing during an outstanding open keeps the callback handler alive so the late device can be closed.
- Native preferences are retained after fallback; beginning a new game test explicitly retries native capture. Startup errors are handled by the startup coroutine, preventing competing runtime teardown during initialization. Runtime errors or stalls switch to working legacy capture.
- The HUD reports active fallback. Logs retain camera lifecycle/status, requested capture mode, and the path attached to the scheduled inference. Summaries count evaluated native/legacy frames separately. Exact accepted body snapshots and action reasons support future replay without reconstructing mirror/calibration settings.

The previous recording proves that Camera2 failed before recording, but does not reveal its root cause. The lifecycle/recovery work addresses plausible release races and adds evidence for remaining failures. It does **not** establish that the phone disconnection is eliminated or that latency is reduced.

## Validation

- `Logs/Practice/game-body-validation.txt`: 107 synthetic checks, including both sides of generic/high foot strikes, body-relative side kicks, non-scoring holds, impossible geometry, stale/missing joints, guide order and physical contact gates.
- `Logs/Practice/game-body-recorded-replay.txt`: editor-only 1.15 baseline reproduces all 25 recorded body events; revised detector recognizes four front extensions, four knee drives and two lateral attempts, with no leg commits in the held-knee, idle, torso, squat, bilateral-arm or final recovery windows. Lateral direction is intentionally not asserted for unfamiliar technique.
- `Logs/Practice/game-body-play-validation.txt`: actual scene validates all eight synthetic foot/knee variants. Recorded leg joints then drive the real rig and bag: held lifts score zero; knee drives score four; four front extensions and two lateral attempts score one hit each. These leg/contact checks use guard arms and neutral torso/head; they are not full phone-video replay or combo accuracy measurements. Posture/overhead, automatic recording/summary completion and home cleanup also pass. Rendered generic-kick and knee poses were visually reviewed.
- `Logs/Practice/camera-recovery-validation.txt`: production Java compiles against Android API 36; 19 pure Java ownership/recovery checks cover callbacks after disposal, stale generations, retry bounds, duplicate errors and late session configuration. Existing 52 YUV/rotation checks pass. Physical Android callbacks, camera throughput and orientation still require the phone.
- Existing boxing and native model regression reports are retained. Android capture is not exercised by host tests.
- `Logs/Practice/punch-play-validation.txt`: the actual-scene punch regression passes, including both fists, early opposite-hand handoff, assistance/direct ownership, recording completion, pause, home cleanup and reentry.

The 1.15 baseline detector is retained only in `Assets/Practice/Editor/GameBodyMotion115.cs` for recorded regression comparison and is excluded from the player. Fourteen original source/settings files were saved under `Docs/ImplementationBackups/20261009-game-body-replay/` before editing; this is not a complete repository snapshot.

## One phone run

Keep the floor camera, install the 1.16 APK, record the screen with media audio, and tap **GAME BODY TEST · 2:44**. The button sets the recommended options. Finish even if the fallback message appears. Send the newest matching JSONL, summary JSON/Markdown and video. Exact punch counts and expert side-kick form are unnecessary.

Data location remains `PunchSessions`; see `Docs/Game-body-test-plan.md` for the pull command and full sequence.
