# Responsive moves: September 30 phone review

Reviewed the new `body.jsonl` / `body.mp4` and `punches.jsonl` / `punches.mp4`. Attachments were treated as recorded evidence. The 2:22–2:32 exclusion from an older recording does not apply to these files.

## Findings

- Median frame-availability-to-tracking delivery: **100.3 ms** in the body run and **100.4 ms** in the punch run. Tracking supplies roughly **9.7 results/s** although the camera supplies about **29 frames/s**. These timestamps start at Unity frame availability, not sensor exposure; they cannot establish complete physical motion-to-display latency.
- In direct body tracking, delivery-to-first-applied-avatar-target is **3.34 ms median**. Render/record intervals have a 61–63 ms p95, so display cadence also varies.
- The original punch run contains **25 commitments and 25 new scored contacts**. The reviewed failures occur before commitment, rather than a committed punch missing the bag.
- Right strokes around recording 46.1 s and 47.4 s were blocked as shared arm movement when the support hand moved less and folded. A left stroke around 54.9 s lost confidence immediately after a decisive extension sample.
- Both overhead arms are visible in the body camera landmarks, but the single-arm forward-depth prior pulls their upper arms toward horizontal.
- The body guide reacquired a standing reference at **22.393°**, replacing the initial 45° setting. Replays use the measured pitch and fresh calibration on the recorded standing segment; accepted-only logs omit preceding calibration observations.

Video/record clock estimates from visible HUD readings: body recording time ≈ video +1.1 s; punch recording time ≈ video +1.7 s. Treat these as approximate alignment, not exposure timestamps. Sparse guide cues were not treated as proof that a movement occurred.

## Changes

- Distinguish a dominant extending arm from a folding support hand in the bilateral gesture gate, including release of a provisional shared-raise block.
- Commit a high-confidence, substantial extension from a previously observed folded reference without waiting for another endpoint sample. Small preparations, half-extensions, hooks and ambiguous strokes still require confirmation.
- Reject shared straight-arm spreading as a bilateral gesture.
- Preserve the observed upward direction when both coherent arm chains extend above the head. This uses a bilateral overhead prior; single-arm depth remains approximate.
- Start inference on the latest fresh, unprocessed camera frame even when readback completes between camera deliveries. Keep one inference in flight and reject duplicate or stale frames. Device throughput improvement remains unmeasured.
- Record CPU scheduling duration, age at scheduling, scheduling-to-readback completion, and completion-to-consumption separately. Scheduling-to-readback includes GPU queueing, execution and callback delivery; it is not pure GPU compute duration.
- Session version: `practice-6-responsive-moves`; existing schema fields retained.

An attempted faster clip was rejected by the real contact test because the right fist exceeded the motion validity limit. The delivered build keeps the previously validated clip timing; earlier intent recognition reduces response delay without weakening collision validation.

## Validation

- All 166 existing assisted-punch checks, 69 body guide/schema checks and 170 tracking checks pass. The optional reference-person model input fixture is absent and remains skipped.
- New replay recognizes **29 commitments versus the prior 25**, preserving all 25 prior recognized strokes. Some decisive strokes commit one observation earlier (up to 101 ms); the median advance across preserved strokes is 0 ms. Same-session new observation replay and frozen previous detector comparison are in `Logs/Practice/responsive-moves.txt` and `responsive-punch-events.csv`. These are regressions, not an independent recognition accuracy estimate.
- New body inputs run through reconstruction, publication and actual Remy bone application. Standing, lateral arms, both knee lifts, torso yaw/side bend and overhead directions pass. Maximum measured final bone direction error is 0.03°. Overhead median upper-arm angle from up is 13.5°.
- Actual rendered settled overhead pose: `Logs/Practice/responsive-fix-overhead.png`.
- Real player-loop test validates both hands: observation → commitment → animated fist → one physical hit. It also validates recording, assist/direct ownership, reentry, home and pause cleanup.

## Phone test

Install `Builds/FitFight-responsive-moves.apk` over the prior build. Open Practice, calibrate in your usual setup, then run **FULL BODY TEST** and the spoken punch test. The body guide reacquires the standing reference; keep head, feet and raised hands inside the preview. Focus on overhead holds, raising/lowering, left/right knee lifts, quick repeated punches and alternating hands.

Save and send each new JSONL and matching video. The additional timing fields will show whether camera scheduling or readback delivery dominates the remaining delay. Physical phone latency and single-camera depth accuracy are not certified by desktop replay.

## Build artifact

Unity Android build succeeded with 0 errors and 731 warnings. APK signing verification passed.

SHA-256: `5dbff5004a0cc3ff7e483d29416c4c83f285ea52cae4133ae97fbe2809f83da1`.
