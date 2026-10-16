using System;
using System.Collections.Generic;
using FitFight.BodyTracking;
using Tracking4All;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace FitFight.Practice
{
    /// <summary>One final full-body owner in punch practice. Inference stays live behind it.</summary>
    [DefaultExecutionOrder(9200)]
    public sealed class AssistedPunchController : MonoBehaviour
    {
        public enum Motion { Guard, Preparing, Striking, Recovering }
        public Motion State { get; private set; }
        public bool Active => useAssistance && graph.IsValid();
        public bool Available => graph.IsValid();
        public bool Recording => recorder != null;
        public bool FullBodyDiagnostic => Recording && recordingFullBody;
        public bool GameBodyRecording => Recording && recordingGameBody;
        public readonly GameBodyMotion BodyMotion = new GameBodyMotion();
        private readonly GameBodyRecordingGuide gameBodyGuide=new GameBodyRecordingGuide();
        private GameBodySessionStats gameBodyStats;
        private bool recordingGameBody;
        private long lastBodySequence=-1, legTargetAction=-1;
        private Vector3 legStrikeTarget, legStrikeStart;
        private Vector3 legKneeStart;
        private float trackedSquat;
        private float squatVelocity;
        private readonly Vector3[] footTargets=new Vector3[2];
        private readonly Vector3[] footVelocities=new Vector3[2],legPoles=new Vector3[2],legPoleVelocities=new Vector3[2];
        private readonly Vector3[] renderedFeet=new Vector3[2],renderedKnees=new Vector3[2];
        private readonly Vector3[] armTargets=new Vector3[2];
        private readonly Vector3[] armVelocities=new Vector3[2],armPoles=new Vector3[2],armPoleVelocities=new Vector3[2];
        private readonly float[] armBlend=new float[2],armBlendVelocity=new float[2];
        private readonly bool[] haveArmTargets=new bool[2];
        public string RecordingPath { get; private set; }
        public string Error { get; private set; }
        public float Phase { get; private set; }
        public long ActionId { get; private set; }
        public int Side { get; private set; }
        public bool CanHit => Active && State == Motion.Striking && Phase >= .18f && Phase < .68f && Fresh && Visible(Side == 0 ? lastFrame.left : lastFrame.right);
        private bool Visible(PunchArmObservation arm) => lastFrame.boxing ? BoxingFeatures.HandVisible(arm) : arm.Valid;
        public bool Fresh => Time.realtimeSinceStartupAsDouble - lastReceived <= .23 && latestValid;
        public string Status
        {
            get
            {
                if (!Available) return Error;
                if (!Active) return "BODY MIRRORING";
                if (BodyMotion.State != GameBodyMotion.LegMotion.Grounded) return "BODY ASSIST · " + BodyMotion.Reason;
                if (lastFrame.boxing && Fresh)
                {
                    if (State != Motion.Guard) return "BOXING ASSIST · " + State.ToString().ToUpperInvariant();
                    return boxingIntent[0].GuardLearned && boxingIntent[1].GuardLearned && !boxingIntent[0].RecoveringTracking && !boxingIntent[1].RecoveringTracking ? "READY · PUNCH AND RETURN TO GUARD"
                        : "HOLD GUARD · KNEES SOFT · FISTS NEAR CHIN";
                }
                if (!Fresh) return "SHOW HANDS, ELBOWS AND HIPS";
                if (gestureGate.Blocked) return "TRACKING BOTH ARMS · PUNCH WITH ONE HAND";
                if (State != Motion.Guard) return "PUNCH ASSIST · " + State.ToString().ToUpperInvariant();
                bool leftReady = intent[0].Phase == PunchIntent.State.Guard && lastFrame.left.InGuard;
                bool rightReady = intent[1].Phase == PunchIntent.State.Guard && lastFrame.right.InGuard;
                if (leftReady && rightReady) return "READY · PUNCH FROM YOUR NATURAL STANCE";
                if (leftReady) return "LEFT READY · SHOW RIGHT HAND AND ELBOW";
                if (rightReady) return "RIGHT READY · SHOW LEFT HAND AND ELBOW";
                return "TRACKING ARMS · HOLD BRIEFLY TO START";
            }
        }
        public string RecordingPrompt
        {
            get
            {
                if (!Recording) return "";
                float elapsed = (float)(Time.realtimeSinceStartupAsDouble - recordingStarted);
                string prompt = recordingGameBody ? GameBodyRecordingGuide.Label(elapsed-calibrationPause)
                    : recordingFullBody ? FullBodyRecordingGuide.Label(elapsed - calibrationPause) : PunchRecordingGuide.Label(elapsed);
                if ((recordingFullBody || recordingGameBody) && standingRequested && poseSource?.ActiveRTMPose != null && !poseSource.ActiveRTMPose.Calibrated)
                    prompt = "HOLD STILL · " + poseSource.ActiveRTMPose.PipelineStatus;
                string voiceNotice = speech?.Error != null ? "\nVOICE UNAVAILABLE · CHECK PHONE TTS" : "";
                return $"REC {elapsed:F1}s · FRAME {lastSequence}\n{prompt}{voiceNotice}";
            }
        }
        private Tracking4All.Avatar avatar;
        private Animator animator;
        private PracticeHeavyBag bag;
        private FitFightPoseReference art;
        private PunchCameraInput cameraInput;
        private readonly PunchGestureGate gestureGate = new PunchGestureGate();
        private readonly PunchIntent[] intent = { new PunchIntent(), new PunchIntent() };
        private PunchCameraInput.Frame lastFrame;
        private readonly BoxingPunchIntent[] boxingIntent = { new BoxingPunchIntent(), new BoxingPunchIntent() };
        private readonly BoxingGestureGate boxingGate=new BoxingGestureGate();
        private BoxingSessionStats boxingStats;
        private RTMPosePoseProvider recordingPose;
        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;
        private AnimationPlayableOutput animationOutput;
        private AnimationClipPlayable guard, leftClip, rightClip;
        private bool useAssistance, previousRootMotion;
        private AnimatorCullingMode previousCulling;
        private double lastReceived = double.NegativeInfinity, motionStarted, recordingStarted;
        private long lastSequence = -1;
        private bool latestValid;
        private float weight, duration = .55f, previousWeight, frozenPhase;
        private int queuedSide = -1;
        private float queuedSpeed;
        private int outgoingSide = -1;
        private float outgoingPhase, outgoingWeight;
        private double crossStarted;
        private double queuedAt;
        private Vector3 strikeTarget, strikeStart;
        private readonly Vector3[] assistedFistTargets=new Vector3[2];
        private Vector3 outgoingFistStart;
        private readonly Quaternion[] facingCorrections = new Quaternion[3];
        private readonly Vector3[] plantedFeet = new Vector3[2];
        private readonly Quaternion[] plantedFootRotations = new Quaternion[2];
        private Vector3 guardHip;
        private Transform hips, leftHand, rightHand, spine, chest, neck, head;
        private Quaternion bodyOffset = Quaternion.identity, headOffset = Quaternion.identity;
        private Vector3 bodyAngularVelocity,headAngularVelocity;
        private Transform[] trackedBones;
        private string[] boneNames;
        private HumanBodyBones[] boneIds;
        private Quaternion[] entryRotations;
        private double entryAt;
        private PunchSessionRecorder recorder;
        private PracticeSpeech speech;
        private readonly PunchRecordingGuide recordingGuide = new PunchRecordingGuide();
        private double nextSpeechStatus;
        private readonly FullBodyRecordingGuide fullBodyGuide = new FullBodyRecordingGuide();
        private bool recordingFullBody, restoreRecordingMode, savedAssistance, savedDirect;
        private bool standingRequested;
        private int diagnosticSequence = -1, diagnosticCalibration = -1;
        private double lastGuideUpdate;
        private double calibrationPause;
        private double nextDiagnostic;
        private double nextCalibrationPrompt;
        private BodyTrackingController recordingController;
        private PoseSolution poseSource;
        private bool bound;
        private SkinnedMeshRenderer[] skins;
        private bool[] previousSkinUpdates;

        [Serializable] private struct Metadata
        {
            public string kind, version, unity, device, rig, engine, timestampNote, test, imageCoordinates;
            public int schemaVersion;
            public double recordingStartedAt;
            public float userHeight, cameraPitchDegrees;
            public bool calibrated, assisted;
            public string[] bones;
            public string tuning;
        }
        public void Bind(Tracking4All.Avatar target, PracticeHeavyBag targetBag)
        {
            avatar=target; bag=targetBag; animator=target.GetComponent<Animator>();
            art=Resources.Load<FitFightPoseReference>("FitFightPoseReference");
            if (animator == null || !animator.isHuman || art == null || art.guard == null || art.leftPunch == null || art.rightPunch == null)
            { Error="Punch clips or humanoid rig missing"; return; }
            if (!art.guard.isHumanMotion || !art.leftPunch.isHumanMotion || !art.rightPunch.isHumanMotion)
            { Error="Punch clips must be imported as Humanoid"; return; }
            poseSource=target.PosePuppet?.PoseSource?.GetComponent<PoseSolution>();
            cameraInput=new PunchCameraInput(poseSource);
            skins=target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            previousSkinUpdates=new bool[skins.Length];
            for(int i=0;i<skins.Length;i++) { previousSkinUpdates[i]=skins[i].updateWhenOffscreen; skins[i].updateWhenOffscreen=true; }
            previousRootMotion=animator.applyRootMotion; previousCulling=animator.cullingMode;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var bones=new List<Transform>(); var names=new List<string>();
            for (int i=0;i<(int)HumanBodyBones.LastBone;i++)
            {
                var bone=animator.GetBoneTransform((HumanBodyBones)i);
                if (bone != null) { bones.Add(bone); names.Add(((HumanBodyBones)i).ToString()); }
            }
            trackedBones=bones.ToArray(); boneNames=names.ToArray(); entryRotations=new Quaternion[bones.Count];
            boneIds=new HumanBodyBones[boneNames.Length];
            for(int i=0;i<boneIds.Length;i++) boneIds[i]=(HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),boneNames[i]);
            hips=animator.GetBoneTransform(HumanBodyBones.Hips);
            spine=animator.GetBoneTransform(HumanBodyBones.Spine);
            chest=animator.GetBoneTransform(HumanBodyBones.Chest);
            neck=animator.GetBoneTransform(HumanBodyBones.Neck);
            head=animator.GetBoneTransform(HumanBodyBones.Head);
            leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand); rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            graph=PlayableGraph.Create("FitFight assisted punches"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            guard=AnimationClipPlayable.Create(graph,art.guard);
            leftClip=AnimationClipPlayable.Create(graph,art.leftPunch); rightClip=AnimationClipPlayable.Create(graph,art.rightPunch);
            guard.SetApplyFootIK(false); leftClip.SetApplyFootIK(false); rightClip.SetApplyFootIK(false);
            mixer=AnimationMixerPlayable.Create(graph,3);
            graph.Connect(guard,0,mixer,0); graph.Connect(leftClip,0,mixer,1); graph.Connect(rightClip,0,mixer,2);
            animationOutput=AnimationPlayableOutput.Create(graph,"Punch practice",animator);
            animationOutput.SetSourcePlayable(mixer);
            graph.Play();
            // Compute imported guard facing once using the same rig sign as the existing guard.
            var skeleton=target.GetComponent<AvatarSkeleton>();
            Vector3 across=skeleton.GetBindingPosition(HumanBodyBones.RightUpperArm)-skeleton.GetBindingPosition(HumanBodyBones.LeftUpperArm);
            float facingSign=Vector3.Dot(Vector3.Cross(across,Vector3.up),skeleton.BindingRootRotation*Vector3.forward)<0?-1:1;
            Vector3 rootPosition=transform.position; Quaternion rootRotation=transform.rotation;
            for (int i=0;i<trackedBones.Length;i++) entryRotations[i]=trackedBones[i].localRotation;
            for(int clipIndex=0;clipIndex<3;clipIndex++)
            {
                for(int input=0;input<3;input++) mixer.SetInputWeight(input,input==clipIndex?1:0);
                guard.SetTime(0);leftClip.SetTime(0);rightClip.SetTime(0);
                graph.Evaluate(0);transform.SetPositionAndRotation(rootPosition,rootRotation);
                Vector3 forward=Vector3.Cross(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position-animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position,Vector3.up)*facingSign;
                facingCorrections[clipIndex]=Quaternion.AngleAxis(Vector3.SignedAngle(forward,rootRotation*Vector3.forward,Vector3.up),Vector3.up);
            }
            mixer.SetInputWeight(0,1);mixer.SetInputWeight(1,0);mixer.SetInputWeight(2,0);
            graph.Evaluate(0);transform.SetPositionAndRotation(rootPosition,rootRotation);
            hips.rotation=facingCorrections[0]*hips.rotation;
            guardHip=transform.InverseTransformPoint(hips.position);
            for(int side=0;side<2;side++)
            {
                var foot=animator.GetBoneTransform(side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                plantedFeet[side]=transform.InverseTransformPoint(foot.position);
                plantedFootRotations[side]=Quaternion.Inverse(rootRotation)*foot.rotation;
            }
            transform.SetPositionAndRotation(rootPosition,rootRotation);
            for (int i=0;i<trackedBones.Length;i++) trackedBones[i].localRotation=entryRotations[i];
            speech=new PracticeSpeech(); // Warm up the phone engine before RECORD TEST is tapped.
            BodyMotion.Decision+=BodyDecision;
            bound=true; SetAssistance(true);
        }
        public void SetAssistance(bool value)
        {
            if (!Available) return;
            if (recorder != null && value != Active) StopRecording(true);
            ResetMotion(); useAssistance=value;
            animationOutput.SetWeight(value ? 1 : 0);
            if (value)
            {
                animator.enabled=true;
                for (int i=0;i<trackedBones.Length;i++) entryRotations[i]=trackedBones[i].localRotation;
                entryAt=Time.realtimeSinceStartupAsDouble;
                avatar.PoseOverride=this; animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                for(int i=0;i<skins.Length;i++) skins[i].updateWhenOffscreen=true;
            }
            else
            {
                if (avatar.PoseOverride==this) avatar.PoseOverride=null;
                animator.applyRootMotion=previousRootMotion; animator.cullingMode=previousCulling;
                RestoreSkinUpdates();
            }
            bag.ResetContacts();
        }
        public void ResetMotion()
        {
            foreach(var detector in intent) detector.Reset();
            foreach(var detector in boxingIntent) detector.Reset();
            boxingGate.Reset();
            gestureGate.Reset();
            bodyOffset = headOffset = Quaternion.identity;
            bodyAngularVelocity=headAngularVelocity=Vector3.zero;squatVelocity=0;
            BodyMotion.Reset();lastBodySequence=-1;trackedSquat=0;legTargetAction=-1;
            Array.Clear(haveArmTargets,0,haveArmTargets.Length);
            Array.Clear(footTargets,0,footTargets.Length);
            Array.Clear(footVelocities,0,2);Array.Clear(legPoles,0,2);Array.Clear(legPoleVelocities,0,2);
            Array.Clear(renderedFeet,0,2);Array.Clear(renderedKnees,0,2);
            Array.Clear(armVelocities,0,2);Array.Clear(armPoles,0,2);Array.Clear(armPoleVelocities,0,2);
            Array.Clear(armBlend,0,2);Array.Clear(armBlendVelocity,0,2);
            Array.Clear(assistedFistTargets,0,2);
            State=Motion.Guard; Phase=weight=0; queuedSide=outgoingSide=-1;
            lastReceived=double.NegativeInfinity; latestValid=false;
        }
        #if UNITY_EDITOR
        public bool EditorPreview { get; set; }
        public bool EditorInput { get; set; }
        private bool editorFrameReady;
        private PunchCameraInput.Frame editorFrame;
        public void SubmitEditorFrame(PunchCameraInput.Frame frame) { editorFrame=frame; editorFrameReady=true; }
        #endif
        private void LateUpdate()
        {
            #if UNITY_EDITOR
            if(EditorPreview) return;
            #endif
            if (!bound || (!Active && recorder == null)) return;
            double now=Time.realtimeSinceStartupAsDouble;
            PunchCameraInput.Frame frame;
            bool received, changed;
            #if UNITY_EDITOR
            if(EditorInput) { frame=editorFrame;received=editorFrameReady;editorFrameReady=false;changed=false; }
            else
            #endif
                received=cameraInput.TryRead(out frame,out changed);
            if (changed) ResetMotion();
            if (Active) ObserveBody(now);
            if (received)
            {
                lastFrame=frame; lastSequence=frame.sequence; lastReceived=frame.received;
                latestValid=Visible(frame.left) || Visible(frame.right);
                if (Active)
                {
                    if(frame.boxing) boxingGate.Observe(frame);else gestureGate.Observe(frame);
                    bool blocked=(frame.boxing ? boxingGate.Blocked : gestureGate.Blocked)
                        || BodyMotion.Fresh(now) && BodyMotion.ClearArmGesture;
                    if (blocked)
                    {
                        queuedSide = -1;
                        if (State == Motion.Preparing) Recover(now);
                    }
                    for (int side=0;side<2;side++)
                    {
                        var arm=side==0?frame.left:frame.right;
                        var signal=frame.boxing ? boxingIntent[side].Observe(arm,frame.sequence,frame.time,blocked)
                            : intent[side].Observe(arm,frame.sequence,frame.time,blocked ? gestureGate.Reason : null);
                        float speed=frame.boxing ? boxingIntent[side].Speed : intent[side].Speed;
                        boxingStats?.Signal(side,signal,frame.time,now);
                        gameBodyStats?.Punch(side,signal,frame.time,now);
                        Handle(signal,side,speed,now);
                    }
                }
                boxingStats?.Observation(frame,now);
                recorder?.Observation(frame,Active ? intent[0] : null,Active ? intent[1] : null,
                    Active && frame.boxing ? boxingIntent[0] : null,Active && frame.boxing ? boxingIntent[1] : null);
            }
            if (!Active) { UpdateRecording(now); return; }
            if (!Fresh)
            {
                foreach(var detector in intent) detector.Reset();
                foreach(var detector in boxingIntent) detector.Reset();
                boxingGate.Reset();
                queuedSide=-1;
                if (State==Motion.Preparing || State==Motion.Striking) Recover(now);
            }
            if (State==Motion.Preparing && now-motionStarted>.24) Recover(now);
            if (State==Motion.Preparing)
            { Phase=Mathf.Min(.12f,(float)(now-motionStarted)*.6f); weight=Mathf.MoveTowards(weight,.28f,Time.unscaledDeltaTime*6); }
            else if (State==Motion.Striking)
            {
                Phase=Mathf.Clamp01(.12f+(float)(now-motionStarted)/duration);
                float envelope=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,1,Phase));
                weight=Mathf.MoveTowards(weight,envelope,Time.unscaledDeltaTime*14);
                if (Phase>=1) { State=Motion.Guard; weight=0; }
            }
            else if (State==Motion.Recovering)
            {
                float t=Mathf.Clamp01((float)(now-motionStarted)/.16f);
                Phase=frozenPhase; weight=previousWeight*(1-Mathf.SmoothStep(0,1,t));
                if (t>=1) { State=Motion.Guard; weight=0; }
            }
            if (queuedSide>=0 && (State==Motion.Guard || (State==Motion.Striking && Phase>.5f)))
            {
                int side=queuedSide; queuedSide=-1;
                if (Fresh && now-queuedAt<=.18) { Begin(side,now); Commit(queuedSpeed,now); }
            }
            BodyMotion.Tick(now);
            Compose(now);
            UpdateRecording(now);
        }
        private void UpdateRecording(double now)
        {
            if (recorder == null) return;
            if (now >= nextSpeechStatus) { speech?.CheckStatus(); nextSpeechStatus = now + .5; }
            double elapsed = now - recordingStarted - calibrationPause;
            if ((recordingFullBody || recordingGameBody) && standingRequested && poseSource?.ActiveRTMPose != null
                && !poseSource.ActiveRTMPose.Calibrated && elapsed >= 15 && elapsed < 16)
            {
                if(recordingGameBody && calibrationPause>=GameBodyRecordingGuide.MaxCalibrationPause)
                { Error="Calibration did not finish: show whole body and stand straight";StopRecording(true);return; }
                calibrationPause += now - lastGuideUpdate;
                elapsed = now - recordingStarted - calibrationPause;
                if(now>=nextCalibrationPrompt)
                {
                    string hint=poseSource.ActiveRTMPose.StandingCalibrationHint;
                    var calibrationCue=new PunchRecordingGuide.Cue(elapsed,"CALIBRATION · HOLD STILL",hint);
                    recorder.Cue("calibration",calibrationCue,recordingStarted+calibrationPause,now);
                    speech?.Speak(hint); nextCalibrationPrompt=now+6;
                }
            }
            lastGuideUpdate = now;
            double duration = recordingGameBody ? GameBodyRecordingGuide.Duration : recordingFullBody ? FullBodyRecordingGuide.Duration : PunchRecordingGuide.Duration;
            if (recorder.Error != null) StopRecording(true);
            else if (elapsed >= duration) StopRecording(true, true);
            else if (TryGuide(elapsed, out var cue)) PublishCue(cue, now);
        }
        private bool TryGuide(double elapsed, out PunchRecordingGuide.Cue cue)
            => recordingGameBody ? gameBodyGuide.TryAdvance(elapsed,out cue)
                : recordingFullBody ? fullBodyGuide.TryAdvance(elapsed, out cue) : recordingGuide.TryAdvance(elapsed, out cue);
        private void PublishCue(PunchRecordingGuide.Cue cue, double now)
        {
            boxingStats?.Cue(cue.label);gameBodyStats?.Cue(cue.label);
            if ((recordingFullBody || recordingGameBody) && cue.at >= 10 && !standingRequested && poseSource?.ActiveRTMPose != null)
            { standingRequested=true; poseSource.ActiveRTMPose.AcquireStandingReference(); }
            recorder.Cue(recordingGameBody ? "game_body" : recordingFullBody ? "full_body" : "punch", cue, recordingStarted + calibrationPause, now);
            speech?.Speak(cue.speech);
        }
        private void Handle(PunchIntent.Signal signal,int side,float speed,double now)
        {
            if (signal==PunchIntent.Signal.Prepare && State==Motion.Guard) Begin(side,now);
            if (signal==PunchIntent.Signal.Commit)
            {
                // A confirmed opposite-hand stroke outranks a tentative
                // preparation. Queuing it behind preparation + recovery can
                // exceed the queue's freshness window and lose the punch.
                if (State==Motion.Guard || State==Motion.Recovering || State==Motion.Preparing && Side!=side)
                { queuedSide=-1;Begin(side,now); }
                if (State==Motion.Preparing && Side==side) Commit(speed,now);
                else if (Side!=side || lastFrame.boxing && State==Motion.Striking)
                { queuedSide=side; queuedSpeed=speed; queuedAt=now; }
            }
            if (signal==PunchIntent.Signal.Cancel && State==Motion.Preparing && Side==side) Recover(now);
        }
        private void Begin(int side,double now)
        {
            if (State != Motion.Guard && weight > 0 && Side != side)
            {
                outgoingSide=Side; outgoingPhase=Phase; outgoingWeight=weight; crossStarted=now;
                outgoingFistStart=(Side==0?leftHand:rightHand).position;
            }
            Side=side; ActionId++; State=Motion.Preparing; motionStarted=now; Phase=weight=0;
            Transform shoulder=animator.GetBoneTransform(side==0?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
            // Capture a reachable target once. Do not chase the bag as it swings away.
            strikeTarget=bag.Surface.ClosestPoint(shoulder.position+transform.forward*.1f);
            strikeTarget-=transform.forward*.045f;
            strikeStart=(side==0?leftHand:rightHand).position;
            assistedFistTargets[side]=strikeStart;
            // A one-observation commit can reach contact on its first composed frame.
            // Seed the swept fist at the current guard before evaluating the fast clip.
            if(lastFrame.boxing)
                bag.SampleAssistedPunch(side,ActionId,(side==0?leftHand:rightHand).position,
                    Mathf.Clamp(Time.unscaledDeltaTime,.001f,.15f),now,true,false);
        }
        public static float StrikeDuration(float speed) => Mathf.Lerp(.62f,.40f,Mathf.InverseLerp(1,5,speed));
        public static float BoxingStrikeDuration(float speed) => Mathf.Lerp(.36f,.24f,Mathf.InverseLerp(1,5,speed));
        private void Commit(float speed,double now)
        { State=Motion.Striking; motionStarted=now; duration=lastFrame.boxing ? BoxingStrikeDuration(speed) : StrikeDuration(speed); }
        private void Recover(double now)
        { previousWeight=weight; frozenPhase=Phase; State=Motion.Recovering; motionStarted=now; }
        private void Compose(double now)
        {
            Vector3 position=transform.position; Quaternion rotation=transform.rotation;
            // The authored idle includes independent head turns and body fidgets.
            // Use its captured fighting stance; observed motion supplies the idle offsets.
            guard.SetTime(0);
            float outgoing=outgoingSide>=0 ? outgoingWeight*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((float)(now-crossStarted)/.10f))) : 0;
            if (outgoing<=0) outgoingSide=-1;
            float current=weight; float sum=current+outgoing;
            if(sum>1) { current/=sum; outgoing/=sum; }
            leftClip.SetTime(ClipPhase(outgoingSide==0?outgoingPhase:Phase,art.leftContactTime)*art.leftPunch.length);
            rightClip.SetTime(ClipPhase(outgoingSide==1?outgoingPhase:Phase,art.rightContactTime)*art.rightPunch.length);
            mixer.SetInputWeight(0,Mathf.Max(0,1-current-outgoing));
            mixer.SetInputWeight(1,(Side==0?current:0)+(outgoingSide==0?outgoing:0));
            mixer.SetInputWeight(2,(Side==1?current:0)+(outgoingSide==1?outgoing:0));
            graph.Evaluate(0); transform.SetPositionAndRotation(position,rotation);
            float leftWeight=mixer.GetInputWeight(1),rightWeight=mixer.GetInputWeight(2),guardWeight=mixer.GetInputWeight(0);
            Quaternion correction=Quaternion.Slerp(facingCorrections[0],facingCorrections[1],leftWeight/Mathf.Max(.001f,guardWeight+leftWeight));
            correction=Quaternion.Slerp(correction,facingCorrections[2],rightWeight);
            hips.rotation=correction*hips.rotation;
            Vector3 referenceHip=transform.TransformPoint(guardHip);
            Vector3 drift=Vector3.ProjectOnPlane(hips.position-referenceHip,Vector3.up);
            // The manual graph need not rewrite pelvis translation on every
            // zero-time evaluation. Reapplying squat offsets to that previous
            // translation can accumulate height drift and break planted-leg IK.
            // Give guard squat/balance a stable height reference each frame.
            // Active punch clips retain their authored vertical motion.
            if(State==Motion.Guard && outgoingSide<0) hips.position=referenceHip+Vector3.ClampMagnitude(drift,.08f);
            else hips.position+=Vector3.ClampMagnitude(drift,.08f)-drift;
            ComposeTrackedBody();
            for(int side=0;side<2;side++)
            {
                var upper=animator.GetBoneTransform(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
                var lower=animator.GetBoneTransform(side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
                var foot=animator.GetBoneTransform(side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                ComposeLeg(side,upper,lower,foot,now);
            }
            ComposeTrackedArms(now);
            if(lastFrame.boxing && outgoingSide>=0)
            {
                var hand=outgoingSide==0?leftHand:rightHand;
                float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((float)(now-crossStarted)/.10f));
                AimArm(outgoingSide,Vector3.Lerp(outgoingFistStart,hand.position,blend),1);
            }
            float aim=State==Motion.Striking ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.35f,Phase)) *
                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,.75f,Phase))) : 0;
            if(lastFrame.boxing && State==Motion.Striking)
            {
                // Keep the authored world target below the physical sweep's 18m/s
                // rejection threshold, including short high-refresh render frames.
                assistedFistTargets[Side]=Vector3.MoveTowards(assistedFistTargets[Side],BoxingFistTarget(strikeStart,strikeTarget,Phase),
                    12*Mathf.Min(Time.unscaledDeltaTime,.05f));
                AimArm(Side,assistedFistTargets[Side],1);
            }
            else if (aim>0) AimArm(Side,strikeTarget,aim);
            float entry=Mathf.SmoothStep(0,1,Mathf.Clamp01((float)(now-entryAt)/.15f));
            if (entry<1) for(int i=0;i<trackedBones.Length;i++) trackedBones[i].localRotation=Quaternion.Slerp(entryRotations[i],trackedBones[i].localRotation,entry);
        }
        private void ObserveBody(double now)
        {
            var provider=poseSource?.ActiveRTMPose;
            if(provider==null || !provider.UsesWorldLandmarks || !provider.HasRecentPose || provider.PoseSequence==lastBodySequence) return;
            lastBodySequence=provider.PoseSequence;
            var joints=new Vector3[33];var confidence=new float[33];
            float scale=1.7f/Mathf.Max(1,provider.userHeight);
            for(int i=0;i<33;i++)
            {
                joints[i]=provider.AcceptedAvatarPoint((PoseLandmarks)i)*scale;
                confidence[i]=provider.AcceptedImageConfidence((PoseLandmarks)i);
            }
            var frame=new GameBodyMotion.Snapshot { sequence=provider.PoseSequence,captured=provider.AcceptedCaptureTime,
                received=provider.AcceptedReceiveTime,joints=joints,confidence=confidence,
                body=provider.BodyRotation,head=provider.HeadRotation,standingHipHeight=provider.StandingHipHeight*scale };
            BodyMotion.Observe(frame,now);gameBodyStats?.Observation(frame,provider);
            if(recorder!=null && gameBodyStats!=null)
                recorder.RecordJson(JsonUtility.ToJson(new BodyObservationRecord{kind="body_observation",recordedAt=now,snapshot=frame,
                    legsVerified=BodyMotion.VerifiedLegs,displayedLegSequence=BodyMotion.LegPose.sequence}));
        }
        [Serializable] private struct BodyObservationRecord
        { public string kind;public double recordedAt;public GameBodyMotion.Snapshot snapshot;public bool legsVerified;public long displayedLegSequence; }
        private void BodyDecision(string kind,int side,long action,double captured,double now)
        {
            gameBodyStats?.Leg(kind,side,action,captured,now);
            if(recorder!=null) recorder.RecordJson(JsonUtility.ToJson(new BodyActionRecord {
                kind="body_action",actionKind=kind,reason=BodyMotion.Reason,side=side,action=action,capturedAt=captured,decidedAt=now }));
        }
        [Serializable] private struct BodyActionRecord
        { public string kind,actionKind,reason;public int side;public long action;public double capturedAt,decidedAt; }
        private void ComposeTrackedBody()
        {
            var pose=avatar != null ? avatar.PosePuppet?.StabilizedPose : null;
            bool full=BodyMotion.Fresh(Time.realtimeSinceStartupAsDouble);
            bool live=pose!=null && pose.HasRecentPose;
            Quaternion bodyTarget=full ? LimitOffset(BodyMotion.Latest.body,60) : live ? LimitOffset(pose.BodyRotation,28) : Quaternion.identity;
            Quaternion headTarget=full ? LimitOffset(BodyMotion.Latest.head,45) : live ? LimitOffset(pose.HeadRotation,30) : Quaternion.identity;
            bodyOffset=AvatarMotionSmoothing.Rotation(bodyOffset,bodyTarget,ref bodyAngularVelocity,.07f,.028f,Time.unscaledDeltaTime);
            headOffset=AvatarMotionSmoothing.Rotation(headOffset,headTarget,ref headAngularVelocity,.09f,.04f,Time.unscaledDeltaTime);
            if(full)
            {
                trackedSquat=Mathf.SmoothDamp(trackedSquat,BodyMotion.Squat,ref squatVelocity,.055f,2,Mathf.Min(Time.unscaledDeltaTime,.05f));
                hips.position-=Vector3.up*trackedSquat;
                if(BodyMotion.IsStrike)
                {
                    float balance=GameBodyMotion.StrikeEnvelope(BodyMotion.Phase);
                    hips.position+=transform.right*(BodyMotion.Side==0?.035f:-.035f)*balance;
                    if(BodyMotion.StrikeKind==GameBodyMotion.LegMotion.Knee) hips.position+=transform.forward*.14f*balance;
                    // Transfer weight toward the target while the support foot
                    // remains constrained. Retreating the pelvis during extension
                    // needlessly moved reachable kick targets beyond the rig.
                    else hips.position+=transform.forward*.08f*balance;
                    if(BodyMotion.StrikeKind==GameBodyMotion.LegMotion.SideKick)
                        AddBodyOffset(hips,Quaternion.Euler(0,BodyMotion.Side==0?25:-25,0),balance);
                }
                AddBodyOffset(hips,bodyOffset,.2f);
                AddBodyOffset(spine,bodyOffset,chest==null?.8f:.4f);
                AddBodyOffset(chest,bodyOffset,spine==null?.8f:.4f);
                AddBodyOffset(neck,headOffset,head==null?1:.4f);
                AddBodyOffset(head,headOffset,neck==null?1:.6f);
            }
            else
            {
                trackedSquat=Mathf.SmoothDamp(trackedSquat,0,ref squatVelocity,.12f,.8f,Mathf.Min(Time.unscaledDeltaTime,.05f));
                hips.position-=Vector3.up*trackedSquat;
                ApplyBodyOffsets(transform,spine,chest,neck,head,bodyOffset,headOffset);
            }
        }
        private void AddBodyOffset(Transform bone,Quaternion offset,float fraction)
        {
            if(bone!=null) bone.rotation=transform.rotation*Quaternion.Slerp(Quaternion.identity,offset,fraction)
                *Quaternion.Inverse(transform.rotation)*bone.rotation;
        }
        private void ComposeTrackedArms(double now)
        {
            bool fresh=BodyMotion.Fresh(now);
            var f=BodyMotion.Latest;
            for(int side=0;side<2;side++)
            {
                bool punchOwner=State!=Motion.Guard && Side==side || outgoingSide==side;
                var arm=side==0?lastFrame.left:lastFrame.right;
                // A quiet observed guard stays on the authored fighting pose. Clear arm
                // gestures blend to tracked positions; punch ownership wins on its arm.
                bool tracked=fresh && BodyMotion.Joint(11+side) && BodyMotion.Joint(13+side) && BodyMotion.Joint(15+side)
                    && !(lastFrame.boxing && !BodyMotion.ClearArmGesture && BoxingFeatures.Guard(arm));
                var upper=animator.GetBoneTransform(side==0?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                var lower=animator.GetBoneTransform(side==0?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
                var hand=side==0?leftHand:rightHand;
                if(punchOwner)
                {
                    // Seed the next handover from the rendered fist; strikes bypass
                    // target damping so their observed onset/contact stays responsive.
                    haveArmTargets[side]=false;armBlend[side]=armBlendVelocity[side]=0;
                    armVelocities[side]=armPoleVelocities[side]=Vector3.zero;continue;
                }
                armBlend[side]=Mathf.SmoothDamp(armBlend[side],tracked?1:0,ref armBlendVelocity[side],.085f,20,Mathf.Min(Time.unscaledDeltaTime,.05f));
                if(!tracked && armBlend[side]<.002f) { haveArmTargets[side]=false;continue; }
                float rigLength=Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position);
                Vector3 target=hand.position,pole=lower.position-upper.position;
                if(tracked)
                {
                    float observedLength=Vector3.Distance(f.joints[11+side],f.joints[13+side])+Vector3.Distance(f.joints[13+side],f.joints[15+side]);
                    target=upper.position+transform.rotation*(f.joints[15+side]-f.joints[11+side])*rigLength/Mathf.Max(.2f,observedLength);
                    pole=transform.rotation*(f.joints[13+side]-f.joints[11+side]);
                }
                if(!haveArmTargets[side]) { armTargets[side]=hand.position;armPoles[side]=lower.position-upper.position; }
                armTargets[side]=AvatarMotionSmoothing.Position(armTargets[side],target,ref armVelocities[side],.035f,6,Time.unscaledDeltaTime);
                armPoles[side]=AvatarMotionSmoothing.Position(armPoles[side],pole,ref armPoleVelocities[side],.06f,5,Time.unscaledDeltaTime);
                haveArmTargets[side]=true;
                SolveArm(upper,lower,hand,Vector3.Lerp(hand.position,armTargets[side],armBlend[side]),armPoles[side],armBlend[side]);
            }
        }
        private void ComposeLeg(int side,Transform upper,Transform lower,Transform foot,double now)
        {
            Vector3 planted=transform.TransformPoint(plantedFeet[side]);
            Vector3 target=planted,pole=transform.forward;
            bool live=BodyMotion.LegPoseFresh(now);
            bool swing=live && BodyMotion.Side==side && BodyMotion.IsStrike;
            if(live)
            {
                var f=BodyMotion.LegPose;
                float lift=Mathf.Max(0,f.joints[27+side].y-f.joints[27+1-side].y);
                if(BodyMotion.FootReleased(side))
                {
                    // Supporting foot stays planted; observed ankle displacement releases
                    // only the lifted foot, including held knees and non-strike side lifts.
                    Vector3 delta=f.joints[27+side]-f.joints[27+1-side];
                    float lateral=delta.x-(plantedFeet[side].x-plantedFeet[1-side].x);
                    target=planted+transform.rotation*new Vector3(Mathf.Clamp(lateral,-.6f,.6f),Mathf.Min(lift,.85f),Mathf.Clamp(delta.z,-.35f,.8f));
                    pole=transform.rotation*(f.joints[25+side]-f.joints[23+side]);
                }
            }
            if(!swing)
            {
                // Provisional lifts must stay clear of the bag. Beginning a
                // strike already inside its contact volume cannot rearm a sweep.
                float clearance=bag.Surface.radius*1.15f;
                if(Vector3.Distance(target,bag.Surface.ClosestPoint(target))<clearance)
                    target=bag.Surface.ClosestPoint(target-transform.forward*2)-transform.forward*clearance;
            }
            if(swing)
            {
                if(legTargetAction!=BodyMotion.Action)
                {
                    legTargetAction=BodyMotion.Action;
                    legStrikeStart=renderedFeet[side]==Vector3.zero?foot.position:renderedFeet[side];
                    legKneeStart=renderedKnees[side]==Vector3.zero?lower.position:renderedKnees[side];
                    Vector3 query=upper.position+transform.forward*.2f;
                    query.y=BodyMotion.StrikeKind==GameBodyMotion.LegMotion.Knee ? upper.position.y+.02f : upper.position.y-.12f;
                    Vector3 surface=bag.Surface.ClosestPoint(query);
                    legStrikeTarget=LegContactTarget(surface,bag.Body.GetPointVelocity(surface),transform.forward,
                        BodyMotion.StrikeKind!=GameBodyMotion.LegMotion.Knee);
                    bag.SampleAssistedLimb(BodyMotion.ContactLimb,BodyMotion.Action,
                        BodyMotion.StrikeKind==GameBodyMotion.LegMotion.Knee?legKneeStart:legStrikeStart,
                        Mathf.Clamp(Time.unscaledDeltaTime,.001f,.15f),now,true,false);
                }
                float envelope=GameBodyMotion.StrikeEnvelope(BodyMotion.Phase);
                if(BodyMotion.StrikeKind==GameBodyMotion.LegMotion.Knee)
                {
                    float thigh=Vector3.Distance(upper.position,lower.position);
                    Vector3 knee=Vector3.Lerp(legKneeStart,legStrikeTarget,envelope);
                    knee=upper.position+Vector3.ClampMagnitude(knee-upper.position,thigh);
                    upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,knee-upper.position)*upper.rotation;
                    // Fold the shin below the raised knee; only the measured knee can score.
                    Vector3 tucked=lower.position-transform.up*Vector3.Distance(lower.position,foot.position)*.85f-transform.forward*.12f;
                    lower.rotation=Quaternion.FromToRotation(foot.position-lower.position,tucked-lower.position)*lower.rotation;
                    foot.rotation=transform.rotation*plantedFootRotations[side];footTargets[side]=foot.position;footVelocities[side]=Vector3.zero;
                    renderedFeet[side]=foot.position;renderedKnees[side]=lower.position;return;
                }
                target=Vector3.Lerp(legStrikeStart,legStrikeTarget,envelope);
                pole=BodyMotion.StrikeKind==GameBodyMotion.LegMotion.SideKick ? transform.right*(side==0?-1:1) : transform.forward;
            }
            footTargets[side]=AvatarMotionSmoothing.Position(footTargets[side]==Vector3.zero?planted:footTargets[side],target,
                ref footVelocities[side],swing?.018f:.05f,swing?12:4,Time.unscaledDeltaTime);
            if(legPoles[side]==Vector3.zero) legPoles[side]=transform.forward;
            legPoles[side]=AvatarMotionSmoothing.Position(legPoles[side],pole,ref legPoleVelocities[side],.055f,6,Time.unscaledDeltaTime);
            SolveArm(upper,lower,foot,footTargets[side],legPoles[side],1);
            foot.rotation=transform.rotation*plantedFootRotations[side];
            renderedFeet[side]=foot.position;renderedKnees[side]=lower.position;
        }
        // Capture one short, bounded lead at commitment. Never chase the bag
        // afterward or change impact permission from a predicted target.
        public static Vector3 LegContactTarget(Vector3 surface,Vector3 velocity,Vector3 forward,bool footStrike)
        {
            Vector3 lead=footStrike && BoxingFeatures.Finite(velocity)
                ?Vector3.ClampMagnitude(velocity*.14f,.10f):Vector3.zero;
            return surface-forward*.035f+lead;
        }
        public static Quaternion LimitOffset(Quaternion value, float degrees)
        {
            if (float.IsNaN(value.x+value.y+value.z+value.w) || float.IsInfinity(value.x+value.y+value.z+value.w))
                return Quaternion.identity;
            return Quaternion.RotateTowards(Quaternion.identity, value, degrees);
        }
        public static void ApplyBodyOffsets(Transform root, Transform spine, Transform chest, Transform neck,
            Transform head, Quaternion body, Quaternion face)
        {
            body = LimitOffset(body, 28); face = LimitOffset(face, 30);
            void Add(Transform bone, Quaternion offset, float fraction)
            {
                if (bone != null) bone.rotation = root.rotation * Quaternion.Slerp(Quaternion.identity, offset, fraction)
                    * Quaternion.Inverse(root.rotation) * bone.rotation;
            }
            Add(spine, body, chest == null ? 1 : .45f);
            Add(chest, body, spine == null ? 1 : .55f);
            Add(neck, face, head == null ? 1 : .4f);
            Add(head, face, neck == null ? 1 : .6f);
        }
        // Both clips contact at the same action phase despite different authored windups.
        public static float ClipPhase(float phase,float contact)
        {
            contact=Mathf.Clamp(contact,.05f,.9f);
            return phase<=.4f ? Mathf.Lerp(0,contact,phase/.4f) : Mathf.Lerp(contact,1,(phase-.4f)/.6f);
        }
        public static Vector3 BoxingFistTarget(Vector3 start,Vector3 target,float phase)
        {
            float extension=phase<=.5f ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.4f,phase))
                : 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,phase));
            return Vector3.Lerp(start,target,extension);
        }
        private void AimArm(int side,Vector3 target,float amount)
        {
            var upper=animator.GetBoneTransform(side==0?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
            var lower=animator.GetBoneTransform(side==0?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
            var hand=side==0?leftHand:rightHand;
            Vector3 elbowPole=transform.right*(side==0?-.7f:.7f)-Vector3.up*.6f;
            SolveArm(upper,lower,hand,Vector3.Lerp(hand.position,target,amount),elbowPole,amount);
        }
        public static void SolveArm(Transform upper,Transform lower,Transform hand,Vector3 target,Vector3 fallbackPole,float poleWeight=0)
        {
            Vector3 root=upper.position, oldElbow=lower.position;
            float a=Vector3.Distance(root,oldElbow),b=Vector3.Distance(oldElbow,hand.position);
            if (a<.001f || b<.001f) return;
            Vector3 delta=target-root;
            if(delta.sqrMagnitude<.000001f) return;
            Vector3 axis=delta.normalized;
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.01f,a+b-.015f);
            Vector3 pole=Vector3.ProjectOnPlane(oldElbow-root,axis);
            Vector3 preferred=Vector3.ProjectOnPlane(fallbackPole,axis);
            if(poleWeight>0 && preferred.sqrMagnitude>.0001f)
                pole=Vector3.Slerp(pole.normalized,preferred.normalized,poleWeight);
            if(pole.sqrMagnitude<.0001f) pole=preferred;
            if(pole.sqrMagnitude<.0001f) pole=Vector3.ProjectOnPlane(Vector3.up,axis);
            float along=(a*a-b*b+distance*distance)/(2*distance);
            Vector3 elbow=root+axis*along+pole.normalized*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(oldElbow-root,elbow-root)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,root+axis*distance-lower.position)*lower.rotation;
            // Keep the authored LOCAL wrist rotation: preserving its world rotation would
            // counter-rotate the wrist as the forearm aims, producing an impossible bend.
        }
        #if UNITY_EDITOR
        // Editor-only deterministic pose sampling, never available to a player build.
        public void PreviewPunch(int side,float phase)
        {
            double now=Time.realtimeSinceStartupAsDouble;
            State=Motion.Guard; Phase=weight=0; outgoingSide=-1; entryAt=now-1; Compose(now);
            Begin(side,now); State=Motion.Striking; Phase=phase; weight=1;
            outgoingSide=-1; entryAt=now-1; Compose(now);
        }
#endif
        private void OnApplicationPause(bool paused) { if(paused) { ResetMotion(); StopRecording(); } }
        public void RecordFinalPose()
        {
            if (recorder == null) return;
            var rtm = poseSource?.ActiveRTMPose;
            if (recordingPose==null && rtm != null && (diagnosticSequence != rtm.PoseSequence || diagnosticCalibration != rtm.CalibrationFrames
                || !rtm.Calibrated && Time.realtimeSinceStartupAsDouble >= nextDiagnostic))
            {
                recorder.Diagnostic(rtm.CaptureDiagnostics()); diagnosticSequence=rtm.PoseSequence; diagnosticCalibration=rtm.CalibrationFrames;
                nextDiagnostic=Time.realtimeSinceStartupAsDouble+.5;
            }
            var targets=new Quaternion[boneIds.Length]; var targetApplied=new bool[boneIds.Length];
            if(!Active && avatar.DirectTracking != null)
                for(int i=0;i<boneIds.Length;i++) targetApplied[i]=avatar.DirectTracking.TryGetAppliedTarget(boneIds[i],out targets[i]);
            var lf = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rf = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            recorder.Pose(transform,trackedBones,leftHand,rightHand,Active ? State.ToString() : "Mirroring",Phase,
                lastSequence,ActionId,Side,bag.HitCount,new PunchSessionRecorder.PoseContext {
                    assisted=Active, canHit=CanHit, poseOwner=Active ? "AssistedPunch" : avatar.DirectTracking != null && avatar.DirectTracking.isActiveAndEnabled ? "DirectTracking" : "Puppet",
                    leftFoot=lf != null ? lf.position : Vector3.zero, rightFoot=rf != null ? rf.position : Vector3.zero,
                    bodyAngles=rtm != null ? rtm.BodyAngles : Vector3.zero, headAngles=rtm != null ? rtm.HeadAngles : Vector3.zero,
                    bagPosition=bag.transform.position, bagRotation=bag.transform.rotation, bagVelocity=bag.Body.linearVelocity,
                    appliedSequence=avatar.DirectTracking != null ? avatar.DirectTracking.LastAppliedSequence : -1,
                    appliedTargets=targets, targetApplied=targetApplied,
                    guideElapsed=Time.realtimeSinceStartupAsDouble-recordingStarted-calibrationPause, calibrationPause=calibrationPause,
                    legState=BodyMotion.State.ToString(),legKind=BodyMotion.StrikeKind.ToString(),legAction=BodyMotion.Action,
                    legReason=BodyMotion.Reason,legLift=BodyMotion.SwingLift,kneeBend=BodyMotion.KneeBend,
                    legSide=BodyMotion.Side,legPhase=BodyMotion.Phase,legCanHit=BodyMotion.ContactWindow(Time.realtimeSinceStartupAsDouble),
                    legVerified=BodyMotion.VerifiedLegs,legPoseSequence=BodyMotion.LegPose.sequence,
                    squat=trackedSquat,legTarget=legStrikeTarget,leftKneeContact=bag.GetContactSample(4),rightKneeContact=bag.GetContactSample(5),
                    strikeTarget=strikeTarget, leftContact=bag.GetContactSample(0), rightContact=bag.GetContactSample(1),
                    leftFootContact=bag.GetContactSample(2), rightFootContact=bag.GetContactSample(3) });
            if(boxingStats!=null) boxingStats.finalPoseFrames++;
            if(gameBodyStats!=null) gameBodyStats.finalPoseFrames++;
        }
        public void RecordBagHit() { gameBodyStats?.Hit(); }
        public void RecordPreviewFrame(ulong cameraSequence)
        { if(boxingStats!=null) { boxingStats.previewFrames++;boxingStats.lastPreviewCameraSequence=cameraSequence; }
            if(gameBodyStats!=null) gameBodyStats.previewFrames++; }
        public void RecordRuntimeError(string stage,Exception error)
        {
            if(gameBodyStats!=null) { gameBodyStats.runtimeErrors++;gameBodyStats.lastRuntimeError=stage+": "+error; }
            if(boxingStats!=null) { boxingStats.runtimeErrors++;boxingStats.lastRuntimeError=stage+": "+error; }
        }
        private void RecordInference()
        {
            if(recorder==null || recordingPose==null) return;
            try { var diagnostic=recordingPose.CaptureDiagnostics();recorder.Diagnostic(diagnostic);gameBodyStats?.Inference(diagnostic); }
            catch(Exception e) { RecordRuntimeError("inference_recording",e);Debug.LogError("[Practice inference_recording] "+e); }
        }
        public void ToggleRecording()
        {
            if (recorder != null) { StopRecording(true); return; }
            StartRecording(!Active);
        }
        public void StartFullBodyRecording()
        {
            if (recorder != null) { StopRecording(true); return; }
            StartRecording(true);
        }
        public void StartGameBodyRecording()
        {
            if(recorder!=null) { StopRecording(true);return; }
            StartRecording(false,true);
        }
        private void StartRecording(bool fullBody,bool gameBody=false)
        {
            if (!bound) return;
            Error = null; recordingFullBody = fullBody; recordingGameBody=gameBody; standingRequested=false; diagnosticSequence=diagnosticCalibration=-1;
            FindFirstObjectByType<QuickPoseCheck>()?.Stop();
            FindFirstObjectByType<MediaPipeImprovementTest>()?.Stop("other_recording");
            var rtm = poseSource?.ActiveRTMPose;
            savedAssistance = Active; savedDirect = rtm != null && rtm.Tuning.directAvatar;
            recordingController = poseSource != null ? poseSource.GetComponent<BodyTrackingController>() : null;
            restoreRecordingMode = fullBody || gameBody;
            if(gameBody)
            {
                SetAssistance(true);
                if(rtm!=null) recordingController?.SetDirectAvatar(false);
                gameBodyStats=new GameBodySessionStats(Application.version,rtm?.EngineLabel,bag.HitCount);
            }
            if (fullBody)
            {
                SetAssistance(false);
                if (rtm != null) recordingController?.SetDirectAvatar(true);
            }
            try
            {
                recordingStarted=Time.realtimeSinceStartupAsDouble;
                lastGuideUpdate=recordingStarted; calibrationPause=0; nextCalibrationPrompt=recordingStarted+15;
                recorder=new PunchSessionRecorder(JsonUtility.ToJson(new Metadata {
                    kind="session",schemaVersion=7,version="practice-11-smooth-game-body",test=gameBody ? "game_body" : fullBody ? "full_body" : "punch",
                    recordingStartedAt=recordingStarted,assisted=Active,unity=Application.unityVersion,device=SystemInfo.deviceModel,
                    rig=avatar.name,bones=boneNames,engine=rtm!=null?rtm.EngineLabel:"Tracking4All",tuning=rtm!=null?JsonUtility.ToJson(rtm.Tuning):"baseline",
                    userHeight=rtm!=null?rtm.userHeight:0,cameraPitchDegrees=rtm!=null?rtm.cameraPitchDegrees:0,calibrated=rtm!=null&&rtm.Calibrated,
                    imageCoordinates="Native model order. x is scaled by upright image aspect, y is up. Joint arrays are not reordered for avatar mirroring; arm features are. Halpe26 ROI uses normalized x-right/y-down.",
                    timestampNote="Seconds on Unity realtime clock. RTMPose time is camera availability; baseline is callback delivery. guide_cue recordedAt is speech enqueue, not audible onset. Video is not encoded. REC time/frame aligns optional screen video." }));
                RecordingPath=recorder.Path; cameraInput.CaptureFullBody=true;
                boxingStats=fullBody || gameBody ? null : new BoxingSessionStats(Application.version,rtm?.EngineLabel,RecordingPath,bag.HitCount);
                if(rtm!=null && rtm.isActiveAndEnabled) { recordingPose=rtm;recordingPose.InferenceEvaluated+=RecordInference; }
                recordingGuide.Reset(); fullBodyGuide.Reset();gameBodyGuide.Reset(); nextSpeechStatus=recordingStarted;
                if (TryGuide(0,out var cue)) PublishCue(cue,recordingStarted);
            }
            catch(Exception e)
            {
                if(recordingPose!=null) recordingPose.InferenceEvaluated-=RecordInference;
                recordingPose=null;
                Error=e.Message; recorder?.Dispose(); recorder=null; boxingStats=null;gameBodyStats=null;
                cameraInput.CaptureFullBody=false; RestoreRecordingMode();
            }
        }
        public void StopRecording(bool announce=false,bool completed=false)
        {
            speech?.Stop();
            if(recorder==null) return;
            if(recordingPose!=null) recordingPose.InferenceEvaluated-=RecordInference;
            recordingPose=null;
            string failure=recorder.Error ?? Error;
            if(failure!=null) Error=failure;
            if(gameBodyStats!=null)
            {
                gameBodyStats.Finish(completed,failure,bag.HitCount,recorder.Dropped);
                GameBodySessionStats.Remember(gameBodyStats,System.IO.Path.ChangeExtension(RecordingPath,"summary.json"));
                recorder.SaveSummary(JsonUtility.ToJson(gameBodyStats,true),gameBodyStats.Markdown());gameBodyStats=null;
            }
            if(boxingStats!=null)
            {
                boxingStats.Finish(completed,failure,bag.HitCount,Time.realtimeSinceStartupAsDouble);
                recorder.SaveSummary(JsonUtility.ToJson(boxingStats,true),boxingStats.Markdown());boxingStats=null;
            }
            recorder.Dispose(); recorder=null; cameraInput.CaptureFullBody=false;
            RestoreRecordingMode();
            if(announce) speech?.Speak(failure!=null ? "Recording failed. Please check the phone." : completed
                ? "Test complete. Recording stopped. Your previous tracking mode is restored. You can return to your phone." : "Recording stopped.");
            Debug.Log("[Practice test] Recording: "+RecordingPath);
        }
        private void RestoreRecordingMode()
        {
            if (!restoreRecordingMode) return;
            restoreRecordingMode=false;
            if (recordingController != null) recordingController.SetDirectAvatar(savedDirect);
            if (isActiveAndEnabled) SetAssistance(savedAssistance);
            recordingController=null;
        }
        private void RestoreSkinUpdates()
        { if(skins!=null) for(int i=0;i<skins.Length;i++) if(skins[i]!=null) skins[i].updateWhenOffscreen=previousSkinUpdates[i]; }
        private void OnDisable()
        {
            BodyMotion.Decision-=BodyDecision;
            StopRecording();speech?.Dispose();speech=null;
            if(avatar!=null && avatar.PoseOverride==this) avatar.PoseOverride=null;
            if(animator!=null && bound) { animator.applyRootMotion=previousRootMotion; animator.cullingMode=previousCulling; }
            RestoreSkinUpdates();
            cameraInput?.Dispose(); cameraInput=null;
            if(graph.IsValid()) graph.Destroy();
            bound=false;
        }
        private void OnDestroy()
        { speech?.Dispose(); cameraInput?.Dispose(); if(graph.IsValid()) graph.Destroy(); }
    }
}
