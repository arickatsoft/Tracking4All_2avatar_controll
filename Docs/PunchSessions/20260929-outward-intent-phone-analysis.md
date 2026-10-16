# Independent phone test of outward-intent build — 29 September 2026

## Assessment

The supplied files are a new test of `punch-assist-3-outward-intent`, using RTMPose, Remy and a Samsung SM-S928B. They expose reproducible recognition failures that the previous recording and synthetic tests did not cover. The current build is not yet reliable enough for unrestricted strikes or fast combinations.

The main problems are the paired-arm suppression latch, the return/rearm rule, and residual triggers during downward arm resets. Camera processing speed remains essentially unchanged. Most landmarks pass the confidence threshold; lack of confidence is not the primary cause of the reviewed misses.

This review does not modify runtime code or produce another APK. Input copies, extracted review frames, derived CSVs and the reproducible analysis script are retained in `Docs/VideoReview/punch-response-phone-test/`.

## File identity and alignment

- Log SHA-256: `7b8fe082f6ddc5360a47cc15d542d47d8cccb01d7053390dc0b48ef398c5b467`.
- Video SHA-256: `4246141377987a835b938b65ba621b3d944d9807f399ca0ac747e4b08da6956e`.
- Video: 80.300 seconds, 392 × 850, variable frame rate; audio track present. Review here used image sequences and log timestamps, not an audio transcription.
- Log: 74.988 seconds, first rendered-pose timestamp 76.68176882916913.
- Approximate mapping: **log-relative time = video time + 1.3 seconds**. At video 30 s, the HUD shows REC 31.3 s; at 50 s, REC 51.3 s; at 60 s, REC 61.3 s. Allow about 0.1 s for HUD rounding and sampled frames. This screen-preview alignment is not exposure-to-display latency calibration.
- Recording begins with action ID 13 and five existing hits. Those are carried-in state, not new actions in this experiment.

## Measurements

| Metric | Result |
|---|---:|
| Independent accepted observations | 720 |
| Recorded final avatar poses | 3,196 |
| Writer-dropped records | 0 |
| New action starts | 14 (IDs 14–27) |
| Commitments / new scored hits | 13 / 13 |
| Left / right commitments | 9 / 4 |
| Canceled preparation | 1 |
| Hit counter before / after recording | 5 / 18 |
| Accepted observations per second, whole session | 9.60 |
| Median interval between accepted observations | 99.9 ms |
| Recorded avatar poses per second | 42.62 |
| Median camera-availability-to-result delay | 99.0 ms |
| 95th percentile camera-availability-to-result delay | 109.4 ms |
| Median preparation-to-commit interval | 100.6 ms |
| Median commitment-to-bag-contact interval | 92.6 ms |
| Median candidate camera-availability-to-bag-contact interval | 300.1 ms |
| 95th percentile rendered-pose interval | 62.4 ms |
| Rendered-pose intervals above 50 ms | 543 / 3,195 (17.0%) |
| Left / right confidence ≥ 0.35 | 719 / 720; 717 / 720 |
| Observations suppressed by paired-arm gate | 138 / 720 (19.2%) |

The first new accepted observation arrives 2.666 seconds after recording begins; an additional 1.513-second observation gap occurs during the final step-out section. Thus 9.60 Hz is the session-wide accepted rate, while the typical interval is close to 10 Hz. It is not a measurement of the camera sensor frame rate.

Every committed app action hits the bag once. That establishes that the downstream animation/contact path works for these events; it does **not** establish that those 13 events were all correctly recognized human punches. Two reviewed events occur during arm lowering/reset. Overall recall cannot be calculated without labeling every intended attempt.

No commitment has an increase greater than 5° in the projected elbow-bend feature from the preceding observation. However, that condition does not distinguish every strike from resetting a hand downward. Projected angles are 2D features, not physical anatomical measurements.

## Failures verified against the video

### 1. The paired-arm filter rejects ongoing combinations

The log remains in `Move one arm at a time` from relative **55.312 to 59.300 s**, approximately video **54.0–58.0 s**. The video shows successive alternating arm strokes while the avatar remains in its stance with some torso motion. No punch action starts in this interval.

The trigger at observation 992 sees both wrists rise in body-relative coordinates: left Δy≈0.106 and right Δy≈0.180 torso units. Subsequent observations contain opposite-direction/alternating movement, but the global latch stays active until both arms settle. For example, observation 1000 has left Δy≈+0.112 and right Δy≈−0.101; it remains blocked despite no new bilateral raising/lowering condition.

Another trigger at observation 874, relative 43.668 s, suppresses a strong right stroke because the other wrist also rises modestly: left Δy≈0.088 versus right Δy≈0.499. Thus a small movement of the non-striking arm can veto the dominant strike.

During the scheduled alternating section, 38 of 81 observations (46.9%) are blocked. The one commitment inside that section at 50.023 s belongs to an action prepared at 49.909 s; there are no new action starts during relative 50–58 s. This is not a quantified count of intended combinations, but it is clear evidence of over-suppression.

Review: `camera-54.jpg`, `comparison-53.jpg`, `observations.csv`.

### 2. Rearming can remain stuck through new strokes

The right detector stays `Spent` from relative **39.271 to 42.828 s**—3.557 seconds—while the video shows additional right-arm strokes, notably around video 38.5–39.2 s and 40.2–40.8 s. The confidence scores remain valid.

The previous action begins with a nearly downward direction: approximately **(0.221, −0.975)** in normalized image coordinates. The rearm rule later requires the return to project negatively onto that old direction. In this curved, changing-height motion, the actual wrist return projects positively instead. At observation 845, the arm has folded by about 114° relative to the stored extended feature, but the projection is +0.379, so the detector does not rearm. It eventually rearms only after sufficiently slow samples near a folded position.

The left detector also remains `Spent` for 4.404 seconds after action 20; that interval partly overlaps the transition to right-hand exercises, so it is not all labeled as missed left attempts.

Review: `comparison-37.jpg`, `comparison-39.jpg`, observations 831–866.

### 3. Lowering one arm can still trigger a strike

Around video **34.0–34.3 s**, the user lowers the left hand after the preceding stroke and the avatar plays another punch (action 20, commitment relative 35.475 s). Around video **37.8–38.1 s**, the right hand lowers toward the waist and the avatar punches again (action 22, commitment relative 39.272 s), before the following upward/outward stroke visible at about video 38.5 s.

These are visible timing mismatches consistent with false reset triggers. The exact player's intent is not independently labeled, but the avatar responds during the downward reset rather than the subsequent outward stroke. Straightening the elbow while lowering the hand can satisfy the current opening/reach tests. The paired-arm rule does not reject an isolated one-arm reset.

The earlier claim that the wrong-phase issue was corrected must therefore be limited to the previously tested return pattern. This new test demonstrates that the broader problem remains.

Review: `comparison-33.jpg`, `comparison-37.jpg`.

### 4. The true bilateral negative gesture is successfully suppressed here

The actual two-hand overhead raise/lower around video **67–70 s** does not produce a new punch. The last hit occurs earlier, around video 64 s, during a separate one-arm stroke. Do not label it an overhead false positive merely because the spoken schedule changes at log-relative 65 s. Prompt times are not ground-truth movement labels.

Review: `camera-68.jpg`, `comparison-67.jpg`.

## Responsiveness and body control

The previous recording had about 98.8 ms median camera-availability-to-result delay and 42.5 recorded poses/second. This test has about 99.0 ms and 42.6 poses/second. There is no demonstrated inference-speed improvement.

Long pose intervals still correlate closely with the inference cycle. In the interior-frame analysis, 518 of 542 intervals above 50 ms are followed one pose later by a changed observation sequence. The association is strong, but it does not identify whether preprocessing, inference scheduling, GPU contention, readback, skinning or recording is responsible. A target-device profiler is required. This statement is deliberately more precise than assuming every long frame is the frame that receives a new observation.

The avatar visibly has torso motion, so the assistance mode no longer holds all torso movement static. This recording does not provide a dedicated labeled dodge, duck, squat or kick test, and cannot establish their accuracy or response time. The camera preview shows feet in this run, so it is not a validation of feet-cropped kick recognition.

The small preview and its landmark overlay limit exact human-motion labeling. The avatar's light-colored leg also overlaps the background of the white REC/frame text, making it hard to read. An opaque diagnostic text panel would improve repeat testing.

## Next corrections, in order

1. Replace the indefinitely latched two-arm veto with bounded gesture evidence. Distinguish dominant single-arm strokes and alternating motion from genuinely shared overhead/relaxation gestures. Release when the motion becomes asymmetric; do not require both hands to stop before combinations can resume.
2. Separate downward reset/chamber motion from outgoing strikes using recent trajectory and body-relative pose context. Elbow opening plus radial travel alone is insufficient. Do not reintroduce a blanket upward-image veto.
3. Rearm per arm from recent motion reversal/chamber evidence across curved paths and changing hand heights. Remove dependence on a single old launch vector. Keep one action per strike and prevent held-extension duplicates.
4. Add both recordings as regression data: retain the original return and bilateral-negative protections while requiring the newly reviewed curved strokes and combinations to respond. Record the exact gate/rearm decision and evidence so future logs explain rejection directly.
5. Profile the actual phone and improve independent observation cadence. The present approximately 10 Hz evidence stream and approximately 99 ms delivery delay cannot support a credible near-zero-latency claim, even if animation completion is smooth.

The supplied files are sufficient to reproduce these punch-recognition defects. Another recording is not needed before correcting them. A later fresh run is necessary to evaluate whether the changes generalize.

## Reproduce metrics

Run `python3 Docs/VideoReview/punch-response-phone-test/analyze.py` from the project. It writes `summary.json`, `actions.csv`, `observations.csv` and `stages.csv`. It excludes carried-in action 13 and the five hits already present when recording starts.
