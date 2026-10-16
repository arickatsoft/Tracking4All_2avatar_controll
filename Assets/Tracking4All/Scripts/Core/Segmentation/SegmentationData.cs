// SegmentationBackend
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    /// <summary>
    /// Defines all segmentation data required from a backend for use in Tracking4All.
    /// <para>Intermediary step to being made a completely backend independent SegmentationFrame.</para>
    /// </summary>
    public class SegmentationData : IDisposableData
    {
        // It is expensive to copy on the cpu.
        // So, don't copy and store references instead.
        // It is an external concern how the data in these references is managed.

        /// <summary>
        /// The flattened segmentation data in row-major order, 0 -> no mask, 1 -> mask.
        /// <para>Might be very large ex: 1920x1080 length.</para>
        /// </summary>
        public float[] maskDataReference;

        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// Write to a segmentation frame on the CPU.
        /// <para>MUST be called on unity main thread.</para>
        /// </summary>
        /// <param name="frame"></param>
        public void UpdateFrame(Segmentation frame)
        {
            frame.Write(maskDataReference);
        }

        public void Create(int width, int height)
        {
            maskDataReference = new float[width * height];
            Width = width;
            Height = height;
        }
        public void Dispose()
        {
            maskDataReference = null;
        }
    }
}