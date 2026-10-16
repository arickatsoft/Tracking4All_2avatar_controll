// NormalizedRectangle
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    public struct NormalizedRectangle : IDisposableData
    {
        private Vector4 data;

        public float X => data.x;
        public float Y => data.y;
        public float Width => data.z;
        public float Height => data.w;

        public NormalizedRectangle(float x, float y, float width, float height)
        {
            data = new(x, y, width, height);
        }

        public void Dispose()
        {
            data = new Vector4(0, 0, 1, 1);
        }
    }
}