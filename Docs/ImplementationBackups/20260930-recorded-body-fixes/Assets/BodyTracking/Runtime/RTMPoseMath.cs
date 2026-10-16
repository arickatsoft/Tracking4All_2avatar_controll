using System;
using UnityEngine;

namespace FitFight.BodyTracking
{
    public static class RTMPoseMath
    {
        public const int JointCount = 26, Width = 192, Height = 256;
        // Halpe26: COCO17, head, neck, hip, left/right big toe, left/right small toe, left/right heel.
        public static readonly int[] ToMediaPipe = {
            0, 1, 1, 1, 2, 2, 2, 3, 4, 0, 0, 5, 6, 7, 8, 9, 10,
            9, 10, 9, 10, 9, 10, 11, 12, 13, 14, 15, 16, 24, 25, 20, 21
        };
        public static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);

        public static void Decode(float[] x, float[] y, Rect roi, Vector2[] points, float[] scores)
        {
            if (x.Length != JointCount * Width * 2 || y.Length != JointCount * Height * 2)
                throw new ArgumentException("Expected Body26 SimCC outputs [1,26,384] and [1,26,512].");
            for (int j = 0; j < JointCount; j++)
            {
                int ix = Maximum(x, j * Width * 2, Width * 2, out float sx);
                int iy = Maximum(y, j * Height * 2, Height * 2, out float sy);
                scores[j] = Mathf.Clamp01(Mathf.Min(sx, sy));
                // Official split ratio 2. Output coordinates are in the upright, unmirrored image.
                points[j] = new Vector2(roi.x + ix / (2f * Width) * roi.width,
                    roi.y + iy / (2f * Height) * roi.height);
            }
        }
        private static int Maximum(float[] values, int offset, int count, out float score)
        {
            score = float.NegativeInfinity;
            int index = 0;
            for (int i = 0; i < count; i++)
                if (Finite(values[offset + i]) && values[offset + i] > score)
                { score = values[offset + i]; index = i; }
            if (!Finite(score)) score = 0;
            return index;
        }

        public static Rect FitAspect(Rect box, float imageAspect)
        {
            float target = (Width / (float)Height) / imageAspect;
            float width = Mathf.Max(box.width, .1f), height = Mathf.Max(box.height, .1f);
            if (width / height > target) height = width / target;
            else width = height * target;
            return new Rect(box.center.x - width * .5f, box.center.y - height * .5f, width, height);
        }
        public static Rect TrackROI(Vector2[] points, float[] scores, float aspect)
        {
            Vector2 min = Vector2.one, max = Vector2.zero;
            int valid = 0;
            for (int i = 0; i < JointCount; i++)
            {
                if (scores[i] < .4f) continue;
                valid++;
                min = Vector2.Min(min, points[i]); max = Vector2.Max(max, points[i]);
            }
            if (valid < 8 || scores[5] < .4f || scores[6] < .4f || scores[11] < .4f || scores[12] < .4f)
                return FitAspect(new Rect(0, 0, 1, 1), aspect);
            var size = Vector2.Max((max - min) * 1.5f, new Vector2(.25f, .4f));
            // More room than a static pose crop for fast wrists and ankles.
            var center = (max + min) * .5f;
            return FitAspect(new Rect(center - size * .5f, size), aspect);
        }
    }

    /// <summary>One Euro filtering with motion-onset response and stop/reversal-aware prediction.</summary>
    public sealed class PoseJointFilter
    {
        private Vector3 position, previous, velocity, rawVelocity, candidate;
        private double lastValid, candidateAt = double.NegativeInfinity;
        private float lastConfidence;
        private bool initialized, observed;
        public bool HasObservation => observed;
        private static readonly RTMPoseTuning Defaults = new RTMPoseTuning();
        public void Reset() { initialized = observed = false; velocity = rawVelocity = Vector3.zero; candidateAt = double.NegativeInfinity; }
        public Vector3 Step(Vector3 measured, float confidence, double time, float lead, out float usable)
            => Step(measured, confidence, time, lead, Defaults, out usable);
        public Vector3 Step(Vector3 measured, float confidence, double time, float lead, RTMPoseTuning tuning, out float usable,
            float noiseOverride = -1)
        {
            bool acquiring = !initialized || time - lastValid > tuning.TrackingGrace;
            float threshold = tuning.jointConfidence + (acquiring ? tuning.confidenceHysteresis : 0);
            observed = confidence >= threshold && RTMPoseMath.Finite(measured);
            if (tuning.filtering && observed && initialized && !acquiring && time > lastValid)
            {
                float dt = Mathf.Clamp((float)(time - lastValid), .001f, .2f);
                // Quarantine only anatomically implausible teleports, not normal fast punches.
                float jump = Vector3.Distance(measured, previous);
                if (jump > Mathf.Max(.35f, 12 * dt) && !(time - candidateAt <= .25 && Vector3.Distance(measured, candidate) < 12 * dt))
                { candidate = measured; candidateAt = time; observed = false; }
            }
            if (observed)
            {
                if (!initialized)
                { position = previous = measured; velocity = rawVelocity = Vector3.zero; initialized = true; }
                else if (time > lastValid)
                {
                    float dt = Mathf.Clamp((float)(time - lastValid), .001f, .2f);
                    if (acquiring) { velocity = rawVelocity = Vector3.zero; previous = position; }
                    float reliability = Mathf.InverseLerp(tuning.jointConfidence, Mathf.Min(1, tuning.jointConfidence + .3f), confidence);
                    float radius = (noiseOverride >= 0 ? noiseOverride : tuning.noiseRadius) * Mathf.Lerp(2, 1, reliability);
                    float distance = Vector3.Distance(measured, position);
                    // Anchored spatial dead zone: tiny movement cannot build velocity. Slow
                    // deliberate motion still accumulates against the held position and escapes.
                    float response = !tuning.filtering || radius <= 0 ? 1 : Mathf.SmoothStep(0, 1, Mathf.InverseLerp(radius, radius * 3, distance));
                    Vector3 target = Vector3.Lerp(position, measured, response);
                    Vector3 speed = Vector3.ClampMagnitude((target - previous) / dt, 12f);
                    bool reversing = Vector3.Dot(speed, rawVelocity) < 0;
                    velocity = Vector3.Lerp(velocity, speed, Alpha(tuning.velocityCutoff, dt));
                    if (response == 0 || speed.magnitude < .08f) velocity = Vector3.zero;
                    else if (reversing) velocity = speed * response; // only the newly observed direction
                    float cutoff = tuning.minCutoff + tuning.speedCoefficient * Mathf.Max(velocity.magnitude, speed.magnitude * reliability);
                    position = tuning.filtering ? Vector3.Lerp(position, target, Alpha(cutoff, dt)) : measured;
                    rawVelocity = speed; previous = target;
                }
                lastValid = time; lastConfidence = confidence;
            }
            else velocity = rawVelocity = Vector3.zero; // missing limbs never continue a punch/kick
            return Predict(time, lead, tuning, out usable);
        }
        public Vector3 Predict(double time, float lead, RTMPoseTuning tuning, out float usable)
        {
            float age = Mathf.Max(0, (float)(time - lastValid));
            usable = initialized && age <= tuning.TrackingGrace ? lastConfidence : 0;
            if (!initialized) return Vector3.zero;
            if (usable == 0 || !observed) return position;
            float horizon = Mathf.Min(age + Mathf.Max(0, lead), .04f);
            float trust = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(tuning.TrackingGrace * .5f, tuning.TrackingGrace, age));
            return position + Vector3.ClampMagnitude(velocity * horizon * trust, tuning.maxPredictionDistance);
        }
        private static float Alpha(float cutoff, float dt) => 1f / (1f + 1f / (2f * Mathf.PI * cutoff * dt));
    }
}
