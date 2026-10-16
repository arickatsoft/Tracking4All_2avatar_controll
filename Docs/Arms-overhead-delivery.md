# App 1.25 — arms overhead and latency review

APK: `Builds/FitFight-arms-overhead.apk` (version 1.25, code 18). Build: `MobileTrackingBuild.BuildV125Batch`.
Source session: 1.24 Game Body Test, SM-S928B (`Logs/MediaPipe/20261012-H`).

## Arm findings (session H, video-checked)

* Overhead raises (50.0–51.6 s, 53.0–54.5 s) were drawn as forward punches, plus one scored right punch (53.1 s).
  MediaPipe's 3-D under-read the raised arms at 59–73° (1.22 rule needed both ≥ 78°): 7 of 73 overhead frames.
* The false punch: both wrists rose together (12.2 / 9.3 torso lengths/s), but the left arm was still "spent" from the
  previous gesture, so the phase-based two-arm rule did not apply.
* When recognised, overhead arms followed the under-read 3-D direction: bent and leaning forward.

## Changes

* `OverheadArms`: enters on 3-D ≥ 76°, or 3-D ≥ 48° with the camera image showing both wrists ≥ 0.88 torso lengths
  above the shoulders and both arms ≥ 0.93 torso lengths long (forward arms foreshorten to ≤ 0.90). Exits when the 3-D
  angle drops below 35° or a wrist falls below 0.6 torso lengths above its shoulder. Labelled frames (D, G, H): 80% of H
  overhead frames (1.24: 10%), 0 of 5,597 punch/kick/guard/arms-forward frames.
* Two arms rising together (other wrist ≥ 3 torso/s up and ≥ 40% of this one) never commit a punch, whatever the other
  arm's phase. 9 sessions, 219 punch decisions: flags only two-arm cues.
* Recognised overhead arms are drawn straight, ≥ 80° up, keeping the player's sideways spread; tracked arms follow at up
  to 10 m/s (was 6).

## Verification

`V125Validation` (37 checks: units + raw-landmark replays of 9 sessions — overhead only ever active in arm-raise cues;
H both raises recognised), all 1.20–1.24 suites, kick/knee/body/latest/punch scene replays (`Logs/run-v125-all.sh`).

## Latency (session H)

| Stage | Median |
|---|---|
| Camera wait | 9.0 ms |
| Worker handoff | 2.7 ms |
| Inference (MediaPipe Full, CPU) | 43.7 ms (37.7 in F; phone at thermal level 1 from 30 s) |
| Result pickup by game frame | 7.3 ms |
| Capture → pose | 64 ms (p95 83) |

Pose rate ≈ 22/s, bounded by inference time. Display still 60 Hz (Motion smoothness = Standard).
Remaining levers, in order: 120 Hz display (phone setting); MediaPipe GPU delegate A/B (existing toggle); keep the
phone cool. A second CPU inference worker was evaluated and not built: MediaPipe already uses several cores per
inference, so two workers would mostly contend for the same cores.
