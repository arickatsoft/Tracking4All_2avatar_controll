using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FitFight.BodyTracking;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;

// App 1.23: facing-swap guard, lean-proof squat, drive-and-stick knee strikes. Replays the 1.22 phone session (F)
// and the earlier full-body sessions to show the changes add the missed knees without scoring holds or idle cues.
public static class V123Validation
{
    private const string F="Logs/MediaPipe/20261011-F/input/20261011-F.jsonl";
    private const string D="Logs/MediaPipe/20261010-D/input/20261010-D.jsonl";
    private const string Oct9="Logs/MediaPipe/20261009-192549-873/input/20261009-192549-873.jsonl";
    private const string Oct9a="Logs/MediaPipe/20261009-151451-407/input/20261009-151451-407.jsonl";
    [Serializable] private class Cue { public string kind,label;public double recordedAt,recordingStartedAt; }
    [Serializable] private class Recon { public string kind;public bool accepted;public double time;public Vector2[] rawImage;public float[] rawConfidence; }
    private struct Event { public string kind,cue;public int side;public double time; }
    private static readonly StringBuilder report=new StringBuilder();
    private static int checks;
    private static void Check(bool ok,string label) { if(!ok) throw new Exception(label);checks++;report.AppendLine("PASS "+label); }
    public static void RunBatch()
    { try { Run();EditorApplication.Exit(0); } catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); } }
    public static void Run()
    {
        checks=0;report.Clear();Directory.CreateDirectory("Logs/Practice");
        try
        {
            Units();
            Facing(F,14);Facing(D,0);Facing(Oct9,2);
            var f=Legs(F,out var squat);
            // Video: knees at REC 125.4 L, 127.5 L, 129.7 R, 131.6 R (the last two return after the cue changes). 1.22 scored 1.
            var knees=f.FindAll(e=>e.kind=="Knee" && e.time>=120 && e.time<134);
            Check(knees.Count==4 && knees[0].side==0 && knees[1].side==0 && knees[2].side==1 && knees[3].side==1,
                $"1.22 session: all four drive-and-stick knee strikes score, L L R R ({knees.Count})");
            var kicks=f.FindAll(e=>e.kind=="FrontKick");
            Check(kicks.Count==4 && kicks[0].side==0 && kicks[1].side==1 && kicks[2].side==0 && kicks[3].side==1,"1.22 session: front kicks L, R, L, R as performed");
            Check(squat["SIDE KICKS · LEFT / RIGHT"]<.33f,$"Side-kick lean no longer shown as a deep squat ({squat["SIDE KICKS · LEFT / RIGHT"]:F2} m, 1.22: 0.41)");
            Check(squat["SQUAT · HOLD · STAND"]>=.44f,$"Real squat depth kept ({squat["SQUAT · HOLD · STAND"]:F2} m)");
            Check(squat["TORSO FORWARD BENDS"]<.08f,$"Forward bends are not squats ({squat["TORSO FORWARD BENDS"]:F2} m)");
            foreach(var path in new[]{F,D,Oct9,Oct9a}) Legs(path,out _);
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { report.AppendLine($"{checks} checks");File.WriteAllText("Logs/Practice/v123-validation.txt",report.ToString());Debug.Log(report.ToString()); }
    }
    private static void Units()
    {
        var g=new FacingSwapGuard();var image=new Vector2[26];var scores=new float[26];
        for(int i=0;i<26;i++) scores[i]=.9f;
        void Pose(float sign) { image[5]=new Vector2(.5f+.08f*sign,.3f);image[6]=new Vector2(.5f-.08f*sign,.3f);image[11]=new Vector2(.5f+.05f*sign,.55f);image[12]=new Vector2(.5f-.05f*sign,.55f); }
        Pose(1);bool any=false;for(int i=0;i<60;i++) any|=g.Observe(image,scores);
        Check(!any && g.Orientation>.6f,"Facing orientation is learned from normal frames without swapping");
        Pose(-1);Check(g.Observe(image,scores),"Shoulders and hips both reversed: labels swapped back");
        image[11]=new Vector2(.55f,.55f);image[12]=new Vector2(.45f,.55f);
        Check(!g.Observe(image,scores),"Shoulders alone reversed (twist) is not a label swap");
        Pose(-1);image[5].x=image[6].x+.01f;Check(!g.Observe(image,scores),"Edge-on shoulders are ambiguous: no swap");
        var a=new Vector2[26];var s=new float[26];var w=new Vector3[26];a[9]=Vector2.one;w[9]=Vector3.one;s[9]=.3f;
        MediaPipeWorldPose.SwapSides(a,s,w);
        Check(a[10]==Vector2.one && w[10]==Vector3.one && s[10]==.3f && a[9]==Vector2.zero,"Swap moves image, world and score together");
        Check(MediaPipeWorldPose.ShoulderLineReversed(new Vector3(-1,0,.1f),new Vector3(1,0,0)),"Torso basis rejects a 175-degree shoulder flip");
        Check(!MediaPipeWorldPose.ShoulderLineReversed(new Vector3(.5f,0,.8f),new Vector3(1,0,0)),"A 60-degree twist is kept");
        // Straight legs leaning: no flexion squat. Bent knees: flexion squat close to the real drop.
        Vector3 h=new Vector3(0,.9f,0),k=new Vector3(.15f,.47f,0),an=new Vector3(.3f,.05f,0);
        Check(GameBodyMotion.KneeFlexionSquat(h,k,an,h,k,an,-1)<.04f,"Leaning straight legs give no squat");
        Vector3 bk=new Vector3(0,.55f,.3f),ba=new Vector3(0,.1f,0),bh=new Vector3(0,.55f,-.05f);
        float drop=Vector3.Distance(bh,bk)+Vector3.Distance(bk,ba)-Vector3.Distance(bh,ba);
        Check(Mathf.Abs(GameBodyMotion.KneeFlexionSquat(bh,bk,ba,bh,bk,ba,-1)-(drop*1.1f+.03f))<.001f && drop>.3f,"Bent knees give a full squat");
        Check(GameBodyMotion.KneeReturnWindow>=1.2,"Knee strike return window covers drive-and-stick knees");
    }
    private static void Facing(string path,int expected)
    {
        double start=0;var g=new FacingSwapGuard();var image=new Vector2[26];var scores=new float[26];var times=new List<string>();
        foreach(var line in File.ReadLines(path))
        {
            if(line.StartsWith("{\"kind\":\"session\"")) { start=JsonUtility.FromJson<Cue>(line).recordingStartedAt;continue; }
            if(!line.StartsWith("{\"kind\":\"reconstruction\"")) continue;
            var r=JsonUtility.FromJson<Recon>(line);
            if(r.rawImage==null || r.rawImage.Length<33 || r.rawConfidence==null) continue;
            for(int i=0;i<26;i++) { int j=MediaPipeWorldPose.HalpeToMediaPipe[i];image[i]=r.rawImage[j];scores[i]=r.rawConfidence[j]; }
            if(g.Observe(image,scores)) times.Add((r.time-start).ToString("F2"));
        }
        report.AppendLine($"FACING {Path.GetFileName(path)}: swaps {times.Count} [{string.Join(", ",times)}]");
        Check(times.Count==expected,$"{Path.GetFileName(path)}: facing guard swaps exactly the {expected} label-swapped frames");
    }
    private static List<Event> Legs(string path,out Dictionary<string,float> squat)
    {
        var samples=SmoothGameBodyValidation.LoadExact(false,path);
        double start=0;var cues=new List<(double t,string label)>();
        foreach(var line in File.ReadLines(path))
        {
            if(line.StartsWith("{\"kind\":\"session\"")) start=JsonUtility.FromJson<Cue>(line).recordingStartedAt;
            else if(line.StartsWith("{\"kind\":\"guide_cue\"")) { var c=JsonUtility.FromJson<Cue>(line);cues.Add((c.recordedAt-start,c.label)); }
        }
        string CueAt(double t) { string label="";foreach(var c in cues) if(c.t<=t) label=c.label;return label; }
        var m=new GameBodyMotion();var events=new List<Event>();int index=0;
        m.Decision+=(kind,side,action,captured,now)=>events.Add(new Event{kind=kind,side=side,time=now-start,cue=CueAt(now-start)});
        squat=new Dictionary<string,float>();
        for(double t=0;t<166;t+=1/60.0)
        {
            while(index<samples.Count && samples[index].elapsed<=t) m.Observe(samples[index++].body,start+t);
            m.Tick(start+t);
            string cue=CueAt(t);squat[cue]=Mathf.Max(squat.TryGetValue(cue,out var v)?v:0,m.Squat);
        }
        var strikes=events.FindAll(e=>e.kind!="prepare" && e.kind!="cancel");
        foreach(var e in strikes) report.AppendLine($"LEGS {Path.GetFileName(path)} {e.time:F2} {(e.side==0?"L":"R")} {e.kind} · {e.cue}");
        Check(!strikes.Exists(e=>e.time<120 || e.time>=156 || e.cue.Contains("KNEE LIFT")),
            $"{Path.GetFileName(path)}: idle, body, arm, knee-hold and recovery cues stay non-scoring ({strikes.Count} leg strikes)");
        return events;
    }
}
