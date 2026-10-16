using System;
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
        public int SensorRotation { get; private set; }
        public string Name => "Android Camera2 CPU";
        public int Width => Texture != null ? Texture.width : 0;
        public int Height => Texture != null ? Texture.height : 0;
        public int FrameRate { get; private set; }
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
        public bool Poll()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(camera==null) return false;
            try
            {
            Error=camera.Call<string>("getError");
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
            AvailableAt=Time.realtimeSinceStartupAsDouble-frame[4]/1000000.0;
            ConversionMs=frame[5]/1000f;SensorRotation=frame[2];IsFrontFacing=frame[3]!=0;Sequence=frame[6];
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
            try { camera?.Call("close"); }
            catch(Exception e) { Debug.LogWarning("Camera2 close: "+e.Message); }
            finally { camera?.Dispose();camera=null; }
#endif
            if(Texture!=null) UnityEngine.Object.Destroy(Texture);Texture=null;Pixels=null;
        }
    }
}
