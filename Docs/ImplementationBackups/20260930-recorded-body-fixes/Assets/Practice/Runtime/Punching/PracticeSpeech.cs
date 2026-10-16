using System;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace FitFight.Practice
{
    /// <summary>Native speech, independent of tracking and Unity's hit-sound AudioSource.</summary>
    public sealed class PracticeSpeech : IDisposable
    {
        public string Error { get; private set; }
        private bool disposed;
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject bridge;
#elif UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void FitFightSpeechInitialize();
        [DllImport("__Internal")] private static extern void FitFightSpeechSpeak(string text);
        [DllImport("__Internal")] private static extern void FitFightSpeechStop();
        [DllImport("__Internal")] private static extern void FitFightSpeechShutdown();
        [DllImport("__Internal")] private static extern int FitFightSpeechStatus();
#endif
        public PracticeSpeech()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity=player.GetStatic<AndroidJavaObject>("currentActivity");
                bridge=new AndroidJavaObject("com.fitfight.practice.PracticeSpeech",activity);
#elif UNITY_IOS && !UNITY_EDITOR
                FitFightSpeechInitialize();
#endif
            }
            catch(Exception e) { Fail(e.Message); }
        }
        public void Speak(string text)
        {
            if(disposed || Error!=null) return;
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                bridge.Call("speak",text);
#elif UNITY_IOS && !UNITY_EDITOR
                FitFightSpeechSpeak(text);
#else
                Debug.Log("[Practice voice] "+text);
#endif
            }
            catch(Exception e) { Fail(e.Message); }
        }
        public void CheckStatus()
        {
            if(disposed || Error!=null) return;
            try
            {
                int status=1;
#if UNITY_ANDROID && !UNITY_EDITOR
                status=bridge.Call<int>("getStatus");
#elif UNITY_IOS && !UNITY_EDITOR
                status=FitFightSpeechStatus();
#endif
                if(status<0) Fail("Phone speech unavailable. Check that an English text-to-speech voice is installed.");
            }
            catch(Exception e) { Fail(e.Message); }
        }
        public void Stop()
        {
            if(disposed) return;
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                bridge?.Call("stop");
#elif UNITY_IOS && !UNITY_EDITOR
                FitFightSpeechStop();
#endif
            }
            catch(Exception e) { Fail(e.Message); }
        }
        private void Fail(string message)
        { Error=message;Debug.LogWarning("[Practice voice] "+message); }
        public void Dispose()
        {
            if(disposed) return;
            Stop();disposed=true;
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                bridge?.Call("shutdown");bridge?.Dispose();bridge=null;
#elif UNITY_IOS && !UNITY_EDITOR
                FitFightSpeechShutdown();
#endif
            }
            catch(Exception e) { Debug.LogWarning("[Practice voice] "+e.Message); }
        }
    }
}
