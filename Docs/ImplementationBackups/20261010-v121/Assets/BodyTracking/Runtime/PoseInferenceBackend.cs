using System;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;

namespace FitFight.BodyTracking
{
    // Owned outputs cross the worker/main-thread boundary; no native result/image escapes its lifetime.
    public sealed class PoseInferenceResult
    {
        public float[] X, Y;
        public Vector2[] Image;
        public Vector3[] World;
        public float[] Confidence;
        public float ComputeMs;
        public bool HasPose;
        public PoseCameraFrame Frame;
        public double InferenceStartedAt, InferenceCompletedAt;
        public long WorkerSequence;
    }

    public sealed class PoseInferenceBackend : IDisposable
    {
        public readonly BodyTrackingSystem Engine;
        private readonly Task initialization;
        private readonly BlockingCollection<Action> jobs=new BlockingCollection<Action>(3);
        private readonly Thread workerThread;
        public string DelegateName { get; private set; } = "CPU";
        public string GpuFallback { get; private set; }
        private Task<T> Enqueue<T>(Func<T> work)
        {
            var completion=new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            jobs.Add(() => { try { completion.SetResult(work()); } catch(Exception e) { completion.SetException(e); } });
            return completion.Task;
        }
        private Task<PoseInferenceResult> inference;
        private IntPtr ncnn;
        private PoseLandmarker landmarker;
        private bool disposed;
        public Task Disposal { get; private set; }
        private long lastTimestamp = -1;
        public readonly float[] FrameFloats;
        public uint[] FrameRGBA { get; private set; }
        private PoseInferenceResult output;
        private PoseInferenceResult published, consumer;
        private readonly object resultLock=new object();
        private bool resultWaiting;
        private IPoseCameraSource cameraSource;
        private volatile Exception cameraError;
        private readonly PoseWorkerClock clock;
        private long workerSequence, overwrittenResults;
        private volatile PoseInputRecorder inputRecorder;
        public PoseInputRecorder InputRecorder { get => inputRecorder;set => inputRecorder=value; }
        private bool cameraRequested;
        private readonly int threadPriority;
        // Android nice value read back on the worker; int.MinValue = not requested/not Android.
        public int AppliedThreadPriority { get; private set; }=int.MinValue;
        public string ThreadPriorityError { get; private set; }
        public long WorkerCompleted => Interlocked.Read(ref workerSequence);
        public long OverwrittenResults => Interlocked.Read(ref overwrittenResults);
        private PoseInferenceResult AllocateOutput() => Engine==BodyTrackingSystem.RTMPoseNCNN
            ? new PoseInferenceResult { X=new float[26*384],Y=new float[26*512] }
            : new PoseInferenceResult { Image=new Vector2[33],World=new Vector3[33],Confidence=new float[33], Frame=new PoseCameraFrame() };
        public void StartCamera(IPoseCameraSource source)
        {
            if(disposed || Engine==BodyTrackingSystem.RTMPoseNCNN) { source?.Dispose();throw new InvalidOperationException("Camera streaming needs a live MediaPipe backend."); }
            if(inference!=null) { source?.Dispose();throw new InvalidOperationException("Drain manual inference before camera streaming."); }
            cameraRequested=true;
            Enqueue(() => {
                cameraSource?.Dispose();cameraSource=source;
                lock(resultLock) { output=AllocateOutput();published=AllocateOutput();consumer=AllocateOutput();resultWaiting=false; }
                return true;
            });
        }
        public void StopCamera()
        { InputRecorder?.Dispose();cameraRequested=false;if(!disposed) Enqueue(() => { cameraSource?.Dispose();cameraSource=null;lock(resultLock) resultWaiting=false;return true; }); }
        private void Work()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            bool attached=AndroidJNI.AttachCurrentThread()==0;
            if(threadPriority!=0)
            {
                // Android nice value for this worker only (DISPLAY = -4). Read back what was applied.
                try
                {
                    using var process=new AndroidJavaClass("android.os.Process");
                    process.CallStatic("setThreadPriority",threadPriority);
                    AppliedThreadPriority=process.CallStatic<int>("getThreadPriority",process.CallStatic<int>("myTid"));
                }
                catch(Exception e) { ThreadPriorityError=e.Message; }
            }
#endif
            try
            {
                while(!jobs.IsCompleted)
                {
                    if(jobs.TryTake(out var job,cameraSource==null?25:0)) { job();continue; }
                    if(cameraSource==null) continue;
                    try
                    {
                        if(!cameraSource.TryGetFrame(out var frame)) { Thread.Sleep(1);continue; }
                        long stamp=Math.Max((long)(frame.AvailableAt*1000),lastTimestamp+1);lastTimestamp=stamp;
                        // Copy tiny metadata before the source reuses its frame descriptor.
                        CopyFrame(frame,output.Frame);
                        output.InferenceStartedAt=clock.Now;
                        Detect(frame.Pixels,frame.Width,frame.Height,stamp,output);
                        output.InferenceCompletedAt=clock.Now;
                        output.ComputeMs=(float)(output.InferenceCompletedAt-output.InferenceStartedAt)*1000;
                        output.WorkerSequence=Interlocked.Increment(ref workerSequence);
                        InputRecorder?.Capture(frame,output.WorkerSequence,clock.Now);
                        lock(resultLock)
                        {
                            if(resultWaiting) Interlocked.Increment(ref overwrittenResults);
                            var spare=published;published=output;output=spare;resultWaiting=true;
                        }
                    }
                    catch(Exception e) {
                        cameraError=e;
                        try { cameraSource?.Dispose(); } catch(Exception cleanup) { cameraError=new AggregateException(e,cleanup); }
                        finally { cameraSource=null; }
                    }
                }
            }
            finally
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if(attached) AndroidJNI.DetachCurrentThread();
#endif
            }
        }
        private static void CopyFrame(PoseCameraFrame a,PoseCameraFrame b)
        {
            b.Pixels=IntPtr.Zero;b.Width=a.Width;b.Height=a.Height;b.Sequence=a.Sequence;b.SourceId=a.SourceId;
            b.SensorRotation=a.SensorRotation;b.Front=a.Front;b.AvailableAt=a.AvailableAt;
            b.ConversionStartedAt=a.ConversionStartedAt;b.ConversionCompletedAt=a.ConversionCompletedAt;
            b.SensorTimestamp=a.SensorTimestamp;b.SensorClockComparable=a.SensorClockComparable;
            b.ExposureMs=a.ExposureMs;b.SensorFrameMs=a.SensorFrameMs;b.SensorIso=a.SensorIso;b.AeState=a.AeState;
            b.AppliedFpsMin=a.AppliedFpsMin;b.AppliedFpsMax=a.AppliedFpsMax;
        }
        private void Detect(IntPtr pixels,int width,int height,long timestamp,PoseInferenceResult result)
        {
            result.HasPose=false;
            using var image=new Mediapipe.Image(Mediapipe.ImageFormat.Types.Format.Srgba,width,height,width*4,pixels,NoFree);
            if(landmarker.TryDetectForVideo(image,timestamp,null,ref detection)
                && detection.poseLandmarks.Count>0 && detection.poseWorldLandmarks.Count>0)
            {
                var points=detection.poseLandmarks[0].landmarks;var world=detection.poseWorldLandmarks[0].landmarks;
                if(points.Count!=33 || world.Count!=33) throw new InvalidOperationException("Expected 33 MediaPipe landmarks.");
                result.HasPose=true;
                for(int i=0;i<33;i++)
                {
                    result.Image[i]=new Vector2(points[i].x,points[i].y);
                    result.World[i]=new Vector3(world[i].x,-world[i].y,world[i].z);
                    result.Confidence[i]=Math.Min(Math.Min(points[i].visibility??0,points[i].presence??0),world[i].visibility??1);
                }
            }
        }
        private PoseLandmarkerResult detection=PoseLandmarkerResult.Alloc(1);
        public void PrepareFrameBuffer(int width,int height)
        {
            if(Engine != BodyTrackingSystem.RTMPoseNCNN && (FrameRGBA == null || FrameRGBA.Length != width*height))
                FrameRGBA=new uint[width*height];
        }
        public bool Ready
        {
            get
            {
                if (disposed) return false;
                if (initialization.IsFaulted) throw initialization.Exception.GetBaseException();
                return initialization.IsCompleted;
            }
        }
        public PoseInferenceBackend(BodyTrackingSystem engine, byte[] model, byte[] weights = null, bool useGpu = false, int threadPriority = 0)
        {
            Engine = engine;this.threadPriority=threadPriority;clock=new PoseWorkerClock(Time.realtimeSinceStartupAsDouble);
            output=AllocateOutput();published=AllocateOutput();consumer=AllocateOutput();
            if(engine == BodyTrackingSystem.RTMPoseNCNN)
            { FrameFloats=new float[3*192*256]; }
            workerThread=new Thread(Work)
                { IsBackground=true, Name="FitFight pose inference" };
            workerThread.Start();
            initialization = Enqueue(() =>
            {
                if (engine == BodyTrackingSystem.RTMPoseNCNN)
                {
                    ncnn = ff_pose_create(model, model.Length, weights, weights.Length, 2);
                    if (ncnn == IntPtr.Zero) throw new InvalidOperationException("ncnn could not load the RTMPose model.");
                }
                else
                {
                    PoseLandmarker Create(BaseOptions.Delegate target) => PoseLandmarker.CreateFromOptions(
                        new PoseLandmarkerOptions(new BaseOptions(target, modelAssetBuffer:model), RunningMode.VIDEO, numPoses:1));
                    if(useGpu)
                    {
                        try { landmarker=Create(BaseOptions.Delegate.GPU);DelegateName="GPU"; }
                        catch(Exception e) { GpuFallback=e.Message;landmarker=Create(BaseOptions.Delegate.CPU);DelegateName="CPU (GPU fallback)"; }
                    }
                    else landmarker=Create(BaseOptions.Delegate.CPU);
                }
                return true;
            });
        }
        public void Begin(float[] chw, uint[] rgba, int width, int height, long timestamp)
        {
            if (!Ready || inference != null || cameraRequested) throw new InvalidOperationException("Inference backend is not idle.");
            inference = Enqueue(() =>
            {
                long monotonic=Math.Max(timestamp,lastTimestamp+1);lastTimestamp=monotonic;
                var timer = Stopwatch.StartNew();
                var result = output; result.HasPose=false;result.Frame=null;result.InferenceStartedAt=clock.Now;
                if (Engine == BodyTrackingSystem.RTMPoseNCNN)
                {
                    if (chw == null || chw.Length != 3 * 192 * 256) throw new ArgumentException("Expected CHW 192x256 input.");

                    int error = ff_pose_infer(ncnn, chw, result.X, result.Y);
                    if (error != 0) throw new InvalidOperationException("ncnn inference failed: " + error);
                    result.HasPose=true;
                }
                else
                {
                    if (rgba == null || rgba.Length != width * height) throw new ArgumentException("Expected packed RGBA image.");
                    var pin = GCHandle.Alloc(rgba, GCHandleType.Pinned);
                    try
                    {
                        Detect(pin.AddrOfPinnedObject(),width,height,monotonic,result);
                    }
                    finally { pin.Free(); }
                }
                result.InferenceCompletedAt=clock.Now;
                result.ComputeMs = (float)timer.Elapsed.TotalMilliseconds;
                return result;
            });
        }
        public bool TryTake(out PoseInferenceResult result)
        {
            result = null;
            if(cameraError!=null) throw new InvalidOperationException("Camera inference worker failed.",cameraError);
            lock(resultLock)
            {
                if(resultWaiting)
                {
                    var spare=consumer;consumer=published;published=spare;resultWaiting=false;
                    result=consumer;return true;
                }
            }
            if (inference == null || !inference.IsCompleted) return false;
            var completed = inference; inference = null;
            result = completed.GetAwaiter().GetResult();
            return true;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            // FIFO on the same worker preserves GPU context affinity through destruction.
            Disposal=Enqueue(() =>
            {
                InputRecorder?.Dispose();
                cameraSource?.Dispose();cameraSource=null;
                if(ncnn!=IntPtr.Zero) { ff_pose_destroy(ncnn);ncnn=IntPtr.Zero; }
                (landmarker as IDisposable)?.Dispose();landmarker=null;
                _=initialization.Exception;_=inference?.Exception;
                return true;
            });
            jobs.CompleteAdding();
        }
        private static readonly Mediapipe.ImageFrame.Deleter NoFree = IgnoreImageMemory;
        [AOT.MonoPInvokeCallback(typeof(Mediapipe.ImageFrame.Deleter))]
        private static void IgnoreImageMemory(IntPtr pointer) { }

        [DllImport("fitfight_pose", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ff_pose_create(byte[] param, int paramLength, byte[] weights, int weightsLength, int threads);
        [DllImport("fitfight_pose", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ff_pose_infer(IntPtr context, float[] input, [Out] float[] x, [Out] float[] y);
        [DllImport("fitfight_pose", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ff_pose_destroy(IntPtr context);
    }
}
