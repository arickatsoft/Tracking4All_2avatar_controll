using Unity.InferenceEngine;
using UnityEngine.Rendering;

namespace FitFight.BodyTracking
{
    /// <summary>Copy exact tensor lengths in the GPU completion callback, before Unity expires the request data.</summary>
    public sealed class Body26Readback
    {
        public readonly float[] X = new float[26 * 384];
        public readonly float[] Y = new float[26 * 512];
        private int generation, completed;
        public bool Done => completed == 2;
        public bool HasError { get; private set; }
        public void Reset() { generation++; completed = 0; HasError = false; }
        public void Begin(Tensor<float> x, Tensor<float> y)
        {
            Reset();
            int current = generation;
            Read(x, X, current); Read(y, Y, current);
        }
        private void Read(Tensor<float> tensor, float[] destination, int current)
        {
            // The inference engine pools buffers larger than shape.length. Request only the logical tensor.
            AsyncGPUReadback.Request(ComputeTensorData.Pin(tensor).buffer, destination.Length * sizeof(float), 0, request =>
            {
                if (generation != current) return;
                if (request.hasError) HasError = true;
                else request.GetData<float>().CopyTo(destination);
                completed++;
            });
        }
    }
}
