using System;
using UnityEngine;
using Tracking4All;

namespace FitFight.Practice
{
    /// <summary>Observed leg intent plus a bounded strike. Render prediction never commits an action.</summary>
    public sealed class GameBodyMotion115
    {
        public enum LegMotion { Grounded, Chamber, FrontKick, SideKick, Knee, Recovering }
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
        public bool ContactWindow(double now) => Fresh(now) && IsStrike && Phase>=.2f && Phase<.62f && Reliable(Side) && Reliable(1-Side);
        public bool IsStrike => State==LegMotion.FrontKick || State==LegMotion.SideKick || State==LegMotion.Knee;
        public int ContactLimb => (StrikeKind==LegMotion.Knee ? 4 : 2)+Side;
        public string Reason { get; private set; } = "Show both feet";
        public Snapshot Latest { get; private set; }
        public event Action<string,int,long,double,double> Decision;
        private Snapshot previous;
        private bool have;
        private long sequence=-1;
        private double lastCapture=double.NegativeInfinity,lastReceived=double.NegativeInfinity,started,spent;
        private float chamberBend, chamberKneeForward, groundKneeForward;
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
            lastCapture=lastReceived=double.NegativeInfinity;Phase=Squat=SwingLift=0;rearmed=true;groundedSamples=0;
        }
        public void Observe(Snapshot frame,double now)
        {
            if(frame.sequence<=sequence || frame.captured<=lastCapture || frame.joints==null || frame.joints.Length<33
                || frame.confidence==null || frame.confidence.Length<33 || now-frame.captured>.3 || now-frame.received>.23) return;
            float dt=(float)(frame.captured-lastCapture);
            previous=Latest;Latest=frame;sequence=frame.sequence;lastCapture=frame.captured;lastReceived=frame.received;
            bool usable=have && dt>0 && dt<=.25f;have=true;
            if(!Reliable(0) || !Reliable(1)) { Cancel(now,"Leg confidence lost");haveGround=false;groundedSamples=0;rearmed=false;releasedFoot[0]=releasedFoot[1]=false;return; }
            Vector3 L(int joint) => frame.joints[joint];
            float ankleDifference=L(27).y-L(28).y;
            int lifted=ankleDifference>=0?0:1;
            float lift=Mathf.Abs(ankleDifference);
            for(int s=0;s<2;s++)
            {
                float observedLift=s==0?ankleDifference:-ankleDifference;
                releasedFoot[s]=observedLift>(releasedFoot[s]?.035f:.075f);
            }
            float bendL=Bend(L(23),L(25),L(27)),bendR=Bend(L(24),L(26),L(28));
            float hipHeight=-Mathf.Min(L(27).y,L(28).y);
            float standing=frame.standingHipHeight>.4f ? frame.standingHipHeight : .9f;
            Squat=Mathf.Clamp(standing-hipHeight,0,.48f);
            if(!usable) { Cancel(now,"Establish fresh stance");haveGround=false;groundedSamples=0;rearmed=false;releasedFoot[0]=releasedFoot[1]=false; }
            bool planted=lift<.055f;
            if(planted)
            {
                for(int s=0;s<2;s++) groundKnees[s]=L(25+s);
                groundedSamples++;haveGround=groundedSamples>=2;
                if(!IsStrike && State!=LegMotion.Recovering)
                { State=LegMotion.Grounded;Phase=0;Reason="Feet grounded"; }
                if(haveGround && !IsStrike && State!=LegMotion.Recovering && now-spent>.25) rearmed=true;
                return;
            }
            groundedSamples=0;
            if(!usable || !haveGround) { Reason="Establish two-foot stance";return; }
            // Bilateral crouches and tracking jumps must never become leg attacks.
            float kneeSpeed=Vector3.Distance(L(25+lifted),previous.joints[25+lifted])/dt;
            float footSpeed=Vector3.Distance(L(27+lifted),previous.joints[27+lifted])/dt;
            if(kneeSpeed>7 || footSpeed>10 || lift>.95f)
            { Cancel(now,"Leg tracking jump");haveGround=false;rearmed=false;return; }
            if(State==LegMotion.Grounded && rearmed && lift>.075f && Squat<.3f)
            {
                Side=lifted;Action++;State=LegMotion.Chamber;started=now;Phase=0;
                chamberBend=Side==0?bendL:bendR;
                groundKneeForward=groundKnees[Side].z;
                chamberKneeForward=L(25+Side).z;
                Reason="Observed leg preparation";Decision?.Invoke("prepare",Side,Action,frame.captured,now);
            }
            if(State!=LegMotion.Chamber || lifted!=Side) return;
            SwingLift=lift;KneeBend=Side==0?bendL:bendR;
            chamberBend=Mathf.Max(chamberBend,KneeBend);
            Vector3 foot=L(27+Side)-L(23+Side);
            float forwardSpeed=(L(27+Side).z-previous.joints[27+Side].z)/dt;
            float kneeForwardSpeed=(L(25+Side).z-previous.joints[25+Side].z)/dt;
            float extension=chamberBend-KneeBend;
            bool unfolding=extension>25 || KneeBend<45;
            if(unfolding && foot.z>.24f && forwardSpeed>.6f && lift>.12f)
                Commit(LegMotion.FrontKick,frame,now);
            else if(unfolding && Mathf.Abs(foot.x)>.3f && footSpeed>.7f && lift>.12f)
                Commit(LegMotion.SideKick,frame,now);
            else if(KneeBend>65 && kneeForwardSpeed>.9f && L(25+Side).z-groundKneeForward>.2f
                && L(25+Side).z-chamberKneeForward>.09f && lift>.14f && now-started>.055)
                Commit(LegMotion.Knee,frame,now);
            else Reason="Tracking knee lift / hold";

        }
        private void Commit(LegMotion kind,Snapshot frame,double now)
        {
            State=StrikeKind=kind;started=now;Phase=0;rearmed=false;
            Reason="Observed " + kind;Decision?.Invoke(kind.ToString(),Side,Action,frame.captured,now);
        }
        private void Cancel(double now,string reason)
        {
            if(State==LegMotion.Chamber || IsStrike)
            {
                Decision?.Invoke("cancel",Side,Action,lastCapture,now);
                recoverPhase=Phase;State=LegMotion.Recovering;started=now;spent=now;
            }
            Reason=reason;
        }
        public void Tick(double now)
        {
            if(!Reliable(0) || !Reliable(1)) Squat=Mathf.MoveTowards(Squat,0,Time.unscaledDeltaTime*.8f);
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
