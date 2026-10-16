using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using FitFight.BodyTracking;
using FitFight.Practice;
using Tracking4All;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Reviewed camera observations are weak labels, never independent 3D ground truth.
[InitializeOnLoad]
public static class RecordedBodyFixValidation
{
    const string Key="FitFight.RecordedBodyFixValidation";
    [Serializable] class Row { public string kind; public double recordedRelative; public PunchCameraInput.Frame observation; public Meta metadata; }
    [Serializable] class Meta { public string tuning; public float userHeight,cameraPitchDegrees; }
    static bool responsive;
    static string ReportName => responsive ? "responsive-body" : "recorded-body-fixes";
    static double deadline;
    static bool opened;
    static System.Collections.IEnumerator replay;
    static RecordedBodyFixValidation()
    {
        EditorApplication.playModeStateChanged+=s=> {
            if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            { responsive=SessionState.GetBool(Key+".Responsive",false); deadline=EditorApplication.timeSinceStartup+90; opened=false; replay=null; EditorApplication.update+=Tick; }
        };
    }
    public static void RunResponsiveBatch() { SessionState.SetBool(Key+".Responsive",true); Start(); }
    public static void RunBatch() { SessionState.SetBool(Key+".Responsive",false); Start(); }
    static void Start()
    {
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Tracking4All/Scenes/Pose Avatar Tracking/AvatarTracking_new.unity");
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Recorded body replay timed out");
            if(!opened)
            {
                var owner=UnityEngine.Object.FindFirstObjectByType<PlayerSelectionManager>(); if(owner==null) return;
                owner.StartPractice(); opened=true; return;
            }
            var practice=UnityEngine.Object.FindFirstObjectByType<FitFightPracticeMode>(); if(practice?.Ready!=true) return;
            if(replay==null) replay=Replay(practice);
            if(!replay.MoveNext()) Finish(0);
        }
        catch(Exception e) { File.AppendAllText("Logs/Practice/"+ReportName+".txt","FAIL "+e); Debug.LogException(e); Finish(1); }
    }
    static System.Collections.IEnumerator Replay(FitFightPracticeMode practice)
    {
        var rows=File.ReadLines(responsive ? "Docs/PunchSessions/20260930-standing-fix-body-regression.jsonl" : "Docs/PunchSessions/20260930-body-fullbody-schema2-regression.jsonl").Select(x=>JsonUtility.FromJson<Row>(x)).ToList();
        var meta=rows[0].metadata;
        BodyTrackingSelection.Select(BodyTrackingSystem.RTMPose);
        var punches=practice.Punches; punches.SetAssistance(false); punches.EditorPreview=true;
        var avatar=punches.GetComponent<Tracking4All.Avatar>();
        var controller=avatar.PosePuppet.PoseSource.GetComponent<BodyTrackingController>(); controller.SetDirectAvatar(true);
        punches.StartFullBodyRecording();
        var provider=avatar.PosePuppet.PoseSource.GetComponent<PoseSolution>().ActiveRTMPose;
        provider.userHeight=meta.userHeight; provider.cameraPitchDegrees=responsive ? 22.39323f : meta.cameraPitchDegrees;
        provider.Tuning=JsonUtility.FromJson<RTMPoseTuning>(meta.tuning); provider.Tuning.directAvatar=true;
        provider.Tuning.prediction=0; provider.Tuning.observationInterval=.1f;
        provider.ResetCalibration();
        var solver=(Skeleton25DSolver)typeof(RTMPosePoseProvider).GetField("solver",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(provider);
        solver.RequireStandingCalibration=true;
        var direct=avatar.DirectTracking; if(direct==null) throw new Exception("Actual direct retargeter not bound");
        var update=typeof(DirectHumanoidRetargeter).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
        var animator=punches.GetComponent<Animator>();
        var standing=new List<float>(); var arms=new List<float>(); var overhead=new List<float>(); var lift=new[]{new List<float>(),new List<float>()};
        var sideBend=new List<float>(); var twist=new List<float>();
        var csv=new StringBuilder("relative,sequence,leftArmUp,rightArmUp,leftThighDown,rightThighDown,leftAnkleY,rightAnkleY,torsoBend,torsoYaw,torsoRoll,rigMaxDirectionError\n");
        float maxRigError=0; int accepted=0;
        var captured=new HashSet<string>();
        Vector2[] Image(PunchCameraInput.Frame f)=>f.imageJoints.Select(p=>new Vector2(p.x/f.imageAspect,1-p.y)).ToArray();
        foreach(var r in rows.Where(r=>r.kind=="observation" && r.recordedRelative<3))
            provider.SubmitRecordedObservation(Image(r.observation),r.observation.jointScores,r.observation.imageAspect,r.observation.time);
        if(provider.Calibrated) throw new Exception("Approach frames incorrectly established standing reference");
        provider.ResetCalibration();
        foreach(var r in rows.Where(r=>r.kind=="observation" && r.recordedRelative>=10))
        {
            var f=r.observation;
            if(!provider.SubmitRecordedObservation(Image(f),f.jointScores,f.imageAspect,f.time)) continue;
            accepted++; update.Invoke(direct,null);
            if(direct.LastAppliedSequence!=provider.PoseSequence) throw new Exception("Recorded pose not applied by actual owner");
            if(animator.enabled) throw new Exception("Fresh direct tracking failed to release cached Animator skinning");
            var result=new Vector3[26]; var scores=new float[26]; solver.Predict(f.time,provider.Tuning,result,scores);
            float[] aa={Vector3.Angle(result[7]-result[5],Vector3.up),Vector3.Angle(result[8]-result[6],Vector3.up)};
            float[] thighs={Vector3.Angle(result[13]-result[11],Vector3.down),Vector3.Angle(result[14]-result[12],Vector3.down)};
            if(r.recordedRelative>=(responsive?15:13) && r.recordedRelative<16)
            { standing.AddRange(thighs); standing.Add(Vector3.Angle(result[15]-result[13],Vector3.down)); standing.Add(Vector3.Angle(result[16]-result[14],Vector3.down)); }
            if(r.recordedRelative>=(responsive?42:45) && r.recordedRelative<(responsive?43:49)) arms.AddRange(aa);
            if(responsive && r.recordedRelative>=30 && r.recordedRelative<32) overhead.AddRange(aa);
            for(int side=0;side<2;side++)
                if(r.recordedRelative>=114+side*12 && r.recordedRelative<126+side*12 && f.imageJoints[13+side].y>f.imageJoints[11+side].y)
                    lift[side].Add(result[15+side].y);
            if(r.recordedRelative>=76 && r.recordedRelative<90) sideBend.Add(solver.TorsoAngles.x);
            if(r.recordedRelative>=90 && r.recordedRelative<104) twist.Add(Mathf.Abs(solver.TorsoAngles.y));
            float error=0;
            foreach(bool left in new[]{true,false})
            {
                var from=left?PoseLandmarks.LEFT_SHOULDER:PoseLandmarks.RIGHT_SHOULDER;
                var to=left?PoseLandmarks.LEFT_ELBOW:PoseLandmarks.RIGHT_ELBOW;
                if(!provider.IsJointObserved(from)||!provider.IsJointObserved(to)) continue;
                var a=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                var b=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
                error=Mathf.Max(error,Vector3.Angle(b.position-a.position,avatar.transform.rotation*(provider.AvatarLandmark(to).Position-provider.AvatarLandmark(from).Position)));
                foreach(bool leg in new[]{false,true})
                {
                    from=leg?(left?PoseLandmarks.LEFT_HIP:PoseLandmarks.RIGHT_HIP):(left?PoseLandmarks.LEFT_ELBOW:PoseLandmarks.RIGHT_ELBOW);
                    to=leg?(left?PoseLandmarks.LEFT_KNEE:PoseLandmarks.RIGHT_KNEE):(left?PoseLandmarks.LEFT_WRIST:PoseLandmarks.RIGHT_WRIST);
                    if(!provider.IsJointObserved(from)||!provider.IsJointObserved(to)) continue;
                    a=animator.GetBoneTransform(leg?(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg):(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm));
                    b=animator.GetBoneTransform(leg?(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg):(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand));
                    error=Mathf.Max(error,Vector3.Angle(b.position-a.position,avatar.transform.rotation*(provider.AvatarLandmark(to).Position-provider.AvatarLandmark(from).Position)));
                }
            }
            maxRigError=Mathf.Max(maxRigError,error);
            csv.AppendLine(FormattableString.Invariant($"{r.recordedRelative:F3},{f.sequence},{aa[0]:F2},{aa[1]:F2},{thighs[0]:F2},{thighs[1]:F2},{result[15].y:F3},{result[16].y:F3},{solver.TorsoAngles.x:F2},{solver.TorsoAngles.y:F2},{solver.TorsoAngles.z:F2},{error:F2}"));
            string shot=responsive && r.recordedRelative>=31.4 && r.recordedRelative<31.6 ? "overhead" : r.recordedRelative>=13 && r.recordedRelative<14?"standing":r.recordedRelative>=46 && r.recordedRelative<47?"lateral-arms"
                :r.recordedRelative>=114 && r.recordedRelative<126 && f.imageJoints[13].y>f.imageJoints[11].y?"left-knee"
                :r.recordedRelative>=126 && r.recordedRelative<138 && f.imageJoints[14].y>f.imageJoints[12].y?"right-knee":null;
            if(shot!=null && captured.Add(shot))
            {
                // Skinning must see separate real player frames; a synchronous CPU
                // transform replay alone cannot validate the displayed mesh.
                int renderAfter=Time.frameCount+4;
                while(Time.frameCount<renderAfter) yield return null;
                update.Invoke(direct,null); Capture(practice,shot);
            }
        }
        File.WriteAllText("Logs/Practice/"+ReportName+".csv",csv.ToString());
        float Median(List<float> v) { v.Sort(); return v[v.Count/2]; }
        var report=new StringBuilder();
        void Check(bool condition,string message) { report.AppendLine((condition?"PASS ":"FAIL ")+message); File.WriteAllText("Logs/Practice/"+ReportName+".txt",report.ToString()); if(!condition) throw new Exception(message); }
        Check(accepted>2100,"Recorded 10 Hz inputs run through solver, publication and actual Remy retargeter");
        Check(Median(standing)<10 && standing.Max()<15,$"Standing thighs and shins recover near vertical: median {Median(standing):F1}, maximum {standing.Max():F1} degrees");
        Check(Median(arms)>55 && Median(arms)<120,$"Captured lateral arms stay near shoulder height: median angle from up {Median(arms):F1} degrees");
        if(responsive) Check(overhead.Count>20 && Median(overhead)<30,$"New phone overhead arms stay raised: median {Median(overhead):F1} degrees from up");
        for(int side=0;side<2;side++) Check(lift[side].Count>(responsive?5:30) && (responsive?lift[side].Max():Median(lift[side]))>-.35f,$"Side {side} recorded knee lift raises ankle: median pelvis-relative Y {Median(lift[side]):F3} m");
        Check(maxRigError<2,$"Final rig arms and thighs follow published targets: maximum direction error {maxRigError:F2} degrees");
        Check(twist.Max()>8,$"Shoulder twist supplies bounded yaw: peak {twist.Max():F1} degrees");
        Check(Median(sideBend)<15,$"Side bending avoids large false forward prior: median {Median(sideBend):F1} degrees");
        report.AppendLine(responsive ? "New recording replay uses the measured 22.393-degree pitch and a fresh standing calibration. No prior-recording time exclusion applies; monocular depth remains approximate." : $"Side-bend median forward prior {Median(sideBend):F1} degrees. Monocular depth remains approximate. Excluded left front-extension and video overlap were not scored.");
        File.WriteAllText("Logs/Practice/"+ReportName+".txt",report.ToString()); Debug.Log(report.ToString());
        punches.StopRecording();
        punches.SetAssistance(true); update.Invoke(direct,null);
        if(!animator.enabled) throw new Exception("Assisted mode did not restore Animator evaluation");
    }
    static void Capture(FitFightPracticeMode practice,string name)
    {
        var animator=practice.Punches.GetComponent<Animator>();
        var diag=new StringBuilder();
        foreach(HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        { if(bone==HumanBodyBones.LastBone) continue; var t=animator.GetBoneTransform(bone); if(t!=null) diag.AppendLine($"{bone} {t.name} id={t.GetEntityId()} pos={t.position} local={t.localPosition}"); }
        foreach(var skin in practice.Punches.GetComponentsInChildren<SkinnedMeshRenderer>())
        { diag.AppendLine($"SKIN {skin.name} root={skin.rootBone?.name} bounds={skin.bounds} offscreen={skin.updateWhenOffscreen}"); foreach(var bone in skin.bones) if(bone!=null && (bone.name.Contains("Leg")||bone.name.Contains("Arm"))) diag.AppendLine($"  {bone.name} id={bone.GetEntityId()} pos={bone.position}"); }
        File.WriteAllText("Logs/Practice/"+(responsive?"responsive-fix-":"recorded-fix-")+name+"-bones.txt",diag.ToString());
        var camera=practice.PracticeCamera; var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
        var target=new RenderTexture(540,960,24); var pixels=new Texture2D(540,960,TextureFormat.RGB24,false);
        try { camera.targetTexture=target; camera.Render(); RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,540,960),0,0);pixels.Apply();File.WriteAllBytes("Logs/Practice/"+(responsive?"responsive-fix-":"recorded-fix-")+name+".png",pixels.EncodeToPNG()); }
        finally { camera.targetTexture=oldTarget;RenderTexture.active=oldActive;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels); }
    }
    static void Finish(int code) { EditorApplication.update-=Tick; SessionState.SetBool(Key,false); EditorApplication.Exit(code); }
}
