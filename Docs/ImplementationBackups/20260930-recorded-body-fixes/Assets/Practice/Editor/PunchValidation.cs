using System;
using System.IO;
using System.Text;
using FitFight.Practice;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Deterministic regressions; these do not certify camera accuracy or physical latency.</summary>
public static class PunchValidation
{
    private static readonly StringBuilder report = new StringBuilder();
    private static int assertions;
    private static void Check(bool value,string message)
    {
        assertions++; if(!value) throw new Exception(message);
        report.AppendLine("PASS "+message);
    }
    [MenuItem("Tools/FitFight/Practice/Validate assisted punches")]
    public static void Run()
    {
        report.Clear(); assertions=0; Directory.CreateDirectory("Logs/Practice");
        try { Detector(); ReturnAndPairedGestures(); NaturalStartingPositions(); PhoneReplay(); September30Replay(); Features(); Contacts(); Clips(); Arms(); BodyOffsets(); Recorder(); SpokenGuide(); report.AppendLine($"PASS: {assertions} checks."); }
        catch(Exception e) { report.AppendLine("FAIL "+e); throw; }
        finally { File.WriteAllText("Logs/Practice/punch-validation.txt",report.ToString()); Debug.Log(report.ToString()); }
    }
    public static void RunBatch()
    {
        try { Run(); EditorApplication.Exit(0); }
        catch { EditorApplication.Exit(1); }
    }
    public static void RunVisualBatch() => PunchPlayValidation.RunBatch();
    private static PunchArmObservation Arm(float x=.25f,float y=.25f,float ex=.40f,float ey=-.20f,float confidence=.95f)
        => new PunchArmObservation { wrist=new Vector2(x,y),elbow=new Vector2(ex,ey),confidence=confidence };
    private static void Prime(PunchIntent d, ref long seq,ref double time,float interval)
    {
        for(int i=0;i<12;i++) { d.Observe(Arm(),seq++,time); time+=interval; }
        Check(d.Phase==PunchIntent.State.Guard,"Guard acquisition at "+interval);
    }
    private static void Detector()
    {
        foreach(float interval in new[]{1f/10,1f/30,1f/60})
        {
            var d=new PunchIntent(); long seq=1; double time=0;
            Prime(d,ref seq,ref time,interval);
            int commits=0,prepares=0;
            for(int i=0;i<30;i++)
            {
                float t=i*interval;
                // 250 ms extension from a bent guard, followed by a held arm.
                float p=Mathf.Clamp01(t/.25f);
                var s=d.Observe(Arm(.25f+.65f*p,.25f-.05f*p,.4f+.15f*p,-.2f+.2f*p),seq++,time); time+=interval;
                if(s==PunchIntent.Signal.Commit) commits++;
                if(s==PunchIntent.Signal.Prepare) prepares++;
            }
            Check(commits==1 && prepares==1,"One commit for extension/hold at "+interval);
            long duplicate=seq-1;
            bool duplicateIgnored=true;
            for(int i=0;i<100;i++) duplicateIgnored &= d.Observe(Arm(),duplicate,time)==PunchIntent.Signal.None;
            Check(duplicateIgnored,"Render duplicates cannot trigger a new punch");
            for(int i=0;i<20;i++) { d.Observe(Arm(),seq++,time);time+=interval; }
            Check(d.Phase==PunchIntent.State.Guard,"Retracted hand rearms at "+interval);
        }
        {
            var d=new PunchIntent(); long seq=1;double time=0;Prime(d,ref seq,ref time,.05f);
            Check(d.Observe(Arm(.34f,.24f,.45f,-.15f),seq++,time)==PunchIntent.Signal.Prepare,"Partial extension prepares"); time+=.05;
            Check(d.Observe(Arm(),seq++,time)==PunchIntent.Signal.Cancel,"Feint cancels without commitment");
        }
        foreach(string test in new[]{"noise","overhead","low","loss","teleport"})
        {
            var d=new PunchIntent();long seq=1;double time=0;Prime(d,ref seq,ref time,.033f);
            int commits=0;
            for(int i=0;i<60;i++)
            {
                PunchArmObservation a=test=="noise"?Arm(.25f+Mathf.Sin(i)*.008f):test=="overhead"?Arm(.25f,.25f+i*.035f,.4f,-.2f+i*.03f):
                    test=="low"?Arm(.5f,-.7f):test=="loss"?Arm(confidence:0):Arm(i%2==0?4:-4);
                if(d.Observe(a,seq++,time)==PunchIntent.Signal.Commit) commits++;time+=.033;
            }
            Check(commits==0,"No phantom punch: "+test);
        }
    }
    private static void ReturnAndPairedGestures()
    {
        foreach(float interval in new[]{.1f, 1f/30, 1f/60})
        {
            var detector = new PunchIntent(); long sequence = 1; double time = 0;
            Prime(detector, ref sequence, ref time, interval);
            int outward = 0, returning = 0;
            for(int i=0;i<Mathf.CeilToInt(2.5f/interval);i++)
            {
                float t=i*interval;
                float p=t<.25f?t/.25f:t<1.5f?1:Mathf.Clamp01(1-(t-1.5f)/.25f);
                var signal=detector.Observe(Arm(.25f+.65f*p,.25f-.05f*p,.4f+.15f*p,-.2f+.2f*p),sequence++,time);
                time+=interval;
                if(signal==PunchIntent.Signal.Commit) { if(t<1.5f) outward++; else returning++; }
            }
            Check(outward==1 && returning==0,"Held extension never becomes return punch at "+interval);
            Check(detector.Phase==PunchIntent.State.Guard,"Return lock releases after settled guard at "+interval);
            foreach(bool upward in new[]{true,false})
            {
                var gate=new PunchGestureGate();var left=new PunchIntent();var right=new PunchIntent();int commits=0;
                for(int i=0;i<Mathf.CeilToInt(2f/interval);i++)
                {
                    float t=i*interval;
                    float p=t<.4f?0:t<.8f?(t-.4f)/.4f:t<1.1f?1:Mathf.Clamp01(1-(t-1.1f)/.4f);
                    var arm=upward?Arm(.25f,.25f+.65f*p,.4f,-.2f+.6f*p):Arm(.25f,.25f-.85f*p,.4f,-.2f-.3f*p);
                    var input=new PunchCameraInput.Frame {sequence=i+1,time=t,left=arm,right=arm};
                    gate.Observe(input);
                    string blocked=gate.Blocked?gate.Reason:null;
                    if(left.Observe(arm,input.sequence,t,blocked)==PunchIntent.Signal.Commit) commits++;
                    if(right.Observe(arm,input.sequence,t,blocked)==PunchIntent.Signal.Commit) commits++;
                }
                Check(commits==0,"Paired "+(upward?"raise/lower":"relax/return")+" does not punch at "+interval);
                Check(!gate.Blocked && left.Phase==PunchIntent.State.Guard,"Paired gesture releases after guard at "+interval);
            }
        }
        // A complete fast extension can be stationary by the second independent observation.
        var fast=new PunchIntent();long seq=1;double clock=0;Prime(fast,ref seq,ref clock,.1f);
        var extended=Arm(.9f,.2f,.55f,0);
        Check(fast.Observe(extended,seq++,clock)==PunchIntent.Signal.Prepare,"Fast extension prepares");clock+=.1;
        Check(fast.Observe(extended,seq++,clock)==PunchIntent.Signal.Commit,"Confirmed held extension completes without waiting for recoil");
        fast.Reset();
        Check(fast.Phase==PunchIntent.State.Unarmed,"Reset removes previous readiness");
    }
    private static void NaturalStartingPositions()
    {
        foreach(float interval in new[]{.1f,1f/30,1f/60})
        foreach(float height in new[]{-.55f,-.25f,.25f})
        foreach(float mirror in new[]{-1f,1f})
        {
            var detector=new PunchIntent(); long sequence=0;double time=0;int commits=0;
            var start=Arm(.25f*mirror,height,.4f*mirror,height-.45f);
            for(int i=0;i<12;i++) {detector.Observe(start,++sequence,time);time+=interval;}
            Check(detector.Phase==PunchIntent.State.Guard,$"Natural start acquired at wrist height {height}, side {mirror}, cadence {interval}");
            for(int i=0;i<Mathf.CeilToInt(.8f/interval);i++)
            {
                float p=Mathf.Clamp01(i*interval/.25f);
                var arm=Arm((.25f+.65f*p)*mirror,height-.05f*p,(.4f+.15f*p)*mirror,height-.45f+.2f*p);
                if(detector.Observe(arm,++sequence,time)==PunchIntent.Signal.Commit) commits++;
                time+=interval;
            }
            Check(commits==1,$"Punch from non-chin start triggers once at height {height}, side {mirror}, cadence {interval}");
        }
        foreach(float interval in new[]{.1f,1f/30,1f/60})
        {
            var hook=new PunchIntent();long sequence=1;double time=0;int commits=0;
            for(int i=0;i<12;i++) {hook.Observe(Arm(.5f,.2f,.4f,-.2f),sequence++,time);time+=interval;}
            for(int i=0;i<Mathf.CeilToInt(.8f/interval);i++)
            {
                float travel=Mathf.Min(.6f,i*interval*2.5f);
                if(hook.Observe(Arm(.5f+travel,.2f,.4f+travel,-.2f),sequence++,time)==PunchIntent.Signal.Commit) commits++;
                time+=interval;
            }
            Check(commits==1,"Coordinated lateral hook works without straightening elbow at "+interval);
        }
        var d=new PunchIntent();long seq=1;double clock=0;int total=0;
        for(int i=0;i<12;i++) {d.Observe(Arm(.15f,-1,.1f,-.5f),seq++,clock);clock+=.05;}
        // Fold from hanging arms, then immediately punch. No pause at chin height.
        for(int i=0;i<10;i++)
        {
            float p=i/9f;
            d.Observe(Arm(Mathf.Lerp(.15f,.25f,p),Mathf.Lerp(-1,-.25f,p),Mathf.Lerp(.1f,.4f,p),-.5f),seq++,clock);clock+=.05;
        }
        for(int i=0;i<15;i++)
        {
            float p=Mathf.Clamp01(i*.05f/.25f);
            if(d.Observe(Arm(.25f+.65f*p,-.25f-.05f*p,.4f+.15f*p,-.5f+.2f*p),seq++,clock)==PunchIntent.Signal.Commit) total++;
            clock+=.05;
        }
        Check(total==1,"Hanging arm can chamber and punch without a chin-height pause");
    }
    [Serializable] private struct ReplayRecord
    {
        public string kind;
        public double recordedRelative;
        public PunchCameraInput.Frame observation;
    }
    private static void PhoneReplay()
    {
        const string path="Docs/PunchSessions/phone-punch-regression.jsonl";
        Check(File.Exists(path),"Phone regression fixture available");
        var gate=new PunchGestureGate();var detectors=new[]{new PunchIntent(),new PunchIntent()};
        var events=new StringBuilder("sequence,recordedRelative,side,signal,bend,reason\n");
        var committed=new System.Collections.Generic.List<(long seq,int side)>();
        var prior=new PunchArmObservation[2];int rows=0,negativeCommits=0,closingCommits=0;
        foreach(string line in File.ReadLines(path))
        {
            var row=JsonUtility.FromJson<ReplayRecord>(line);if(row.kind!="observation") continue;
            rows++;var frame=row.observation;gate.Observe(frame);
            for(int side=0;side<2;side++)
            {
                var arm=side==0?frame.left:frame.right;
                var signal=detectors[side].Observe(arm,frame.sequence,frame.time,gate.Blocked?gate.Reason:null);
                if(signal!=PunchIntent.Signal.None)
                    events.AppendLine(FormattableString.Invariant($"{frame.sequence},{row.recordedRelative:F3},{side},{signal},{arm.Bend:F1},{detectors[side].Reason}"));
                if(signal==PunchIntent.Signal.Commit)
                {
                    committed.Add((frame.sequence,side));
                    if(row.recordedRelative>=65) negativeCommits++;
                    if(arm.Bend>prior[side].Bend+5) closingCommits++;
                }
                prior[side]=arm;
            }
        }
        File.WriteAllText("Logs/Practice/phone-replay-events.csv",events.ToString());
        Check(rows==756,"Replay consumes all 756 independent camera observations");
        Check(negativeCommits==0,"No replay punches during recorded bilateral raise/lower and step-away negatives");
        Check(closingCommits==0,"Replay commitments occur before projected elbow closes");
        // Windows identified visually, independent of the old detector's action count.
        foreach(var window in new[]{(269L,271L,0),(311L,314L,0),(366L,368L,0),(380L,382L,0),(520L,523L,1),(535L,537L,1)})
        {
            int count=committed.FindAll(x=>x.side==window.Item3 && x.seq>=window.Item1 && x.seq<=window.Item2).Count;
            Check(count==1,$"Recorded outward attempt recognized once: side {window.Item3}, observations {window.Item1}–{window.Item2}");
        }
        foreach(var window in new[]{(276L,282L,0),(319L,325L,0),(410L,415L,0),(525L,530L,1),(539L,546L,1)})
            Check(!committed.Exists(x=>x.side==window.Item3 && x.seq>=window.Item1 && x.seq<=window.Item2),
                $"No return-phase punch: side {window.Item3}, observations {window.Item1}–{window.Item2}");
        report.AppendLine($"REPLAY {committed.Count} commitments; same-session regression, not an independent accuracy estimate.");
    }
    private static void September30Replay()
    {
        const string path = "Docs/PunchSessions/20260930-punch-regression.jsonl";
        var gate = new PunchGestureGate(); var detectors = new[] { new PunchIntent(), new PunchIntent() };
        var events = new StringBuilder("sequence,recordedRelative,side,signal,reason\n");
        var commits = new System.Collections.Generic.List<(long seq, int side)>();
        bool loweringPrepared = false; int rows = 0, bilateralCommits = 0;
        foreach (string line in File.ReadLines(path))
        {
            var row = JsonUtility.FromJson<ReplayRecord>(line); if (row.kind != "observation") continue;
            rows++; var frame = row.observation; gate.Observe(frame);
            for (int side = 0; side < 2; side++)
            {
                var signal = detectors[side].Observe(side == 0 ? frame.left : frame.right,
                    frame.sequence, frame.time, gate.Blocked ? gate.Reason : null);
                if (signal != PunchIntent.Signal.None)
                    events.AppendLine(FormattableString.Invariant($"{frame.sequence},{row.recordedRelative:F3},{side},{signal},{detectors[side].Reason}"));
                if (side == 0 && frame.sequence >= 554 && frame.sequence <= 557 && signal == PunchIntent.Signal.Prepare) loweringPrepared = true;
                if (signal != PunchIntent.Signal.Commit) continue;
                commits.Add((frame.sequence, side));
                if (row.recordedRelative >= 67.3 && row.recordedRelative <= 70.5) bilateralCommits++;
            }
        }
        File.WriteAllText("Logs/Practice/20260930-replay-events.csv", events.ToString());
        Check(rows == 703, "Second phone replay consumes all 703 observations");
        Check(!loweringPrepared, "Recorded downward reset 554–557 does not start a preparation");
        Check(commits.FindAll(x => x.side == 0 && x.seq >= 559 && x.seq <= 562).Count == 1,
            "Next recorded left stroke 559–562 commits once after the return");
        Check(commits.FindAll(x => x.side == 1 && x.seq >= 766 && x.seq <= 770).Count == 1,
            "Recorded quick right follow-up 766–770 commits once after gate release");
        Check(bilateralCommits == 0, "Actual two-hand overhead hold/lower remains a negative in the second phone replay");
        report.AppendLine($"SECOND REPLAY {commits.Count} commitments; reviewed windows, not a complete accuracy estimate.");
    }
    private static void Features()
    {
        PunchFeatures.TryCreate(new Vector2(-.2f,1),new Vector2(.2f,1),new Vector2(0,.5f),new Vector2(-.1f,.8f),new Vector2(-.2f,1.2f),.9f,out var a);
        PunchFeatures.TryCreate(new Vector2(-.4f,2)+Vector2.one,new Vector2(.4f,2)+Vector2.one,new Vector2(0,1)+Vector2.one,new Vector2(-.2f,1.6f)+Vector2.one,new Vector2(-.4f,2.4f)+Vector2.one,.9f,out var b);
        Check(Vector2.Distance(a.wrist,b.wrist)<.0001f && Vector2.Distance(a.elbow,b.elbow)<.0001f,"Features reject body translation and scale changes");
        Check(!PunchFeatures.TryCreate(Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,Vector2.one,1,out _),"Degenerate torso rejected");
    }
    private static void Contacts()
    {
        var obj=new GameObject("Punch test bag");var bag=obj.AddComponent<PracticeHeavyBag>();
        try
        {
            bag.Configure(1.7f); Physics.SyncTransforms(); double time=1;
            // Approach a real collider in modest render-frame steps.
            bool acceptedDiagnostic = false;
            bool Sweep(long id,bool window)
            {
                bool hit=false;
                for(int i=0;i<12;i++)
                {
                    bool scored=bag.SampleAssistedPunch(0,id,new Vector3(0,0,-.8f+i*.06f),.016f,time,true,window); hit |= scored;
                    var sample=bag.GetContactSample(0);
                    if(scored) acceptedDiagnostic |= sample.hit && sample.reason=="Accepted hit" && sample.speed>.3f && sample.from!=sample.to;
                    time+=.016;
                }
                return hit;
            }
            Check(!Sweep(1,false),"Preparation cannot score");
            Check(bag.GetContactSample(0).reason=="Outside contact window or already scored" && !bag.GetContactSample(0).allowImpact,"Preparation contact diagnosis explains rejected scoring");
            Check(Sweep(2,true),"Committed sweep contacting bag scores");
            Check(acceptedDiagnostic,"Accepted contact reports the scored sweep and speed");
            Check(!Sweep(2,true),"Same action cannot score twice after separation");
            bag.ResetContacts();Check(!Sweep(2,true),"Reset contacts cannot duplicate action score");
            Check(Sweep(3,true),"New action can score");
            Check(bag.SoundCount==bag.HitCount && bag.EffectCount==bag.HitCount,"One sound and effect per validated impact");
        }
        finally {Object.DestroyImmediate(obj);}
    }
    private static void Clips()
    {
        var art=Resources.Load<FitFightPoseReference>("FitFightPoseReference");
        Check(art!=null && art.guard!=null && art.leftPunch!=null && art.rightPunch!=null,"Runtime clips are included by resource references");
        Check(art.guard.isHumanMotion && art.leftPunch.isHumanMotion && art.rightPunch.isHumanMotion,"All assist clips retarget as Humanoid");
        Check(Mathf.Abs(AssistedPunchController.ClipPhase(.4f,art.leftContactTime)-art.leftContactTime)<.0001f && Mathf.Abs(AssistedPunchController.ClipPhase(.4f,art.rightContactTime)-art.rightContactTime)<.0001f,"Authored contacts align to common action phase");
        report.AppendLine($"CLIPS guard={art.guard.name}/{art.guard.length:F3}s left={art.leftPunch.name}/{art.leftPunch.length:F3}s right={art.rightPunch.name}/{art.rightPunch.length:F3}s");
    }
    private static void Arms()
    {
        var root=new GameObject("Arm test");var lower=new GameObject("Elbow");var hand=new GameObject("Hand");
        lower.transform.SetParent(root.transform);hand.transform.SetParent(lower.transform);
        lower.transform.localPosition=new Vector3(.3f,-.1f,0);hand.transform.localPosition=new Vector3(.3f,.1f,0);
        float a=Vector3.Distance(root.transform.position,lower.transform.position),b=Vector3.Distance(lower.transform.position,hand.transform.position);
        try
        {
            foreach(var target in new[]{new Vector3(.1f,0,.5f),new Vector3(0,0,4),new Vector3(.01f,0,0)})
            {
                Quaternion wristLocal=hand.transform.localRotation;
                AssistedPunchController.SolveArm(root.transform,lower.transform,hand.transform,target,Vector3.right);
                Check(Quaternion.Angle(wristLocal,hand.transform.localRotation)<.001f,"IK preserves anatomical local wrist pose");
                Check(Mathf.Abs(Vector3.Distance(root.transform.position,lower.transform.position)-a)<.0001f && Mathf.Abs(Vector3.Distance(lower.transform.position,hand.transform.position)-b)<.0001f,"IK preserves imported lengths");
                Check(Vector3.Distance(root.transform.position,hand.transform.position)<a+b-.01f,"IK prevents hyperextension");
                Check(!float.IsNaN(hand.transform.position.sqrMagnitude),"IK remains finite");
            }
        }
        finally {Object.DestroyImmediate(root);}
    }
    private static void BodyOffsets()
    {
        var root=new GameObject("Body offset test");
        var spine=new GameObject("Spine");spine.transform.SetParent(root.transform);
        var chest=new GameObject("Chest");chest.transform.SetParent(spine.transform);chest.transform.localPosition=Vector3.up*.3f;
        var neck=new GameObject("Neck");neck.transform.SetParent(chest.transform);
        var head=new GameObject("Head");head.transform.SetParent(neck.transform);head.transform.localPosition=Vector3.up*.2f;
        try
        {
            root.transform.rotation=Quaternion.Euler(0,65,0);
            AssistedPunchController.ApplyBodyOffsets(root.transform,spine.transform,chest.transform,neck.transform,head.transform,
                Quaternion.Euler(0,0,80),Quaternion.identity);
            Check(Mathf.Abs(Quaternion.Angle(root.transform.rotation,chest.transform.rotation)-28)<.01f,"Torso overlay distributes bounded bend without doubling it");
            Check(Mathf.Abs(Vector3.Distance(spine.transform.position,chest.transform.position)-.3f)<.0001f,"Torso overlay preserves bone lengths");
            Quaternion chestBefore=chest.transform.rotation;
            AssistedPunchController.ApplyBodyOffsets(root.transform,null,null,neck.transform,head.transform,Quaternion.identity,Quaternion.Euler(0,80,0));
            Check(Quaternion.Angle(chestBefore,head.transform.rotation)<=30.01f,"Neck and head share one bounded rotation");
            Check(Quaternion.Angle(AssistedPunchController.LimitOffset(new Quaternion(float.NaN,0,0,1),28),Quaternion.identity)<.01f,"Invalid body estimate cannot corrupt rig");
        }
        finally {Object.DestroyImmediate(root);}
    }
    private static void SpokenGuide()
    {
        var guide=new PunchRecordingGuide();
        Check(guide.TryAdvance(0,out var cue) && cue.speech.Contains("Step back"),"Spoken guide starts with positioning directions");
        Check(!guide.TryAdvance(.016,out _),"Render frames do not repeat speech");
        Check(guide.TryAdvance(7,out cue) && cue.speech=="Three", "Spoken countdown starts after setup time");
        Check(guide.TryAdvance(9,out cue) && cue.speech=="One", "Slow frames skip expired speech rather than queue it");
        Check(guide.TryAdvance(10,out cue) && cue.label=="HOLD STILL · NATURAL STANCE", "Natural stance test starts after countdown");
        Check(guide.TryAdvance(18,out cue) && cue.label=="LEFT PUNCHES · SLOW", "Left slow instruction matches visible stage");
        Check(guide.TryAdvance(26,out cue) && cue.label=="LEFT PUNCHES · FAST", "Left speed change is spoken");
        Check(guide.TryAdvance(34,out cue) && cue.label=="RIGHT PUNCHES · SLOW", "Right slow instruction matches visible stage");
        Check(guide.TryAdvance(42,out cue) && cue.label=="RIGHT PUNCHES · FAST", "Right speed change is spoken");
        Check(guide.TryAdvance(50,out cue) && cue.speech.Contains("Alternate"), "Alternating punches have their own cue");
        Check(guide.TryAdvance(58,out cue) && cue.speech.Contains("pull it back"), "Feint instruction is spoken");
        Check(guide.TryAdvance(65,out cue) && cue.speech.Contains("Do not punch"), "Negative test distinguishes raises from punches");
        Check(guide.TryAdvance(70,out cue) && cue.speech.Contains("return"), "Step-out instruction asks user to return");
        Check(!guide.TryAdvance(PunchRecordingGuide.Duration,out _), "Completion cannot enqueue another exercise");
        guide.Reset();Check(guide.TryAdvance(0,out _), "Restarted recordings begin a new spoken guide");
    }
    private static void Recorder()
    {
        var r=new PunchSessionRecorder("{\"kind\":\"test\"}");
        r.Observation(new PunchCameraInput.Frame { sequence=42, lw=new Vector2(.25f,.75f), left=Arm() },new PunchIntent(),new PunchIntent());
        r.Dispose();
        double deadline=EditorApplication.timeSinceStartup+3;
        while(!r.Finished && EditorApplication.timeSinceStartup<deadline) System.Threading.Thread.Sleep(10);
        Check(r.Finished && r.Error==null && File.Exists(r.Path),"Recorder drains without blocking the game loop");
        Check(File.ReadAllLines(r.Path).Length==3 && File.ReadAllText(r.Path).Contains("droppedRecords"),"Recorder writes metadata and completeness marker");
        Check(File.ReadAllText(r.Path).Contains("\"sequence\":42") && File.ReadAllText(r.Path).Contains("\"lw\":{\"x\":0.25"),"Recorder serializes landmark frames for replay");
    }
}
