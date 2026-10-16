using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;

public static class BoxingPriorityValidation
{
    [Serializable] private class Record
    {
        public string kind,label;
        public double recordedAt,recordingStartedAt;
        public PunchCameraInput.Frame observation;
    }
    private struct Decision { public int side;public double time;public string cue; }
    private static readonly StringBuilder report=new StringBuilder();
    private static void Check(bool ok,string message)
    { if(!ok)throw new Exception(message);report.AppendLine("PASS "+message); }
    public static void RunBatch()
    {
        try { Run();MediaPipeOctoberReplayValidation.Run();MediaPipeBoxingValidation.Run();PunchValidation.Run();EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
    public static void Run()
    {
        report.Clear();Directory.CreateDirectory("Logs/Practice");
        try
        {
            Recovery();
            SingleUpwardStroke();
            CancelledPreparation();
            Replay("20261007-163114-549",23);
            Replay("20261007-170107-047",26);
            Replay("20261007-172344-665",22);
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { File.WriteAllText("Logs/Practice/boxing-priority-validation.txt",report.ToString());Debug.Log(report.ToString()); }
    }
    private static BoxingPunchIntent Prime()
    {
        var d=new BoxingPunchIntent();
        for(int i=0;i<8;i++) d.Observe(MediaPipeBoxingValidation.Guard(),i+1,i*.1);
        Check(d.GuardLearned,"Observed guard acquired");return d;
    }
    private static void Recovery()
    {
        var d=Prime();var guard=MediaPipeBoxingValidation.Guard();var jump=guard;jump.wrist.y+=1.6f;
        Check(d.Observe(jump,9,.8)!=PunchIntent.Signal.Commit && d.RecoveringTracking,"Isolated landmark jump cannot punch");
        d.Observe(guard,10,.9);
        d.Observe(guard,10,.95);
        Check(d.RecoveringTracking,"Duplicate frame cannot satisfy guard recovery");
        Check(d.Observe(guard,11,1)==PunchIntent.Signal.None && !d.RecoveringTracking && d.Phase==PunchIntent.State.Guard,"Two fresh stable guard observations recover without committing");
        Check(d.Observe(MediaPipeBoxingValidation.Extended(),12,1.1)==PunchIntent.Signal.Commit,"Next punch survives isolated tracking glitch");
        d=Prime();d.Observe(jump,9,.8);
        for(int i=10;i<16;i++) d.Observe(jump,i,i*.1);
        Check(!d.GuardLearned && !d.RecoveringTracking,"Unrecovered glitch times out instead of retaining stale guard");
        d=Prime();d.Observe(jump,9,.8);guard.handConfidence=0;d.Observe(guard,10,.9);
        Check(!d.GuardLearned && !d.RecoveringTracking,"Missing wrist still requires full guard reacquisition");
    }
    private static void SingleUpwardStroke()
    {
        var d=Prime();var guard=MediaPipeBoxingValidation.Guard();var up=guard;up.wrist.y+=.35f;
        // Depth is unchanged: intentionally ambiguous lift/uppercut from a floor view.
        Check(d.Observe(up,9,.8)==PunchIntent.Signal.Commit,"Single upward stroke from guard uses boxing prior even without depth extension");
        int extra=0;for(int i=10;i<16;i++) if(d.Observe(up,i,i*.1)==PunchIntent.Signal.Commit)extra++;
        if(d.Observe(guard,16,1.6)==PunchIntent.Signal.Commit)extra++;
        Check(extra==0,"Holding and returning an upward stroke do not add punches");
        d=Prime();var gate=new BoxingGestureGate();
        gate.Observe(new PunchCameraInput.Frame{sequence=8,time=.7,left=guard,right=guard});
        gate.Observe(new PunchCameraInput.Frame{sequence=9,time=.8,left=up,right=up});
        Check(gate.Blocked && d.Observe(up,9,.8,gate.Blocked)!=PunchIntent.Signal.Commit,"Clearly paired upward arms remain a distinct gesture");
    }
    private static bool Near(List<Decision> events,int side,double at,double tolerance=.25)
        => events.Exists(e=>e.side==side && Math.Abs(e.time-at)<=tolerance);
    private static void CancelledPreparation()
    {
        var d=Prime();var arm=MediaPipeBoxingValidation.Guard();arm.wrist.x+=.14f;arm.wristWorld.z+=.17f;
        Check(d.Observe(arm,9,.8)==PunchIntent.Signal.Prepare,"Modest observed extension prepares without a premature commit");
        Check(d.Observe(arm,10,.9)==PunchIntent.Signal.Cancel && d.Phase==PunchIntent.State.Spent,"Stationary preparation cancels and waits for guard");
        var guard=MediaPipeBoxingValidation.Guard();
        Check(d.Observe(guard,11,1.01)==PunchIntent.Signal.None,"Return after cancellation cannot become a fresh punch");
        Check(d.Phase==PunchIntent.State.Guard,"Observed folded return rearms a cancelled preparation");
        Check(d.Observe(MediaPipeBoxingValidation.Extended(),12,1.11)==PunchIntent.Signal.Commit,"A new outgoing stroke works after cancellation and guard return");
    }
    private static void Replay(string session,int savedCount)
    {
        var before=new[]{new BoxingPunchIntent111(),new BoxingPunchIntent111()};
        var after=new[]{new BoxingPunchIntent(),new BoxingPunchIntent()};var gate=new BoxingGestureGate();
        var oldEvents=new List<Decision>();var newEvents=new List<Decision>();
        var trace=new StringBuilder();string cue="setup";double start=0;
        foreach(string line in File.ReadLines("Logs/MediaPipe/"+session+"/input/"+session+".jsonl"))
        {
            var r=JsonUtility.FromJson<Record>(line);
            if(r.kind=="session")start=r.recordingStartedAt;
            if(r.kind=="guide_cue")cue=r.label;
            if(r.kind!="observation")continue;
            var f=r.observation;gate.Observe(f);
            for(int side=0;side<2;side++)
            {
                var arm=side==0?f.left:f.right;
                var origin=(PunchArmObservation)typeof(BoxingPunchIntent).GetField("origin",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(after[side]);
                var old=before[side].Observe(arm,f.sequence,f.time,gate.Blocked);
                var newer=after[side].Observe(arm,f.sequence,f.time,gate.Blocked);
                if(old==PunchIntent.Signal.Commit)oldEvents.Add(new Decision{side=side,time=f.time-start,cue=cue});
                if(newer==PunchIntent.Signal.Commit)newEvents.Add(new Decision{side=side,time=f.time-start,cue=cue});
                if(old!=PunchIntent.Signal.None || newer!=PunchIntent.Signal.None || after[side].RecoveringTracking || f.time-start>46.5 && f.time-start<47.4 || f.time-start>60.1 && f.time-start<60.7)
                    trace.AppendLine($"{f.time-start:F3} {(side==0?"L":"R")} old={old} new={newer} {after[side].Phase} {after[side].Reason} speed={after[side].Speed:F3} travel={after[side].Travel:F3} imageOpening={origin.Bend-arm.Bend:F1} worldOpening={origin.WorldBend-arm.WorldBend:F1} forward={arm.wristWorld.z-origin.wristWorld.z:F3} reach={arm.wristWorld.magnitude-origin.wristWorld.magnitude:F3} {cue}");
            }
        }
        File.WriteAllText("Logs/MediaPipe/"+session+"/punch-priority-replay.txt",trace.ToString());
        report.AppendLine($"REPLAY {session}: saved={savedCount}, baseline={oldEvents.Count}, revised={newEvents.Count}");
        foreach(var e in newEvents)report.AppendLine($"COMMIT {e.time:F3} {(e.side==0?"L":"R")} {e.cue}");
        Check(oldEvents.Count==savedCount,session+" reproduces saved v1.12 commits before changing logic");
        foreach(var e in oldEvents)
        {
            bool falseSupport=session=="20261007-170107-047" && (e.side==0 && Math.Abs(e.time-46.951)<.05 || e.side==1 && Math.Abs(e.time-47.170)<.05);
            bool positive=e.cue.Contains("PUNCHES") || e.cue.StartsWith("ALTERNATE");
            // Cue changes precede audible completion. Preserve video-confirmed continuations too.
            positive|=session=="20261007-172344-665" && e.time>58 && e.time<61;
            positive|=session=="20261007-170107-047" && e.time>58 && e.time<61;
            // A stronger early observation can replace the old endpoint/return
            // decision. Match within the same manually reviewed stroke window.
            if(positive && !falseSupport)Check(newEvents.Exists(n=>n.side==e.side && n.time>=e.time-.45 && n.time<=e.time+.25),$"{session} retains observed outgoing stroke {e.side} at {e.time:F3}");
        }
        Check(!newEvents.Exists(e=>e.cue.StartsWith("HOLD") || e.cue=="STEP OUT, THEN STEP BACK"),session+" has no idle/step-out commits");
        if(session=="20261007-163114-549")
        {
            Check(newEvents.Exists(e=>e.side==1 && e.time>56.6 && e.time<57.2),"Recorded right punch after 56.37s tracking jump is recovered");
            Check(!Near(newEvents,1,57.569,.10),"Cancelled preparation's return at 57.57s cannot create an extra right punch");
        }
        if(session=="20261007-170107-047")
        {
            Check(!Near(newEvents,0,46.951,.16),"Support-hand depth artifact at 46.95s does not commit");
            Check(!Near(newEvents,1,47.170,.16),"Settled/returning right hand at 47.17s does not confirm");
        }
        if(session=="20261007-172344-665")Check(!newEvents.Exists(e=>e.cue=="RAISE AND LOWER YOUR HANDS"),"Recorded clear bilateral raise remains excluded");
    }
}
