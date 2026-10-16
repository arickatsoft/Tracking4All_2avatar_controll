using System;
using UnityEngine;

namespace FitFight.BodyTracking
{
    // Adapts learned 3D landmarks; limb depth is never replaced by the 2.5D solver.
    public sealed class MediaPipeWorldPose
    {
        public static readonly int[] HalpeToMediaPipe = { 0,2,5,7,8,11,12,13,14,15,16,23,24,25,26,27,28,0,11,23,31,32,31,32,29,30 };
        private readonly PoseJointFilter[] filters = new PoseJointFilter[26];
        private readonly Vector3[] neutral = new Vector3[26];
        private Quaternion correction = Quaternion.identity;
        private float scale = 1;
        private int frames;
        private readonly Vector2[] previousImage = new Vector2[26];
        private readonly Vector3[] headPlane = new Vector3[26];
        private readonly BodyMotionEstimator headMotion = new BodyMotionEstimator();
        private readonly MediaPipeMotionGate motionGate = new MediaPipeMotionGate();
        private bool havePrevious, haveRoot;
        private double previousTime, calibrationStarted, lastSample = double.NegativeInfinity;
        private Vector3 lastRoot;
        private static readonly int[] StandingJoints = { 0, 5, 6, 11, 12, 13, 14, 15, 16 };
        public bool Calibrated => frames >= 20;
        public int CalibrationFrames => frames;
        public float CalibrationScale => scale;
        public float StandingHipHeight => Mathf.Max(.4f,-(correction*((neutral[15]+neutral[16]-neutral[11]-neutral[12])*.5f)).y*scale);
        public Vector3 HeadAngles => headMotion.HeadAngles;
        public string CalibrationStatus { get; private set; } = "Waiting for standing reference";
        public MediaPipeWorldPose() { for (int i=0;i<26;i++) filters[i] = new PoseJointFilter(); }
        public bool Observed(int i) => filters[i].HasObservation;
        public void Reset()
        {
            frames=0; correction=Quaternion.identity; scale=1; havePrevious=haveRoot=false;
            lastSample=double.NegativeInfinity; headMotion.Reset();motionGate.Reset();
            CalibrationStatus="Waiting for standing reference";
            Array.Clear(neutral,0,26); foreach(var f in filters) f.Reset();
        }
        // Acquire a still, straight 3D reference; image projection never gates calibrated limb motion.
        public bool ObserveCalibration(Vector2[] image, Vector3[] source, float[] confidence,
            double time, float height, RTMPoseTuning tuning)
        {
            if (Calibrated) return true;
            if (time <= lastSample) return false;
            bool valid = true;
            foreach(int j in StandingJoints)
                valid &= confidence[j] >= Mathf.Max(.5f,tuning.jointConfidence) && RTMPoseMath.Finite(source[j])
                    && OneEuroFilterFloat.Finite(image[j].x) && OneEuroFilterFloat.Finite(image[j].y)
                    && image[j].x >= .02f && image[j].x <= .98f && image[j].y >= .02f && image[j].y <= .98f;
            Vector3 torso=(source[5]+source[6]-source[11]-source[12])*.5f;
            valid &= RTMPoseMath.Finite(torso) && torso.magnitude >= .15f && torso.magnitude <= 1.2f;
            for(int side=0;side<2;side++)
            {
                Vector3 thigh=source[13+side]-source[11+side], shin=source[15+side]-source[13+side];
                valid &= thigh.magnitude > .1f && shin.magnitude > .1f
                    && Vector3.Angle(thigh,-torso)<35 && Vector3.Angle(shin,-torso)<35 && Vector3.Angle(thigh,shin)<25;
            }
            double gap=time-previousTime;
            bool still=havePrevious && gap > 0 && gap <= Skeleton25DSolver.CalibrationGap(tuning.observationInterval);
            if(still) foreach(int j in StandingJoints)
                still &= Vector2.Distance(image[j],previousImage[j]) / Mathf.Max(.01f,(float)gap) < .35f;
            if(!valid || !still || frames>0 && time-calibrationStarted > Skeleton25DSolver.CalibrationWindow(tuning.observationInterval))
            {
                Reset(); CalibrationStatus=!valid ? "Stand straight: show head and both feet" : "Hold still with arms down";
                Array.Copy(image,previousImage,26); previousTime=time; havePrevious=true;
                return false;
            }
            if(frames==0) calibrationStarted=time;
            CollectNeutral(source); lastSample=time;
            Array.Copy(image,previousImage,26); previousTime=time;
            ReferenceTransform(height);
            for(int i=0;i<26;i++) headPlane[i]=correction*(source[i]-(source[11]+source[12])*.5f)*scale;
            headMotion.Observe(headPlane,confidence,time,true,tuning);
            CalibrationStatus=Calibrated ? "Standing reference captured" : "Acquiring standing reference";
            return Calibrated;
        }
        private void ReferenceTransform(float height)
        {
            Vector3 torso=neutral[18]-neutral[19];
            correction=Quaternion.FromToRotation(torso,Vector3.up);
            scale=height*.29f/Mathf.Max(.15f,torso.magnitude);
        }
        public static (float noise, float cutoff, float motion) RegionPreset(int joint)
        {
            if(joint==9 || joint==10) return (.3f,2f,1.25f);
            if(joint==7 || joint==8) return (.5f,1.7f,1.15f);
            if(joint>=13 && joint<=16 || joint>=20) return (.7f,1.35f,1f);
            return (1f,1f,1f);
        }
        public static void Map(PoseInferenceResult result, Vector2[] image, float[] scores, Vector3[] world)
        {
            for(int i=0;i<26;i++) { int j=HalpeToMediaPipe[i]; image[i]=result.Image[j]; scores[i]=result.Confidence[j]; world[i]=result.World[j]; }
            world[18]=(world[5]+world[6])*.5f; world[19]=(world[11]+world[12])*.5f;
            image[18]=(image[5]+image[6])*.5f; image[19]=(image[11]+image[12])*.5f;
            scores[18]=Mathf.Min(scores[5],scores[6]); scores[19]=Mathf.Min(scores[11],scores[12]);
        }
        public void CollectNeutral(Vector3[] source)
        {
            frames++;
            for(int i=0;i<26;i++) neutral[i] += (source[i]-neutral[i])/frames;
        }
        public void Step(Vector3[] source, float[] confidence, double time, float height, RTMPoseTuning tuning,
            Vector3[] output, float[] usable, Vector2[] image = null, float aspect = 1)
        {
            if (frames == 0) CollectNeutral(source);
            ReferenceTransform(height);
            if(confidence[11]>=tuning.jointConfidence && confidence[12]>=tuning.jointConfidence
                && RTMPoseMath.Finite(source[11]) && RTMPoseMath.Finite(source[12]))
            { lastRoot=(source[11]+source[12])*.5f; haveRoot=true; }
            Vector3 root=haveRoot ? lastRoot : Vector3.zero;
            motionGate.Observe(image,confidence,aspect,time,tuning.jointConfidence+tuning.confidenceHysteresis);
            for(int i=0;i<26;i++)
            {
                headPlane[i]=correction*(source[i]-root)*scale;
                var preset=RegionPreset(i);
                float amount=tuning.mediaPipeMotionAwareFiltering ? motionGate.Amount(i) : 1;
                float noise=Mathf.Lerp(1,preset.noise,amount);
                float cutoff=Mathf.Lerp(1,preset.cutoff,amount);
                float motion=Mathf.Lerp(1,preset.motion,amount);
                output[i]=tuning.mediaPipeRegionFiltering
                    ? filters[i].Step(headPlane[i],confidence[i],time,0,tuning,out usable[i],
                        tuning.noiseRadius*noise,tuning.minCutoff*cutoff,tuning.speedCoefficient*motion)
                    : filters[i].Step(headPlane[i],confidence[i],time,0,tuning,out usable[i]);
            }
            headMotion.Observe(headPlane,confidence,time,false,tuning);
        }
        public void MarkMissing(double time, RTMPoseTuning tuning, Vector3[] output, float[] usable)
        { motionGate.Reset(); for(int i=0;i<26;i++) output[i]=filters[i].Step(Vector3.zero,0,time,0,tuning,out usable[i]); }
        public void Predict(double time, float processing, RTMPoseTuning tuning, Vector3[] output, float[] confidence)
        { for(int i=0;i<26;i++) output[i]=filters[i].Predict(time,tuning.prediction+processing,tuning,out confidence[i]); }
        private Vector3 Transform(Vector3 p, bool mirror, bool flip)
        { p=correction*p; p.x *= (mirror?1:-1)*(flip?-1:1); p.z *= flip?1:-1; return p; }
        private Quaternion Basis(Vector3 left,Vector3 right,Vector3 up)
        {
            var across=right-left; var forward=Vector3.Cross(across,up);
            return forward.sqrMagnitude > .0001f && up.sqrMagnitude > .0001f ? Quaternion.LookRotation(forward,up) : Quaternion.identity;
        }
        public Quaternion BodyRotation(Vector3[] pose, bool mirror, bool flip)
        {
            // pose has already been gravity corrected; neutral is corrected here once.
            Vector3 P(int i) { var p=pose[i]; p.x *= (mirror?1:-1)*(flip?-1:1); p.z *= flip?1:-1; return p; }
            var current=Basis(P(5),P(6),(P(5)+P(6)-P(11)-P(12))*.5f);
            var reference=Basis(Transform(neutral[5],mirror,flip),Transform(neutral[6],mirror,flip),
                Transform(neutral[18]-neutral[19],mirror,flip));
            return current*Quaternion.Inverse(reference);
        }
    }
}
