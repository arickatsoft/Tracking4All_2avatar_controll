using System;
using System.Threading;
using Mediapipe.Unity;
using Tracking4All;
using UnityEngine;

namespace FitFight.BodyTracking
{
    /// <summary>Upright, unmirrored CPU frames shared by native inference and a texture preview.</summary>
    public sealed class AndroidCpuCamera : IDisposable, IVideoSourceProperties
    {
        public Texture2D Texture { get; private set; }
        public uint[] Pixels { get; private set; }
        public double AvailableAt { get; private set; }
        public float ConversionMs { get; private set; }
        public int Sequence { get; private set; }
        public string Error { get; private set; }
        public string Status { get; private set; } = "Waiting for Android camera";
        public string LifecycleTrace { get; private set; }
        public int SensorRotation { get; private set; }
        public string Name => "Android Camera2 CPU";
        public int Width => Texture != null ? Texture.width : 0;
        public int Height => Texture != null ? Texture.height : 0;
        public int FrameRate { get; private set; }
        private double nextPreview;
        public float ActualFrameRate { get; private set; }
        public int ReplacedImages { get; private set; }
        public int ConvertedImages { get; private set; }
        public float CameraIntervalMs { get; private set; }
        public int AcquiredImages { get; private set; }
        public long[] IntervalHistogram { get; }=new long[7];
        private static int sourceIds;
        public int SourceId { get; }=Interlocked.Increment(ref sourceIds);
        public bool IsPrepared => Texture != null;
        public bool IsPlaying => IsPrepared;
        // Pixels are top-down; Unity textures use a bottom-up texture coordinate system.
        public bool IsVerticallyFlipped => true;
        public bool IsFrontFacing { get; private set; }
        public RotationAngle Rotation => RotationAngle.Rotation0;
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject camera;
#endif
        public AndroidCpuCamera(string device,bool front,int fps)
        {
            FrameRate=fps;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity=player.GetStatic<AndroidJavaObject>("currentActivity");
                camera=new AndroidJavaObject("com.fitfight.camera.FitFightCamera",activity);
                camera.Call("start",device,front,fps);
            }
            catch(Exception e) { Error=e.Message; }
#else
            Error="Native camera is Android-only";
#endif
        }
        public IPoseCameraSource CreateInferenceSource(int maxDimension)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(camera==null) throw new ObjectDisposedException(nameof(AndroidCpuCamera));
            double before=Time.realtimeSinceStartupAsDouble;
            long androidNow=camera.Call<long>("clockNanos");
            double offset=(before+Time.realtimeSinceStartupAsDouble)*.5-androidNow/1e9;
            int consumer=camera.Call<int>("beginConsumer");
            return new Source(camera.Call<AndroidJavaObject>("getSource"),offset,maxDimension,SourceId,consumer);
#else
            throw new NotSupportedException("Android camera frames require a device.");
#endif
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        private sealed class Source : IPoseCameraSource
        {
            private readonly AndroidJavaObject bridge;
            private AndroidJavaObject buffer;
            private readonly double offset;
            private readonly int maxDimension,sourceId,consumer;
            private readonly PoseCameraFrame frame=new PoseCameraFrame();
            public Source(AndroidJavaObject bridge,double offset,int maxDimension,int sourceId,int consumer)
            { this.bridge=bridge;this.offset=offset;this.maxDimension=maxDimension;this.sourceId=sourceId;this.consumer=consumer; }
            public unsafe bool TryGetFrame(out PoseCameraFrame value)
            {
                value=null;buffer?.Dispose();buffer=null;
                using var captured=bridge.Call<AndroidJavaObject>("waitForInferenceFrame",25,maxDimension,consumer);
                if(captured==null) return false;
                buffer=captured.Call<AndroidJavaObject>("getPixels");
                long[] m=captured.Call<long[]>("getInfo");
                frame.Pixels=(IntPtr)AndroidJNI.GetDirectBufferAddress(buffer.GetRawObject());
                if(frame.Pixels==IntPtr.Zero) throw new InvalidOperationException("Camera buffer is not direct.");
                frame.Width=(int)m[0];frame.Height=(int)m[1];frame.SensorRotation=(int)m[2];frame.Front=m[3]!=0;
                frame.AvailableAt=offset+m[4]/1e9;frame.ConversionStartedAt=offset+m[5]/1e9;frame.ConversionCompletedAt=offset+m[6]/1e9;
                frame.Sequence=(int)m[7];frame.SourceId=sourceId;frame.SensorTimestamp=offset+m[8]/1e9;
                frame.SensorClockComparable=m[9]!=0 && m[8]>0;frame.ExposureMs=m[10]/1e6;frame.SensorFrameMs=m[11]/1e6;
                frame.SensorIso=(int)m[12];frame.AeState=(int)m[13];frame.AppliedFpsMin=(int)m[14];frame.AppliedFpsMax=(int)m[15];
                value=frame;return true;
            }
            public void Dispose() { buffer?.Dispose();buffer=null;bridge.Dispose(); }
        }
#endif
        public bool Poll()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(camera==null) return false;
            try
            {
            Error=camera.Call<string>("getError");
            string status=camera.Call<string>("getStatus");
            if(status!=Status || !string.IsNullOrEmpty(Error)) LifecycleTrace=camera.Call<string>("getTrace");
            Status=status;
            long[] stats=camera.Call<long[]>("getCameraStats");
            ActualFrameRate=stats[2]/1000f;ReplacedImages=(int)stats[3];ConvertedImages=(int)stats[4];
            AcquiredImages=(int)stats[0];CameraIntervalMs=stats[5]/1e6f;
            Array.Copy(stats,6,IntervalHistogram,0,7);
            double now=Time.realtimeSinceStartupAsDouble;
            if(stats[1]>0) AvailableAt=now-(camera.Call<long>("clockNanos")-stats[1])/1e9;
            if(now-AvailableAt>.75) ActualFrameRate=0;
            if(now<nextPreview) return false;
            nextPreview=now+1.0/15;
            int[] frame=camera.Call<int[]>("takeFrame");
            if(frame==null || frame.Length<8) return false;
            int w=frame[0],h=frame[1];if(w<=0 || h<=0 || frame.Length!=8+w*h) throw new InvalidOperationException("Invalid Camera2 frame");
            if(Texture==null || Texture.width!=w || Texture.height!=h)
            {
                if(Texture!=null) UnityEngine.Object.Destroy(Texture);
                Texture=new Texture2D(w,h,TextureFormat.RGBA32,false,true);
                Pixels=new uint[w*h];
            }
            Buffer.BlockCopy(frame,8*sizeof(int),Pixels,0,Pixels.Length*sizeof(uint));
            // Preview age is independent of the physical camera availability used by the stall watchdog.
            ConversionMs=frame[5]/1000f;SensorRotation=frame[2];IsFrontFacing=frame[3]!=0;Sequence=frame[6];
            FrameRate=frame[7]>0?frame[7]:FrameRate;
            Texture.SetPixelData(Pixels,0);Texture.Apply(false,false);
            return true;
            }
            catch(Exception e) { Error=e.Message;return false; }
#else
            return false;
#endif
        }
        public void Dispose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { camera?.Call("close");LifecycleTrace=camera?.Call<string>("getTrace"); }
            catch(Exception e) { Debug.LogWarning("Camera2 close: "+e.Message); }
            finally { camera?.Dispose();camera=null; }
#endif
            if(Texture!=null) UnityEngine.Object.Destroy(Texture);Texture=null;Pixels=null;
        }
    }
}
