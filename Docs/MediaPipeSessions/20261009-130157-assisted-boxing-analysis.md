# October 9 assisted boxing — app 1.13

Session `20261009-130157-756`, matching screen video archived with the three original session files in `Logs/MediaPipe/20261009-130157-756/input/`. Metrics, event CSV/JSON and cropped review sheets are in the parent directory. This recording uses MediaPipe Full CPU, punch assistance ON, per-region motion-aware filtering ON, stable correction ON, direct avatar OFF and requested camera rate 60. Saved floor pitch is **24.46° upward**.

## Main result

The detector commits **24 times**, and **23 physical bag hits** are recorded. Summing the summary's per-cue commits gives 24; its headline 23 is bag hits, not detector commits. The main sequence contains **19 visible outgoing strokes**: two slow left, five fast left, two slow right, four fast right and six alternating. Each has a matching-side detector decision. One fast-right decision never becomes an avatar strike, because of an application handoff bug described below. This is a manual review of the small screen preview, not a precision/recall benchmark or proof that arbitrary punches are always detected.

| Section | Visible main strokes | Detector decisions L/R | Bag hits |
|---|---:|---:|---:|
| Slow left | 2 | 2/0 | 2 |
| Fast left | 5 | 5/0 | 5 |
| Slow right | 2 | 0/2 | 2 |
| Fast right | 4 | 1/4 | 4: three right, one support-hand left |
| Alternating | 6 | 3/3 | 6 |
| Partial/withdrawal motions | 4 partial strokes | 2/2 | 4 |
| Setup, idle, bilateral overhead, step-out | — | 0/0 | 0 |

The fast-left five are five distinct visible extension/return cycles, rather than a duplicate decision for four strokes. All six alternating strokes occur within that cue in this run. The four withdrawal-cue hits correspond to actual smaller outward movements; early acceptance of partial motions remains a tradeoff of the user's requested boxing prior.

## Missed fast-right hit: detected but never animated

The third fast-right stroke commits at **REC 48.119 s**, from camera observation **48.018 s**, with observed speed 5.474 torso units/s. At **48.019 s**, the other hand had already entered a tentative left preparation. The avatar remains `Preparing`, side left, action 15 after the right decision; it does not enter a right `Striking` phase. The left candidate cancels at **48.219 s**, the avatar recovers, and it reaches guard around **48.387 s**, without a hit from that right decision.

The controller's existing `Handle` code queues a committed opposite-hand punch behind an uncommitted preparation. It only consumes the queue in guard or later in an active strike, and queued commits expire after 180 ms. Waiting through preparation and recovery exceeds that window. The JSONL does not record the private queue directly; its decision/pose timeline and the code identify the mechanism, and an actual-scene regression reproduces it.

App 1.14 makes a committed opposite-hand stroke immediately take ownership from tentative preparation. Queueing behind an already active strike remains intact. The new scene test starts a tentative left preparation, then submits a decisive right stroke while the left candidate is still pending. Before the fix it reports `started=False, hits=0`; after the fix the right strike starts within the test's 80 ms bound and makes one physical contact. Other scene/recording tests pass. This is Editor verification of the code fix, not a new phone result.

## Remaining support-hand extra decision

A left preparation at **REC 48.519 s** confirms at **48.618 s** and scores at **48.690 s** during the fast-right section. The camera preview shows the support arm dropping and moving back near guard, without a separate full left punch. Treat this as a support-hand/return false positive to improve, with the qualification that the preview is small and the native arm estimates can be inaccurate. It is separate from the lost committed-right handoff. App 1.14 leaves detector thresholds and the single-arm lift prior unchanged; it does not claim this extra decision is fixed.

## Preview, availability and latency

The 75-second recording has **4,503 final poses**, **4,504 preview refreshes**, zero runtime errors and zero dropped JSONL records. Final-pose rate is **60.07 FPS**, with a maximum pose-record gap of **43.5 ms**. The preview and REC clock continue through the run. There are 757 completed inferences: 749 accepted and eight no-person results. During the guided punch sections both wrists meet the visibility criterion on all observations. The longest observation gap, about 799 ms, occurs during loss/re-entry; visibility still does not establish positional accuracy.

| Measurement | Median |
|---|---:|
| Camera FPS | 52.46 |
| Accepted arm observation rate | 9.99 FPS over the full recording |
| Native computation | 40.63 ms |
| Scheduling to GPU readback | 48.50 ms |
| Readback to consumption | 50.81 ms |
| Complete inference age | 100.10 ms |
| Committed decision delay (summary median/p95) | 100.91 / 115.34 ms |
| Camera observation to scored hit, 23 matched hits | about 153 ms |
| Decision to scored hit, 23 matched hits | about 53 ms |

These timings exclude exposure, buffering before Unity camera-frame availability and display latency. Hit times have rendered-frame precision. There is no material evidence of a native pipeline speedup; the approximately 48 ms readback wait remains. The handoff fix prevents a recognized punch from expiring and does not speed up MediaPipe inference.

## Next validation

Keep the same floor placement and settings. Test app 1.14 with punch assistance ON and BOXING TEST. Watch fast-right and alternating strokes, particularly a punch thrown while the other hand makes a small guard adjustment. Check that committed strokes become avatar punches, then evaluate the remaining wrong-hand decision separately. No torso comparison is required. Pull the three matching files from PunchSessions with the same command as before and pair them with a screen video.

Video timing: at video 10.0 s the displayed REC clock is approximately 11.9 s. Use the onscreen clock and JSONL as the reference; contact-sheet FPS timestamps can differ by about half a sampling interval. Cue timestamps are speech enqueue times, not audible completion.
