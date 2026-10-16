# Full-body game delivery — app 1.15

APK: `Builds/FitFight-game-body.apk`; package `com.DefaultCompany.Tracking4AllUnity`; version code **8**; minimum API **26**. Build succeeded with **0 errors and 750 warnings**. APK v2 signature verification passed; manifest version/package, defined native camera classes, ARM64 MediaPipe library and Full/Lite models were verified.

SHA-256: `18f4b30138bea866be2314275ecc18f69732c2fba1b39d913b62f86a819a94f0`. Package verification: `Logs/game-body-apk-verification.txt`.

## Implementation

- One final avatar owner composes authored fighting guard/punches, continuous calibrated torso/head motion, tracked arm gestures, planted support feet, lifted legs and procedural leg strikes. Direct retargeting does not compete with assistance.
- Full-body posture distributes up to 60° tracked torso rotation through pelvis/spine/chest, and up to 45° head rotation through neck/head. Measured squat depth lowers the pelvis by at most 48 cm, with both feet solved to their support positions. These larger ranges apply only with fresh body observations; the existing boxing fallback remains bounded.
- Leg actions use observed preparations and distinguish front extension, lateral extension and forward bent-knee travel. A held knee lift remains a tracked preparation. A committed strike follows a bounded 460 ms animation envelope. Supporting-leg confidence, frame freshness and physical swept contact gate scoring. Foot release uses a 7.5 cm onset / 3.5 cm return hysteresis so minor idle ankle noise does not unlock the stance. Knee contacts have their own volumes and labels. Each action can score only once.
- Clear bilateral overhead, side and forward arm gestures remain tracked and do not become punches. The accepted single-arm upward-stroke boxing prior and opposite-hand punch handoff are preserved.
- Android Camera2 acquires latest YUV frames on a dedicated handler, converts/resizes/rotates them to upright CPU RGBA, shares those frames with the preview and feeds MediaPipe directly. This bypasses Unity's camera GPU-to-CPU readback. Existing single-camera ownership, permission handling, pause/resume and stalled-camera recovery remain in charge. Native startup/error fallback uses the existing Unity camera path. Sensor rotation is refreshed during capture; changing upright dimensions rehooks the shared preview texture.
- Native inference initialization, processing and teardown now share a dedicated thread. Optional GPU inference uses that thread and falls back to CPU if GPU initialization fails. CPU remains the test default; no phone speedup is assumed.
- **GAME BODY TEST · 2:44** selects the recommended full-body test profile, speaks all instructions, records one run, waits a bounded interval for calibration, auto-stops and restores prior assistance/direct selection. Raw records and both summaries use **PunchSessions**. The tracking panel can display the last game summary.

The work changes runtime control and capture architecture; it does not retrain MediaPipe weights. A separate fast optical-flow intent branch or additional pose model is not introduced: the first phone run should establish whether bypassing readback and using existing early preparation is sufficient.

## Validation

- `Logs/Practice/game-body-validation.txt`: 92 synthetic checks cover both sides of front/side kicks and knee strikes, held knees, squats, stale/missing support joints, duplicate observations, bilateral arm gestures and one physical hit per action. Guide ordering, duration and summary semantics are also checked.
- `Logs/Practice/game-body-play-validation.txt`: actual-scene observations drive the real rig and bag; each of six leg strikes produces one physical hit. Held knees, squats, torso/head and overhead arm gestures remain non-scoring. Test-button activation, automatic completion, summary output, prior mode restoration and home cleanup are checked. Rendered poses are saved alongside the report and visually reviewed.
- `Logs/Practice/punch-play-validation.txt`: existing legacy/boxing contacts, opposite-hand handoff, direct/assistance restoration, pause and home/reentry pass with the new controller.
- Existing recorded boxing-session replays, detector checks and real host MediaPipe/ncnn inference/disposal checks run before the build. These do not establish Android camera throughput or real-user kick accuracy. In the native path, frame availability begins at the Android image callback before conversion; the legacy path measures Unity texture availability, so the stage breakdown should be considered alongside total age when comparing runs.
- `NativePose/tests/CameraPixelsTest.java`: 52 checks cover all sensor rotations, pixel bounds, limited-range YUV conversion and packed RGBA channel order. Java/Android lifecycle integration is also compiled in the APK, but physical-camera behavior needs phone validation.

See `Docs/Game-body-test-plan.md` for the single phone run and data pull command. Source backups are in `Docs/ImplementationBackups/20261009-game-body/`.
