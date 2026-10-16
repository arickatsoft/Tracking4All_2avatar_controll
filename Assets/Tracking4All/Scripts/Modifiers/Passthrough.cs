// Passthrough
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    /// <summary>
    /// Modifier that simply sets the current to the target instantly.
    /// <para>Should be universally supported.</para>
    /// </summary>
    public class Passthrough : Modifier,
        ILandmarkModifier, INormalizedLandmarkModifier, ISegmentationFrameModifier
    {
        public void Modify(int dataIndex, ref Landmark current, ref Landmark target, ref bool stayAlive, float deltaTime)
        {
            current = target;
        }
        public void Modify(int dataIndex, ref NormalizedLandmark current, ref NormalizedLandmark target, ref bool stayAlive, float deltaTime)
        {
            current = target;
        }

        public void Modify(int dataIndex, ref Segmentation current, ref Segmentation target, ref bool stayAlive, float deltaTime)
        {
            if (!current.Initialized || !target.Initialized) return;

            // Copy the target into current.
            current.Write(target);
        }
    }
}