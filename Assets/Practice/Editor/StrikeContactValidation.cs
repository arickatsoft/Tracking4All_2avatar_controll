using System;
using System.IO;
using System.Text;
using System.Reflection;
using FitFight.Practice;
using Tracking4All;
using UnityEditor;
using UnityEngine;

public static class StrikeContactValidation
{
    private static readonly StringBuilder report=new StringBuilder();
    private static int checks;
    private static void Check(bool value,string label)
    { if(!value)throw new Exception(label);checks++;report.AppendLine("PASS "+label); }
    public static void Run()
    {
        report.Clear();checks=0;
        try
        {
            Targets();CameraSettings();Contacts120();
            report.AppendLine($"PASS {checks} strike-contact and capture-request checks; device startup and visual motion still require phone verification.");
        }
        catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { Directory.CreateDirectory("Logs/Practice");File.WriteAllText("Logs/Practice/strike-contact-validation.txt",report.ToString()); }
    }
    private static void Targets()
    {
        var surface=new Vector3(2,1,3);var facing=Vector3.forward;
        var still=AssistedPunchController.LegContactTarget(surface,Vector3.zero,facing,true);
        Check(Vector3.Distance(still,surface-facing*.035f)<.00001f,"Stationary bag retains the original contact inset");
        var moving=AssistedPunchController.LegContactTarget(surface,new Vector3(.2f,0,.3f),facing,true);
        Check((moving-still).z>0 && (moving-still).x>0,"Foot target leads the measured bag motion");
        var fast=AssistedPunchController.LegContactTarget(surface,new Vector3(20,-30,40),facing,true);
        Check(Mathf.Abs(Vector3.Distance(fast,still)-.10f)<.00001f,"Fast bag motion cannot move the captured goal more than ten centimeters");
        Check(AssistedPunchController.LegContactTarget(surface,Vector3.one*20,facing,false)==still,"Knee goal keeps its existing stationary targeting");
        Check(AssistedPunchController.LegContactTarget(surface,new Vector3(float.NaN,0,0),facing,true)==still,"Invalid bag velocity cannot poison the strike goal");
    }
    // App 1.20 contact rules on a stationary bag at the origin (front surface z=-0.20 m).
    private static void Contacts120()
    {
        var root=new GameObject("1.20 contact validation");var bag=root.AddComponent<PracticeHeavyBag>();bag.Configure(1.7f);
        try
        {
            Physics.SyncTransforms();
            Vector3[] Probes(float ankleZ) => new[]{ new Vector3(0,0,ankleZ+.17f),new Vector3(0,0,ankleZ+.12f),new Vector3(0,0,ankleZ),new Vector3(0,.12f,ankleZ-.02f) };
            // Ankle stops 10 cm short (outside its 9.5 cm sphere); the shoe reaches the bag.
            bool ankleOnly=false;bag.ResetContacts();
            for(int i=0;i<=6;i++) ankleOnly|=bag.SampleAssistedLimb(2,500,new Vector3(0,0,-.70f+i*(.40f/6)),.02f,20+i*.02,true,i>0);
            Check(!ankleOnly,"Ankle-point contact alone misses a reach-limited front kick (1.17 behaviour)");
            bool segment=false;bag.ResetContacts();var sample=default(PracticeHeavyBag.ContactSample);
            for(int i=0;i<=6;i++)
                if(bag.SampleAssistedSegment(2,501,Probes(-.70f+i*(.40f/6)),.02f,21+i*.02,true,i>0)) { segment=true;sample=bag.GetContactSample(2); }
            Check(segment && sample.hit && (sample.reason.Contains("shoe tip") || sample.reason.Contains("ball")),"Shoe tip/ball contact scores the same reach-limited kick");
            int count=bag.HitCount;bag.ResetContacts();
            for(int i=0;i<=6;i++) bag.SampleAssistedSegment(2,501,Probes(-.70f+i*(.40f/6)),.02f,22+i*.02,true,true);
            Check(bag.HitCount==count,"A foot action scores once even when several probes touch");
            bool outside=false;bag.ResetContacts();
            for(int i=0;i<=6;i++) outside|=bag.SampleAssistedSegment(3,502,Probes(-.70f+i*(.40f/6)),.02f,23+i*.02,true,false);
            Check(!outside,"Foot probes cannot score outside the strike contact window");
            bool inside=false;bag.ResetContacts();
            for(int i=0;i<=4;i++) inside|=bag.SampleAssistedSegment(3,503,Probes(-.17f+i*.01f),.02f,24+i*.02,true,true);
            Check(!inside,"A foot already deep inside the bag cannot score");
            // A committed fist starting 6 cm from the surface (bag swung close to the guard).
            bool close=false;bag.ResetContacts();
            close|=bag.SampleAssistedPunch(0,504,new Vector3(0,0,-.26f),.02f,25,true,false);
            for(int i=1;i<=3;i++) close|=bag.SampleAssistedPunch(0,504,new Vector3(0,0,-.26f+i*.03f),.02f,25+i*.02,true,true);
            Check(close,"Committed punch scores when the swinging bag rests a few cm from the guard fist");
            // Authored seeding/blend jump restarts the sweep instead of disarming the action.
            bool jumped=false;bag.ResetContacts();
            bag.SampleAssistedPunch(1,505,new Vector3(0,0,-.90f),.016f,26,true,false);
            jumped|=bag.SampleAssistedPunch(1,505,new Vector3(0,0,-.40f),.001f,26.001,true,true);
            Check(!jumped && bag.GetContactSample(1).reason=="Reseeded after authored jump","Authored jump reseeds without sweeping across it");
            for(int i=1;i<=4;i++) jumped|=bag.SampleAssistedPunch(1,505,new Vector3(0,0,-.40f+i*.05f),.016f,26.001+i*.016,true,true);
            Check(jumped,"Punch still scores after an authored jump");
            // Tracked (non-assisted) sampling keeps the teleport disarm.
            bag.ResetContacts();bag.Sample(0,new Vector3(0,0,-.9f),.016f,27,true);
            bool tracked=bag.Sample(0,new Vector3(0,0,-.19f),.001f,27.001,true);
            Check(!tracked && bag.GetContactSample(0).reason=="Teleport or excessive speed","Tracked-limb teleport still disarms and cannot score");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    private static void CameraSettings()
    {
        // Inactive backend: exercise the actual FPS override/restoration without opening a camera.
        var root=new GameObject("Capture request settings check");root.SetActive(false);
        try
        {
            var backend=root.AddComponent<UnityWebCamBackend>();var fps=new FloatSetting();fps.Set(30);
            typeof(UnityWebCamBackend).GetField("cameraSettings",BindingFlags.NonPublic|BindingFlags.Instance)
                .SetValue(backend,new WebCamSettings{frameRate=fps});
            Check(backend.ConfigureCapture(false,60) && backend.RequestedFrameRate==60,"Combined request applies sixty FPS");
            Check(!backend.ConfigureCapture(false,60),"Identical capture request does not reopen the camera");
            Check(backend.ConfigureCapture(false,30) && backend.RequestedFrameRate==30,"Engine can switch frame rate while retaining original preference");
            Check(backend.ConfigureCapture(false,60) && backend.ConfigureCapture(false,null) && backend.RequestedFrameRate==30,"Baseline restoration returns to the original camera FPS");
            Check(!backend.ConfigureCapture(false,null),"Repeated baseline restoration does not reopen capture");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    public static void RunBatch()
    {
        try { Run();GameBodyValidation.Run();GameBodyReplayValidation.Run();SmoothGameBodyValidation.Run();BoxingPriorityValidation.Run();MediaPipeOctoberReplayValidation.Run();MediaPipeBoxingValidation.Run();EngineComparisonValidation.Run();FullBodyTestValidation.Run();EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
}
