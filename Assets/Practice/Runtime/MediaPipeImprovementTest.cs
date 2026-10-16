using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FitFight.BodyTracking;
using FitFight.Practice;
using UnityEngine;

// Short labeled evidence collection. Stops on engine/profile changes, pause, other recording, or exit.
[DefaultExecutionOrder(11000)]
public sealed class MediaPipeImprovementTest : MonoBehaviour
{
    public const float StepSeconds=8;
    public static readonly string[] Labels={"still_start","arms_side","arms_overhead","left_elbow","right_elbow",
        "torso_forward","torso_side","left_knee","right_knee","shallow_squat","punch_slow","punch_fast","wrist_occlusion","still_end"};
    public static readonly string[] Cues={
        "Stand still with arms relaxed for eight seconds.",
        "Raise both arms to the sides, then lower them. Repeat twice.",
        "Raise both arms overhead, then lower them. Repeat twice.",
        "Raise your left upper arm to the side. Bend and straighten that elbow twice.",
        "Raise your right upper arm to the side. Bend and straighten that elbow twice.",
        "Bend your torso forward comfortably, then stand straight. Repeat twice.",
        "Lean left, return upright, then lean right and return upright.",
        "Lift and lower your left knee twice. Keep both feet in view.",
        "Lift and lower your right knee twice. Keep both feet in view.",
        "Do two comfortable shallow squats, then stand straight.",
        "Throw two slow left and right punches, returning to guard.",
        "Throw a few quick left and right punches, returning to guard.",
        "Put your left hand behind your back briefly, then bring it back into view. Repeat once.",
        "Stand still again with arms relaxed. The test will finish shortly."};
    [Serializable] public sealed class Summary
    {
        public string kind="mediapipe_summary", schema="mp-improvement-1", engine, model, integration, runtime;
        public string profile, unity, device, appVersion, startedUtc, finishReason, recordingPath, tuning, timingNote, coordinates;
        public bool completed;
        public double recordingStartedAt, recordingEndedAt;
        public float userHeight, cameraPitch, cameraAspect;
        public int cameraWidth,cameraHeight,cameraRotation,requestedCameraFPS,droppedRecords;
        public float averageCameraFPS;
        public MediaPipeTestStats.Movement[] movements;
    }
    [Serializable] private struct CueRecord { public string kind,label,speech; public int step; public double recordedAt,elapsed; }
    [Serializable] private struct AvatarRecord
    {
        public string kind,label;
        public double time,capturedAt;
        public int sourceSequence,appliedSequence;
        public bool fresh;
        public float frameAvailableToAvatarMs;
        public Quaternion[] localRotations;
        public Vector3[] worldPositions;
    }
    public static readonly HumanBodyBones[] Bones={HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,
        HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,
        HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,
        HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,
        HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
    public static Summary Latest { get; private set; }
    public static string SummaryPath { get; private set; }
    public static string SaveError { get; private set; }
    public bool Running { get; private set; }
    public string Prompt { get; private set; }
    public string RecordingPath => recorder?.Path ?? SummaryPath;
    private RTMPosePoseProvider pose;
    private DirectHumanoidRetargeter driver;
    private Transform[] transforms;
    private PunchSessionRecorder recorder;
    private PracticeSpeech speech;
    private MediaPipeTestStats stats;
    private Summary metadata;
    private double began,movementsBegan=double.NaN;
    private int step=-1,avatarSequence=-1,cameraSamples;
    private float cameraFPS;
    private bool profile;
    private BodyTrackingSystem engine;
    private int generation;
    public void ClearPrompt() { if(!Running) Prompt=null; }
    public void Begin(RTMPosePoseProvider source,DirectHumanoidRetargeter target,Animator animator)
    {
        Stop("restarted");
        if(source==null || !source.UsesWorldLandmarks || !source.isActiveAndEnabled) { Prompt="Select MediaPipe Full first."; return; }
        pose=source;driver=target;profile=pose.Tuning.mediaPipeRegionFiltering;engine=pose.ConfiguredEngine;generation=pose.EngineGeneration;
        stats=new MediaPipeTestStats();step=avatarSequence=pendingAvatar=-1;cameraSamples=0;cameraFPS=0;movementsBegan=double.NaN;
        began=Time.realtimeSinceStartupAsDouble;
        transforms=new Transform[Bones.Length];for(int i=0;i<Bones.Length;i++) transforms[i]=animator!=null ? animator.GetBoneTransform(Bones[i]) : null;
        metadata=new Summary{kind="mediapipe_session",engine=pose.EngineLabel,model=engine==BodyTrackingSystem.MediaPipeFull ? "pose_landmarker_full" : "pose_landmarker_lite",
            integration="MediaPipeUnityPlugin 0.14.4",runtime="MediaPipe 0.10.14 (plugin release)",
            profile=profile ? (pose.Tuning.mediaPipeMotionAwareFiltering ? "region-motion-aware" : "region") : "legacy",unity=Application.unityVersion,device=SystemInfo.deviceModel,appVersion=Application.version,
            recordingStartedAt=began,startedUtc=DateTime.UtcNow.ToString("O"),tuning=JsonUtility.ToJson(pose.Tuning),userHeight=pose.userHeight,cameraPitch=pose.cameraPitchDegrees,
            requestedCameraFPS=pose.RequestedCameraFPS,
            timingNote="Unity camera-frame availability to accepted pose/first avatar application. Excludes exposure, prior camera buffering, and display latency. Cue timestamps are speech enqueue, not audible onset. No video encoded.",
            coordinates="rawImage/world/confidence: original MediaPipe33 (world y-up, meters). joints/filteredJoints: Halpe26 calibrated pelvis-relative meters. Avatar arrays: named humanoid local rotations/world positions; unavailable bones omitted as zero/identity."};
        try
        {
            SaveError=null;
            recorder=new PunchSessionRecorder(JsonUtility.ToJson(metadata),"MediaPipeSessions");metadata.recordingPath=recorder.Path;
            recorder.RecordJson(JsonUtility.ToJson(new BoneMetadata{kind="avatar_bones",bones=Array.ConvertAll(Bones,x=>x.ToString())}));
            pose.InferenceEvaluated+=OnInference;BodyTrackingSelection.Changed+=OnEngineChange;
            speech ??=new PracticeSpeech();speech.Stop();Running=true;
            Prompt="MediaPipe recorded test · stand upright and still, with head and feet visible.";
            speech.Speak("MediaPipe recorded test. Stand upright and still with your whole body visible. Wait for calibration.");
        }
        catch(Exception e) { SaveError=e.Message;Stop("recording_error");Prompt="Could not start recording: "+e.Message; }
    }
    [Serializable] private struct BoneMetadata { public string kind;public string[] bones; }
    private void OnEngineChange() => Stop("engine_changed");
    private void OnInference()
    {
        if(!Running) return;
        try
        {
            var d=pose.CaptureDiagnostics();d.movementLabel=step>=0 ? Labels[step] : "calibration";recorder.Diagnostic(d);stats.Add(d);
            if(step>=0) { cameraFPS+=d.cameraFPS;cameraSamples++; }
            if(d.accepted && step>=0) pendingAvatar=d.sequence;
        }
        catch(Exception e) { SaveError=e.Message;Stop("diagnostic_error"); }
    }
    private int pendingAvatar=-1;
    private void Update()
    {
        if(!Running) return;
        try
        {
            speech.CheckStatus();double now=Time.realtimeSinceStartupAsDouble;
            if(pose==null || !pose.isActiveAndEnabled || pose.EngineGeneration!=generation || pose.ConfiguredEngine!=engine) { Stop("engine_unavailable");return; }
            if(pose.Tuning.mediaPipeRegionFiltering!=profile || JsonUtility.ToJson(pose.Tuning)!=metadata.tuning
                || !Mathf.Approximately(pose.userHeight,metadata.userHeight) || !Mathf.Approximately(pose.cameraPitchDegrees,metadata.cameraPitch))
                { Stop("settings_changed");return; }
            if(step>=0 && !pose.Calibrated) { Stop("calibration_reset");return; }
            if(recorder.Error!=null) { SaveError=recorder.Error;Stop("recording_error");return; }
            if(double.IsNaN(movementsBegan))
            {
                Prompt="CALIBRATING · "+pose.CalibrationFrames+"/20 · "+pose.StandingCalibrationHint;
                if(!pose.Calibrated) { if(now-began>60) Stop("calibration_timeout"); return; }
                movementsBegan=now;
            }
            double elapsed=now-movementsBegan;
            if(elapsed>=Labels.Length*StepSeconds) { Stop("completed",true);return; }
            int next=(int)(elapsed/StepSeconds);
            if(next==step) return;
            step=next;stats.StartMovement(Labels[step],now);
            Prompt=$"MP TEST {step+1}/{Labels.Length} · {Labels[step]}\n{Cues[step]}";
            recorder.RecordJson(JsonUtility.ToJson(new CueRecord{kind="guide_cue",label=Labels[step],speech=Cues[step],step=step,recordedAt=now,elapsed=elapsed}));
            speech.Speak(Cues[step]);
        }
        catch(Exception e) { SaveError=e.Message;Stop("test_error"); }
    }
    private void LateUpdate()
    {
        if(!Running || step<0 || pendingAvatar<0 || pendingAvatar==avatarSequence || driver==null
            || !driver.HasFreshPose || driver.LastAppliedSequence!=pendingAvatar) return;
        float delay=driver.FrameAvailableToAvatarMs;
        stats.Avatar(delay);avatarSequence=pendingAvatar;
        var rotation=new Quaternion[transforms.Length];var positions=new Vector3[transforms.Length];
        for(int i=0;i<transforms.Length;i++) if(transforms[i]!=null) { rotation[i]=transforms[i].localRotation;positions[i]=transforms[i].position; }
        recorder.RecordJson(JsonUtility.ToJson(new AvatarRecord{kind="mediapipe_avatar",label=Labels[step],time=Time.realtimeSinceStartupAsDouble,
            capturedAt=pose.AcceptedCaptureTime,sourceSequence=pose.PoseSequence,appliedSequence=driver.LastAppliedSequence,fresh=driver.HasFreshPose,
            frameAvailableToAvatarMs=delay,localRotations=rotation,worldPositions=positions}));
    }
    public void Stop(string reason="stopped",bool completed=false)
    {
        if(!Running) return;
        Running=false;BodyTrackingSelection.Changed-=OnEngineChange;
        if(pose!=null) pose.InferenceEvaluated-=OnInference;
        speech?.Stop();
        metadata.kind="mediapipe_summary";
        metadata.recordingEndedAt=Time.realtimeSinceStartupAsDouble;
        metadata.completed=completed;metadata.finishReason=reason;metadata.movements=stats.Finish(Time.realtimeSinceStartupAsDouble);
        metadata.droppedRecords=recorder.Dropped;
        metadata.cameraAspect=pose!=null ? pose.CameraAspect : 0;metadata.cameraRotation=pose!=null ? pose.CameraRotation : 0;
        var texture=pose!=null ? pose.CameraTexture : null;
        metadata.cameraWidth=texture!=null ? texture.width : 0;metadata.cameraHeight=texture!=null ? texture.height : 0;
        metadata.averageCameraFPS=cameraSamples>0 ? cameraFPS/cameraSamples : 0;
        Latest=metadata;SummaryPath=Path.ChangeExtension(recorder.Path,"summary.json");
        string json=JsonUtility.ToJson(metadata,true),markdown=Report(metadata);
        recorder.RecordJson(JsonUtility.ToJson(metadata));recorder.Dispose();recorder=null;
        string path=SummaryPath;
        Task.Run(()=>{try {File.WriteAllText(path,json);File.WriteAllText(Path.ChangeExtension(path,"md"),markdown);}catch(Exception e){SaveError=e.Message;}});
        PlayerPrefs.SetString("FitFight.MediaPipe.LastSummary",path);PlayerPrefs.Save();
        Prompt=completed ? "MediaPipe test complete. Open Tracking → Show MediaPipe results." : "MediaPipe test stopped: "+reason;
        if(completed) speech?.Speak("Test complete. Results saved. Open Tracking to see MediaPipe results.");
        else if(reason=="calibration_timeout") speech?.Speak("Calibration could not finish. Check that your whole body is visible, then repeat the test.");
    }
    public static void LoadLatest()
    {
        if(Latest!=null) return;
        SummaryPath=PlayerPrefs.GetString("FitFight.MediaPipe.LastSummary","");
        try { if(File.Exists(SummaryPath)) Latest=JsonUtility.FromJson<Summary>(File.ReadAllText(SummaryPath)); }
        catch(Exception e) { SaveError=e.Message; }
    }
    public static string Report(Summary s)
    {
        var b=new StringBuilder();
        b.AppendLine("# MediaPipe improvement test");b.AppendLine();b.AppendLine($"{s.engine} · {s.profile} · {s.startedUtc} · {s.finishReason}");
        b.AppendLine($"Device: {s.device}; app {s.appVersion}; camera {s.cameraWidth}×{s.cameraHeight}, average {s.averageCameraFPS:F1} FPS.");
        b.AppendLine();b.AppendLine("Availability uses all evaluated movement frames, including no person/stale results. It is not pose accuracy. Calibration excluded. Zero timing with zero samples means unavailable.");
        b.AppendLine();b.AppendLine("| Movement | Frames accepted/evaluated | Accepted FPS | Pose median/p95 ms | Avatar median/p95 ms (n) | Wrist L/R % | Knee L/R % | Still raw/filtered RMS cm (n) |");
        b.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");
        foreach(var m in s.movements) b.AppendLine(FormattableString.Invariant($"| {m.label} | {m.accepted}/{m.evaluated} | {m.acceptedFPS:F1} | {m.medianPoseMs:F1}/{m.p95PoseMs:F1} | {m.medianAvatarMs:F1}/{m.p95AvatarMs:F1} ({m.avatarSamples}) | {m.leftWristAvailablePercent:F0}/{m.rightWristAvailablePercent:F0} | {m.leftKneeAvailablePercent:F0}/{m.rightKneeAvailablePercent:F0} | {m.rawStillnessRmsCm:F2}/{m.filteredStillnessRmsCm:F2} ({m.stillnessSamples}) |"));
        b.AppendLine();b.AppendLine(s.timingNote);b.AppendLine(s.coordinates);
        b.AppendLine($"Dropped JSONL records: {s.droppedRecords}. Raw stream: {s.recordingPath}");return b.ToString();
    }
    private void OnApplicationPause(bool paused) { if(paused) Stop("paused"); }
    private void OnDisable() { Stop("exit"); }
    private void OnDestroy() { Stop("exit");speech?.Dispose(); }
}
