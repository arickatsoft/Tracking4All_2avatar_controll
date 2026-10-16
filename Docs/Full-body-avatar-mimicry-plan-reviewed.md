# Full-body avatar mimicry: reviewed execution plan

> October 9 update: app 1.15 implements the next full-body game milestone: native Android CPU camera capture, one assisted posture/arm/leg compositor, front/side kicks, knee contacts and a single 164-second spoken test. See [delivery](Game-body-delivery.md) and [phone test](Game-body-test-plan.md). Physical-phone accuracy, camera lifecycle and latency gains remain to be established by that run. Earlier measurements and gates below are historical.


Reviewed 30 September 2026 against the supplied “Full-Body Avatar Mimicry Plan”, this working folder and the primary references below. This is the current execution plan; older FitFight motion plans remain useful historical context. There is no `.git` directory in this checkout, so changes cannot be attributed to commits. Copies of the files edited for this milestone are under `ImplementationBackups/20260930-rotation-foundation/`.

## Decision

Keep the continuous tracking → intent → animation-completion architecture, with **one final pose owner**. Start with measurable stability and full-body capture, then one new action at a time. Treat a 3D lifter as an optional measured experiment, not a prerequisite for kicks or a selected production dependency. Remove the platform scores, star ratings, mobile FPS claims and calendar promises until comparable device measurements exist.

## Corrections to the supplied plan

| Claim or assumption | Verified state / implication |
| --- | --- |
| Current RTMPose has 17 joints and no feet | `Body26.onnx` and `RTMPoseMath` use Halpe26 with heel/toe mapping. No second engine is needed merely to obtain feet. The [upstream joint definition](https://github.com/open-mmlab/mmpose/blob/main/configs/_base_/datasets/halpe26.py) includes these landmarks. |
| RTMPose plus simultaneous MediaPipe torso/head | `BodyTrackingController` switches engines; the README explicitly documents stopping the baseline while RTMPose runs. The RTMPose path already has a calibrated body/head estimator. Simultaneous fusion would need synchronized observations, matching coordinates and a new resource budget. |
| Add Sentis and One Euro from scratch | Unity AI Inference 2.6.1 is installed. `PoseJointFilter` already smooths vectors/joints, and `BodyMotionEstimator` filters torso/head. A new `OneEuroFilter.cs` existed but had no callers. Another filter is an A/B experiment, not a guaranteed fix. |
| Direct retargeter controls the game avatar at all times | It exits when `avatar.HasPoseOverride`. `AssistedPunchController` evaluates its graph, composes torso/head offsets and solves planted legs. Its behavior needs a separate composition milestone; direct-mode smoothing alone cannot improve assisted-mode output. |
| Grounding is absent | There is existing fighter grounding and assisted two-leg planting. Kicks/jumps require releasing a foot and selecting stance/swing/airborne states. Adding another unconditional pinning layer would conflict with those behaviors. |
| MotionBERT-Lite is a proven 10-frame mobile lifter | The [official repository](https://github.com/Walter0807/MotionBERT) lists a 61 MB Lite pretrained encoder, separate pose fine-tuning, H36M 17-joint input and variable lengths up to 243 frames. It does not establish this plan's phone FPS, 10-frame accuracy or 70° camera performance. Pin a pose checkpoint/head and Halpe26→H36M conversion before export. Test past-only windows with the newest output; do not use future frames in latency claims. |
| Lifting resolves monocular depth | It supplies learned depth estimates under model/data assumptions. Unseen camera angles, occlusion and depth-sign errors remain possible. Validate the actual pitch convention and footage; the current manual tilt UI is limited to ±45°, and current floor fixtures cover 0/20/40°. A claimed 70° setup is outside that tested range. |
| Every vertical move gives a strong vertical image signal at 70° | That does not follow from perspective projection: visibility, foreshortening, distance, intrinsics and orientation matter. Knee above hip in image space is not a reliable general front-kick rule. |
| One Euro guarantees non-shaky poses, and 720°/s is a universal human limit | The [filter authors](https://gery.casiez.net/1euro/) describe a jitter/lag tradeoff. Tune on both stillness and fast motion. Angular caps are configurable visual controls and can attenuate punches; they are not anatomical constraints or proof of stability. |
| Existing JSONL can validate all new moves | `PunchCameraInput.Frame` stores torso and arms; the checked-in 756-observation fixture stores arm features. It cannot replay legs, model conversion or raw full-body retargeting. Add a versioned full-body stream before training/replaying kicks. |
| Voice labels can be assigned to the next two seconds as truth | Spoken cues are weak labels; speech recognition completion is later than speech onset, and actual movement may precede/follow the cue. Store cue and reviewed action boundaries separately on a shared monotonic timeline. |
| Speech integration starts from zero | `PracticeSpeech` already supplies native text-to-speech and `PunchRecordingGuide` supplies scripted cues. Start by recording cue events and actual onset annotations. Add ASR only if hands-free trainer speech improves collection. Android's [SpeechRecognizer](https://developer.android.com/reference/android/speech/SpeechRecognizer) exposes explicit on-device availability checks; offline recognition is not universally available. |
| HybrIK/WHAM results are ground truth | Treat inferred 3D as pseudo-labels, retaining model/version, confidence and provenance. Independent mocap/multiview measurements or reviewed annotations are needed to claim ground truth. Do not evaluate solely against the teacher used to create training targets. |
| A 10-frame window and softmax > .8 ensure <100 ms response | At 10 Hz, ten observations span about 0.9 s before compute. A warmed causal window need not delay every prediction that long, but onset sensitivity must be measured. Softmax confidence needs calibration plus phase/hysteresis/rearm logic to avoid repeated or false triggers. |

## Evidence update — 30 September phone test

[The supplied phone test review](PunchSessions/20260930-phone-test-analysis.md) records 703 accepted observations, 25 committed actions and 23 new contacts. Assistance was active; direct tracking and its new rotation controls were off. M1 device A/B acceptance remains pending.

Before expanding assisted actions, use the new 703-observation regression together with the earlier fixture to correct (1) a downward reset beginning a canceled preparation and blocking the next stroke and (2) lost readiness after brief bilateral gating. Extend M2 with bag/contact diagnostics and guide cue records. Add a spoken recording path independent of punch assistance so direct-mode smoothing can be tested hands-free. Profile the persistent ~100 ms observation delivery and long rendered-pose intervals on the phone. App event counts are not gesture recall, and scheduled prompts are not reviewed action labels.

## Evidence update — new full-body and punch recordings

[The new paired phone review](PunchSessions/20260930-full-body-and-punch-analysis.md) honors the user-excluded full-body 2:22–2:32 movement. Both schema-2 logs complete with zero recorder drops, but the full-body avatar retains bent-forward legs in standing and suppresses several lateral arm/elbow/left-knee movements. Reinitializing on captured standing samples greatly improves reconstructed leg neutral; lateral arms remain distorted. The report also identifies a sweep candidate that cannot transition to extension, and distinguishes three inward-direction contact rejections from one geometry near-miss. Actual bilateral overhead raising remains negative. Neither rotation experiment was enabled.

M2 collection is working, but its motion acceptance gate has **not passed**. Add standing-distance/framing calibration gates, source reconstruction/application state capture and actual recorded input-to-rendered-rig checks before M3. Explicitly record unclamped camera tilt and pre-session calibration state; the logged 45° setting is the current ceiling. Keep the incorrect left-extension segment unscored. The new fixtures and diagnostic editor replay are available; this review makes no new runtime behavior or device-performance claim.

## Execution order and exit gates

### M1 — Rotation stability foundation (first implementation in this change)

- Harden the new scalar/quaternion filters against invalid samples and repeated/backward timestamps. Use shortest-arc quaternion interpolation with shared angular-speed adaptation; define beta in Hz per degree/second.
- Add per-bone root-relative state to the direct path, including its stabilized torso/head targets and its limb/foot targets. Add separate persisted smoothing and angular-cap switches, initially off. Keep the default direct-mode selection unchanged.
- Hold accepted rotations during a per-joint confidence loss while the overall pose is fresh; reset histories when global tracking expires, another pose owner takes over, modes change or the avatar unbinds. Seed resumption from the visible pose. The angular cap has a maximum 50 ms catch-up budget after a stall.
- Validate rest jitter and step response at 30/60/120 render Hz, normalized rotations, q/−q equivalence, timestamps, gap recovery, speed limiting and settings compatibility. Run existing punch validation against the checked-in phone fixture.
- Device gate: same capture/rig, assistance off, RTMPose direct on; compare existing filtering alone, added rotation smoothing alone, cap alone, and both. Record onset/peak/reversal error and final bone jitter. Keep experimental stages off by default until device evidence supports enabling them.

### M2 — Full-body recording and replay

Extend the bounded recorder with a new schema version: all 26 raw image joints and scores in documented model order; camera aspect/orientation/mirror/ROI; source ID; frame-available, result-received and applied timestamps; effective settings and calibration; final local bone rotations/root; action phase and contact state; queue drops. Preserve old punch readers. Log scripted cue events now; keep cue labels independent of detector output.

Build replay at original observation timestamps with render resampling and an A/B result report. Include standing noise, overhead hold, squat hold, left/right knee raise and front kick, withdrawal, ankle loss/reentry and camera movement. Repeated render frames must not count as extra observations. Identify whether an error enters at image detection, reconstruction, retargeting or composition. Exit: repeatable full-body fixtures and stage-specific measurements. Do not label slow pose cadence as low render FPS or frame availability as exposure time.

### M3 — One new assisted action with explicit contact states

First prove uppercut and front-kick animations independently using deterministic controls, side/phase events and cancel/recover behavior. Add one heuristic detector at a time in shadow mode, using confidence, body-relative features, trajectory, time-based hysteresis and rearm. A held knee, foot shuffle, overhead reach or hand return must not trigger repeated strikes. Review detection footage before enabling hit effects.

For front kicks, replace unconditional two-foot planting with stance/swing/recovery states in the existing final pose owner. Release the kicking leg, bound support-foot corrections and return smoothly to tracking/guard. Gate hit events once per action at validated contact. Squats need calibrated root/pelvis displacement; jumps need an airborne state and landing, not simply a pair of rising image ankles. Exit: left/right success plus negatives, withdrawal and loss tests, with no pose-owner conflicts.

Do not expand the assisted 28° torso offset to 40° globally yet. Measure torso response and foot/contact reach together; parameterize and A/B the bound once full-body replay exists. A larger bound cannot supply unobserved yaw.

### M4 — 3D model experiment (conditional)

Compare existing 2.5D output, the already integrated MediaPipe world pose, and one pinned lifting checkpoint on the same footage. Record joint order, axes, units/scale, normalization, missing-score policy and Halpe26→H36M synthetic joints. Keep original foot observations with explicit confidence; a 17-joint lifter does not create measured toe/heel depth. Test causal short windows, startup padding and gaps before ONNX conversion; compare exported outputs numerically with the reference. Profile supported operators, allocations, memory, p50/p95 inference and sustained thermal load in the actual Unity game. Adopt only if added depth accuracy/visual quality outweighs latency and resource cost on the target phone. Retain the baseline fallback.

### M5 — Learned intent and optional trainer speech

Collect diverse participants/sessions and many negatives. Use the scripted guide first; optional Vosk/native ASR must retain speech onset/end, recognition completion and label provenance. Review movement onset/apex/end. Split train/validation/test by participant/session **before** extracting overlapping windows. Resample in time and include confidence/missing masks and useful velocity features. Retain idle/unknown classes. Start with a small causal temporal CNN, then export and compare reference/Unity logits. Confidence thresholds, refractory/rearm behavior, false triggers per minute, miss/side confusion and onset latency are acceptance criteria. Prefer heuristics until held-out improvements are demonstrated.

### M6 — Broader motion and polish

Extend side kick/roundhouse, squat/duck/jump, calibration, per-region settings and both rigs only after the previous gates. Test varied camera heights/pitches including the actual floor setup, framing, lighting, user proportions, recovery and ten-minute device runs. Use physical-person-plus-display video for actual motion-to-display latency. Proposed goals of ≥20 independent poses/s and <100 ms detect-to-avatar require measurements; report physical motion-to-display separately. Publish limits if the target device cannot meet them.

## Current delivery status

Implemented M1 code and automated checks. M2 now has versioned full-body/cue/contact capture and a spoken body-test guide; full-body recording replay, A/B metrics and its device exit gate remain pending. M3–M6 remain planned. This milestone adds no model downloads, ASR runtime, new action clips or enabled gameplay triggers. Validation results and exact test instructions are recorded below after execution.

### Validation result — 30 September 2026

Unity **6000.6.0f1** compiled the project and exited successfully after `RotationStabilityValidation.RunBatch`:

- **61/61** rotation stability checks passed; report: `Logs/rotation-stability-validation.txt`.
- **150/150** existing punch checks passed, including `PhoneReplay()` over the 756-observation fixture; report: `Logs/Practice/punch-validation.txt`.
- Editor compilation/execution log: `Logs/rotation-foundation-validation.log`. Existing deprecation/import warnings remain; these are not evidence of device readiness.

Run from the project root with the installed editor:

```sh
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD" \
  -executeMethod RotationStabilityValidation.RunBatch \
  -logFile "$PWD/Logs/rotation-foundation-validation.log"
```

Device/rendered-rig A/B testing, combined-filter latency and 70° camera accuracy remain unverified. These synthetic tests do not establish final avatar quality, action accuracy on new people, or Android performance. Full-body replay and broader assisted actions remain pending. The later full-body collection milestone is described in [Full-body-test-delivery.md](Full-body-test-delivery.md).

To try this change: enter RTMPose practice, disable punch assistance, select Direct avatar in Tracking, then enable each new rotation option separately. Save responsiveness settings only after comparing stillness, fast extensions, overhead holds, reversals and loss/reentry with the same rig and setup. Revert the two toggles to return to the original unfiltered rotation application. Original edited-file copies are retained under `ImplementationBackups/20260930-rotation-foundation/`; no Git commit was created because this folder has no Git metadata.


## Recorded-data fix build — 30 September 2026

The standing-reference, neutral-shin, lateral-arm/leg-direction, rendered-skinning ownership, torso-axis, punch type-transition and relative-contact fixes are implemented. See `Docs/Standing-body-fix-delivery.md` for evidence, APK and phone instructions. The recorded input → published pose → actual Remy rig path passes the reviewed neutral/arm/knee windows and rendered snapshots; the excluded left extension remains unscored. M2 device acceptance remains pending the new phone test; the original 10 Hz cadence and physical latency target are not claimed as improved.
