using System;
using System.IO;
using System.Text;
using FitFight.Practice;
using UnityEngine;

public static class GameBodyValidation
{
    private static readonly StringBuilder report=new StringBuilder();
    private static int checks;
    private static void Check(bool condition,string label)
    { if(!condition) throw new Exception(label);checks++;report.AppendLine("PASS "+label); }
    public static GameBodyMotion.Snapshot Frame(long sequence,double time,int side=-1,string action="standing")
    {
        var p=new Vector3[33];var c=new float[33];for(int i=0;i<33;i++) c[i]=1;
        p[11]=new Vector3(-.2f,.5f,0);p[12]=new Vector3(.2f,.5f,0);
        p[13]=new Vector3(-.35f,.25f,0);p[14]=new Vector3(.35f,.25f,0);
        p[15]=new Vector3(-.2f,.45f,.15f);p[16]=new Vector3(.2f,.45f,.15f);
        p[23]=new Vector3(-.15f,0,0);p[24]=new Vector3(.15f,0,0);
        p[25]=new Vector3(-.15f,-.45f,0);p[26]=new Vector3(.15f,-.45f,0);
        p[27]=new Vector3(-.15f,-.9f,0);p[28]=new Vector3(.15f,-.9f,0);
        if(action=="squat") { p[25].y=p[26].y=-.18f;p[25].z=p[26].z=.2f;p[27].y=p[28].y=-.55f; }
        if(side>=0)
        {
            float x=side==0?-.15f:.15f;
            p[25+side]=new Vector3(x,-.12f,.28f);p[27+side]=new Vector3(x,-.5f,.06f);
            if(action=="front") { p[25+side]=new Vector3(x,-.15f,.28f);p[27+side]=new Vector3(x,-.2f,.75f); }
            if(action=="side") { p[25+side]=new Vector3(x*3,-.2f,.1f);p[27+side]=new Vector3(x*5,-.3f,.1f); }
            if(action=="knee") { p[25+side]=new Vector3(x,-.05f,.48f);p[27+side]=new Vector3(x,-.4f,.12f); }
            if(action=="generic") { p[25+side]=new Vector3(x,.3f,.1f);p[27+side]=new Vector3(x,.72f,.1f); }
        }
        return new GameBodyMotion.Snapshot { sequence=sequence,captured=time,received=time,joints=p,confidence=c,
            standingHipHeight=.9f,body=Quaternion.identity,head=Quaternion.identity };
    }
    public static void Run()
    {
        checks=0;report.Clear();
        try
        {
            Guide();Intent();Contacts();Summary();
            report.AppendLine($"PASS {checks} full-body checks. Synthetic intent/contact checks do not establish phone accuracy.");
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { Directory.CreateDirectory("Logs/Practice");File.WriteAllText("Logs/Practice/game-body-validation.txt",report.ToString()); }
    }
    private static void Guide()
    {
        var guide=new GameBodyRecordingGuide();int count=0;double previous=-1;var speech=new StringBuilder();
        for(double t=0;t<GameBodyRecordingGuide.Duration;t+=.25)
            if(guide.TryAdvance(t,out var cue))
            { Check(cue.at>previous && GameBodyRecordingGuide.Label(t)==cue.label,"Ordered spoken cue: "+cue.label);previous=cue.at;count++;speech.Append(cue.speech); }
        Check(count==GameBodyRecordingGuide.Cues.Length && !guide.TryAdvance(164,out _),"Single run finishes at 2:44");
        Check(GameBodyRecordingGuide.Duration+GameBodyRecordingGuide.MaxCalibrationPause<=180,"Calibration extension bounded to three minutes");
        foreach(string word in new[]{"punches","elbows","overhead","sides","head","Bend forward","Lean left","twist","squats","Do not strike","knee forward","front kicks","side kick"})
            Check(speech.ToString().Contains(word),"Audio covers "+word);
        guide.Reset();guide.TryAdvance(0,out _);Check(guide.TryAdvance(130,out var latest) && latest.at==130 && !guide.TryAdvance(130.1,out _),"Late frames skip obsolete speech");
    }
    private static void Intent()
    {
        for(int side=0;side<2;side++) foreach(string kind in new[]{"front","side","knee","generic"})
        {
            var motion=new GameBodyMotion();int commits=0;
            motion.Decision+=(name,_,__,___,____) => { if(name!="prepare" && name!="cancel") commits++; };
            motion.Observe(Frame(1,1),1);motion.Observe(Frame(2,1.1),1.1);
            motion.Observe(Frame(3,1.2,side),1.2);
            Check(motion.State==GameBodyMotion.LegMotion.Chamber,"Observed chamber before "+side+kind);
            motion.Observe(Frame(4,1.3,side,kind),1.3);
            double checkedAt=1.43;long duplicate=4;
            if(kind=="knee")
            {
                Check(motion.State==GameBodyMotion.LegMotion.Chamber,"Bent-knee rise stays provisional before observed return");
                motion.Observe(Frame(5,1.4,side),1.4);checkedAt=1.53;duplicate=5;
            }
            motion.Tick(checkedAt);
            var expected=kind=="front"?GameBodyMotion.LegMotion.FrontKick:kind=="side"?GameBodyMotion.LegMotion.SideKick:
                kind=="knee"?GameBodyMotion.LegMotion.Knee:GameBodyMotion.LegMotion.Kick;
            Check(motion.State==expected && motion.Side==side && commits==1,"Commit distinct "+side+kind);
            Check(motion.ContactWindow(checkedAt),"Fresh committed leg has a bounded contact window");
            motion.Observe(Frame(duplicate,checkedAt-.13,side,kind),checkedAt);Check(commits==1,"Duplicate observation cannot recommit");
            motion.Tick(1.9);Check(!motion.ContactWindow(1.9),"Stale leg cannot score or finish an unseen strike");
        }
        var idle=new GameBodyMotion();int strikes=0;
        idle.Decision+=(name,_,__,___,____)=> { if(name!="prepare" && name!="cancel") strikes++; };
        for(int i=0;i<20;i++) { var f=Frame(i+1,2+i*.1);idle.Observe(f,f.received); }
        Check(strikes==0 && idle.State==GameBodyMotion.LegMotion.Grounded,"Idle posture produces no leg attacks");
        idle.Observe(Frame(21,4,-1,"squat"),4);Check(strikes==0 && Mathf.Abs(idle.Squat-.35f)<.01f,"Bilateral squat lowers pelvis without a kick");
        idle.Observe(Frame(22,4.1),4.1);
        for(int i=0;i<8;i++) { var f=Frame(23+i,4.2+i*.1,0);idle.Observe(f,f.received); }
        Check(strikes==0 && idle.State==GameBodyMotion.LegMotion.Chamber,"Held knee lift remains non-scoring preparation");
        idle.Observe(Frame(31,5),5);Check(strikes==0 && idle.State==GameBodyMotion.LegMotion.Grounded,"Withdrawn preparation returns without scoring");
        idle.Observe(Frame(32,5.1,0),5.1);var lost=Frame(33,5.2,0,"front");lost.confidence[28]=0;idle.Observe(lost,5.2);
        Check(strikes==0 && !idle.ContactWindow(5.2),"Loss of supporting ankle cancels an attack");
        var feet=new GameBodyMotion();
        for(int i=0;i<10;i++)
        { var quiet=Frame(i+1,6+i*.01);quiet.joints[27].y+=.05f;feet.Observe(quiet,quiet.received); }
        Check(!feet.FootReleased(0) && !feet.FootReleased(1),"Small idle ankle noise keeps both feet planted");
        var lifted=Frame(11,6.2,0);feet.Observe(lifted,6.2);Check(feet.FootReleased(0) && !feet.FootReleased(1),"Observed lift releases only the attacking foot");
        var lowering=Frame(12,6.3);lowering.joints[27].y+=.045f;feet.Observe(lowering,6.3);Check(feet.FootReleased(0),"Foot release hysteresis avoids flickering during return");
        lowering=Frame(13,6.4);lowering.joints[27].y+=.025f;feet.Observe(lowering,6.4);Check(!feet.FootReleased(0),"Returning foot replants near the floor");
        var gestures=new GameBodyMotion();var arms=Frame(1,7);
        arms.joints[15]=arms.joints[11]+Vector3.up*.65f;arms.joints[16]=arms.joints[12]+Vector3.up*.65f;
        gestures.Observe(arms,7);Check(gestures.ClearArmGesture,"Clear bilateral overhead gesture blocks punching intent");
        arms=Frame(2,7.1);arms.joints[15]=arms.joints[11]-Vector3.right*.5f;arms.joints[16]=arms.joints[12]+Vector3.right*.5f;
        gestures.Observe(arms,7.1);Check(gestures.ClearArmGesture,"Clear bilateral side gesture stays a tracked arm movement");
        arms=Frame(3,7.2);arms.joints[15]=arms.joints[11]+Vector3.forward*.5f;arms.joints[16]=arms.joints[12]+Vector3.forward*.5f;
        gestures.Observe(arms,7.2);Check(gestures.ClearArmGesture,"Clear bilateral forward extension is excluded from punching");
        arms=Frame(4,7.3);arms.joints[15]=arms.joints[11]+Vector3.up*.5f;
        gestures.Observe(arms,7.3);Check(!gestures.ClearArmGesture,"Ambiguous single-arm upward stroke retains boxing priority");
        idle.Reset();idle.Observe(Frame(1,8),8);idle.Observe(Frame(2,8.1),8.1);idle.Observe(Frame(3,8.8,0,"front"),8.8);
        Check(strikes==0,"Reacquisition after a gap cannot launch a strike");
        for(int side=0;side<2;side++)
        {
            var turned=new GameBodyMotion();var rotation=Quaternion.Euler(0,70,0);
            for(int i=0;i<4;i++)
            {
                var f=Frame(i+1,10+i*.1,i<2?-1:side,i==3?"side":"chamber");
                for(int j=0;j<33;j++) f.joints[j]=rotation*f.joints[j];
                turned.Observe(f,f.received);
            }
            Check(turned.State==GameBodyMotion.LegMotion.SideKick,"Side direction follows turned body, side "+side);
        }
        var jump=new GameBodyMotion();jump.Observe(Frame(1,11),11);jump.Observe(Frame(2,11.1),11.1);
        var impossible=Frame(3,11.2,0,"front");impossible.joints[27].y=3;
        jump.Observe(impossible,11.2);Check(!jump.IsStrike && !jump.ContactWindow(11.2),"Impossible limb geometry still rejects a teleport");
    }
    private static void Contacts()
    {
        var root=new GameObject("Leg bag contact validation");var bag=root.AddComponent<PracticeHeavyBag>();bag.Configure(1.7f);
        try
        {
            Physics.SyncTransforms();
            for(int limb=2;limb<6;limb++)
            {
                bag.ResetContacts();long action=100+limb;double time=10+limb;
                bag.SampleAssistedLimb(limb,action,new Vector3(0,0,-.6f),.02f,time,true,false);
                bool hit=false;
                for(int i=1;i<=6;i++) hit |= bag.SampleAssistedLimb(limb,action,new Vector3(0,0,-.6f+i*.06f),.02f,time+i*.02,true,true);
                Check(hit,"Observed limb "+limb+" reaches a physical bag contact");
                int count=bag.HitCount;bag.ResetContacts();
                bag.SampleAssistedLimb(limb,action,new Vector3(0,0,-.6f),.02f,time+.5,true,true);
                for(int i=1;i<=6;i++) bag.SampleAssistedLimb(limb,action,new Vector3(0,0,-.6f+i*.06f),.02f,time+.5+i*.02,true,true);
                Check(bag.HitCount==count,"Limb "+limb+" scores once per action, including contact reset");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    private static void Summary()
    {
        var s=new GameBodySessionStats("test","Full",0);s.Cue("squat");s.Punch(0,PunchIntent.Signal.Commit,1,1.05);
        s.Leg("Knee",1,1,1,1.06);s.Hit();s.Finish(true,null,1,0);
        string json=JsonUtility.ToJson(s);Check(json.Contains("game-body-1") && s.segments[0].hits==1 && s.segments[0].knees==1,"Saved summary separates detector decisions and physical hits");
        Check(s.Markdown().Contains("not ground truth"),"Summary does not present speech cues as accuracy labels");
    }
    public static void RunBatch()
    {
        try { Run();GameBodyReplayValidation.Run();SmoothGameBodyValidation.Run();BoxingPriorityValidation.Run();MediaPipeOctoberReplayValidation.Run();MediaPipeBoxingValidation.Run();EngineComparisonValidation.Run();FullBodyTestValidation.Run();UnityEditor.EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e);UnityEditor.EditorApplication.Exit(1); }
    }
}
