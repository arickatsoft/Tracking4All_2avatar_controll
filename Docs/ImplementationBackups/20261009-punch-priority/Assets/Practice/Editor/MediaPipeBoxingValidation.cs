using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FitFight.Practice;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

public static class MediaPipeBoxingValidation
{
    private static readonly StringBuilder report=new StringBuilder();
    private static void Check(bool value,string message) { if(!value) throw new Exception(message);report.AppendLine("PASS "+message); }
    public static PunchArmObservation Guard() => new PunchArmObservation { confidence=1,handConfidence=1,elbowConfidence=1,hasWorld=true,
        wrist=new Vector2(.15f,.08f),elbow=new Vector2(.34f,-.30f),wristWorld=new Vector3(.1f,.08f,.32f),elbowWorld=new Vector3(.30f,-.30f,.05f) };
    public static PunchArmObservation Extended()
    { var a=Guard();a.wrist=new Vector2(.13f,.60f);a.elbow=new Vector2(.18f,-.08f);a.wristWorld=new Vector3(.1f,.05f,.92f);a.elbowWorld=new Vector3(.2f,-.1f,.48f);return a; }
    private static BoxingPunchIntent Prime()
    { var d=new BoxingPunchIntent();for(int i=0;i<8;i++) d.Observe(Guard(),i+1,i*.1);Check(d.GuardLearned,"Learn an observed folded guard");return d; }
    public static void RunBatch()
    {
        try { Run();PunchValidation.Run();MediaPipeIdleValidation.Run();MediaPipeImprovementValidation.Run();EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
    public static void Run()
    {
        report.Clear();Directory.CreateDirectory("Logs/Practice");
        try
        {
            var d=Prime();Check(d.Observe(Extended(),9,.8)==PunchIntent.Signal.Commit,"A strong upward-projected punch commits on one camera observation");
            Check(d.Observe(Extended(),9,.9)==PunchIntent.Signal.None,"Duplicate capture cannot commit twice");
            int held=0;for(int i=10;i<15;i++) if(d.Observe(Extended(),i,i*.1)==PunchIntent.Signal.Commit) held++;
            Check(held==0,"Held extension never produces another punch");
            Check(d.Observe(Guard(),15,1.5)==PunchIntent.Signal.None && d.Phase==PunchIntent.State.Guard,"Observed guard return rearms the same hand");
            Check(d.Observe(Extended(),16,1.6)==PunchIntent.Signal.Commit,"Second outgoing stroke works immediately after return");
            d=Prime();var partial=Extended();partial.elbowConfidence=0;partial.confidence=0;
            Check(d.Observe(partial,9,.8)==PunchIntent.Signal.Commit,"Visible wrist plus depth supports a briefly hidden elbow");
            d=Prime();var missing=Extended();missing.handConfidence=0;
            Check(d.Observe(missing,9,.8)!=PunchIntent.Signal.Commit && !d.GuardLearned,"An invisible wrist cannot invent a strike");
            d=Prime();Check(d.Observe(Extended(),9,1.2)!=PunchIntent.Signal.Commit && !d.GuardLearned,"A stale capture gap requires guard reacquisition");
            d=Prime();int noise=0;
            for(int i=9;i<109;i++) { var a=Guard();a.wrist.x+=Mathf.Sin(i)*.008f;a.wristWorld.z+=Mathf.Sin(i)*.10f;if(d.Observe(a,i,i*.1)==PunchIntent.Signal.Commit) noise++; }
            Check(noise==0,"Idle image jitter and depth noise cannot commit");
            d=Prime();var overhead=Extended();overhead.wrist.y=1;
            Check(BoxingFeatures.Overhead(overhead,overhead),"Clear bilateral overhead posture is distinct");
            Check(d.Observe(overhead,9,.8,true)!=PunchIntent.Signal.Commit,"Overhead gate prevents a boxing commit");
            var m=BoxingFeatures.AddWorld(Guard(),new Vector3(-.2f,.5f,0),new Vector3(.2f,.5f,0),Vector3.zero,new Vector3(-.2f,.2f,.1f),new Vector3(-.2f,.5f,-.4f),1,1);
            var n=BoxingFeatures.AddWorld(Guard(),new Vector3(.2f,.5f,0),new Vector3(-.2f,.5f,0),Vector3.zero,new Vector3(.2f,.2f,.1f),new Vector3(.2f,.5f,-.4f),1,1);
            Check(m.hasWorld && n.hasWorld && Mathf.Abs(m.wristWorld.z-n.wristWorld.z)<.0001f && m.wristWorld.z>0,"World forward sign is consistent for both arms");
            Replay();Summary();report.AppendLine("PASS boxing validation complete");
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { File.WriteAllText("Logs/Practice/mediapipe-boxing-validation.txt",report.ToString());Debug.Log(report.ToString()); }
    }
    private static void Replay()
    {
        string path="Logs/MediaPipe/20261002-125522/input/20261002-125522-238.jsonl";
        var newer=new[]{new BoxingPunchIntent(),new BoxingPunchIntent()};var older=new[]{new PunchIntent(),new PunchIntent()};var gate=new PunchGestureGate();
        var counts=new Dictionary<string,int[]>();var detail=new StringBuilder();
        foreach(string line in File.ReadLines(path))
        {
            if(!line.Contains("\"kind\":\"reconstruction\"")) continue;
            var d=JsonUtility.FromJson<RTMPosePoseProvider.Diagnostics>(line);if(!d.accepted) continue;
            Vector2 P(int i) => new Vector2(d.rawImage[i].x*.5625f,1-d.rawImage[i].y);
            float C(int i) => d.rawConfidence[i];
            var arms=new PunchArmObservation[2];
            for(int s=0;s<2;s++)
            {
                float torso=Mathf.Min(C(11),C(12),C(23),C(24));
                PunchFeatures.TryCreate(P(11+s),P(12-s),(P(23)+P(24))*.5f,P(13+s),P(15+s),Mathf.Min(torso,C(13+s),C(15+s)),out arms[s]);
                arms[s]=BoxingFeatures.AddWorld(arms[s],d.rawWorld[11+s],d.rawWorld[12-s],(d.rawWorld[23]+d.rawWorld[24])*.5f,d.rawWorld[13+s],d.rawWorld[15+s],Mathf.Min(torso,C(15+s)),C(13+s));
            }
            var frame=new PunchCameraInput.Frame{sequence=d.sequence,time=d.capturedAt,left=arms[0],right=arms[1]};gate.Observe(frame);
            if(!counts.ContainsKey(d.movementLabel)) counts[d.movementLabel]=new int[2];
            for(int s=0;s<2;s++)
            {
                var old=older[s].Observe(arms[s],d.sequence,d.capturedAt,gate.Blocked?gate.Reason:null);
                var updated=newer[s].Observe(arms[s],d.sequence,d.capturedAt,BoxingFeatures.Overhead(arms[0],arms[1]));
                if(old==PunchIntent.Signal.Commit) counts[d.movementLabel][0]++;
                if(updated==PunchIntent.Signal.Commit) counts[d.movementLabel][1]++;
                if(updated!=PunchIntent.Signal.None) detail.AppendLine($"{d.movementLabel} {d.capturedAt:F3} side={s} {updated} {newer[s].Reason}");
            }
        }
        foreach(var row in counts) report.AppendLine($"REPLAY {row.Key}: legacy commits={row.Value[0]}, boxing commits={row.Value[1]}");
        File.WriteAllText("Logs/MediaPipe/20261002-125522/boxing-replay-decisions.txt",detail.ToString());
        Check(counts["still_start"][1]==0 && counts["still_end"][1]==0,"Recorded stillness produces zero boxing commits");
        Check(counts["arms_overhead"][1]==0,"Recorded bilateral overhead exercise produces zero boxing commits");
        Check(counts["punch_fast"][1]>0 && counts["punch_slow"][1]>0,"Both recorded punch segments activate boxing assistance");
    }
    private static void Summary()
    {
        var s=new BoxingSessionStats("test","MediaPipe Full","test",0);s.Cue("punch");s.Signal(0,PunchIntent.Signal.Commit,1,1.1);s.Observation(new PunchCameraInput.Frame{boxing=true,left=Guard(),right=Guard()},1.1);s.Finish(true,null,1,2);
        Check(s.segments[0].leftCommitted==1 && s.hits==1 && Mathf.Abs(s.decisionMedianMs-100)<.01f,"Summary records decisions, hits and capture-to-decision delay separately");
        var writer=new PunchSessionRecorder("{\"kind\":\"boxing_summary_validation\"}","BoxingValidation");
        writer.SaveSummary(JsonUtility.ToJson(s),s.Markdown());writer.Dispose();
        var deadline=DateTime.UtcNow.AddSeconds(3);while(!writer.Finished && DateTime.UtcNow<deadline) System.Threading.Thread.Sleep(10);
        Check(writer.Finished && writer.Error==null && File.Exists(Path.ChangeExtension(writer.Path,"summary.json")) && File.Exists(Path.ChangeExtension(writer.Path,"summary.md")),"Background writer persists both summary files before reporting finished");
    }
}
