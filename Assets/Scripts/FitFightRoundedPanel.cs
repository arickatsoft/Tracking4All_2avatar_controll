using UnityEngine;
using UnityEngine.UI;

/// <summary>Rounded HUD card without a texture or additional material.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class FitFightRoundedPanel : MaskableGraphic
{
    [SerializeField, Range(0, .5f)] private float cornerRoundness = .12f;
    public float CornerRoundness { get => cornerRoundness; set { cornerRoundness = Mathf.Clamp(value, 0, .5f); SetVerticesDirty(); } }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        float radius = Mathf.Min(rect.width, rect.height) * cornerRoundness;
        vh.AddVert(rect.center, color, Vector2.zero);
        const int segments = 8;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                corner < 2 ? rect.yMax - radius : rect.yMin + radius);
            for (int step = 0; step <= segments; step++)
            {
                float angle = (corner * 90f + step * 90f / segments) * Mathf.Deg2Rad;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
            }
        }
        int perimeter = 4 * (segments + 1);
        for (int i = 0; i < perimeter; i++) vh.AddTriangle(0, 1 + i, 1 + (i + 1) % perimeter);
    }
}
