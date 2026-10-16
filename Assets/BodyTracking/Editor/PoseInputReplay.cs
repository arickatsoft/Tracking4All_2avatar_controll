using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

public static class PoseInputReplay
{
    private sealed class Frame
    {
        public int Width,Height,CameraSequence,SourceId,Rotation;public long InferenceSequence;
        public double AvailableAt;public bool Front;public uint[] Pixels;
    }
    private static IEnumerable<Frame> Read(string path)
    {
        using var zip=new GZipStream(File.OpenRead(path),CompressionMode.Decompress);using var reader=new BinaryReader(zip);
        if(reader.ReadInt32()!=0x31524646) throw new InvalidDataException("Expected FFR1 archive.");
        while(true)
        {
            int first=zip.ReadByte();if(first<0) yield break;
            var widthBytes=reader.ReadBytes(3);if(widthBytes.Length!=3) throw new InvalidDataException("Truncated width.");
            int width=first|widthBytes[0]<<8|widthBytes[1]<<16|widthBytes[2]<<24;
            var f=new Frame { Width=width,Height=reader.ReadInt32(),CameraSequence=reader.ReadInt32(),SourceId=reader.ReadInt32(),InferenceSequence=reader.ReadInt64(),AvailableAt=reader.ReadDouble() };
            reader.ReadDouble();reader.ReadDouble();f.Rotation=reader.ReadInt32();f.Front=reader.ReadBoolean();int length=reader.ReadInt32();
            if(f.Width<1 || f.Width>512 || f.Height<1 || f.Height>512 || length!=f.Width*f.Height*4) throw new InvalidDataException("Invalid archive dimensions.");
            byte[] bytes=reader.ReadBytes(length);if(bytes.Length!=length) throw new InvalidDataException("Truncated RGBA frame.");
            f.Pixels=new uint[length/4];Buffer.BlockCopy(bytes,0,f.Pixels,0,length);yield return f;
        }
    }
    private static uint[] Resize(Frame f,int max,out int w,out int h)
    {
        float ratio=Math.Min(1,max/(float)Math.Max(f.Width,f.Height));w=Math.Max(1,(int)Math.Round(f.Width*ratio));h=Math.Max(1,(int)Math.Round(f.Height*ratio));
        if(w==f.Width && h==f.Height) return f.Pixels;
        var output=new uint[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++) output[y*w+x]=f.Pixels[Math.Min(f.Height-1,(int)((y+.5f)*f.Height/h))*f.Width+Math.Min(f.Width-1,(int)((x+.5f)*f.Width/w))];
        return output;
    }
    public static void Compare(string path)
    {
        string folder=Path.GetFullPath("Logs/PoseInputReplay/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(folder);
        var baseline=new List<Vector2[]>();var baselineScores=new List<float[]>();var notes=new StringBuilder();
        notes.AppendLine("# Identical inference-input replay\n\nArchive: "+path+"\n\nCPU host measurements, not Android speed. Landmark disagreement is not labelled accuracy. Confidence is not calibrated across models. The smaller variant downsamples the saved RGBA input; camera-side YUV scaling can differ slightly.\n");
        notes.AppendLine("| Variant | Frames / detected | Median / p95 compute ms | Common visible landmarks | Mean image disagreement px |\n|---|---:|---:|---:|---:|");
        foreach(var variant in new[]{(BodyTrackingSystem.MediaPipeFull,512),(BodyTrackingSystem.MediaPipeFull,384),(BodyTrackingSystem.MediaPipeLite,512)})
        {
            var engine=variant.Item1;int max=variant.Item2;string name=engine+"-"+max;
            using var backend=new PoseInferenceBackend(engine,File.ReadAllBytes("Assets/StreamingAssets/pose_landmarker_"+(engine==BodyTrackingSystem.MediaPipeLite?"lite":"full")+".bytes"));
            Wait(()=>backend.Ready);
            var compute=new List<float>();int index=0,detected=0,common=0;double error=0;
            using var csv=new StreamWriter(Path.Combine(folder,name+".csv"));csv.WriteLine("inputInferenceSequence,cameraSequence,capturedAt,width,height,hasPose,computeMs,commonLandmarks,meanImageDifferencePixels");
            foreach(var frame in Read(path))
            {
                uint[] pixels=Resize(frame,max,out int w,out int h);backend.Begin(null,pixels,w,h,(long)(frame.AvailableAt*1000));
                PoseInferenceResult result=null;Wait(()=>backend.TryTake(out result));compute.Add(result.ComputeMs);if(result.HasPose) detected++;
                int matched=0;double delta=0;
                if(name=="MediaPipeFull-512") {
                    baseline.Add(result.HasPose?(Vector2[])result.Image.Clone():null);
                    baselineScores.Add(result.HasPose?(float[])result.Confidence.Clone():null);
                } else if(index<baseline.Count && result.HasPose && baseline[index]!=null) {
                    for(int j=0;j<33;j++) if(result.Confidence[j]>=.3f && baselineScores[index][j]>=.3f) {
                        var d=result.Image[j]-baseline[index][j];delta+=Math.Sqrt(d.x*d.x*frame.Width*frame.Width+d.y*d.y*frame.Height*frame.Height);matched++;
                    }
                }
                common+=matched;error+=delta;
                csv.WriteLine(string.Join(",",frame.InferenceSequence,frame.CameraSequence,frame.AvailableAt.ToString("R",CultureInfo.InvariantCulture),w,h,result.HasPose,result.ComputeMs.ToString("F3",CultureInfo.InvariantCulture),matched,(matched>0?delta/matched:0).ToString("F4",CultureInfo.InvariantCulture)));index++;
            }
            compute.Sort();float median=compute.Count>0?compute[(compute.Count-1)/2]:0,p95=compute.Count>0?compute[(int)((compute.Count-1)*.95)]:0;
            notes.AppendLine($"| {name} | {index}/{detected} | {median:F2}/{p95:F2} | {common} | {(common>0?(error/common).ToString("F3",CultureInfo.InvariantCulture):"baseline")} |");
            backend.Dispose();Wait(()=>backend.Disposal.IsCompleted);backend.Disposal.GetAwaiter().GetResult();
        }
        notes.AppendLine("\nNo model is promoted automatically. Review exported frames and fast punches/feet separately, then compare phone timing at the same capture settings.");
        File.WriteAllText(Path.Combine(folder,"comparison.md"),notes.ToString());Debug.Log(notes.ToString()+"\nSaved "+folder);
    }
    private static void Wait(Func<bool> condition)
    {
        var clock=System.Diagnostics.Stopwatch.StartNew();while(!condition()) { if(clock.Elapsed.TotalSeconds>45) throw new TimeoutException("Input replay timed out.");Thread.Sleep(1); }
    }
    public static void RunBatch()
    {
        try { string path=Environment.GetEnvironmentVariable("FF_POSE_INPUT_ARCHIVE");if(string.IsNullOrEmpty(path)) path="Logs/input-archive-roundtrip.ffr.gz";Compare(path);EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
}
