# Camera-driven full-body avatar control: independent review handoff

Prepared 9 October 2026. Covers the development and recorded tests available in this project through app **1.17**, including the latest session **20261009-192549-873**. This document is self-contained so it can be supplied to another model without the chat history. It summarizes implementation and observed evidence; it is not a claim of motion-capture accuracy.

## Request to the reviewing model

Please independently assess this system and propose additional ways to achieve stable, responsive, full-body game-avatar control from one smartphone camera. Challenge the current architecture and conclusions where warranted. Do not assume that adding a newer pose model, stronger smoothing, a temporal lifter, or a learned classifier is automatically the answer.

For each promising direction, identify:

1. The specific observed failure it addresses and the mechanism by which it might help.
2. Whether it changes capture, estimation, temporal state, retargeting, animation, contact, or evaluation.
3. The smallest experiment that could confirm or reject it, using existing recordings where possible.
4. Expected latency/resource tradeoffs and risks to successful punches, held lifts and idle stability.
5. The reference labels and acceptance metrics needed before adopting it.

Rank near-term engineering changes separately from larger research experiments. If suggesting libraries/models, verify current versions, licensing, Android/Unity integration, model inputs and sustained device performance. Distinguish documented capabilities from hypotheses. Explain what evidence is missing before recommending more recording or model training.

## 1. Goal, constraints and accepted tradeoffs

The end goal is a game avatar that feels controlled by the user's whole body: punches, leg kicks and knee drives, arm gestures, head movement, torso bends/twists and squats, with coherent posture, stable idle, support-foot grounding and near-real-time response. The immediate application is a Unity punching-bag fighting game.

The desired output is a polished game interpretation of the movement, rather than an exact copy of every noisy landmark. Early observed intent may initiate a bounded animation that completes an action. Continuous posture/gestures should still work between attacks. The user explicitly accepts interpreting an ambiguous **single-arm upward stroke as a punch after fighting-guard acquisition**. Clearly distinguishable coordinated bilateral arm gestures should remain available. Extra punches and nonexpert side-kick technique during guided tests are allowed; requested repetition counts are not ground truth.

The user's preferred final setup is **a phone at floor level**, using the front camera, with the entire body, feet and raised hands visible. Torso placement was tried and did not look dramatically better to the user. There is no controlled evidence of a sufficiently large torso-height advantage to justify abandoning floor placement. Starting upright for calibration and then adopting fighting guard is acceptable.

Current evidence is principally from one user and a Samsung **SM-S928B**. Earlier integration work also involved a Galaxy S21, but recent quantitative results are from SM-S928B. No second camera, depth sensor, wearable or server inference is required by the current implementation. Alternative hardware could be discussed as an explicit change to the product constraint, not assumed available.

“Full body” is not fully achieved in the unrestricted sense: reliable walking/global translation, jumps/landing, roundhouse/back kicks, distinct jab/cross/hook/uppercut output, finger articulation, dense facial expression, floor work and arbitrary occlusion have not been established. The stage is largely anchored around the bag; squat/pelvis and balance offsets are supported. This is not general-purpose world-space motion capture.

## 2. Current platform and pipeline

- Unity **6000.6.0f1**, Android, IL2CPP/native plugins; latest delivered app **1.17**, minimum Android API 26, target API 36.
- Unity AI Inference package **2.6.1** is installed for the ONNX path. Unity PlayableGraph and two-bone IK provide animation and limb composition.
- Five selectable tracking paths: original Tracking4All; Unity GPU RTMPose; native ncnn RTMPose; MediaPipe Pose Landmarker Lite; MediaPipe Pose Landmarker Full.
- **MediaPipe Full, CPU** is the current preferred path after qualitative user comparison. No quantitative cross-model pose-accuracy ranking was established.
- Repository MediaPipe Unity integration is **0.14.4**. The dated 1 October audit identifies its underlying MediaPipe as 0.10.14 and documents a newer Unity plugin at that time. This is historical version information, not a fresh claim of today's latest release. The plugin was not upgraded during the behavioral/capture iterations.
- The same dated audit compared bundled Lite/Full task model payloads with Google's then-current downloads: trained payloads matched. Runtime version and trained weights are separate questions.
- RTMPose is **RTMPose-s, Halpe26, 256×192**, checkpoint `7f134165_20230605`; it already has heel/toe joints. Native ncnn is pinned to **20260526**, FP32 CPU, two threads. ncnn conversion changes runtime, not training or model accuracy.

Current data flow:

```text
Front camera / Android Camera2 latest YUV image
    -> CPU resize, YUV-to-RGBA and upright rotation
    -> dedicated native inference thread, MediaPipe Full VIDEO task
    -> image33 + estimated world33 landmarks, source timestamps/confidence
    -> standing-reference orientation/scale calibration
    -> confidence/finite/freshness checks and motion-aware joint filtering
    -> accepted torso/head/arm/leg evidence
         -> independent per-hand boxing state machines
         -> provisional leg state and front/side/generic/knee resolution
    -> ONE final avatar compositor
         authored fixed guard + punch animation + tracked posture/gestures
         + support-foot IK + procedural leg strike envelope
         + render-cadence damping and ownership transitions
    -> swept fist/foot/knee contact against moving bag
    -> one physical hit at most per action
```

The old fallback capture path remains Unity WebCamTexture -> GPU preprocessing -> asynchronous GPU readback -> CPU inference. The active capture path is logged per inference; an enabled native checkbox does not prove that native capture actually ran.

There is at most one inference in flight and no growing inference backlog. Latest unprocessed source frames are preferred. Initialization, inference and teardown share a dedicated native worker. Optional MediaPipe GPU inference exists with CPU fallback; recent phone comparisons use CPU, so GPU improvement is unproven. Engine switching resets relevant state and releases the prior owner.

### Pose representation and application

MediaPipe provides monocular **estimated** 3D world landmarks; these are not depth-camera measurements. They are calibrated to a standing orientation and user-height scale. The shared publication interface maps them to the existing 26-joint layout, with MediaPipe's single foot-index/toe point duplicated into the two Halpe toe slots. Body-only hand landmarks do not supply full finger articulation.

RTMPose supplies 2D joints, with depth from a calibrated geometric 2.5D solver. It does not currently run a learned temporal 3D lifter. MediaPipe torso/limbs use its world estimates; current MediaPipe head rotation uses image ear/eye/nose geometry with image-space torso-roll compensation to avoid unstable inferred ear depth.

Direct retargeting is a separate diagnostic/body-mirroring mode. Assisted game mode owns the final skeleton and composes continuous body motion itself; changing a direct-mode smoothing checkbox cannot fix the assisted compositor. Automatic humanoid Animator evaluation must not overwrite directly applied transforms.

Latest recommended game profile: MediaPipe Full CPU; native CPU camera on; per-region and motion-aware filtering on; stable body correction on; assistance on; experimental direct avatar off; requested camera 60 FPS. Actual camera, pose and rendering rates are measured separately. Latest session's saved shared tuning includes cutoff 6 Hz, response 8, velocity cutoff 12 Hz, noise radius 1.8 cm, confidence threshold 0.30 with 0.08 hysteresis, prediction setting 10 ms and bounded prediction distance 12 cm. These are current settings, not universal optimal values; older sessions used about 30.7 ms prediction.

Critical leg-state gates currently include both-leg confidence of at least 0.5; learned thigh/shin lengths between 0.3 and 1.75 times reference; knee speed at most 7 m/s; foot speed at most the larger of 10 m/s and 16 times total reference leg length; accepted source age at most 300 ms and receipt age at most 230 ms. Knee commitment requires accumulated forward evidence followed by a bent-leg return within 750 ms of preparation. The 460 ms strike's score window is phase 0.20–0.62, with verified geometry and supporting joints. These are engineering heuristics on estimated coordinates, not universal anatomical truths, and are appropriate targets for critique.

## 3. Development history: problems, changes and findings

### A. Foundation: grounding, lifecycle, ownership and the hybrid design

Early work repaired Android native callback dispatch, source stop/restart/subscriptions, avatar selection, missing-leg fallback and foot-to-ground correction. Mirrored confidence mapping and bone restoration order mattered. Scaled-mesh shoe-ground measurements initially disagreed with independently skinned geometry; the corrected mesh-baking scale handling removed that error. These are foundational application/retargeting issues, not evidence that a different pose network is required.

The motion-control design adopted three cooperating layers: continuous observed motion, intent/phase detection and animation completion. Crucially, there is one final skeleton owner, rather than competing direct tracking, Animator, guard and assistance writers.

A supplied plan proposed RTMPose + MotionBERT-Lite, supplementary MediaPipe feet, One Euro rotations, spoken training labels and later a temporal classifier. The review retained the hybrid idea but corrected unsupported assumptions:

- Installed RTMPose is 26 joints with feet, not a 17-joint body-only model.
- Existing engines are switched, not already synchronized and fused per region.
- Filtering, grounding and Unity inference were already present; adding another layer blindly could increase lag or conflict with ownership.
- MotionBERT's compatibility, causal window length, exported pose head, unusual-camera performance and sustained Android cost were not demonstrated for this setup.
- A 3D lifter does not eliminate monocular ambiguity. Offline HybrIK/WHAM output would be pseudo-labels, not independent ground truth.
- Confidence thresholds, platform star ratings and nominal FPS claims are not a validation plan.
- Voice prompts are weak task labels, not exact movement boundaries.

Optional direct-mode quaternion smoothing/angular caps were implemented and tested with independent toggles, shortest-arc handling, timestamps, loss/reentry and pose-owner reset. They were not presented as a universal solution or automatically enabled everywhere.

### B. Early RTMPose punching: state history was a major source of misses

September recordings delivered approximately **9.4–9.8 accepted poses/s** with about **99–100 ms result age**, despite higher camera/render rates. Some required a second observation to confirm intent, adding approximately another 100 ms. Rendered pose streams were around 40–43 Hz, with p95 gaps around 62–64 ms.

Reviewed failures included:

- Paired-arm suppression remaining latched during otherwise valid alternating punches.
- Downward hand reset starting a false preparation, then preventing the next real stroke from rearming.
- Brief gesture inhibition wiping readiness as if tracking had been lost.
- Curved/changed-height returns failing a simplistic launch-direction rearm rule.
- A candidate initially classified as a sweep failing to promote to extension when later elbow/reach evidence became strong.
- Physical hits rejected because bag/fist relative contact direction was handled incorrectly, or a moving bag was genuinely out of reach.

Changes separated cancelled preparation from completed-punch recovery, preserved trusted folded-arm references through brief inhibition, released shared gates when one arm dominated, allowed corroborated type transitions and improved relative surface-velocity contact checks. Independent timestamps, action IDs and contact diagnostics were added so recognized-but-unapplied, applied-but-not-contacting and unrecognized movements could be distinguished.

Older saved fixtures of 756, 733, 720 and 703 observations became regressions. On the 703-observation fixture, revised replay rejected the reviewed downward preparation, restored the following left and quick right strokes, and retained bilateral-overhead negatives. These are tuned regression successes, not held-out generalization accuracy.

Earliest reports measured roughly **297–307 ms from the first candidate's frame availability to a bag hit**. Later reports often start at the *committing* observation instead. These have different origins and must not be compared as a single percentage speedup.

### C. Full-body recording exposed calibration and rendered-rig defects

Spoken full-body tests were expanded to arms, elbows, torso, head, legs, knees and squats. A user-declared incorrect left-knee movement at video 2:22–2:32 in one early recording was excluded from accuracy judgments.

Problems included standing legs reconstructed bent forward, lateral/overhead arms distorted by forward-depth assumptions, torso yaw/roll coupling and correct exposed transforms not appearing correctly in the rendered humanoid.

Changes:

- Acquire standing reference only after stepping back, from visible, still, sufficiently upright full-body observations.
- Wait with spoken framing/calibration hints instead of learning from approach motion.
- Accept strong neutral shin foreshortening from the low camera rather than inventing depth from an inappropriate minimum projection.
- Preserve lateral/rising image evidence and limit ambiguous forward-depth priors.
- Correct torso/head normalization and axis coupling.
- Control Animator evaluation to avoid cached humanoid skinning ignoring applied direct transforms.
- Log raw/reconstructed joints, accepted/observed masks, calibration and final rig transforms.

Recorded replay reached the actual avatar/renderer. Reviewed neutral thigh/shin angles were about **2.6° median, 3.0° max from down**; applied limb directions matched published targets within about **0.03°**. This verifies solver-to-rig application, not human 3D accuracy. Later overhead replay had median upper-arm angle about 13.5° from up.

### D. Model/runtime comparison, then MediaPipe-focused development

App **1.08** added native ncnn RTMPose-s and MediaPipe Lite/Full, retaining the previous engines. A spoken no-recording sanity check allowed visual comparison across body parts. The user preferred MediaPipe Full overall; RTMPose also worked reasonably in some regions. There was no saved per-model quantitative summary from those initial sanity checks.

Native ncnn versus original ONNX checks on zero, seeded random and a recorded-person image gave maximum output error below **1e-5**, with matching SimCC argmax bins. Real host Lite/Full detection/world landmarks, switching, disposal and actual avatar publication were tested. These establish conversion/integration correctness, not comparative phone accuracy.

App **1.09** added MediaPipe-specific standing 3D calibration and acceptance, bypassing inappropriate RTMPose 2D projection gates for MediaPipe world limbs. It added per-region filtering, a roughly 112-second spoken body protocol, raw/filtered/avatar diagnostics, JSON/Markdown summaries and label-export scaffolding.

No pose-model fine-tuning or action-classifier training has occurred. No multi-model body-part fusion was deployed.

The spoken tests use native **text-to-speech**. The original plan's user-spoken keyword recognition, Vosk/ASR integration and automatic trainer-speech labeling were not implemented. Current cue times mark speech enqueue; speech completion and actual user movement boundaries are not measured.

### E. Filtering improved through same-input replay, not just stronger smoothing

The first region preset was more responsive but shook more at rest. The 2 October region-on recording was replayed through the actual filter with saved tuning/calibration. Its original output reproduced within about **0.0000301 cm**, showing the replay matched production processing.

| Same raw recording | Initial idle wrist/knee RMS | Final idle RMS |
|---|---:|---:|
| Shared legacy filter | 2.040 cm | 1.037 cm |
| Original region preset | 2.319 cm | 1.392 cm |
| Revised image-motion-aware preset | 2.040 cm | 1.037 cm |

App **1.10** kept shared smoothing at rest and activated faster regional response only from confident pelvis-relative **image motion**. Inferred depth jitter alone could no longer request the faster preset. On recorded fast punches, revised mean wrist deviation from the raw model trajectory was 1.245 cm versus 1.480 cm for legacy filtering. This is fidelity to model output, not accuracy against the person.

Phone processing stayed around **100 ms** and 10 Hz. Filter changes did not remove camera-transfer/inference delay.

### F. Boxing-specific interpretation and preview repair

The 2 October body-mirroring video showed forward punches becoming upward avatar arm extensions. Because assistance was off, it did not test the hybrid punch detector. This led to explicit separation of body-mirroring and game tests.

App **1.11** added a MediaPipe boxing prior: acquire a folded fighting guard, combine independent image travel with raw world opening/reach in torso units, commit strong evidence promptly, and complete a coherent bag-directed punch instead of copying erroneous upward depth. Each hand rearms independently. Missing/stale wrists require reacquisition; prediction cannot commit. Single-arm upward projection is accepted as a punch when ambiguous; clear bilateral overhead remains distinct.

October 6 floor/torso recordings exposed a preview/recording update stall while arm observations and some gameplay continued. The exact Android exception was not captured, so a definitive root cause was not claimed. App **1.12** separated preview/HUD Update from contact/final-pose LateUpdate, refreshed from the latest texture rather than a late consumer's update flag, isolated error handling and recorded diagnostics on inference-completion events. Counts and error stacks made subsequent failures visible. An injected scene contact failure verified that preview/timer logging continued.

Subsequent October 7 phone runs had approximately **60 final poses/s**, live previews and zero logged runtime errors/dropped records. Several useful successful single-arm and alternating sequences were preserved. Support-hand/return false decisions and isolated-jump recovery remained.

App **1.13** strengthened the upward-stroke policy, recovered a trusted guard through two fresh folded observations after an isolated jump, and improved candidate motion/reach checks without a global sensitivity increase. Replays across the three October 7 recordings preserved or restored reviewed strokes and removed some reviewed support/return decisions. Partial outward movements can still score by product policy.

App **1.14** fixed a distinct application bug: a confirmed opposite-hand punch was queued behind an uncommitted preparation and could expire after 180 ms. In the 13:01 session there were 24 decisions but 23 physical hits; one reviewed fast-right decision was never animated. The fix gives a committed opposite-hand stroke priority over tentative preparation. An actual-scene regression failed before the fix and then started the strike within its 80 ms test bound and hit once.

### G. Full-body game composition and native camera capture

App **1.15** unified tracked torso/head/arm gestures, squat displacement, support feet, provisional lifts and front/side/knee strikes in the assisted final owner. Tracked body rotation is bounded to 60°, head to 45°, squat/pelvis lowering to 48 cm. Procedural leg strikes use a bounded **460 ms** envelope. Foot release has **7.5 cm onset / 3.5 cm return** hysteresis. Clear bilateral arm gestures remain tracked; scoring requires a fresh supported action and real swept contact.

It also added native **Camera2 CPU capture** to bypass GPU readback and a single **164-second audio-directed GAME BODY TEST** with bounded additional calibration waiting (maximum 16 seconds).

The first 1.15 phone run, session **142232**, exposed failures that synthetic tests missed:

- Camera2 had disconnected; **every evaluated frame used legacy fallback**, so the native optimization had not been validated.
- Held knee lifts scored attacks.
- Four visible front extensions produced no FrontKick decisions.
- A fixed **95 cm inter-ankle lift cap** rejected genuine high kicks before classification.
- Knee commits occurred prematurely during a kick chamber and prevented later foot resolution.
- Knee travel and speed had to cross thresholds in the same sparse sample.

App **1.16** replaced the fixed cap with calibrated limb geometry/speed bounds, retained visually responsive but provisional chambers, accumulated causal peak evidence, delayed knee commitment until a short bent-leg return, and allowed generic foot strikes when direction was uncertain. Camera availability/closure/retry handling and active-path diagnostics were expanded. Exact accepted body snapshots were added for replay.

The 1.16 session **151451** finally ran Camera2 CPU throughout. Pose age improved and inference cadence nearly doubled, but knee classification, transient leg corruption, leg contact and head roll remained problematic.

### H. Current smoothing/composition update: app 1.17

Analysis of 1.16 separated two head-motion sources: unstable inferred face depth and the imported guard idle itself turning/fidgeting. App 1.17 fixes the guard at a reference pose and lets observed offsets supply movement. MediaPipe head angles use image geometry; torso/limbs retain world estimates.

Additional changes:

- Causal render-cadence damping: stronger idle smoothing, faster response for deliberate posture; no future-frame buffer.
- Smooth arm/guard handover and elbow/knee poles. Punch-owned arms bypass tracked-arm damping.
- Authored fist target bounded to **12 m/s**, below the contact sweep's **18 m/s** rejection threshold; smooth 100 ms outgoing-arm handover.
- Lift/return foot targets use about 50 ms damping; committed kicks use faster about 18 ms damping.
- Kick handover starts from the last rendered foot/knee instead of freshly evaluated guard joints.
- Provisional legs keep bag clearance so confirmed sweeps can approach/rearm normally.
- Both legs are geometrically checked before visual release/squat updates. Brief corrupt observations hold the last verified leg display for up to **120 ms**, but **immediately disable impact permission**. Sustained loss cancels/requires stance reacquisition.
- Foot attacks require substantially extended knees (bend below 60°); knee drive remains short-return-based. Action IDs and physical-contact requirements are preserved.
- A bounded initial camera-open probe handles stale availability notifications; retries still wait for closure and respect attempt limits. Device startup remains unresolved.

## 4. Most useful quantitative phone results

These are sequential recordings of different human performances. Camera resolution/aspect changed with the native path, and recording/thermal conditions are not controlled. They are useful engineering evidence, not a clean algorithm benchmark.

| Metric | 1.15 / 142232 | 1.16 / 151451 | 1.17 / 192549 |
|---|---:|---:|---:|
| Active evaluated capture | Legacy, all 1,653 | Native, all 3,186 | Native 3,152; initial legacy 1 |
| Pose age median / p95 | 100.1 / 117.1 ms | 72.6 / 93.0 ms | 72.2 / 92.9 ms |
| Native model compute median | 40.7 ms | 43.3 ms | 43.4 ms |
| Schedule-to-readback/copy median | 48.7 ms | 0.5 ms | 0.5 ms |
| Inference estimate median | 10.0 Hz | 19.5 Hz | 19.5 Hz |
| Camera estimate median | 51.9 Hz | 48.3 Hz | 49.1 Hz |
| Matched fist availability-to-contact median / p95 | 171.5 / 187.6 ms | 141.5 / 191.4 ms | 137.2 / 215.2 ms |
| Punch decisions / fist hits | 23 / 21 | 24 / 23 | 27 / 24 |
| Foot / knee hits | 0 / 6 | 6 / 0 | 5 / 0 |
| Total physical bag hits | 27 | 29 | 29 |

Native capture correlates with about **27% lower median pose age** and almost twice the independent inference cadence. Compute did not speed up. The roughly 48 ms readback wait was bypassed, but native YUV conversion still costs about **11.7 ms**. In 1.16 the frame age at scheduling was about 21.9 ms and already included conversion; adding those two values would double count. Native 640×480 becomes upright 384×512; legacy 1280×720 became 288×512, so image/FOV differences must also be considered.

The contact median improves, but its tail does not. These timings begin at an observed source frame's software availability and end at logged physical bag contact; exposure, preceding driver buffering, movement onset before the sampled frame and display presentation are excluded. Contact is also later than the first visible avatar response. No verified physical motion-to-photon measurement exists.

### Latest 1.17 results in detail

- 9,804 final pose records, 9,750 preview refreshes, zero runtime errors and zero dropped log records; sampled preview keeps moving. Median/p95 render intervals about **16.5 / 24.2 ms**. This is distinct from 19.5 Hz inference.
- All 210 unaccepted evaluations occur before REC 11.01: 162 calibrating, 46 no-person, 2 discarded. They are not 210 missed movements during the active test.
- All **13 punch decisions around the main punch sequence** hit. The fourth slow punch happens after the next cue begins, so the summary's three slow-row hits do not establish a missed fourth punch. This does not prove complete punch recall or precision.
- During the rapid sequence, the shared overhead gate briefly blocks around REC 35.76–35.85. A later left commit hits; this interval is a possible avoidable onset-delay regression.
- Held knee lifts produce **zero strikes and zero hits**.
- Nine leg-strike decisions: seven front, one side, one knee. Five foot hits; zero knee hits.
- Three committed foot attacks have 11–12 eligible contact frames but never touch the bag. Closest eligible surface gaps are approximately **10.4, 14.3 and 19.6 cm**. This is downstream trajectory/reach/contact failure after recognition.
- The single Knee action is cancelled after prolonged rejected leg geometry. Only six of 2,943 body observations fail leg verification; rejecting corrupted geometry remains necessary.
- Two movements during knee instructions extend to nearly straight knees at commit (about 7° and 13° bend). They cannot be assumed to be pure knee drives incorrectly classified solely because of the cue.
- Side-kick direction remains inconsistent. One sequence changes knee/side state; another resolves as front. User technique and monocular projection both complicate class labels.
- Torso bends, head turns/nods, bilateral arm gestures and squats are visibly represented. This is qualitative success, not measured 3D correctness. Deep squats lower the avatar behind the UI; camera/framing presentation also limits assessment.

Quiet REC 160–164 comparison:

| Measure | 1.16 phone | 1.17 phone |
|---|---:|---:|
| Estimated head-roll span | ~32° | ~2° |
| Rendered local head deviation from first sample | 17.3° | 5.2° |
| Local head frame-step p95 | 3.66° | 0.093° |
| Local chest deviation from first sample | 5.53° | 0.16° |

This supports substantially steadier quiet animation, with different human performances as a confound. Current estimated head pitch still spans about 8.4° in that interval. Local bone rotations are not absolute world head orientation.

Camera startup is **not fully fixed**. The latest lifecycle snapshot retains earlier zero-frame disconnects and a camera request error. The native attempt for this recorded test succeeds on first open and streams. A successful active test does not prove reliable cold start, ownership handover or pause/resume.

## 5. Testing and evidence infrastructure

Testing evolved from numerical/synthetic checks to saved-input detector replay, actual processing replay, actual rendered-avatar checks and actual moving-bag scene tests.

- Unit/synthetic checks cover timestamps, duplicate observations, finite coordinates, q/−q equivalence, confidence/loss/reentry, guard/rearm, bilateral negatives, foot support/release, held lifts, short knees, high/generic/directional kicks, impossible geometry, smoothing response and one score per action.
- Frozen previous detectors reproduce historical recordings before revised behavior is compared. The 1.15 baseline reproduces 25 body events; the 1.16 baseline reproduces all 33 body events.
- Original and half-cadence exact body replay tests preserve ten foot-extension decisions on the 1.16 recording. Direction labels change at reduced cadence, exposing classification sensitivity instead of hiding it.
- Exact full-scene 1.16 body **and arms together**, using all 3,085 paired observations, exercises actual rig and moving bag. Revised replay gets all 14 main punch contacts, zero held-knee hits, 31 total physical hits including seven feet. This differs from original phone totals and is not an accuracy score or device-physics equivalence proof.
- Earlier leg scene tests deliberately neutralized torso/head and used guard arms. Those tests passed but missed combined full-body/contact problems. Complete coupled replay was added afterward.
- Latest suite includes **107 synthetic full-body checks**, **56 smoothing/quality/exact-recording checks**, actual scene/punch/lifecycle regressions, native model checks, **26 pure-Java camera ownership/recovery checks** and **52 YUV/rotation checks**. Test counts are coverage indicators, not accuracy metrics.
- A controlled 20 Hz noisy target rendered at 60 Hz reduced angular-step variation about 89%; deliberate rotation steps settled within roughly 100 ms at 30/60/120 render Hz. These are algorithm tests, not phone smoothness or latency benchmarks.
- APK builds/signatures/manifests/native classes/models/libraries were verified. Latest build has zero errors and 750 warnings. Host Java policy tests do not emulate Android's camera service, and host model timings are not phone timings.

Saved JSONL includes source/receipt times, engine/capture path/settings, raw image/world landmarks, filtered/published geometry, confidence/observed masks, exact accepted body/arm snapshots, cue enqueue times, detector states/reasons, resolved action IDs, rig local rotations/world positions, contact sweeps/rejections and runtime/drop counts. The format grew to schema 7, `practice-11-smooth-game-body`.

Matching summary JSON/Markdown and optional phone screen recording accompany sessions. The screen recording mainly shows the game and a **small camera preview, often with landmark overlay**. It is not generally a clean full-resolution source-camera video, and is insufficient for precise independent body-angle/depth ground truth. Reused Desktop `vid.mp4` files are archived per session before overwrite.

Current boxing/game-body sessions save under `PunchSessions`; separate direct MediaPipe body tests use `MediaPipeSessions`. Captures use different controllers and should not be compared solely by file names or a zero hit total.

## 6. Camera placement findings

“70 degrees,” phone-screen angle, lens optical-axis elevation, gravity pitch and the solver's clamped correction are not interchangeable. Older UI correction was limited to ±45° while recorded gravity pitches sometimes exceeded 57–65°. Other viable floor runs measured around 24–28° upward. MediaPipe standing-world calibration is distinct from the older RTMPose perspective correction. Latest session metadata's initial 20° field is not a verified physical mounting angle.

Lower lens elevation may reduce some forward/vertical ambiguity, but the exact optimum was not established. Distance, lens/FOV, framing, orientation, lighting, occlusion and full-body pixel coverage also matter. A floor view can work; it is not evidence that every floor angle is equivalent or that temporal lifting would automatically solve it.

Given the user's preference and the software defects already isolated, floor placement remains the default. An additional torso/floor study should be justified by a particular unresolved projection error and use matched motion, distance/framing, calibration and independently reviewed labels.

## 7. Conclusions and remaining gaps

### Supported conclusions

1. The hybrid approach is viable for a bounded fighting-game vocabulary, while continuous body gestures can coexist with assisted attacks.
2. Many important failures were in state transitions, calibration, skeleton ownership, camera transfer and contact composition—not just pose detection.
3. Bypassing GPU readback provided the clearest measured latency/cadence gain; changing filters did not accelerate inference.
4. Stable authored guard plus appropriate render damping and head geometry substantially improved quiet presentation.
5. Recognition, animation start and bag contact must be evaluated separately. Scoring every decision would conceal real application/contact failures.
6. Real recordings exposed defects absent from synthetic tests; exact baseline reproduction and full coupled scene replay materially improved diagnosis.
7. The current user data supports keeping MediaPipe Full and floor capture as working baselines. It does not establish either as universally best.

### Unresolved

- Knee versus held lift versus kick chamber has a real causality tradeoff: initial motion can be identical, so immediate visual following and delayed score may need separate policies.
- Knee and side-kick labels do not generalize consistently; direction changes with cadence and uncertain depth.
- Confirmed foot strikes can miss the moving bag due to captured target, anatomical reach, balance offsets, damping, phase/contact window or simultaneous fist/body composition. The exact contribution should be isolated in scene replay.
- Support-arm/return movements and some partial strokes can still be interpreted as punches; the accepted boxing prior deliberately favors sensitivity.
- The paired-arm gesture gate can briefly suppress a rapid combination.
- Capture still spends ~11.7 ms converting YUV, model compute ~43 ms, and pose age ~72 ms. Tail contact response is still substantial. Thermal, battery and diagnostic-on/off performance are not characterized.
- Native camera cold start/transition errors persist despite successful recording operation.
- No multi-participant held-out benchmark, independent 3D ground truth, calibrated end-to-end latency study or model training has been performed.

## 8. Directions to challenge or investigate — hypotheses, not deployed improvements

The existing immediate priority is to replay and repair the three missed foot contacts, then investigate knee/side resolution, rapid-combination gating and camera startup. Please also consider alternatives beyond this sequence:

- **Contact-aware procedural motion:** bounded reachable strike goals, moving-target prediction, support/pelvis coordination, IK feasibility and score-window alignment. Preserve actual contact and avoid unlimited target chasing or body teleportation.
- **Anatomically constrained temporal state:** constant segment-length estimation, confidence-weighted bone constraints, joint-angle limits and robust outlier handling before classification. Compare with the current ad hoc bounds on the same corrupt snapshots.
- **Probabilistic/phase-based intent:** competing knee/kick/hold hypotheses, explicit uncertainty, provisional visuals, later class refinement and generic attacks. A strict early class lock may be the wrong representation.
- **Higher-rate cheap image evidence:** optical flow or a local wrist/foot tracker between full-pose evaluations, used for bounded early visual preparation. Measure drift, occlusion and false starts; never assume it supplies reliable depth or scoring permission.
- **Capture/inference optimization:** direct/native preprocessing, buffer copies, YUV handling, input resolution/crop, CPU threads/delegate choices and persistent camera ownership. Profile the actual device and full game rather than quote model-only FPS.
- **Temporal 3D lifting/body fitting:** causal small lifter or parametric-body constraints, only if it beats MediaPipe's existing world path on identified errors after accounting for mapping, depth, unusual view, startup and latency.
- **Model/runtime alternatives:** controlled Lite/Full, upgraded MediaPipe runtime, RTMPose variants or another estimator. We have not tested a larger RTMPose checkpoint or demonstrated that an upgrade improves this use case.
- **Selective fusion:** image versus world evidence or per-region models with synchronized source timestamps and shared coordinates. Simultaneous full-model fusion could cost more latency and introduce disagreement; justify it against one-model constrained estimation first.
- **Learned intent:** a small causal classifier trained on reviewed onset/apex/end and negatives, with session/person splits before overlapping windows. Do not train on cue rows as truth or evaluate on the same recordings used for threshold tuning.
- **Better measurement:** clean source camera frames/video, larger synchronized human/display reference, actual speech-onset records, calibrated timing stimulus, and a limited independent pose reference for difficult knees/side kicks.
- **Test-protocol timing:** verify that each spoken instruction leaves enough execution time before the next cue. Multiple repetitions after a long instruction can cross nominal windows. Add reviewed movement boundaries or speech start/end evidence before treating those crossings as classifier errors.
- **Presentation:** full-body framing during squats/kicks and clearer diagnostics. Perceived instability can come from authored clips, camera/UI and ownership even with stable landmarks.

Please recommend a minimal, prioritized experimental program rather than an unbounded list of dependencies. State what would falsify each recommendation and which successful regressions must remain intact.

## 9. Repository map and evidence references

These paths are for a reviewer who also receives the repository; this summary does not require opening them.

- `Assets/BodyTracking/Runtime/RTMPosePoseProvider.cs`: scheduling, publication, camera/backend integration.
- `PoseInferenceBackend.cs`, `AndroidCpuCamera.cs`: native inference worker and managed capture wrapper.
- `MediaPipeWorldPose.cs`, `MediaPipeMotionGate.cs`, `BodyMotionEstimator.cs`, `DirectHumanoidRetargeter.cs`: calibration/filtering/head/body/direct application.
- `Assets/Practice/Runtime/Punching/BoxingPunchIntent.cs`, `BoxingGestureGate.cs`: boxing intent and shared gesture gating.
- `GameBodyMotion.cs`, `AssistedPunchController.cs`, `AvatarMotionSmoothing.cs`: leg state, single final pose owner and render smoothing.
- `GameBodyRecordingGuide.cs`, `GameBodySessionStats.cs`, `PunchSessionRecorder.cs`: guided collection, summaries and diagnostics.
- `Assets/Plugins/Android/FitFightCamera.java`, `FitFightCameraRecovery.java`, `FitFightCameraPixels.java`: capture, lifecycle policy and conversion.
- `Assets/Practice/Editor/`: exact/frozen detector replay and actual-scene validation.
- `NativePose/README.md`, `Docs/Engine-version-and-stats-audit-20261001.md`: model/runtime provenance and conversion checks. The native README's original shared-Unity-camera description is historical; app 1.15+ adds Camera2.
- `Docs/Full-body-avatar-mimicry-plan-reviewed.md`: initial proposal corrections and staged acceptance gates; some status sections are historical.
- `Docs/Standing-body-fix-delivery.md`, `Responsive-moves-delivery.md`, `MediaPipe-improvements-delivery.md`, `MediaPipe-idle-fix-delivery.md`: earlier changes and checks.
- `Docs/MediaPipeSessions/`: October recorded analyses and boxing/preview history.
- `Docs/Game-body-delivery.md`, `Game-body-replay-fixes.md`, `Smooth-game-body-delivery.md`: apps 1.15–1.17 implementation/validation.
- `Logs/MediaPipe/20261009-142232-221/`, `20261009-151451-407/`, `20261009-192549-873/`: archived inputs, reproducible metrics and session `review/Review.md`.
- `Logs/Practice/`: validation reports. These can be overwritten by later runs; archived per-session reviews retain historical results.

There is no Git metadata in this checkout. Selected pre-change files are backed up under `Docs/ImplementationBackups/`; these are not complete repository snapshots. Latest APK is `Builds/FitFight-smooth-body.apk`, app 1.17, version code 10. This handoff adds documentation only and does not change the implementation or produce another build.
