using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;

// App 1.24 hybrid kicks: a chambered leg's observed unfolding drives the avatar's kick path before confirmation, and a
// confirmed kick continues from the shown/observed extension. Replays check holds never animate as kicks.
public static class V124Validation
{
    private static readonly string[] Sessions={
        "Logs/MediaPipe/20261011-G/input/20261011-G.jsonl","Logs/MediaPipe/20261011-F/input/20261011-F.jsonl",
        "Logs/MediaPipe/20261010-D/input/20261010-D.jsonl","Logs/MediaPipe/20261009-192549-873/input/20261009-192549-873.jsonl"};
    [Serializable] private class Cue { public string kind,label;public double recordedAt,recordingStartedAt; }
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
            Check(GameBodyMotion.ExtensionFromBends(60,10)==0,"No anticipation without a real chamber (knee under 70 degrees)");
            Check(Mathf.Abs(GameBodyMotion.ExtensionFromBends(131,49)-.707f)<.01f,"Snap kick 131 -> 49 degrees reads as 71% extended");
            Check(GameBodyMotion.ExtensionFromBends(120,120)==0,"Held chamber shows no extension");
            Check(Mathf.Abs(GameBodyMotion.ProgressToPhase(1)-(.14f+.22f*.7f))<1e-4f,"Phase skip capped so 0.15 s of contact window remains");
            Check((.62f-GameBodyMotion.ProgressToPhase(1))*GameBodyMotion.StrikeSeconds>=.149f,"Remaining contact window after the cap >= 0.15 s");
            foreach(var s in Sessions) Replay(s);
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { report.AppendLine($"{checks} checks");File.WriteAllText("Logs/Practice/v124-validation.txt",report.ToString());Debug.Log(report.ToString()); }
    }
    private static void Replay(string path)
    {
        var samples=SmoothGameBodyValidation.LoadExact(false,path);
        double start=0;var cues=new List<(double t,string label)>();
        foreach(var line in File.ReadLines(path))
        {
            if(line.StartsWith("{\"kind\":\"session\"")) start=JsonUtility.FromJson<Cue>(line).recordingStartedAt;
            else if(line.StartsWith("{\"kind\":\"guide_cue\"")) { var c=JsonUtility.FromJson<Cue>(line);cues.Add((c.recordedAt-start,c.label)); }
        }
        string CueAt(double t) { string label="";foreach(var c in cues) if(c.t<=t) label=c.label;return label; }
        var m=new GameBodyMotion();int index=0;float holdMax=0,idleMax=0;var commits=new List<string>();int skipped=0,footKicks=0;
        m.Decision+=(kind,side,action,captured,now)=>
        {
            if(kind=="prepare" || kind=="cancel" || kind=="Knee") return;
            footKicks++;if(m.CommitProgress>.05f) skipped++;
            commits.Add($"{now-start:F2} {(side==0?"L":"R")} {kind} progress {m.CommitProgress:F2}");
        };
        for(double t=0;t<166;t+=1/60.0)
        {
            while(index<samples.Count && samples[index].elapsed<=t) m.Observe(samples[index++].body,start+t);
            m.Tick(start+t);
            string cue=CueAt(t);
            if(cue.Contains("KNEE LIFT")) holdMax=Mathf.Max(holdMax,m.ExtensionProgress);
            if(t<108 || t>=156) idleMax=Mathf.Max(idleMax,m.ExtensionProgress);
        }
        string name=Path.GetFileName(path);
        foreach(var c in commits) report.AppendLine($"KICK {name} {c}");
        Check(holdMax<.05f,$"{name}: knee-lift holds never animate as kicks (max progress {holdMax:F2})");
        Check(idleMax<.05f,$"{name}: idle, body and recovery cues never animate kicks (max progress {idleMax:F2})");
        // G: front kicks with an observed high chamber continue from the extension (135.9, 137.8, 142.2). 127.45 chambers
        // and extends in one frame; 149.9's high-chamber frame was rejected as a tracking jump, so it rises through the
        // chamber first (skipping a low chamber would hit the bag's lower edge); the side kick has no deep chamber.
        if(name.Contains("-G")) Check(skipped>=3 && footKicks>=6,$"{name}: front kicks continue from the extension ({skipped}/{footKicks})");
    }
}
