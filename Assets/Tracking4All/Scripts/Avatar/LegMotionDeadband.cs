using UnityEngine;

namespace Tracking4All
{
    /// <summary>Hold one coherent leg pose until any landmark crosses its accepted position.</summary>
    public static class LegMotionDeadband
    {
        public static bool IsFinite(Vector3 p) => !(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z)
            || float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z));

        // Hip-relative, level ankle/heel/toe positions; a slight knee bend avoids
        // degenerate knee planes. Never reuse an occluded raised-foot pose.
        public static void StandingFallback(Vector3[] accepted, int start, float length, Vector3 up, Vector3 forward)
        {
            accepted[start] = -up * (length * .5f) + forward * (length * .04f);
            accepted[start + 1] = -up * length;
            accepted[start + 2] = -up * length - forward * (length * .06f);
            accepted[start + 3] = -up * length + forward * (length * .16f);
        }

        public static bool Filter(Vector3[] sample, Vector3[] accepted, int start, float threshold, bool initialized)
        {
            bool accept = !initialized;
            for (int i = start; i < start + 4; i++)
            {
                var p = sample[i];
                if (!IsFinite(p)) return false;
                if ((p - accepted[i]).sqrMagnitude >= threshold * threshold) accept = true;
            }
            if (!accept) return false;
            for (int i = start; i < start + 4; i++) accepted[i] = sample[i];
            return true;
        }
    }
}
