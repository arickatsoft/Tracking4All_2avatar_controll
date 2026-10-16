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

App 1.19 uses a frame-driven Camera2 CPU capture path for MediaPipe on Android. A camera callback retains one latest YUV image. The inference worker takes it directly, performs native libyuv rotation/scaling/RGBA conversion and passes the direct buffer to MediaPipe synchronously. ImageReader images close before inference begins. Each new model consumer gets independent pixel storage while the preceding worker finishes. Results use three owned buffers with a short reference-exchange lock. Preview copies/uploads at at most 15 Hz with nonblocking pickup; inference scheduling does not wait for preview or Unity render ticks.

Default input maximum dimension remains 512. The Tracking panel also exposes 384 for measured A/B testing. Preserve aspect ratio and unmirrored, upright RGBA. The camera requests only supported processing controls, retains automatic exposure and reports matched capture metadata when available. Latest-frame overwrite, replaced-result counters and legacy busy-poll counters are distinct. Sensor timestamps are mapped only when Android reports a comparable realtime clock; availability-to-pose still excludes prior camera/display delay.

RTMPose/ncnn and legacy MediaPipe fallback retain GPU preprocessing/readback and their existing input shapes, ROI/reconstruction and freshness gates. MediaPipe CPU/GPU selection remains available; CPU is the game-test baseline. Initialization, inference and disposal stay on the same worker. Do not equate a requested camera FPS with actual capture/inference/render rates or confidence with labelled accuracy.

Opt-in ten-second exact-input archives (`.ffr.gz`, metadata and stats) are under MediaPipeSessions. `tools/read_input_archive.py` inspects/exports them; `PoseInputReplay.RunBatch` compares Full/512, Full/384 and Lite/512 on identical inputs. Standard full game tests still save JSONL/summaries under PunchSessions. See `Docs/External-recommendations-adaptation.md` for device validation and deferred experiments.

Native libyuv is pinned in `sdk/libyuv-revision.txt`, source hashes in `libyuv-source.sha256`, and its license is shipped in StreamingAssets. The Android library retains SIMD dispatch; universal macOS validation uses portable Arm kernels because Apple's compiler does not accept upstream architecture flags. `build.sh` ad-hoc signs the macOS output after linking; unsigned Arm libraries cannot be loaded for host tests.

## Validation artifacts

- `Logs/ncnn-equivalence.json`: original ONNX/native output comparison.
- `Logs/engine-comparison-validation.txt`: actual native inference, MediaPipe person/world33, camera orientation, lifecycle and 3D filtering checks.
- `Logs/engine-comparison-play.txt`: scene engine switching, standing calibration, avatar publication and no-recording guide lifecycle.
- Existing tracking, punch and full-body regression reports remain under `Logs` and `Logs/Practice`.

`tests/standing.png` is a low-resolution crop of the user's already-provided screen recording, including the earlier overlay. It is a functional smoke fixture, not ground truth for model accuracy or a phone performance benchmark. Host timings must not be presented as expected Android timings.
