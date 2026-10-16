using System;
using System.IO;
using System.Reflection;
using System.Text;
using FitFight.Practice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class GameBodyPlayValidation
{
    private const string Key="FitFight.GameBodyPlayValidation";
    private static readonly StringBuilder report=new StringBuilder();
    private static FitFightPracticeMode practice;
    private static PlayerSelectionManager owner;
    private static int stage,index,previousSample=-1,initialHits,tracePhase=-1;
    private static long sequence;
    private static double deadline,started;
    private static float minGap;
    private static bool captured;
    private static RenderTexture target;
    private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static GameBodyPlayValidation()
    {
        EditorApplication.playModeStateChanged+=state => {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            { deadline=EditorApplication.timeSinceStartup+100;stage=index=0;EditorApplication.update+=Tick; }
        };
    }
    public static void RunBatch()
    {
        GameBodyValidation.Run();SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Tracking4All/Scenes/Pose Avatar Tracking/AvatarTracking_new.unity");
        EditorApplication.isPlaying=true;
    }
    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Body play validation timeout at stage "+stage);
            if(stage==0)
            {
                owner=Object.FindFirstObjectByType<PlayerSelectionManager>();if(owner==null)return;
                owner.StartPractice();stage=1;return;
            }
            if(stage==1)
            {
                practice=Object.FindFirstObjectByType<FitFightPracticeMode>();if(practice?.Ready!=true)return;
                practice.Punches.EditorInput=true;
                target=new RenderTexture(540,960,24);practice.PracticeCamera.targetTexture=target;
                StartAction();stage=2;return;
            }
            double now=Time.realtimeSinceStartupAsDouble;
            if(stage==2)
            {
                int side=index%2;string kind=index<2?"front":index<4?"side":"knee";
                float elapsed=(float)(now-started);int sample=Mathf.FloorToInt(elapsed*10);
                if(sample!=previousSample)
                {
                    string pose=elapsed<.5f || elapsed>=1.25f ? "standing" : elapsed<.7f ? "chamber" : elapsed<.9f ? kind : "chamber";
                    Submit(now,pose=="standing"?-1:side,pose);previousSample=sample;
                }
                var motion=practice.Punches.BodyMotion;
                if(motion.IsStrike)
                {
                    var sampleContact=practice.Bag.GetContactSample(motion.ContactLimb);
                    int bucket=Mathf.FloorToInt(motion.Phase*5);
                    if(bucket!=tracePhase) { tracePhase=bucket;report.AppendLine($"CONTACT {kind}{side} phase {motion.Phase:F2}: {sampleContact.reason}, gap {sampleContact.gap:F3}m"); }
                    var animator=practice.Punches.GetComponent<Animator>();
                    var bone=animator.GetBoneTransform(kind=="knee" ? (side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg)
                        : (side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot));
                    minGap=Mathf.Min(minGap,Vector3.Distance(bone.position,practice.Bag.Surface.ClosestPoint(bone.position)));
                    if(!captured && motion.Phase>.41f) { Capture($"game-{kind}-{side}");captured=true; }
                }
                if(elapsed<1.8f) return;
                int hits=practice.Bag.HitCount-initialHits;
                if(hits!=1) throw new Exception($"Full-body {side} {kind}: expected one physical hit, got {hits}, min gap {minGap:F3}, reason {motion.Reason}");
                report.AppendLine($"PASS actual rig {side} {kind}: observed chamber → strike → one physical contact; min gap {minGap:F3}m");
                if(++index<6) { StartAction();return; }
                practice.Punches.ResetMotion();practice.Bag.ResetBag();initialHits=practice.Bag.HitCount;
                started=now;previousSample=-1;stage=3;return;
            }
            if(stage==3)
            {
                float elapsed=(float)(now-started);int sample=Mathf.FloorToInt(elapsed*10);
                if(sample!=previousSample) { Submit(now,elapsed<.5f || elapsed>1.2f?-1:0,"chamber");previousSample=sample; }
                if(elapsed<1.6f)return;
                if(practice.Bag.HitCount!=initialHits) throw new Exception("Held knee lift caused a hit");
                report.AppendLine("PASS actual rig held knee lift returns without a bag hit");
                practice.Punches.ResetMotion();started=now;previousSample=-1;stage=4;return;
            }
            if(stage==4)
            {
                float elapsed=(float)(now-started);int sample=Mathf.FloorToInt(elapsed*10);
                if(sample!=previousSample) { Submit(now,-1,elapsed<.5f?"standing":"squat");previousSample=sample; }
                if(elapsed<1.2f)return;
                if(practice.Bag.HitCount!=initialHits || practice.Punches.BodyMotion.Squat<.3f) throw new Exception("Squat did not remain tracked and non-scoring");
                Capture("game-squat");report.AppendLine("PASS actual rig shallow squat follows continuous posture without scoring a strike");
                practice.Punches.ResetMotion();started=now;previousSample=-1;stage=5;return;
            }
            if(stage==5 || stage==6)
            {
                float elapsed=(float)(now-started);int sample=Mathf.FloorToInt(elapsed*10);
                if(sample!=previousSample)
                {
                    var f=GameBodyValidation.Frame(++sequence,now);
                    if(stage==5) { f.body=Quaternion.Euler(45,0,12);f.head=Quaternion.Euler(18,25,0); }
                    else for(int side=0;side<2;side++)
                    {
                        f.joints[13+side]=f.joints[11+side]+Vector3.up*.3f;
                        f.joints[15+side]=f.joints[11+side]+Vector3.up*.65f;
                    }
                    practice.Punches.BodyMotion.Observe(f,now);
                    var guard=MediaPipeBoxingValidation.Guard();var arm=guard;
                    if(stage==6) { arm.wrist=new Vector2(0,1.1f);arm.wristWorld=new Vector3(0,1,0); }
                    practice.Punches.SubmitEditorFrame(new PunchCameraInput.Frame { sequence=sequence,time=now,received=now,boxing=true,left=arm,right=arm });
                    previousSample=sample;
                }
                if(elapsed<1.1f) return;
                if(practice.Bag.HitCount!=initialHits) throw new Exception("Posture/overhead gesture caused a strike");
                if(stage==5)
                {
                    var body=(Quaternion)typeof(AssistedPunchController).GetField("bodyOffset",Private).GetValue(practice.Punches);
                    var head=(Quaternion)typeof(AssistedPunchController).GetField("headOffset",Private).GetValue(practice.Punches);
                    if(Quaternion.Angle(body,Quaternion.identity)<35 || Quaternion.Angle(head,Quaternion.identity)<20)
                        throw new Exception("Coherent torso/head motion did not exceed old boxing limits");
                    Capture("game-torso-head");report.AppendLine("PASS actual rig torso/pelvis and head follow measured bends without creating attacks");
                    practice.Punches.ResetMotion();started=now;previousSample=-1;stage=6;return;
                }
                var animator=practice.Punches.GetComponent<Animator>();float headY=animator.GetBoneTransform(HumanBodyBones.Head).position.y;
                if(animator.GetBoneTransform(HumanBodyBones.LeftHand).position.y<headY || animator.GetBoneTransform(HumanBodyBones.RightHand).position.y<headY)
                    throw new Exception("Both raised arms were not mirrored above head");
                Capture("game-overhead");report.AppendLine("PASS actual rig bilateral overhead arms remain tracked and non-scoring");
                Recording();owner.ShowPlayerSelection();stage=7;started=now;return;
            }
            if(stage==7 && now-started>.2)
            {
                if(Object.FindFirstObjectByType<AssistedPunchController>()!=null) throw new Exception("Full-body owner leaked after home");
                report.AppendLine("PASS full-body recorder/pose owner released on home");Finish(0);
            }
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);Debug.LogException(e);Finish(1); }
    }
    private static void StartAction()
    {
        practice.Punches.ResetMotion();practice.Bag.ResetBag();initialHits=practice.Bag.HitCount;
        started=Time.realtimeSinceStartupAsDouble;previousSample=-1;minGap=100;captured=false;tracePhase=-1;
    }
    private static void Submit(double now,int side,string pose)
    {
        var f=GameBodyValidation.Frame(++sequence,now,side,pose);
        practice.Punches.BodyMotion.Observe(f,now);
        var guard=MediaPipeBoxingValidation.Guard();
        practice.Punches.SubmitEditorFrame(new PunchCameraInput.Frame { sequence=sequence,time=now,received=now,boxing=true,left=guard,right=guard });
    }
    private static void Recording()
    {
        var punches=practice.Punches;punches.SetAssistance(false);
        var button=GameObject.Find("Practice HUD/Safe area/GAME BODY TEST · 2:44")?.GetComponent<UnityEngine.UI.Button>();
        if(button==null) throw new Exception("Single-run test button missing");button.onClick.Invoke();
        if(!punches.Recording || !punches.Active || !punches.GameBodyRecording || punches.FullBodyDiagnostic)
            throw new Exception("Game test must record with full-body assistance active");
        string path=punches.RecordingPath;
        var prompt=(UnityEngine.UI.Text)typeof(FitFightPracticeMode).GetField("recordingPrompt",Private).GetValue(practice);
        prompt.text=punches.RecordingPrompt;prompt.transform.parent.gameObject.SetActive(true);
        ((UnityEngine.UI.Text)typeof(FitFightPracticeMode).GetField("recordingLabel",Private).GetValue(practice)).text="STOP RECORDING";
        Capture("game-body-guide-ui",true);
        typeof(AssistedPunchController).GetField("recordingStarted",Private).SetValue(punches,Time.realtimeSinceStartupAsDouble-GameBodyRecordingGuide.Duration-.1);
        typeof(AssistedPunchController).GetMethod("UpdateRecording",Private).Invoke(punches,new object[]{Time.realtimeSinceStartupAsDouble});
        if(punches.Recording || punches.Active) throw new Exception("Completed game test did not restore prior mirroring mode");
        double until=EditorApplication.timeSinceStartup+3;
        while(!File.Exists(Path.ChangeExtension(path,"summary.md")) && EditorApplication.timeSinceStartup<until) System.Threading.Thread.Sleep(10);
        if(!File.Exists(Path.ChangeExtension(path,"summary.json")) || !File.ReadAllText(Path.ChangeExtension(path,"summary.json")).Contains("\"completed\": true"))
            throw new Exception("Auto completion did not persist game summary");
        report.AppendLine("PASS single-run UI enables assistance, auto-finishes, writes JSONL/summary.json/summary.md and restores previous mode");
    }
    private static void Capture(string name,bool hud=false)
    {
        var canvas=GameObject.Find("Practice HUD").GetComponent<Canvas>();var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;
        if(hud) { canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=practice.PracticeCamera;canvas.planeDistance=1; }
        else canvas.enabled=false;
        try
        {
            Canvas.ForceUpdateCanvases();practice.PracticeCamera.Render();var old=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes("Logs/Practice/"+name+".png",image.EncodeToPNG());
            Object.DestroyImmediate(image);RenderTexture.active=old;
        }
        finally { canvas.enabled=true;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera; }
    }
    private static void Finish(int code)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
        File.WriteAllText("Logs/Practice/game-body-play-validation.txt",report.ToString());EditorApplication.Exit(code);
    }
}
