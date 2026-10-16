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
        public int PipelineDepth=-1;
    }
    public interface IPoseCameraSource : IDisposable
    {
        // Bounded wait (at most 25 ms). Pixel lifetime ends at the next call or Dispose.
        bool TryGetFrame(out PoseCameraFrame frame);
    }
    public sealed class PoseWorkerClock
    {
        // Unity's realtime clock stops while an Android app is paused; Stopwatch does not. The main
        // thread resyncs the offset every frame (1.20 sessions showed a constant 74 s error after a pause).
        private long offsetBits;
        public PoseWorkerClock(double realtime) { Resync(realtime); }
        public void Resync(double realtime)
        { System.Threading.Interlocked.Exchange(ref offsetBits,BitConverter.DoubleToInt64Bits(realtime-Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency)); }
        public double Now => BitConverter.Int64BitsToDouble(System.Threading.Interlocked.Read(ref offsetBits))+Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
    }
}
