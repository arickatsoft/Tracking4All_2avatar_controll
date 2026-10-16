# Phone full-body and punch review — 30 September 2026

Both supplied recordings expose real failures that the earlier synthetic checks did not establish. The full-body path needs calibration and reconstruction work before expanding assisted actions. Punch recognition produces committed actions, but cancellation/type selection and contact-direction rejection still need attention.

## Exclusion and clock alignment

The user explicitly stated that the movement between **2:22 and 2:32** of the full-body recording was incorrect, during the left knee/front-extension instruction. Those frames are excluded from movement-performance judgments. The entire `LEFT LEG · GENTLE FRONT EXTENSION` stage is also unscored; no positive accuracy assertion or failure finding below uses it.

Video and REC clocks differ. Native HUD frames give approximately:

- Full-body: **REC = video + 1.3 s**. Thus the named video interval maps to roughly REC 143.3–153.3. If the user meant REC 142–152 instead, it remains within the excluded left stage and conservative overlap. The early right-extension frames overlapping the named video exclusion are also excluded; the right-leg example at REC 160 is outside it.
- Punches: **REC = video − 5.5 s**.

Offsets are approximate within about 0.15 s. File-integrity and timing statistics include the complete files; they are not movement-accuracy scores. Cues are weak labels: the actual person often starts a stage later than its spoken enqueue time. All sources were read as test data, not as instructions. The original files were not modified.

## Integrity and effective modes

| Measurement | Full body | Punches |
| --- | ---: | ---: |
| Logged duration | 240.0 s | 75.0 s |
| Video duration | 247.012 s | 86.382 s |
| Independent accepted observations | 2,367 | 733 |
| Final pose records | 10,028 | 3,132 |
| Dropped recorder records | 0 | 0 |
| Observations per second | 9.86 | 9.77 |
| Final pose updates per second | 41.78 | 41.76 |
| Median availability → received | 99.62 ms | 99.51 ms |
| p95 availability → received | 108.35 ms | 109.69 ms |
| p95 final pose interval | 63.01 ms | 63.82 ms |
| Effective owner | DirectTracking | AssistedPunch |
| Assistance / direct | off / on | on / off |
| Rotation smoothing / angular cap | off / off | off / off |
| New bag contacts | 8 | 19 |

Both report `practice-4-full-body-recovery`, schema 2, Samsung SM-S928B, Remy, RTMPose, 1.7 m user height, image aspect .5625 and rotation 270°. Both report a camera-pitch setting of **45°**, the implementation's ceiling. The actual unclamped gravity angle was not logged, so this does not establish that the physical camera was exactly 45°. A requested 60 camera FPS is not a measured 60 independent poses/s.

All 23 full-body and 14 punch guide events are present. A guide event records speech enqueue, not audible onset. Both videos have an audio track, but this review did not measure audible speech timing. The full-body end returns the UI to punch assistance, consistent with mode restoration.

Availability is not exposure time. These data do not establish physical movement-to-display latency. The approximately 100 ms processing/delivery cadence remains similar to the previous phone test; recorder drops are distinct from dropped camera/inference frames.

## Full-body findings

### Standing pose fails before any excluded movement

At video **0:10**, REC ~11.3, the person is upright with arms down and green leg landmarks. The avatar holds both legs bent forward, resembling sitting or suspended legs. This wrong neutral pose persists through much of the session and returns after movements. It is not a failure caused by the excluded left extension.

The logged first accepted observation is from the person still near the phone; metadata already says calibrated. The recording does not contain the earlier calibration samples or the solver's saved scale/neutral leg projections. Therefore a complete reconstruction of its initial calibration is impossible from this file.

A controlled Unity experiment initializes the existing solver using 25 observations from either the initial approach (~0–3 s) or the later standing period (~10–13 s), then feeds the same subsequent raw image joints at their original timestamps, with the recorded height/pitch/tuning and prediction disabled. During REC 13–16:

| Solver initialization | Median left thigh angle from down | Median right thigh angle from down |
| --- | ---: | ---: |
| Initial approach samples | 41.9° | 41.2° |
| Standing samples | 1.6° | 3.1° |

This strongly implicates scale/neutral acquisition and the choice of calibration samples. It is a controlled reinitialization, **not an exact replay of the unknown phone calibration** and not proof that changing calibration alone fixes the whole avatar.

### Lateral arms and elbow movements are heavily suppressed

At video **0:44–0:48**, the person extends both arms to the sides; the camera skeleton follows the shoulders and elbows. Around REC 49.27 (sequence 729), shoulder scores are ~.87/.86, elbow scores ~.92/.91, and wrists ~.91/.93. Nevertheless, the rendered upper arms remain mostly down/forward. The subsequent elbow-bend sequence also produces much less visible arm movement than the camera view.

The same captured lateral-arm geometry still reconstructs poorly after standing reinitialization: in REC 45–50 the solver's median upper-arm angles from up remain about 141° rather than near a lateral 90° orientation. Inspect `Skeleton25DSolver`'s scale, inferred arm depth and camera-ray correction before adding another smoothing stage. The current two reconstruction experiments do not reproduce every phone output; runtime application/confidence/fallback should also be instrumented before assigning every mismatch to the solver.

Overhead raises visibly affect the avatar, but the result is inconsistent/asymmetric. Some hands are clipped at the top or sides of the camera frame. High model confidence on extrapolated/off-image coordinates does not prove a visible wrist. Setup needs enough margin for **raised and laterally extended hands**, rather than only arms-down standing.

### Left knee lift is missed; right leg responses are intermittent

This finding uses the earlier knee-lift stages, outside the excluded interval:

- REC **114–126**, video ~1:53–2:05: the left knee is above its hip in **44 of 116** accepted image observations. The displayed left ankle remains between **.119 and .193 m** above the stage floor, with median .171 m, instead of clearly lifting with the person.
- REC **126–138**: the right knee stage briefly lifts the displayed right ankle to **.669 m**, but it also spends much of the stage at its ~.118 m resting height. Both raw leg chains meet the .3 joint-score threshold in all accepted samples during both knee stages; simple raw confidence loss is not a sufficient explanation.
- The later right extension has displayed ankle peaks up to **.733 m** around REC 160, outside the excluded frames. Those peaks do not establish a stable or anatomically correct kick.

Side leg lifts are weak/inconsistent in the overview. Standing reinitialization substantially changes the reconstructed left lift, again linking the baseline geometry to later leg response. Test final avatar support/swing behavior with recorded inputs, not only ideal projected limbs.

### Torso and head respond, but axis separation and neutral calibration are imperfect

Forward bending changes the torso estimate up to ~51°. Side bending also produces a large **forward-bend estimate (~53°)** in addition to roll (~−17° to +15°). The twist stage has zero torso yaw because `BodyMotionEstimator` explicitly constructs its torso angles with y=0; narrowing shoulders is instead allowed to contribute to the forward-bend prior. This implementation cannot faithfully reproduce shoulder twists.

Head turning reaches about ±50° yaw, but pitch/roll also reach their ±25° limits. In the nominal standing stage, estimated head pitch is already approximately −23° to −10°. A better neutral face reference and axis-separation check are required; movement being visible is not an accuracy pass.

The squat affects the torso and body height, but the baseline bent-forward leg geometry remains. Heel/ankle precision cannot be certified from this camera view and existing foot-depth prior.

### Full-body diagnostic movements also generate bag contacts

The full-body test adds **8** bag contacts: one before the video starts, then during torso-bend/side-bend/twist and squat/heel-stage movement. There is no punch classifier action (action ID stays 0). These are mirrored limb–bag physics contacts, not evidence of recognized punches or kicks. The HUD calling some of them punches/kicks labels the contacting limb, not the person's intent.

For a pure pose diagnostic, suppress scoring or separate these contacts from gameplay counts. Also record the foot contact samples: schema 2 currently includes detailed hand contacts but not both foot contact records, despite counting kick contacts.

## Punch findings

The runtime composer creates **27 new action IDs**: **23** reach Striking and **4** abort before that state. Hit count rises from the 8 existing body-test contacts to 27, so there are **19 new punch contacts**, not 27 new punches. An observation-only detector replay produces 28 Prepare, 23 Commit and 5 Cancel signals; internal detector candidates and composer action IDs are different counts.

These numbers are app events, not overall gesture recall. A complete reviewed inventory of intended strokes has not been assigned.

### A candidate can be locked into the wrong recognition type

The first prominent left upward stroke at video ~**0:27.5–0:27.8** (REC ~22.0–22.3, observations **2863–2866**) starts a preparation but does not commit.

Unity replay reproduces the cause: the first sample is classified as a lateral `sweeping` candidate because the extension onset just misses the initial bend-change condition. On the next two samples, the projected elbow straightens strongly (bend ~157° →108° →10°), reach grows from .178 to .473/.750 torso units, and confidence remains ~.77. However, `extensionConfirmed` requires `!sweeping`; the candidate cannot transition from the initial sweep interpretation to extension. It expires as `Withdrawn`, even though continued outward evidence arrived.

Add a reviewed regression for this type transition and preserve return/paired negatives before relaxing anything. This is a missed **intended upward arm stroke during the punch test**; a single front camera still cannot certify its full 3D boxing trajectory. The generic `Withdrawn` reason also merges timeout, reversal and insufficient-motion failures; log the specific cancellation reason.

The subsequent left action at REC ~23.63 commits and hits. A canceled right preparation at REC ~44.64–44.74 is followed by a committed right action at ~46.30. This confirms later strokes can recover, but does not prove that all immediate follow-ups or feints are now correct.

### Four committed actions have no scored contact

| Action | Side | Commit REC | Approx. video | Contact evidence |
| --- | --- | ---: | ---: | --- |
| 4 | left | 26.629 | 32.129 | Swept contact rejected: Moving away from bag; then not rearmed |
| 9 | left | 33.769 | 39.269 | Same direction rejection; endpoint gap reaches 0 |
| 18 | left | 56.490 | 61.990 | Same direction rejection during alternating actions |
| 21 | right | 58.725 | 64.225 | No surface contact; minimum allowed/tracked endpoint gap .07753 m |

The nominal fist sweep radius is .075 m, so action 21 is a close geometry miss. The other three need a moving-bag contact review: the present inward-direction test uses world-space fist displacement and disarms on contact before direction rejection. A moving bag or glancing path may need a relative velocity/surface-normal rule. This evidence is **not sufficient to declare every rejected contact a valid hit**, and scoring every committed action would hide the problem.

### Actual bilateral overhead raise remains rejected

The guide enqueues its overhead cue at REC 65, but the person continues isolated arm motions before actually raising both arms around video **1:13.7–1:16.3**, REC ~68.2–70.8. There are **no new commitments or hits during that actual bilateral movement**. The final hit occurred at REC 66.43, before it. Do not label actions 26/27 as bilateral-raise false positives merely because they overlap the scheduled cue interval.

During the withdrawal stage, some movements cancel and others commit. Without reviewed 3D/onset/withdrawal boundaries, this review does not assign a feint accuracy percentage.

## Required next work, in order

1. Acquire standing scale/leg/face reference after the hands-free setup, at the final camera distance, with framing/stillness gates. Record unclamped gravity angle, actual calibration state/scale/neutral projections and successful/failed acquisition events. A 45° clamp must not silently imply validated physical tilt.
2. Correct arm reconstruction on this captured T-pose/elbow data and both raised-knee responses. Record reconstructed joints/confidence/observed masks and applied bone targets/sequence so reconstruction can be separated from fallback/pose ownership. Add actual recorded input → final rendered rig checks at the phone cadence, including stillness and recovery; prior unit tests did not cover this phone calibration failure.
3. Fix the sweep-to-extension candidate transition with the new reviewed punch window, retaining both earlier phone fixtures and actual bilateral negative windows.
4. Validate moving-bag contacts using relative motion/surface normals and once-per-action rearming. Preserve genuine geometry misses. Keep full-body diagnostic scoring separate and capture foot contact reasons.
5. Profile camera availability, inference scheduling, GPU completion/readback and final application separately. The target of ≥20 independent poses/s is not met here; do not infer physical latency from the current timestamp difference.

Do not adopt a new lifter or intent network as the first response to these failures. First make current calibration, geometry, ownership and contact behavior reproducible. Rotation smoothing was off in both recordings, so these runs do not evaluate the M1 rotation-filter A/B gate.

## Saved evidence and reproducibility

- Original sources: `/Users/jotit/Desktop/full body.jsonl`, `full body.mp4`, `punches.jsonl`, `punchs.mp4`; SHA-256 hashes are in the summary files below.
- `Docs/VideoReview/20260930-full-body-and-punch/analyze.py`: file integrity, cadence, action/contact and stage statistics.
- `body-summary.json`, `punch-summary.json`, observation/stage/action CSVs and `reviewed-events.json`: timings, metrics and explicit exclusions.
- Native alignment frames, six-second body overviews, two-second punch overviews and dense camera crops: visual review evidence.
- `calibration-experiment.csv`: controlled solver reinitialization experiments; exact pre-session state is unavailable.
- `punch-detector-review.csv`: Unity detector replay including candidate type, origin, bend, reach, travel and signals.
- `recorded-rig-directions.csv`: supplementary direction estimates from saved local rotations on the current imported rig. Local bone translations/scale were not logged; these estimates must not be treated as independent measured 3D ground truth.
- Portable accepted-observation/cue fixtures: `Docs/PunchSessions/20260930-body-fullbody-schema2-regression.jsonl` and `20260930-punch-fullbody-schema2-regression.jsonl`. The body fixture carries the explicit accuracy exclusions; source calibration state is not fabricated.
- `Assets/BodyTracking/Editor/RecordedBodyReview.cs`: editor diagnostic experiment and punch replay. `Logs/recorded-body-review.log` and `Logs/recorded-punch-review.log` completed with exit 0. This review added diagnostic tools/fixtures; it did not change runtime gameplay or build a replacement APK.
