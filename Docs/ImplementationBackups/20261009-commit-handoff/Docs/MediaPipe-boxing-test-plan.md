# MediaPipe boxing test — app 1.13

Install `Builds/FitFight-punch-priority.apk`. Use your **floor camera position**, keeping distance, tilt and lighting fixed. No torso comparison or full-body recording is needed for this iteration.

## Settings and calibration

- MediaPipe Full.
- MediaPipe per-region filtering ON; stable body motion / floor camera correction ON.
- Direct avatar OFF; camera 60 FPS.
- In Practice, PUNCH ASSIST ON. BOXING TEST also enables it automatically.

Tap **CALIBRATE** and stand upright with relaxed arms, head and feet visible. After calibration, take a fighting stance: knees slightly bent, fists near your chin. Standing calibration and learning the fighting guard are separate stages.

## One 75-second boxing recording

Tap **BOXING TEST**, hold your fighting guard through the countdown, then follow the spoken instructions: two slow left punches, four fast left, two slow right, four fast right, six alternating punches, partial withdrawals, paired overhead motion and step-out/re-entry. Return each fist to guard. Keep shoulders, elbows and fists visible throughout extension. Record the screen if possible.

Observe misses, extra hits, wrong-side punches, support-hand motion, idle stability and visible delay. A **single upward arm stroke from guard may intentionally count as a punch**; distinguishing that from an arm lift is not a success criterion for this game. Clear paired overhead movement should remain excluded. Partial outward strokes may score under the early punch policy. A held extension or return should not produce an additional hit.

The raw JSONL and matching `.summary.json` and `.summary.md` are in **PunchSessions**. Pull them with:

```sh
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/PunchSessions/ ./
```

Send the three files with the same timestamp plus the matching screen video. Summaries separate detector commits from physical bag hits; cue counts are not accuracy labels. Speech completion and the user's movement can cross a cue boundary.

## Choosing the correct test

**BOXING TEST** records the hybrid punching-bag behavior and forces punch assistance ON. **BODY TRACKING TEST · SAVE RESULTS** is the separate pose/mirroring diagnostic, disables assistance, and saves to MediaPipeSessions. **FULL BODY TEST** is also a mirroring diagnostic. Use BOXING TEST for the next comparison.

App 1.13 changes punch decisions and tracking-jump recovery. Native MediaPipe computation and the roughly 100 ms frame-availability-to-pose pipeline are unchanged. Replays and Editor tests do not establish performance on the phone; this recording will measure the delivered behavior.
