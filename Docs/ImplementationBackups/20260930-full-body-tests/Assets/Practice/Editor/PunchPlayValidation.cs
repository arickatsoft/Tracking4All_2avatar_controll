using System;
using System.IO;
using System.Text;
using FitFight.Practice;
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
                practice.PracticeCamera.targetTexture=null;Object.DestroyImmediate(target);
                owner.ShowPlayerSelection();step=3;return;
            }
            if(step==3)
            {
                if(++frame<4) return;
                if(Object.FindFirstObjectByType<AssistedPunchController>()!=null) throw new Exception("Controller leaked after return home");
                report.AppendLine("PASS return home releases punch controller");
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
    private static void Finish(int code)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
        File.WriteAllText("Logs/Practice/punch-play-validation.txt",report.ToString());
        Debug.Log(report.ToString());EditorApplication.Exit(code);
    }
}
