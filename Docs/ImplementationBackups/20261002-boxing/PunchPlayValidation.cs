using System;
using System.IO;
using System.Text;
using FitFight.Practice;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Runs the real scene/player loop; preview scenes cannot reliably refresh all skinned meshes.
[InitializeOnLoad]
public static class PunchPlayValidation
{
    private const string Key = "FitFight.PunchPlayValidation";
    private static int step, index, frame;
    private static double deadline;
    private static readonly StringBuilder report = new StringBuilder();
    private static FitFightPracticeMode practice;
    private static PlayerSelectionManager owner;
    private static RenderTexture target;
    private static double actionStarted;
    private static int initialHits, testSide;
    private static long sequence;
    private static readonly float[] phases = { 0, .2f, .4f, .6f, .85f };
    static PunchPlayValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {
                deadline=EditorApplication.timeSinceStartup+90;
                step=index=frame=0;
                EditorApplication.update+=Tick;
            }
        };
    }
    public static void RunBatch()
    {
        PunchValidation.Run();
        RTMPoseValidation.Run();
        FullBodyTestValidation.Run();
        if(File.ReadAllText("Logs/RTMPose-validation.txt").Contains("FAIL:")) throw new Exception("Tracking regression failed");
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Tracking4All/Scenes/Pose Avatar Tracking/AvatarTracking_new.unity");
        EditorApplication.isPlaying=true;
    }
    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Play mode validation timed out at step "+step);
            if(step==0)
            {
                owner=Object.FindFirstObjectByType<PlayerSelectionManager>();
                if(owner==null || ++frame<10) return;
                owner.StartPractice();step=1;return;
            }
            if(step==1)
            {
                practice=Object.FindFirstObjectByType<FitFightPracticeMode>();
                if(practice?.Ready!=true) return;
                if(practice.Punches?.Active!=true) throw new Exception("Practice did not enable punch assistance");
                report.AppendLine("PASS runtime practice enables punch assistance");
                practice.Punches.EditorPreview=true;
                target=new RenderTexture(540,960,24);
                practice.PracticeCamera.targetTexture=target;
                frame=0;step=2;
            }
            if(step==2)
            {
                practice.Punches.PreviewPunch(index/5,phases[index%5]);
                if(++frame<5) return;
                practice.PracticeCamera.Render();
                var old=RenderTexture.active;RenderTexture.active=target;
                var pixels=new Texture2D(540,960,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,540,960),0,0);pixels.Apply();
                File.WriteAllBytes($"Logs/Practice/play-punch-{index/5}-{phases[index%5]:F2}.png",pixels.EncodeToPNG());
                Object.DestroyImmediate(pixels);RenderTexture.active=old;
                var animator=practice.Punches.GetComponent<Animator>();
                Vector3 hand=animator.GetBoneTransform(index<5?HumanBodyBones.LeftHand:HumanBodyBones.RightHand).position;
                float gap=Vector3.Distance(hand,practice.Bag.Surface.ClosestPoint(hand));
                report.AppendLine($"POSE side={index/5} phase={phases[index%5]:F2} gap={gap:F3}");
                if(index%5==2 && gap>.10f) throw new Exception("Runtime punch misses target: "+gap);
                index++;frame=0;
                if(index<10) return;
                practice.Punches.EditorPreview=false;
                practice.Punches.SetAssistance(false);
                if(practice.Punches.GetComponent<Tracking4All.Avatar>().HasPoseOverride) throw new Exception("Mirroring ownership not restored");
                practice.Punches.SetAssistance(true);
                report.AppendLine("PASS assist / mirroring toggle restores ownership");
                ValidateBodyRecording();
                practice.PracticeCamera.targetTexture=null;Object.DestroyImmediate(target);
                practice.Punches.StartFullBodyRecording();
                owner.ShowPlayerSelection();step=3;return;
            }
            if(step==3)
            {
                if(++frame<4) return;
                if(Object.FindFirstObjectByType<AssistedPunchController>()!=null) throw new Exception("Controller leaked after return home");
                report.AppendLine("PASS return home releases punch controller and active body recording");
                owner.StartPractice();step=4;return;
            }
            if(step==4)
            {
                practice=Object.FindFirstObjectByType<FitFightPracticeMode>();
                if(practice?.Ready!=true) return;
                if(practice.Punches?.Active!=true) throw new Exception("Reentry did not bind punches");
                if(Object.FindObjectsByType<AssistedPunchController>(FindObjectsSortMode.None).Length!=1) throw new Exception("Duplicate punch owner");
                report.AppendLine("PASS reentry creates exactly one active punch owner");
                practice.Punches.EditorInput=true;
                actionStarted=Time.realtimeSinceStartupAsDouble;
                initialHits=practice.Bag.HitCount;testSide=0;
                practice.Bag.ResetBag();step=5;return;
            }
            if(step==5)
            {
                double now=Time.realtimeSinceStartupAsDouble;
                float elapsed=(float)(now-actionStarted);
                float extension=elapsed<.4f ? 0 : elapsed<.65f ? (elapsed-.4f)/.25f : elapsed<.8f ? 1 : Mathf.Clamp01(1-(elapsed-.8f)/.25f);
                var guard=new PunchArmObservation { wrist=new Vector2(.25f,.25f),elbow=new Vector2(.4f,-.2f),confidence=1 };
                var arm=new PunchArmObservation { wrist=new Vector2(.25f+.65f*extension,.25f-.05f*extension),elbow=new Vector2(.4f+.15f*extension,-.2f+.2f*extension),confidence=1 };
                practice.Punches.SubmitEditorFrame(new PunchCameraInput.Frame { sequence=++sequence,time=now,received=now,left=testSide==0?arm:guard,right=testSide==1?arm:guard });
                if(elapsed<1.5f) return;
                int hits=practice.Bag.HitCount-initialHits;
                if(hits!=1) throw new Exception($"Observed {testSide} punch should score once, got {hits}, state={practice.Punches.State}");
                report.AppendLine($"PASS side {testSide}: observation → commitment → animated fist → one physical contact");
                if(testSide++==0)
                {
                    practice.Punches.ResetMotion();practice.Bag.ResetBag();
                    initialHits=practice.Bag.HitCount;actionStarted=now;return;
                }
                practice.Punches.EditorInput=false;
                owner.ShowPlayerSelection();Finish(0);
            }
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);Debug.LogException(e);Finish(1); }
    }
    private static void ValidateBodyRecording()
    {
        var punches=practice.Punches; var avatar=punches.GetComponent<Tracking4All.Avatar>();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var update=typeof(AssistedPunchController).GetMethod("LateUpdate",flags);
        punches.EditorInput=true;
        punches.StartFullBodyRecording();
        if(!punches.Recording || punches.Active || avatar.HasPoseOverride) throw new Exception("Full body recording failed to release punch ownership");
        string path=punches.RecordingPath;
        punches.SubmitEditorFrame(new PunchCameraInput.Frame { sequence=9001,time=Time.realtimeSinceStartupAsDouble,received=Time.realtimeSinceStartupAsDouble,jointLayout="Halpe26",imageJoints=new Vector2[26],jointScores=new float[26] });
        update.Invoke(punches,null); punches.RecordFinalPose();
        CaptureBodyGuide();
        // The next rendered frame must not duplicate the observation.
        update.Invoke(punches,null); punches.RecordFinalPose();
        typeof(AssistedPunchController).GetField("recordingStarted",flags).SetValue(punches,Time.realtimeSinceStartupAsDouble-FullBodyRecordingGuide.Duration-1);
        update.Invoke(punches,null);
        if(punches.Recording || !punches.Active || !avatar.HasPoseOverride) throw new Exception("Full body timeout did not restore assisted ownership");
        double drain=EditorApplication.timeSinceStartup+3;
        string data="";
        while(EditorApplication.timeSinceStartup<drain)
        {
            if(File.Exists(path)) { data=File.ReadAllText(path); if(data.Contains("droppedRecords")) break; }
            System.Threading.Thread.Sleep(10);
        }
        int observations=0,poses=0;
        foreach(string line in data.Split('\n')) { if(line.Contains("\"kind\":\"observation\"")) observations++; if(line.Contains("\"kind\":\"pose\"")) poses++; }
        if(observations!=1 || poses!=2 || !data.Contains("\"assisted\":false")) throw new Exception("Body capture does not separate inference observations from final rendered poses");
        report.AppendLine("PASS body test: mirroring ownership, fresh-only observations, final poses, automatic stop and assistance restore");
        punches.SetAssistance(false); punches.StartFullBodyRecording(); punches.StopRecording();
        if(punches.Active || avatar.HasPoseOverride) throw new Exception("Body test incorrectly restored assistance when started from mirroring");
        punches.SetAssistance(true); punches.StartFullBodyRecording();
        typeof(AssistedPunchController).GetMethod("OnApplicationPause",flags).Invoke(punches,new object[]{true});
        if(punches.Recording || !punches.Active) throw new Exception("Pause did not stop body capture and restore mode");
        var original=BodyTrackingSelection.Selected;
        try
        {
            BodyTrackingSelection.Select(BodyTrackingSystem.RTMPose);
            var source=avatar.PosePuppet.PoseSource.GetComponent<Tracking4All.PoseSolution>();
            var controller=source.GetComponent<BodyTrackingController>();
            if(source.ActiveRTMPose==null || controller==null) throw new Exception("Cannot test actual RTMPose binding");
            bool direct=source.ActiveRTMPose.Tuning.directAvatar;
            foreach(bool priorDirect in new[]{false,true})
            {
                controller.SetDirectAvatar(priorDirect);
                punches.StartFullBodyRecording();
                if(!source.ActiveRTMPose.Tuning.directAvatar || avatar.DirectTracking==null || !avatar.PosePuppet.RetargetingBypassed) throw new Exception("Body test did not bind direct retargeter");
                punches.StopRecording();
                if(source.ActiveRTMPose.Tuning.directAvatar!=priorDirect || (avatar.DirectTracking!=null)!=priorDirect) throw new Exception("Body test failed to restore prior direct selection");
            }
            controller.SetDirectAvatar(direct);
            report.AppendLine("PASS body test enables actual direct retargeter and restores both prior direct/puppet selections");
        }
        finally { BodyTrackingSelection.Select(original); }
        punches.EditorInput=false;
        report.AppendLine("PASS body test: manual stop, original mirroring mode and pause cleanup");
    }
    private static void CaptureBodyGuide()
    {
        var canvas=GameObject.Find("Practice HUD").GetComponent<Canvas>();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var prompt=(UnityEngine.UI.Text)typeof(FitFightPracticeMode).GetField("recordingPrompt",flags).GetValue(practice);
        prompt.text=practice.Punches.RecordingPrompt;prompt.transform.parent.gameObject.SetActive(true);
        ((UnityEngine.UI.Text)typeof(FitFightPracticeMode).GetField("recordingLabel",flags).GetValue(practice)).text="STOP RECORDING";
        ((UnityEngine.UI.Text)typeof(FitFightPracticeMode).GetField("punchModeLabel",flags).GetValue(practice)).text="BODY MIRRORING";
        var mode=canvas.renderMode;var camera=canvas.worldCamera;float distance=canvas.planeDistance;
        try
        {
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=practice.PracticeCamera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();practice.PracticeCamera.Render();
            var old=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try { pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes("Logs/Practice/full-body-guide-ui.png",pixels.EncodeToPNG()); }
            finally { Object.DestroyImmediate(pixels);RenderTexture.active=old; }
        }
        finally { canvas.renderMode=mode;canvas.worldCamera=camera;canvas.planeDistance=distance; }
    }
    private static void Finish(int code)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
        File.WriteAllText("Logs/Practice/punch-play-validation.txt",report.ToString());
        Debug.Log(report.ToString());EditorApplication.Exit(code);
    }
}
