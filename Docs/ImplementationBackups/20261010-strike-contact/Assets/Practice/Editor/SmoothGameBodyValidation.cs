using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FitFight.Practice;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

public static class SmoothGameBodyValidation
{
    private const string Path116="Logs/MediaPipe/20261009-151451-407/input/20261009-151451-407.jsonl";
    [Serializable] private class Record
    {
        public string kind,actionKind;
        public double recordingStartedAt,recordedAt,decidedAt;
        public int side;
        public GameBodyMotion.Snapshot snapshot;
        public PunchCameraInput.Frame observation;
    }
    public struct Sample { public double elapsed;public GameBodyMotion.Snapshot body;public PunchCameraInput.Frame arms; }
    private struct Event { public string kind;public int side;public double time; }
    private static readonly StringBuilder report=new StringBuilder();
    private static int checks;
    private static void Check(bool value,string label)
    { if(!value)throw new Exception(label);checks++;report.AppendLine("PASS "+label); }
    public static List<Sample> LoadExact(bool imageHead=true)
    {
        double start=0;string savedTuning=null;var bodies=new List<Record>();var arms=new Dictionary<long,PunchCameraInput.Frame>();
        foreach(var line in File.ReadLines(Path116))
        {
            if(!line.StartsWith("{\"kind\":\"session\"") && !line.StartsWith("{\"kind\":\"body_observation\"")
                && !line.StartsWith("{\"kind\":\"observation\"")) continue;
            var r=JsonUtility.FromJson<Record>(line);
            if(r.kind=="session") { start=r.recordingStartedAt;savedTuning=JsonUtility.FromJson<TuningRecord>(line).tuning; }
            else if(r.kind=="body_observation") bodies.Add(r);
            else arms[r.observation.sequence]=r.observation;
        }
        var samples=new List<Sample>();var head=new BodyMotionEstimator();var tuning=new RTMPoseTuning();
        if(!string.IsNullOrEmpty(savedTuning)) JsonUtility.FromJsonOverwrite(savedTuning,tuning);
        var plane=new Vector3[26];var face=new Vector3[26];var scores=new float[26];
        foreach(var r in bodies)
        {
            if(!arms.TryGetValue(r.snapshot.sequence,out var arm)) throw new Exception("Missing exact arm pair "+r.snapshot.sequence);
            var body=r.snapshot;double elapsed=r.recordedAt-start;
            if(imageHead)
            {
                for(int i=0;i<26;i++)
                {
                    plane[i]=body.joints[MediaPipeWorldPose.HalpeToMediaPipe[i]];
                    face[i]=new Vector3(arm.imageJoints[i].x*arm.imageAspect,arm.imageJoints[i].y,0);
                    scores[i]=arm.jointScores[i];
                }
                tuning.observationInterval=samples.Count==0?.05f:(float)(body.captured-samples[samples.Count-1].body.captured);
                head.Observe(plane,scores,body.captured,elapsed>=10 && elapsed<16,tuning,face);
                var angles=head.HeadAngles;
                float mirror=arm.mirrored?1:-1;
                body.head=Quaternion.Euler(angles.x,angles.y*mirror,angles.z*mirror);
            }
            samples.Add(new Sample{elapsed=elapsed,body=body,arms=arm});
        }
        return samples;
    }
    [Serializable] private class TuningRecord { public string tuning; }
    public static void RunBatch()
    { try { Run();EditorApplication.Exit(0); } catch(Exception e){ Debug.LogException(e);EditorApplication.Exit(1); } }
    public static void Run()
    {
        checks=0;report.Clear();
        try
        {
            Smoothing();Quality();Head();Recorded();
            report.AppendLine($"PASS {checks} smooth-body checks. Recorded cues are weak labels; replay is not phone/display latency validation.");
        }
        catch(Exception e){report.AppendLine("FAIL "+e);throw;}
        finally { Directory.CreateDirectory("Logs/Practice");File.WriteAllText("Logs/Practice/smooth-game-body-validation.txt",report.ToString()); }
    }
    private static void Smoothing()
    {
        foreach(float fps in new[]{30f,60f,120f})
        {
            Quaternion q=Quaternion.identity;Vector3 velocity=Vector3.zero;float peak=0;bool finite=true;
            for(int i=0;i<(int)fps;i++)
            {
                var previous=q;q=AvatarMotionSmoothing.Rotation(q,Quaternion.Euler(45,20,0),ref velocity,.07f,.028f,1/fps);
                peak=Mathf.Max(peak,Quaternion.Angle(previous,q));
                finite&=BoxingFeatures.Finite(new Vector3(q.x,q.y,q.z));
                if(i==Mathf.CeilToInt(fps*.1f)) Check(Quaternion.Angle(q,Quaternion.Euler(45,20,0))<5,"Deliberate body motion settles within ~100ms at "+fps);
            }
            Check(Quaternion.Angle(q,Quaternion.Euler(45,20,0))<.1f,"Rotation spring settles without persistent drift at "+fps);
            Check(finite,"Rotation spring stays finite throughout response at "+fps);
            report.AppendLine($"MEASURE {fps} FPS step response maximum rendered rotation step {peak:F2} degrees");
        }
        Vector3 p=Vector3.zero,v=Vector3.zero;float maxStep=0;bool bounded=true;
        for(int i=0;i<60;i++)
        {
            var old=p;p=AvatarMotionSmoothing.Position(p,Vector3.right*.5f,ref v,.035f,6,1/60f);
            maxStep=Mathf.Max(maxStep,(p-old).magnitude);
            bounded&=p.x>=old.x && p.x<=.50001f;
        }
        Check(bounded && p.x>.499f && maxStep<.1f,"Tracked arm step is bounded, non-overshooting and settles");
        var oldQ=Quaternion.identity;var smoothQ=Quaternion.identity;var angular=Vector3.zero;
        float oldStep=0,newStep=0,oldVariation=0,newVariation=0,oldAngle=0,newAngle=0;
        for(int i=0;i<360;i++)
        {
            // A 20 Hz observed target is displayed at 60 Hz. Measure changes in
            // angular velocity rather than claiming that smaller total motion is better.
            float t=(i/3)*.05f;var target=Quaternion.Euler(0,10*Mathf.Sin(t*5)+((i/3)%2==0?.8f:-.8f),0);
            oldQ=Quaternion.RotateTowards(oldQ,target,360/60f);
            smoothQ=AvatarMotionSmoothing.Rotation(smoothQ,target,ref angular,.07f,.028f,1/60f);
            float a=Mathf.DeltaAngle(0,oldQ.eulerAngles.y),b=Mathf.DeltaAngle(0,smoothQ.eulerAngles.y);
            float da=Mathf.DeltaAngle(oldAngle,a),db=Mathf.DeltaAngle(newAngle,b);
            if(i>5) { oldVariation+=Mathf.Abs(da-oldStep);newVariation+=Mathf.Abs(db-newStep); }
            oldStep=da;newStep=db;oldAngle=a;newAngle=b;
        }
        Check(newVariation<oldVariation*.7f,"20 Hz target at 60 Hz render reduces angular velocity changes by at least 30%");
        report.AppendLine($"MEASURE angular-step variation old/new {oldVariation:F2}/{newVariation:F2} degrees per frame, sampled sinusoid with noise");
    }
    private static void Quality()
    {
        var m=new GameBodyMotion();m.Observe(GameBodyValidation.Frame(1,1),1);m.Observe(GameBodyValidation.Frame(2,1.1),1.1);
        m.Observe(GameBodyValidation.Frame(3,1.2,0),1.2);m.Observe(GameBodyValidation.Frame(4,1.3,0,"front"),1.3);
        var trusted=m.LegPose;var corrupt=GameBodyValidation.Frame(5,1.35,0,"front");corrupt.joints[25].y=3;
        m.Observe(corrupt,1.35);m.Tick(1.43);
        Check(m.IsStrike && !m.VerifiedLegs && !m.ContactWindow(1.43),"One corrupt sample preserves visual action but blocks impact");
        Check(m.LegPose.sequence==trusted.sequence && m.LegPoseFresh(1.43),"Corrupt sample never becomes the leg display target");
        m.Observe(GameBodyValidation.Frame(6,1.44,0,"front"),1.44);m.Tick(1.44);
        Check(m.VerifiedLegs && m.IsStrike && m.ContactWindow(1.44),"Fresh verified reacquisition resumes same contact window");
        long id=m.Action;var missing=GameBodyValidation.Frame(7,1.45,0,"front");missing.confidence[28]=0;
        m.Observe(missing,1.45);m.Tick(1.58);
        Check(!m.IsStrike && !m.ContactWindow(1.58) && m.Action==id,"Prolonged loss cancels without inventing a new attack");
        var stance=new GameBodyMotion();stance.Observe(GameBodyValidation.Frame(1,2),2);stance.Observe(GameBodyValidation.Frame(2,2.1),2.1);
        var teleport=GameBodyValidation.Frame(3,2.15);teleport.joints[27].y=teleport.joints[28].y=2;
        stance.Observe(teleport,2.15);
        Check(!stance.VerifiedLegs && stance.Squat==0 && stance.LegPose.sequence==2,"Bilateral corruption cannot lower pelvis or pollute planted targets");
    }
    private static void Head()
    {
        var samples=LoadExact();float min=180,max=-180,maxTurn=0,maxNod=0;
        foreach(var s in samples)
        {
            var a=s.body.head.eulerAngles;float roll=Mathf.DeltaAngle(0,a.z);
            if(s.elapsed>=160 && s.elapsed<=164) { min=Mathf.Min(min,roll);max=Mathf.Max(max,roll); }
            if(s.elapsed>=64 && s.elapsed<74) { maxTurn=Mathf.Max(maxTurn,Mathf.Abs(Mathf.DeltaAngle(0,a.y)));maxNod=Mathf.Max(maxNod,Mathf.Abs(Mathf.DeltaAngle(0,a.x))); }
        }
        report.AppendLine($"MEASURE image-head quiet roll span {max-min:F2}°, head-cue max yaw/pitch {maxTurn:F1}/{maxNod:F1}°");
        Check(max-min<8,"Recorded quiet head roll is bounded below 8 degrees (previous estimate span 32 degrees)");
        Check(maxTurn>8 && maxNod>5,"Recorded head turns and nods remain visible with image geometry");
    }
    private static void Recorded()
    {
        var samples=LoadExact(false);var recorded=new List<Event>();double start=0;
        foreach(var line in File.ReadLines(Path116))
        {
            if(!line.StartsWith("{\"kind\":\"session\"") && !line.StartsWith("{\"kind\":\"body_action\"")) continue;
            var r=JsonUtility.FromJson<Record>(line);if(r.kind=="session") start=r.recordingStartedAt;
            else recorded.Add(new Event{kind=r.actionKind,side=r.side,time=r.decidedAt-start});
        }
        var baseline=new GameBodyMotion116();var baselineEvents=new List<Event>();
        baseline.Decision+=(kind,side,action,captured,now)=>baselineEvents.Add(new Event{kind=kind,side=side,time=now-start});
        int index=0;
        for(double t=0;t<165;t+=1/120.0)
        {
            while(index<samples.Count && samples[index].elapsed<=t)
            {
                var f=samples[index++].body;
                baseline.Observe(new GameBodyMotion116.Snapshot{sequence=f.sequence,captured=f.captured,received=f.received,
                    joints=f.joints,confidence=f.confidence,body=f.body,head=f.head,standingHipHeight=f.standingHipHeight},start+t);
            }
            baseline.Tick(start+t);
        }
        Check(baselineEvents.Count==recorded.Count,"App 1.16 baseline reproduces all 33 body events");
        for(int i=0;i<recorded.Count;i++) Check(recorded[i].kind==baselineEvents[i].kind && recorded[i].side==baselineEvents[i].side
            && Math.Abs(recorded[i].time-baselineEvents[i].time)<.025,"App 1.16 baseline matches event "+i);
        foreach(int stride in new[]{1,2})
        {
            var m=new GameBodyMotion();var events=new List<Event>();index=0;
            m.Decision+=(kind,side,action,captured,now)=>events.Add(new Event{kind=kind,side=side,time=now-start});
            for(double t=0;t<165;t+=1/60.0)
            {
                while(index<samples.Count && samples[index].elapsed<=t)
                { var s=samples[index];if(index%stride==0)m.Observe(s.body,start+t);index++; }
                m.Tick(start+t);
            }
            int count=0;foreach(var e in events) if(e.kind!="prepare" && e.kind!="cancel") { count++;report.AppendLine($"REPLAY stride={stride} {e.time:F3} side={e.side} {e.kind}"); }
            Check(!events.Exists(e=>e.kind!="prepare" && e.kind!="cancel" && (e.time<120 || e.time>=156)),"Exact replay idle/holds/body/arm/recovery remain non-scoring, stride "+stride);
            Check(count>=8,"Exact replay retains at least eight observed foot-extension attacks, stride "+stride+": "+count);
        }
    }
}
