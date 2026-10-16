using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;

// App 1.21: punch commit rule, followed/held punch arm, forward-not-overhead arms, kick shaping.
// Expectations come from the 1.20 phone sessions A (game body), B and C (boxing).
public static class V121Validation
{
    [Serializable] private class Record
    {
        public string kind,label;
        public double recordingStartedAt;
        public PunchCameraInput.Frame observation;
    }
    private static readonly StringBuilder report=new StringBuilder();
    private static int checks;
    private static void Check(bool ok,string message)
    { if(!ok)throw new Exception(message);checks++;report.AppendLine("PASS "+message); }
    public static void RunBatch()
    {
        try { Run();EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
    public static void Run()
    {
        report.Clear();checks=0;Directory.CreateDirectory("Logs/Practice");
        try
        {
            Functions();
            var b=Replay("20261010-B");var c=Replay("20261010-C");var a=Replay("20261010-A");
            foreach(var (name,cues) in new[]{("B",b),("C",c)})
            {
                Expect(name,cues,"LEFT PUNCHES · SLOW","LL");
                Expect(name,cues,"LEFT PUNCHES · FAST","LLLL");
                Expect(name,cues,"RIGHT PUNCHES · SLOW","RR");
                Expect(name,cues,"RIGHT PUNCHES · FAST","RRRR");
                Expect(name,cues,"ALTERNATE LEFT / RIGHT","LRLRL");
                Check(Count(cues,"START A PUNCH, THEN WITHDRAW")<=1,$"{name}: withdrawn starts no longer commit (only the full-extension stroke at the cue change)");
            }
            Expect("A",a,"SLOW PUNCHES · LEFT / RIGHT","LLRR");
            Expect("A",a,"FAST PUNCHES · ALTERNATE","LRLRLR");
            foreach(string kick in new[]{"KNEE STRIKES · LEFT / RIGHT","LEFT FRONT KICKS","RIGHT FRONT KICKS","SIDE KICKS · LEFT / RIGHT","TORSO FORWARD BENDS","SQUAT · HOLD · STAND"})
                Check(Count(a,kick)==0,$"A {kick}: arm swings/bends do not commit punches");
            // 1.16 session: the old detector re-committed single right strokes (7 rights vs 4 lefts in 2.4 s).
            var old=Replay("20261009-151451-407");
            Expect("1.16",old,"SLOW PUNCHES · LEFT / RIGHT","LLRR");
            Expect("1.16",old,"FAST PUNCHES · ALTERNATE","LRLRLRLR");
            Check(Count(old,"TORSO FORWARD BENDS")==0 && Count(old,"QUIET GUARD · RECOVERY")==0 && Count(old,"KNEE STRIKES · LEFT / RIGHT")==0,
                "1.16: torso bend, quiet guard and knee-strike arm swings do not commit punches");
            report.AppendLine($"PASS {checks} app 1.21 checks");
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { File.WriteAllText("Logs/Practice/v121-validation.txt",report.ToString());Debug.Log(report.ToString()); }
    }
    private static int Count(Dictionary<string,string> cues,string cue) => cues.TryGetValue(cue,out var v) ? v.Length : 0;
    private static void Expect(string session,Dictionary<string,string> cues,string cue,string expected)
    {
        cues.TryGetValue(cue,out var got);
        Check(got==expected,$"{session} {cue}: commits {got ?? "(none)"} == {expected}");
    }
    // Detector replay with the 1.21 boxing policy (no overhead block; side gestures/kicks are controller gates).
    private static Dictionary<string,string> Replay(string session)
    {
        string path="Logs/MediaPipe/"+session+"/input/"+session+".jsonl";
        var detectors=new[]{new BoxingPunchIntent(),new BoxingPunchIntent()};
        var cues=new Dictionary<string,string>();string cue="setup";
        var trace=new StringBuilder();double start=0;
        foreach(string line in File.ReadLines(path))
        {
            if(!line.StartsWith("{\"kind\":\"observation\"") && !line.StartsWith("{\"kind\":\"guide_cue\"") && !line.StartsWith("{\"kind\":\"session\"")) continue;
            var r=JsonUtility.FromJson<Record>(line);
            if(r.kind=="session") start=r.recordingStartedAt;
            if(r.kind=="guide_cue") cue=r.label;
            if(r.kind!="observation") continue;
            var f=r.observation;
            for(int side=0;side<2;side++)
            {
                var signal=detectors[side].Observe(side==0?f.left:f.right,f.sequence,f.time,false);
                if(signal!=PunchIntent.Signal.None)
                    trace.AppendLine($"{f.time-start:F3} {(side==0?"L":"R")} {signal} {detectors[side].Reason} ext={detectors[side].Extension:F2} {cue}");
                if(signal==PunchIntent.Signal.Commit)
                    cues[cue]=(cues.TryGetValue(cue,out var v)?v:"")+(side==0?"L":"R");
            }
        }
        File.WriteAllText("Logs/MediaPipe/"+session+"/v121-punch-replay.txt",trace.ToString());
        foreach(var kv in cues) report.AppendLine($"  {session} {kv.Key}: {kv.Value}");
        return cues;
    }
    private static void Functions()
    {
        // Strike extension: starts from the followed extension, reaches contact, holds while the player holds.
        Check(Mathf.Approximately(AssistedPunchController.StrikeExtension(.12f,.4f,0),.4f),"Strike continues from the followed preparation (no pull-back at commit)");
        Check(Mathf.Approximately(AssistedPunchController.StrikeExtension(.45f,0,0),1),"Strike reaches full extension at contact");
        Check(AssistedPunchController.StrikeExtension(.95f,0,0)<.05f,"Fast punch retracts on its own");
        Check(AssistedPunchController.StrikeExtension(.9f,0,1)>=AssistedPunchController.HoldCap-.001f,"Slow punch stays out while the player's arm is out");
        // Reach target: arm straightens toward the bag, at most 6 cm into the surface.
        var shoulder=new Vector3(.22f,1.26f,.06f);var surface=new Vector3(.17f,1.16f,.58f);
        var target=AssistedPunchController.BoxingReachTarget(shoulder,surface,.58f,Vector3.forward);
        float into=Vector3.Distance(shoulder,target)-Vector3.Distance(shoulder,surface);
        Check(into>.0f && into<=.1001f,$"Punch target extends the arm up to 10 cm past the bag surface ({into*100:F1} cm)");
        var far=AssistedPunchController.BoxingReachTarget(shoulder,shoulder+Vector3.forward*2,.58f,Vector3.forward);
        Check(Mathf.Abs(Vector3.Distance(shoulder,far)-.58f*.97f)<.001f,"Out-of-reach bag: arm extends to its reach, no stretching");
        // Arms never rendered overhead: height above the shoulder becomes forward reach, length preserved.
        var up=new Vector3(.05f,.55f,.1f);float length=up.magnitude;
        Check(AssistedPunchController.ForwardInsteadOfOverhead(ref up,.58f) && up.y<=.12f*.58f+1e-5f && up.z>.4f && Mathf.Abs(up.magnitude-length)<1e-4f,
            "Raised arm is rendered as a forward reach at shoulder height");
        var guard=new Vector3(.05f,-.2f,.3f);var before=guard;
        Check(!AssistedPunchController.ForwardInsteadOfOverhead(ref guard,.58f) && guard==before,"Guard and low arms are unchanged");
        var side=new Vector3(.55f,.02f,.05f);var sideBefore=side;
        Check(!AssistedPunchController.ForwardInsteadOfOverhead(ref side,.58f) && side==sideBefore,"Arms out to the sides are unchanged");
        // Kick path: chamber, extension held through contact window, re-chamber, plant.
        Vector3 start=Vector3.zero,chamber=new Vector3(0,.6f,.35f),hit=new Vector3(0,.95f,.6f),plant=new Vector3(0,0,.05f);
        Check(AssistedPunchController.KickFootPath(.14f,start,chamber,hit,plant)==chamber,"Kick reaches chamber first");
        Check(AssistedPunchController.KickFootPath(.36f,start,chamber,hit,plant)==hit && AssistedPunchController.KickFootPath(.5f,start,chamber,hit,plant)==hit,
            "Foot is fully extended from phase .36 to .52 (inside the .20-.62 contact window)");
        Check(AssistedPunchController.KickFootPath(.74f,start,chamber,hit,plant)==chamber && AssistedPunchController.KickFootPath(1,start,chamber,hit,plant)==plant,"Kick re-chambers, then plants");
        Check(AssistedPunchController.KickExtensionEnvelope(.1f)==0 && AssistedPunchController.KickExtensionEnvelope(.45f)==1 && AssistedPunchController.KickExtensionEnvelope(.9f)==0,"Kick body envelope follows the leg");
        float back=AssistedPunchController.KickHipSetBack(new Vector3(0,.76f,0),new Vector3(0,.98f,.56f),.84f,Vector3.forward);
        Check(back>.1f && back<=.2f,$"Close-range teep sets the hips back {back*100:F0} cm so the leg can straighten");
        Check(AssistedPunchController.KickHipSetBack(new Vector3(0,.76f,0),new Vector3(0,.98f,.9f),.84f,Vector3.forward)==0,"No set-back when the bag is already at leg length");
    }
}
