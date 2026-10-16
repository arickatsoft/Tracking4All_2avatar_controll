using System;

internal static class FitFightFramingMath
{
    // Solve distance AND the lateral/vertical camera offset. Fixing the look
    // target at a world AABB center needlessly shrinks asymmetric fighter poses.
    public struct FitWindow
    {
        private float left, right, bottom, top;
        private float lowerX, upperX, lowerY, upperY, closest;
        public FitWindow(float tanX, float tanY, float left, float right, float bottom, float top)
        {
            this.left = tanX * left; this.right = tanX * right;
            this.bottom = tanY * bottom; this.top = tanY * top;
            lowerX = lowerY = float.NegativeInfinity;
            upperX = upperY = float.PositiveInfinity;
            closest = 0;
        }
        public void Include(float x, float y, float z)
        {
            lowerX = Math.Max(lowerX, x - right * z);
            upperX = Math.Min(upperX, x + left * z);
            lowerY = Math.Max(lowerY, y - top * z);
            upperY = Math.Min(upperY, y + bottom * z);
            closest = Math.Max(closest, .05f - z);
        }
        public float Distance => Math.Max(closest, Math.Max((lowerX - upperX) / (left + right), (lowerY - upperY) / (bottom + top)));
        public float OffsetX(float distance) => (lowerX + upperX + (left - right) * distance) * .5f;
        public float OffsetY(float distance) => (lowerY + upperY + (bottom - top) * distance) * .5f;
    }

    // Coordinates are relative to the look target in camera axes. Camera sits at z = -distance.
    public static float RequiredDistance(float x, float y, float z, float tanX, float tanY,
        float left, float right, float bottom, float top)
    {
        return Math.Max(Math.Abs(x) / (tanX * (x < 0 ? left : right)) - z,
            Math.Abs(y) / (tanY * (y < 0 ? bottom : top)) - z);
    }
}
