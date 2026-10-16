using System;
using System.Collections.Generic;
using System.Text;
using FitFight.BodyTracking;
using UnityEngine;

namespace FitFight.Practice
{
    [Serializable] public sealed class GameBodySessionStats
    {
        public static GameBodySessionStats Latest { get; private set; }
        public static string SummaryPath { get; private set; }
        public static void Remember(GameBodySessionStats summary,string path)
        { Latest=summary;SummaryPath=path;PlayerPrefs.SetString("FitFight.GameBody.LastSummary",path);PlayerPrefs.Save(); }
        public static void LoadLatest()
        {
            if(Latest!=null) return;
            SummaryPath=PlayerPrefs.GetString("FitFight.GameBody.LastSummary","");
            try { if(System.IO.File.Exists(SummaryPath)) Latest=JsonUtility.FromJson<GameBodySessionStats>(System.IO.File.ReadAllText(SummaryPath)); }
            catch(Exception e) { Debug.LogWarning("Game body summary: "+e.Message); }
        }
        [Serializable] public sealed class Segment
        {
            public string label;
            public int observations,legVisible,leftPunches,rightPunches,frontKicks,sideKicks,knees,preparations,cancellations,hits;
            public float maximumSquatCm,maximumBodyDegrees,maximumHeadDegrees;
        }
        public string schema="game-body-1",appVersion,engine,startedUtc,finishReason,capturePath,delegateName,cameraFallback;
        public string interpretation="Cues are requested movements, not ground truth. Decisions and physical bag hits are separate. Timing starts at camera-frame availability; exposure and display are excluded. GPU readback is bypassed only on camera2-cpu.";
        public bool completed;
        public int hits,finalPoseFrames,previewFrames,runtimeErrors,droppedRecords,evaluated,accepted;
        public string lastRuntimeError;
        public float medianPoseMs,p95PoseMs,medianComputeMs,medianReadbackMs,medianCameraConversionMs;
        public float medianDecisionMs,p95DecisionMs;
        public List<Segment> segments=new List<Segment>();
        [NonSerialized] private Segment current;
        [NonSerialized] private readonly List<float> pose=new List<float>(),compute=new List<float>(),readback=new List<float>(),conversion=new List<float>(),decision=new List<float>();
        [NonSerialized] private readonly int initialHits;
        public GameBodySessionStats(string version,string name,int initial)
        { appVersion=version;engine=name;initialHits=initial;startedUtc=DateTime.UtcNow.ToString("O"); }
        public void Cue(string label) { current=new Segment { label=label };segments.Add(current); }
        private Segment Current { get { if(current==null) Cue("unguided");return current; } }
        public void Observation(GameBodyMotion.Snapshot f,RTMPosePoseProvider provider)
        {
            var s=Current;s.observations++;
            bool legs=true;for(int i=23;i<=28;i++) legs &= f.confidence[i]>=.5f;
            if(legs) s.legVisible++;
            if(legs) s.maximumSquatCm=Mathf.Max(s.maximumSquatCm,Mathf.Clamp(f.standingHipHeight+Mathf.Min(f.joints[27].y,f.joints[28].y),0,.48f)*100);
            s.maximumBodyDegrees=Mathf.Max(s.maximumBodyDegrees,Quaternion.Angle(Quaternion.identity,f.body));
            s.maximumHeadDegrees=Mathf.Max(s.maximumHeadDegrees,Quaternion.Angle(Quaternion.identity,f.head));
        }
        public void Punch(int side,PunchIntent.Signal signal,double captured,double now)
        {
            if(signal!=PunchIntent.Signal.Commit) return;
            if(side==0) Current.leftPunches++;else Current.rightPunches++;
            decision.Add(Mathf.Max(0,(float)(now-captured)*1000));
        }
        public void Leg(string kind,int side,long action,double captured,double now)
        {
            var s=Current;
            if(kind=="prepare") s.preparations++;
            else if(kind=="cancel") s.cancellations++;
            else
            {
                if(kind=="FrontKick") s.frontKicks++;else if(kind=="SideKick") s.sideKicks++;else if(kind=="Knee") s.knees++;
                decision.Add(Mathf.Max(0,(float)(now-captured)*1000));
            }
        }
        public void Hit() { Current.hits++; }
        public void Inference(RTMPosePoseProvider.Diagnostics d)
        {
            evaluated++;if(d.accepted) accepted++;
            capturePath=d.capturePath;delegateName=d.delegateName;cameraFallback=d.cameraFallback;
            if(d.inferenceMs>0) pose.Add(d.inferenceMs);
            if(d.computeMs>0) compute.Add(d.computeMs);
            readback.Add(d.scheduledToReadbackMs);conversion.Add(d.cameraConversionMs);
        }
        private static float Percentile(List<float> values,float percentile)
        { if(values.Count==0) return 0;values.Sort();return values[Mathf.Min(values.Count-1,Mathf.FloorToInt((values.Count-1)*percentile))]; }
        public void Finish(bool complete,string error,int finalHits,int dropped)
        {
            completed=complete;finishReason=error??(complete?"completed":"stopped");hits=Mathf.Max(0,finalHits-initialHits);droppedRecords=dropped;
            medianPoseMs=Percentile(pose,.5f);p95PoseMs=Percentile(pose,.95f);medianComputeMs=Percentile(compute,.5f);
            medianReadbackMs=Percentile(readback,.5f);medianCameraConversionMs=Percentile(conversion,.5f);
            medianDecisionMs=Percentile(decision,.5f);p95DecisionMs=Percentile(decision,.95f);
        }
        public string Markdown()
        {
            var b=new StringBuilder("# Full-body game test summary\n\n");
            b.AppendLine($"{engine} · app {appVersion} · {finishReason} · {hits} physical bag hits\n");
            b.AppendLine($"Camera path: {capturePath}; inference: {delegateName}; accepted/evaluated: {accepted}/{evaluated}.");
            if(!string.IsNullOrEmpty(cameraFallback)) b.AppendLine("Camera fallback: "+cameraFallback);
            b.AppendLine($"Pose median/p95: {medianPoseMs:F1}/{p95PoseMs:F1} ms; compute: {medianComputeMs:F1} ms; schedule-to-readback: {medianReadbackMs:F1} ms; camera conversion: {medianCameraConversionMs:F1} ms.");
            b.AppendLine($"Action decision median/p95: {medianDecisionMs:F1}/{p95DecisionMs:F1} ms.\n\n{interpretation}\n");
            b.AppendLine("| Requested movement | Observations / legs visible | Punch L/R | Front / side / knee | Physical hits | Squat cm | Torso / head degrees |\n|---|---:|---:|---:|---:|---:|---:|");
            foreach(var s in segments) b.AppendLine($"| {s.label} | {s.observations}/{s.legVisible} | {s.leftPunches}/{s.rightPunches} | {s.frontKicks}/{s.sideKicks}/{s.knees} | {s.hits} | {s.maximumSquatCm:F1} | {s.maximumBodyDegrees:F1}/{s.maximumHeadDegrees:F1} |");
            b.AppendLine($"\nFinal poses: {finalPoseFrames}; preview refreshes: {previewFrames}; runtime errors: {runtimeErrors}; dropped records: {droppedRecords}.");
            if(runtimeErrors>0) b.AppendLine(lastRuntimeError);
            return b.ToString();
        }
    }
}
