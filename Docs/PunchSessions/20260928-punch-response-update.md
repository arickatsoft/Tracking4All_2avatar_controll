# Flexible punch response update — 28 September 2026

## What changed

The controller no longer requires punches to start near the chin. It acquires a natural arm position, follows chambering toward the body, and recognizes extension away from that reference. It can rearm along the return trajectory rather than waiting for the hand to revisit one fixed spot. Lateral hooks use coordinated wrist/elbow travel and can retain their bend. These are causal 2D gesture heuristics, not a learned classifier or a claim to understand every arm motion.

The wrong-phase bug is corrected: a held extension cannot become a new resting origin, and folding back alone cannot satisfy extension commitment. Upward image motion remains eligible because the low camera in the supplied video projects intended punches upward. Correlated bilateral raises/lowering are gated before either arm emits an action. A cancellation or tracking discontinuity cannot score a bag hit.

Preparation still responds on the first qualifying observation; commitment uses a second independent observation. A large extension still visible at the next observation can complete even if the hand has stopped moving. Animation runs independently between observations and one action still needs real avatar/bag contact to score. Alternating punches may transition after the previous contact phase, earlier than the old late-recovery handoff.

RTMPose's existing calibrated torso/head estimator now contributes bounded additive motion while punch assistance owns the rig. Its previous suppression explains why assistance held the torso still. Torso and head offsets are limited to 28° and 30°, distributed along the spine/chest and neck/head rather than applied fully at every joint. Unseen feet remain in the planted authored stance. Forward torso bend is still an ambiguous monocular estimate. Tracking4All baseline has no matching torso/head estimator, so this overlay is RTMPose-specific.

Readiness is tied to detector state and visible arms. Calibration and spoken positioning instructions ask for shoulders, hips, hands and elbows; feet are optional. The spoken experiment now requests varied starting heights and natural movement between punches.

## Verification

- Unity compilation and 150 deterministic checks passed.
- Replayed every one of the supplied session's 756 independent observations using the production detector and paired-arm gate.
- Six visually reviewed outward-attempt windows each produce exactly one commitment; five reviewed return windows produce none.
- No commitments during the recorded raise/lower and step-away negative section (relative 65–75 s), including the two previously confirmed false triggers.
- No replay commitment occurs while the projected elbow-bend feature increases by more than 5° in that observation.
- Replay produces 25 commitments (11 left, 14 right). This is a diagnostic count, **not** a punch-recognition accuracy rate: not every movement in the session has been manually labeled, and some movements in the feint section still produce commitments.
- For the reviewed first scoring left attempt, the new commitment is around relative 24.894 s instead of the original 25.796 s—about 0.9 s earlier, on extension instead of withdrawal. This fixes event ordering; it is not a measured general motion-to-display latency reduction.
- Additional synthetic checks: low/middle/high hand starts on both sides at 10/30/60 Hz, continuous chambering from a hanging arm, lateral hooks, held extension, return lockout, small feints, paired raise/relax, lost confidence, duplicate observations, discontinuities, body offset bounds and bone lengths.
- Actual Play-mode scene checks passed: both hands produce one physical bag contact per injected punch; contact-phase fist gap is about 4.3 cm with the existing fist sweep radius; mode toggle, home cleanup and reentry pass. Pose images were produced from the running scene.
- Existing RTMPose regression/GPU smoke checks passed; the optional reference-person fixture was unavailable and skipped.

The compact fixture `phone-punch-regression.jsonl` preserves the unmodified arm observations and their timestamps, records the source SHA-256, and excludes video/irrelevant per-render bone records. The runtime never reads this fixture or its time windows.

## Build and phone check

Android build completed successfully: zero errors, 731 project warnings; APK approximately 249 MB. Build report: `Logs/punch-response-build.txt`.

Build entry: `MobileTrackingBuild.BuildPunchResponseBatch`. Output: `Builds/FitFight-punch-response.apk`. Session metadata identifies `punch-assist-3-outward-intent`.

1. Install this APK, choose RTMPose and enter Heavy Bag practice with punch assistance on.
2. Position the upward-facing front camera so shoulders, hips, hands and elbows remain visible. Feet can be cropped. Calibrate while upright.
3. With recording off first, try punches from waist/chest/chin heights, lateral hooks and alternating hands. Bend/lean and turn the head to check the bounded overlay. Compare responsiveness and false activations as well as misses.
4. Start the phone screen recorder, then tap RECORD TEST. Follow the spoken 75-second varied-position experiment. Export the new matching JSONL and screen video. This fresh run is needed for independent phone validation; the old replay was used during development.

## Remaining work toward whole-body camera controls

Zero latency and reliable recognition from every arm location are not realistic guarantees. The supplied run delivered roughly 10 independent observations/second and about 99 ms median camera-availability-to-result delay. The new recognizer has not increased that inference rate. Very brief movements between observations, hidden hands, and ambiguous movements directly toward the lens can still be missed or misclassified. Shared bilateral raises intentionally suppress punch actions. A front camera cannot determine whether two visually identical motion prefixes will become a feint or a punch.

The next performance task is a target-phone profile separating camera delivery, preprocessing, GPU inference, readback, animation/rendering and optional recording. Reduce measured stalls and improve observation cadence before lowering confirmation requirements. Prefer bounded early animation over long extrapolation of noisy limbs.

For the broader controller, use continuous measured torso/hip motion for lean and crouch, and discrete movement intent plus authored completion for strikes, kicks and jumps. Existing left/right kick and squat assets are available, but require separate validated intent and contact windows. For feet-cropped kicks, use confidence-qualified hip/knee lift and trajectory; complete the lower leg with the authored animation and preserve the support foot. Do not fabricate kick intent when thighs/knees are also invisible. Squat/jump discrimination needs visible hip movement relative to a stable camera reference and temporal evidence; apparent scale change alone is insufficient.

A small next dataset should include natural strikes from varied starts, relaxed arm lifting, hooks/uppercuts, feints, torso lean/crouch and visible-knee kick starts, with matching timestamped landmarks and video from the actual phone position. The current video is enough to fix the demonstrated punch bugs, but does not validate all of these new movement classes.
