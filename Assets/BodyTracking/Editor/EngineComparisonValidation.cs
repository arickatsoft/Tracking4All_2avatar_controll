using System;
using System.IO;
using System.Text;
using System.Threading;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

public static class EngineComparisonValidation
{
    private static readonly StringBuilder report=new StringBuilder();
    private static void Check(bool condition,string label)
    { if(!condition) throw new Exception(label); report.AppendLine("PASS "+label); }
    private static void Wait(Func<bool> predicate)
    {
        var timer=System.Diagnostics.Stopwatch.StartNew();
        while(!predicate()) { if(timer.Elapsed.TotalSeconds>30) throw new TimeoutException("Native inference did not complete."); Thread.Sleep(5); }
    }
    [MenuItem("Tools/FitFight/Tracking/Validate engine comparison")]
    public static void Run()
    {
        report.Clear();
        try
        {
            ValidateRGBA();
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            texture.LoadImage(File.ReadAllBytes("NativePose/tests/standing.png"));
            var pixels=texture.GetPixels32(); var rgba=new uint[pixels.Length];
            for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++)
            {
                var c=pixels[(texture.height-1-y)*texture.width+x];
                rgba[y*texture.width+x]=(uint)(c.r | c.g<<8 | c.b<<16 | c.a<<24);
            }
            foreach(var engine in new[]{BodyTrackingSystem.RTMPoseNCNN,BodyTrackingSystem.MediaPipeLite,BodyTrackingSystem.MediaPipeFull})
            {
                byte[] model=engine==BodyTrackingSystem.RTMPoseNCNN ? Resources.Load<TextAsset>("PoseComparison/NcnnParam").bytes
                    : File.ReadAllBytes("Assets/StreamingAssets/pose_landmarker_"+(engine==BodyTrackingSystem.MediaPipeLite?"lite":"full")+".bytes");
                byte[] weights=engine==BodyTrackingSystem.RTMPoseNCNN ? Resources.Load<TextAsset>("PoseComparison/NcnnWeights").bytes : null;
                using(var backend=new PoseInferenceBackend(engine,model,weights))
                {
                    Wait(()=>backend.Ready);
                    var chw=new float[192*256*3];
                    backend.Begin(chw,rgba,texture.width,texture.height,1000);
                    PoseInferenceResult result=null; Wait(()=>backend.TryTake(out result));
                    if(engine==BodyTrackingSystem.RTMPoseNCNN)
                        Check(result.X.Length==26*384 && result.Y.Length==26*512,"ncnn real inference output shape");
                    else
                    {
                        Check(result.HasPose && result.World.Length==33,"Real "+engine+" detects recorded standing person / world33");
                        Check(result.Confidence[23]>.5f && RTMPoseMath.Finite(result.World[23]),engine+" world landmarks valid");
                    }
                    backend.Begin(chw,rgba,texture.width,texture.height,1000); // duplicate capture must become monotonic
                    Wait(()=>backend.TryTake(out result));
                    Check(result.ComputeMs>0,engine+" second video frame / monotonic timestamp");
                    report.AppendLine("HOST compute ms "+engine+": "+result.ComputeMs.ToString("F2"));
                    if(engine != BodyTrackingSystem.RTMPoseNCNN)
                    {
                        backend.Begin(null,new uint[rgba.Length],texture.width,texture.height,1002);
                        Wait(()=>backend.TryTake(out result));
                        Check(!result.HasPose,engine+" blank frame clears detection instead of reusing previous outputs");
                    }
                    backend.Begin(chw,rgba,texture.width,texture.height,1003);
                    backend.Dispose(); Wait(()=>backend.Disposal.IsCompleted); backend.Disposal.GetAwaiter().GetResult();
                    Check(true,engine+" in-flight inference released safely");
                }
                // Switch away while initialization/inference is pending; disposal must not race native work.
                var abandoned=new PoseInferenceBackend(engine,model,weights); abandoned.Dispose(); Wait(()=>abandoned.Disposal.IsCompleted); abandoned.Disposal.GetAwaiter().GetResult();
                Check(!abandoned.Ready,engine+" abandoned initialization is detached");
            }
            UnityEngine.Object.DestroyImmediate(texture);
            var world=new MediaPipeWorldPose(); var source=new Vector3[26]; var scores=new float[26];
            for(int i=0;i<26;i++) scores[i]=1;
            source[5]=new Vector3(.2f,.5f,0); source[6]=new Vector3(-.2f,.5f,0);
            source[11]=new Vector3(.1f,0,0); source[12]=new Vector3(-.1f,0,0); source[18]=new Vector3(0,.5f,0);
            source[9]=new Vector3(.4f,.25f,-.3f); source[19]=Vector3.zero;
            for(int i=0;i<20;i++) world.CollectNeutral(source);
            var output=new Vector3[26];var usable=new float[26];var tuning=new RTMPoseTuning{filtering=false,prediction=0};
            world.Step(source,scores,1,1.7f,tuning,output,usable);
            Check(output[9].z<-.25f,"MediaPipe retains measured wrist depth instead of 2.5D prior");
            Check(Quaternion.Angle(world.BodyRotation(output,false,false),Quaternion.identity)<.01f,"World neutral torso identity");
            var bend=Quaternion.Euler(30,0,0);
            source[5]=bend*source[5];source[6]=bend*source[6];source[18]=(source[5]+source[6])*.5f;
            world.Step(source,scores,1.1,1.7f,tuning,output,usable);
            Check(Quaternion.Angle(world.BodyRotation(output,false,false),Quaternion.identity)>25,"World torso bend survives camera calibration");
            scores[9]=0;world.Step(source,scores,1.2,1.7f,tuning,output,usable);
            Check(!world.Observed(9),"Missing MediaPipe wrist cannot claim observed contact");
            world.Reset();Check(!world.Observed(5),"Engine/calibration reset clears 3D filter history");
            Check(BodyTrackingSelection.Labels.Length==5,"Five distinct selector choices");
            report.AppendLine("Phone throughput/accuracy are not established by these host checks.");
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/engine-comparison-validation.txt",report.ToString());
            Debug.Log(report.ToString());
        }
        catch(Exception e)
        { report.AppendLine("FAIL "+e);File.WriteAllText("Logs/engine-comparison-validation.txt",report.ToString());throw; }
    }
    private static void ValidateRGBA()
    {
        var shader=UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("RTMPose/Preprocess"));
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true); texture.filterMode=FilterMode.Point;
        var palette=new[]{new Color32(255,0,0,255),new Color32(0,255,0,255),new Color32(0,0,255,255),new Color32(255,255,0,255)};
        texture.SetPixels32(palette);texture.Apply();
        int[][] corners={new[]{2,3,0,1},new[]{0,2,1,3},new[]{1,0,3,2},new[]{3,1,2,0}};
        using(var buffer=new ComputeBuffer(8*8,4))
        {
            int kernel=shader.FindKernel("MediaPipeRGBA");shader.SetBuffer(kernel,"RGBA",buffer);
            shader.SetTexture(kernel,"CameraTexture",texture);shader.SetInt("ImageWidth",8);shader.SetInt("ImageHeight",8);
            var output=new uint[64]; int[] samples={0,7,56,63};
            for(int r=0;r<4;r++) for(int flip=0;flip<2;flip++)
            {
                shader.SetInt("Rotation",r*90);shader.SetInt("VerticalFlip",flip);shader.Dispatch(kernel,1,1,1);buffer.GetData(output);
                for(int i=0;i<4;i++)
                {
                    int color=corners[r][i];if(flip==1) color^=2;
                    var c=palette[color];uint packed=(uint)(c.r|c.g<<8|c.b<<16|255<<24);
                    Check(output[samples[i]]==packed,$"RGBA upright orientation {r*90} flip {flip} corner {i}");
                }
            }
        }
        UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(shader);
    }
    public static void DumpHomeView()
    {
        var home=FitFightSelectionView.Create(out _,out _);
        var root=new GameObject("Home view validation camera");var camera=root.AddComponent<Camera>();
        var target=new RenderTexture(540,960,24);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.025f,.03f);
        var canvas=home.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        // Edit-mode render does not advance Toggle's fade coroutine; capture its settled selection state.
        int selected=0;
        foreach(var toggle in home.GetComponentsInChildren<UnityEngine.UI.Toggle>())
        { toggle.graphic.canvasRenderer.SetAlpha(toggle.isOn?1:0); if(toggle.isOn) selected++; }
        if(selected!=1) throw new Exception("Home selector must have exactly one active engine.");
        Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(540,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();
        File.WriteAllBytes("Logs/engine-comparison-home.png",image.EncodeToPNG());RenderTexture.active=previous;
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(home);UnityEngine.Object.DestroyImmediate(root);target.Release();UnityEngine.Object.DestroyImmediate(target);
        EditorApplication.Exit(0);
    }
    public static void RunBatch()
    { try { Run(); RTMPoseValidation.Run(); if(File.ReadAllText("Logs/RTMPose-validation.txt").Contains("FAIL:")) throw new Exception("RTMPose validation failed."); PunchValidation.Run(); FullBodyTestValidation.Run(); ResponsiveMoveValidation.Run(); EditorApplication.Exit(0); }
      catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); } }
}
