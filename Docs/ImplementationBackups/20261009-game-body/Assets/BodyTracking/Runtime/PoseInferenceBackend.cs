using System;
using System.Diagnostics;
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
    }

    public sealed class PoseInferenceBackend : IDisposable
    {
        public readonly BodyTrackingSystem Engine;
        private readonly Task initialization;
        private Task<PoseInferenceResult> inference;
        private IntPtr ncnn;
        private PoseLandmarker landmarker;
        private bool disposed;
        public Task Disposal { get; private set; }
        private long lastTimestamp = -1;
        public readonly float[] FrameFloats;
        public uint[] FrameRGBA { get; private set; }
        private readonly PoseInferenceResult output=new PoseInferenceResult();
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
        public PoseInferenceBackend(BodyTrackingSystem engine, byte[] model, byte[] weights = null)
        {
            Engine = engine;
            if(engine == BodyTrackingSystem.RTMPoseNCNN)
            { FrameFloats=new float[3*192*256]; output.X=new float[26*384]; output.Y=new float[26*512]; }
            else
            { output.Image=new Vector2[33]; output.World=new Vector3[33]; output.Confidence=new float[33]; }
            initialization = Task.Run(() =>
            {
                if (engine == BodyTrackingSystem.RTMPoseNCNN)
                {
                    ncnn = ff_pose_create(model, model.Length, weights, weights.Length, 2);
                    if (ncnn == IntPtr.Zero) throw new InvalidOperationException("ncnn could not load the RTMPose model.");
                }
                else
                {
                    var options = new PoseLandmarkerOptions(new BaseOptions(BaseOptions.Delegate.CPU,
                        modelAssetBuffer: model), RunningMode.VIDEO, numPoses: 1);
                    landmarker = PoseLandmarker.CreateFromOptions(options);
                }
            });
        }
        public void Begin(float[] chw, uint[] rgba, int width, int height, long timestamp)
        {
            if (!Ready || inference != null) throw new InvalidOperationException("Inference backend is not idle.");
            long monotonic = Math.Max(timestamp, lastTimestamp + 1); lastTimestamp = monotonic;
            inference = Task.Run(() =>
            {
                var timer = Stopwatch.StartNew();
                var result = output; result.HasPose=false;
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
                        using var image = new Mediapipe.Image(Mediapipe.ImageFormat.Types.Format.Srgba,
                            width, height, width * 4, pin.AddrOfPinnedObject(), NoFree);

                        if (landmarker.TryDetectForVideo(image, monotonic, null, ref detection)
                            && detection.poseLandmarks.Count > 0 && detection.poseWorldLandmarks.Count > 0)
                        {
                            var points = detection.poseLandmarks[0].landmarks;
                            var world = detection.poseWorldLandmarks[0].landmarks;
                            if (points.Count != 33 || world.Count != 33) throw new InvalidOperationException("Expected 33 MediaPipe landmarks.");
                            result.HasPose=true;
                            for (int i = 0; i < 33; i++)
                            {
                                result.Image[i] = new Vector2(points[i].x, points[i].y);
                                result.World[i] = new Vector3(world[i].x, -world[i].y, world[i].z);
                                // Missing optional world visibility is not evidence of an invisible joint.
                                result.Confidence[i] = Math.Min(Math.Min(points[i].visibility ?? 0, points[i].presence ?? 0), world[i].visibility ?? 1);
                            }
                        }
                    }
                    finally { pin.Free(); }
                }
                result.ComputeMs = (float)timer.Elapsed.TotalMilliseconds;
                return result;
            });
        }
        public bool TryTake(out PoseInferenceResult result)
        {
            result = null;
            if (inference == null || !inference.IsCompleted) return false;
            var completed = inference; inference = null;
            result = completed.GetAwaiter().GetResult();
            return true;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            // Never free a native network/image while an inference or initialization is still running.
            Task last = (Task)inference ?? initialization;
            Disposal=last.ContinueWith(completed =>
            {
                if (ncnn != IntPtr.Zero) { ff_pose_destroy(ncnn); ncnn = IntPtr.Zero; }
                (landmarker as IDisposable)?.Dispose(); landmarker = null;
                // Observe faults even when switching abandons a pending request.
                _ = last.Exception;
            }, TaskScheduler.Default);
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
