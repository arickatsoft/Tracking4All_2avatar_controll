# Standing and full-body fixes — 30 September 2026

Install `Builds/FitFight-standing-body-fix.apk` for the next phone test. The previous full-body APK is retained.

## Next test

1. Install this APK over the existing app. Select RTMPose and the same Remy fighter, then open Practice.
2. Start a phone screen recording with audio and tap **FULL BODY TEST**. Step back with your head and both feet visible and enough room for hands overhead and sideways.
3. Follow the spoken instructions. At the arms-down stage the app acquires a fresh standing reference at your final distance. If acquisition is incomplete, the guide waits and repeats a spoken framing/stillness hint. No second tap is needed after stepping back. REC remains actual elapsed time; movement cues can run later if calibration needs more time.
4. Complete the full-body guide, including both knee lifts and both gentle front extensions. Then enable punch assistance and run the spoken punch recording test.
5. Send both JSONL files and their corresponding screen recordings. The previous incorrectly performed video interval 2:22–2:32 was excluded from movement accuracy judgments; it was not used as a positive or a failure.

## Runtime changes

- Standing calibration requires a visible, confident head/hip/knee/ankle chain, upright straight legs, and consecutive still observations. Approach samples cannot define the full-body guide reference. The guide explicitly resets the previous reference at the arms-down stage.
- Standing shin projection now accepts the strong foreshortening in the supplied low-camera recording. The previous lower bound silently rejected those neutral shins, inventing depth even at rest.
- Lateral arm evidence limits the ambiguous forward-depth prior and avoids downward camera-ray correction. Legs retain the observed projected rise/side direction, with fixed segment lengths and standing projection normalization.
- Direct tracking controls Animator evaluation as well as exposed bone transforms. The rendered replay exposed cached humanoid skinning that ignored otherwise correct transforms. Fresh direct tracking disables automatic Animator evaluation; assistance, tracking loss, and unbinding restore it. Inactive punch/guard outputs release their animation weight. Renderer settings are restored on release.
- Torso bend uses hip width as its distance reference so shoulder narrowing cannot define forward bend. A bounded shoulder-asymmetry yaw prior removes side roll first. Head pitch normalization uses calibrated face width instead of amplifying a narrowing turned face.
- Punch candidates can promote from sweep to extension when later independent observations show substantial elbow straightening and reach gain. Cancellation reasons distinguish reversal, timeout and stopped preparation.
- Contact direction uses relative fist/bag point velocity and the capsule surface normal. Rejected separating contact preserves an unscored action; genuine geometry contact and once-per-action limits remain required. Full-body diagnostics suppress scoring for hands and feet.

## Validation

- 166 punch checks, including all three recorded phone fixtures (756, 703 and 733 independent observations), the previously missed 2863–2865 candidate transition, actual bilateral negatives, relative-motion rejection, subsequent valid contact and diagnostic foot scoring suppression.
- 69 guide/snapshot/recorder checks.
- 170 RTMPose checks, including noise, loss/recovery, limbs, torso/head and real GPU/model execution. The optional reference-person image fixture was unavailable and skipped.
- Actual Play mode: both assisted fists produce exactly one physical contact; recording timeout/manual stop/pause/Home and mode restoration pass.
- Recorded full-body observations at their captured cadence pass through the solver, publication, observation masks and actual Remy retargeter. Standing thigh/shin angles: median 2.6°, maximum 3.0° from down in the reviewed neutral window. Lateral arm median: 93.9° from up. Both reviewed knee lifts raise the reconstructed ankle near pelvis height. Applied arm/thigh directions differ by at most 0.03° from published targets. Side-bend forward prior median: 0.3°.
- Actual rendered standing, lateral arms and both knee lifts were captured across real player frames and visually reviewed. Cached-skinning errors were fixed before delivery. These checks do not supply independent measured 3D ground truth or certify physical latency.

## Recording schema 3

`practice-5-standing-geometry` adds reconstruction records with solver joints, confidence/observed masks, calibration scale/neutral projections/status, gravity pitch before clamping, measured camera/inference/pose rates and drop counters. Final poses add local positions/scales, world bone positions, applied targets/sequence and both foot contact samples. Guide time and accumulated calibration pause are logged separately from recording time. Diagnostic logging is optional and can produce larger files; its phone performance needs the next run.

## Limits

Monocular depth, shoulder yaw and head orientation remain approximate. The source recorded approximately 10 independent poses/s; this patch does not claim 20 poses/s or a new physical latency measurement. Physical tilt above the existing 45° clamp is now observable in logs, but has not been validated as an accurate camera model. New phone recordings are required to assess visual quality, framing, speech and sustained performance after these changes.

## Reports

- `Logs/Practice/recorded-body-fixes.txt` and `.csv`
- `Logs/Practice/recorded-fix-standing.png`, `recorded-fix-lateral-arms.png`, `recorded-fix-left-knee.png`, `recorded-fix-right-knee.png`
- `Logs/Practice/punch-validation.txt`, `full-body-validation.txt`, `punch-play-validation.txt`
- `Logs/RTMPose-validation.txt`
- `Logs/recorded-fixes-body.log`, `recorded-fixes-play.log`
- `Logs/standing-body-fix-android-build.log`, `standing-body-fix-build.txt`

Prior source versions are preserved in `Docs/ImplementationBackups/20260930-recorded-body-fixes/`. This project has no Git metadata.

## Android artifact

Build succeeded with zero errors and 731 reported project/compiler warnings. APK size: 261,438,464 bytes. SHA-256: `2b11dab5c35281f6401381dcb83e86e79902405f740d9159bad2925a3ab9380e`. ZIP integrity verification passed. The phone was not installed or launched by this task.
