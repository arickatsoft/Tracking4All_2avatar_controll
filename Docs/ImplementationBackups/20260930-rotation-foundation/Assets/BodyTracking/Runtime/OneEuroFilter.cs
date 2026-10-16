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
        private float minCutoff, beta, dCutoff;
        private float x, dx;
        private double lastTime = double.NegativeInfinity;
        private bool initialized;

        public OneEuroFilterFloat(float minCutoff = 1f, float beta = 0.01f, float dCutoff = 1f)
        {
            this.minCutoff = minCutoff; this.beta = beta; this.dCutoff = dCutoff;
        }
        public float Filter(float value, double timestamp)
        {
            if (!initialized) { x = value; dx = 0; initialized = true; lastTime = timestamp; return value; }
            float dt = Mathf.Max((float)(timestamp - lastTime), 0.0001f);
            lastTime = timestamp;
            float dxRaw = (value - x) / dt;
            dx = LowPass(dxRaw, dx, Alpha(dt, dCutoff));
            float cutoff = minCutoff + beta * Mathf.Abs(dx);
            x = LowPass(value, x, Alpha(dt, cutoff));
            return x;
        }
        private static float Alpha(float dt, float cutoff)
        {
            float tau = 1f / (2f * Mathf.PI * cutoff);
            return 1f / (1f + tau / dt);
        }
        private static float LowPass(float a, float b, float alpha) => alpha * a + (1f - alpha) * b;
    }

    /// <summary>
    /// One Euro filter for Quaternion rotations.
    /// Filters the four components independently, flipping to the same hemisphere to prevent long-way-around interpolation.
    /// </summary>
    public class OneEuroFilterQuaternion
    {
        private readonly OneEuroFilterFloat fx, fy, fz, fw;
        private Quaternion last = Quaternion.identity;
        private bool initialized;

        public OneEuroFilterQuaternion(float minCutoff = 1f, float beta = 0.01f)
        {
            fx = new OneEuroFilterFloat(minCutoff, beta);
            fy = new OneEuroFilterFloat(minCutoff, beta);
            fz = new OneEuroFilterFloat(minCutoff, beta);
            fw = new OneEuroFilterFloat(minCutoff, beta);
        }
        public Quaternion Filter(Quaternion value, double timestamp)
        {
            // Flip to the same quaternion hemisphere to prevent interpolating the long way around.
            if (initialized && Quaternion.Dot(value, last) < 0)
                value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
            last = value; initialized = true;
            var result = new Quaternion(
                fx.Filter(value.x, timestamp),
                fy.Filter(value.y, timestamp),
                fz.Filter(value.z, timestamp),
                fw.Filter(value.w, timestamp));
            float mag = Mathf.Sqrt(result.x*result.x + result.y*result.y + result.z*result.z + result.w*result.w);
            if (mag < 0.0001f) return Quaternion.identity;
            return new Quaternion(result.x/mag, result.y/mag, result.z/mag, result.w/mag);
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
