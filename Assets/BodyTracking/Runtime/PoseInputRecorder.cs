using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;

namespace FitFight.BodyTracking
{
    /// <summary>Opt-in exact RGBA inputs: 10 seconds, four reusable slots, no blocking disk IO on inference.</summary>
    public sealed class PoseInputRecorder : IDisposable
    {
        private sealed class Slot { public readonly byte[] Pixels=new byte[512*512*4]; public readonly PoseCameraFrame Frame=new PoseCameraFrame();public long Inference; }
        private readonly ConcurrentQueue<Slot> free=new ConcurrentQueue<Slot>();
        private readonly BlockingCollection<Slot> queue=new BlockingCollection<Slot>(4);
        private readonly object gate=new object();
        private readonly Thread writer;
        private readonly double until;
        private bool closed;
        private int saved,dropped;
        private volatile string error;
        public string Path { get; }
        public int Saved => Volatile.Read(ref saved);
        public int Dropped => Volatile.Read(ref dropped);
        public string Error => error;
        private volatile bool finished;
        public bool Finished => finished;
        public PoseInputRecorder(string path,double start)
        {
            Path=path;until=start+10;
            for(int i=0;i<4;i++) free.Enqueue(new Slot());
            writer=new Thread(Write) { IsBackground=true,Name="FitFight input archive" };writer.Start();
        }
        public void Capture(PoseCameraFrame frame,long inference,double now)
        {
            lock(gate)
            {
                if(closed || error!=null) return;
                if(now>=until) { Close();return; }
                if(!free.TryDequeue(out var slot)) { dropped++;return; }
                int bytes=checked(frame.Width*frame.Height*4);
                if(bytes>slot.Pixels.Length || frame.Pixels==IntPtr.Zero) { free.Enqueue(slot);dropped++;return; }
                Marshal.Copy(frame.Pixels,slot.Pixels,0,bytes);
                var f=slot.Frame;f.Width=frame.Width;f.Height=frame.Height;f.Sequence=frame.Sequence;f.SourceId=frame.SourceId;
                f.AvailableAt=frame.AvailableAt;f.SensorRotation=frame.SensorRotation;f.Front=frame.Front;
                f.ConversionStartedAt=frame.ConversionStartedAt;f.ConversionCompletedAt=frame.ConversionCompletedAt;
                slot.Inference=inference;
                if(!queue.TryAdd(slot)) { free.Enqueue(slot);dropped++; }
            }
        }
        private void Write()
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                using var file=File.Create(Path);
                using var compressed=new GZipStream(file,CompressionLevel.Fastest);
                using var output=new BinaryWriter(compressed);
                output.Write(0x31524646); // FFR1, little-endian; continuous gzip stream
                foreach(var slot in queue.GetConsumingEnumerable())
                {
                    var f=slot.Frame;int length=f.Width*f.Height*4;
                    output.Write(f.Width);output.Write(f.Height);output.Write(f.Sequence);output.Write(f.SourceId);output.Write(slot.Inference);
                    output.Write(f.AvailableAt);output.Write(f.ConversionStartedAt);output.Write(f.ConversionCompletedAt);
                    output.Write(f.SensorRotation);output.Write(f.Front);output.Write(length);output.Write(slot.Pixels,0,length);
                    Interlocked.Increment(ref saved);free.Enqueue(slot);
                }
            }
            catch(Exception e) { error=e.Message; }
            finally {
                try { File.WriteAllText(Path+".stats.json","{\"savedFrames\":"+Saved+",\"droppedFrames\":"+Dropped+",\"writerFailed\":"+(error!=null?"true":"false")+"}"); }
                catch(Exception e) { error=error??e.Message; }
                finished=true;
            }
        }
        private void Close() { if(closed) return;closed=true;queue.CompleteAdding(); }
        public void Dispose() { lock(gate) Close(); }
    }
}
