using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FitFight.Practice;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

public static class GameBodyReplayValidation
{
    [Serializable] private class Record
    {
        public string kind,actionKind;
        public double recordingStartedAt,time,capturedAt,receivedAt,decidedAt;
        public int sequence,side;
        public bool accepted;
        public Vector3[] joints;
        public float[] rawConfidence;
    }
    private struct Event { public string kind;public int side;public double time; }
    public struct RecordedSample { public double elapsed;public GameBodyMotion.Snapshot frame; }
    public static List<RecordedSample> LoadFrames()
    {
        var frames=new List<RecordedSample>();double start=0;
        foreach(string line in File.ReadLines("Logs/MediaPipe/20261009-142232-221/input/20261009-142232-221.jsonl"))
        {
            if(!line.StartsWith("{\"kind\":\"session\"") && !line.StartsWith("{\"kind\":\"reconstruction\"")) continue;
            var r=JsonUtility.FromJson<Record>(line);
            if(r.kind=="session") { start=r.recordingStartedAt;continue; }
            if(!r.accepted)continue;
            var p=new Vector3[33];var c=new float[33];
            for(int i=0;i<33;i++) { p[i]=r.joints[RTMPoseMath.ToMediaPipe[i]];p[i].x=-p[i].x;p[i].z=-p[i].z;c[i]=r.rawConfidence[i]; }
            frames.Add(new RecordedSample{elapsed=r.receivedAt-start,frame=new GameBodyMotion.Snapshot{
                joints=p,confidence=c,standingHipHeight=.94f,body=Quaternion.identity,head=Quaternion.identity,
                captured=r.capturedAt,received=r.receivedAt,sequence=r.sequence}});
        }
        return frames;
    }
    private static readonly StringBuilder report=new StringBuilder();
    public static void RunBatch()
    { try { Run();EditorApplication.Exit(0); }catch(Exception e){ Debug.LogException(e);EditorApplication.Exit(1); } }
    private static void Check(bool value,string label)
    { if(!value)throw new Exception(label);report.AppendLine("PASS "+label); }
    public static void Run()
    {
        report.Clear();
        try
        {
            var old=new GameBodyMotion115();var motion=new GameBodyMotion();
            var oldEvents=new List<Event>();var events=new List<Event>();var recorded=new List<Event>();
            double start=0;GameBodyMotion.Snapshot next=default;long last=-1;
            old.Decision+=(kind,side,action,captured,now)=>oldEvents.Add(new Event{kind=kind,side=side,time=now-start});
            motion.Decision+=(kind,side,action,captured,now)=>events.Add(new Event{kind=kind,side=side,time=now-start});
            foreach(string line in File.ReadLines("Logs/MediaPipe/20261009-142232-221/input/20261009-142232-221.jsonl"))
            {
                // Pose records dominate the file and contain large bone arrays that
                // are not needed for intent replay. JsonUtility ignores those fields.
                var r=JsonUtility.FromJson<Record>(line);
                if(r.kind=="session") start=r.recordingStartedAt;
                if(r.kind=="body_action") recorded.Add(new Event{kind=r.actionKind,side=r.side,time=r.decidedAt-start});
                if(r.kind=="reconstruction" && r.accepted)
                {
                    var p=new Vector3[33];var c=new float[33];
                    for(int i=0;i<33;i++)
                    {
                        p[i]=r.joints[RTMPoseMath.ToMediaPipe[i]];p[i].x=-p[i].x;p[i].z=-p[i].z;
                        c[i]=r.rawConfidence[i];
                    }
                    next=new GameBodyMotion.Snapshot{sequence=r.sequence,captured=r.capturedAt,received=r.receivedAt,
                        joints=p,confidence=c,standingHipHeight=.94f,body=Quaternion.identity,head=Quaternion.identity};
                }
                if(r.kind!="pose") continue;
                if(next.joints!=null && next.sequence!=last)
                {
                    last=next.sequence;motion.Observe(next,r.time);
                    old.Observe(new GameBodyMotion115.Snapshot{sequence=next.sequence,captured=next.captured,received=next.received,
                        joints=next.joints,confidence=next.confidence,standingHipHeight=next.standingHipHeight,body=next.body,head=next.head},r.time);
                }
                motion.Tick(r.time);old.Tick(r.time);
            }
            foreach(var e in events) report.AppendLine($"REPLAY {e.time:F3} side={e.side} {e.kind}");
            Check(recorded.Count==oldEvents.Count,"Baseline reproduces recorded body event count");
            for(int i=0;i<recorded.Count;i++) Check(recorded[i].kind==oldEvents[i].kind && recorded[i].side==oldEvents[i].side
                && Math.Abs(recorded[i].time-oldEvents[i].time)<.04,"Baseline reproduces body event "+i);
            Count(events,108,120,0,"Held knee lifts remain non-scoring");
            Count(events,124.8,131.8,4,"Four visible knee drives commit once each, including drive after the next cue");
            foreach(var span in new[]{new Vector2(135.7f,137.4f),new Vector2(138.1f,139.7f),new Vector2(141.1f,142.9f),new Vector2(143.5f,145.6f)})
            {
                Count(events,span.x,span.y,1,"Visible high front extension "+span);
                Check(events.Exists(e=>e.time>=span.x && e.time<span.y && (e.kind=="FrontKick" || e.kind=="Kick")),"High extension resolves to a foot attack "+span);
            }
            Count(events,0,108,0,"Idle, torso, squats and bilateral arm segments do not create leg attacks");
            Count(events,156,165,0,"Quiet recovery has no new leg attacks");
            // Direction is intentionally not asserted for the unfamiliar side kicks.
            Count(events,150.8,154.6,2,"Both visible lateral-attempt extensions produce one foot attack each");
            report.AppendLine("Recorded transitions validate these cases only; this is not a population accuracy estimate or a camera throughput test.");
        }
        catch(Exception e){report.AppendLine("FAIL "+e);throw;}
        finally { Directory.CreateDirectory("Logs/Practice");File.WriteAllText("Logs/Practice/game-body-recorded-replay.txt",report.ToString()); }
    }
    private static void Count(List<Event> events,double from,double to,int expected,string label)
    { int count=events.FindAll(e=>e.time>=from && e.time<to && e.kind!="prepare" && e.kind!="cancel").Count;Check(count==expected,label+": "+count); }
}
