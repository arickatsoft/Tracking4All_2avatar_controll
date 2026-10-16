using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FitFight.BodyTracking;
using FitFight.Practice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Diagnostic experiment: reinitialize scale on two different portions of the same recording.
// Neither experiment can reproduce the phone's unknown pre-session calibration state.
[InitializeOnLoad]
public static class RecordedBodyReview
{
    private const string Key="FitFight.RecordedBodyReview";
    private const string Input="/Users/jotit/Desktop/full body.jsonl";
    private const string Output="Docs/VideoReview/20260930-full-body-and-punch";
    private static int step;
    private static double deadline;
    [Serializable] private class Row
    {
        public string kind,tuning;
        public string[] bones;
        public double recordingStartedAt,recordedAt,time;
        public long observationSequence;
        public float userHeight,cameraPitchDegrees;
        public PunchCameraInput.Frame observation;
        public Quaternion[] localRotations;
    }
    static RecordedBodyReview()
    {
        EditorApplication.playModeStateChanged+=s=> {
            if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            { step=0;deadline=EditorApplication.timeSinceStartup+60;EditorApplication.update+=Tick; }
        };
    }
    public static void RunBatch()
    {
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Tracking4All/Scenes/Pose Avatar Tracking/AvatarTracking_new.unity");
        EditorApplication.isPlaying=true;
    }
    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Recorded body review timed out");
            if(step==0)
            {
                var owner=UnityEngine.Object.FindFirstObjectByType<PlayerSelectionManager>();
                if(owner==null) return; owner.StartPractice();step=1;return;
            }
            var practice=UnityEngine.Object.FindFirstObjectByType<FitFightPracticeMode>();
            if(practice?.Ready!=true) return;
            Review(practice);Finish(0);
        }
        catch(Exception e) { Debug.LogException(e);Finish(1); }
    }
    private static Vector2[] Image(PunchCameraInput.Frame f)
        => f.imageJoints.Select(p=>new Vector2(p.x/f.imageAspect,1-p.y)).ToArray();
    private static void Review(FitFightPracticeMode practice)
    {
        var all=File.ReadLines(Input).Select(s=>JsonUtility.FromJson<Row>(s)).ToList();
        var meta=all[0];var observations=all.Where(r=>r.kind=="observation").ToList();var poses=all.Where(r=>r.kind=="pose").ToList();
        var csv=new StringBuilder("experiment,relative,sequence,calibrated,leftUpperArmUpAngle,leftForearmUpAngle,rightUpperArmUpAngle,rightForearmUpAngle,leftThighDownAngle,rightThighDownAngle,leftAnkleY,rightAnkleY,torsoBend\n");
        foreach(bool neutral in new[]{false,true})
        {
            var solver=new Skeleton25DSolver();var result=new Vector3[26];var confidence=new float[26];
            var tuning=JsonUtility.FromJson<RTMPoseTuning>(meta.tuning);tuning.prediction=0;tuning.observationInterval=.1f;
            var seed=observations.Where(r=> {double t=r.recordedAt-meta.recordingStartedAt;return neutral ? t>=10&&t<13 : t<3;}).ToList();
            // Each condition gets exactly 25 accepted observations, preserving recorded timestamps.
            foreach(var row in seed.Take(25)) solver.Solve(Image(row.observation),row.observation.jointScores,row.observation.imageAspect,row.observation.time,meta.userHeight,meta.cameraPitchDegrees,0,result,confidence,tuning);
            foreach(var row in observations.Where(r=>r.recordedAt-meta.recordingStartedAt>=13))
            {
                var f=row.observation;
                if(!solver.Solve(Image(f),f.jointScores,f.imageAspect,f.time,meta.userHeight,meta.cameraPitchDegrees,0,result,confidence,tuning)) continue;
                csv.AppendLine(FormattableString.Invariant($"{(neutral?"neutral_10s_seed":"approach_0s_seed")},{row.recordedAt-meta.recordingStartedAt:F3},{f.sequence},{solver.Calibrated},{Vector3.Angle(result[7]-result[5],Vector3.up):F2},{Vector3.Angle(result[9]-result[7],Vector3.up):F2},{Vector3.Angle(result[8]-result[6],Vector3.up):F2},{Vector3.Angle(result[10]-result[8],Vector3.up):F2},{Vector3.Angle(result[13]-result[11],Vector3.down):F2},{Vector3.Angle(result[14]-result[12],Vector3.down):F2},{result[15].y:F3},{result[16].y:F3},{solver.TorsoAngles.x:F2}"));
            }
        }
        File.WriteAllText(Output+"/calibration-experiment.csv",csv.ToString());
        var animator=practice.Punches.GetComponent<Animator>();practice.Punches.EditorPreview=true;
        var transforms=meta.bones.Select(name=>animator.GetBoneTransform((HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),name))).ToArray();
        var fk=new StringBuilder("relative,sequence,leftUpperArmUpAngle,leftForearmUpAngle,rightUpperArmUpAngle,rightForearmUpAngle,leftThighDownAngle,rightThighDownAngle\n");
        Transform B(HumanBodyBones bone)=>animator.GetBoneTransform(bone);
        float Arm(bool left,bool lower)=>Vector3.Angle(B(left?(lower?HumanBodyBones.LeftHand:HumanBodyBones.LeftLowerArm):(lower?HumanBodyBones.RightHand:HumanBodyBones.RightLowerArm)).position-B(left?(lower?HumanBodyBones.LeftLowerArm:HumanBodyBones.LeftUpperArm):(lower?HumanBodyBones.RightLowerArm:HumanBodyBones.RightUpperArm)).position,Vector3.up);
        foreach(var row in poses.Where(r=>r.time-meta.recordingStartedAt>=10))
        {
            for(int i=0;i<transforms.Length;i++) transforms[i].localRotation=row.localRotations[i];
            float lt=Vector3.Angle(B(HumanBodyBones.LeftLowerLeg).position-B(HumanBodyBones.LeftUpperLeg).position,Vector3.down);
            float rt=Vector3.Angle(B(HumanBodyBones.RightLowerLeg).position-B(HumanBodyBones.RightUpperLeg).position,Vector3.down);
            fk.AppendLine(FormattableString.Invariant($"{row.time-meta.recordingStartedAt:F3},{row.observationSequence},{Arm(true,false):F2},{Arm(true,true):F2},{Arm(false,false):F2},{Arm(false,true):F2},{lt:F2},{rt:F2}"));
        }
        File.WriteAllText(Output+"/recorded-rig-directions.csv",fk.ToString());
        Debug.Log("Recorded body calibration experiment and rig direction review completed. Not a pre-session calibration replay.");
    }
    public static void ReviewPunchBatch()
    {
        try
        {
            var rows=File.ReadLines("/Users/jotit/Desktop/punches.jsonl").Select(s=>JsonUtility.FromJson<Row>(s)).ToList();
            var meta=rows[0];var gate=new PunchGestureGate();var detectors=new[]{new PunchIntent(),new PunchIntent()};
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var originField=typeof(PunchIntent).GetField("origin",flags);var sweepField=typeof(PunchIntent).GetField("sweeping",flags);
            var report=new StringBuilder("sequence,relative,side,signal,state,reason,sweeping,originBend,currentBend,originReach,currentReach,travel,speed\n");
            foreach(var row in rows.Where(r=>r.kind=="observation"))
            {
                var f=row.observation;gate.Observe(f);
                for(int side=0;side<2;side++)
                {
                    var a=side==0?f.left:f.right;var detector=detectors[side];
                    var signal=detector.Observe(a,f.sequence,f.time,gate.Blocked?gate.Reason:null);
                    var origin=(PunchArmObservation)originField.GetValue(detector);bool sweeping=(bool)sweepField.GetValue(detector);
                    report.AppendLine(FormattableString.Invariant($"{f.sequence},{row.recordedAt-meta.recordingStartedAt:F3},{side},{signal},{detector.Phase},{detector.Reason},{sweeping},{origin.Bend:F2},{a.Bend:F2},{origin.wrist.magnitude:F3},{a.wrist.magnitude:F3},{detector.Travel:F3},{detector.Speed:F3}"));
                }
            }
            File.WriteAllText(Output+"/punch-detector-review.csv",report.ToString());EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
    private static void Finish(int code)
    {
        EditorApplication.update-=Tick;SessionState.SetBool(Key,false);EditorApplication.Exit(code);
    }
}
