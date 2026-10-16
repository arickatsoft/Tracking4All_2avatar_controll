# Video-matched phone test analysis — 28 September 2026

Inputs: `tracking.jsonl` and `vid.mp4`, supplied by the user. Working copies and review frames are in `Docs/VideoReview/phone-punch-review/`. This is a new recording, not the earlier `20260928-050811-641.jsonl` session. No runtime code was changed in this analysis.

## Conclusion

The dominant recognition defect is **wrong movement phase**. Many intended punch attempts are rejected during outward extension, then recognized during withdrawal. The low-angle camera view projects the outward attempts largely upward in image coordinates. The detector's upward-motion veto rejects them; its guard handling can then establish a new origin while the arm is held extended. Withdrawal becomes the next large movement away from that incorrect origin.

This explains why the avatar often stays in guard while the user punches, then reacts late. Faster animation or merely lowering speed thresholds would not fix the event ordering. Stricter blanket rejection of upward image motion would worsen the missed attempts in this setup.

## Video/log alignment

- Video duration: 93.794 seconds; image size 392 × 850, variable frame rate.
- Log duration: 74.995 seconds.
- Approximate mapping: **video time = log-relative time + 11.9 seconds**.
- At video 30.0 s the HUD reads REC 18.1 s / FRAME 243. Observation 243 was logged at relative 18.113 s.
- At video 80.0 s the HUD reads REC 68.1 s / approximately FRAME 755. Observation 755 was logged at 68.076 s.
- Allow approximately 0.1 s for HUD rounding, sampled video frames and the displayed frame counter. This alignment establishes movement ordering, not physical motion-to-photon latency.

## What is visible

| Approximate video time | User camera preview | Avatar/log result |
|---|---|---|
| 32.3–33.2 s | Left-hand outward punch attempt, followed by return | Avatar remains in guard; no action. The detector stays Guard during early extension, then reports Outside punch band. |
| 36.7–37.3 s | Another left-hand outward attempt | No early commitment. |
| 37.5–38.0 s | Hand returns toward guard | Action 1 starts; avatar punches and scores. |
| 42.0–42.4 and 43.4–43.8 s | Further left-hand outward attempts | Avatar remains in guard in the sampled sequence. |
| 58.0–58.5 s | Right-hand stroke returns to guard | Action 7 plays during the return; no bag hit on this action. |
| 59.0–59.8 s | Another right stroke and return | Avatar responds during the return phase. |
| 82.0–83.2 s | Both hands overhead, then both lowered | **Confirmed false punch:** action 17 starts at log 70.903 s and hits at 71.114 s. |
| 85.2–85.8 s | User relaxes arms and begins stepping away | **Confirmed false punch:** action 18 starts at log 73.581 s and hits at 73.796 s. |

The tiny camera preview supports the gesture ordering and bilateral raise/lower distinction. It does not support precise anatomical angles in physical 3D. The joint angles below are projected 2D features, not measurements of actual anatomical elbow flexion.

## Log measurements

- 3,188 final avatar poses, 756 independent accepted observations, no malformed records, zero writer-dropped records.
- 18 action starts, 17 detector commitments, 16 bag hits; one cancellation and one committed action without contact.
- **15 of 17 commitments have a downward-moving projected wrist.**
- **14 of 17 commitments have an increasing projected elbow-bend feature** between the observation before preparation and commitment: the arm is folding back rather than opening in the recorded image.
- Only four commitments are left-hand; thirteen are right-hand. These are not accuracy rates, since not every attempted punch has been individually annotated.
- Arm confidence passes on 748/756 left and 752/756 right observations. Confidence loss is not the main explanation for the missed attempts.
- Accepted pose cadence: 10.08 Hz. Recorded avatar update cadence: 42.51 Hz.
- Median camera-availability-to-result delay: 98.8 ms.
- Median preparation-to-commitment interval: 98.6 ms; commitment-to-hit interval for scoring actions: 95.1 ms.
- Median timestamp of first candidate camera frame to hit: 286.3 ms. **This begins at the detected candidate, often already during withdrawal**, so it understates how late the response feels relative to the original outward attempt. It is not end-to-end physical latency.
- 565/3,187 final-pose intervals (17.7%) exceed 50 ms; 95th percentile interval 63.2 ms.
- 560 of those 565 long intervals occur immediately before the next logged frame containing a new observation. Profiling is still needed to separate inference scheduling/GPU contention/readback/rendering/recording costs.

## Concrete detector trace

For the first scoring left punch, camera observations at relative 24.803–25.399 s show extension: wrist y rises from 0.299 to about 0.82 torso units and the projected bend feature drops from 150° to about 14°. The detector stays Guard and does not prepare.

At relative 25.695 s the wrist descends to y=0.493 and the bend feature rises to 110°. The detector enters Candidate. At 25.794 s the wrist descends to y=0.299 and bend rises to 164.5°; it commits. The video shows the same return-to-guard sequence.

## Code mechanisms

In `Assets/Practice/Runtime/Punching/PunchIntent.cs`:

1. Guard-state onset rejects `verticalRaise` using a fixed image-plane direction test. It has no camera-angle-aware interpretation of a forward punch.
2. While in Guard, every slow sample updates `origin`, even if that pose is an extended hand rather than a real bent-arm guard.
3. `InGuard` accepts either a near-shoulder wrist **or** a bent elbow. An extended wrist just inside the radius threshold can qualify.
4. The onset `opening` condition is satisfied by elbow speed alone. It can be true while the elbow is closing.
5. Commitment can be satisfied by elbow travel alone, even when the bend-change term says the elbow is closing.
6. `verticalRaise` is checked at onset only. Overhead-to-guard and guard-to-relaxed bilateral motion can still start actions.

Synthetic tests covered simple sideward extension, rest noise and a narrow overhead sequence. They did not cover this projected forward stroke followed by withdrawal, or the recorded bilateral negative gestures.

## Required correction

1. Acquire guard from a genuinely folded arm, with a short stable dwell. Do not redefine guard while a stroke is extended or held.
2. Separate extension, held extension, return and rearm. A return must never be eligible to start a new punch. Elbow speed/travel alone is insufficient commitment evidence.
3. Recognize outward extension under the camera geometry used here. Preserve useful early image-space evidence without classifying every upward image movement as a hand raise.
4. Suppress correlated bilateral overhead/relaxation gestures and require a real return to guard before rearming. Recheck the accumulated trajectory at commitment, not only onset.
5. Add replay regressions from the matched video windows: score outward attempts once, never score their return, and never score the confirmed bilateral negatives. Report that these are same-session regression results, not independent accuracy measurements.
6. Profile and reduce the inference-correlated frame stalls after the recognition phase is correct. Keep the authored animation-completion approach; it is currently receiving events at the wrong time.
7. Make READY reflect per-hand guard acquisition. The present UI can display READY even while the recognizer is not capable of accepting the intended stroke.

The existing pair of files is sufficient for the next targeted detector revision. No additional recording is needed before that work. A later phone run should validate the revised behavior independently.

## Review artifacts

- `Docs/VideoReview/phone-punch-review/actions.csv`: all action, commitment and hit timestamps.
- `comparison-32.jpg`: missed left-hand attempt.
- `comparison-36.jpg`: outward attempt ignored, return triggers avatar.
- `comparison-41.jpg`: repeated left-hand attempts ignored.
- `comparison-58.jpg`: right-hand return-phase responses.
- `comparison-82.jpg`: confirmed false punch while lowering both raised hands.
- `comparison-85.jpg`: confirmed false punch while relaxing arms/stepping away.

Each comparison sheet reads left-to-right, then top-to-bottom. Each tile shows the avatar above an enlarged camera preview, with the video timestamp printed at the top. The sheets use 4 samples/second; exact log event times are in the CSV.
