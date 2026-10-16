using System;
using Tracking4All;
using UnityEngine;

namespace FitFight.BodyTracking
{
    public enum BodyTrackingSystem { Tracking4All = 0, RTMPose = 1, RTMPoseNCNN = 2, MediaPipeLite = 3, MediaPipeFull = 4 }

    public static class BodyTrackingSelection
    {
        private const string Key = "FitFight.BodyTracking.System";
        public static event Action Changed;
        public static readonly string[] Labels = { "Tracking4All baseline", "RTMPose · Unity GPU", "RTMPose · native ncnn", "MediaPipe Lite · CPU", "MediaPipe Full · CPU" };
        public static BodyTrackingSystem Selected
        {
            get { int i=PlayerPrefs.GetInt(Key,0); return i>=0 && i<Labels.Length ? (BodyTrackingSystem)i : BodyTrackingSystem.Tracking4All; }
        }
        public static string Label(BodyTrackingSystem engine) => Labels[(int)engine];
        public static void Select(BodyTrackingSystem value)
        {
            if ((int)value < 0 || (int)value >= Labels.Length) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == Selected) return;
            PlayerPrefs.SetInt(Key, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEvents() { Changed = null; }
    }

    public interface IBodyPoseProvider
    {
        ILandmarkProvider<PoseLandmarks> World { get; }
        INormalizedLandmarkProvider<PoseLandmarks> Normalized { get; }
    }

    /// <summary>The unchanged baseline sources, before scene modifiers and avatar retargeting.</summary>
    public sealed class Tracking4AllPoseProvider : IBodyPoseProvider
    {
        public ILandmarkProvider<PoseLandmarks> World { get; }
        public INormalizedLandmarkProvider<PoseLandmarks> Normalized { get; }
        public Tracking4AllPoseProvider(ILandmarkProvider<PoseLandmarks> world,
            INormalizedLandmarkProvider<PoseLandmarks> normalized)
        { World = world; Normalized = normalized; }
    }
}
