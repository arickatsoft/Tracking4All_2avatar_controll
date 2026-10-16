# App 1.23 — latency and legs

APK: `Builds/FitFight-latency-and-legs.apk` (version 1.23, code 16). Build: `MobileTrackingBuild.BuildV123Batch`.
Source session: 1.22 Game Body Test on SM-S928B (`Logs/MediaPipe/20261011-F`).

## 1.22 session findings

| Area | 1.21 (D) | 1.22 (F) | Cause |
|---|---|---|---|
| Camera rate | 60 fps | **30 fps** | First 60 fps stream config failed (CAMERA_ERROR -38); the retry policy dropped straight to 30 fps |
| Camera wait (median) | 9.0 ms | 15.4 ms | 30 fps |
| Pose latency median / p95 | 58.4 / 76.7 ms | 61.4 / 83.1 ms | 30 fps (compute was faster: 37.7 vs 40.5 ms) |
| Display | 60 Hz | 60 Hz | 120 Hz is an opt-in A/B toggle, never tested |
| Torso flips | — | 2 episodes (71.9 s, 79.0–79.4 s) | MediaPipe labelled the player as facing away for 14 frames: every left/right landmark swapped; chest swung up to 60° |
| Knee strikes | — | 1 of 4 scored | Drive-and-stick knees return 0.9–1.2 s after the drive; the rule waited only 0.75 s |
| Side-kick squat | 22.5 cm | 41 cm | Sideways lean lowers the hips without knee bend |
| Front kicks | — | L, R, L, R — correct | — |
| Arms overhead / knee lift | — | Correct in video | 1.22 fixes confirmed |

## Changes

* **Camera**: the requested 60 fps is retried on attempts 1–3 (growing delay 350/650/950 ms); only the 4th attempt falls back to 30 fps.
* **Facing swap guard** (`FacingSwapGuard`): learns the normal image order of shoulders and hips; a frame where both pairs are
  reversed is a label swap and all 11 left/right pairs (image, world, confidence) are swapped back before any use. Replays:
  14/14 swapped frames in F, 0 false swaps in D, 2 in Oct 9 (step-back). Torso basis also rejects a >100° shoulder-line flip.
* **Knee strikes**: return window 0.75 → 1.25 s. F: 4/4 knees (L L R R). Knee-lift holds show no return for ≥1.25 s in all sessions → still non-scoring.
* **Squat**: bounded by the supporting leg(s)' knee flexion. Side kick 41 → 31 cm, forward bends 10 → 5 cm, real squat 48 cm unchanged.

## Verification

`V123Validation` (23 checks: units + replays of F, D, Oct 9 ×2), Java host tests (camera retry policy), all 1.20–1.22 suites,
knee-lift/body/latest/punch play-mode scenes (`Logs/run-v123-all.sh`).

## Still open

* 120 Hz display: the one remaining cheap latency lever (~4 ms pickup + faster display); needs a phone A/B for thermals.
* Side kicks from a floor camera: when the body leans far over, MediaPipe's leg skeleton breaks up (first side kick at 148–150 s).

## Follow-up (same 1.23 build)

* **120 Hz**: "Render 120 Hz" was on in both the 1.21 and 1.22 sessions (saved tuning) and the game asked for 120 fps,
  but frames ran at 16.6 ms — the display stayed at 60 Hz. Unity's `Screen.SetResolution(refreshRate)` only works if
  its mode list contains a 120 Hz entry. 1.23 also sets the window's preferred display mode through Android
  (`FitFightPerformance.requestRefresh`) and records `displayStatus` (modes offered, mode requested) and the refresh
  Android actually reports in every reconstruction record.
* **Camera first open**: wait 600 ms (was 250) after the camera reports available before opening; retries keep 60 fps.
