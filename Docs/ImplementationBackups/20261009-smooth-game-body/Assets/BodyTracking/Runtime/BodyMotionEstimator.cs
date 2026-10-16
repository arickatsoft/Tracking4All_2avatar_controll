using UnityEngine;

namespace FitFight.BodyTracking
{
    /// <summary>Calibrated, bounded motion from monocular torso/face geometry. Forward bend is a prior, not measured 3D.</summary>
    public sealed class BodyMotionEstimator
    {
        private Vector3 neutralTorso, neutralFace;
        private float neutralWidth;
        private float neutralHipWidth, neutralFaceWidth, neutralShoulderSlope;
        private int torsoSamples, faceSamples;
        private readonly PoseJointFilter torsoFilter = new PoseJointFilter(), headFilter = new PoseJointFilter();
        public Vector3 TorsoAngles { get; private set; }
        public Vector3 HeadAngles { get; private set; }
        public void Reset()
        {
            neutralTorso = neutralFace = TorsoAngles = HeadAngles = Vector3.zero;
            neutralWidth = neutralHipWidth = neutralFaceWidth = neutralShoulderSlope = 0; torsoSamples = faceSamples = 0; torsoFilter.Reset(); headFilter.Reset();
        }
        public void Observe(Vector3[] plane, float[] scores, double time, bool calibrating, RTMPoseTuning tuning)
        {
            Vector3 torso = (plane[5] + plane[6] - plane[11] - plane[12]) * .5f;
            float width = Mathf.Max(.001f, Vector3.Distance(plane[5], plane[6]));
            float torsoScore = Mathf.Min(scores[5], scores[6], scores[11], scores[12]);
            float hipWidth = Mathf.Max(.001f, Vector3.Distance(plane[11], plane[12]));
            if (calibrating || torsoSamples == 0)
            {
                torsoSamples++; float w = 1f / torsoSamples;
                neutralTorso = Vector3.Lerp(neutralTorso, torso, w); neutralWidth = Mathf.Lerp(neutralWidth, width, w);
                neutralHipWidth = Mathf.Lerp(neutralHipWidth, hipWidth, w);
                neutralShoulderSlope = Mathf.Lerp(neutralShoulderSlope, (plane[6].y-plane[5].y) / Mathf.Max(.001f, Mathf.Abs(plane[6].x-plane[5].x)), w);
            }
            // Hips provide a distance reference independent of shoulder twist.
            float ratio = torso.magnitude / Mathf.Max(.001f, neutralTorso.magnitude)
                / Mathf.Max(.25f, hipWidth / Mathf.Max(.001f, neutralHipWidth));
            // Ignore small contraction; ease in the ambiguous depth prior rather than
            // using sqrt/acos at full gain near a straight standing pose.
            float contraction = Mathf.Max(0, 1 - ratio);
            float bend = Mathf.Acos(Mathf.Clamp01(ratio)) * Mathf.Rad2Deg
                * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.04f, .20f, contraction));
            float roll = Vector3.SignedAngle(neutralTorso, torso, Vector3.forward);
            Vector3 shoulderLine = Quaternion.Euler(0, 0, -roll) * (plane[6] - plane[5]);
            float slope = shoulderLine.y / Mathf.Max(.001f, Mathf.Abs(shoulderLine.x)) - neutralShoulderSlope;
            // Low-camera shoulder height asymmetry supplies a bounded yaw prior;
            // remove side roll first so a side bend is not also a twist.
            float yaw = Mathf.Clamp(Mathf.Atan(slope * 2) * Mathf.Rad2Deg, -45, 45);
            var angles = new Vector3(Mathf.Clamp(bend, 0, tuning.maxBodyBend), yaw, Mathf.Clamp(roll, -35, 35));
            TorsoAngles = torsoFilter.Step(angles / 90, torsoScore, time, 0, tuning, out _, tuning.bodyNoiseDegrees / 90) * 90;

            // Ear and eye geometry is used only when every required face point is visible.
            float faceScore = Mathf.Min(scores[0], scores[1], scores[2], scores[3], scores[4]);
            Vector3 ears = (plane[3] + plane[4]) * .5f, eyes = (plane[1] + plane[2]) * .5f;
            float faceWidth = Vector3.Distance(plane[3], plane[4]);
            if (faceWidth < .015f) faceScore = 0;
            Vector3 face = new Vector3((plane[0].y - eyes.y) / Mathf.Max(faceWidth, .015f),
                (plane[0].x - ears.x) / Mathf.Max(faceWidth, .015f),
                Mathf.Atan2(plane[4].y - plane[3].y, Mathf.Abs(plane[4].x - plane[3].x)) * Mathf.Rad2Deg / 90);
            if ((calibrating || faceSamples < 8) && faceScore >= Mathf.Max(.5f, tuning.jointConfidence))
            { faceSamples++; neutralFace = Vector3.Lerp(neutralFace, face, 1f / faceSamples); neutralFaceWidth = Mathf.Lerp(neutralFaceWidth, faceWidth, 1f / faceSamples); }
            // A turning face narrows in the image; dividing pitch by that narrowed
            // width amplifies an ordinary head turn into the pitch limit.
            face.x *= faceWidth / Mathf.Max(.015f, neutralFaceWidth);
            Vector3 change = face - neutralFace;
            angles = faceSamples == 0 ? Vector3.zero : new Vector3(Mathf.Clamp(-change.x * 100, -25, 25),
                Mathf.Clamp(-change.y * 100, -50, 50), Mathf.Clamp(change.z * 90 - roll, -25, 25));
            var filteredHead = headFilter.Step(angles / 90, faceSamples == 0 ? 0 : faceScore,
                time, 0, tuning, out float headConfidence, tuning.bodyNoiseDegrees / 90) * 90;
            // Brief loss holds still. Prolonged loss returns gently to neutral rather than snapping.
            HeadAngles = headConfidence > 0 ? filteredHead : Vector3.MoveTowards(HeadAngles, Vector3.zero,
                75 * Mathf.Clamp(tuning.observationInterval, .001f, .2f));
        }
    }
}
