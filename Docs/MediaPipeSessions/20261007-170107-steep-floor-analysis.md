# Second October 7 floor session — app 1.12

Session `20261007-170107-047`, matching `vid.mp4`. Inputs are archived under `Logs/MediaPipe/20261007-170107-047/input/`; extracted video sheets, metrics and decision/hit timelines are in the parent directory. This review changes no runtime code or APK.

## The preview fix remains verified

The session completed with **4,505 final poses**, **4,506 preview refreshes**, **zero runtime errors** and **zero dropped JSONL records**. Final-avatar recording runs at **60.07 FPS**; the largest pose-record gap is **39.4 ms**. Video preview and REC time/frame advance throughout the run. The screen starts at 24 cumulative hits and ends at 50, correctly giving **26 new hits**.

There were 752 completed model inferences: **735 accepted**, 17 `no_person`. The arm observation rate is about **9.86 FPS**. There are no commits during initial setup, countdown, both idle cues, or step-out/re-entry. These are positive stability results, not proof that all other decisions are correct punches.

## Camera setup differs from the previous session

Both sessions use MediaPipe Full CPU, region motion-aware filtering ON, stable body correction ON, requested camera rate 60, direct avatar OFF and punch assistance ON. Their saved pitch differs:

| Measurement | 16:31 session | 17:01 session |
|---|---:|---:|
| Gravity-based pitch captured at calibration | 24.02° | 61.80° |
| Saved correction pitch | 24.02° | 45.00°, clamped |
| Median camera FPS | 52.3 | 45.3 |
| Median native computation | 41.57 ms | 41.69 ms |
| Median scheduling → GPU readback | 48.42 ms | 48.64 ms |
| Median complete inference age | 100.15 ms | 100.88 ms |
| Committed-punch decision median/p95 | 101.75/120.32 ms | 113.46/120.77 ms |
| Approximate capture → bag-hit median | 168.9 ms | 170.0 ms |

The second saved camera/calibration tilt is much steeper. That makes this an additional viewpoint case rather than a controlled repeat of the 24° setup. It does not establish a causal link between tilt and any detection error. Native compute/transfer costs and capture-to-hit latency remain essentially unchanged; the decision medians sample different movement events.

Keep floor placement as the target. For the next software comparison, use the earlier roughly 24° setup as the baseline, while retaining this steeper view as a replay regression case. No torso comparison is needed. If this placement was intended to be unchanged, verify phone tilt and recalibrate after repositioning; gravity pitch reflects the captured calibration reference rather than a continuously logged live angle.

## Cue counts need video alignment

| Cue | Commits L/R |
|---|---:|
| Slow left | 2/0 |
| Fast left | 4/0 |
| Slow right | 0/2 |
| Fast right | 1/5 |
| Alternating | 2/3 |
| Start then withdraw | 2/3 |
| Raise/lower both hands | 2/0 |

All 26 commits are followed by one physical bag-hit transition. The contact system does not repeatedly score a single committed action here. Classification can still create additional committed actions, so that one-to-one correspondence does not prove accuracy.

### Alternating: the sixth decision crosses the cue boundary

The five decisions in the alternating cue occur at REC **54.708 R, 55.607 L, 56.307 R, 57.189 L and 57.890 R**. The next **left at 58.702** follows the same sequence and is visible as a continued alternating movement, but is assigned to the withdrawal cue because that cue begins at 58 seconds. Further **R at 59.506, L at 59.988 and R at 60.545** also align with continued alternating motions in the video.

Thus this run does **not** reproduce the previous session's one missed alternating right punch. The summary's five alternating commits alone would give a misleading miss count. No `TRACKING JUMP` guard reset appears during this alternating sequence. The previous outlier-induced miss remains a valid independent regression case to fix.

### Fast right: guard/return motion produces extra decisions

During the right-hand fast section, the left hand commits at REC **47.072**. Its image wrist y changes little around the decision (approximately 0.18 → 0.17), with image speed only **1.35 torso lengths/second**; the video does not show a separate full left outgoing stroke. This is a suspected false support-hand classification using the current permissive world/extension evidence.

The right hand also commits at REC **47.270**, between its preceding decision at 46.687 and following outgoing decision at 47.568. The candidate begins at captured REC **47.068**, but confirmation occurs at **47.170** with image speed only **0.326 torso lengths/second** and wrist movement from y −0.15 to −0.12, near guard. The video and feature trace make this a guard/return or near-stationary confirmation regression target.

The candidate-confirmation branch currently checks remaining travel and extension/hook evidence, without requiring continued outgoing image speed. Raw world estimates can maintain apparent extension even when the observed wrist has settled. That explains why one physical right stroke can acquire an additional decision. Add coherent outgoing progress and recovery checks rather than globally raising sensitivity thresholds or increasing cooldowns.

### Withdrawal and raise labels overlap preceding motions

The five decisions recorded under withdrawal include the four continued alternating movements described above. The remaining right-hand decision at REC **64.481** aligns with a partial movement. Left-hand decisions at **65.225** and **66.310** are recorded under the next raise cue, but occur while the video still shows partial single-hand motions, before the actual bilateral raise.

The clear two-hand raise begins later, around REC **67 seconds**, and produces **no additional commit or hit**. Its overhead suppression still works in the observed video. The two summary decisions therefore should not be reported as two failed bilateral-overhead detections.

The partial movements still produce gameplay hits, confirming the need to improve withdrawal/settled-hand discrimination. Exact labels remain approximate because the screen video contains a small preview and cue times represent speech enqueue, not audible completion. Extracted-sheet timestamps can also differ by a sampling interval; use the visible REC clock for alignment.

## What this adds to the next fixes

1. Preserve safe learned-guard recovery after isolated outliers, using the earlier 16:31 missed right punch as the regression case.
2. Require coherent outgoing progress for candidate confirmation, and prevent guard/support-hand adjustments or returns from becoming fresh strokes. Use the 47.072 left and 47.270 right decisions from this run as negative replay cases, alongside the partial movements in both October 7 sessions.
3. Retain bilateral-overhead and idle suppression. The source of these two raise-cue decisions is preceding single-hand motion, so changing the bilateral gate alone would target the wrong problem.
4. Reduce camera-transfer latency while retaining MediaPipe Full and the floor baseline. GPU readback still adds about 49 ms before native inference.
5. Separate movement labels from cue boundaries in analysis and future test summaries, so continued punches do not appear as misses or withdrawal false positives merely because an instruction changed.

No additional recording is required to establish these cases. Both sessions are now archived for repeatable validation before the next APK. Evidence: `events.csv`, `reviewed-events.json`, `metrics.json`, `fast-right.png`, `alternating.png`, `raise.png` and `contact.png` in the session directory.
