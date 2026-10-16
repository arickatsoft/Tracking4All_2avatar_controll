using System;
using System.Collections.Generic;
using UnityEngine;

namespace FitFight.BodyTracking
{
    // Availability is not accuracy. One sample per completed inference, including detection loss.
    public sealed class MediaPipeTestStats
    {
        [Serializable] public sealed class Movement
        {
            public string label;
            public float durationSeconds, acceptedFPS, medianPoseMs, p95PoseMs, medianComputeMs, p95ComputeMs;
            public float medianAvatarMs, p95AvatarMs;
            public int evaluated, accepted, noPerson, stale, otherRejected, avatarSamples;
            public float leftWristAvailablePercent, rightWristAvailablePercent, leftKneeAvailablePercent, rightKneeAvailablePercent;
            public float rawStillnessRmsCm, filteredStillnessRmsCm;
            public int stillnessSamples;
        }
        private sealed class Window
        {
            public Movement result=new Movement();
            public double start,end;
            public readonly List<float> pose=new List<float>(), compute=new List<float>(), avatar=new List<float>();
            public readonly int[] available=new int[4];
            public readonly Vector3[] rawMean=new Vector3[4], filteredMean=new Vector3[4];
            public double rawM2,filteredM2;
        }
        private readonly List<Window> windows=new List<Window>();
        private Window current;
        private static readonly int[] Joints={9,10,13,14};
        public void StartMovement(string label,double now)
        {
            if(current!=null) current.end=now;
            current=new Window{start=now,end=now};current.result.label=label; windows.Add(current);
        }
        public void Add(RTMPosePoseProvider.Diagnostics d)
        {
            if(current==null) return; // Calibration/initialization excluded from movement statistics.
            var r=current.result; r.evaluated++; current.end=d.receivedAt;
            current.compute.Add(d.computeMs);
            if(d.accepted) { r.accepted++; current.pose.Add(d.inferenceMs); }
            else if(d.resultReason=="no_person") r.noPerson++;
            else if(d.resultReason=="stale") r.stale++;
            else r.otherRejected++;
            for(int k=0;k<Joints.Length;k++)
            {
                int j=Joints[k];
                if(d.accepted && d.observed!=null && d.observed[j] && d.confidence[j]>0) current.available[k]++;
            }
            // Ignore cue transition; use only confident, accepted still frames. Root-relative, not absolute camera movement.
            if(!r.label.StartsWith("still") || d.receivedAt-current.start<2 || !d.accepted || d.rawWorld==null) return;
            foreach(int j in Joints) if(d.observed==null || !d.observed[j]) return;
            var root=(d.rawWorld[23]+d.rawWorld[24])*.5f;
            for(int k=0;k<Joints.Length;k++)
                if(!RTMPoseMath.Finite(d.rawWorld[MediaPipeWorldPose.HalpeToMediaPipe[Joints[k]]]-root)
                    || !RTMPoseMath.Finite(d.joints[Joints[k]])) return;
            int n=++r.stillnessSamples;
            for(int k=0;k<Joints.Length;k++)
            {
                var raw=(d.rawWorld[MediaPipeWorldPose.HalpeToMediaPipe[Joints[k]]]-root)*d.scale;
                var filtered=d.joints[Joints[k]];
                var delta=raw-current.rawMean[k];current.rawMean[k]+=delta/n;
                current.rawM2+=Vector3.Dot(delta,raw-current.rawMean[k]);
                delta=filtered-current.filteredMean[k];current.filteredMean[k]+=delta/n;
                current.filteredM2+=Vector3.Dot(delta,filtered-current.filteredMean[k]);
            }
        }
        public void Avatar(float delay) { if(current!=null) current.avatar.Add(delay); }
        public Movement[] Finish(double now)
        {
            if(current!=null) current.end=now;
            var output=new Movement[windows.Count];
            for(int i=0;i<windows.Count;i++)
            {
                var w=windows[i];var r=w.result;
                r.durationSeconds=Mathf.Max(0,(float)(w.end-w.start));
                r.acceptedFPS=r.durationSeconds>0 ? r.accepted/r.durationSeconds : 0;
                r.medianPoseMs=Percentile(w.pose,.5f);r.p95PoseMs=Percentile(w.pose,.95f);
                r.medianComputeMs=Percentile(w.compute,.5f);r.p95ComputeMs=Percentile(w.compute,.95f);
                r.avatarSamples=w.avatar.Count;r.medianAvatarMs=Percentile(w.avatar,.5f);r.p95AvatarMs=Percentile(w.avatar,.95f);
                float unit=r.evaluated>0 ? 100f/r.evaluated : 0;
                r.leftWristAvailablePercent=w.available[0]*unit;r.rightWristAvailablePercent=w.available[1]*unit;
                r.leftKneeAvailablePercent=w.available[2]*unit;r.rightKneeAvailablePercent=w.available[3]*unit;
                if(r.stillnessSamples>1)
                { r.rawStillnessRmsCm=100*(float)Math.Sqrt(w.rawM2/(r.stillnessSamples*4));
                  r.filteredStillnessRmsCm=100*(float)Math.Sqrt(w.filteredM2/(r.stillnessSamples*4)); }
                output[i]=r;
            }
            return output;
        }
        // Nearest-rank percentile, explicitly tested for empty and small samples.
        public static float Percentile(List<float> samples,float p)
        {
            if(samples.Count==0) return 0;
            var copy=samples.ToArray();Array.Sort(copy);
            return copy[Mathf.Clamp(Mathf.CeilToInt(p*copy.Length)-1,0,copy.Length-1)];
        }
    }
}
