# Review fixes — app 1.20

APK: `Builds/FitFight-review-fixes.apk` — **1.20, version code 13**, package `com.DefaultCompany.Tracking4AllUnity`, 286,742,551 bytes.
SHA-256 `d9afb9ff82165df9f41913098f6272c72972b5e3badb8a869a660f6af09d6321`. Build: 0 errors, 752 warnings. Signature verified.
Builds on 1.19 (frame-driven pipeline). Source backups: `Docs/ImplementationBackups/20261010-review-fixes/`.

## What changed

**Foot strikes contact with the shoe, not just the ankle point.** Assisted foot strikes were scored from the ankle bone with a 9.5 cm sphere. The avatar's shoe extends 12–17 cm past that point, so reach-limited kicks that visibly struck the bag were scored as misses (the three 1.17 phone misses had 10.4 / 14.3 / 19.6 cm ankle gaps). Each foot strike now sweeps four probes — shoe tip, ball (toe bone), ankle and lower shin — each with its own sweep/rearm history (`PracticeHeavyBag.SampleAssistedSegment`, `FitFightPracticeMode.FootProbes`). Commit, contact window, approach direction and once-per-action rules are unchanged. Knee strikes are unchanged.

**A committed action starts armed unless the limb is already deep in the bag.** The swinging bag can come to rest a few cm from a guard fist; the old rule (start armed only with >12 cm gap) refused the punch thrown from there ("Limb not rearmed"). Committed assisted actions now seed armed when the gap exceeds half the contact radius. Idle/tracked sampling keeps the old 1.6× rule.

**Authored jumps restart the sweep instead of disarming it.** Every punch start was being flagged "Teleport or excessive speed": the controller seeds the sweep at the hand, then the scene samples the fist tip in the same frame and divides by a full frame. That disarmed punches nondeterministically. For assisted (authored) limbs a jump now reseeds without sweeping across it. Tracked (non-assisted) sampling still disarms on teleport.

**A planted support foot is never pushed by the bag.** The provisional-lift bag-clearance rule in `ComposeLeg` ran for planted feet too. It now applies only to released (lifted) feet.

**Camera downscale is anti-aliased.** `fitfight_camera.cpp` now box-filters in sensor orientation, then rotates the smaller image (previously full-resolution rotate, then nearest-neighbour scale). Also computes a sparse mean luma for the exposure option.

**Latency A/B options** (Diagnostics → Tracking):
- *Inference thread priority* (default **on**): the inference worker requests Android nice −4 and reads back the applied value.
- *Render 120 Hz* (default off).
- *Motion-blur exposure cap* Auto / 8 ms / 6 ms (default Auto). Requires MANUAL_SENSOR. Once AE converges, exposure is fixed at the cap and ISO is raised to keep AE's brightness; a slow luma loop corrects later lighting changes. If AE is already at or under the cap, it stays automatic. At max ISO the image gets darker; the status line says so.
- Every `reconstruction` record now logs `inferenceThreadPriority`, `targetFrameRate` and `exposureStatus` (per-frame exposure/ISO were already logged in 1.19).

## Investigated and not changed

**Overhead-gate block at REC 35.70 (my earlier recommendation).** Cause found: a right-punch return plus a 12 cm left-guard dip satisfied the bilateral "lowering" rule. A fix requiring both hands above the guard band removed the block, but replay through the real `BoxingPunchIntent` moved the next left commit from 35.886 to **35.937 — later**. The brief block re-seats the dipped guard origin, which helps the next stroke. Gate reverted; `GateLoweringValidation` now locks in the 35.886 timing.

**Punch height/hook variation from wrist direction.** Not implemented: from the floor camera, forward punches project upward in the image (documented in the 2 October review), so wrist height cannot distinguish head from body shots without labelled data. It would make every punch look wrong in a new way.

**Per-frame JNI allocations.** A few small Java/C# objects per inference (~20 Hz). Not worth the lifetime risk until Perfetto shows GC pauses.

**Optical-flow wrist tracking, image-apex knee/kick classification, temporal lifter.** Need labelled phone data; the A/B runs below supply timing evidence first.

## Verification (host)

| Check | Result |
|---|---|
| Latest-session full moving-bag replay (1.17 recording) | 13/13 main punches, both knee holds non-scoring, **8/8 committed foot attacks hit** (phone 1.17: 5; 1.19 host: 7–8), planted-foot step 0.00 cm — **4 consecutive clean runs** |
| 1.16 recording moving-bag replay | 14/14 main punches, holds non-scoring, **10 foot hits** (1.18: 7), quiet head 2.59°, foot step 0.00 cm |
| Synthetic rig strikes (front/side/knee/generic × both legs), recorded leg spans, squat/torso/overhead | all pass |
| Punch scene (legacy + fast boxing, handover, UI) | pass |
| Unit suites: strike-contact 20, game-body 109, smooth 57, boxing-priority 91, October replay 77, MediaPipe boxing 24, punch 167, full-body 70, gate 3, frame-driven pipeline 19, engine comparison | all pass |
| Native conversion (new scale→rotate path) | 432 checks pass |
| Java host: lease 8, recovery 26, pixels 52 | pass (`NativePose/tests/run_java_host_tests.sh`, host JNI shim added) |
| APK contents | new JNI symbols, Java exposure methods, IL2CPP contact/options code present |

Two caveats: earlier runs before the last two fixes showed intermittent 2.5–2.6 cm planted-foot steps in the quiet window; the per-frame `QUIET STEP` trace now in `GameBodyPlayValidation` will identify it if it recurs. Host physics/timing is not phone behaviour; foot-hit counts are not detection accuracy.

## Phone test — one change per run

Same floor position, distance, tilt and lighting as before. Fully close/reopen after installing. Diagnostics → Tracking: MediaPipe Full, native camera ON, GPU OFF, input 512.

1. **Run A — 1.20 defaults** (priority on, 120 Hz off, exposure Auto). Run **GAME BODY TEST · 2:44**. Check the diagnostics status line shows `worker priority -4`.
2. **Run B — exposure cap 8 ms.** Tap *8 ms*, wait 2 s, read the Exposure line. If it says `auto within cap`, your lighting already gives short exposure — skip this run. If it says `capped … (max, darker)`, add light or skip. Otherwise run the Game Body Test.
3. **Run C — 120 Hz.** Exposure back to Auto, toggle *Render 120 Hz*, run the Game Body Test.
4. Optional **Run D — input 384** (everything else as Run A).

Send each run's JSONL, summary JSON/MD and the screen recording:

```sh
adb install -r Builds/FitFight-review-fixes.apk
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/PunchSessions/ ./
```

What to judge by eye: kicks that visibly reach the bag should now score; with the exposure cap, fast fists should look less smeared in the preview but the image may be noisier; at 120 Hz the avatar should look smoother. Optional: film yourself and the screen with a second phone at 240 fps to measure punch-to-avatar latency directly.
