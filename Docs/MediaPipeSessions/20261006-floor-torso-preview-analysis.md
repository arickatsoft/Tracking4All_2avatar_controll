# October 6 boxing recordings — preview stall and punch decisions

Video filenames identify camera placement. Screen hit counters establish the session pairing:

| View | Session | Video | Screen hits | New hits |
|---|---|---|---:|---:|
| Floor | 20261006-044622-113 | bottom.mp4 | 0 → 25 | 25 |
| Torso | 20261006-044819-059 | torso.mp4 | 25 → 47 | 22 |

The second recording retains the first run's cumulative hit counter. Original JSONL and summaries are archived under the matching `Logs/MediaPipe/<session>/input/` directories, with the paired video and a contact sheet in each session directory.

## What the data establishes

Both runs used app 1.11, MediaPipe Full CPU, motion-aware per-region filtering and punch assistance. Direct avatar was OFF. Punch assistance owns the final pose, so that checkbox alone does not explain the observed stall.

| Measurement | Floor | Torso |
|---|---:|---:|
| Arm observations | 745 | 754 |
| Recorded final poses | 160 | 104 |
| Reconstruction diagnostics | 12 | 13 |
| Capture availability → commit median | 100.6 ms | 100.8 ms |
| Capture availability → commit p95 | 112.0 ms | 119.8 ms |
| Commits in both idle cues | 0 | 0 |
| Saved gravity camera pitch | 25.44° | 1.12° |

Arm observations continue at roughly ten updates per second, while final-pose records appear only in short bursts separated by long gaps. The videos show the preview and REC time/frame readout freezing together even as the gameplay scene and hit counter continue. This is evidence of a stalled practice update path rather than a complete loss of camera/model input. The record writers report no dropped records.

The precise Android exception is not captured by app 1.11. A native MediaPipe Full editor run with direct avatar OFF did not reproduce the phone problem. No connected phone was available for logcat. The following update removes coupling between those paths and adds diagnostics; resolving the Android symptom still needs a phone check.

Higher bag-hit totals do not establish better camera accuracy. Some commits fall in withdrawal and raise/lower sections, and a few strokes cross cue transitions. Cue timestamps record speech enqueue, not audible onset. The frozen camera panel prevents reliable frame-by-frame movement labels for these intervals.

## Changes in app 1.12

- Update the camera preview and recording HUD in `Update`, independently of contact sampling and final-pose recording in `LateUpdate`.
- Refresh the preview from the latest camera texture each rendered frame. The preview no longer depends on the `didUpdateThisFrame` flag seen by a later component; capture/inference scheduling is unchanged.
- Skip unused leg-confidence reads in assisted boxing. Catch and report contact, pose-recording and preview failures separately, with throttled console errors.
- Record reconstruction diagnostics on the provider's completed-inference event, independently of final-avatar recording. Remove the subscription when recording ends or initialization fails.
- Add final-pose/preview counts, last preview capture sequence, runtime-error count and the last error with its stack to `.summary.json`/`.summary.md`.
- Detect shared vertical wrist motion at the onset of a two-hand raise or lowering, before both wrists reach the final overhead posture. A single upward-projected punch still uses the boxing prior.

The stateful gate uses observed frames, never cue labels. It retains all recorded decisions in the designated punching sections:

| Cue | Floor old → revised L/R | Torso old → revised L/R |
|---|---|---|
| Slow left | 2/0 → 2/0 | 3/0 → 3/0 |
| Fast left | 4/0 → 4/0 | 4/0 → 4/0 |
| Slow right | 0/2 → 0/2 | 0/2 → 0/2 |
| Fast right | 0/5 → 0/5 | 0/4 → 0/4 |
| Alternating | 2/3 → 2/3 | 2/3 → 2/3 |
| Start then withdraw | 2/3 → 2/3 | 2/0 → 2/0 |
| Raise/lower both hands | 2/0 → 1/0 | 2/1 → 0/0 |

The remaining floor raise-section decision is about 0.23 seconds into its cue, near the transition from withdrawal. No raise-section decision remains after the first second in either replay. This is a reduction in ambiguous gesture decisions on supplied inputs, not a measured false-positive rate. Withdrawal behavior is deliberately unchanged until the visible input can be labelled; adding a full-extension wait would trade early response for cancellation certainty.

## Validation and next measurement

`MediaPipeOctoberReplayValidation` reproduces app 1.11's saved cue decisions exactly, verifies the revised results, and checks early paired-motion gating, duplicate-frame handling, settled-guard release and single-hand upward motion. `Logs/Practice/october-replay-validation.txt` contains the results.

`MediaPipePracticeFrameValidation` runs the real scene with native MediaPipe Full and direct avatar OFF. It recorded 40 observations, 602 final poses and 50 inference diagnostics across normal operation and an injected contact-collider failure. The timer continued; the summary captured the contact error. This establishes isolation under a controlled failure, without claiming to reproduce the original Android exception. See `Logs/Practice/mediapipe-practice-frames.txt`.

First verify the new phone preview stays live before recording another full test. Then make one torso-height RECORD TEST with matched screen video and three session files. Confirm the REC timer continuously advances, runtimeErrors is zero, and the preview moves through punches and two-hand raises. If a freeze remains, pull Android logcat alongside the session files. Compare camera placements again only after that capture path is working reliably.

Native model/runtime and the approximately 100 ms capture-to-decision processing delay are unchanged. Capture/readback/inference cadence remains the next latency work item after this phone verification.
