using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FitFight.Practice
{
    [Serializable]
    public sealed class BoxingSessionStats
    {
        [Serializable] public sealed class Segment
        {
            public string label;
            public int observations,leftVisible,rightVisible,leftPrepared,rightPrepared,leftCommitted,rightCommitted,cancelled;
        }
        public string schema="boxing-assist-1",appVersion,engine,recordingPath,startedUtc,finishReason;
        public string timingNote="Capture availability to decision, excluding exposure/display. Counts are detector decisions, not labelled accuracy. Bag hits may be fewer than commits.";
        public bool completed;
        public int hits;
        public float decisionMedianMs,decisionP95Ms;
        public List<Segment> segments=new List<Segment>();
        [NonSerialized] private readonly List<float> delays=new List<float>();
        [NonSerialized] private readonly int initialHits;
        [NonSerialized] private Segment current;
        public BoxingSessionStats(string version,string engineName,string path,int initial)
        { appVersion=version;engine=engineName;recordingPath=path;initialHits=initial;startedUtc=DateTime.UtcNow.ToString("O"); }
        public void Cue(string label)
        { current=new Segment{label=label};segments.Add(current); }
        public void Observation(PunchCameraInput.Frame frame,double now)
        {
            if(current==null) Cue("unguided");
            current.observations++;
            if(frame.boxing ? BoxingFeatures.HandVisible(frame.left) : frame.left.Valid) current.leftVisible++;
            if(frame.boxing ? BoxingFeatures.HandVisible(frame.right) : frame.right.Valid) current.rightVisible++;
        }
        public void Signal(int side,PunchIntent.Signal signal,double captured,double now)
        {
            if(current==null) Cue("unguided");
            if(signal==PunchIntent.Signal.Prepare) { if(side==0) current.leftPrepared++;else current.rightPrepared++; }
            if(signal==PunchIntent.Signal.Cancel) current.cancelled++;
            if(signal==PunchIntent.Signal.Commit)
            { if(side==0) current.leftCommitted++;else current.rightCommitted++;delays.Add(Mathf.Max(0,(float)(now-captured)*1000)); }
        }
        public void Finish(bool complete,string error,int finalHits,double now)
        {
            completed=complete;finishReason=error ?? (complete ? "completed" : "stopped");hits=Mathf.Max(0,finalHits-initialHits);
            delays.Sort();if(delays.Count>0) { decisionMedianMs=delays[delays.Count/2];decisionP95Ms=delays[Mathf.Min(delays.Count-1,Mathf.FloorToInt(delays.Count*.95f))]; }
        }
        public string Markdown()
        {
            var b=new StringBuilder("# Boxing assistance test\n\n");
            b.AppendLine($"{engine} · app {appVersion} · {finishReason} · {hits} bag hits\n");
            b.AppendLine($"Committed-punch decision median/p95: {decisionMedianMs:F1}/{decisionP95Ms:F1} ms. {timingNote}\n");
            b.AppendLine("| Cue | Observations | Visible L/R | Committed L/R | Cancelled |\n|---|---:|---:|---:|---:|");
            foreach(var s in segments) b.AppendLine($"| {s.label} | {s.observations} | {s.leftVisible}/{s.rightVisible} | {s.leftCommitted}/{s.rightCommitted} | {s.cancelled} |");
            return b.ToString();
        }
    }
}
