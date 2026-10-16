using System;
using UnityEngine;
using Tracking4All;

namespace FitFight.Practice
{
    /// <summary>Observed leg intent plus a bounded strike. Render prediction never commits an action.</summary>
    public sealed class GameBodyMotion
    {
        public enum LegMotion { Grounded, Chamber, FrontKick, SideKick, Knee, Recovering, Kick }
        [Serializable] public struct Snapshot
        {
            public long sequence;
            public double captured, received;
            // Avatar coordinates, meters scaled to a 1.7m character, with hips at the origin.
            public Vector3[] joints;
            public float[] confidence;
            public Quaternion body, head;
            public float standingHipHeight;
        }
        public LegMotion State { get; private set; }
        public LegMotion StrikeKind { get; private set; }
        public int Side { get; private set; }
        public long Action { get; private set; }
        public float Phase { get; private set; }
        public float Squat { get; private set; }
        public float SwingLift { get; private set; }
        public float KneeBend { get; private set; }
        public bool Fresh(double now) => have && now-lastReceived<=.23 && now-lastCapture<=.3;
        public bool ContactWindow(double now) => VerifiedLegs && Fresh(now) && IsStrike && Phase>=.2f && Phase<.62f && Reliable(Side) && Reliable(1-Side);
        public bool IsStrike => State==LegMotion.FrontKick || State==LegMotion.SideKick || State==LegMotion.Knee || State==LegMotion.Kick;
        public int ContactLimb => (StrikeKind==LegMotion.Knee ? 4 : 2)+Side;
        public string Reason { get; private set; } = "Show both feet";
        public Snapshot Latest { get; private set; }
        // Display may bridge a short corrupt sample; impact eligibility never does.
        public Snapshot LegPose { get; private set; }
        public bool VerifiedLegs { get; private set; }
        public bool LegPoseFresh(double now) => legPoseHave && now-LegPose.received<=.23 && now-LegPose.captured<=.3;
        private bool legPoseHave;
        private double invalidSince=double.NaN;
        public event Action<string,int,long,double,double> Decision;
        private Snapshot previous;
        private bool have;
        private long sequence=-1;
        private double lastCapture=double.NegativeInfinity,lastReceived=double.NegativeInfinity,started,spent;
        private float chamberBend, groundKneeForward, peakLift, peakKneeForward, peakKneeSpeed;
        private double chamberCaptured;
        private Vector3 preparationForward;
        private readonly float[] thighLength=new float[2],shinLength=new float[2];
        private int groundedSamples;
        private bool rearmed=true;
        private readonly Vector3[] groundKnees=new Vector3[2];
        private bool haveGround;
        private readonly bool[] releasedFoot=new bool[2];
        public bool FootReleased(int side) => side>=0 && side<2 && releasedFoot[side];
        private float recoverPhase;
        public static float Bend(Vector3 hip,Vector3 knee,Vector3 ankle) => Vector3.Angle(knee-hip,ankle-knee);
        public bool Joint(int joint) => Latest.joints!=null && Latest.confidence!=null && joint<Latest.joints.Length
            && joint<Latest.confidence.Length && Latest.confidence[joint]>=.5f && BoxingFeatures.Finite(Latest.joints[joint]);
        public bool ClearArmGesture
        {
            get
            {
                if(!Joint(11) || !Joint(12) || !Joint(13) || !Joint(14) || !Joint(15) || !Joint(16)) return false;
                Vector3 left=Latest.joints[15]-Latest.joints[11],right=Latest.joints[16]-Latest.joints[12];
                bool overhead=left.y>.22f && right.y>.22f;
                bool sides=Mathf.Abs(left.x)>.32f && Mathf.Abs(right.x)>.32f && Mathf.Abs(left.y)<.25f && Mathf.Abs(right.y)<.25f
                    && Mathf.Abs(left.z)<.35f && Mathf.Abs(right.z)<.35f;
                bool forward=left.z>.32f && right.z>.32f && Vector3.Dot(left.normalized,right.normalized)>.6f;
                return overhead || sides || forward;
            }
        }
        public bool Reliable(int side) => Joint(23+side) && Joint(25+side) && Joint(27+side);
        public void Reset()
        {
            State=LegMotion.Grounded;StrikeKind=LegMotion.Grounded;have=haveGround=false;sequence=-1;releasedFoot[0]=releasedFoot[1]=false;
            legPoseHave=VerifiedLegs=false;invalidSince=double.NaN;
            lastCapture=lastReceived=double.NegativeInfinity;Phase=Squat=SwingLift=0;rearmed=true;groundedSamples=0;
            Array.Clear(thighLength,0,2);Array.Clear(shinLength,0,2);
        }
        public void Observe(Snapshot frame,double now)
        {
            if(frame.sequence<=sequence || frame.captured<=lastCapture || frame.joints==null || frame.joints.Length<33
                || frame.confidence==null || frame.confidence.Length<33 || now-frame.captured>.3 || now-frame.received>.23) return;
            float dt=(float)(frame.captured-lastCapture);
            previous=Latest;Latest=frame;sequence=frame.sequence;lastCapture=frame.captured;lastReceived=frame.received;
            bool usable=have && dt>0 && dt<=.25f;have=true;
            if(!Reliable(0) || !Reliable(1)) { RejectLegs(now,"Leg confidence lost");return; }
            Vector3 L(int joint) => frame.joints[joint];
            float ankleDifference=L(27).y-L(28).y;
            int lifted=ankleDifference>=0?0:1;
            float lift=Mathf.Abs(ankleDifference);
            float bendL=Bend(L(23),L(25),L(27)),bendR=Bend(L(24),L(26),L(28));
            float hipHeight=-Mathf.Min(L(27).y,L(28).y);
            float standing=frame.standingHipHeight>.4f ? frame.standingHipHeight : .9f;
            float observedSquat=Mathf.Clamp(standing-hipHeight,0,.48f);
            if(!usable) { Cancel(now,"Establish fresh stance");haveGround=false;groundedSamples=0;rearmed=false;releasedFoot[0]=releasedFoot[1]=false; }
            bool planted=lift<.055f;
            // Validate both legs, including planted frames, before releasing a foot,
            // changing squat height or publishing a display target.
            {
                for(int s=0;s<2;s++)
                {
                    float tr=thighLength[s]>0?thighLength[s]:standing*.5f;
                    float sr=shinLength[s]>0?shinLength[s]:standing*.5f;
                    float t=Vector3.Distance(L(23+s),L(25+s)),sh=Vector3.Distance(L(25+s),L(27+s));
                    // Compare against the last verified pose. A rejected sample must
                    // not make the next good observation look like another teleport.
                    float verifiedDt=legPoseHave?(float)(frame.captured-LegPose.captured):dt;
                    var prior=legPoseHave?LegPose:previous;
                    float ks=usable?Vector3.Distance(L(25+s),prior.joints[25+s])/Mathf.Max(.001f,verifiedDt):0;
                    float fs=usable?Vector3.Distance(L(27+s),prior.joints[27+s])/Mathf.Max(.001f,verifiedDt):0;
                    if(t<tr*.3f || t>tr*1.75f || sh<sr*.3f || sh>sr*1.75f || ks>7 || fs>Mathf.Max(10,(tr+sr)*16)
                        || lift>standing+(tr+sr)*1.25f)
                    { RejectLegs(now,"Leg tracking jump");return; }
                }
            }
            if(!double.IsNaN(invalidSince) && now-invalidSince>.12)
            { Cancel(now,"Leg tracking reacquired");haveGround=false;rearmed=false; }
            if(legPoseHave && frame.captured-LegPose.captured<=.25)
            { previous=LegPose;dt=(float)(frame.captured-LegPose.captured); }
            VerifiedLegs=legPoseHave=true;invalidSince=double.NaN;LegPose=frame;Squat=observedSquat;
            for(int s=0;s<2;s++)
            {
                float observedLift=s==0?ankleDifference:-ankleDifference;
                releasedFoot[s]=observedLift>(releasedFoot[s]?.035f:.075f);
            }
            if(planted)
            {
                for(int s=0;s<2;s++)
                {
                    groundKnees[s]=L(25+s);
                    // Learn lengths only in an extended stance, never from an
                    // occluded chamber or a low squat.
                    if((s==0?bendL:bendR)<35 && Squat<.15f)
                    {
                        float thigh=Vector3.Distance(L(23+s),L(25+s)),shin=Vector3.Distance(L(25+s),L(27+s));
                        if(thigh>.15f && shin>.15f && thigh+shin<1.3f)
                        {
                            thighLength[s]=thighLength[s]==0?thigh:Mathf.Lerp(thighLength[s],thigh,.05f);
                            shinLength[s]=shinLength[s]==0?shin:Mathf.Lerp(shinLength[s],shin,.05f);
                        }
                    }
                }
                groundedSamples++;haveGround=groundedSamples>=2;
                if(!IsStrike && State!=LegMotion.Recovering)
                { State=LegMotion.Grounded;Phase=0;Reason="Feet grounded"; }
                if(haveGround && !IsStrike && State!=LegMotion.Recovering && now-spent>.25) rearmed=true;
                return;
            }
            groundedSamples=0;
            if(!usable || !haveGround) { Reason="Establish two-foot stance";return; }
            // Bilateral crouches and tracking jumps must never become leg attacks.
            float footSpeed=Vector3.Distance(L(27+lifted),previous.joints[27+lifted])/dt;
            float thighReference=thighLength[lifted]>0?thighLength[lifted]:standing*.5f;
            float shinReference=shinLength[lifted]>0?shinLength[lifted]:standing*.5f;
            // A real high kick can put the ankle above the head. Bound it by
            // calibrated anatomy and motion, not a fixed waist-height ceiling.
            if(State==LegMotion.Grounded && rearmed && lift>.075f && Squat<.3f)
            {
                Side=lifted;Action++;State=LegMotion.Chamber;started=now;Phase=0;
                StrikeKind=LegMotion.Chamber;
                chamberBend=Side==0?bendL:bendR;
                chamberCaptured=frame.captured;
                Vector3 across=Vector3.ProjectOnPlane(L(24)-L(23),Vector3.up).normalized;
                preparationForward=across.sqrMagnitude>.5f?Vector3.Cross(across,Vector3.up):Vector3.forward;
                groundKneeForward=Vector3.Dot(groundKnees[Side],preparationForward);
                peakKneeForward=Vector3.Dot(L(25+Side),preparationForward);
                peakKneeSpeed=0;peakLift=lift;
                Reason="Observed leg preparation";Decision?.Invoke("prepare",Side,Action,frame.captured,now);
            }
            if(State!=LegMotion.Chamber || lifted!=Side) return;
            SwingLift=lift;KneeBend=Side==0?bendL:bendR;
            chamberBend=Mathf.Max(chamberBend,KneeBend);
            Vector3 foot=L(27+Side)-L(23+Side);
            Vector3 footVelocity=(L(27+Side)-previous.joints[27+Side])/dt;
            float kneeForwardSpeed=Vector3.Dot((L(25+Side)-previous.joints[25+Side])/dt,preparationForward);
            peakKneeForward=Mathf.Max(peakKneeForward,Vector3.Dot(L(25+Side),preparationForward));
            peakKneeSpeed=Mathf.Max(peakKneeSpeed,kneeForwardSpeed);peakLift=Mathf.Max(peakLift,lift);
            Vector3 right=Vector3.ProjectOnPlane(L(24)-L(23),Vector3.up).normalized;
            if(right.sqrMagnitude<.5f) right=Vector3.right;
            Vector3 forward=Vector3.Cross(right,Vector3.up);
            float forwardFoot=Vector3.Dot(foot,forward),sideFoot=Vector3.Dot(foot,right);
            float forwardSpeed=Vector3.Dot(footVelocity,forward);
            float extension=chamberBend-KneeBend;
            // A partially unfolding bent knee is still provisional. Require a
            // substantially extended leg before choosing a foot-strike direction.
            bool unfolding=KneeBend<60 && (extension>25 || KneeBend<45);
            bool extending=lift>=peakLift-.06f && unfolding && lift>.12f;
            if(extending && Mathf.Abs(sideFoot)>.3f && Mathf.Abs(sideFoot)>Mathf.Abs(forwardFoot)*1.1f
                && Vector3.Dot(footVelocity,right)*Mathf.Sign(sideFoot)>.5f)
                Commit(LegMotion.SideKick,frame,now);
            else if(extending && forwardFoot>.24f && forwardSpeed>.6f)
                Commit(LegMotion.FrontKick,frame,now);
            else if(extending && extension>35 && lift>.2f && footSpeed>.7f
                && foot.magnitude>(thighReference+shinReference)*.65f
                && Vector3.Dot(footVelocity,foot.normalized)>.4f)
                Commit(LegMotion.Kick,frame,now);
            // The rise of a held knee and a kick chamber can look identical to
            // a knee drive. Track it immediately, but require a short bent-leg
            // return before scoring. A held lift has no such short return.
            else if(KneeBend>65 && chamberBend>65 && peakKneeSpeed>.6f && peakKneeForward-groundKneeForward>.2f
                && peakLift>.25f && peakLift-lift>.08f && frame.captured-chamberCaptured<.75)
                Commit(LegMotion.Knee,frame,now);
            else Reason="Tracking knee lift / hold";

        }
        private void RejectLegs(double now,string reason)
        {
            VerifiedLegs=false;
            if(double.IsNaN(invalidSince)) invalidSince=now;
            Reason=reason+": holding last reliable pose";
            if(now-invalidSince>.12 || !legPoseHave)
            {
                Cancel(now,reason);haveGround=false;groundedSamples=0;rearmed=false;
                releasedFoot[0]=releasedFoot[1]=false;
            }
        }
        private void Commit(LegMotion kind,Snapshot frame,double now)
        {
            State=StrikeKind=kind;started=now;Phase=0;rearmed=false;
            Reason="Observed " + kind;Decision?.Invoke(kind.ToString(),Side,Action,frame.captured,now);
        }
        private void Cancel(double now,string reason)
        {
            Reason=reason;
            if(State==LegMotion.Chamber || IsStrike)
            {
                Decision?.Invoke("cancel",Side,Action,lastCapture,now);
                recoverPhase=Phase;State=LegMotion.Recovering;started=now;spent=now;
            }
        }
        public void Tick(double now)
        {
            if(!double.IsNaN(invalidSince) && now-invalidSince>.12)
            { Cancel(now,"Leg tracking lost");haveGround=false;groundedSamples=0;rearmed=false;releasedFoot[0]=releasedFoot[1]=false; }
            if(!VerifiedLegs && !double.IsNaN(invalidSince) && now-invalidSince>.12)
                Squat=Mathf.MoveTowards(Squat,0,Time.unscaledDeltaTime*.8f);
            if(!Fresh(now)) { releasedFoot[0]=releasedFoot[1]=false;Cancel(now,"Tracking lost: returning to stance");Squat=Mathf.MoveTowards(Squat,0,Time.unscaledDeltaTime*.8f); }
            if(IsStrike)
            {
                Phase=Mathf.Clamp01((float)(now-started)/.46f);
                if(Phase>=1) { State=LegMotion.Grounded;Phase=0;spent=now; }
            }
            else if(State==LegMotion.Recovering)
            {
                Phase=Mathf.Lerp(recoverPhase,1,Mathf.Clamp01((float)(now-started)/.2f));
                if(now-started>=.2) { State=LegMotion.Grounded;Phase=0; }
            }
        }
        // Causal interpolation from preparation to contact and back; no future pose samples.
        public static float StrikeEnvelope(float phase) => phase<.42f
            ? Mathf.SmoothStep(0,1,phase/.42f) : 1-Mathf.SmoothStep(0,1,(phase-.42f)/.58f);
    }
}
