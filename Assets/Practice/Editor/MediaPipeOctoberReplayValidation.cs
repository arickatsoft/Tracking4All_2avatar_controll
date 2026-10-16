using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;

public static class MediaPipeOctoberReplayValidation
{
    [Serializable] private class Record
    {
        public string kind,label,leftState,rightState;
        public double recordedAt,recordingStartedAt;
        public PunchCameraInput.Frame observation;
    }
    [Serializable] private class Summary { public List<BoxingSessionStats.Segment> segments; }
    private static readonly StringBuilder report=new StringBuilder();
    private static void Check(bool ok,string message) { if(!ok)throw new Exception(message);report.AppendLine("PASS "+message); }
    public static void RunBatch()
    { try { Run();MediaPipeBoxingValidation.Run();PunchValidation.Run();EditorApplication.Exit(0); }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);} }
    public static void Run()
    {
        report.Clear();
        try
        {
            Gate();
            Replay("20261006-044622-113","bottom");Replay("20261006-044819-059","torso");
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { File.WriteAllText("Logs/Practice/october-replay-validation.txt",report.ToString());Debug.Log(report.ToString()); }
    }
    private static void Gate()
    {
        var gate=new BoxingGestureGate();var a=MediaPipeBoxingValidation.Guard();
        gate.Observe(new PunchCameraInput.Frame{sequence=1,time=1,left=a,right=a});
        var b=a;b.wrist.y+=.3f;
        gate.Observe(new PunchCameraInput.Frame{sequence=2,time=1.1,left=b,right=b});
        Check(gate.Blocked && b.wrist.y<.75f,"Shared upward motion blocks before wrists reach overhead");
        gate.Observe(new PunchCameraInput.Frame{sequence=2,time=1.2,left=a,right=a});
        Check(gate.Blocked,"Duplicate camera sequence cannot release shared gesture");
        gate.Observe(new PunchCameraInput.Frame{sequence=3,time=1.2,left=a,right=a});
        Check(gate.Blocked,"Shared lowering remains blocked");
        gate.Observe(new PunchCameraInput.Frame{sequence=4,time=1.3,left=a,right=a});
        Check(!gate.Blocked,"Observed settled guard releases shared gesture");
        gate.Observe(new PunchCameraInput.Frame{sequence=5,time=1.4,left=MediaPipeBoxingValidation.Extended(),right=a});
        Check(!gate.Blocked,"A single upward-projected punch keeps the boxing prior");
    }
    private static void Replay(string session,string position)
    {
        string root="Logs/MediaPipe/"+session+"/input/";
        var summary=JsonUtility.FromJson<Summary>(File.ReadAllText(root+session+".summary.json"));
        var old=new[]{new BoxingPunchIntent111(),new BoxingPunchIntent111()};var newer=new[]{new BoxingPunchIntent(),new BoxingPunchIntent()};var gate=new BoxingGestureGate();
        var counts=new Dictionary<string,int[]>();string cue="setup";double cueAt=0,start=0;int settledOverhead=0;
        var trace=new StringBuilder();
        var oldPunches=new List<Vector2>();var newPunches=new List<Vector2>();
        var extensions=new List<Vector3>(); // side, time, wrist extension from quiet guard (1.21 rule)
        foreach(string line in File.ReadLines(root+session+".jsonl"))
        {
            var record=JsonUtility.FromJson<Record>(line);
            if(record.kind=="session")start=record.recordingStartedAt;
            if(record.kind=="guide_cue") { cue=record.label;cueAt=record.recordedAt; }
            if(record.kind!="observation")continue;
            var f=record.observation;gate.Observe(f);
            if(!counts.ContainsKey(cue)) counts[cue]=new int[4];
            for(int side=0;side<2;side++)
            {
                var arm=side==0?f.left:f.right;
                var origin=(PunchArmObservation)typeof(BoxingPunchIntent).GetField("origin",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(newer[side]);
                var legacy=old[side].Observe(arm,f.sequence,f.time,BoxingFeatures.Overhead(f.left,f.right));
                var revised=newer[side].Observe(arm,f.sequence,f.time,gate.Blocked);
                extensions.Add(new Vector3(side,(float)(f.time-start),newer[side].Extension));
                if(legacy==PunchIntent.Signal.Commit) counts[cue][side]++;
                if(revised==PunchIntent.Signal.Commit)
                { counts[cue][2+side]++;if(cue=="RAISE AND LOWER YOUR HANDS" && f.time-cueAt>1)settledOverhead++; }
                if(cue.Contains("PUNCHES") || cue.StartsWith("ALTERNATE"))
                {
                    if(legacy==PunchIntent.Signal.Commit)oldPunches.Add(new Vector2(side,(float)(f.time-start)));
                    if(revised==PunchIntent.Signal.Commit)newPunches.Add(new Vector2(side,(float)(f.time-start)));
                }
                if(legacy!=PunchIntent.Signal.None || revised!=PunchIntent.Signal.None || cue.Contains("SLOW"))
                    trace.AppendLine($"{f.time-start:F3} {(side==0?"L":"R")} old={legacy} new={revised} {newer[side].Phase} {newer[side].Reason} speed={newer[side].Speed:F3} travel={newer[side].Travel:F3} imageOpening={origin.Bend-arm.Bend:F1} worldOpening={origin.WorldBend-arm.WorldBend:F1} forward={arm.wristWorld.z-origin.wristWorld.z:F3} reach={arm.wristWorld.magnitude-origin.wristWorld.magnitude:F3} {cue}");
            }
        }
        File.WriteAllText("Logs/MediaPipe/"+session+"/punch-priority-replay.txt",trace.ToString());
        int punchOld=0,punchNew=0;
        foreach(var segment in summary.segments)
        {
            var c=counts[segment.label];
            Check(c[0]==segment.leftCommitted && c[1]==segment.rightCommitted,$"{position} reproduces saved 1.11 decisions: {segment.label}");
            report.AppendLine($"REPLAY {position} {segment.label}: 1.11={c[0]}/{c[1]}, revised={c[2]}/{c[3]}");
            if(segment.label.Contains("PUNCHES") || segment.label.StartsWith("ALTERNATE"))
            { punchOld+=c[0]+c[1];punchNew+=c[2]+c[3]; }
            if(segment.label.StartsWith("HOLD")) Check(c[2]+c[3]==0,position+" has no idle commits");
        }
        Check(settledOverhead==0,position+" has no commits after the overhead cue transition");
        int partial=0;
        foreach(var stroke in oldPunches)
        {
            // 1.21: an old commit whose stroke never reached the commit extension is a partial movement
            // (withdrawn start / drift, or a 10 Hz sample that missed the peak) and is not required.
            float peak=0;foreach(var e in extensions) if(e.x==stroke.x && e.y>=stroke.y-.05f && e.y<=stroke.y+.4f) peak=Mathf.Max(peak,e.z);
            if(peak<BoxingPunchIntent.CommitExtension && !newPunches.Exists(n=>n.x==stroke.x && Mathf.Abs(n.y-stroke.y)<.25f))
            { partial++;report.AppendLine($"PARTIAL {position} {stroke.x} at {stroke.y:F3}: peak extension {peak:F2} < {BoxingPunchIntent.CommitExtension:F2}");continue; }
            // The torso run's first slow left stroke was scored again on its
            // low-motion return. Two requested slow strokes produced three
            // saved commits at 20.010, 20.511 and 21.520 seconds.
            bool repeatedReturn=position=="torso" && stroke.x==0 && Mathf.Abs(stroke.y-20.511f)<.03f;
            bool retained=newPunches.Exists(n=>n.x==stroke.x && Mathf.Abs(n.y-stroke.y)<.25f);
            Check(repeatedReturn ? !retained : retained,$"{position} {(repeatedReturn?"rejects repeated return":"retains outgoing stroke")} {stroke.x} at {stroke.y:F3}");
        }
        Check(punchNew>=punchOld-(position=="torso"?1:0)-partial,position+" retains punch-section decisions excluding the identified repeated return and 1.21 partial strokes");
        Check(partial<=2,position+" partial-stroke exemptions stay rare ("+partial+")");
    }
}
