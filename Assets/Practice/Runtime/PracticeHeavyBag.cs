using System;
using UnityEngine;

namespace FitFight.Practice
{
    /// <summary>Contact-only hits with continuous swept limb volumes and per-limb rearming.</summary>
    public sealed class PracticeHeavyBag : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public CapsuleCollider Surface { get; private set; }
        public int HitCount { get; private set; }
        public int EffectCount { get; private set; }
        public int SoundCount { get; private set; }
        public string LastHit { get; private set; } = "READY";
        public event Action Hit;
        [Serializable] public struct ContactSample
        {
            public Vector3 from, to, point;
            public Vector3 normal, relativeVelocity;
            public float gap, speed;
            public bool tracked, allowImpact, hit;
            public string reason;
        }
        private readonly ContactSample[] samples = new ContactSample[6];
        public ContactSample GetContactSample(int limb) => samples[limb];
        private readonly Vector3[] previous = new Vector3[6];
        private readonly bool[] initialized = new bool[6], armed = new bool[6];
        private readonly double[] lastHit = new double[6];
        private readonly long[] assistedAction = { -1, -1, -1, -1, -1, -1 }, scoredAction = { -1, -1, -1, -1, -1, -1 };
        // Preparation seeds the sweep but cannot score. A committed action scores at most once,
        // even if the bag swings back into the fist or contact is reset mid-action.
        public bool SampleAssistedPunch(int side, long action, Vector3 position, float dt, double time, bool active, bool contactWindow)
            => SampleAssistedLimb(side,action,position,dt,time,active,contactWindow);
        public bool SampleAssistedLimb(int side,long action,Vector3 position,float dt,double time,bool active,bool contactWindow)
        {
            if (side < 0 || side >= 6) return false;
            bool newAction = assistedAction[side] != action;
            if (newAction)
            {
                initialized[side] = armed[side] = false;
                assistedAction[side] = action;
            }
            // A committed action starts armed unless the limb already overlaps the bag deeply.
            // The swinging bag can come to rest a few cm from a guard fist; a punch thrown from
            // there is a real hit. Approach direction, speed and once-per-action still apply.
            bool hit = Sample(side, position, dt, time, active, contactWindow && scoredAction[side] != action,
                newAction || !initialized[side] ? ActionSeedArm : IdleSeedArm, true);
            if (hit) scoredAction[side] = action;
            return hit;
        }
        // Foot strikes contact with the shoe tip, ball, ankle or lower shin, not only the ankle
        // joint. Each probe keeps its own sweep/rearm history, so a point sliding along the
        // limb cannot fabricate speed. The same commit, window, approach and once-per-action
        // rules apply; the action scores once even if several probes touch the surface.
        public const int SegmentProbes=4;
        private readonly Vector3[,] probePrevious=new Vector3[6,SegmentProbes];
        private readonly bool[,] probeInitialized=new bool[6,SegmentProbes],probeArmed=new bool[6,SegmentProbes];
        public static float ProbeRadius(int probe,float scale) => (probe==SegmentProbes-1 ? .07f : .095f)*scale;
        public bool SampleAssistedSegment(int limb,long action,Vector3[] probes,float dt,double time,bool active,bool contactWindow)
        {
            if(limb<2 || limb>=6 || probes==null || probes.Length<SegmentProbes) return false;
            if(assistedAction[limb]!=action)
            {
                for(int k=0;k<SegmentProbes;k++) probeInitialized[limb,k]=probeArmed[limb,k]=false;
                initialized[limb]=armed[limb]=false;assistedAction[limb]=action;
            }
            bool allowImpact=contactWindow && scoredAction[limb]!=action;
            var best=new ContactSample{ to=probes[0],tracked=active,allowImpact=allowImpact,reason="Invalid or untracked sample",gap=float.MaxValue };
            if(!active || dt<=0 || dt>.15f)
            {
                for(int k=0;k<SegmentProbes;k++) probeInitialized[limb,k]=probeArmed[limb,k]=false;
                best.gap=0;samples[limb]=best;return false;
            }
            int strikeProbe=-1;Vector3 strikePoint=Vector3.zero,strikeDelta=Vector3.zero;float strikeSpeed=0;
            string strikeReason=null;
            for(int k=0;k<SegmentProbes;k++)
            {
                Vector3 position=probes[k];
                if(!Finite(position)) { probeInitialized[limb,k]=probeArmed[limb,k]=false;continue; }
                float radius=ProbeRadius(k,scale);
                float gap=Vector3.Distance(Surface.ClosestPoint(position),position);
                var probe=new ContactSample{ to=position,tracked=true,allowImpact=allowImpact,gap=gap };
                if(!probeInitialized[limb,k])
                {
                    probePrevious[limb,k]=position;probeInitialized[limb,k]=true;probeArmed[limb,k]=gap>radius*ActionSeedArm;
                    probe.reason="Seeded sweep";if(gap<best.gap) best=probe;continue;
                }
                Vector3 start=probePrevious[limb,k],delta=position-start;probePrevious[limb,k]=position;
                float speed=delta.magnitude/dt;probe.from=start;probe.speed=speed;
                if(delta.magnitude>.85f*scale || speed>18*scale)
                { probeArmed[limb,k]=gap>radius*ActionSeedArm;probe.reason="Reseeded after authored jump";if(gap<best.gap) best=probe;continue; }
                bool contact=SweptContact(Surface,start,position,radius,out Vector3 point);
                probe.point=point;
                probe.reason=!allowImpact ? "Outside contact window or already scored" : !contact ? "No surface contact"
                    : !probeArmed[limb,k] ? "Limb not rearmed" : speed<=.3f*scale ? "Too slow" : "Contact cooldown";
                bool strike=allowImpact && contact && probeArmed[limb,k] && speed>.3f*scale && time-lastHit[limb]>.18;
                if(!contact && gap>radius*1.6f) probeArmed[limb,k]=true;
                if(strike)
                {
                    Vector3 normal=SurfaceNormal(point),relative=delta/dt-Body.GetPointVelocity(point);
                    probe.normal=normal;probe.relativeVelocity=relative;
                    if(!ApproachingSurface(relative,normal)) { probe.reason="Moving away from bag surface";strike=false; }
                }
                if(strike && strikeProbe<0) { strikeProbe=k;strikePoint=point;strikeDelta=delta;strikeSpeed=speed;best=probe; }
                else if(strikeProbe<0 && gap<best.gap) best=probe;
                if(strike) strikeReason="Accepted hit";
            }
            if(strikeProbe<0) { samples[limb]=best;return false; }
            for(int k=0;k<SegmentProbes;k++) probeArmed[limb,k]=false;
            lastHit[limb]=time;scoredAction[limb]=action;
            best.hit=true;best.reason=strikeReason+(strikeProbe==0?" (shoe tip)":strikeProbe==1?" (ball)":strikeProbe==2?" (ankle)":" (lower shin)");
            samples[limb]=best;
            ApplyHit(limb,strikePoint,strikeDelta.normalized,strikeSpeed/scale);
            return true;
        }
        private ParticleSystem sparks;
        private AudioSource audioSource;
        private AudioClip punch, kick;
        private Material particleMaterial;
        private Texture2D particleTexture;
        private float scale;
        private Vector3 rest;
        private LineRenderer[] straps;
        private Vector3 suspension;

        public void Configure(float humanHeight)
        {
            scale = humanHeight / 1.7f;
            rest = transform.position;
            Surface = gameObject.AddComponent<CapsuleCollider>();
            Surface.radius = .20f * scale;
            Surface.height = 1.22f * scale;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = 28;
            Body.linearDamping = .25f; Body.angularDamping = .65f;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.solverIterations = 12; Body.solverVelocityIterations = 6;
            // A point suspension keeps the bag attached while gravity produces a pendulum return.
            var joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.up * 1.38f * scale;
            suspension = rest + joint.anchor;
            joint.connectedAnchor = suspension;
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
            joint.lowAngularXLimit = new SoftJointLimit { limit = -42 };
            joint.highAngularXLimit = new SoftJointLimit { limit = 42 };
            joint.angularYLimit = new SoftJointLimit { limit = 25 };
            joint.angularZLimit = new SoftJointLimit { limit = 42 };
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = .025f;
            joint.enableCollision = false;
            straps = new LineRenderer[4];
            var metal = PracticeGym.Material(new Color(.39f, .40f, .42f), .7f);
            for (int i = 0; i < 4; i++)
            {
                straps[i] = new GameObject("Suspension chain").AddComponent<LineRenderer>();
                straps[i].transform.SetParent(transform, false);
                straps[i].sharedMaterial = metal;
                straps[i].startWidth = straps[i].endWidth = .009f * scale;
                straps[i].positionCount = 2;
                straps[i].numCapVertices = 3;
            }
            BuildFeedback();
        }

        private void LateUpdate()
        {
            if (straps == null) return;
            for (int i = 0; i < straps.Length; i++)
            {
                float a = i * Mathf.PI * .5f;
                straps[i].SetPosition(0, transform.TransformPoint(new Vector3(Mathf.Cos(a) * .16f, .61f, Mathf.Sin(a) * .16f) * scale));
                straps[i].SetPosition(1, suspension);
            }
        }

        public void ResetBag()
        {
            Body.position = rest; Body.rotation = Quaternion.identity;
            Body.linearVelocity = Body.angularVelocity = Vector3.zero;
            ResetContacts();
        }
        public void ResetContacts() { Array.Clear(initialized, 0, 6); Array.Clear(armed, 0, 6); Array.Clear(samples, 0, 6); Array.Clear(probeInitialized,0,probeInitialized.Length); Array.Clear(probeArmed,0,probeArmed.Length); }

        /// <returns>True only for a newly reached bag surface with a moving, tracked limb.</returns>
        public const float IdleSeedArm=1.6f, ActionSeedArm=.5f;
        public bool Sample(int limb, Vector3 position, float dt, double time, bool tracked, bool allowImpact = true, float seedArm = IdleSeedArm, bool authored = false)
        {
            if (limb < 0 || limb >= 6) return false;
            samples[limb] = new ContactSample { to=position, tracked=tracked, allowImpact=allowImpact, reason="Invalid or untracked sample" };
            float radius = (limb < 2 ? .075f : .095f) * scale;
            if (!tracked || !Finite(position) || dt <= 0 || dt > .15f)
            { initialized[limb] = armed[limb] = false; return false; }
            float gap = Vector3.Distance(Surface.ClosestPoint(position), position);
            samples[limb].gap = gap;
            if (!initialized[limb])
            { previous[limb] = position; initialized[limb] = true; armed[limb] = gap > radius * seedArm; samples[limb].reason="Seeded sweep"; return false; }
            Vector3 start = previous[limb], delta = position - start;
            previous[limb] = position;
            float speed = delta.magnitude / dt;
            samples[limb].from = start; samples[limb].speed = speed;
            // Tracking reacquisition/teleport cannot generate a strike across the whole room.
            if (delta.magnitude > .85f * scale || speed > 18 * scale)
            {
                // Tracking reacquisition disarms. An authored assisted limb jumps only from our own
                // seeding/blending (e.g. guard seed and fist-tip sample in one frame), so it restarts
                // the sweep without sweeping across the jump, armed unless it landed deep in the bag.
                armed[limb] = authored && gap > radius * ActionSeedArm;
                samples[limb].reason = authored ? "Reseeded after authored jump" : "Teleport or excessive speed"; return false;
            }
            bool contact = SweptContact(Surface, start, position, radius, out Vector3 point);
            samples[limb].point = point;
            samples[limb].reason = !allowImpact ? "Outside contact window or already scored" : !contact ? "No surface contact"
                : !armed[limb] ? "Limb not rearmed" : speed <= .3f * scale ? "Too slow" : "Contact cooldown";
            bool strike = allowImpact && contact && armed[limb] && speed > .3f * scale && time - lastHit[limb] > .18;
            if (!contact && gap > radius * 1.6f) armed[limb] = true;
            if (!strike) return false;
            Vector3 normal = SurfaceNormal(point);
            Vector3 relative = delta / dt - Body.GetPointVelocity(point);
            samples[limb].normal=normal; samples[limb].relativeVelocity=relative;
            if (!ApproachingSurface(relative, normal))
            { samples[limb].reason="Moving away from bag surface"; return false; }
            armed[limb] = false; lastHit[limb] = time;
            samples[limb].hit = true; samples[limb].reason="Accepted hit";
            ApplyHit(limb, point, delta.normalized, speed / scale);
            return true;
        }
        public static bool ApproachingSurface(Vector3 relativeVelocity, Vector3 outwardNormal)
            => Vector3.Dot(relativeVelocity, outwardNormal) < -.05f;
        private Vector3 SurfaceNormal(Vector3 point)
        {
            Vector3 center=transform.TransformPoint(Surface.center), axis=transform.up;
            float half=Mathf.Max(0, Surface.height*.5f-Surface.radius)*transform.lossyScale.y;
            Vector3 closestAxis=center+axis*Mathf.Clamp(Vector3.Dot(point-center,axis),-half,half);
            Vector3 normal=point-closestAxis;
            return normal.sqrMagnitude > .000001f ? normal.normalized : -transform.forward;
        }

        public static bool SweptContact(Collider surface, Vector3 from, Vector3 to, float radius, out Vector3 point)
        {
            // Exact closest-point overlap for endpoints, and a sphere sweep for thin/fast crossings.
            Vector3 delta = to - from;
            if (delta.sqrMagnitude > .000001f)
            {
                int count = Physics.SphereCastNonAlloc(from, radius, delta.normalized, sweepHits,
                    delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                    if (sweepHits[i].collider == surface) { point = sweepHits[i].point; return true; }
            }
            point = surface.ClosestPoint(to);
            return (point - to).sqrMagnitude <= radius * radius;
        }
        private static readonly RaycastHit[] sweepHits = new RaycastHit[32];
        private static bool Finite(Vector3 v) => !float.IsNaN(v.x + v.y + v.z) && !float.IsInfinity(v.x + v.y + v.z);

        private void ApplyHit(int limb, Vector3 point, Vector3 direction, float speed)
        {
            bool isKick = limb >= 2;
            float impulse = Mathf.Clamp(speed * (isKick ? 3.8f : 2.5f), 2.5f, isKick ? 25f : 17f);
            Body.AddForceAtPosition(direction * impulse, point, ForceMode.Impulse);
            HitCount++;
            LastHit = (limb % 2 == 0 ? "LEFT " : "RIGHT ") + (limb>=4 ? "KNEE" : isKick ? "KICK" : "PUNCH");
            sparks.transform.position = point;
            sparks.Emit(isKick ? 28 : 19); EffectCount++;
            audioSource.pitch = isKick ? .93f : 1.03f;
            audioSource.PlayOneShot(isKick ? kick : punch, Mathf.Clamp(.45f + speed * .055f, .45f, .95f)); SoundCount++;
            Hit?.Invoke();
        }

        private void BuildFeedback()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.spatialBlend = .2f;
            audioSource.minDistance = 1; audioSource.maxDistance = 15;
            punch = ImpactSound(false); kick = ImpactSound(true);
            var fx = new GameObject("Contact splash"); fx.transform.SetParent(transform.parent, false);
            sparks = fx.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main;
            main.loop = false; main.playOnAwake = false; main.duration = .4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.12f, .24f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.5f * scale, 2.1f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(.016f * scale, .045f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.12f, 1, .18f), new Color(.35f, 1, .45f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128; main.gravityModifier = .25f;
            var emission = sparks.emission; emission.enabled = false;
            var shape = sparks.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .025f * scale;
            var color = sparks.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(.3f, 1, .4f), 0), new GradientColorKey(new Color(.05f, .8f, .15f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var size = sparks.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            particleTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 15.5f;
                particleTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(3 - 3 * d)));
            }
            particleTexture.Apply();
            particleMaterial = new Material(Resources.Load<Shader>("PracticeImpact"));
            particleMaterial.mainTexture = particleTexture;
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2; renderer.velocityScale = .04f;
        }

        public static AudioClip ImpactSound(bool isKick)
        {
            const int rate = 44100;
            float duration = isKick ? .26f : .17f;
            var data = new float[(int)(rate * duration)];
            var random = new System.Random(isKick ? 73 : 41);
            float low = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float noise = (float)random.NextDouble() * 2 - 1;
                low = Mathf.Lerp(low, noise, .13f);
                float body = Mathf.Sin(2 * Mathf.PI * (isKick ? 68 : 108) * t) * Mathf.Exp(-t * (isKick ? 20 : 35));
                float snap = noise * Mathf.Exp(-t * 130) * .44f + low * Mathf.Exp(-t * 24) * .4f;
                data[i] = Mathf.Clamp((body * .65f + snap) * Mathf.Clamp01(t * 1800), -.95f, .95f);
            }
            var clip = AudioClip.Create(isKick ? "Kick leather thump" : "Punch leather snap", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
        private void OnDestroy()
        {
            if (sparks != null) Destroy(sparks.gameObject);
            if (punch != null) Destroy(punch); if (kick != null) Destroy(kick);
            if (particleMaterial != null) Destroy(particleMaterial); if (particleTexture != null) Destroy(particleTexture);
            if (straps != null && straps.Length > 0 && straps[0] != null) Destroy(straps[0].sharedMaterial);
        }
    }
}
