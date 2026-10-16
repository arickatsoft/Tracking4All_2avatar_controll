using System;
using System.Diagnostics;

namespace FitFight.BodyTracking
{
    // No Unity objects or render-loop calls on the inference worker.
    public sealed class PoseCameraFrame
    {
        public IntPtr Pixels;
        public int Width, Height, Sequence, SourceId, SensorRotation;
        public bool Front;
        public double AvailableAt, ConversionStartedAt, ConversionCompletedAt;
        public double SensorTimestamp, ExposureMs, SensorFrameMs;
        public int SensorIso, AeState, AppliedFpsMin, AppliedFpsMax;
        public bool SensorClockComparable;
    }
    public interface IPoseCameraSource : IDisposable
    {
        // Bounded wait (at most 25 ms). Pixel lifetime ends at the next call or Dispose.
        bool TryGetFrame(out PoseCameraFrame frame);
    }
    public sealed class PoseWorkerClock
    {
        private readonly double offset;
        public PoseWorkerClock(double realtime) { offset=realtime-Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency; }
        public double Now => offset+Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
    }
}
