// Segmentation
// (C) 2024-2025 G8gaming Ltd.
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Index for segmentation data.
    /// <para>NOTE: this is not a group.</para>
    /// </summary>
    public enum SegmentationType { DEFAULT }

    /// <summary>
    /// Backend independent segmentation data provider which deals with the data on the GPU for performance.
    /// <para>Can only be used on the unity main thread.</para>
    /// </summary>
    public class Segmentation : IDisposableData
    {
        /// <summary>
        /// Wrap graphics buffer to efficiently provide segmentation data.
        /// </summary>
        protected GraphicsBuffer segmentationBuffer;

        public bool Initialized { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public void Reinitialize(int width, int height)
        {
            // Only recreate buffer if either it dne or the width/height of the existing one wont do.
            if (segmentationBuffer == null || Width != height || Height != height)
            {
                Width = width;
                Height = height;

                Logger.LogInfo("Create segmentation buffer: " + Width + "x" + Height, this);
                Dispose();

                segmentationBuffer =
                    new GraphicsBuffer(GraphicsBuffer.Target.Structured, Width * Height, Marshal.SizeOf(typeof(float)));
            }

            Initialized = true;
        }

        /// <summary>
        /// Perform a fast copy of the segmentation frame on the GPU.
        /// </summary>
        /// <param name="copyFrom"></param>
        public void Write(Segmentation copyFrom)
        {
            if (!Initialized)
            {
                Logger.LogError("The segmentation frame is not initialized.", this);
                return;
            }
            if (!copyFrom.Initialized)
            {
                Logger.LogError("Cannot copy from a disposed segementation frame.", this);
                return;
            }

            Graphics.CopyBuffer(copyFrom.segmentationBuffer, segmentationBuffer);
        }
        /// <summary>
        /// Copy the segmentation frame to the graphics buffer.
        /// </summary>
        /// <param name="data"></param>
        public void Write(float[] data)
        {
            if (!Initialized)
            {
                Logger.LogError("The segmentation frame is not initialized.", this);
                return;
            }
            if (data == null)
            {
                //Logger.LogError("Data input dne.", this);
                return;
            }
            if (data == null || data.Length != Width * Height)
            {
                Logger.LogError("Buffer size mismatch locally:" + (Width) + "x" + (Height) + "=" + (Width * Height) + " vs input:" + data.Length, this);
                return;
            }

            // Unity handles efficiently updating the graphics buffer depending on the platform.
            segmentationBuffer.SetData(data);
        }

        /// <summary>
        /// Tell compute shader to use this graphics buffer.
        /// <para>Note: You probably only need to call this once.</para>
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="kernalIndex"></param>
        /// <param name="nameId"></param>
        public void SetComputeBuffer(ComputeShader shader, int kernalIndex, int nameId)
        {
            if (!Initialized)
            {
                Logger.LogError("The segmentation frame is not initialized. The buffer was probably accessed to early.", this);
                return;
            }

            shader.SetBuffer(kernalIndex, nameId, segmentationBuffer);
        }
        /// <summary>
        /// Tell shader for material to use this graphics buffer.
        /// <para>Note: You probably only need to call this once.</para>
        /// </summary>
        /// <param name="material"></param>
        /// <param name="nameId"></param>
        public void SetMaterialBuffer(Material material, int nameId)
        {
            if (!Initialized)
            {
                Logger.LogError("The segmentation is not initialized. The buffer was probably accessed to early. ", this);
                return;
            }

            material.SetBuffer(nameId, segmentationBuffer);
        }

        /// <summary>
        /// Dispose the segmentation.
        /// <para>You need to be sure that you are not using Segmentation resources anymore.</para>
        /// </summary>
        public void Dispose()
        {
            // Only does anything if actually something to dispose.
            if (segmentationBuffer != null)
            {
                segmentationBuffer.Dispose();
                segmentationBuffer = null;
                Logger.LogInfo("Disposed segmentation");
            }

            Initialized = false;
        }

        ~Segmentation()
        {
            // Properly cleanup gpu resources.
            Dispose();
        }
    }

}