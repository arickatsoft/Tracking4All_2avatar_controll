# Engine comparison delivery — 2026-09-30

APK: `Builds/FitFight-engine-comparison.apk` (273 MB), package `com.DefaultCompany.Tracking4AllUnity`, version 1.08, Android API 26+, ARM64 and ARMv7.

SHA-256: `4cb9a077e6dffcf16321df26bc229691b603555df2a150e47ba73414fcc45aad`.

Build succeeded with zero errors and 746 warnings (see `Logs/engine-comparison-android-build.log`); APK v2 signature verified. Both ncnn ABI libraries and MediaPipe Lite/Full task models were verified inside the APK.

New engines are native ncnn RTMPose-s Halpe26 FP32 CPU (two threads), MediaPipe Pose Landmarker Lite CPU VIDEO, and Full CPU VIDEO. Existing Unity GPU RTMPose and Tracking4All remain selectable. All paths use the existing Unity camera; this build does not replace camera capture with CameraX or enable a Vulkan/GPU MediaPipe delegate.

MediaPipe publishes learned 3D limb positions; ncnn keeps the prior RTMPose 2.5D reconstruction. Switching resets native state/calibration/crop/filters and punch-observation generation. A failed model switches back to the working baseline and reports the error. Native work finishes before disposal; frame/output buffers are reused to reduce garbage collection.

The no-recording spoken sanity guide covers arms sideways/overhead, both elbows, forward/side torso bends, both knee lifts and fast punches. It waits for standing calibration, turns off punch assistance, stops existing session/benchmark recording, displays the active engine and measured timing/FPS, and releases on exit.

Validation:
- 54 native/model/orientation/lifecycle/world-pose checks passed, including actual Lite/Full person detection/world33, detection loss, monotonic video timestamps and disposal during inference/initialization.
- Original ONNX/ncnn numerical comparison passed for zero, seeded random and a recorded-person image; max absolute error below 0.00001 and zero argmax-bin differences.
- Real scene checks passed through native inference → standing calibration → publication → direct avatar for each new engine, repeated switching through all five choices, and sanity-guide cleanup on return home.
- Existing tracking checks, 166 punch checks, 69 full-body guide checks and recorded-motion response checks passed. The older optional reference contact-sheet test remained skipped because its fixture is absent; the new native recorded-person fixture ran successfully.

Phone runtime and end-to-end perceived latency remain to be measured by the user's live check. Host inference timings are not Android benchmarks. See `Docs/Engine-comparison-testing.md` for the test sequence and `NativePose/README.md` for reproducibility and timing limitations.
