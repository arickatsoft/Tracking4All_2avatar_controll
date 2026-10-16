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
        public MediaPipeWorldPose() { for (int i=0;i<26;i++) filters[i] = new PoseJointFilter(); }
        public bool Observed(int i) => filters[i].HasObservation;
        public void Reset() { frames=0; correction=Quaternion.identity; scale=1; Array.Clear(neutral,0,26); foreach(var f in filters) f.Reset(); }
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
            Vector3[] output, float[] usable)
        {
            if (frames == 0) CollectNeutral(source);
            Vector3 torso = neutral[18]-neutral[19];
            correction=Quaternion.FromToRotation(torso,Vector3.up);
            scale=height*.29f/Mathf.Max(.15f,torso.magnitude);
            Vector3 root=(source[11]+source[12])*.5f;
            for(int i=0;i<26;i++) output[i]=filters[i].Step(correction*(source[i]-root)*scale,confidence[i],time,0,tuning,out usable[i]);
        }
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
