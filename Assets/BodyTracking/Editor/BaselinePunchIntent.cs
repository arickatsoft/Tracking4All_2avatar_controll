using System;
using UnityEngine;
namespace FitFight.Practice {
    public sealed class BaselinePunchIntent
    {
        public enum Signal { None, Prepare, Commit, Cancel }
        public enum State { Unarmed, Guard, Candidate, Spent }
        public State Phase { get; private set; }
        public float Speed { get; private set; }
        public float Travel { get; private set; }
        public string Reason { get; private set; } = "Show arms";
        public float Sensitivity = 1;
        private PunchArmObservation previous, origin;
        private Vector2 direction;
        private PunchArmObservation extended;
        private bool sweeping;
        private bool inhibited;
        private double last = double.NegativeInfinity, candidateAt, guardAt = double.NaN, spentAt;
        private long lastSequence = -1;

        public void Reset()
        {
            Phase = State.Unarmed; last = double.NegativeInfinity; lastSequence = -1;
            guardAt = double.NaN; Speed = Travel = 0; Reason = "Show arms"; sweeping = inhibited = false;
        }
        // The optional inhibition comes from the paired-arm gate, before either hand can commit.
        public Signal Observe(PunchArmObservation value, long sequence, double time, string inhibition = null)
        {
            if (sequence <= lastSequence || time <= last) return Signal.None;
            lastSequence = sequence;
            float dt = (float)(time - last); last = time;
            bool cancel = Phase == State.Candidate;
            if (!value.Valid || !previous.Valid || dt > .25f || dt <= 0)
            {
                Phase = State.Unarmed; guardAt = double.NaN; previous = value;
                Speed = Travel = 0; inhibited = false;
                Reason = !value.Valid ? "Show hand and elbow" : "Tracking arms";
                return cancel ? Signal.Cancel : Signal.None;
            }
            if (inhibition != null)
            {
                // Shared gestures suspend recognition, not the camera's valid arm history.
                // Keep a bent return reference so an immediate follow-up need not settle.
                inhibited = true; previous = value; Speed = Travel = 0; guardAt = double.NaN;
                if (value.InGuard && value.Bend > 60) { Phase = State.Guard; origin = value; }
                else Phase = State.Unarmed;
                Reason = inhibition; return cancel ? Signal.Cancel : Signal.None;
            }
            if (inhibited)
            {
                inhibited = false;
                // Seed from the last suppressed observation; it cannot itself commit.
                if (previous.InGuard && previous.Bend > 60) { Phase = State.Guard; origin = previous; }
            }
            var prior = previous; previous = value;
            Vector2 step = value.wrist - prior.wrist;
            Speed = step.magnitude / Mathf.Max(.008f, dt);
            float elbowSpeed = (value.elbow - prior.elbow).magnitude / Mathf.Max(.008f, dt);
            if (Speed > 14 || elbowSpeed > 12)
            {
                Phase = State.Unarmed; guardAt = double.NaN;
                Reason = "Tracking jump · hold briefly";
                return cancel ? Signal.Cancel : Signal.None;
            }
            float threshold = Mathf.Clamp(Sensitivity, .65f, 1.5f);
            if (Phase == State.Unarmed || Phase == State.Spent)
            {
                // The reference is a natural arm pose, not a chin-height zone. Following a
                // strike, wait for a real return trajectory or a settled folded arm elsewhere.
                if (Phase == State.Spent && value.Bend < extended.Bend) extended = value;
                bool retracting = Phase == State.Spent && time - spentAt > .12 &&
                    value.Bend > extended.Bend + 20 &&
                    Vector2.Dot(value.wrist - extended.wrist, direction) < -.075f;
                if (retracting)
                {
                    Phase = State.Guard; origin = value; guardAt = double.NaN;
                    Reason = "Ready"; return Signal.None;
                }
                bool returned = Phase != State.Spent || (time - spentAt > .18 &&
                    ((Vector2.Distance(value.wrist, origin.wrist) < .25f && value.Bend >= origin.Bend - 20) ||
                     (time - spentAt > .25 && Speed < .6f && value.Bend > 60) ||
                     (time - spentAt > .8 && value.Bend > 90)));
                if (value.InGuard && Speed < 1 && returned)
                {
                    if (double.IsNaN(guardAt)) guardAt = time;
                    if (time - guardAt >= .08)
                    { Phase = State.Guard; origin = value; Reason = "Ready"; }
                    else Reason = "Tracking arms";
                }
                else { guardAt = double.NaN; Reason = "Tracking recovery"; }
                return Signal.None;
            }
            Vector2 delta = value.wrist - origin.wrist;
            Travel = delta.magnitude;
            float opening = prior.Bend - value.Bend;
            float bendChange = origin.Bend - value.Bend;
            if (!value.InStrikeBand)
                return LockReturn(time, "Tracking recovery", cancel);
            if (Phase == State.Guard)
            {
                // Follow an arm being chambered at any height, including from hanging arms.
                // Keep the most folded recent reference; opening must move away from it.
                if (value.Bend > origin.Bend + 6 ||
                    (value.Bend > 60 && value.wrist.magnitude < origin.wrist.magnitude - .025f))
                { origin = value; Reason = "Ready"; return Signal.None; }
                if (Speed < .45f && value.InGuard && Travel < .12f && bendChange < 8)
                { origin = value; Reason = "Ready"; return Signal.None; }
                // A wrist moving predominantly downward from origin is an arm drop, not a strike.
                bool dropping = (delta.y < -.15f && Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 1.2f) ||
                    (step.y < -Mathf.Max(.025f, .4f * dt) && Mathf.Abs(step.y) > Mathf.Abs(step.x) * 1.2f);
                bool extending = !dropping && opening > Mathf.Max(2, 25 * dt) && bendChange > 6 &&
                    (value.wrist.y > value.elbow.y - .1f || Mathf.Abs(delta.x) > Mathf.Abs(delta.y));
                // Hooks can retain their bend. Require a lateral, coordinated upper-arm
                // drive rather than treating arbitrary elbow speed as extension evidence.
                Vector2 elbowDelta = value.elbow - origin.elbow;
                bool sweep = Mathf.Abs(delta.x) > .12f && Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.2f &&
                    Speed > 1.7f * threshold && elbowDelta.magnitude > .08f &&
                    Vector2.Dot(delta.normalized, elbowDelta.normalized) > .7f && Mathf.Abs(bendChange) < 20;
                if (Speed >= 1.2f * threshold && Travel >= .075f * threshold && (extending || sweep))
                {
                    sweeping = sweep && !extending;
                    Phase = State.Candidate; candidateAt = time; direction = step.normalized;
                    Reason = "Preparing"; return Signal.Prepare;
                }
                Reason = "Tracking arms";
                return Signal.None;
            }
            bool reversed = Vector2.Dot(step, direction) < -.025f || (!sweeping && opening < -5);
            if (reversed || time - candidateAt > .25)
                return LockReturn(time, reversed ? "Reversed" : "Preparation timed out", true);
            // Still needs a second independent camera observation. A fast punch may already
            // be held extended at that sample; do not require it to keep moving indefinitely.
            bool extendedHold = Travel >= .3f * threshold && bendChange > 30 && opening >= -3;
            float reachGain = value.wrist.magnitude - origin.wrist.magnitude;
            // The first sample is provisional. Later independent straightening evidence
            // can resolve a hook-like onset into extension without starting another action.
            if (sweeping && opening > 6 && bendChange > 25 && reachGain > .10f && Speed >= .7f
                && !(step.y < -.025f && Mathf.Abs(step.y) > Mathf.Abs(step.x) * 1.2f))
            { sweeping = false; direction = delta.normalized; }
            Vector2 elbowTravel = value.elbow - origin.elbow;
            bool sweepConfirmed = sweeping && Travel >= .25f * threshold && elbowTravel.magnitude > .12f &&
                Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.2f && Mathf.Abs(bendChange) < 25 &&
                Vector2.Dot(delta.normalized, elbowTravel.normalized) > .7f && Speed > 1.2f;
            bool extensionConfirmed = !sweeping && Travel >= .16f * threshold && bendChange > 12 &&
                reachGain > .05f && (Speed >= .7f || extendedHold) &&
                !(delta.y < -.15f && Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 1.2f);
            if (extensionConfirmed || sweepConfirmed)
            {
                LockReturn(time, "Committed", false);
                return Signal.Commit;
            }
            if (Speed < .25f) return LockReturn(time, "Preparation stopped", true);
            return Signal.None;
        }
        private Signal LockReturn(double time, string reason, bool cancel)
        {
            if (cancel)
            {
                // A withdrawn preparation never hit. Follow its folded return rather than
                // imposing a completed-strike lock on the next outgoing stroke.
                Phase = previous.InGuard && previous.Bend > 60 ? State.Guard : State.Unarmed;
                origin = previous; guardAt = double.NaN; Reason = reason;
                return Signal.Cancel;
            }
            extended = previous; Phase = State.Spent; spentAt = time; guardAt = double.NaN; Reason = reason;
            return Signal.None;
        }
    }

    /// <summary>Reject shared raise/lower gestures before either arm publishes an action.</summary>
    public sealed class BaselinePunchGestureGate
    {
        public bool Blocked { get; private set; }
        public string Reason => "Move one arm at a time";
        private PunchCameraInput.Frame previous;
        private bool hasPrevious;
        private double settledAt = double.NaN;
        public void Reset() { Blocked = hasPrevious = false; settledAt = double.NaN; }
        public void Observe(PunchCameraInput.Frame frame)
        {
            if (hasPrevious && (frame.sequence <= previous.sequence || frame.time <= previous.time)) return;
            float dt = hasPrevious ? (float)(frame.time - previous.time) : 0;
            var left = frame.left; var right = frame.right;
            Vector2 dl = left.wrist - previous.left.wrist, dr = right.wrist - previous.right.wrist;
            bool paired = dt > 0 && dt <= .25f && left.Valid && right.Valid && previous.left.Valid && previous.right.Valid;
            // Both arms must be moving at comparable speeds for bilateral detection.
            // A much faster arm indicates a one-armed strike, not a shared raise/lower.
            float dlSpeed = paired ? dl.magnitude / Mathf.Max(.008f, dt) : 0;
            float drSpeed = paired ? dr.magnitude / Mathf.Max(.008f, dt) : 0;
            bool symmetric = Mathf.Min(dlSpeed, drSpeed) > Mathf.Max(dlSpeed, drSpeed) * .3f;
            bool lowering = paired && symmetric && dl.y < -Mathf.Max(.012f, .55f * dt) && dr.y < -Mathf.Max(.012f, .55f * dt) &&
                Mathf.Abs(dl.y) > Mathf.Abs(dl.x) * .8f && Mathf.Abs(dr.y) > Mathf.Abs(dr.x) * .8f;
            bool raising = paired && symmetric && dl.y > Mathf.Max(.012f, .7f * dt) && dr.y > Mathf.Max(.012f, .7f * dt) &&
                dl.y > Mathf.Abs(dl.x) * 1.2f && dr.y > Mathf.Abs(dr.x) * 1.2f;
            bool raised = left.Valid && right.Valid && left.wrist.y > .55f && right.wrist.y > .55f;
            if (lowering || raising || raised) { Blocked = true; settledAt = double.NaN; }
            else if (Blocked)
            {
                bool settling = paired && left.InGuard && right.InGuard && dlSpeed < 1 && drSpeed < 1;
                // When one arm clearly moves alone, the bilateral reason no longer applies.
                bool oneArmed = paired && !symmetric && (left.InGuard || right.InGuard);
                if (settling || oneArmed)
                {
                    if (double.IsNaN(settledAt)) settledAt = frame.time;
                    if (frame.time - settledAt >= (settling ? .12 : .08)) Blocked = false;
                }
                else settledAt = double.NaN;
            }
            previous = frame; hasPrevious = true;
        }
    }

}
