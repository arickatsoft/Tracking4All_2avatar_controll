using System;
using Tracking4All;
using UnityEngine;

namespace FitFight.BodyTracking
{
    public enum BodyTrackingSystem { Tracking4All, RTMPose }

    public static class BodyTrackingSelection
    {
        private const string Key = "FitFight.BodyTracking.System";
        public static event Action Changed;
        public static BodyTrackingSystem Selected => PlayerPrefs.GetInt(Key, 0) == 1
            ? BodyTrackingSystem.RTMPose : BodyTrackingSystem.Tracking4All;
        public static void Select(BodyTrackingSystem value)
        {
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
