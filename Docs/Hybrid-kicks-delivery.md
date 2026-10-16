# App 1.24 — hybrid kicks

APK: `Builds/FitFight-hybrid-kicks.apk` (version 1.24, code 17). Build: `MobileTrackingBuild.BuildV124Batch`.
Source session: 1.23 Game Body Test, SM-S928B (`Logs/MediaPipe/20261011-G`).

## 1.23 session

* Camera: 60 fps on the first attempt (1.23 retry/delay fix confirmed). Pose median/p95 60.8/78.7 ms, decision 59.1/76.1 ms.
* Display: Android offers this phone only 60/48/30/24/10 Hz modes (no 120 Hz) → Samsung *Motion smoothness* is on Standard.
  Before the request the window ran at 30 Hz. Setting it to Adaptive is required for 120 Hz.
* Punches: fast punches 6/6 visible strokes detected; procedural arms reach 15-26 m/s (no lag source).
* Kicks: the kick is confirmed when the leg is nearly straight; the avatar then replayed its own chamber before extending.
  Before confirmation the tracked foot was speed-capped at 4 m/s and held at the bag clearance (~250 ms freeze on one kick).
  Some kicks (e.g. 141.7 s) are snap kicks: chamber held ~0.4 s, then 131° → 49° knee within one camera frame.

## Hybrid approach

Tracking supplies timing, the procedural kick supplies shape, confirmation supplies scoring:

1. `GameBodyMotion.ExtensionProgress` (0..1): unfolding of a chambered leg from observed knee extension, only while the
   foot stays high and moves away faster than 1.5 m/s (held-knee wobble ≤ 0.6 m/s; kicks 3-10 m/s). Never scores.
2. While unfolding, the avatar's foot follows the professional kick path (chamber → bag target) in step with that progress,
   stopping just outside the bag until the kick is confirmed.
3. A confirmed kick continues from the extension already shown or observed (phase skip, capped so ≥0.15 s of the contact
   window remains) — no re-chamber.
4. A lifted foot follows at up to 10 m/s (was 4 m/s).

## Result (scene replay of session G vs the phone's own 1.23 rendering)

| Kick | 1.23 avatar at bag | 1.24 | Earlier |
|---|---|---|---|
| L front 135.6 | 136.066 | 135.937 | 129 ms |
| L front 137.5 | 137.925 | 137.604 | 321 ms |
| R front 141.7 (snap kick) | 142.374 | 142.237 | 137 ms |
| L front 149.8 | 150.085 | 150.087 | — (its high-chamber frame was rejected as a tracking jump, so it rises through the chamber first) |
| R side 153.6 / knees | — | — | unchanged |

All 8 leg strikes still hit the bag; every foot kick lands 0.38–0.50 m above the bag bottom (rounded edge 0.20 m).
The head start is only used when the player's foot was observed at chamber height (≥55% of leg length): skipping a low
chamber swept the synthetic test kick into the bag's lower edge. Knee holds and idle/body cues never animate as kicks in
sessions G, F, D, Oct 9.

## Not changed

* Knee strikes still score on the return (drive-and-stick); the avatar's knee is shown live, only the hit registers later.
* Detection latency (~60 ms capture → decision) is the pose pipeline; the remaining lever is 120 Hz display (phone setting).
