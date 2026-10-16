using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;

// App 1.25: overhead arms from 3-D + camera image, two arms rising together never punch, straight overhead render.
// Replays every recorded session's raw landmarks through OverheadArms and checks where it activates.
public static class V125Validation
{
    private static readonly string[] Sessions={"20261012-H","20261011-G","20261011-F","20261010-D","20261010-A","20261010-B","20261010-C","20261010-E","20261009-192549-873"};
    [Serializable] private class Cue { public string kind,label;public double recordedAt,recordingStartedAt; }
    [Serializable] private class Recon
    { public string kind;public bool accepted;public double time;public int inputWidth,inputHeight;public Vector2[] rawImage;public Vector3[] rawWorld;public float[] rawConfidence; }
    private static readonly StringBuilder report=new StringBuilder();
    private static int checks;
    private static void Check(bool ok,string label) { if(!ok) throw new Exception(label);checks++;report.AppendLine("PASS "+label); }
    public static void RunBatch()
    { try { Run();EditorApplication.Exit(0); } catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); } }
    public static void Run()
    {
        checks=0;report.Clear();Directory.CreateDirectory("Logs/Practice");
        try { Units();foreach(var s in Sessions) Replay(s); }
        catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { report.AppendLine($"{checks} checks");File.WriteAllText("Logs/Practice/v125-validation.txt",report.ToString());Debug.Log(report.ToString()); }
    }
    private static Vector3 Up(Vector3 s,float deg,float len) => s+new Vector3(0,Mathf.Sin(deg*Mathf.Deg2Rad),Mathf.Cos(deg*Mathf.Deg2Rad))*len;
    private static OverheadArms.ImageArms Image(float rise,float length)
    {
        // Torso = 1 (shoulders at y 1, hips at y 0); wrists straight above/forward of the shoulders.
        float side=Mathf.Sqrt(Mathf.Max(0,length*length-rise*rise));
        return new OverheadArms.ImageArms{ls=new Vector2(-.2f,1),rs=new Vector2(.2f,1),lh=new Vector2(-.15f,0),rh=new Vector2(.15f,0),
            lw=new Vector2(-.2f-side,1+rise),rw=new Vector2(.2f+side,1+rise)};
    }
    private static void Feed(OverheadArms d,float deg,OverheadArms.ImageArms image,int frames=1)
    {
        Vector3 ls=new Vector3(-.18f,1.4f,0),rs=new Vector3(.18f,1.4f,0);
        for(int i=0;i<frames;i++) d.Observe(true,ls,Up(ls,deg,.28f),Up(ls,deg,.56f),rs,Up(rs,deg,.28f),Up(rs,deg,.56f),image);
    }
    private static void Units()
    {
        var d=new OverheadArms();
        Feed(d,60,Image(.95f,.98f),2);
        Check(d.Active,"Raised arms under-read at 60 degrees in 3-D are overhead when the image shows them raised and extended");
        d.Reset();Feed(d,60,Image(.88f,.88f),3);
        Check(!d.Active,"Arms forward (image foreshortened to 0.88) are not overhead");
        d.Reset();Feed(d,45,Image(.95f,.98f),3);
        Check(!d.Active,"Below 48 degrees in 3-D the image cannot make arms overhead");
        d.Reset();Feed(d,60,Image(.95f,.98f),2);Feed(d,50,Image(.7f,.9f));
        Check(d.Active,"Hysteresis: lowering a little keeps arms overhead");
        Feed(d,50,Image(.5f,.9f));
        Check(!d.Active,"Wrists below 0.6 torso above the shoulders end overhead");
        d.Reset();Feed(d,82,default,2);
        Check(d.Active,"3-D only (no image) still recognises clearly raised arms");
        Check(AssistedPunchController.RisingTogether(12.2f,9.3f),"Session H 53.05 s: both wrists rising (12.2 / 9.3 torso/s) is a two-arm gesture");
        Check(!AssistedPunchController.RisingTogether(8f,-4f),"Other hand returning from a punch (moving down) is not a gesture");
        Check(!AssistedPunchController.RisingTogether(8f,1f),"Other hand quiet in guard is not a gesture");
        float arm=.55f;var r=AssistedPunchController.OverheadReach(new Vector3(.1f,.25f,.35f),arm);
        float elev=Mathf.Atan2(r.y,new Vector2(r.x,r.z).magnitude)*Mathf.Rad2Deg;
        Check(elev>=79.9f && Mathf.Abs(r.magnitude-arm*.985f)<1e-3f,$"Overhead arm is drawn straight and at least 80 degrees up ({elev:F1} degrees)");
    }
    private static void Replay(string session)
    {
        string path="Logs/MediaPipe/"+session+"/input/"+session+".jsonl";
        if(!File.Exists(path)) { report.AppendLine("SKIP missing "+path);return; }
        double start=0;string cue="setup",startCue="setup";var d=new OverheadArms();bool was=false;double since=0;
        var spans=new List<(double a,double b,string cue)>();var image=new OverheadArms.ImageArms();
        foreach(var line in File.ReadLines(path))
        {
            if(line.StartsWith("{\"kind\":\"session\"")) { start=JsonUtility.FromJson<Cue>(line).recordingStartedAt;continue; }
            if(line.StartsWith("{\"kind\":\"guide_cue\"")) { cue=JsonUtility.FromJson<Cue>(line).label;continue; }
            if(!line.StartsWith("{\"kind\":\"reconstruction\"")) continue;
            var r=JsonUtility.FromJson<Recon>(line);
            if(!r.accepted || r.rawWorld==null || r.rawWorld.Length<33 || r.rawImage==null || r.rawImage.Length<33 || r.rawConfidence==null) continue;
            float aspect=r.inputWidth>0 && r.inputHeight>0 ? r.inputWidth/(float)r.inputHeight : .75f;
            Vector2 I(int i) => new Vector2(r.rawImage[i].x*aspect,1-r.rawImage[i].y);
            bool valid=true;for(int i=11;i<=16;i++) valid&=r.rawConfidence[i]>=.5f;
            image.ls=I(11);image.rs=I(12);image.lw=I(15);image.rw=I(16);image.lh=I(23);image.rh=I(24);
            d.Observe(valid,r.rawWorld[11],r.rawWorld[13],r.rawWorld[15],r.rawWorld[12],r.rawWorld[14],r.rawWorld[16],image);
            double t=r.time-start;
            if(d.Active && !was) { since=t;startCue=cue; }
            if(!d.Active && was) spans.Add((since,t,startCue));
            was=d.Active;
        }
        double overheadSeconds=0;
        foreach(var s in spans)
        {
            report.AppendLine($"OVERHEAD {session} {s.a:F2}-{s.b:F2} ({s.b-s.a:F2} s) during {s.cue}");
            bool allowed=s.cue.Contains("OVERHEAD") || s.cue.Contains("FORWARD") || s.cue.Contains("RAISE") || s.cue.Contains("TO SIDES");
            Check(allowed,$"{session}: overhead arms only during two-arm cues ({s.a:F2} s, {s.cue})");
            if(s.cue.Contains("OVERHEAD") || s.cue.Contains("RAISE")) overheadSeconds+=s.b-s.a;
        }
        if(session=="20261012-H") Check(overheadSeconds>=2.5,$"Session H: both overhead raises recognised ({overheadSeconds:F2} s; 1.24 drew punches)");
        if(session=="20261011-G" || session=="20261010-D") Check(overheadSeconds>=2,$"{session}: overhead raises still recognised ({overheadSeconds:F2} s)");
    }
}
