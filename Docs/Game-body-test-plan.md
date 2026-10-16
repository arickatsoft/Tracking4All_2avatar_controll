# One-run full-body game test — app 1.19

Install `Builds/FitFight-frame-driven.apk` as an update. Keep the phone in the intended **floor-level, front-camera** position. Aim it so both feet, your head, sideways hands and overhead hands remain visible. Use the same placement throughout this run.

Keep model input at **512** for this baseline; leave the optional ten-second input archive off during the timed run. The preview refreshes at up to 15 FPS while pose inference runs independently. New pipeline timings are described in `Docs/External-recommendations-adaptation.md`.

After installation, fully close the app and open it again once. Check that the front-camera preview is live before starting; include that startup in the screen recording if convenient.

Open practice and tap **GAME BODY TEST · 2:44**. No separate calibration or diagnostic run is needed: the spoken sequence includes standing calibration. The button selects MediaPipe Full, native Android CPU camera capture, CPU inference, adaptive per-region filtering, stable body motion, 60 requested camera FPS and body assistance. Direct avatar is temporarily off. Prior assistance/direct selection is restored when the recording ends. Requested FPS is not a guarantee of camera or inference FPS.

Use phone media volume for speech. Record the screen with the phone's media audio enabled so the instructions and REC clock are included. Follow the voice, return to guard when asked, and use controlled low kicks. Keep recording until the completion announcement. The routine lasts **164 seconds**, with up to **16 extra seconds** for standing calibration. If calibration still cannot complete, the test stops with an error summary rather than extending indefinitely.

## Spoken sequence

| Test clock | Requested movement |
|---|---|
| 0–16 s | Step back, countdown, stand upright with arms down for calibration |
| 16–22 s | Quiet fighting guard |
| 22–30 s | Two slow left punches, two slow right punches |
| 30–40 s | Six quick alternating punches |
| 40–48 s | Both arms forward; bend and straighten elbows |
| 48–56 s | Both arms overhead, then lower |
| 56–64 s | Arms to the sides; bend and straighten elbows |
| 64–74 s | Head turns and two nods |
| 74–84 s | Two comfortable forward torso bends |
| 84–96 s | Side bends and shoulder twists |
| 96–108 s | Three comfortable squats with a brief hold |
| 108–114 s | Slow left knee lift, hold and lower — **no strike intended** |
| 114–120 s | Slow right knee lift, hold and lower — **no strike intended** |
| 120–130 s | Two left knee strikes, two right knee strikes |
| 130–138 s | Two controlled left front kicks |
| 138–146 s | Two controlled right front kicks |
| 146–156 s | One low left side kick, one right side kick |
| 156–164 s | Quiet guard and recovery |

Calibration pauses shift later timestamps. The recorded guide clock and cue records account for that shift.

## What to look for

- Avatar begins responding promptly and returns smoothly after an action.
- Supporting foot stays grounded; the lifted leg can move independently.
- Squats lower the pelvis; torso/head movement and bilateral arm gestures remain visible.
- Held knee lifts and quiet guard do not score. Strikes score only on physical bag contact, at most once per action.
- Note missed attacks, wrong-side attacks, unintended hits, idle shaking, foot sliding and preview freezes. You do not need to repeat the run just to investigate a single problem.

Ambiguous single-arm upward strokes retain the accepted boxing priority. Clear bilateral overhead, side and forward extensions are excluded from punching. A kick or knee detector decision can still produce no hit if the avatar cannot physically reach the moving bag; the log records both stages separately. Side kicks are a first procedural implementation requiring phone validation.

App 1.18 retains the damped torso/head/squat/limb targets and blended arm-to-guard handovers. Confirmed foot attacks add a modest forward weight transfer and a captured, bounded lead toward the moving bag; physical contact is still required. Guard pelvis height is anchored before applying observed squat and balance, while active punch clips retain their authored vertical motion. Confirmed punches retain their fast animation path. Invalid leg samples briefly hold the last reliable display target while blocking impact; sustained loss cancels. A held lift remains provisional, and knee strike confirmation waits for a short bent-leg return. The knee cue now explicitly asks you to keep the lower leg folded. A directional label may be generic when front versus side is unclear. During knee strikes, keep the foot below the knee; during front kicks, extend the lower leg. Comfortable technique is sufficient.

You do not need expert kick technique or exactly the requested number of punches. Use comfortable controlled movements; extra strokes and movement variations are allowed and will be reviewed against the video.

## Files to send

This **game test saves to PunchSessions**, alongside the previous assisted boxing recordings:

```sh
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/PunchSessions/ ./
```

Send the newest matching `.jsonl`, `.summary.json`, `.summary.md`, and the screen recording. Filenames use UTC; identify the run by its matching stem and app version **1.18**, rather than local clock alone. In the app, open **Tracking → Show game body summary** to inspect the last result.

The JSONL includes model observations, exact accepted avatar body snapshots, camera/delegate diagnostics and lifecycle traces, spoken cue enqueue timestamps, leg preparation/commit/cancel events with reasons, final bones, pelvis squat offset, leg phase, action IDs, leg verification/display-source sequence, and six contact volumes (two fists, feet and knees). The summaries separate requested movement windows, observed availability, action decisions and physical hits, and count evaluated native versus legacy camera frames. Cues are weak labels, not verified motion accuracy or training ground truth.

The expected capture path is `camera2-cpu`. Startup waits for release and makes a delayed open probe if availability notifications are stale. It retries up to three times, trying 30 FPS on retries. If native capture still fails or stalls, the app falls back to `unity-gpu-readback`, displays **CAMERA FALLBACK ACTIVE**, and records the reason. A new game test explicitly retries native capture. If fallback appears, finish the same run and send its files; a second run is unnecessary. Do not toggle GPU inference during this run. The latest app 1.17 phone run measured about 72 ms median pose age on native capture, compared with about 100 ms in earlier legacy-capture runs. App 1.18 avoids opening the baseline camera between inference-model switches and combines capture-mode/FPS changes into one reopen. Kick reach and cold startup still need this phone validation; no new end-to-end latency measurement is claimed.
