using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

public static class MediaPipeImprovementValidation
{
    private static readonly StringBuilder report=new StringBuilder();
    private static void Check(bool value,string label) { if(!value) throw new Exception(label);report.AppendLine("PASS "+label); }
    private static void Fixture(out Vector2[] image,out Vector3[] source,out float[] scores)
    {
        image=new Vector2[26];source=new Vector3[26];scores=new float[26];
        for(int i=0;i<26;i++) {image[i]=new Vector2(.5f,.4f);scores[i]=1;}
        source[0]=new Vector3(0,.8f,0);
        for(int side=0;side<2;side++)
        {
            float sign=side==0 ? 1 : -1;
            source[5+side]=new Vector3(sign*.2f,.5f,0);source[11+side]=new Vector3(sign*.1f,0,0);
            source[13+side]=new Vector3(sign*.1f,-.4f,0);source[15+side]=new Vector3(sign*.1f,-.8f,0);
            source[7+side]=new Vector3(sign*.35f,.3f,0);source[9+side]=new Vector3(sign*.4f,.1f,-.1f);
            image[5+side]=new Vector2(.5f+sign*.12f,.3f);image[11+side]=new Vector2(.5f+sign*.06f,.5f);
            image[13+side]=new Vector2(.5f+sign*.06f,.7f);image[15+side]=new Vector2(.5f+sign*.06f,.9f);
        }
        image[0]=new Vector2(.5f,.12f);source[18]=(source[5]+source[6])*.5f;source[19]=Vector3.zero;
    }
    public static void Run()
    {
        report.Clear();
        try
        {
            Fixture(out var image,out var source,out var scores);
            var tuning=new RTMPoseTuning{filtering=false,prediction=0};var world=new MediaPipeWorldPose();
            Check(!world.ObserveCalibration(image,source,scores,1,1.7f,tuning),"First frame cannot establish stillness");
            world.ObserveCalibration(image,source,scores,1.04,1.7f,tuning);
            world.ObserveCalibration(image,source,scores,1.04,1.7f,tuning);
            Check(world.CalibrationFrames==1,"Duplicate calibration timestamp cannot add a sample");
            for(int i=2;i<=20;i++) world.ObserveCalibration(image,source,scores,1+i*.04,1.7f,tuning);
            Check(world.Calibrated && world.CalibrationFrames==20,"Independent MediaPipe standing calibration accepts 20 still 3D frames");
            var output=new Vector3[26];var usable=new float[26];world.Step(source,scores,2,1.7f,tuning,output,usable);
            source[5].y=source[6].y=.01f;source[18]=(source[5]+source[6])*.5f; // severe 2D/torso foreshortening
            scores[5]=0;source[9].z=-.4f;
            Check(world.ObserveCalibration(image,source,scores,2.04,1.7f,tuning),"Calibrated limbs are independent of torso projection/visibility");
            world.Step(source,scores,2.04,1.7f,tuning,output,usable);
            Check(world.Observed(9) && !world.Observed(5) && output[9].z<-.35f,"Observed wrist depth survives a missing shoulder without claiming torso observation");
            Check(world.CalibrationFrames==20,"Movement never alters standing reference");
            source[9]=new Vector3(float.NaN,0,0);world.Step(source,scores,2.08,1.7f,tuning,output,usable);
            Check(!world.Observed(9) && RTMPoseMath.Finite(output[9]),"Nonfinite model joint holds finite history and loses observation");
            world.MarkMissing(2.12,tuning,output,usable);
            Check(!world.Observed(13),"No-person result clears observed limbs immediately");
            world.Predict(3,.2f,tuning,output,usable);
            Check(usable[13]==0,"Prolonged occlusion expires usable confidence");
            world.Reset();Fixture(out image,out source,out scores);
            world.ObserveCalibration(image,source,scores,4,1.7f,tuning);
            world.ObserveCalibration(image,source,scores,4.04,1.7f,tuning);scores[15]=0;
            world.ObserveCalibration(image,source,scores,4.08,1.7f,tuning);
            Check(!world.Calibrated && world.CalibrationFrames==0,"Missing foot cannot calibrate a false neutral");
            var wrist=MediaPipeWorldPose.RegionPreset(9);var torso=MediaPipeWorldPose.RegionPreset(5);
            Check(wrist.noise<torso.noise && wrist.cutoff>torso.cutoff,"Region preset reduces wrist dead zone and increases responsiveness");
            Check(MediaPipeTestStats.Percentile(new List<float>(),.95f)==0,"Empty percentile is unavailable zero");
            var samples=new List<float>();for(int i=1;i<=20;i++)samples.Add(i);
            Check(MediaPipeTestStats.Percentile(samples,.5f)==10 && MediaPipeTestStats.Percentile(samples,.95f)==19,"Nearest-rank median/p95 retain tail latency");
            var stats=new MediaPipeTestStats();var observed=new bool[26];var confidence=new float[26];
            foreach(int j in new[]{9,10,13,14}) { observed[j]=true;confidence[j]=1; }
            var d=new RTMPosePoseProvider.Diagnostics{accepted=true,inferenceMs=20,computeMs=10,receivedAt=5.1,
                observed=observed,confidence=confidence};
            stats.Add(d);stats.StartMovement("punch_fast",5);stats.Add(d);
            d.accepted=false;d.resultReason="no_person";d.receivedAt=5.2;stats.Add(d);
            d.resultReason="stale";d.receivedAt=5.3;stats.Add(d);stats.Avatar(25);
            var result=stats.Finish(6)[0];
            Check(result.evaluated==3 && result.accepted==1 && result.noPerson==1 && result.stale==1,"Movement stats exclude calibration and retain all rejection outcomes");
            Check(Mathf.Abs(result.leftWristAvailablePercent-100f/3)<.01f,"Tracking availability includes detection loss in denominator");
            Check(result.avatarSamples==1 && result.p95AvatarMs==25 && result.acceptedFPS==1,"Unique avatar sample and duration-based accepted FPS");
            var stillStats=new MediaPipeTestStats();stillStats.StartMovement("still_start",10);
            var raw=new Vector3[33];var joints=new Vector3[26];
            d=new RTMPosePoseProvider.Diagnostics{accepted=true,observed=observed,confidence=confidence,rawWorld=raw,joints=joints,scale=2,receivedAt=12.1};
            stillStats.Add(d);
            foreach(int j in new[]{9,10,13,14}) {raw[MediaPipeWorldPose.HalpeToMediaPipe[j]]=Vector3.right*.01f;joints[j]=Vector3.right*.01f;}
            d.receivedAt=12.2;stillStats.Add(d);
            var stillResult=stillStats.Finish(13)[0];
            Check(stillResult.stillnessSamples==2 && Mathf.Abs(stillResult.rawStillnessRmsCm-1)<.001f
                && Mathf.Abs(stillResult.filteredStillnessRmsCm-.5f)<.001f,"Stillness RMS uses root-relative calibrated units and excludes cue transition");
            Check(MediaPipeImprovementTest.Labels.Length==MediaPipeImprovementTest.Cues.Length
                && Array.IndexOf(MediaPipeImprovementTest.Labels,"torso_forward")>=0
                && Array.IndexOf(MediaPipeImprovementTest.Labels,"right_knee")>=0
                && Array.IndexOf(MediaPipeImprovementTest.Labels,"punch_fast")>=0,"Spoken protocol covers torso, both legs, elbows, overhead and punches");
            File.WriteAllText("Logs/mediapipe-improvement-validation.txt",report.ToString());Debug.Log(report.ToString());
        }
        catch(Exception e) {report.AppendLine("FAIL "+e);File.WriteAllText("Logs/mediapipe-improvement-validation.txt",report.ToString());throw;}
    }
    public static void RunBatch()
    {
        try
        {
            Run();EngineComparisonValidation.Run();RTMPoseValidation.Run();
            if(File.ReadAllText("Logs/RTMPose-validation.txt").Contains("FAIL:")) throw new Exception("RTMPose regression failed");
            PunchValidation.Run();FullBodyTestValidation.Run();ResponsiveMoveValidation.Run();EditorApplication.Exit(0);
        }
        catch(Exception e) {Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
