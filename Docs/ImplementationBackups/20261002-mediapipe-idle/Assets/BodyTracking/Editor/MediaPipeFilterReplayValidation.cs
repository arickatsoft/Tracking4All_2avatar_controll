using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

public static class MediaPipeFilterReplayValidation
{
    private const string Input="/Users/jotit/Desktop/20261002-110117-737.jsonl";
    [Serializable] private struct Header {public string kind;public string tuning;public float userHeight;}
    [Serializable] public sealed class Mode
    {
        public string name;
        public float originalReplayMaxErrorCm;
        public MediaPipeTestStats.Movement[] movements;
    }
    [Serializable] private sealed class Comparison {public string source;public Mode[] modes;}
    public static Mode Replay(bool region)
    {
        var rows=File.ReadAllLines(Input);var header=JsonUtility.FromJson<Header>(rows[0]);
        var tuning=new RTMPoseTuning();JsonUtility.FromJsonOverwrite(header.tuning,tuning);tuning.mediaPipeRegionFiltering=region;
        var world=new MediaPipeWorldPose();var stats=new MediaPipeTestStats();
        var image=new Vector2[26];var source=new Vector3[26];var scores=new float[26];var output=new Vector3[26];var usable=new float[26];
        double lastCapture=double.NaN;float error=0;string previousLabel=null;double ended=0;
        foreach(var line in rows)
        {
            var kind=JsonUtility.FromJson<Header>(line).kind;
            if(kind!="reconstruction") continue;
            var d=JsonUtility.FromJson<RTMPosePoseProvider.Diagnostics>(line);ended=d.receivedAt;
            if(!double.IsNaN(lastCapture)) tuning.observationInterval=Mathf.Clamp((float)(d.capturedAt-lastCapture),1f/120,.5f);
            lastCapture=d.capturedAt;
            if(d.resultReason=="discarded" || d.resultReason=="calibration_wait") continue;
            if(!d.detected) {world.MarkMissing(d.capturedAt,tuning,output,usable);continue;}
            MediaPipeWorldPose.Map(new PoseInferenceResult{Image=d.rawImage,World=d.rawWorld,Confidence=d.rawConfidence},image,scores,source);
            if(d.resultReason=="stale") continue;
            if(!world.ObserveCalibration(image,source,scores,d.capturedAt,header.userHeight,tuning)) continue;
            world.Step(source,scores,d.capturedAt,header.userHeight,tuning,output,usable);
            if(d.accepted)
            {
                for(int i=0;i<26;i++) error=Mathf.Max(error,Vector3.Distance(output[i],d.joints[i])*100);
                if(d.movementLabel!=previousLabel)
                { stats.StartMovement(d.movementLabel,d.receivedAt);previousLabel=d.movementLabel; }
                d.joints=(Vector3[])output.Clone();d.confidence=(float[])usable.Clone();
                stats.Add(d);
            }
        }
        return new Mode{name=region ? "region" : "legacy",originalReplayMaxErrorCm=error,movements=stats.Finish(ended)};
    }
    public static void RunBatch()
    {
        try
        {
            var legacy=Replay(false);var region=Replay(true);
            if(region.originalReplayMaxErrorCm>.001f) throw new Exception("Replay does not reproduce phone filtering: "+region.originalReplayMaxErrorCm+" cm");
            string dir="Logs/MediaPipe/20261002-110117";Directory.CreateDirectory(dir);
            File.WriteAllText(dir+"/same-input-replay.json",JsonUtility.ToJson(new Comparison{source=Input,modes=new[]{legacy,region}},true));
            var report=new StringBuilder("Same phone inputs; no detector retraining.\n");
            report.AppendLine("Original replay max error cm: "+region.originalReplayMaxErrorCm);
            for(int i=0;i<region.movements.Length;i++)
            {
                var a=legacy.movements[i];var b=region.movements[i];
                if(a.stillnessSamples>0) report.AppendLine($"{a.label}: legacy RMS {a.filteredStillnessRmsCm:F3} cm, region RMS {b.filteredStillnessRmsCm:F3} cm, raw {b.rawStillnessRmsCm:F3} cm");
            }
            File.WriteAllText(dir+"/same-input-replay.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        catch(Exception e) {Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
