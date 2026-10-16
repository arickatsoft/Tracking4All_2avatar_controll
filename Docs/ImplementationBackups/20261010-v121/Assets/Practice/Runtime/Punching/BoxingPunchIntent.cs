using UnityEngine;

namespace FitFight.Practice
{
    public static class BoxingFeatures
    {
        public static bool Finite(Vector3 p) => !float.IsNaN(p.x+p.y+p.z) && !float.IsInfinity(p.x+p.y+p.z);
        public static PunchArmObservation AddWorld(PunchArmObservation arm, Vector3 shoulder, Vector3 other,
            Vector3 hip, Vector3 elbow, Vector3 wrist, float handConfidence, float elbowConfidence)
        {
            Vector3 torso=(shoulder+other)*.5f-hip;
            float scale=torso.magnitude;
            Vector3 up=torso.normalized;
            Vector3 across=Vector3.ProjectOnPlane(other-shoulder,up).normalized;
            Vector3 forward=Vector3.Cross(across,up).normalized;
            // Camera-facing axis has the same sign for either arm and mirrored joint order.
            if(forward.z>0) forward=-forward;
            Vector3 right=Vector3.Cross(up,forward).normalized;
            arm.hasWorld=scale>.15f && scale<1.2f && across.sqrMagnitude>.5f && forward.sqrMagnitude>.5f
                && Finite(torso) && Finite(elbow) && Finite(wrist);
            if(!arm.hasWorld) return arm;
            Vector3 Local(Vector3 p) { var d=(p-shoulder)/scale;return new Vector3(Vector3.Dot(d,right),Vector3.Dot(d,up),Vector3.Dot(d,forward)); }
            arm.wristWorld=Local(wrist);arm.elbowWorld=Local(elbow);
            arm.handConfidence=handConfidence;arm.elbowConfidence=elbowConfidence;
            return arm;
        }
        public static bool HandVisible(PunchArmObservation arm) => arm.hasWorld && arm.handConfidence>=.5f
            && PunchArmObservation.Finite(arm.wrist) && Finite(arm.wristWorld) && arm.wristWorld.magnitude<1.8f;
        public static bool Guard(PunchArmObservation arm) => HandVisible(arm) && arm.elbowConfidence>=.5f
            && (arm.WorldBend>65 || arm.Bend>90) && arm.wrist.y>-.45f && arm.wrist.y<.65f;
        // Two clear overhead arms are distinct. A single upward extension is ambiguous
        // from a floor camera and defaults to a punch in this game after guard acquisition.
        public static bool Overhead(PunchArmObservation left, PunchArmObservation right)
            => HandVisible(left) && HandVisible(right) && left.wrist.y>.75f && right.wrist.y>.75f;
    }

    /// <summary>Boxing prior with observed image/depth evidence; no commits from prediction or render ticks.</summary>
    public sealed class BoxingPunchIntent
    {
        public PunchIntent.State Phase { get; private set; }
        public string Reason { get; private set; }="HOLD GUARD · FISTS NEAR CHIN";
        public float Speed { get; private set; }
        public float Travel { get; private set; }
        public bool GuardLearned { get; private set; }
        public bool RecoveringTracking { get; private set; }
        private PunchArmObservation previous,origin,guard;
        private long lastSequence=-1;
        private double last=double.NegativeInfinity,guardAt=double.NaN,candidateAt,spentAt;
        private Vector2 direction;
        private bool havePrevious;
        private int recoverySamples;
        private double recoveryAt;
        public void Reset()
        {
            Phase=PunchIntent.State.Unarmed;GuardLearned=havePrevious=false;
            lastSequence=-1;last=double.NegativeInfinity;guardAt=double.NaN;Speed=Travel=0;
            RecoveringTracking=false;recoverySamples=0;
            Reason="HOLD GUARD · FISTS NEAR CHIN";
        }
        public PunchIntent.Signal Observe(PunchArmObservation arm,long sequence,double time,bool overhead=false)
        {
            if(double.IsNaN(time) || double.IsInfinity(time) || sequence<=lastSequence || time<=last) return PunchIntent.Signal.None;
            double dt=time-last;last=time;lastSequence=sequence;
            bool candidate=Phase==PunchIntent.State.Candidate;
            if(!BoxingFeatures.HandVisible(arm) || dt>.25 || !havePrevious)
            {
                Phase=PunchIntent.State.Unarmed;guardAt=double.NaN;Speed=Travel=0;
                previous=arm;havePrevious=BoxingFeatures.HandVisible(arm);
                // Loss/gaps require a new observed guard; never complete a guessed punch.
                GuardLearned=RecoveringTracking=false;recoverySamples=0;Reason="HOLD GUARD · SHOW FISTS AND ELBOWS";
                return candidate ? PunchIntent.Signal.Cancel : PunchIntent.Signal.None;
            }
            var prior=previous;previous=arm;
            Vector2 step=arm.wrist-prior.wrist;
            Speed=step.magnitude/(float)dt;
            float worldSpeed=Vector3.Distance(arm.wristWorld,prior.wristWorld)/(float)dt;
            if(RecoveringTracking)
            {
                // A single bad frame is not a new person/stance. Require two
                // observed returns near the trusted guard before accepting motion.
                // Paired lowering may still be gated while a hand returns to
                // guard. Recovery itself never commits; the next stroke must
                // independently pass the shared-gesture gate below.
                bool near=BoxingFeatures.Guard(arm) && Vector2.Distance(arm.wrist,guard.wrist)<.50f
                    && Vector3.Distance(arm.wristWorld,guard.wristWorld)<.55f;
                recoverySamples=near ? recoverySamples+1 : 0;
                if(recoverySamples>=2 && Speed<1.1f && worldSpeed<2)
                { RecoveringTracking=false;Phase=PunchIntent.State.Guard;origin=arm;Reason="READY · GUARD RECOVERED"; }
                else if(time-recoveryAt>.5)
                { RecoveringTracking=GuardLearned=false;guardAt=double.NaN;Reason="HOLD GUARD · SHOW FISTS AND ELBOWS"; }
                else Reason="TRACKING JUMP · RETURN TO GUARD";
                return PunchIntent.Signal.None;
            }
            if(Speed>14 || worldSpeed>14)
            {
                if(GuardLearned) { Phase=PunchIntent.State.Unarmed;RecoveringTracking=true;recoveryAt=time;recoverySamples=0; }
                else ResetHistory(arm);
                Reason="TRACKING JUMP · HOLD GUARD";return candidate?PunchIntent.Signal.Cancel:PunchIntent.Signal.None;
            }
            if(overhead)
            { Phase=PunchIntent.State.Unarmed;guardAt=double.NaN;Reason="OVERHEAD GESTURE";return candidate?PunchIntent.Signal.Cancel:PunchIntent.Signal.None; }
            if(!GuardLearned)
            {
                if(BoxingFeatures.Guard(arm) && Speed<.8f && worldSpeed<1.2f)
                {
                    if(double.IsNaN(guardAt)) guardAt=time;
                    if(time-guardAt>=.25)
                    { guard=origin=arm;GuardLearned=true;Phase=PunchIntent.State.Guard;Reason="READY · BOXING GUARD"; }
                }
                else guardAt=double.NaN;
                return PunchIntent.Signal.None;
            }
            if(Phase==PunchIntent.State.Spent)
            {
                // The same hand must visibly fold back; no global cooldown blocks the other hand.
                if(time-spentAt>=.10 && BoxingFeatures.Guard(arm) &&
                    (Vector2.Distance(arm.wrist,guard.wrist)<.30f || arm.Bend>150))
                { Phase=PunchIntent.State.Guard;origin=arm;Reason="READY · RETURNED TO GUARD"; }
                return PunchIntent.Signal.None;
            }
            if(Phase==PunchIntent.State.Unarmed)
            {
                if(BoxingFeatures.Guard(arm)) { origin=arm;Phase=PunchIntent.State.Guard; }
                return PunchIntent.Signal.None;
            }
            Vector2 delta=arm.wrist-origin.wrist;
            Travel=delta.magnitude;
            bool elbow=arm.elbowConfidence>=.35f && prior.elbowConfidence>=.35f;
            float imageOpening=elbow ? origin.Bend-arm.Bend : 0;
            float opening=elbow ? Mathf.Max(origin.Bend-arm.Bend,origin.WorldBend-arm.WorldBend) : 0;
            float forward=arm.wristWorld.z-origin.wristWorld.z;
            float reach=arm.wristWorld.magnitude-origin.wristWorld.magnitude;
            Vector2 ed=arm.elbow-origin.elbow;
            bool hook=elbow && Speed>=1.6f && Mathf.Abs(delta.x)>.18f && Mathf.Abs(delta.x)>Mathf.Abs(delta.y)*1.3f
                && ed.magnitude>.06f && Vector2.Dot(delta.normalized,ed.normalized)>.65f;
            bool extension=opening>15 || forward>.12f && reach>.07f;
            // In boxing mode an observed upward stroke from guard is a punch
            // even when image/depth foreshortening cannot distinguish an arm lift.
            bool rising=delta.y>.24f && step.y>.04f && Speed>=1.1f;
            // A rising fist can be a straight punch seen from below. Falling hands are recovery.
            bool dropping=delta.y<-.18f && Mathf.Abs(delta.y)>Mathf.Abs(delta.x)*1.5f && forward<.12f;
            bool evidence=!dropping && (extension || hook || rising) && Speed>=1.1f && Travel>=.10f;
            // One strong independent observation already supports a punch; no extra endpoint wait.
            bool decisive=evidence && (Travel>=.22f && opening>30 || rising
                || forward>.18f && reach>.10f && Travel>.10f && Speed>=1.6f || hook && Travel>.28f);
            if(Phase==PunchIntent.State.Guard)
            {
                if(decisive) return Commit(time,"BOXING EXTENSION");
                if(evidence)
                { Phase=PunchIntent.State.Candidate;candidateAt=time;direction=delta.normalized;Reason="BOXING PREPARATION";return PunchIntent.Signal.Prepare; }
                if(BoxingFeatures.Guard(arm) && (arm.Bend>origin.Bend+8 || Speed<.5f && Travel<.12f)) origin=arm;
                Reason="READY · BOXING GUARD";return PunchIntent.Signal.None;
            }
            // A slow stroke can reach its endpoint between model samples.
            // Corroborating world unfolding supports a foreshortened image;
            // a still-folded support hand does not satisfy this exception.
            bool clearEndpoint=elbow && (arm.Bend<90 || arm.Bend<120 && arm.WorldBend<75)
                && imageOpening>30 && Travel>.16f;
            // Initial image motion can point the wrong way during floor-view
            // foreshortening. A strong unfolded endpoint plus increased world
            // reach takes precedence over that tentative image direction.
            if(decisive && clearEndpoint && reach>.07f) return Commit(time,"BOXING ENDPOINT");
            bool reversed=Vector2.Dot(step,direction)<-.05f;
            bool unfolding=elbow && opening>30 && reach>.07f && Speed>=1.6f && Travel>.22f;
            if(reversed && !unfolding || time-candidateAt>.22 || dropping)
                return Cancel(time,"WITHDRAWN");
            bool progressing=Speed>=.8f && Vector2.Dot(step,direction)>.02f;
            bool stroke=Speed>=1.6f || clearEndpoint || rising || hook;
            if((extension || hook || rising) && Travel>.16f && !dropping && stroke && (progressing || clearEndpoint))
                return Commit(time,elbow ? "BOXING CONFIRMED" : "BOXING PARTIAL ARM");
            if(!progressing && !clearEndpoint && Speed<.8f)
                return Cancel(time,"SETTLED OR RETURNING");
            Reason="BOXING PREPARATION";return PunchIntent.Signal.None;
        }
        private PunchIntent.Signal Commit(double time,string reason)
        { Phase=PunchIntent.State.Spent;spentAt=time;Reason=reason;return PunchIntent.Signal.Commit; }
        private PunchIntent.Signal Cancel(double time,string reason)
        {
            // A withdrawal/timeout can be sampled with the arm still extended.
            // Do not adopt that endpoint as a new guard and punch on its return.
            Phase=PunchIntent.State.Spent;spentAt=time;Reason=reason;return PunchIntent.Signal.Cancel;
        }
        private void ResetHistory(PunchArmObservation arm)
        { GuardLearned=RecoveringTracking=false;Phase=PunchIntent.State.Unarmed;guardAt=double.NaN;previous=arm;havePrevious=true; }
    }
}
