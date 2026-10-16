using UnityEngine;

namespace FitFight.BodyTracking
{
    /// <summary>
    /// One Euro adaptive low-pass filter for scalar values.
    /// Reference: Casiez et al. "1€ Filter: A Simple Speed-based Low-pass Filter for Noisy Input in Interactive Systems" (CHI 2012).
    /// minCutoff: lower → more smoothing at rest (but more lag). Start 1.0 Hz.
    /// beta: higher → less lag during fast movement. Start 0.01.
    /// </summary>
    public class OneEuroFilterFloat
    {
        private readonly float minCutoff, beta, dCutoff;
        private float x, previous, dx;
        private double lastTime;
        private bool initialized;

        public OneEuroFilterFloat(float minCutoff = 1f, float beta = 0.01f, float dCutoff = 1f)
        {
            this.minCutoff = Mathf.Max(.001f, minCutoff);
            this.beta = Mathf.Max(0, beta); this.dCutoff = Mathf.Max(.001f, dCutoff);
        }
        public void Reset() { initialized = false; }
        public float Filter(float value, double timestamp)
        {
            if (!Finite(value) || double.IsNaN(timestamp) || double.IsInfinity(timestamp)) return initialized ? x : 0;
            if (!initialized)
            { x = previous = value; dx = 0; initialized = true; lastTime = timestamp; return value; }
            if (timestamp <= lastTime) return x;
            float dt = (float)(timestamp - lastTime);
            lastTime = timestamp;
            // Derivative of successive observations, not accumulated filter lag.
            dx = Mathf.Lerp(dx, (value - previous) / dt, Alpha(dt, dCutoff));
            previous = value;
            x = Mathf.Lerp(x, value, Alpha(dt, minCutoff + beta * Mathf.Abs(dx)));
            return x;
        }
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static float Alpha(float dt, float cutoff) => 1f / (1f + 1f / (2f * Mathf.PI * cutoff * dt));
    }

    /// <summary>
    /// One Euro rotation filter using shortest-arc slerp and a shared angular-speed cutoff.
    /// Beta is Hz per degree/second; component-wise quaternion filtering has different units.
    /// Duplicate/out-of-order times and invalid samples leave history unchanged.
    /// </summary>
    public class OneEuroFilterQuaternion
    {
        private readonly float minCutoff, beta;
        private Quaternion filtered = Quaternion.identity, previous;
        private float speed;
        private double lastTime;
        private bool initialized;

        public OneEuroFilterQuaternion(float minCutoff = 1f, float beta = 0.01f)
        { this.minCutoff = Mathf.Max(.001f, minCutoff); this.beta = Mathf.Max(0, beta); }
        public void Reset() { initialized = false; speed = 0; }
        public Quaternion Filter(Quaternion value, double timestamp)
        {
            if (!TryNormalize(value, out value) || double.IsNaN(timestamp) || double.IsInfinity(timestamp)) return filtered;
            if (!initialized)
            { filtered = previous = value; lastTime = timestamp; initialized = true; return value; }
            if (timestamp <= lastTime) return filtered;
            float dt = (float)(timestamp - lastTime);
            speed = Mathf.Lerp(speed, Quaternion.Angle(previous, value) / dt, OneEuroFilterFloat.Alpha(dt, 1));
            previous = value; lastTime = timestamp;
            filtered = Quaternion.Slerp(filtered, value, OneEuroFilterFloat.Alpha(dt, minCutoff + beta * speed));
            return filtered;
        }
        public static bool TryNormalize(Quaternion value, out Quaternion result)
        {
            result = Quaternion.identity;
            float norm = Quaternion.Dot(value, value);
            if (!OneEuroFilterFloat.Finite(norm) || norm < .00000001f) return false;
            float inverse = 1f / Mathf.Sqrt(norm);
            result = new Quaternion(value.x * inverse, value.y * inverse, value.z * inverse, value.w * inverse);
            return true;
        }
    }

    /// <summary>Root-relative rotation state for one directly driven bone, updated at render cadence.</summary>
    public sealed class BoneRotationFilter
    {
        private readonly OneEuroFilterQuaternion filter;
        private Quaternion applied = Quaternion.identity;
        private double lastTime;
        private bool initialized, reseed;
        public bool HasValue => initialized;
        public Quaternion Value => applied;
        public BoneRotationFilter(HumanBodyBones bone)
        {
            var preset = BoneFilterPresets.ForBone(bone);
            filter = new OneEuroFilterQuaternion(preset.minCutoff, preset.beta);
        }
        public void Reset() { initialized = false; reseed = true; filter.Reset(); }
        public void Hold() { reseed = true; }
        public Quaternion Step(Quaternion target, Quaternion current, double time, bool smoothing, bool limitSpeed, float degreesPerSecond)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || !OneEuroFilterQuaternion.TryNormalize(target, out target))
                return initialized ? applied : current;
            if (!initialized)
            {
                if (!OneEuroFilterQuaternion.TryNormalize(current, out applied)) applied = target;
                initialized = true; lastTime = time; reseed = true;
                // Seed from the visible pose at handoff. The next render frame starts motion.
                return applied;
            }
            if (time <= lastTime) return applied;
            // A pause/occlusion must not buy a large catch-up rotation in one frame.
            float dt = Mathf.Min((float)(time - lastTime), .05f);
            if (time - lastTime > .2) reseed = true;
            lastTime = time;
            if (reseed)
            { filter.Reset(); filter.Filter(applied, time - dt); reseed = false; }
            var desired = smoothing ? filter.Filter(target, time) : target;
            applied = limitSpeed ? Quaternion.RotateTowards(applied, desired, Mathf.Max(0, degreesPerSecond) * dt) : desired;
            return applied;
        }
    }

    /// <summary>Per-region smoothing presets. Lower minCutoff = more smoothing at rest; higher beta = less lag when moving fast.</summary>
    public static class BoneFilterPresets
    {
        public static (float minCutoff, float beta) Torso => (1.5f, 0.010f);
        public static (float minCutoff, float beta) Arm   => (3.0f, 0.050f);
        public static (float minCutoff, float beta) Leg   => (2.0f, 0.030f);
        public static (float minCutoff, float beta) Head  => (2.0f, 0.020f);
        public static (float minCutoff, float beta) ForBone(HumanBodyBones bone) => bone switch
        {
            HumanBodyBones.Hips  or HumanBodyBones.Spine or HumanBodyBones.Chest               => Torso,
            HumanBodyBones.Neck  or HumanBodyBones.Head                                         => Head,
            HumanBodyBones.LeftUpperLeg  or HumanBodyBones.RightUpperLeg or
            HumanBodyBones.LeftLowerLeg  or HumanBodyBones.RightLowerLeg or
            HumanBodyBones.LeftFoot      or HumanBodyBones.RightFoot                            => Leg,
            _                                                                                    => Arm,
        };
    }
}
