using System;
using System.IO;
using System.Text;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

public static class MediaPipeIdleValidation
{
    private static readonly StringBuilder report=new StringBuilder();
    private static void Check(bool value,string label) { if(!value) throw new Exception(label);report.AppendLine("PASS "+label); }
    private static void Fixture(out Vector2[] image,out Vector3[] world,out float[] scores)
    {
        image=new Vector2[26];world=new Vector3[26];scores=new float[26];
        for(int i=0;i<26;i++) {image[i]=new Vector2(.5f,.4f);scores[i]=1;}
        world[5]=new Vector3(.2f,.5f,0);world[6]=new Vector3(-.2f,.5f,0);
        world[11]=new Vector3(.1f,0,0);world[12]=new Vector3(-.1f,0,0);world[18]=new Vector3(0,.5f,0);
        world[9]=new Vector3(.4f,.2f,-.1f);
        image[5]=new Vector2(.4f,.3f);image[6]=new Vector2(.6f,.3f);
        image[11]=new Vector2(.45f,.6f);image[12]=new Vector2(.55f,.6f);image[9]=new Vector2(.3f,.4f);
    }
    public static void Run()
    {
        report.Clear();
        try
        {
            Fixture(out var image,out var source,out var scores);var gate=new MediaPipeMotionGate();
            gate.Observe(image,scores,1,1,.38f);
            for(int n=1;n<20;n++) gate.Observe(image,scores,1,1+n*.04,.38f);
            Check(gate.Amount(9)==0,"Stationary image does not activate fast-motion preset");
            image[9].x+=.12f;gate.Observe(image,scores,1,1.8,.38f);
            Check(gate.Amount(9)>.8f,"Visible fast wrist activates stronger response in one observation");
            float amount=gate.Amount(9);image[9].x+=.2f;gate.Observe(image,scores,1,1.8,.38f);
            Check(gate.Amount(9)==amount,"Duplicate timestamp cannot advance motion-gate history");
            scores[11]=0;gate.Observe(image,scores,1,1.84,.38f);
            Check(gate.Amount(9)==0,"Missing root resets boost without inventing motion");
            scores[11]=1;gate.Observe(image,scores,1,1.88,.38f);
            Check(gate.Amount(9)==0,"Reacquisition seeds image velocity instead of teleport boost");
            Fixture(out image,out source,out scores);
            var legacy=new MediaPipeWorldPose();var revised=new MediaPipeWorldPose();var aggressive=new MediaPipeWorldPose();
            for(int i=0;i<20;i++) {legacy.CollectNeutral(source);revised.CollectNeutral(source);aggressive.CollectNeutral(source);}
            var oldT=new RTMPoseTuning{mediaPipeRegionFiltering=false,minCutoff=6,speedCoefficient=8,observationInterval=.04f};
            var newT=new RTMPoseTuning{mediaPipeRegionFiltering=true,mediaPipeMotionAwareFiltering=true,minCutoff=6,speedCoefficient=8,observationInterval=.04f};
            var fastT=new RTMPoseTuning{mediaPipeRegionFiltering=true,mediaPipeMotionAwareFiltering=false,minCutoff=6,speedCoefficient=8,observationInterval=.04f};
            var a=new Vector3[26];var b=new Vector3[26];var c=new Vector3[26];var usable=new float[26];
            legacy.Step(source,scores,2,1.7f,oldT,a,usable,image);revised.Step(source,scores,2,1.7f,newT,b,usable,image);aggressive.Step(source,scores,2,1.7f,fastT,c,usable,image);
            float difference=0,oldJitter=0,newJitter=0;var anchor=b[9];
            for(int i=1;i<=80;i++)
            {
                source[9].z=-.1f+(i%2==0 ? .01f : -.01f);
                legacy.Step(source,scores,2+i*.04,1.7f,oldT,a,usable,image);
                revised.Step(source,scores,2+i*.04,1.7f,newT,b,usable,image);
                aggressive.Step(source,scores,2+i*.04,1.7f,fastT,c,usable,image);
                difference=Mathf.Max(difference,Vector3.Distance(a[9],b[9]));
                newJitter+=Vector3.Distance(anchor,b[9]);oldJitter+=Vector3.Distance(anchor,c[9]);
            }
            Check(difference<.000001f,"Motion-aware region matches legacy for depth-only idle noise");
            Check(newJitter<oldJitter*.1f,"Idle preset suppresses synthetic inferred-depth jitter");
            float legacyLag=0,revisedLag=0;
            for(int i=1;i<=8;i++)
            {
                source[9].x+=.08f;image[9].x-=.06f;
                legacy.Step(source,scores,5.2+i*.04,1.7f,oldT,a,usable,image);
                revised.Step(source,scores,5.2+i*.04,1.7f,newT,b,usable,image);
                var expected=(source[9]-(source[11]+source[12])*.5f)*(.493f/.5f);
                legacyLag+=Vector3.Distance(a[9],expected);revisedLag+=Vector3.Distance(b[9],expected);
            }
            Check(revisedLag<legacyLag,"Fast movement remains more responsive than rest filter");
            var before=b[9];scores[11]=0;source[9].x+=.02f;
            revised.Step(source,scores,5.6,1.7f,newT,b,usable,image);
            Check(revised.Observed(9),"Missing hip disables boost but does not block observed wrist");
            revised.MarkMissing(5.64,newT,b,usable);
            Check(!revised.Observed(9),"No-person result clears wrist observation");
            revised.Reset();Check(!revised.Observed(9),"Calibration/engine reset clears all filter and boost history");
            File.WriteAllText("Logs/mediapipe-idle-validation.txt",report.ToString());Debug.Log(report.ToString());
        }
        catch(Exception e) {report.AppendLine("FAIL "+e);File.WriteAllText("Logs/mediapipe-idle-validation.txt",report.ToString());throw;}
    }
    public static void RunBatch()
    {
        try
        {
            Run();MediaPipeImprovementValidation.Run();EngineComparisonValidation.Run();RTMPoseValidation.Run();
            if(File.ReadAllText("Logs/RTMPose-validation.txt").Contains("FAIL:")) throw new Exception("Tracking regression failed");
            PunchValidation.Run();FullBodyTestValidation.Run();ResponsiveMoveValidation.Run();EditorApplication.Exit(0);
        }
        catch(Exception e) {Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
