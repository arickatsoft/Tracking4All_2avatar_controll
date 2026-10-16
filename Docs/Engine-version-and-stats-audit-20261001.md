# Engine versions and comparison data — 1 October 2026

MediaPipe Full is the user's visually preferred engine after trying torso-height and floor camera positions, with no dramatic placement difference. This is a user observation, not a numerical accuracy benchmark. No phone recordings or exported benchmark files were supplied for these sanity checks.

## What the tested APK uses

| Component | Installed in the comparison APK | Upstream status checked today |
| --- | --- | --- |
| MediaPipe Unity integration | Plugin 0.14.4; its upstream release uses MediaPipe 0.10.14 | Unity plugin latest is 0.16.3, based on MediaPipe 0.10.22. The separate Google core release latest is v1.0.0; its release number is not the Unity plugin version. |
| Pose Landmarker Lite weights | Existing bundled task model | Entire archive matches Google's current Latest download exactly. |
| Pose Landmarker Full weights | Existing bundled task model | Detector and landmark-model payloads match Google's current Latest download exactly. Archive timestamps differ by two seconds; trained model bytes do not differ. |
| RTMPose weights, both Unity and ncnn | RTMPose-s, Halpe26, 256×192, checkpoint `7f134165_20230605` | This exact small-model checkpoint remains listed in the current official model zoo. Medium/larger variants are also available; the tested APK uses the small variant. |
| ncnn runtime | Pinned 20260526, CPU FP32, two threads | Runtime release is separate from the RTMPose checkpoint. ncnn conversion does not train or improve the model. |

Local plugin manifest: `Assets/Tracking4All/Third Party/MediaPipeUnity/Core/package.json`.
RTMPose provenance: `Assets/BodyTracking/detail.json`.
Model identity evidence: `Docs/EngineAudits/20261001-model-identity.json`.

Sources: [Unity plugin releases](https://github.com/homuler/MediaPipeUnityPlugin/releases), [Google MediaPipe releases](https://github.com/google-ai-edge/mediapipe/releases), [Google's current Pose Landmarker models](https://developers.google.com/edge/mediapipe/solutions/vision/pose_landmarker), [official RTMPose Halpe26 model zoo](https://github.com/open-mmlab/mmpose/blob/main/configs/body_2d_keypoint/rtmpose/body8/rtmpose_body8-halpe26.md).

## Where stats currently exist

- Practice HUD and Tracking panel: live camera, inference and accepted-pose FPS; frame-availability-to-pose delay; backend processing time; leg tracking status.
- **Sanity Check · No Recording** does not save a per-engine summary. It stops existing session/CSV recording when started. Its completed runs cannot be reconstructed from a saved comparison summary because that feature is absent.
- **Tracking → Record benchmark CSV** explicitly saves sampled metrics to `Application.persistentDataPath/tracking-YYYYMMDD-HHMMSS.csv`; the Tracking panel displays the full device path. Start it after starting the sanity check if you want to retain that check's sampled metrics. It is a raw CSV, not an aggregate report, and excludes video/large pose JSONL.
- `Logs/engine-comparison-validation.txt`, `Logs/engine-comparison-play.txt` and `Logs/ncnn-equivalence.json` contain development/Editor checks. Their host timings are not measurements from the user's phone.

Existing timing excludes camera exposure/driver buffering before Unity receives a frame and the display's presentation delay. Confidence values from different models are not directly comparable accuracy scores.

## Proposed next work — not implemented in this audit

1. Add a compact results screen and small summary export for each sanity check: engine/model/runtime version, camera setup, median/p95 delay, actual camera/inference/pose FPS, usable wrist/knee tracking rate, and stillness jitter. Separate calibration from movement measurements. No video or large per-frame recording required.
2. Keep MediaPipe Full as the leading candidate and compare it primarily with Lite and RTMPose ncnn using the same short moves and camera setup. Include simple user movement-following ratings for arms, torso and legs; statistics alone cannot establish pose accuracy without a reference.
3. Upgrade the Unity MediaPipe plugin in isolation, preserving model weights and settings. Verify compatibility with existing body/face integrations and repeat the comparison so runtime effects are identifiable.
4. If RTMPose remains competitive, test RTMPose-m at the same 256×192 resolution as an accuracy/processing tradeoff. Its current avatar limb depth is reconstructed from 2D; MediaPipe supplies learned 3D world estimates. Better 2D weights alone do not remove that reconstruction difference.

For now, keep torso/waist-height placement for convenience and full-body framing since the user saw no meaningful difference. This does not establish that the positions are equivalent for all movements. Do not combine body parts from multiple engines before measuring which remaining errors arise in detection, depth reconstruction, filtering or avatar retargeting.
