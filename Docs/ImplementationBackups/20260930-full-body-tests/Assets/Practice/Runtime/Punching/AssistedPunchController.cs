using System;
using System.Collections.Generic;
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
        public string RecordingPath { get; private set; }
        public string Error { get; private set; }
        public float Phase { get; private set; }
        public long ActionId { get; private set; }
        public int Side { get; private set; }
        public bool CanHit => Active && State == Motion.Striking && Phase >= .18f && Phase < .68f && Fresh && (Side == 0 ? lastFrame.left.Valid : lastFrame.right.Valid);
        public bool Fresh => Time.realtimeSinceStartupAsDouble - lastReceived <= .23 && latestValid;
        public string Status
        {
            get
            {
                if (!Available) return Error;
                if (!Active) return "BODY MIRRORING";
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
                string prompt = PunchRecordingGuide.Label(elapsed);
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
        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;
        private AnimationClipPlayable guard, leftClip, rightClip;
        private bool useAssistance, previousRootMotion;
        private AnimatorCullingMode previousCulling;
        private double lastReceived = double.NegativeInfinity, motionStarted, recordingStarted;
        private long lastSequence = -1;
        private bool latestValid;
        private float weight, duration = .55f, previousWeight, frozenPhase, guardTime;
        private int queuedSide = -1;
        private float queuedSpeed;
        private int outgoingSide = -1;
        private float outgoingPhase, outgoingWeight;
        private double crossStarted;
        private double queuedAt;
        private Vector3 strikeTarget;
        private readonly Quaternion[] facingCorrections = new Quaternion[3];
        private readonly Vector3[] plantedFeet = new Vector3[2];
        private readonly Quaternion[] plantedFootRotations = new Quaternion[2];
        private Vector3 guardHip;
        private Transform hips, leftHand, rightHand, spine, chest, neck, head;
        private Quaternion bodyOffset = Quaternion.identity, headOffset = Quaternion.identity;
        private Transform[] trackedBones;
        private string[] boneNames;
        private Quaternion[] entryRotations;
        private double entryAt;
        private PunchSessionRecorder recorder;
        private PracticeSpeech speech;
        private readonly PunchRecordingGuide recordingGuide = new PunchRecordingGuide();
        private double nextSpeechStatus;
        private bool bound;
        private SkinnedMeshRenderer[] skins;
        private bool[] previousSkinUpdates;

        [Serializable] private struct Metadata
        {
            public string kind, version, unity, device, rig, engine, timestampNote;
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
            cameraInput=new PunchCameraInput(target.PosePuppet?.PoseSource?.GetComponent<PoseSolution>());
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
            AnimationPlayableOutput.Create(graph,"Punch practice",animator).SetSourcePlayable(mixer);
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
            bound=true; SetAssistance(true);
        }
        public void SetAssistance(bool value)
        {
            if (!Available) return;
            ResetMotion(); useAssistance=value;
            if(!value) StopRecording();
            if (value)
            {
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
            gestureGate.Reset();
            bodyOffset = headOffset = Quaternion.identity;
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
            if (!bound || !Active) return;
            double now=Time.realtimeSinceStartupAsDouble;
            PunchCameraInput.Frame frame;
            bool received, changed;
            #if UNITY_EDITOR
            if(EditorInput) { frame=editorFrame;received=editorFrameReady;editorFrameReady=false;changed=false; }
            else
            #endif
                received=cameraInput.TryRead(out frame,out changed);
            if (changed) ResetMotion();
            if (received)
            {
                lastFrame=frame; lastSequence=frame.sequence; lastReceived=frame.received;
                latestValid=frame.left.Valid || frame.right.Valid;
                gestureGate.Observe(frame);
                if (gestureGate.Blocked)
                {
                    queuedSide = -1;
                    if (State == Motion.Preparing) Recover(now);
                }
                for (int side=0;side<2;side++)
                {
                    var signal=intent[side].Observe(side==0?frame.left:frame.right,frame.sequence,frame.time,
                        gestureGate.Blocked ? gestureGate.Reason : null);
                    Handle(signal,side,intent[side].Speed,now);
                }
                recorder?.Observation(frame,intent[0],intent[1]);
            }
            if (!Fresh)
            {
                foreach(var detector in intent) detector.Reset();
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
            Compose(now);
            if (recorder != null)
            {
                if(now>=nextSpeechStatus) { speech?.CheckStatus();nextSpeechStatus=now+.5; }
                double elapsed=now-recordingStarted;
                if(recorder.Error!=null) StopRecording(true);
                else if(elapsed>=PunchRecordingGuide.Duration) StopRecording(true,true);
                else if(recordingGuide.TryAdvance(elapsed,out var cue)) speech?.Speak(cue.speech);
            }
        }
        private void Handle(PunchIntent.Signal signal,int side,float speed,double now)
        {
            if (signal==PunchIntent.Signal.Prepare && State==Motion.Guard) Begin(side,now);
            if (signal==PunchIntent.Signal.Commit)
            {
                if (State==Motion.Guard || State==Motion.Recovering) Begin(side,now);
                if (State==Motion.Preparing && Side==side) Commit(speed,now);
                else if (Side!=side) { queuedSide=side; queuedSpeed=speed; queuedAt=now; }
            }
            if (signal==PunchIntent.Signal.Cancel && State==Motion.Preparing && Side==side) Recover(now);
        }
        private void Begin(int side,double now)
        {
            if (State != Motion.Guard && weight > 0 && Side != side)
            { outgoingSide=Side; outgoingPhase=Phase; outgoingWeight=weight; crossStarted=now; }
            Side=side; ActionId++; State=Motion.Preparing; motionStarted=now; Phase=weight=0;
            Transform shoulder=animator.GetBoneTransform(side==0?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
            // Capture a reachable target once. Do not chase the bag as it swings away.
            strikeTarget=bag.Surface.ClosestPoint(shoulder.position+transform.forward*.1f);
            strikeTarget-=transform.forward*.045f;
        }
        private void Commit(float speed,double now)
        { State=Motion.Striking; motionStarted=now; duration=Mathf.Lerp(.62f,.40f,Mathf.InverseLerp(1,5,speed)); }
        private void Recover(double now)
        { previousWeight=weight; frozenPhase=Phase; State=Motion.Recovering; motionStarted=now; }
        private void Compose(double now)
        {
            Vector3 position=transform.position; Quaternion rotation=transform.rotation;
            guardTime+=Time.unscaledDeltaTime;
            guard.SetTime(guardTime%Mathf.Max(.01f,art.guard.length));
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
            hips.position+=Vector3.ClampMagnitude(drift,.08f)-drift;
            ComposeTrackedBody();
            for(int side=0;side<2;side++)
            {
                var upper=animator.GetBoneTransform(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
                var lower=animator.GetBoneTransform(side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
                var foot=animator.GetBoneTransform(side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                SolveArm(upper,lower,foot,transform.TransformPoint(plantedFeet[side]),transform.forward);
                foot.rotation=transform.rotation*plantedFootRotations[side];
            }
            float aim=State==Motion.Striking ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.35f,Phase)) *
                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,.75f,Phase))) : 0;
            if (aim>0) AimArm(Side,strikeTarget,aim);
            float entry=Mathf.SmoothStep(0,1,Mathf.Clamp01((float)(now-entryAt)/.15f));
            if (entry<1) for(int i=0;i<trackedBones.Length;i++) trackedBones[i].localRotation=Quaternion.Slerp(entryRotations[i],trackedBones[i].localRotation,entry);
        }
        private void ComposeTrackedBody()
        {
            // Reuse the existing calibrated torso/face estimator. It needs shoulders and
            // hips, not feet. Add bounded motion to the authored pose rather than replacing it.
            var pose = avatar != null ? avatar.PosePuppet?.StabilizedPose : null;
            bool live = pose != null && pose.HasRecentPose;
            Quaternion bodyTarget = live ? LimitOffset(pose.BodyRotation, 28) : Quaternion.identity;
            Quaternion headTarget = live ? LimitOffset(pose.HeadRotation, 30) : Quaternion.identity;
            bodyOffset = Quaternion.RotateTowards(bodyOffset, bodyTarget, 240 * Time.unscaledDeltaTime);
            headOffset = Quaternion.RotateTowards(headOffset, headTarget, 240 * Time.unscaledDeltaTime);
            ApplyBodyOffsets(transform, spine, chest, neck, head, bodyOffset, headOffset);
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
            if (recorder != null) recorder.Pose(transform,trackedBones,leftHand,rightHand,State.ToString(),Phase,lastSequence,ActionId,Side,bag.HitCount);
        }
        public void ToggleRecording()
        {
            if(recorder!=null) { StopRecording(true); return; }
            if(!Active) return;
            try
            {
                var source=avatar.PosePuppet?.PoseSource?.GetComponent<PoseSolution>();
                recorder=new PunchSessionRecorder(JsonUtility.ToJson(new Metadata { kind="session",version="punch-assist-3-outward-intent",
                    unity=Application.unityVersion,device=SystemInfo.deviceModel,rig=avatar.name,bones=boneNames,
                    engine=source?.ActiveRTMPose!=null?"RTMPose":"Tracking4All",tuning=source?.ActiveRTMPose!=null?JsonUtility.ToJson(source.ActiveRTMPose.Tuning):"baseline",
                    timestampNote="Seconds on Unity realtime clock; RTMPose time is camera availability, baseline time is callback delivery. Video is not encoded. REC frame counter aligns an optional device screen recording." }));
                RecordingPath=recorder.Path; recordingStarted=Time.realtimeSinceStartupAsDouble;
                recordingGuide.Reset();nextSpeechStatus=recordingStarted;
                if(recordingGuide.TryAdvance(0,out var cue)) speech?.Speak(cue.speech);
            }
            catch(Exception e) { Error=e.Message; recorder=null; }
        }
        public void StopRecording(bool announce=false,bool completed=false)
        {
            speech?.Stop();
            if(recorder==null) return;
            string failure=recorder.Error;
            if(failure!=null) Error=failure;
            recorder.Dispose(); recorder=null;
            if(announce) speech?.Speak(failure!=null ? "Recording failed. Please check the phone." : completed
                ? "Test complete. Recording stopped. You can return to your phone." : "Recording stopped.");
            Debug.Log("[Punch practice] Recording: "+RecordingPath);
        }
        private void RestoreSkinUpdates()
        { if(skins!=null) for(int i=0;i<skins.Length;i++) if(skins[i]!=null) skins[i].updateWhenOffscreen=previousSkinUpdates[i]; }
        private void OnDisable()
        {
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
