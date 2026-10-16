using UnityEngine;

namespace FitFight.BodyTracking
{
    /// <summary>Main-thread thermal status/headroom, polled at most once per second (1.21 diagnostics).
    /// 1.23: also requests the display refresh natively and reports the refresh Android actually runs.</summary>
    public static class AndroidThermal
    {
        public static int Status { get; private set; }=-1;
        public static float Headroom { get; private set; }=float.NaN;
        public static float AndroidRefreshHz { get; private set; }=float.NaN;
        public static string DisplayStatus { get; private set; }="not requested";
        private static double next;
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject performance,activity;
        private static void Ensure()
        {
            if(performance!=null) return;
            using var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            activity=player.GetStatic<AndroidJavaObject>("currentActivity");
            performance=new AndroidJavaObject("com.fitfight.camera.FitFightPerformance",activity);
        }
#endif
        public static void RequestRefresh(float hz)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { Ensure();performance.Call("requestRefresh",activity,hz); }
            catch(System.Exception e) { DisplayStatus="error: "+e.Message; }
#endif
        }
        public static void Poll(double now)
        {
            if(now<next) return;
            next=now+1;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                Ensure();
                Status=performance.Call<int>("thermalStatus");
                Headroom=performance.Call<float>("thermalHeadroom",10);
                AndroidRefreshHz=performance.Call<float>("currentRefresh",activity);
                DisplayStatus=performance.Call<string>("getDisplayStatus");
            }
            catch(System.Exception) { Status=-1;Headroom=float.NaN; }
#endif
        }
    }
}
