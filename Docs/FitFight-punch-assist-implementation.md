# Punch assistance implementation — 27 September 2026

Practice now defaults to a hybrid punch controller. Camera landmarks identify the start and continuation of a punch; authored humanoid animations supply the coordinated stance, torso and punch completion. The previous body mirroring mode remains available from the practice HUD.

## What changed

- Recognize intent from independent accepted 2D observations, before display smoothing and estimated depth. Use torso-relative arm motion, confidence, guard acquisition, reversal cancellation, discontinuity rejection and rearming. Repeated rendering of one observation cannot trigger a punch.
- Give a single animation composer ownership of the final practice pose. The existing direct retargeter, skeleton driver and fallback guard defer to it. Do not disable tracking inference.
- Align the different left/right hook clip contact times, blend preparation/commitment/recovery, and permit a short alternating-hand queue. Brief lost tracking recovers to guard.
- Aim toward one bag target captured at action start, preserve limb lengths, avoid elbow hyperextension, stabilize the elbow bend direction, retain a natural local wrist pose, and plant the feet. The existing footwear grounding runs after composition.
- Gate physical swept-fist contact by the committed action and a strike window. One action produces at most one hit, sound and effect.
- Add an optional 65-second guided landmark/pose recorder. Its bounded background writer exports JSONL locally; recording captures raw observations, timestamps, detector decisions, action IDs, final bone rotations and hits. The REC timer/frame ID supports alignment with a simultaneous phone screen recording.
- Restore pose ownership when toggling modes or leaving practice. Harden practice and camera-settings teardown when Unity destroys scene objects.

## Verification

Unity 6000.6.0f1 compiles the changes. All 51 punch checks pass, including 10/30/60 Hz detection, negative movements, feints, duplicate observations, rearming, contact gates, IK and landmark serialization. The existing RTMPose regression and GPU inference smoke suite passes; its optional reference-person image fixture is unavailable.

The real practice scene also passes mode switching, leaving/reentering, and synthetic left/right observations through the complete animation/contact path: exactly one hit for each punch. Both wrists reach the bag's contact envelope at the intended phase. Play-mode captures were inspected for intact skinning, stance, elbow and wrist behavior.

Commands (run from the project with the Unity Editor executable):

```
-batchmode -projectPath <project> -executeMethod PunchValidation.RunBatch -logFile <log>
-batchmode -projectPath <project> -executeMethod PunchPlayValidation.RunBatch -logFile <log>
```

The latter runs both numerical suites and the actual scene lifecycle/contact tests. See `Logs/Practice/punch-validation.txt`, `Logs/Practice/punch-play-validation.txt`, and `Logs/RTMPose-validation.txt`.

## Phone validation still needed

This implements the hybrid approach; it does not establish reference-video quality or measured movement-to-screen latency. Inference throughput, camera delivery and 2D foreshortening still limit recognition. No mobile build has been installed by this change, and no live human camera experiment was performed.

On the target phone, enter Practice with PUNCH ASSIST on, hold guard briefly, and use RECORD TEST while screen recording. Follow its prompts for 65 seconds. Collect the JSONL from `Application.persistentDataPath/PunchSessions` and the video. This is enough for the first pass at missed/false punch analysis, left/right mapping and onset timing. The logger does not encode or upload video; timestamps describe camera availability/callback delivery, not physical exposure.

This assist mode currently covers punches. Kicks, squats and bending remain part of the previous body mirroring mode. No new trained recognizer or inference-model optimization is claimed.

## Spoken guide update — 28 September 2026

RECORD TEST now speaks fixed English directions through the phone TTS engine. A ten-second setup period/countdown precedes the 65-second exercise sequence, so each recording is now 75 seconds. Visible and spoken cues share one schedule, including separate slow/fast cues. Stop, exit, mode switch and backgrounding cancel queued speech. The Editor logs cues instead of playing native speech. See the practice README for voice requirements and the Android build command.

The expanded numerical suite passes 66 checks, and both Android Java and iOS Objective-C++ bridges compile. These checks do not confirm audible output on the user's phone.
