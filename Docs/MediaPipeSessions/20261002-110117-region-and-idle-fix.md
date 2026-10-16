# MediaPipe region filtering / idle shake — 2026-10-02

Reviewed the full 11:01:17 UTC phone session, its summary and screen recording. Region mode was on; the remaining stored tuning values match the earlier legacy run: cutoff 6 Hz, response 8, prediction 30.7 ms, direct avatar on, direct bone smoothing/speed limit off, requested 60 FPS. The run completed, with zero dropped records. Model/runtime were unchanged.

## Observation and comparison

The user's extra idle shake is consistent with the evidence. Initial rest showed more raw-model variation as well as weaker filtering:

| Phone recording | Initial raw / filtered RMS cm | Final raw / filtered RMS cm |
|---|---:|---:|
| Earlier legacy/off run | 1.08 / 0.60 | 2.16 / 1.89 |
| Region/on run | 2.50 / 2.32 | 1.53 / 1.39 |

These are wrist/knee RMS values during requested stillness, excluding the first two seconds. They include any real body movement and slow model drift, not just high-frequency noise. Different raw estimates prevent attributing the full between-run difference to the filter. Initial filtered variation increased, but final filtered variation was lower because final raw variation was lower. The screen recording shows torso/limb movement during the requested idle intervals; exact human-versus-model contributions require a better visual reference.

Measured processing throughput remained approximately 9.62 accepted poses/second. Frame-to-avatar median was 102.32 ms and p95 131.87 ms; the earlier run had median 102.24 ms and p95 122.47 ms. Inference median was 47.92 ms; GPU scheduling/readback median 48.22 ms. Region mode did not materially reduce that upstream wait. Camera FPS averaged 49.7 versus 52.4 earlier; these sequential runs do not isolate temperature or recording-load effects.

## Isolating the filter using identical input

Replayed this phone session's raw MediaPipe landmarks through the actual Unity filter code, with the same calibration, capture timestamps, confidence, aspect ratio and tuning. The original region replay matches the phone's stored accepted joints to within **0.0000301 cm** maximum error (float/JSON roundoff). This establishes that the replay follows the recorded processing, rather than an approximate replacement algorithm.

| Same recorded input | Initial filtered RMS cm | Final filtered RMS cm |
|---|---:|---:|
| Legacy shared filter | 2.040 | 1.037 |
| Original 1.09 region filter | 2.319 | 1.392 |
| Revised motion-aware region filter | 2.040 | 1.037 |

The original region preset passes about **14% / 34% more** idle variation than legacy on the same data. It reduces the wrist dead zone to 30% of the shared value and doubles the wrist cutoff even at rest, which favors responsiveness but passes more noise. For this session, the corrected preset reduces filtered idle RMS by approximately **12% / 26%** versus the original region preset. This is replay evidence, not a new phone measurement or ground-truth accuracy result.

## Correction in app 1.10

The per-region option now keeps shared filtering at rest and blends toward the faster regional preset only when confident, pelvis-relative **image movement** supports it. The motion evidence uses a fixed standing-image torso scale and actual observation timestamps; it is independent of input resolution. Inferred 3D depth noise alone cannot activate the faster setting. Missing image/root confidence, tracking gaps and recalibration clear the boost; they do not reject an otherwise observed wrist or knee. Prediction remains bounded as before.

The enabled profile is named **region-motion-aware** in new diagnostic records and summaries. Legacy/off behavior, model weights, camera pipeline, Unity MediaPipe integration and direct bone-smoothing options remain unchanged. This correction removes the region preset's additional idle noise; it does not promise to remove all raw-model drift or torso shake, and it does not address the approximately 100 ms processing pipeline yet.

On the recorded fast punches, mean wrist deviation from the unfiltered calibrated model output was **1.245 cm revised versus 1.480 cm legacy**. Elbow segments also remained closer to the raw trajectory. This is a responsiveness/tracking-deviation check against model output, not human accuracy or a measurement of end-to-end latency. Synthetic checks separately verify that visible fast movement activates the boost, stationary/depth-only jitter does not, and missing hips do not block an observed wrist.

## Next phone check

Install **FitFight-mediapipe-idle-fix.apk**, app version **1.10**. Use MediaPipe Full, per-region filtering **on**, stable body motion **on**, direct avatar **on**, requested **60 FPS**; adaptive filtering on, direct bone smoothing and speed limit off. Keep the same cutoff/response/prediction values (6 / 8 / 30.7 ms), camera, distance, lighting and screen-recording conditions.

Tap **MEDIAPIPE TEST · SAVE RESULTS** and follow the normal guide. Focus on both stillness sections and whether fast punches/elbow movements still follow. Send the matching JSONL, summary JSON and screen recording. The existing full test automatically labels stillness versus movement; no special recording procedure is needed.

The old Desktop legacy inputs were no longer present during this review; comparison of the earlier run uses its saved analysis and previously verified summary values. This new run's original inputs have been copied into `Logs/MediaPipe/20261002-110117/input/` so future replays remain reproducible even if Desktop files are moved or `vid.mp4` is replaced.

Replay outputs: `Logs/MediaPipe/20261002-110117/same-input-replay.json` and `.txt`. Regression log: `Logs/mediapipe-idle-validation.log`. Review images: `Logs/MediaPipe/20261002-110117/idle-start.png`, `idle-end.png`, and `contact-sheet.png`. No source recordings were edited.
