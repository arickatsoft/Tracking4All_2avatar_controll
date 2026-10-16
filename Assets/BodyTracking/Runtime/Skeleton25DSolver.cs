using UnityEngine;

namespace FitFight.BodyTracking
{
    /// <summary>Experimental single-camera depth prior, not measured 3D. Pelvis-relative metric skeleton.</summary>
    public sealed class Skeleton25DSolver
    {
        private static readonly int[] Parents = { 5, 7, 6, 8, 11, 13, 12, 14 };
        private static readonly int[] Children = { 7, 9, 8, 10, 13, 15, 14, 16 };
        private static readonly float[] Proportions = { .186f, .146f, .186f, .146f, .245f, .246f, .245f, .246f };
        private readonly float[] lengths = new float[8];
        private readonly PoseJointFilter[] filters = new PoseJointFilter[26];
        private readonly Vector3[] measured = new Vector3[26];
        private readonly float[] depth = new float[8];
        private readonly Vector3[] plane = new Vector3[26];
        private readonly PoseJointFilter[] limbFilters = new PoseJointFilter[8];
        private readonly float[] acceptedScores = new float[26];
        private readonly BodyMotionEstimator bodyMotion = new BodyMotionEstimator();
        public Vector3 TorsoAngles => bodyMotion.TorsoAngles;
        public Vector3 HeadAngles => bodyMotion.HeadAngles;
        public bool JointObserved(int index) => filters[index].HasObservation;
        private readonly float[] neutralLegProjection = new float[4];
        private readonly int[] neutralLegSamples = new int[4];
        private float shoulderWidth, hipWidth;
        private float scale;
        private double lastObservation = double.NaN;
        private readonly float[] imageSpeeds = new float[26];
        private readonly Vector3[] previousMeasured = new Vector3[26];
        private int calibrationFrames;
        private double calibrationStarted = double.NaN, lastCalibrationSample = double.NegativeInfinity;
        public bool Calibrated => calibrationFrames >= 20;
        public int CalibrationFrames => calibrationFrames;
        public float CalibrationScale => scale;
        public float[] NeutralLegProjection => (float[])neutralLegProjection.Clone();
        public bool RequireStandingCalibration { get; set; }
        private Vector2[] calibrationPrevious;
        private double calibrationPreviousTime;
        public string CalibrationStatus { get; private set; } = "Waiting for standing reference";
        public static float CalibrationWindow(float interval) => Mathf.Clamp(interval * 28, 2.5f, 8);
        public static float CalibrationGap(float interval) => Mathf.Clamp(interval * 2.5f, .25f, .75f);
        public Skeleton25DSolver()
        {
            for (int i = 0; i < 26; i++) filters[i] = new PoseJointFilter();
            for (int i = 0; i < 8; i++) limbFilters[i] = new PoseJointFilter();
        }
        public static float AdvanceDepth(float current, float desired, float dt, float motionSpeed, RTMPoseTuning tuning)
            => Mathf.MoveTowards(current, desired, (tuning.depthSpeed + tuning.depthMotionGain * motionSpeed) * dt);
        public void Predict(double time, RTMPoseTuning tuning, Vector3[] result, float[] confidence, float processingDelay = 0)
        {
            for (int i = 0; i < 26; i++) result[i] = filters[i].Predict(time, tuning.prediction + processingDelay, tuning, out confidence[i]);
        }
        public void Reset()
        {
            calibrationPrevious = null;
            bodyMotion.Reset();
            foreach (var filter in limbFilters) filter.Reset();
            calibrationFrames = 0; scale = 0; lastObservation = double.NaN;
            calibrationStarted = double.NaN; lastCalibrationSample = double.NegativeInfinity;
            System.Array.Clear(depth, 0, depth.Length);
            System.Array.Clear(neutralLegProjection, 0, 4);
            System.Array.Clear(neutralLegSamples, 0, 4);
            shoulderWidth = hipWidth = 0;
            System.Array.Clear(previousMeasured, 0, previousMeasured.Length);
            for (int i = 0; i < 26; i++) filters[i].Reset();
        }
        private bool StandingReference(Vector2[] points, float[] scores, double time)
        {
            foreach (int j in new[] { 0, 5, 6, 11, 12, 13, 14, 15, 16 })
                if (scores[j] < .5f || points[j].x < .02f || points[j].x > .98f || points[j].y < .02f || points[j].y > .98f)
                { CalibrationStatus = "Step back: head and both feet must be visible"; return false; }
            for (int side = 0; side < 2; side++)
            {
                Vector2 thigh = points[13 + side] - points[11 + side], shin = points[15 + side] - points[13 + side];
                if (Vector2.Angle(thigh, Vector2.up) > 30 || Vector2.Angle(shin, Vector2.up) > 30 || Vector2.Angle(thigh, shin) > 25)
                { CalibrationStatus = "Stand upright with both feet down"; return false; }
            }
            bool still = calibrationPrevious != null && time - calibrationPreviousTime <= .3;
            if (still)
                foreach (int j in new[] { 5, 6, 11, 12, 13, 14, 15, 16 })
                    still &= Vector2.Distance(points[j], calibrationPrevious[j]) / Mathf.Max(.01f, (float)(time - calibrationPreviousTime)) < .35f;
            calibrationPrevious = (Vector2[])points.Clone(); calibrationPreviousTime = time;
            CalibrationStatus = still ? "Acquiring standing reference" : "Hold still with arms down";
            return still;
        }
        /// <summary>Approximate inverse perspective on a vertical body plane, not measured depth.
        /// The assumed vertical FOV can be adjusted for a wide front camera.</summary>
        public static Vector3 RectifyFloorPoint(Vector2 point, float aspect, float pitch, float verticalFov)
        {
            float focal = .5f / Mathf.Tan(Mathf.Clamp(verticalFov, 40, 90) * Mathf.Deg2Rad * .5f);
            var ray = Quaternion.Euler(-Mathf.Clamp(pitch, -45, 45), 0, 0)
                * new Vector3((point.x - .5f) * aspect / focal, (.5f - point.y) / focal, 1);
            return new Vector3(ray.x, ray.y, 0) / Mathf.Max(.25f, ray.z);
        }
        private void CaptureStandingLegs(float[] scores, RTMPoseTuning tuning)
        {
            for (int side = 0; side < 2; side++)
            {
                int hip = 11 + side, knee = 13 + side, ankle = 15 + side;
                if (!RTMPosePoseProvider.ReliableChain(scores[hip], scores[knee], scores[ankle], Mathf.Max(.5f, tuning.jointConfidence))) continue;
                Vector3 thigh = plane[knee] - plane[hip], shin = plane[ankle] - plane[knee];
                if (Vector3.Angle(thigh, Vector3.down) > 30 || Vector3.Angle(shin, Vector3.down) > 30
                    || Vector3.Angle(thigh, shin) > 25) continue;
                // Capture each side independently; missing ankles must not block torso calibration.
                // Freeze after 20 standing samples, so a later kick cannot redefine neutral.
                for (int segment = 0; segment < 2; segment++)
                {
                    int slot = side * 2 + segment;
                    if (neutralLegSamples[slot] >= 20) continue;
                    float observed = segment == 0 ? thigh.magnitude : shin.magnitude;
                    // A low camera strongly shortens the shin in the rectified body
                    // plane. Rejecting that neutral projection invents standing depth.
                    // Upright, straight, confident chain gates above establish reference.
                    if (observed < lengths[4 + slot] * .2f || observed > lengths[4 + slot] * 1.6f) continue;
                    neutralLegSamples[slot]++;
                    neutralLegProjection[slot] = Mathf.Lerp(neutralLegProjection[slot], observed, 1f / neutralLegSamples[slot]);
                }
            }
        }
        private static float EstimateDepth(Vector3 delta, float length, float branch, bool stable)
        {
            float depth = Mathf.Sqrt(Mathf.Max(0, length * length - delta.sqrMagnitude)) * branch;
            if (!stable) return depth;
            float contraction = 1 - delta.magnitude / length;
            return depth * Mathf.SmoothStep(0, 1, Mathf.Max(0, contraction - .02f) / .10f);
        }
        public bool Solve(Vector2[] points, float[] scores, float aspect, double time,
            float height, float pitch, float prediction, Vector3[] result, float[] confidence, RTMPoseTuning tuning = null)
        {
            tuning ??= new RTMPoseTuning();
            if (!Calibrated && RequireStandingCalibration && !StandingReference(points, scores, time))
            {
                // Acquisition must be consecutive; approach and moving samples never
                // contribute to scale, face or leg neutral references.
                if (calibrationFrames > 0) Reset();
                return false;
            }
            float dt = double.IsNaN(lastObservation) ? 1f / 60 : Mathf.Clamp((float)(time - lastObservation), .001f, .2f);
            // Collect 20 confident observations within a short window. A single noisy frame
            // pauses acquisition; prolonged loss or scattered background detections reset it.
            if (!Calibrated && calibrationFrames > 0
                && (time - lastCalibrationSample > CalibrationGap(tuning.observationInterval)
                    || time - calibrationStarted > CalibrationWindow(tuning.observationInterval))) Reset();
            float torsoThreshold = Calibrated ? tuning.jointConfidence : Mathf.Max(.5f, tuning.jointConfidence);
            if (scores[5] < torsoThreshold || scores[6] < torsoThreshold || scores[11] < torsoThreshold || scores[12] < torsoThreshold)
            {
                // Do not add weak measurements to the scale estimate.
                return false;
            }
            for (int i = 0; i < 26; i++)
                plane[i] = tuning.uprightBody
                    ? RectifyFloorPoint(points[i], aspect, pitch, tuning.cameraVerticalFov)
                    : new Vector3(points[i].x * aspect, -points[i].y, 0);
            var hips = (plane[11] + plane[12]) * .5f;
            var shoulder = (plane[5] + plane[6]) * .5f;
            float torso = (shoulder - hips).magnitude;
            if (torso < .04f) return false;
            if (!Calibrated)
            {
                if (calibrationFrames == 0) calibrationStarted = time;
                lastCalibrationSample = time;
                float sample = height * .29f / torso;
                scale = calibrationFrames == 0 ? sample : Mathf.Lerp(scale, sample, 1f / (calibrationFrames + 1));
                calibrationFrames++;
                for (int i = 0; i < lengths.Length; i++) lengths[i] = height * Proportions[i];
            }
            var orientation = tuning.uprightBody ? Quaternion.identity : Quaternion.Euler(-pitch, 0, 0);
            // Express movement in an upright torso frame. Tilt is a camera property,
            // never an instruction to lean the entire avatar away from the floor.
            // Keep screen-space limb directions. Only the calibrated torso/head estimator
            // removes static camera bias; do not normalize away every deliberate body lean.
            var bodyFrame = Quaternion.identity;
            for (int i = 0; i < 26; i++)
            {
                plane[i] = bodyFrame * ((plane[i] - hips) * scale);
                measured[i] = plane[i];
                imageSpeeds[i] = double.IsNaN(lastObservation) ? 0 : (measured[i] - previousMeasured[i]).magnitude / dt;
                previousMeasured[i] = measured[i];
            }
            System.Array.Copy(scores, acceptedScores, 26);
            if (tuning.uprightBody)
            {
                bodyMotion.Observe(plane, scores, time, !Calibrated, tuning);
                if (shoulderWidth == 0 || !Calibrated)
                {
                    float weight = 1f / Mathf.Max(1, calibrationFrames);
                    shoulderWidth = Mathf.Lerp(shoulderWidth, Mathf.Clamp(Mathf.Abs(plane[6].x - plane[5].x), height * .15f, height * .26f), weight);
                    hipWidth = Mathf.Lerp(hipWidth, Mathf.Clamp(Mathf.Abs(plane[12].x - plane[11].x), height * .1f, height * .2f), weight);
                }
                float sign = plane[6].x >= plane[5].x ? 1 : -1;
                measured[11] = Vector3.left * (sign * hipWidth * .5f);
                measured[12] = -measured[11];
                // In solver coordinates forward is -Z. Published avatar coordinates invert Z.
                Quaternion bodyRotation = Quaternion.Euler(-TorsoAngles.x, -TorsoAngles.y, TorsoAngles.z);
                measured[5] = bodyRotation * new Vector3(-sign * shoulderWidth * .5f, height * .29f, 0);
                measured[6] = bodyRotation * new Vector3(sign * shoulderWidth * .5f, height * .29f, 0);
                CaptureStandingLegs(scores, tuning);
            }
            // Both straight arms visibly above the head provide stronger overhead
            // evidence than the ambiguous forward-depth prior of a single wrist.
            bool overhead = scores[0] >= .5f;
            float imageTorso = ((points[11] + points[12] - points[5] - points[6]) * .5f).magnitude;
            for (int side = 0; side < 2; side++)
            {
                Vector2 upperImage = points[7 + side] - points[5 + side];
                Vector2 lowerImage = points[9 + side] - points[7 + side];
                upperImage.x *= aspect; lowerImage.x *= aspect;
                overhead &= Mathf.Min(scores[5 + side], Mathf.Min(scores[7 + side], scores[9 + side])) >= .5f &&
                    points[7 + side].y < points[5 + side].y - imageTorso * .15f &&
                    points[9 + side].y < points[0].y - imageTorso * .1f && Vector2.Angle(upperImage, lowerImage) < 35;
            }
            for (int i = 0; i < Parents.Length; i++)
            {
                int a = Parents[i], b = Children[i];
                float chainScore = Mathf.Min(acceptedScores[a], scores[b]);
                // Filter a bone vector before the square-root depth reconstruction amplifies noise.
                Vector3 observedDelta = limbFilters[i].Step(plane[b] - plane[a], chainScore, time, 0, tuning, out _);
                if (!limbFilters[i].HasObservation)
                { acceptedScores[b] = 0; continue; }
                // Use original endpoints, not the already reconstructed parent. Otherwise
                // clamping a thigh changes the shin's measured direction as a side effect.
                Vector3 delta = observedDelta; delta.z = 0;
                float length = lengths[i];
                if (tuning.uprightBody && i >= 4 && neutralLegSamples[i - 4] > 0)
                    delta *= length / neutralLegProjection[i - 4];
                float branch = -1;
                if (tuning.uprightBody && (i == 5 || i == 7))
                {
                    int hip = i == 5 ? 11 : 12;
                    float ankleDrop = (plane[hip].y - plane[b].y) / (lengths[i - 1] + lengths[i]);
                    branch = Mathf.Lerp(-1, 1, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, .8f, ankleDrop)));
                }
                Vector3 projectedDelta = delta;
                float desired = EstimateDepth(delta, length, branch, tuning.uprightBody);
                bool lateralArm = i < 4 && Mathf.Abs(delta.x) > delta.magnitude * .65f;
                bool planarArm = lateralArm || (i < 4 && overhead);
                if (tuning.uprightBody && i < 4)
                {
                    int side = i / 2;
                    Vector3 upper = plane[7 + side] - plane[5 + side], lower = plane[9 + side] - plane[7 + side];
                    // A coherent lateral chain is strong planar evidence. Inferring
                    // forward depth here used to pull a captured T pose below the shoulder.
                    if (overhead) desired = 0;
                    else if (lateralArm && Vector3.Angle(upper, lower) < 30)
                        desired *= Mathf.InverseLerp(10, 30, Vector3.Angle(upper, lower));
                }
                if (tuning.uprightBody && i < 4 && !planarArm)
                {
                    // A point moving toward a low camera grows upward in its image.
                    // Account for the estimated depth along that camera ray, rather than
                    // treating the apparent rise as a real overhead punch. Bounded fixed
                    // iterations keep this inexpensive; this remains a monocular prior.
                    Vector3 parentRay = plane[a] / scale + hips, childRay = plane[b] / scale + hips;
                    for (int iteration = 0; iteration < 4; iteration++)
                    {
                        delta = projectedDelta + childRay * (measured[a].z + desired) - parentRay * measured[a].z;
                        delta.z = 0;
                        desired = EstimateDepth(delta, length, branch, true);
                    }
                    depth[i] = AdvanceDepth(depth[i], desired, dt, imageSpeeds[b], tuning);
                    delta = projectedDelta + childRay * (measured[a].z + depth[i]) - parentRay * measured[a].z;
                    delta.z = 0;
                }
                else depth[i] = AdvanceDepth(depth[i], desired, dt, imageSpeeds[b], tuning);
                if (tuning.uprightBody && (i >= 4 || planarArm))
                {
                    // Preserve observed rise/side motion. Ambiguous camera-ray depth
                    // must not reverse a knee above its hip or turn a side raise down.
                    float planarLength = Mathf.Sqrt(Mathf.Max(0, length * length - depth[i] * depth[i]));
                    delta = delta.normalized * planarLength;
                }
                delta = Vector3.ClampMagnitude(delta, length);
                measured[b] = measured[a] + new Vector3(delta.x, delta.y, depth[i]);
            }
            // Preserve the observed foot direction while carrying the ankle depth.
            for (int side = 0; side < 2; side++)
                for (int foot = 20 + side; foot <= 24 + side; foot += 2)
                {
                    int ankle = 15 + side;
                    acceptedScores[foot] = Mathf.Min(acceptedScores[ankle], scores[foot]);
                    if (tuning.uprightBody)
                    {
                        // Body26 supplies foot keypoints, but no measured foot depth.
                        // Keep toes forward instead of aiming shoes vertically at the image plane.
                        Vector3 offset = plane[foot] - plane[ankle];
                        measured[foot] = measured[ankle] + new Vector3(Mathf.Clamp(offset.x, -.08f, .08f),
                            Mathf.Clamp(offset.y, -.025f, .025f), foot >= 24 ? .04f : -.14f);
                    }
                    else measured[foot].z = measured[ankle].z;
                }
            for (int i = 0; i < 26; i++)
            {
                result[i] = filters[i].Step(orientation * measured[i], acceptedScores[i], time, prediction, tuning, out confidence[i]);
            }
            lastObservation = time;
            return true;
        }
    }
}
