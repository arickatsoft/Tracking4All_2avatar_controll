using UnityEngine;

namespace FitFight.BodyTracking
{
    [System.Serializable]
    public sealed class RTMPoseTuning
    {
        // Serialized name retained for saved settings; now allows bounded torso/head motion, not a rigid lock.
        public bool uprightBody = true;
        [Range(40, 90)] public float cameraVerticalFov = 60;
        public bool filtering = true;
        // MediaPipe-only A/B option; RTMPose settings and trained model weights are unchanged.
        public bool mediaPipeRegionFiltering = true;
        // Internal replay switch; normal device region mode uses motion-aware rest behavior.
        public bool mediaPipeMotionAwareFiltering = true;
        [Range(0, .05f)] public float noiseRadius = .018f;
        [Range(0, .2f)] public float confidenceHysteresis = .08f;
        [Range(0, 8)] public float bodyNoiseDegrees = 2.5f;
        [Range(15, 75)] public float maxBodyBend = 65;
        [Range(1, 20)] public float minCutoff = 1.5f;
        [Range(0, 20)] public float speedCoefficient = 12;
        [Range(1, 30)] public float velocityCutoff = 12;
        [Range(.15f, .7f)] public float jointConfidence = .3f;
        [Range(0, .04f)] public float prediction = .01f;
        [Range(.02f, .15f)] public float occlusionGrace = .08f;
        [Range(.02f, .25f)] public float maxPredictionDistance = .12f;
        [Range(0, .05f)] public float legDeadband = 0;
        [Range(1, 12)] public float depthSpeed = 6;
        [Range(0, 4)] public float depthMotionGain = 2;
        public int requestedCameraFPS = 60;
        // Keep the established retargeter as the default until device A/B testing.
        public bool directAvatar;
        // Opt-in A/B stages: reconstructed joints already have temporal filtering.
        public bool directRotationSmoothing;
        public bool directRotationSpeedLimit;
        [Range(180, 2160)] public float directMaxDegreesPerSecond = 1080;
        // Runtime cadence is measured; it is not a saved user preference.
        [System.NonSerialized] public float observationInterval = 1f / 30;
        public float TrackingGrace => Mathf.Max(occlusionGrace, Mathf.Min(.2f, observationInterval * 1.5f));

        public static RTMPoseTuning Load()
        {
            var value = new RTMPoseTuning();
            string json = PlayerPrefs.GetString("FitFight.RTMPose.Tuning", "");
            if (!string.IsNullOrEmpty(json))
                try { JsonUtility.FromJsonOverwrite(json, value); } catch (System.ArgumentException) { }
            value.Clamp();
            return value;
        }
        public void Save() { Clamp(); PlayerPrefs.SetString("FitFight.RTMPose.Tuning", JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
        public void Clamp()
        {
            directMaxDegreesPerSecond = OneEuroFilterFloat.Finite(directMaxDegreesPerSecond)
                ? Mathf.Clamp(directMaxDegreesPerSecond, 180, 2160) : 1080;
            noiseRadius = Mathf.Clamp(noiseRadius, 0, .05f);
            confidenceHysteresis = Mathf.Clamp(confidenceHysteresis, 0, .2f);
            bodyNoiseDegrees = Mathf.Clamp(bodyNoiseDegrees, 0, 8); maxBodyBend = Mathf.Clamp(maxBodyBend, 15, 75);
            cameraVerticalFov = Mathf.Clamp(cameraVerticalFov, 40, 90);
            minCutoff = Mathf.Clamp(minCutoff, 1, 20); speedCoefficient = Mathf.Clamp(speedCoefficient, 0, 20);
            velocityCutoff = Mathf.Clamp(velocityCutoff, 1, 30); jointConfidence = Mathf.Clamp(jointConfidence, .15f, .7f);
            prediction = Mathf.Clamp(prediction, 0, .04f); occlusionGrace = Mathf.Clamp(occlusionGrace, .02f, .15f);
            maxPredictionDistance = Mathf.Clamp(maxPredictionDistance, .02f, .25f); legDeadband = Mathf.Clamp(legDeadband, 0, .05f);
            depthSpeed = Mathf.Clamp(depthSpeed, 1, 12); depthMotionGain = Mathf.Clamp(depthMotionGain, 0, 4);
            requestedCameraFPS = requestedCameraFPS >= 60 ? 60 : 30;
        }
    }

    /// <summary>Counts observations, not requested FPS or render ticks.</summary>
    public sealed class PoseRateMeter
    {
        private double started = double.NaN, last = double.NegativeInfinity;
        private int count;
        private float rate;
        public void Add(double now)
        {
            if (double.IsNaN(started)) started = now;
            last = now; count++;
            if (now - started >= .5) { rate = count / (float)(now - started); count = 0; started = now; }
        }
        public float Get(double now) => now - last > .75 ? 0 : rate;
        public void Reset() { started = double.NaN; last = double.NegativeInfinity; count = 0; rate = 0; }
    }
}
