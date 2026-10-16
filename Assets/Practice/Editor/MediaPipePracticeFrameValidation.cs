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
public static class MediaPipePracticeFrameValidation
{
    private const string Key="FitFight.PracticeFrames";
    private static FitFightPracticeMode practice;
    private static PlayerSelectionManager owner;
    private static BodyTrackingController controller;
    private static uint[] rgba;
    private static double deadline,nextSubmit,began;
    private static int phase,submitted,initialFrame;
    private static string path,exception;
    private static bool savedDirect;
    private static object savedBag;
    private static string faultPrompt;
    private static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    private static readonly StringBuilder report=new StringBuilder();
    static MediaPipePracticeFrameValidation()
    {
        EditorApplication.playModeStateChanged+=s=>{ if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            { phase=submitted=0;deadline=EditorApplication.timeSinceStartup+100;Application.logMessageReceived+=Log;EditorApplication.update+=Tick; } };
    }
    public static void RunBatch()
    {
        SessionState.SetBool(Key,true);SessionState.SetInt(Key+".saved",(int)BodyTrackingSelection.Selected);
        BodyTrackingSelection.Select(BodyTrackingSystem.MediaPipeFull);
        EditorSceneManager.OpenScene("Assets/Tracking4All/Scenes/Pose Avatar Tracking/AvatarTracking_new.unity");EditorApplication.isPlaying=true;
    }
    private static void Log(string text,string stack,LogType type)
    { if(type==LogType.Exception && (stack.Contains("FitFightPracticeMode") || stack.Contains("AssistedPunchController"))) exception=text+"\n"+stack; }
    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Practice frame check timeout "+phase+" "+controller?.RTMPose?.PipelineStatus);
            if(exception!=null) throw new Exception(exception);
            if(phase==0)
            { owner=Object.FindFirstObjectByType<PlayerSelectionManager>();if(owner==null || Time.frameCount<10)return;owner.StartPractice();phase=1;return; }
            if(phase==1)
            {
                practice=Object.FindFirstObjectByType<FitFightPracticeMode>();if(practice?.Ready!=true) return;
                controller=practice.Punches.GetComponent<Tracking4All.Avatar>().PosePuppet.PoseSource.GetComponent<BodyTrackingController>();
                savedDirect=controller.RTMPose.Tuning.directAvatar;controller.SetDirectAvatar(false);
                var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);t.LoadImage(File.ReadAllBytes("NativePose/tests/standing.png"));
                var colors=t.GetPixels32();rgba=new uint[colors.Length];
                for(int y=0;y<512;y++)for(int x=0;x<288;x++) { var c=colors[(511-y)*288+x];rgba[y*288+x]=(uint)(c.r|c.g<<8|c.b<<16|255<<24); }
                Object.DestroyImmediate(t);phase=2;
            }
            var pose=controller.RTMPose;
            if(Time.realtimeSinceStartupAsDouble>=nextSubmit && pose.EditorSubmitNativeFrame(null,rgba,288,512))
            {submitted++;nextSubmit=Time.realtimeSinceStartupAsDouble+.1;}
            if(phase==2)
            {
                if(!pose.Calibrated || !pose.HasRecentPose) return;
                practice.StartBoxingTest();path=practice.Punches.RecordingPath;
                began=Time.realtimeSinceStartupAsDouble;initialFrame=Time.frameCount;phase=3;return;
            }
            if(phase==3 && Time.realtimeSinceStartupAsDouble-began>4)
            {
                report.AppendLine("Rendered frames "+(Time.frameCount-initialFrame));
                practice.Punches.PreviewPunch(0,.3f);practice.Punches.EditorPreview=true;
                savedBag=practice.Bag.Surface;
                faultPrompt=((UnityEngine.UI.Text)typeof(FitFightPracticeMode).GetField("recordingPrompt",flags).GetValue(practice)).text;
                // Fault only the contact collider reference. The bag, model and
                // final pose recorder remain live; HUD must advance independently.
                typeof(FitFight.Practice.PracticeHeavyBag).GetField("<Surface>k__BackingField",flags).SetValue(practice.Bag,null);
                began=Time.realtimeSinceStartupAsDouble;phase=4;return;
            }
            if(phase==4 && Time.realtimeSinceStartupAsDouble-began>1)
            {
                string prompt=((UnityEngine.UI.Text)typeof(FitFightPracticeMode).GetField("recordingPrompt",flags).GetValue(practice)).text;
                if(prompt==faultPrompt) throw new Exception("Recording HUD froze during contact failure");
                typeof(FitFight.Practice.PracticeHeavyBag).GetField("<Surface>k__BackingField",flags).SetValue(practice.Bag,savedBag);
                practice.Punches.EditorPreview=false;
                report.AppendLine("PASS recording timer continues during injected contact failure");
                practice.Punches.StopRecording();phase=5;return;
            }
            if(phase==5)
            {
                if(!File.Exists(path) || !File.ReadAllText(path).Contains("droppedRecords")) return;
                int observations=0,poses=0,diagnostics=0;
                foreach(string line in File.ReadLines(path)) { if(line.Contains("\"kind\":\"observation\""))observations++;if(line.Contains("\"kind\":\"pose\""))poses++;if(line.Contains("\"kind\":\"reconstruction\""))diagnostics++; }
                report.AppendLine($"observations={observations}, final poses={poses}, diagnostics={diagnostics}");
                if(observations<20 || poses<20) throw new Exception("Practice HUD/pose recording stalled despite live native observations");
                var stats=JsonUtility.FromJson<FitFight.Practice.BoxingSessionStats>(File.ReadAllText(Path.ChangeExtension(path,"summary.json")));
                if(stats.runtimeErrors<1 || !stats.lastRuntimeError.StartsWith("contacts:") || diagnostics<20)
                    throw new Exception("Contact failure or independent inference diagnostic was not recorded");
                report.AppendLine("PASS contact error captured in summary; inference diagnostics continue independently");
                report.AppendLine("PASS native MediaPipe Full with direct avatar OFF → assisted practice updates and records final pose");Finish(null);
            }
        }
        catch(Exception e) { Finish(e); }
    }
    private static void Finish(Exception e)
    {
        EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SessionState.SetBool(Key,false);
        if(controller!=null) controller.SetDirectAvatar(savedDirect);
        if(practice!=null && savedBag!=null) typeof(FitFight.Practice.PracticeHeavyBag).GetField("<Surface>k__BackingField",flags).SetValue(practice.Bag,savedBag);
        BodyTrackingSelection.Select((BodyTrackingSystem)SessionState.GetInt(Key+".saved",0));
        if(e!=null) { report.AppendLine("FAIL "+e);Debug.LogException(e); }
        File.WriteAllText("Logs/Practice/mediapipe-practice-frames.txt",report.ToString());
        EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(e==null?0:1);
    }
}
