# App 1.22 — arms overhead and knee lifts

APK: `Builds/FitFight-arms-and-knees.apk` (version 1.22, code 15). Build: `MobileTrackingBuild.BuildV122Batch`.

## Findings from the 1.21 sessions (D = full body, E = punches)

* LEFT FRONT KICKS showed 3 front kicks although one left kick was performed. Replay: two were the
  late right "knee lift + straighten" strokes carried over from the knee-strike cue (REC 131.4 / 133.4,
  visible in the video), one was the real left kick (136.7). Detector behaved correctly.
* Knee lift rendered with the shin slanting inward. Two causes in the held-lift leg:
  1. The lifted foot stayed on the avatar's wide stance line (0.96 m) while the player's stance is ~0.28 m.
  2. When the foot came up near hip height, the IK knee direction became ambiguous and the knee flipped
     outward (up to 0.85 m sideways from the foot) — a ">" shaped leg.
* Hands fully overhead were drawn as forward punches. In the 2-D image a forward arm and an overhead arm
  project to the same place from a floor camera, but MediaPipe's 3-D world landmarks separate them:
  forward arms measure ≤ 75–76° elevation, held overhead arms 78–86°, and no punch, kick or guard in
  any session reached 78° on both arms.

## Changes

* `OverheadArms` (new): both arms ≥ 78° elevation and ≥ 0.85 straightness for 2 frames → overhead
  (exit < 62°). When active the avatar shows the arms overhead and punches are blocked.
* Two-arm gesture rule: a commit while the other hand is also rising (extension ≥ 0.30 and preparing,
  committing or committed ≤ 120 ms ago) is a gesture, not a punch.
* Held knee lift: a clearly lifted leg (≥ ~25 cm) is rebuilt from the player's observed thigh and shin
  directions (sideways damped to 25 %) with the avatar's bone lengths, starting at the avatar's hip; the
  knee always bends forward. Knee-lift-then-straighten is reproduced as performed. Low lifts keep the
  stance-relative foot (`LiftedFootOffset`, player's own stance).

## Verification

* `V122Validation` — units (overhead detector, two-arm rule, lifted leg rebuild), punch replays A–E,
  overhead replay from recorded world landmarks.
* `GameBodyPlayValidation.RunKneeLiftReplayBatch` — full scene replay of session D knee-lift holds:
  rendered shin sideways lean 0.85 m (1.21) → 0.05 m; captures `Logs/Practice/knee-lift-*-1.22.png`.
* Full 1.21/1.20 suites, body/latest/punch play-mode scenes (`Logs/run-v122-all.sh`).

## Camera angle

Tilting the phone does not help: the view geometry depends on where the camera is, not where it points.
Raising the camera (e.g. ~chest height on a shelf, still seeing feet) would separate forward from
overhead in the image as well, but it is not required — the 3-D rule handles held overhead arms.
