using System;
using System.IO;
using System.Reflection;
using System.Text;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class EngineComparisonPlayValidation
{
    private const string Key="FitFight.EngineComparisonPlay";
    private static int phase,index,frames,submitted;
    private static double deadline,nextSubmit;
    private static PlayerSelectionManager owner;
    private static FitFightPracticeMode practice;
    private static BodyTrackingController controller;
    private static uint[] rgba;
    private static float[] chw;
    private static BodyTrackingSystem saved;
    private static readonly BodyTrackingSystem[] Engines={BodyTrackingSystem.RTMPoseNCNN,BodyTrackingSystem.MediaPipeLite,BodyTrackingSystem.MediaPipeFull,BodyTrackingSystem.RTMPose,BodyTrackingSystem.Tracking4All,BodyTrackingSystem.RTMPoseNCNN,BodyTrackingSystem.MediaPipeLite,BodyTrackingSystem.MediaPipeFull};
    private static readonly StringBuilder report=new StringBuilder();
    static EngineComparisonPlayValidation()
    {
        EditorApplication.playModeStateChanged += state => {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            { phase=index=frames=submitted=0; deadline=EditorApplication.timeSinceStartup+180; EditorApplication.update+=Tick; }
        };
    }
    public static void RunBatch()
    {
        SessionState.SetBool(Key,true);SessionState.SetInt(Key+".saved",(int)BodyTrackingSelection.Selected);
        BodyTrackingSelection.Select(BodyTrackingSystem.Tracking4All);
        EditorSceneManager.OpenScene("Assets/Tracking4All/Scenes/Pose Avatar Tracking/AvatarTracking_new.unity");
        EditorApplication.isPlaying=true;
    }
    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Engine play check timed out: phase="+phase+" engine="+index+" frames="+submitted+" "+controller?.Status+" "+controller?.RTMPose?.PipelineStatus);
            if(phase==0)
            {
                owner=Object.FindFirstObjectByType<PlayerSelectionManager>();if(owner==null || ++frames<15) return;
                saved=(BodyTrackingSystem)SessionState.GetInt(Key+".saved",0);
                LoadPixels();owner.StartPractice();phase=1;return;
            }
            if(phase==1)
            {
                practice=Object.FindFirstObjectByType<FitFightPracticeMode>();if(practice?.Ready!=true) return;
                var avatar=practice.Punches.GetComponent<Tracking4All.Avatar>();
                controller=avatar.PosePuppet.PoseSource.GetComponent<BodyTrackingController>();
                if(controller==null) throw new Exception("No local engine controller");
                Select();phase=2;return;
            }
            if(phase==2)
            {
                var engine=Engines[index];var pose=controller.RTMPose;
                if(engine==BodyTrackingSystem.Tracking4All)
                { if(pose != null && pose.isActiveAndEnabled) throw new Exception("Baseline switch retained alternative provider"); Next();return; }
                if(pose==null || !pose.isActiveAndEnabled) throw new Exception("Engine fell back: "+controller.Status);
                if(pose.ConfiguredEngine != engine) throw new Exception("Selector did not switch engine");
                if(engine==BodyTrackingSystem.RTMPose) { Next();return; }
                var native=(PoseInferenceBackend)typeof(RTMPosePoseProvider).GetField("native",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pose);
                if(native==null || !native.Ready) return;
                pose.cameraPitchDegrees=0;
                if(Time.realtimeSinceStartupAsDouble>=nextSubmit && pose.EditorSubmitNativeFrame(chw,rgba,288,512))
                { submitted++;nextSubmit=Time.realtimeSinceStartupAsDouble+.04; }
                if(submitted>100 && !pose.Calibrated) throw new Exception("Standing fixture could not calibrate "+engine+": "+pose.StandingCalibrationHint+" "+pose.CalibrationFrames+"/20 ageMs="+pose.LastInferenceMs+" points="+string.Join(";",pose.Points)+" scores="+string.Join(",",pose.Scores));
                if(!pose.HasRecentPose || !pose.Calibrated || pose.PoseSequence<3) return;
                var diagnostics=pose.CaptureDiagnostics();
                if(diagnostics.learnedWorldLandmarks != pose.UsesWorldLandmarks || diagnostics.engine != pose.EngineLabel) throw new Exception("Diagnostic engine labels are wrong");
                if(controller.DirectDriver == null || !controller.DirectDriver.isActiveAndEnabled) throw new Exception("Alternative engine did not bind direct avatar");
                report.AppendLine("PASS real native → standing calibration → pose publication → direct avatar: "+engine+" poses="+pose.PoseSequence);
                Next();return;
            }
            if(phase==3)
            {
                practice.StartSanityCheck();
                if(practice.Punches.Recording || practice.Punches.Active) throw new Exception("Sanity check must mirror without recording/punch assistance");
                if(Object.FindFirstObjectByType<QuickPoseCheck>()==null) throw new Exception("Spoken quick guide missing");
                report.AppendLine("PASS sanity check mirrors with recording off and spoken guide");
                owner.ShowPlayerSelection();phase=4;frames=Time.frameCount;return;
            }
            if(phase==4)
            {
                if(Time.frameCount<frames+2) return;
                if(Object.FindFirstObjectByType<QuickPoseCheck>() != null) throw new Exception("Quick guide leaked after return home");
                report.AppendLine("PASS return home stops and releases spoken guide");
                Finish(null);
            }
        }
        catch(Exception e) { Finish(e); }
    }
    private static void Select()
    { submitted=0;nextSubmit=0;BodyTrackingSelection.Select(Engines[index]);controller.SetDirectAvatar(true); }
    private static void Next()
    { report.AppendLine("PASS active engine switch: "+Engines[index]);if(++index<Engines.Length) Select();else phase=3; }
    private static void LoadPixels()
    {
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true);texture.LoadImage(File.ReadAllBytes("NativePose/tests/standing.png"));
        var colors=texture.GetPixels32();rgba=new uint[colors.Length];
        for(int y=0;y<512;y++) for(int x=0;x<288;x++) { var c=colors[(511-y)*288+x];rgba[y*288+x]=(uint)(c.r|c.g<<8|c.b<<16|255<<24); }
        var modelInput=new Texture2D(2,2,TextureFormat.RGBA32,false,true);modelInput.LoadImage(File.ReadAllBytes("NativePose/tests/ncnn-input.png"));
        var modelPixels=modelInput.GetPixels32();
        chw=new float[3*192*256];float[] mean={123.675f,116.28f,103.53f},std={58.395f,57.12f,57.375f};
        for(int y=0;y<256;y++)for(int x=0;x<192;x++)
        {
            var c=modelPixels[(255-y)*192+x];int i=y*192+x;
            chw[i]=(c.r-mean[0])/std[0];chw[49152+i]=(c.g-mean[1])/std[1];chw[98304+i]=(c.b-mean[2])/std[2];
        }
        Object.DestroyImmediate(modelInput);
        Object.DestroyImmediate(texture);
    }
    private static void Finish(Exception error)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
        BodyTrackingSelection.Select(saved);
        if(error!=null) { report.AppendLine("FAIL "+error);Debug.LogException(error); }
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/engine-comparison-play.txt",report.ToString());Debug.Log(report.ToString());
        EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(error==null?0:1);
    }
}
