using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

public static class MediaPipeFilterReplayValidation
{
    private const string Input="Logs/MediaPipe/20261002-110117/input/20261002-110117-737.jsonl";
    [Serializable] private struct Header {public string kind,tuning,label;public float userHeight,cameraAspect;public double recordedAt,recordingEndedAt;}
    [Serializable] public sealed class Mode
    {
        public string name;
        public float originalReplayMaxErrorCm;
        public MediaPipeTestStats.Movement[] movements;
        public TrackingError[] trackingErrors;
    }
    [Serializable] public sealed class TrackingError {public string label;public float meanWristDeviationCm;public int samples;}
    [Serializable] private sealed class Comparison {public string source;public Mode[] modes;}
    public static Mode Replay(bool region, bool adaptive = false)
    {
        var rows=File.ReadAllLines(Input);var header=JsonUtility.FromJson<Header>(rows[0]);
        var tuning=new RTMPoseTuning();JsonUtility.FromJsonOverwrite(header.tuning,tuning);tuning.mediaPipeRegionFiltering=region;
        tuning.mediaPipeMotionAwareFiltering=adaptive;
        float aspect=JsonUtility.FromJson<Header>(rows[rows.Length-2]).cameraAspect;
        var world=new MediaPipeWorldPose();var raw=new MediaPipeWorldPose();var stats=new MediaPipeTestStats();
        var rawT=new RTMPoseTuning();JsonUtility.FromJsonOverwrite(header.tuning,rawT);rawT.filtering=false;
        var rawOut=new Vector3[26];var rawUsable=new float[26];
        var errors=new Dictionary<string,TrackingError>();
        var image=new Vector2[26];var source=new Vector3[26];var scores=new float[26];var output=new Vector3[26];var usable=new float[26];
        double lastCapture=double.NaN;float error=0;string previousLabel=null;double ended=0;
        foreach(var line in rows)
        {
            var h=JsonUtility.FromJson<Header>(line);var kind=h.kind;
            if(kind=="guide_cue") {stats.StartMovement(h.label,h.recordedAt);previousLabel=h.label;continue;}
            if(kind=="mediapipe_summary" && h.recordingEndedAt>0) ended=h.recordingEndedAt;
            if(kind!="reconstruction") continue;
            var d=JsonUtility.FromJson<RTMPosePoseProvider.Diagnostics>(line);ended=d.receivedAt;
            if(!double.IsNaN(lastCapture)) tuning.observationInterval=Mathf.Clamp((float)(d.capturedAt-lastCapture),1f/120,.5f);
            lastCapture=d.capturedAt;
            if(d.resultReason=="discarded" || d.resultReason=="calibration_wait") continue;
            if(!d.detected) {world.MarkMissing(d.capturedAt,tuning,output,usable);continue;}
            MediaPipeWorldPose.Map(new PoseInferenceResult{Image=d.rawImage,World=d.rawWorld,Confidence=d.rawConfidence},image,scores,source);
            if(d.resultReason=="stale") continue;
            bool ready=world.ObserveCalibration(image,source,scores,d.capturedAt,header.userHeight,tuning);
            bool rawReady=raw.ObserveCalibration(image,source,scores,d.capturedAt,header.userHeight,tuning);
            if(ready!=rawReady) throw new Exception("Raw reference calibration mismatch");
            if(!ready) continue;
            raw.Step(source,scores,d.capturedAt,header.userHeight,rawT,rawOut,rawUsable,image,aspect);
            world.Step(source,scores,d.capturedAt,header.userHeight,tuning,output,usable,image,aspect);
            if(d.accepted)
            {
                for(int i=0;i<26;i++) error=Mathf.Max(error,Vector3.Distance(output[i],d.joints[i])*100);
                if(previousLabel==null) continue;
                if(!errors.TryGetValue(d.movementLabel,out var e)) {e=new TrackingError{label=d.movementLabel};errors.Add(d.movementLabel,e);}
                e.samples++;e.meanWristDeviationCm+=(Vector3.Distance(output[9],rawOut[9])+Vector3.Distance(output[10],rawOut[10]))*50;
                d.joints=(Vector3[])output.Clone();d.confidence=(float[])usable.Clone();
                stats.Add(d);
            }
        }
        foreach(var e in errors.Values) if(e.samples>0) e.meanWristDeviationCm/=e.samples;
        var tracking=new List<TrackingError>(errors.Values).ToArray();
        return new Mode{trackingErrors=tracking,name=region ? (adaptive ? "region-motion-aware" : "region") : "legacy",originalReplayMaxErrorCm=error,movements=stats.Finish(ended)};
    }
    public static void RunBatch()
    {
        try
        {
            var legacy=Replay(false);var region=Replay(true);var revised=Replay(true,true);
            if(region.originalReplayMaxErrorCm>.001f) throw new Exception("Replay does not reproduce phone filtering: "+region.originalReplayMaxErrorCm+" cm");
            string dir="Logs/MediaPipe/20261002-110117";Directory.CreateDirectory(dir);
            File.WriteAllText(dir+"/same-input-replay.json",JsonUtility.ToJson(new Comparison{source=Input,modes=new[]{legacy,region,revised}},true));
            var report=new StringBuilder("Same phone inputs; no detector retraining.\n");
            report.AppendLine("Original replay max error cm: "+region.originalReplayMaxErrorCm);
            for(int i=0;i<region.movements.Length;i++)
            {
                var a=legacy.movements[i];var b=region.movements[i];
                if(a.stillnessSamples>0)
                {
                    var n=revised.movements[i];
                    if(n.filteredStillnessRmsCm>b.filteredStillnessRmsCm*.98f) throw new Exception("Revised rest preset did not improve "+a.label);
                    report.AppendLine($"{a.label}: legacy RMS {a.filteredStillnessRmsCm:F3} cm, region RMS {b.filteredStillnessRmsCm:F3} cm, revised RMS {n.filteredStillnessRmsCm:F3} cm, raw {b.rawStillnessRmsCm:F3} cm");
                }
            }
            for(int i=0;i<legacy.trackingErrors.Length;i++)
            {
                var e=legacy.trackingErrors[i];var n=revised.trackingErrors[i];
                if(e.label=="punch_fast" || e.label.Contains("elbow"))
                {
                    if(e.label=="punch_fast" && n.meanWristDeviationCm>=e.meanWristDeviationCm) throw new Exception("Fast punch response did not improve versus legacy");
                    report.AppendLine($"{e.label}: legacy wrist deviation {e.meanWristDeviationCm:F3} cm, revised {n.meanWristDeviationCm:F3} cm (reference is model output, not human accuracy)");
                }
            }
            File.WriteAllText(dir+"/same-input-replay.txt",report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(0);
        }
        catch(Exception e) {Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
