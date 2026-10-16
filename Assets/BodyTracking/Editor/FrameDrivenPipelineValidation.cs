using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FitFight.BodyTracking;
using UnityEditor;
using UnityEngine;

public static class FrameDrivenPipelineValidation
{
    private static readonly StringBuilder report=new StringBuilder();
    private static void Check(bool value,string label) { if(!value) throw new Exception(label);report.AppendLine("PASS "+label); }
    private static void Wait(Func<bool> test) {
        var clock=System.Diagnostics.Stopwatch.StartNew();
        while(!test()) { if(clock.Elapsed.TotalSeconds>45) throw new TimeoutException("Pipeline test timed out.");Thread.Sleep(2); }
    }
    private sealed class Source : IPoseCameraSource
    {
        private readonly GCHandle pin;
        private readonly PoseWorkerClock clock;
        private readonly PoseCameraFrame frame;
        public int Disposals,Frames;
        public Source(uint[] rgba,int width,int height,int id,double time) {
            pin=GCHandle.Alloc(rgba,GCHandleType.Pinned);clock=new PoseWorkerClock(time);
            frame=new PoseCameraFrame { Width=width,Height=height,Pixels=pin.AddrOfPinnedObject(),SourceId=id };
        }
        public bool TryGetFrame(out PoseCameraFrame value) {
            if(Disposals!=0) throw new ObjectDisposedException("test source");
            Thread.Sleep(3);frame.Sequence=++Frames;
            frame.AvailableAt=frame.ConversionStartedAt=frame.ConversionCompletedAt=clock.Now;value=frame;return true;
        }
        public void Dispose() { if(Interlocked.Increment(ref Disposals)!=1) throw new Exception("Source disposed twice.");pin.Free(); }
    }
    private sealed class BrokenSource : IPoseCameraSource
    {
        public int Disposals;
        public bool TryGetFrame(out PoseCameraFrame frame) { frame=null;throw new InvalidDataException("Injected camera source failure"); }
        public void Dispose() { Interlocked.Increment(ref Disposals); }
    }
    public static void Run()
    {
        report.Clear();
        try {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            texture.LoadImage(File.ReadAllBytes("NativePose/tests/standing.png"));
            var pixels=texture.GetPixels32();var rgba=new uint[pixels.Length];
            for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++) {
                var c=pixels[(texture.height-1-y)*texture.width+x];rgba[y*texture.width+x]=(uint)(c.r|c.g<<8|c.b<<16|255<<24);
            }
            var backend=new PoseInferenceBackend(BodyTrackingSystem.MediaPipeFull,File.ReadAllBytes("Assets/StreamingAssets/pose_landmarker_full.bytes"));
            Wait(()=>backend.Ready);
            var first=new Source(rgba,texture.width,texture.height,101,Time.realtimeSinceStartupAsDouble);
            var inputArchive=new PoseInputRecorder(Path.GetFullPath("Logs/pose-input-standing.ffr.gz"),Time.realtimeSinceStartupAsDouble);
            backend.InputRecorder=inputArchive;backend.StartCamera(first);
            // No render ticks and no TryTake: inference must keep consuming camera frames.
            Wait(()=>backend.WorkerCompleted>=6);
            inputArchive.Dispose();Wait(()=>inputArchive.Finished);
            Check(inputArchive.Saved>=2 && inputArchive.Error==null,"Archive captures actual inputs during camera-driven inference");
            Check(backend.WorkerCompleted>=6 && first.Frames>=6,"Camera drives successive real MediaPipe inferences without Unity scheduling");
            PoseInferenceResult result=null;Wait(()=>backend.TryTake(out result));
            Check(result.HasPose && result.Frame.SourceId==101 && result.Frame.Sequence>=5,"Latest completed result carries exact camera identity and frame metadata");
            Check(backend.OverwrittenResults>=4,"Slow main consumer replaces results instead of building a latency queue");
            long sequence=result.WorkerSequence;int frameSequence=result.Frame.Sequence;
            var snapshot=(Vector3[])result.World.Clone();
            Wait(()=>backend.WorkerCompleted>=sequence+4);
            bool unchanged=result.WorkerSequence==sequence && result.Frame.Sequence==frameSequence;
            for(int i=0;i<33;i++) unchanged &= snapshot[i]==result.World[i];
            Check(unchanged,"Triple buffer keeps consumed landmarks and metadata immutable during later inferences");
            var second=new Source(rgba,texture.width,texture.height,202,Time.realtimeSinceStartupAsDouble);
            backend.StartCamera(second);
            Wait(()=>first.Disposals==1);
            Wait(()=>backend.TryTake(out result) && result.Frame.SourceId==202);
            Check(second.Frames>0 && first.Disposals==1,"Camera replacement releases old source once and publishes the new generation");
            backend.StopCamera();Wait(()=>second.Disposals==1);
            long stopped=backend.WorkerCompleted;Thread.Sleep(40);
            Check(backend.WorkerCompleted==stopped,"Stopped camera cannot keep scheduling work");
            backend.Begin(null,rgba,texture.width,texture.height,1000);Wait(()=>backend.TryTake(out result));
            Check(result.HasPose,"Legacy/manual input remains usable after stopping camera streaming");
            var third=new Source(rgba,texture.width,texture.height,303,Time.realtimeSinceStartupAsDouble);
            backend.StartCamera(third);Wait(()=>third.Frames>=1);backend.Dispose();
            Wait(()=>backend.Disposal.IsCompleted);backend.Disposal.GetAwaiter().GetResult();
            Check(third.Disposals==1 && !backend.Ready,"In-flight shutdown releases camera and model on their owning worker");
            var brokenBackend=new PoseInferenceBackend(BodyTrackingSystem.MediaPipeFull,File.ReadAllBytes("Assets/StreamingAssets/pose_landmarker_full.bytes"));
            Wait(()=>brokenBackend.Ready);var brokenSource=new BrokenSource();brokenBackend.StartCamera(brokenSource);
            bool failed=false;
            Wait(()=> {
                try { brokenBackend.TryTake(out _);return false; }
                catch(InvalidOperationException e) { failed=e.InnerException is InvalidDataException;return true; }
            });
            Check(failed && brokenSource.Disposals==1,"Camera worker failure reaches the main consumer and releases its source");
            brokenBackend.Dispose();Wait(()=>brokenBackend.Disposal.IsCompleted);brokenBackend.Disposal.GetAwaiter().GetResult();
            Check(!brokenBackend.Ready,"Failed camera worker can still destroy the native model without hanging");
            ValidateArchive();UnityEngine.Object.DestroyImmediate(texture);
            report.AppendLine("Host checks do not establish Android camera cadence, GPU delegate gains or motion-to-photon latency.");
        } catch(Exception e) { report.AppendLine("FAIL "+e);throw; }
        finally { Directory.CreateDirectory("Logs");File.WriteAllText("Logs/frame-driven-pipeline-validation.txt",report.ToString());Debug.Log(report.ToString()); }
    }
    private static void ValidateArchive() {
        string path=Path.GetFullPath("Logs/input-archive-roundtrip.ffr.gz");
        var bytes=new byte[4*6*4];for(int i=0;i<bytes.Length;i++) bytes[i]=(byte)i;
        var pin=GCHandle.Alloc(bytes,GCHandleType.Pinned);
        try {
            var archive=new PoseInputRecorder(path,0);
            var frame=new PoseCameraFrame { Width=4,Height=6,Pixels=pin.AddrOfPinnedObject(),SourceId=5,Sequence=7,AvailableAt=.1,ConversionStartedAt=.11,ConversionCompletedAt=.12,SensorRotation=90,Front=true };
            archive.Capture(frame,11,1);archive.Capture(frame,12,2);archive.Capture(frame,13,11);
            Wait(()=>archive.Finished);Check(archive.Saved==2 && archive.Dropped==0 && archive.Error==null,"Input archive stops at ten seconds and drains bounded writer");
            using var compressed=new GZipStream(File.OpenRead(path),CompressionMode.Decompress);using var reader=new BinaryReader(compressed);
            Check(reader.ReadInt32()==0x31524646,"Lossless input archive magic/version");
            for(int n=0;n<2;n++) {
                Check(reader.ReadInt32()==4 && reader.ReadInt32()==6 && reader.ReadInt32()==7 && reader.ReadInt32()==5 && reader.ReadInt64()==11+n,"Archive dimensions/camera/inference identities round trip");
                Check(reader.ReadDouble()==.1 && reader.ReadDouble()==.11 && reader.ReadDouble()==.12 && reader.ReadInt32()==90 && reader.ReadBoolean(),"Archive exact availability/conversion/orientation metadata round trip");
                int length=reader.ReadInt32();var restored=reader.ReadBytes(length);bool equal=length==bytes.Length;
                for(int i=0;i<restored.Length;i++) equal &= restored[i]==bytes[i];
                Check(equal,"Archive preserves exact RGBA bytes supplied to the model");
            }
        } finally { pin.Free(); }
    }
    public static void RunBatch() { try { Run();EngineComparisonValidation.Run();EditorApplication.Exit(0); } catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); } }
}
