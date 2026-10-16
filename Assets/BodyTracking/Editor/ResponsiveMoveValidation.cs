using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using FitFight.BodyTracking;
using FitFight.Practice;

public static class ResponsiveMoveValidation
{
    [Serializable] class Row { public string kind; public double recordedRelative; public PunchCameraInput.Frame observation; public Meta metadata; }
    [Serializable] class Meta { public string tuning; public float userHeight; }
    public static void RunBatch() { try { Run(); EditorApplication.Exit(0); } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } }
    public static void Run()
    {
        Directory.CreateDirectory("Logs/Practice"); var report=new StringBuilder();
        void Check(bool ok,string message) {report.AppendLine((ok?"PASS ":"FAIL ")+message);File.WriteAllText("Logs/Practice/responsive-moves.txt",report.ToString());if(!ok)throw new Exception(message);}
        List<Row> Load(string name)=>File.ReadLines("Docs/PunchSessions/20260930-standing-fix-"+name+"-regression.jsonl").Select(l=>JsonUtility.FromJson<Row>(l)).ToList();
        var rows=Load("punches");var gate=new PunchGestureGate();var detectors=new[]{new PunchIntent(),new PunchIntent()};
        var baselineGate=new BaselinePunchGestureGate();var baselineDetectors=new[]{new BaselinePunchIntent(),new BaselinePunchIntent()};
        var baseline=new List<(long seq,int side,double at)>();
        var commits=new List<(long seq,int side,double at)>();var csv=new StringBuilder("sequence,relative,side,signal,reason\n");
        foreach(var r in rows.Where(r=>r.kind=="observation"))
        {
            var f=r.observation;gate.Observe(f);baselineGate.Observe(f);
            for(int side=0;side<2;side++)
            {
                if(baselineDetectors[side].Observe(side==0?f.left:f.right,f.sequence,f.time,baselineGate.Blocked?baselineGate.Reason:null)==BaselinePunchIntent.Signal.Commit)baseline.Add((f.sequence,side,r.recordedRelative));
                var signal=detectors[side].Observe(side==0?f.left:f.right,f.sequence,f.time,gate.Blocked?gate.Reason:null);
                if(signal!=PunchIntent.Signal.None)csv.AppendLine($"{f.sequence},{r.recordedRelative:F3},{side},{signal},{detectors[side].Reason}");
                if(signal==PunchIntent.Signal.Commit)commits.Add((f.sequence,side,r.recordedRelative));
            }
        }
        File.WriteAllText("Logs/Practice/responsive-punch-events.csv",csv.ToString());
        Check(baseline.Count==25,"Frozen prior detector reproduces the phone's 25 commitments");
        var advances=new List<double>();
        foreach(var old in baseline)
        {
            var matching=commits.Where(c=>c.side==old.side&&Math.Abs(c.at-old.at)<.3).OrderBy(c=>Math.Abs(c.at-old.at)).ToList();
            Check(matching.Count==1,$"Prior recognized stroke preserved: side {old.side}, sequence {old.seq}");
            advances.Add((old.at-matching[0].at)*1000);
        }
        advances.Sort();report.AppendLine($"REPLAY recognition advance median {advances[advances.Count/2]:F1} ms, maximum {advances.Max():F1} ms. Observation-clock comparison, not physical camera latency.");
        Check(commits.Count>25,$"New phone replay recognizes more than the original 25 commitments ({commits.Count})");
        foreach(var window in new[]{(2781L,2783L,1),(2794L,2797L,1),(2868L,2871L,0)})
            Check(commits.Count(c=>c.side==window.Item3&&c.seq>=window.Item1&&c.seq<=window.Item2)==1,$"Reviewed missed outgoing stroke commits once: side {window.Item3}, sequences {window.Item1}–{window.Item2}");
        Check(!commits.Any(c=>c.at>=64&&c.at<70),"Reviewed paired raise/lower phase has no commits");
        // The 50.8-second preparation is not labelled as a missed punch: video shows lowering arms.
        Check(!commits.Any(c=>c.seq>=2828&&c.seq<=2838),"Cancelled preparation during arm lowering stays cancelled");
        Check(!commits.Any(c=>c.seq>=2933&&c.seq<=2937),"Recorded withdrawn half-extension remains a cancelled preparation");
        var body=Load("body");gate.Reset();foreach(var d in detectors)d.Reset();int phantom=0;
        foreach(var r in body.Where(r=>r.kind=="observation"&&r.recordedRelative>=26&&r.recordedRelative<50))
        {var f=r.observation;gate.Observe(f);for(int side=0;side<2;side++)if(detectors[side].Observe(side==0?f.left:f.right,f.sequence,f.time,gate.Blocked?gate.Reason:null)==PunchIntent.Signal.Commit){phantom++;report.AppendLine($"BODY COMMIT {r.recordedRelative:F3} {f.sequence} {side}");}}
        Check(phantom==0,"New phone overhead and lateral-arm movements do not trigger assisted punches");
        var solver=new Skeleton25DSolver{RequireStandingCalibration=true};var result=new Vector3[26];var confidence=new float[26];
        var tuning=JsonUtility.FromJson<RTMPoseTuning>(body[0].metadata.tuning);tuning.prediction=0;tuning.observationInterval=.1f;
        var overhead=new List<float>();var standing=new List<float>();
        // New session gravity changed pitch to 22.393° when standing acquisition began.
        // Accepted observations omit preceding calibration samples; this is a fresh calibration
        // on the recorded standing segment, not a claim of byte-identical phone state.
        foreach(var r in body.Where(r=>r.kind=="observation"&&r.recordedRelative>=10))
        {
            var f=r.observation;var points=f.imageJoints.Select(p=>new Vector2(p.x/f.imageAspect,1-p.y)).ToArray();
            if(!solver.Solve(points,f.jointScores,f.imageAspect,f.time,body[0].metadata.userHeight,22.39323f,0,result,confidence,tuning)||!solver.Calibrated)continue;
            if(r.recordedRelative>=15&&r.recordedRelative<16)for(int side=0;side<2;side++)standing.Add(Vector3.Angle(result[13+side]-result[11+side],Vector3.down));
            if(r.recordedRelative>=30&&r.recordedRelative<32)for(int side=0;side<2;side++)overhead.Add(Vector3.Angle(result[7+side]-result[5+side],Vector3.up));
        }
        overhead.Sort();standing.Sort();
        Check(overhead.Count>=20&&overhead[overhead.Count/2]<30,$"Recorded overhead upper arms point up: median {overhead.DefaultIfEmpty(180).ElementAt(overhead.Count/2):F1}° from up");
        Check(standing.Count>=10&&standing[standing.Count/2]<12,$"New phone standing legs remain near vertical ({standing.Count} samples, median {standing.DefaultIfEmpty(180).ElementAt(standing.Count/2):F1}°)");
        foreach(float speed in new[]{1f,3f,5f})Check(AssistedPunchController.StrikeDuration(speed)*(.4f-.12f)<=.18f,$"Punch contact phase reached within 180 ms of commitment at speed {speed}");
        Debug.Log(report.ToString());
    }
}
