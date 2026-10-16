# Native pose comparison integration

Runtime choices preserve the existing saved IDs (baseline = 0; Unity GPU RTMPose = 1). New IDs are native ncnn RTMPose = 2, MediaPipe Pose Landmarker Lite = 3, and Full = 4.

## Reproduce ncnn

Pinned upstream binaries:
- [Tencent/ncnn 20260526](https://github.com/Tencent/ncnn/releases/tag/20260526), CPU static Android SDK and macOS frameworks.
- [pnnx 20260704](https://github.com/pnnx/pnnx/releases/tag/20260704), macOS conversion tool.
- Original RTMPose-s Halpe26 ONNX is the existing `Assets/BodyTracking/Resources/RTMPose/Body26.onnx`, from the official MMPose RTMPose-s body7/Halpe26 export documented in `Assets/BodyTracking/README.md`.

Download/extract the pinned archives under `NativePose/downloads`, `sdk`, and `tools`, then run `NativePose/convert.sh` and `NativePose/build.sh`. `checksums.sha256` records the archive and model hashes. Compilation uses Unity 6000.6.0f1's NDK, C++ static runtime, and 16 KB page alignment for both Android ABIs. The universal macOS dylib is for Editor validation. No additional shared OpenMP/STL library is required.

The converter emitted a singleton batch dimension in the flattened head (`reshape_96`); ncnn Gemm interprets 3D channels as matrix rows, reducing this to one joint. `convert.sh` removes only that singleton dimension, with an exact topology assertion. Weight bytes remain unchanged. `tests/equivalence.py` compares all SimCC outputs against the original ONNX through ONNX Runtime on zero, seeded random and recorded-person image inputs. Maximum error is below 0.00001 and every argmax bin matches. Rerun this comparison after any SDK/model/converter update.

The native wrapper keeps memory-loaded weights alive until Net destruction, runs two CPU threads, uses FP32 and optimized internal packing, validates output shapes, and exposes only create/infer/destroy functions to C#. The asynchronous managed backend serializes initialization and inference. Disposal waits for active work before destroying a native graph/network.

## MediaPipe

Uses the repository's existing MediaPipeUnity native plugin and bundled `pose_landmarker_lite.bytes` / `pose_landmarker_full.bytes` task models. Android loads StreamingAssets from the APK with UnityWebRequest. The model is loaded into a CPU-delegate VIDEO task on a worker; timestamps are strictly monotonic. A single synchronous Video inference runs off the Unity thread, with no input queue. Model results are copied before the native image/result lifetime ends. Task and pinned image memory are disposed explicitly.

Both variants publish learned 3D world limb landmarks, mapped into the existing 26-joint avatar/publication interface. Standing calibration supplies neutral camera/body orientation and body scale, then the existing adaptive joint filters and capped prediction operate on these 3D positions. The 2D solver is retained for standing acquisition and head estimation; it does not supply MediaPipe limb depth. MediaPipe has one toe point per foot (duplicated in the Halpe toe slots); hand-direction extensions remain the existing body-only retarget convention. World landmarks are monocular model estimates, not physical depth-camera measurements.

## Camera and timing

All comparison engines share UnityWebCamBackend. Both new CPU paths resolve the camera texture, preprocess on GPU, and asynchronously copy the input into CPU memory before native inference. **This is native ncnn inference, not a CameraX-native camera pipeline or a Vulkan delegate.** All three new choices explicitly label their CPU/native engine.

RTMPose keeps the same 192×256 crop, RGB normalization, SimCC decoding, ROI tracking, standing gate and 2.5D reconstruction as the Unity GPU baseline. MediaPipe receives an upright, full-frame RGBA image, preserving aspect ratio with maximum dimension 512. Camera rotation and vertical-mirror orientation are tested in all eight combinations. At most one readback/inference is pending, so there is no growing backlog. Large managed input/output arrays are reused per backend instead of allocated every frame; switching gives the new backend separate buffers while old work finishes.

Live metrics display actual camera, inference and accepted-pose rates, result age from Unity camera availability, and backend time. CPU backend time covers the native call. Unity GPU backend time is schedule-to-output-readback, which includes queue/preprocessing and is not a GPU kernel profiler measurement. Frame-to-pose excludes exposure, camera/driver buffering before Unity availability, avatar/render smoothing and screen display latency. Confidence values from different models are not directly comparable; they are not accuracy scores.

## Validation artifacts

- `Logs/ncnn-equivalence.json`: original ONNX/native output comparison.
- `Logs/engine-comparison-validation.txt`: actual native inference, MediaPipe person/world33, camera orientation, lifecycle and 3D filtering checks.
- `Logs/engine-comparison-play.txt`: scene engine switching, standing calibration, avatar publication and no-recording guide lifecycle.
- Existing tracking, punch and full-body regression reports remain under `Logs` and `Logs/Practice`.

`tests/standing.png` is a low-resolution crop of the user's already-provided screen recording, including the earlier overlay. It is a functional smoke fixture, not ground truth for model accuracy or a phone performance benchmark. Host timings must not be presented as expected Android timings.
