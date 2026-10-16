# Smooth full-body game update — app 1.17

APK: `Builds/FitFight-smooth-body.apk` (**273 MiB**), package `com.DefaultCompany.Tracking4AllUnity`, version **1.17**, version code **10**. Build succeeded with **0 errors and 750 warnings**. APK v2 signature, Android API 26 minimum / 36 target, Full/Lite models, ARM64 native libraries, defined camera classes and the new camera probe were verified. The prior 1.16 APK's hash is unchanged.

SHA-256: `5c58ae41b4ff45909df0f4982308ad0f0c109d3005dd27c2255d31d771a72e2f`.

Install as an update:

```sh
adb install -r Builds/FitFight-smooth-body.apk
```

Then use the same floor placement and tap **GAME BODY TEST · 2:44**. The button selects the recommended settings. Send the newest matching JSONL, summary JSON/Markdown and screen video. See `Docs/Game-body-test-plan.md` for the full protocol and unchanged `PunchSessions` pull command.

This update follows the app 1.16 recording `20261009-151451-407`. Its review and original inputs are preserved under `Logs/MediaPipe/20261009-151451-407/`.

## Resulting behavior

- The captured fighting guard is a stable reference pose. The imported idle clip's independent head turns and body fidgets are removed; observed head/torso motion supplies the offsets.

- Torso, head and squat offsets follow continuous damped motion at rendering cadence. Larger deliberate rotations use a faster response than small idle changes. The target follows observed poses; it does not buffer future frames.
- Tracked arms blend into and out of fighting guard. Elbow/knee pole targets are smoothed so changes in the model's bend plane do not instantly flip the joint. An active punch, including the outgoing arm during opposite-hand handover, retains animation ownership and bypasses tracked-arm damping. The authored fist target is bounded to 12 m/s, below the physical sweep's 18 m/s rejection threshold, and the outgoing fist has a smooth 100 ms handover.
- Held lifts and foot returns use damped targets. A confirmed kick uses a faster response. Strike handovers start from the last rendered foot/knee positions rather than the guard clip's freshly evaluated joints. Provisional lifts keep clearance from the bag so a later confirmed strike can approach and rearm its physical contact sweep.
- Invalid leg geometry is rejected before changing displayed feet, squat height or release state. One brief invalid interval can retain the last reliable leg target and action identity for up to 120 ms. **Impact permission is disabled immediately**; it resumes only with a fresh, verified observation inside the original contact window. Sustained loss cancels and requires a new grounded stance. Prediction or a held target cannot score.
- Foot attacks require a substantially extended knee, reducing premature foot labels during partial knee unfolding. Knee scoring still requires short bent-leg return evidence, preserving non-scoring holds. The prior recording's knee drives remain regression cases. This latest recording includes substantial leg extensions during the knee cue; its cue is not used to force Knee labels.
- MediaPipe head angles use observed image ear/eye geometry with image-space torso-roll compensation. This avoids using unstable learned ear depth to infer head roll. World landmarks still drive torso and limb motion. The non-MediaPipe solver keeps its existing head path.
- Camera startup makes one delayed, bounded open probe when availability notifications have not allowed any attempt. Retries still wait for the previous device to close and respect the three-attempt limit; they can recover from stale availability state. Runtime fallback and explicit test retry remain available. Phone cold startup still needs validation.

The existing 2:44 spoken full-body test remains the next phone protocol. It selects the recommended MediaPipe Full/native CPU camera/CPU inference/adaptive filtering/body assistance settings and includes standing calibration. Data stays in `PunchSessions`.

## Validation and limits

The actual-scene suite passes (`Logs/Practice/game-body-play-validation.txt`):

- Eight synthetic front/side/knee/generic strikes each reach the physical bag once.
- The previous 1.15 recording's held lifts score zero; four knee drives and four front/two lateral extensions retain their expected contacts with guard arms and neutral head/torso.
- The complete 1.16 accepted body and arm sequence drives the actual avatar and moving bag together. All fourteen main punching-section decisions score; both held-knee windows score zero. Total scene replay is **31 physical hits, seven feet**. The phone's original total was 29, six feet; different rig composition and physics mean this comparison is not a detection accuracy score.
- Final quiet rendered head deviation from the first sample is **2.59°**, and maximum planted-foot frame displacement rounds to **0.00 cm**. The first complete replay exposed roughly 20° of head movement from the authored guard clip even after fixing the head estimate; the stable reference resolves that source of idle motion.
- Posture, bilateral overhead arms, automatic 2:44 completion/summaries, prior-mode restoration and home cleanup pass. All 3,085 exact paired observations are delivered; no host-frame replacement occurs.

The build also runs exact 1.16 detector replay at original and half sampling cadence, the previous 1.15 detector cases, synthetic stale/missing/jump cases, head-geometry replay and smoothing response checks. Both leg-replay cadences retain ten foot-extension decisions and zero attacks in idle, holds, torso/squat/bilateral-arm and final recovery windows. Directional front/side labels can differ at the reduced cadence; they are not forced from the cue. Exact 1.16 baseline reproduction covers all 33 original body events. The older baseline is kept only in editor code.

`Logs/Practice/smooth-camera-validation.txt` records Android API 36 compilation, 26 ownership/recovery checks and 52 YUV/rotation checks. These do not emulate the phone camera service. The final punch scene regression also passes (`Logs/Practice/punch-play-validation.txt`), including both fists, fast boxing paths, tentative-left to confirmed-right handover, mirroring ownership, recording summaries and home/reentry. Final build checks pass: 107 synthetic full-body checks, 56 smoothing/quality/exact-recording checks, both recorded detector replays, existing boxing-priority/October punch replays, native model comparison and full-body diagnostic regressions. The image-head quiet roll estimate spans **1.93°** with the recording's saved tuning, retaining the head-turn/nod segment. A controlled 20 Hz noisy target rendered at 60 Hz reduces angular-step variation from **455.97 to 51.27** (about 89%); this is an algorithm response test, not a measured phone smoothness score. Deliberate rotation step tests settle within approximately 100 ms at 30/60/120 rendering FPS.

Reports: `Logs/smooth-body-build.txt`, `Logs/smooth-body-apk-verification.txt`, `Logs/Practice/smooth-game-body-validation.txt`, `Logs/Practice/game-body-play-validation.txt`, `Logs/Practice/punch-play-validation.txt`, and `Logs/Practice/smooth-camera-validation.txt`.

The APK is ready for phone testing. Perceived smoothness, cold Camera2 startup and final phone latency remain to be checked on the device.

The previous phone recording already established native capture throughout that run, 72.6 ms median pose age and approximately 19.5 inference updates per second. This update does not claim a new measured phone latency. Rendering smoothing adds some following time to tracked posture/gestures; confirmed punches retain their existing fast path. The video preview is small, so detection precision/recall and exact physical head motion cannot be derived from cue totals.

Seventeen pre-change files are preserved in `Docs/ImplementationBackups/20261009-smooth-game-body/`; this is not a full repository snapshot. The app 1.16 APK remains at `Builds/FitFight-game-body-replay-fix.apk`.
