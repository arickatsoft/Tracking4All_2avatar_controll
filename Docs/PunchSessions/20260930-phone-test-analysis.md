# Phone test review — 30 September 2026

Reviewed `/Users/jotit/Desktop/tracking.jsonl` together with `/Users/jotit/Desktop/vid.mp4`. The recording shows a working assisted-punch path, persistent observation/render delays, and two reproducible missed strokes caused by detector state transitions. It does not test the newly added direct-bone smoothing controls.

## Capture and alignment

- Samsung SM-S928B, Remy, RTMPose, session version `punch-assist-3-outward-intent`, Unity 6000.6.0f1.
- `directAvatar=false`, `directRotationSmoothing=false`, `directRotationSpeedLimit=false`; the video displays PUNCH ASSIST ON.
- Saved tuning differs from defaults: minimum cutoff 6 Hz, motion response 8, extra prediction about 30.7 ms. Those values belong to the pose pipeline; they do not speed up independent inference or smooth the authored attack clips.
- Log spans 74.992 s. Video spans 78.684 s at 392×850 and has an audio track. Review used timestamped log records and extracted image sequences, without transcribing or judging speech audibility.
- Approximate mapping: **recording-relative time = video time + 2.15 s**, within about 0.1 s. Native seeks at video 20 s and 50 s show REC 22.2 s and REC 52.1 s. First logged pose (`107.13219464959731`) defines relative zero. All exact times below are recording-relative unless marked video.
- One-FPS overview sheets use nominal sampling bins; dense crops use 4 Hz. The screen preview and inference overlay have their own age. They are useful for broad gesture/state matching, not exposure timing or physical latency measurement.
- The initial action ID 8 and six existing hits are carried-in state and excluded from new-event totals.

Log SHA-256: `4a337c1ecd9f472b07738e552d44a94409569446c85f94c4313bcd465590a743`.

Video SHA-256: `6478ff21728276407c7a724b8f27ea40506e38f395fecc43dde9d69b14aed876`.

## Measurements

| Measure | Result |
| --- | ---: |
| Accepted independent observations | 703 |
| Recorded final avatar poses | 3,148 |
| Writer-dropped records | 0 |
| New action starts | 28 (IDs 9–36) |
| Committed app actions | 25: left 11, right 14 |
| Canceled preparations | 3 (IDs 14, 16, 18) |
| New scored contacts | 23; hit counter 6→29 |
| Committed actions with no hit | 2 (IDs 22, 28) |
| Session-wide accepted observation rate | 9.37 Hz |
| Typical observation interval, median / p95 | 100.4 / 116.8 ms |
| Camera availability → delivered result, median / p95 | 99.4 / 110.1 ms |
| Recorded final poses per second | 41.98 |
| Recorded pose interval, p95 | 64.1 ms |
| Pose intervals over 50 ms | 597 / 3,147 (19.0%) |
| Preparation → commitment, median | 100.1 ms |
| Commitment → accepted bag contact, median | 91.2 ms |
| Candidate frame availability → accepted bag contact, median | 297.1 ms |
| Arm confidence ≥0.35, left / right | 699 / 703; 701 / 703 |
| Observations blocked by shared-arm gate | 76 / 703 (10.8%) |

The first new accepted observation arrives about 3.02 s after recording starts while the person is moving into position. The typical active observation rate is about 10 Hz; session-wide rate includes setup and loss periods. Saved pose cadence and screen-video cadence are distinct from camera/model cadence.

**The 297 ms value is a software pipeline timing to bag contact, not physical motion-to-display latency.** The available-frame→result, extra observation needed for commitment, and clip→contact intervals explain why the game response can feel delayed. Source-frame exposure, native camera queues and display timing remain unmeasured. The log lacks stage-by-stage GPU/CPU timings, so it cannot prove which inference or render stage causes the long frames.

## What the video and log establish

### 1. Downward reset starts a false preparation and blocks the next stroke

After the committed left action 13, the hand lowers. At observation **554**, relative **29.469 s** (video roughly **27.3 s**), the detector starts candidate/action 14 during that lowering motion. The wrist's latest step is approximately `(0.093, −0.148)` in torso units: predominantly down. The current guard rule tests a downward displacement of more than 0.15 from its changing origin; this particular return slips below that threshold and also satisfies elbow-opening evidence.

The preparation is withdrawn at observation **557**, relative **29.790 s**. `LockReturn` sets the detector to `Spent`. Observations **559–561**, relative **29.990–30.185 s**, show renewed upward extension (wrist y rises from about 0.262 to 0.938), but the detector remains `Tracking recovery`. The video at about **27.7–28.2 s** shows that new upward stroke. It produces no new app action or hit.

This is a missed input caused by state history, rather than low arm confidence. Action 14 is a **false preparation**, not a false scored hit. Preserve that distinction when measuring results.

Evidence: [dense camera sequence](../VideoReview/20260930-phone-test/recovery-camera-0.jpg), observations 551–561 in `observations.csv`, and the source `PunchIntent.Observe` / `LockReturn` logic.

**Next fix:** reject predominantly downward onset using recent trajectory in addition to changing-origin displacement; keep a canceled, uncommitted preparation from imposing the same rearm policy as a completed strike. Check one-arm returns, feints, low starts and lateral hooks on both sides before enabling new thresholds.

### 2. Gate release still leaves the detector unarmed for a quick follow-up

The right action 26 commits at **49.593 s**. Shared-arm suppression runs from **49.995 to 50.292 s** while the hand returns. The gate releases at observation **764**, relative **50.404 s**, but both detectors have been reset to `Unarmed` by the inhibition path.

One relatively quiet right observation at **50.501 s** begins reacquisition, then the next observation at **50.598 s** is already moving again. The stillness requirement is not fulfilled. Observations **766–769**, through **50.895 s**, show the new upward right stroke, but the right detector stays `Unarmed`/`Tracking recovery`. The video around **48.4–48.8 s** confirms an upward stroke; no action starts for it.

This suppression lasts less than the earlier indefinitely latched problem, but losing readiness on every gate frame still causes a missed quick follow-up.

Evidence: [dense camera sequence](../VideoReview/20260930-phone-test/alternate-camera-0.jpg), observations 760–770 and `PunchGestureGate` plus `PunchIntent`'s inhibition handling.

**Next fix:** distinguish brief gesture inhibition from lost tracking. Preserve or reconstruct a folded-arm readiness reference on release without demanding a long stationary pose. Require independent fresh outgoing evidence, and retain bilateral-overhead rejection. Replay aborted/bilateral gestures to ensure fast rearming does not create phantom actions.

### 3. Two committed animations do not score a contact

Actions **22** (commit **43.104 s**, video about **41.0 s**) and **28** (commit **56.136 s**, video about **54.0 s**) enter Striking but never increase the hit counter. Nearby video shows the bag swinging; the current controller captures a bounded target at preparation and deliberately does not chase the bag afterward.

This is sufficient to identify **two committed attacks without scored contact**, but not to label a recognition miss or a contact bug. Bag transform, captured strike target, contact point and rejection reason are absent from the recorder. All other committed actions score once; none scores twice.

**Next instrumentation:** record bag pose/velocity, target, fist sweep, phase, action ID and contact/rejection events. Keep contact-dependent scoring; do not score every classifier commitment merely to make the totals equal.

### 4. The actual bilateral overhead negative is suppressed

The person raises both hands together at roughly video **65.0–65.3 s**, holds them overhead, and lowers them around **67.5–68 s**. The gate blocks from relative **67.221 to 70.535 s**, and there is no new action or hit during that actual gesture.

A commitment at **65.689 s** falls inside the guide's scheduled 65–70 s “raise/lower” interval, but the video still shows a separate single-arm motion then. It is not evidence of a bilateral-overhead false positive. Prompt schedules are weak labels, and the participant's movements run later than the prompts.

Likewise, the video shows a pause at roughly **49.5–52.7 s** before the alternating sequence. There are seven alternating-side commitments during approximately **55.44–59.73 s**, with no prolonged global blocking then. The entire preceding quiet interval must not be counted as missed combinations.

### 5. Assistance supplies attack shape; this is not direction replication

The physical strokes in the preview often travel upward, sometimes with the hand above the head. The avatar completes these as its existing bag-facing left/right punch clips. That is consistent with the current intent-driven assistance design, which exposes side and timing but does not classify uppercuts or reconstruct attack direction. If the desired output is “my upward stroke becomes an uppercut,” the action type and corresponding clip/composition support remain missing.

The assisted stance also supplies authored animation and planted legs. The video has torso/head movement, but arm/torso variation in the final pose cannot be attributed entirely to tracking noise: the guard clip itself moves. A dedicated direct-mode capture or decomposition of authored/observed contributions is necessary for smoothing acceptance.

## Consequences for the execution plan

1. Fix and replay the two demonstrated detector-state failures before increasing action vocabulary. Retain the older phone fixture and add this new session as a second regression; neither is independent generalization evidence after tuning.
2. Extend the recorder to log guide cues, raw full-body joints/calibration and contact diagnostics. The current session contains arms/torso but no raw leg/foot joints or camera pitch/calibration details, so it cannot validate kicks, lifting or the 70° setup.
3. Run a separate spoken direct-mode test for the rotation-stability A/B experiment. The present RECORD TEST path requires assisted mode. Add recording/guide support independent of the final pose owner so the person can test hands-free with assistance off.
4. Profile camera/inference and long render frames on the target device. This run's ~99 ms delivery timing remains close to the earlier run; increasing cutoff or prediction has not established faster observation delivery.
5. Add uppercut/front-kick completion only after labeled intent/recovery regressions and stance/swing contact states exist.

No runtime thresholds or gameplay behavior were changed during the initial review. The later correction is recorded below. A deterministic observation fixture and reviewed diagnostic labels were saved for the next correction.

## Reproduce and inspect

Run `python3 Docs/VideoReview/20260930-phone-test/analyze.py` from this project while the supplied files remain at their original paths. Outputs are `summary.json`, `actions.csv`, `observations.csv`, and `stages.csv` in that directory. The stages are guide schedule bins, not ground-truth action labels.

The portable regression fixture is `Docs/PunchSessions/20260930-punch-regression.jsonl`: it retains all 703 original observation objects and the source hash, without rendered pose arrays. `reviewed-events.json` records the reviewed diagnostic constraints separately from model predictions. Full-resolution native alignment stills, overview sheets and dense preview crops are under `Docs/VideoReview/20260930-phone-test/`.


## Follow-up correction

Implemented the recent downward-trajectory rejection, separate canceled-preparation recovery, and preserved folded-arm reference across brief gesture inhibition. The second replay rejects preparation at observations 554–557, commits the next left stroke once at 559–562 and the quick right follow-up once at 766–770, and retains zero commits during the reviewed bilateral overhead window. The older 756-observation replay also passes. This is regression evidence after tuning, not held-out recognition accuracy.

Added a separate four-minute spoken full-body test, schema-2 full-body/cue/final-pose capture, and bag/contact rejection diagnostics. See `../Full-body-test-delivery.md`. Moving-bag misses remain contact-dependent. The new Android build needs a fresh target-phone run before claiming improved physical performance or final animation quality.
