using UnityEngine;

namespace FitFight.BodyTracking
{
    // Evidence for fast movement comes from visible image motion, not noisy inferred depth.
    // Units are calibrated image torso lengths per second, independent of camera resolution.
    public sealed class MediaPipeMotionGate
    {
        private readonly Vector2[] previous=new Vector2[26];
        private readonly float[] speed=new float[26],amount=new float[26];
        private readonly bool[] observed=new bool[26];
        private double lastTime;
        private float torsoLength;
        private bool initialized;
        public float Amount(int joint) => amount[joint];
        public void Reset()
        { initialized=false;torsoLength=0;System.Array.Clear(amount,0,26);System.Array.Clear(speed,0,26);System.Array.Clear(observed,0,26); }
        public void Observe(Vector2[] image,float[] scores,float aspect,double time,float threshold)
        {
            if(image==null || image.Length!=26 || scores[11]<threshold || scores[12]<threshold
                || !Finite(image[11]) || !Finite(image[12]))
            { Reset();return; }
            if(initialized && time<=lastTime) return;
            Vector2 P(int i) => new Vector2(image[i].x*aspect,image[i].y);
            Vector2 root=(P(11)+P(12))*.5f;
            if(torsoLength==0)
            {
                if(scores[5]<threshold || scores[6]<threshold || !Finite(image[5]) || !Finite(image[6])) return;
                torsoLength=Mathf.Max(.05f,Vector2.Distance((P(5)+P(6))*.5f,root));
            }
            float dt=initialized ? (float)(time-lastTime) : 0;
            bool contiguous=initialized && dt>0 && dt<=.25f;
            for(int i=0;i<26;i++)
            {
                bool visible=scores[i]>=threshold && Finite(image[i]);
                Vector2 point=visible ? (P(i)-root)/torsoLength : previous[i];
                if(visible && observed[i] && contiguous)
                {
                    float measured=Vector2.Distance(point,previous[i])/dt;
                    speed[i]=Mathf.Lerp(speed[i],Mathf.Min(measured,20),OneEuroFilterFloat.Alpha(dt,6));
                    // Normal model jitter at rest remains below the fast-motion range.
                    amount[i]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1.8f,speed[i]));
                }
                else {speed[i]=0;amount[i]=0;}
                observed[i]=visible;previous[i]=point;
            }
            lastTime=time;initialized=true;
        }
        private static bool Finite(Vector2 p) => OneEuroFilterFloat.Finite(p.x) && OneEuroFilterFloat.Finite(p.y);
    }
}
