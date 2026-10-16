using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;

// Regression for the REC 35.70 shared-gate block in 20261009-192549-873.
// App 1.20 investigation: requiring both hands above the guard band before a
// lowering could start the block removed this block, but replay through the real
// BoxingPunchIntent moved the following left commit from 35.886 to 35.937 (later).
// The brief block re-seats the dipped guard origin, which helps the next stroke.
// The 1.19 gate is therefore kept; this test locks in the observed timing.
public static class GateLoweringValidation
{
    [Serializable] private class Record
    {
        public string kind,label;
        public double recordedAt,recordingStartedAt;
        public PunchCameraInput.Frame observation;
    }
    private static readonly StringBuilder report=new StringBuilder();
    private static void Check(bool ok,string message)
    { if(!ok)throw new Exception(message);report.AppendLine("PASS "+message); }
    public static void RunBatch()
    {
        try { Run();EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
    public static void Run()
    {
        report.Clear();Directory.CreateDirectory("Logs/Practice");
        try
        {
            const string session="20261009-192549-873";
            var gate=new BoxingGestureGate();var intent=new[]{new BoxingPunchIntent(),new BoxingPunchIntent()};
            var commits=new List<(int side,double time,string cue)>();string cue="setup";double start=0;bool blocked=false;
            foreach(string line in File.ReadLines("Logs/MediaPipe/"+session+"/input/"+session+".jsonl"))
            {
                var r=JsonUtility.FromJson<Record>(line);
                if(r.kind=="session")start=r.recordingStartedAt;
                if(r.kind=="guide_cue")cue=r.label;
                if(r.kind!="observation")continue;
                var f=r.observation;gate.Observe(f);double t=f.time-start;
                if(t>35.65 && t<35.80 && gate.Blocked)blocked=true;
                for(int side=0;side<2;side++)
                    if(intent[side].Observe(side==0?f.left:f.right,f.sequence,f.time,gate.Blocked)==PunchIntent.Signal.Commit)
                        commits.Add((side,t,cue));
            }
            report.AppendLine($"REPLAY {session}: {commits.Count} commits");
            Check(blocked,"Recorded REC 35.70 shared block is reproduced");
            Check(commits.Exists(c=>c.side==0 && Math.Abs(c.time-35.886)<.01),"Left commit after the block stays at 35.886 or earlier");
            Check(commits.FindAll(c=>c.cue.Contains("PUNCHES")).Count==13,"All 13 main-block punch commits are retained");
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { File.WriteAllText("Logs/Practice/gate-lowering-validation.txt",report.ToString());Debug.Log(report.ToString()); }
    }
}
