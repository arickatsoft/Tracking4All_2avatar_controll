# MediaPipe punching bag test — app 1.12

Build: `Builds/FitFight-preview-boxing-fix.apk`. Use **MediaPipe Full**, per-region filtering **ON**, stable body motion **ON**, direct avatar **ON**, camera **60 FPS**. Keep the previous filter values. Punch assistance owns the final avatar pose while enabled.

## Preview check first

Install app 1.12, enter Practice and calibrate. Use the **floor camera position**, which is the target setup. Move both hands for 15–20 seconds and confirm the LIVE CAMERA panel keeps updating. Then tap **RECORD TEST** and verify the REC seconds/frame readout keeps advancing. The October 7 floor recording verified continuous preview/pose recording; keep that placement for subsequent software comparisons.

The new summary includes `finalPoseFrames`, `previewFrames`, `lastPreviewCameraSequence`, `runtimeErrors` and `lastRuntimeError`. `previewFrames` counts refreshes, not unique exposures. If it freezes again, stop recording and send the matching three files plus video; capture `adb logcat -d -v threadtime > fitfight-logcat.txt` promptly for the original exception. App 1.12 records each completed model inference even when contact scoring fails.

## First run: gameplay

1. Use floor height and keep placement fixed through the recording. Keep shoulders, elbows and fists visible during extension; the October 7 run used approximately 24° upward camera pitch.
2. Enter Practice, tap **CALIBRATE**, and stand upright with arms relaxed and head/feet visible until calibration completes. Standing calibration and learning the fighting guard are separate stages.
3. Tap **RECORD TEST**. This explicitly enables **PUNCH ASSIST: ON**. Keep knees slightly bent and fists near your chin; hold this guard through the countdown. Keep shoulders, hips, elbows and wrists visible. Feet are not needed by the punch detector after standing calibration.
4. Follow the spoken counts: two slow left, four fast left, two slow right, four fast right, then six alternating punches. Return each fist to guard. Follow withdrawal, overhead and step-out checks too.
5. Record the screen if possible. Note actual punches thrown, misses, extra hits, wrong side and visible delay.

Matching `.jsonl`, `.summary.json` and `.summary.md` files are saved to **PunchSessions**. The summary separates detector commits from bag hits and reports capture-to-decision delay. Counts are decisions, not labelled accuracy; a committed punch may miss a moving bag.

Your existing pull command is sufficient:

```sh
adb pull /sdcard/Android/data/com.DefaultCompany.Tracking4AllUnity/files/PunchSessions/ ./
```

Send the three matching files and video. **MEDIAPIPE TEST · SAVE RESULTS** remains the full-body mirroring diagnostic, disables assistance, and saves to **MediaPipeSessions**. Use **RECORD TEST** for this iteration. No new checkbox is needed.

## Optional camera comparison

Floor placement is the required target. A torso comparison is optional only if further evidence suggests a substantial benefit. If needed, try waist/lower-chest height with the optical axis **0–15 degrees upward from horizontal**. At zero, the phone screen is roughly vertical (90 degrees to the floor); at 15 degrees upward, it is roughly 75 degrees to the floor. Keep distance, light and tracking settings comparable, both fists/elbows visible during extension, and enough body in frame for standing calibration.

The earlier October 2 run reported about **49.8 degrees upward** from gravity, while the saved pitch was clamped at 45. The October 6 floor and torso runs report about **25.4°** and **1.1°** respectively; their frozen preview prevents a reliable accuracy ranking. A flatter view may distinguish forward extension from arm raising better. This is an engineering hypothesis, not a measured optimum. Start facing the camera; a large side view can hide the far arm.

## What to evaluate

- Guard/idle: no spontaneous commits or hits; stable support arm and feet.
- Outgoing punches: correct side, one decision per stroke, no hit on return/held extension.
- Fast/alternating punches: few misses, no waiting for the other hand to finish its animation.
- Brief hidden elbow: visible wrist evidence can support extension. An absent wrist or stale gap cannot invent a strike.
- Clear bilateral overhead: no hit. A single upward extension from learned guard intentionally favors a punch when the view is ambiguous.

Label actual punch onset, maximum extension, return and mistakes from selected video frames. Cue timestamps are speech enqueue times, not audible onset. Repeat a fresh session before further threshold tuning. This changes the application/animation policy; it does not retrain MediaPipe or establish true 3D accuracy.

The October 6 runs have approximately **10 arm observations/second** and **100 ms capture-to-decision delay**. Earlier commitment and faster guided animation remove additional waiting; they cannot recover a punch entirely between observations. After gameplay validation, optimize GPU readback/capture and inference cadence separately while keeping idle filtering fixed.
