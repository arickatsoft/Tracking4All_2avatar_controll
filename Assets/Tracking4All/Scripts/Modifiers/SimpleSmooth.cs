using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Classic responsive smoothing. Set higher for more responsiveness but more jitter, lower for less responsiveness but smoother.
    /// </summary>
    public class SimpleSmooth : Modifier, ILandmarkModifier, INormalizedLandmarkModifier
    {
        public bool waitBeforeTimeout = true;
        // Opted in only by local body solutions. Other users retain their existing settings.
        public bool adaptiveBody;
        public float restCutoff = 2;
        public float motionGain = 20;
        public static float AdaptiveAlpha(float distance, float dt, float cutoff, float gain)
            => 1 - Mathf.Exp(-2 * Mathf.PI * (cutoff + gain * Mathf.Max(0, distance)) * Mathf.Max(0, dt));
        private float PositionAlpha(Vector3 current, Vector3 target, float dt)
            => adaptiveBody ? AdaptiveAlpha(Vector3.Distance(current, target), dt, restCutoff, motionGain) : dt * speed;
        public float speed = 10f; // lower is smoother, higher is more responsive but also more noisy
        public float occlusionMeasuresSpeed = 30f; // the speed that presense and visibility change at (same as above).

        public void Modify(int dataIndex, ref Landmark current, ref Landmark target, ref bool stayAlive, float deltaTime)
        {
            if (!Enabled) return;

            current.Position = Vector3.Lerp(current.Position, target.Position, PositionAlpha(current.Position, target.Position, deltaTime));
            current.Presence = Mathf.Lerp(current.Presence, target.Presence, deltaTime * occlusionMeasuresSpeed);
            current.Visibility = Mathf.Lerp(current.Visibility, target.Visibility, deltaTime * occlusionMeasuresSpeed);

            if (waitBeforeTimeout && (current.Position - target.Position).sqrMagnitude > 0.01f)
            {
                stayAlive = true;
            }
        }

        public void Modify(int dataIndex, ref NormalizedLandmark current, ref NormalizedLandmark target, ref bool stayAlive, float deltaTime)
        {
            if (!Enabled) return;

            current.Position = Vector3.Lerp(current.Position, target.Position, PositionAlpha(current.Position, target.Position, deltaTime));
            current.Presence = Mathf.Lerp(current.Presence, target.Presence, deltaTime * occlusionMeasuresSpeed);
            current.Visibility = Mathf.Lerp(current.Visibility, target.Visibility, deltaTime * occlusionMeasuresSpeed);

            /*if ((current.Position - target.Position).sqrMagnitude > 0.01f)
            {
                stayAlive = true;
            }*/
        }
    }
}