// FaceData
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    public class FaceData : IDisposableData
    {
        private float[] blendshapes = new float[Helpers.GetLength(typeof(FaceBlendshape))];
        private Matrix4x4 faceOrientationMatrix = Matrix4x4.identity;

        public Matrix4x4 FaceOrientationMatrix => faceOrientationMatrix;
        public int BlendshapeCount => blendshapes.Length;

        public void Set(int blendshape, float v)
        {
            blendshapes[(int)blendshape] = v;
        }
        public void Set(FaceBlendshape blendshape, float v)
        {
            blendshapes[(int)blendshape] = v;
        }
        public void Set(Matrix4x4 orientationMatrix)
        {
            faceOrientationMatrix = orientationMatrix;
        }

        public float GetBlendshape(FaceBlendshape blendshape)
        {
            return blendshapes[(int)blendshape];
        }
        public float GetBlendshape(int blendshape)
        {
            return blendshapes[(int)blendshape];
        }

        public void Dispose()
        {
            for (int i = 0; i < blendshapes.Length; ++i)
            {
                blendshapes[i] = 0;
            }
            faceOrientationMatrix = Matrix4x4.identity;
        }

        public Vector3 GetTranslation()
        {
            return Helpers.GetTranslation(faceOrientationMatrix);
        }
        public Vector3 GetRotationAngles()
        {
            return Helpers.GetRotationAngles(faceOrientationMatrix);
        }
    }
}