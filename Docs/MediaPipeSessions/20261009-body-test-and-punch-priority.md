# October 9 session and boxing priority

Session `20261009-084756-943`, app 1.12. The JSONL, both summaries and matching screen video are archived under `Logs/MediaPipe/20261009-084756-943/input/`.

## What this recording measures

This is the MediaPipe improvement/body tracking test (`mp-improvement-1`), with direct avatar mirroring enabled. The video switches to **BODY MIRRORING**; starting this test explicitly disables punch assistance. Its punch sections therefore exercise raw pose estimation and retargeting, rather than the hybrid boxing detector. There are no boxing observation/commit records in this capture. Zero bag hits here does not establish that the assisted boxing detector missed all the punches.

The test completed with 1,203 inference diagnostics and 1,131 avatar application records, without dropped JSONL records. Each movement has 79–82 accepted/evaluated observations, approximately 10 accepted poses per second. Movement segments contain no rejected/stale/no-person evaluations. Wrist/knee availability is 100%, except for the intentionally hidden left wrist (90%). Visibility is not positional accuracy or movement recognition accuracy.

Camera rate averages 48.9 FPS. Median camera-frame availability to pose is approximately 100 ms, and to the first avatar application approximately 101–102 ms; p95 is about 117–119 ms. These exclude exposure, earlier camera buffering and display latency. Per-region motion-aware filtering reduces stillness RMS from 2.19 to 1.74 cm initially and 1.70 to 1.27 cm at the end. Residual idle motion remains; the recording does not prove that all tracked joints are accurate.

Saved calibration pitch is 28.45° upward. Keep this floor setup fixed for the next comparison. A torso recording is not required.

## Accepted game behavior

After acquiring an observed fighting guard, an ambiguous **single-arm upward stroke counts as a punch**. This prioritizes the fighting game over distinguishing arm lifts. Clearly coordinated bilateral raises/lowers remain excluded. Holding an extended arm, duplicate render/camera frames, missing wrists and isolated landmark jumps must not independently generate strikes. Partial outward movements can still score under the early boxing prior; withdrawal-cue counts are not automatically false positives because the user can continue the preceding punches after a cue changes.

This change does not replace MediaPipe or reduce native camera/inference latency. Its purpose is to improve the decisions and recovery built on observed pose data, while retaining the stable authored fighting stance and physical fist-to-bag scoring.

## Implementation and validation

App 1.13 adds an explicit upward-stroke prior, recovery through two fresh folded guard observations after an isolated tracking jump, and stronger motion checks for low-energy depth/hook candidates. Cancelled preparations retain the trusted guard rather than adopting an extended endpoint as a new guard. Corroborated unfolding can survive a misleading initial image direction from the floor view. Genuine loss or a capture gap still clears the learned guard. A recovered guard frame cannot itself commit a punch.

The interface separates **BOXING TEST** from **BODY TRACKING TEST · SAVE RESULTS**. BOXING TEST forces assistance on and saves raw data plus boxing summaries to `PunchSessions`. The body tracking test continues to disable assistance and saves pose evaluation summaries to `MediaPipeSessions`; its active prompt now states that punch assistance is off.

Automated replay and runtime results are recorded in `Logs/Practice/boxing-priority-validation.txt`, `october-replay-validation.txt`, `mediapipe-boxing-validation.txt` and `punch-play-validation.txt`. Replay results are detector decisions on saved observations, not a new phone accuracy measurement. A fresh floor-camera boxing recording is needed to assess the delivered build.

### Recorded boxing regressions

| Session | Saved 1.12 commits | Revised replay commits | Interpretation |
|---|---:|---:|---|
| October 7, 16:31 | 23 | 25 | Recovers the right stroke after the 56.37 s landmark jump; accepts an ambiguous single upward stroke. The later cancelled preparation's return does not add a strike. |
| October 7, 17:01 | 26 | 23 | Removes the support-hand and settled/returning-hand decisions near 47 s; preserves the reviewed single-arm and alternating strokes across cue boundaries. One partial decision also disappears. |
| October 7, 17:23 | 22 | 22 | Preserves all 12 requested single-arm strokes and eight observed alternating strokes, with zero idle, step-out or clear bilateral-overhead decisions. Two partial motions still score. |

The October 6 floor replay preserves the punch-section decisions. The torso replay preserves the reviewed outgoing strokes while removing the repeated first slow-left return (three saved decisions for two requested strokes). The revised older-session test uses stroke timing and this identified return, rather than requiring every old detector decision to remain.

These totals are not precision/recall scores: setup motions, partial strokes and ambiguous single-arm lifts can contribute to them. The latest October 9 body-test recording cannot supply boxing decision metrics. Unit checks also cover duplicate captures, held strokes, fresh guard recovery, genuine wrist loss, unrecovered jump timeout, cancellation/return/rearming and paired-gesture exclusion. Actual scene tests verify both legacy and boxing arms make physical contact once per test stroke, plus mode ownership, recorder summaries and home/re-entry cleanup.
