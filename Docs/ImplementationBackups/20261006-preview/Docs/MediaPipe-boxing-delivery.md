# MediaPipe boxing delivery — app 1.11

APK: `Builds/FitFight-mediapipe-boxing.apk`, package `com.DefaultCompany.Tracking4AllUnity`, Android version code **4**, minimum API **26**.

SHA-256: `e0da808f8efaa222fbb5a047801e6be8c382d9493f7956c52fee625862290f07`

Build succeeded with **0 errors**, **748 warnings**, including existing Unity/Sentis shader variant warnings. APK v2 signature verification passed. Manifest package and app version were checked. No new physical-phone result is claimed.

## Delivered behavior

- MediaPipe boxing guard reference, separate from standing calibration.
- Image/world evidence and early commitment, with a visible-wrist path for brief elbow occlusion.
- Independent arm return states, with same-arm and alternate-arm follow-ups handled by the animation queue.
- Stable guard/feet and tracked bounded torso/head offsets; smooth fist trajectory toward the bag prevents copying erroneous upward extensions.
- Swept contact seeded before fast pose composition; physical contact, speed/teleport protections and one-hit-per-action remain required.
- `RECORD TEST` enables assistance explicitly and saves JSONL plus JSON/Markdown summaries to `PunchSessions`.

## Verification

- 24 boxing checks passed, including real recorded replay and summary persistence.
- 166 legacy punch assertions, 11 idle filter checks and 18 MediaPipe improvement checks passed.
- 54 engine checks passed; full-body and responsive movement regressions passed.
- Actual scene: both legacy arms and both new boxing arms score exactly one physical contact per observed stroke; gameplay recording selects assistance and persists its summary; mirroring ownership and home cleanup pass.

One initial scene run exposed an accelerated authored right-arm windup crossing the existing contact speed limit. The final guided wrist path passed both arms without raising that limit.

## Phone next step

Follow `Docs/MediaPipe-boxing-test-plan.md`: MediaPipe Full, region filtering on, stable body motion on, direct avatar on, 60 FPS. Keep camera placement for the first run, calibrate standing, then **RECORD TEST** and hold a fighting guard through countdown. Send matching `PunchSessions` files and video. Try waist/lower-chest camera height with a near-horizontal optical axis only in a separate comparison run.

Native MediaPipe model/runtime and approximately 100 ms processing delay are unchanged. Replay increased fast-punch commits from 13 to 16; this is sensitivity evidence, not an accuracy score. Ambiguous single upward extensions from learned guard intentionally favor punches; bilateral overhead positions remain excluded. Actual miss/extra-hit rates require the next labelled phone run.
