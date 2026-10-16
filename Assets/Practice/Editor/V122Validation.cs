using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;

// App 1.22: two-arm gestures are not punches, real overhead arms from raw 3-D, lifted-foot stance fix.
// Sessions: A (1.20 game body), B/C (1.20 boxing), D (1.21 game body), E (1.21 boxing).
public static class V122Validation
{
    [Serializable] private class Record
    {
        public string kind,label;
        public double recordingStartedAt;
        public bool accepted;
        public Vector3[] rawWorld;
        public float[] rawConfidence;
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
            Units();
            var a=Punches("20261010-A");var b=Punches("20261010-B");var c=Punches("20261010-C");var d=Punches("20261010-D");var e=Punches("20261010-E");
            foreach(var (name,cues) in new[]{("B",b),("C",c)})
            {
                Expect(name,cues,"LEFT PUNCHES · SLOW","LL");Expect(name,cues,"LEFT PUNCHES · FAST","LLLL");
                Expect(name,cues,"RIGHT PUNCHES · SLOW","RR");Expect(name,cues,"RIGHT PUNCHES · FAST","RRRR");
                Expect(name,cues,"ALTERNATE LEFT / RIGHT","LRLRL");Expect(name,cues,"RAISE AND LOWER YOUR HANDS","");
            }
            Expect("E",e,"LEFT PUNCHES · SLOW","LL");Expect("E",e,"LEFT PUNCHES · FAST","LLLL");
            Expect("E",e,"RIGHT PUNCHES · SLOW","RR");Expect("E",e,"RIGHT PUNCHES · FAST","RRRRR");
            Expect("E",e,"RAISE AND LOWER YOUR HANDS","");
            Expect("A",a,"SLOW PUNCHES · LEFT / RIGHT","LLRR");Expect("A",a,"FAST PUNCHES · ALTERNATE","LRLRLR");
            foreach(var (name,cues) in new[]{("A",a),("D",d)})
                Expect(name,cues,"BOTH ARMS FORWARD · BEND ELBOWS","");
            Expect("D",d,"BOTH ARMS OVERHEAD · LOWER","");
            Overhead("20261010-A",true);Overhead("20261010-B",true);Overhead("20261010-C",true);Overhead("20261010-D",true);Overhead("20261010-E",true);
            report.AppendLine($"PASS {checks} app 1.22 checks");
        }
        catch(Exception x) { report.AppendLine("FAIL "+x);throw; }
        finally { File.WriteAllText("Logs/Practice/v122-validation.txt",report.ToString());Debug.Log(report.ToString()); }
    }
    private static void Expect(string session,Dictionary<string,string> cues,string cue,string expected)
    {
        cues.TryGetValue(cue,out var got);got??="";
        Check(got==expected,$"{session} {cue}: punches '{got}' == '{expected}'");
    }
    // Detector replay with the controller's two-arm rule (same public rule, same order as the game loop).
    private static Dictionary<string,string> Punches(string session)
    {
        var detectors=new[]{new BoxingPunchIntent(),new BoxingPunchIntent()};
        var raw=new[]{double.NegativeInfinity,double.NegativeInfinity};double until=double.NegativeInfinity;
        var cues=new Dictionary<string,string>();string cue="setup";
        foreach(string line in File.ReadLines("Logs/MediaPipe/"+session+"/input/"+session+".jsonl"))
        {
            if(line.StartsWith("{\"kind\":\"guide_cue\"")) { cue=JsonUtility.FromJson<Record>(line).label;continue; }
            if(!line.StartsWith("{\"kind\":\"observation\"")) continue;
            var f=JsonUtility.FromJson<Record>(line).observation;
            var s=new[]{detectors[0].Observe(f.left,f.sequence,f.time,false),detectors[1].Observe(f.right,f.sequence,f.time,false)};
            var gesture=new bool[2];
            for(int side=0;side<2;side++)
                if(s[side]==PunchIntent.Signal.Commit)
                {
                    var o=detectors[1-side];
                    gesture[side]=AssistedPunchController.TwoArmGesture(o.Extension,o.Phase,s[1-side]==PunchIntent.Signal.Commit,f.time-raw[1-side],f.time<until);
                }
            for(int side=0;side<2;side++)
            {
                if(s[side]==PunchIntent.Signal.Commit) raw[side]=f.time;
                if(gesture[side]) { until=f.time+.4;continue; }
                if(s[side]==PunchIntent.Signal.Commit) cues[cue]=(cues.TryGetValue(cue,out var v)?v:"")+(side==0?"L":"R");
            }
        }
        foreach(var kv in cues) report.AppendLine($"  {session} {kv.Key}: {kv.Value}");
        return cues;
    }
    // Raw camera-frame 3-D (reconstruction.rawWorld, MediaPipe 33 order, y up) through OverheadArms.
    private static void Overhead(string session,bool expectRaise)
    {
        var detector=new OverheadArms();string cue="setup";
        var active=new Dictionary<string,int>();
        foreach(string line in File.ReadLines("Logs/MediaPipe/"+session+"/input/"+session+".jsonl"))
        {
            if(line.StartsWith("{\"kind\":\"guide_cue\"")) { cue=JsonUtility.FromJson<Record>(line).label;continue; }
            if(!line.StartsWith("{\"kind\":\"reconstruction\"")) continue;
            var r=JsonUtility.FromJson<Record>(line);
            if(!r.accepted || r.rawWorld==null || r.rawWorld.Length<33) continue;
            var w=r.rawWorld;var c=r.rawConfidence;
            bool seen=c[11]>=.5f && c[12]>=.5f && c[13]>=.5f && c[14]>=.5f && c[15]>=.5f && c[16]>=.5f;
            detector.Observe(seen,w[11],w[13],w[15],w[12],w[14],w[16]);
            if(detector.Active) active[cue]=(active.TryGetValue(cue,out var n)?n:0)+1;
        }
        foreach(var kv in active) report.AppendLine($"  {session} overhead active in {kv.Key}: {kv.Value} frames");
        bool raised=false;
        foreach(var kv in active)
        {
            bool raiseCue=kv.Key.Contains("OVERHEAD") || kv.Key.StartsWith("RAISE") || kv.Key.Contains("TO SIDES");
            Check(raiseCue,$"{session}: overhead arms only during arm-raise cues ({kv.Key})");
            raised|=raiseCue && !kv.Key.Contains("TO SIDES");
        }
        if(expectRaise) Check(raised,$"{session}: the arms-overhead / raise cue is recognised as overhead");
    }
    private static void Units()
    {
        // Lifted foot follows the player's own stance (0.28 m), not the avatar's 0.96 m stance.
        var standing=new Vector3(-.28f,0,0);var lifted=new Vector3(-.16f,.6f,.05f);
        var off=AssistedPunchController.LiftedFootOffset(lifted,standing);
        Check(Mathf.Abs(off.x-.12f)<.001f && Mathf.Abs(off.z-.05f)<.001f,$"Knee lift moves the foot 12 cm inward as observed, not 60 cm ({off.x:F2})");
        Check(AssistedPunchController.LiftedFootOffset(standing,standing)==Vector3.zero,"Standing stance gives no offset");
        // Held knee lift rebuilt from observed thigh/shin: thigh forward-level, shin hanging, with a sideways-noisy
        // camera thigh. The rebuilt ankle stays under the knee and the knee bends forward.
        var kneeThigh=new Vector3(.20f,-.02f,.40f);var kneeShin=new Vector3(.02f,-.42f,.05f);
        var leg=AssistedPunchController.LiftedLegOffset(kneeThigh,kneeShin,.45f,.45f);
        var kneeAt=new Vector3(kneeThigh.x*.25f,kneeThigh.y,kneeThigh.z).normalized*.45f;
        Check(Mathf.Abs(leg.x-kneeAt.x)<.03f && leg.z>.3f && leg.y<-.35f,$"Held knee lift: foot hangs under the forward knee ({leg:F2}, knee {kneeAt:F2})");
        Check(Mathf.Abs(kneeAt.x)<.07f,"Camera thigh sideways noise moves the knee less than 7 cm");
        var pole=AssistedPunchController.LiftedKneePole(new Vector3(.9f,.1f,-.2f));
        Check(pole.z>=.85f && Mathf.Abs(pole.x)<.4f,$"Knee pole stays forward even with a sideways/backward thigh reading ({pole:F2})");
        Check(AssistedPunchController.LiftUnderHip(.02f)==0 && AssistedPunchController.LiftUnderHip(.30f)==1,"Low lifts keep the stance foot; clear lifts use the rebuilt leg");
        // Knee lift then straighten (session D mistake): shin forward follows.
        var straight=AssistedPunchController.LiftedLegOffset(kneeThigh,new Vector3(0,-.05f,.42f),.45f,.45f);
        Check(straight.z>.75f,$"Knee lift plus leg straighten renders the straightened leg ({straight:F2})");
        // Two-arm rule.
        Check(AssistedPunchController.TwoArmGesture(.46f,PunchIntent.State.Candidate,false,9,false),"Both hands rising together is a two-arm gesture");
        Check(!AssistedPunchController.TwoArmGesture(.03f,PunchIntent.State.Candidate,false,9,false),"Next punch just starting in a combination is not a two-arm gesture");
        Check(!AssistedPunchController.TwoArmGesture(.6f,PunchIntent.State.Spent,false,.5,false),"Other hand still returning from an earlier punch is not a two-arm gesture");
        // Overhead detector: bilateral, two frames, hysteresis.
        var d=new OverheadArms();Vector3 ls=new Vector3(-.18f,1.4f,0),rs=new Vector3(.18f,1.4f,0);
        Vector3 Up(Vector3 s,float deg,float len) => s+new Vector3(0,Mathf.Sin(deg*Mathf.Deg2Rad),Mathf.Cos(deg*Mathf.Deg2Rad))*len;
        d.Observe(true,ls,Up(ls,82,.28f),Up(ls,82,.56f),rs,Up(rs,82,.28f),Up(rs,82,.56f));
        Check(!d.Active,"One overhead frame is not enough");
        d.Observe(true,ls,Up(ls,82,.28f),Up(ls,82,.56f),rs,Up(rs,82,.28f),Up(rs,82,.56f));
        Check(d.Active,"Two straight overhead frames show raised arms");
        d.Observe(true,ls,Up(ls,70,.28f),Up(ls,70,.56f),rs,Up(rs,70,.28f),Up(rs,70,.56f));
        Check(d.Active,"Hysteresis keeps arms up while slightly lowering");
        d.Observe(true,ls,Up(ls,45,.28f),Up(ls,45,.56f),rs,Up(rs,45,.28f),Up(rs,45,.56f));
        Check(!d.Active,"Arms forward (45 degrees) are not overhead");
        d.Reset();
        for(int i=0;i<3;i++) d.Observe(true,ls,Up(ls,82,.28f),Up(ls,82,.56f),rs,Up(rs,0,.28f),Up(rs,-10,.56f));
        Check(!d.Active,"A single raised arm (e.g. an upward-projected punch) is not overhead arms");
    }
}
