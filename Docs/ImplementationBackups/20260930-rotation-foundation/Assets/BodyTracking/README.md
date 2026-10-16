# RTMPose alternative tracking experiment

Open the existing game scene and press Play. On the home screen, choose **Tracking4All** or **RTMPose**, then choose Adam or Remy. Tracking4All is the default; the choice is saved between launches. No scene or prefab migration is required. The same selector is attached to an existing serialized selection UI if one is assigned.

During gameplay, open **Tracking** (upper-right in practice). Switch engines, view the live camera and 26 confidence-gated landmarks, adjust height/camera tilt, recalibrate, record benchmark CSVs, or return home. Stand normally with your entire body in frame for the first 20 accepted calibration frames in a cadence-adjusted window. After changing height, press Recalibrate. Camera rotation changes reset calibration automatically. Preview coordinates are upright and unmirrored; the avatar keeps the existing mirror settings.

## Included

- Official RTMPose-s Halpe26 ONNX model, bundled for offline use. Input: RGB NCHW `[1,3,256,192]`. Outputs: `simcc_x [1,26,384]`, `simcc_y [1,26,512]`.
- Unity AI Inference/Sentis 2.6.1, GPU compute preprocessing/inference, nonblocking output readback. Requires a compute-capable graphics device. No inference service, Python process, cloud, or extra camera connection.
- Existing local camera backend reused. The selected MediaPipe inference component stops while RTMPose runs. Switching back restores its enabled state. Inference/setup errors restore Tracking4All and display an error in the test controls.
- `IBodyPoseProvider`, `Tracking4AllPoseProvider`, and `RTMPosePoseProvider`. The existing PoseSolution adapters retain their lifecycle identity across switches so avatar, landmark visualizers, and network subscribers keep working. The UDP opponent provider is not switched.
- GPU crop/resize and RGB normalization; camera rotation and vertical inversion compensation. A padded full image acquires the person, landmark bounds update the crop, and full-frame inference reacquires every second or after loss. This initial version assumes one person and does not include a separate person detector.
- Experimental pelvis-relative 2.5D reconstruction with scale calibration, height-based limb length priors, manual or gravity-estimated camera pitch compensation, adaptive One Euro filtering, and capped velocity prediction for short joint occlusions. Low-confidence whole-body observations expire. Halpe heels and big toes map to the existing avatar rig; fingers use derived hand direction proxies.
- Existing coordinate modifiers and humanoid retargeting are reused. RTMPose bypasses the scene's SimpleSmooth/EuroSmooth stage to avoid applying two temporal filters.
- Only one inference is in flight. New frames are skipped while busy; results older than 200 ms are discarded.

## Responsiveness controls

RTMPose defaults request 60 camera FPS (actual FPS is measured), use adaptive filtering with 2 Hz minimum cutoff / 8 motion gain / 12 Hz velocity cutoff, 0.30 joint confidence, zero leg deadband, 15 ms additional bounded prediction, and an 80 ms occlusion grace. Prediction is capped at 40 ms / 12 cm and cancels the previous trajectory at a stop or reversal. It never completes an entire punch or kick. Depth response is time-based, with a 6 m/s base limit and projected-motion gain. Hip, knee and ankle confidence determine leg reliability independently of heel/toe visibility; a missing toe cannot lock an otherwise visible leg. The legacy LegGroundLock is suspended for the local RTMPose avatar and restored on switching back.

**Tracking controls → Direct avatar** is an experimental A/B option that rotates the humanoid bones from RTMPose landmarks, bypassing the intermediate puppet and its skeleton update. Switching it off restores the existing retargeter. It preserves imported bone lengths but does not solve monocular depth ambiguity or automatically improve inference time. Use **Save responsiveness settings** to persist these controls.

Readback copies exactly the logical tensor sizes inside GPU completion callbacks. This avoids both pooled-buffer padding and accessing Unity request data after it expires. Update and LateUpdate can consume completed inference; render-frame prediction reuses the latest observation without creating an inference backlog.

For floor placement, the practice **Calibrate** button estimates camera elevation from the phone's gravity reading, then resets scale calibration. This is an approximate static tilt correction, not camera-intrinsics or floor-plane calibration. The full-body camera thumbnail and confidence overlay help verify framing.

## Validation

Run **Tools → FitFight → RTMPose → Run validation**. The report is written to `Logs/RTMPose-validation.txt`. Checks cover SimCC decoding, wrong-model rejection, ROI reacquisition, heel/toe mapping, occlusion expiry, home controls, GPU preprocessing, and real inference using the bundled model.

Benchmark CSVs are written under `Application.persistentDataPath`. Columns include the active engine, pose FPS, rendering FPS, Unity allocated memory, RTMPose frame-available-to-pose time, and dropped frames. Blank timing on Tracking4All means uninstrumented, not zero. Compare engines on the same device, camera settings, lighting, fighter, and movement sequence: stand still, jab/cross, hook, front/side kick, squat, crossed arms, leave/reenter frame. Warm up each model before recording; repeat after several minutes to expose thermal effects.

**The displayed timing is not physical movement-to-display latency.** It starts when Unity consumes an available camera frame and ends at pose publication; it excludes exposure, camera/driver buffering before availability, rendering, and display scanout. GPU/CPU utilization, battery, NPU use, thermal throttling, and actual motion-to-display latency require device profiling and high-speed video. No <60 ms claim has been established.

## Scope of this first experiment

This implements the first testable integration, not the entire proposed motion-capture architecture. Monocular depth is ambiguous; the current solver uses a toward-camera limb prior and approximate anthropometric lengths. It does not recover measured depth, full joint limits, body-specific bone calibration, root translation/footwork, floor-plane contact, full IMU/intrinsics calibration, persistent calibration, new foot IK/locking, or fight classification. Existing avatar grounding behavior still applies. Test punch/kick quality on a phone before treating the alternative as production-ready. Calibration is deliberately reset for each tracking session. QNN, ncnn, CoreML, CPU fallback, and NPU-specific inference are future backends.

## Model provenance

Source: [OpenMMLab RTMPose model zoo](https://github.com/open-mmlab/mmpose/tree/main/projects/rtmpose#body-2d).

[Original ONNX archive](https://download.openmmlab.com/mmpose/v1/projects/rtmposev1/onnx_sdk/rtmpose-s_simcc-body7_pt-body7-halpe26_700e-256x192-7f134165_20230605.zip).

The archive's `end2end.onnx` is stored as `Resources/RTMPose/Body26.onnx`; its original `pipeline.json`, `deploy.json`, and `detail.json` are retained here. No model conversion or retraining was performed. See [MMPose's upstream license](https://github.com/open-mmlab/mmpose/blob/main/LICENSE) and the upstream project for model/dataset provenance.

Model SHA-256: `85c5d669aa0b0c7969799ea9e0620d3b8c1d3c2b6878243e98a1ef8351457986`.

References: [Halpe26 keypoint order](https://github.com/open-mmlab/mmpose/blob/main/configs/_base_/datasets/halpe26.py), [Unity asynchronous readback](https://docs.unity3d.com/Packages/com.unity.ai.inference@2.6/manual/read-output-async.html).

Detailed calibration, every control, troubleshooting and tuning order: [TUNING.md](TUNING.md).

Startup preview: detected RTMPose landmarks appear in amber before calibration, then green once calibrated. Short confidence dips pause calibration instead of restarting it; gaps beyond the measured cadence allowance (250–750 ms) without a confident torso reset acquisition. Avatar motion remains disabled until calibration completes. Both the practice HUD and Tracking panel display progress. No separate model download or direct-avatar toggle is required.

Phone startup and step-back calibration changes are documented in `TUNING.md`. Build the Android diagnostic APK with **Tools → FitFight → RTMPose → Build Android camera fix**; the output is `Builds/FitFight-mobile-camera-fix.apk`.
