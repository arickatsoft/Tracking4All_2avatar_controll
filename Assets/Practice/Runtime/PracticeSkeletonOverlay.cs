using FitFight.BodyTracking;
using Tracking4All;
using UnityEngine;
using UnityEngine.UI;

namespace FitFight.Practice
{
    /// <summary>Confidence-gated skeleton drawn in the same upright image coordinates as the camera preview.</summary>
    public sealed class PracticeSkeletonOverlay : MaskableGraphic
    {
        private readonly Vector2[] points = new Vector2[33];
        private readonly float[] confidence = new float[33];
        private bool calibrating;
        private static readonly int[] Edges = { 11,12, 11,13, 13,15, 12,14, 14,16, 11,23, 12,24,
            23,24, 23,25, 25,27, 24,26, 26,28, 27,31, 28,32, 0,11, 0,12 };
        public void Sample(RTMPosePoseProvider pose, MPUPoseTrackingSolution graph, bool front)
        {
            System.Array.Clear(confidence, 0, confidence.Length);
            calibrating = pose != null && !pose.HasRecentPose;
            if (pose != null && pose.HasRecentLandmarks)
            {
                for (int i = 0; i < 33; i++)
                {
                    int j = RTMPoseMath.ToMediaPipe[i];
                    points[i] = new Vector2(pose.Points[j].x, 1 - pose.Points[j].y);
                    confidence[i] = pose.Scores[j];
                }
            }
            else if (pose == null && graph != null)
            {
                var data = graph as INormalizedLandmarkProvider<MPPoseLandmarks>;
                if (data?.Lifecycle != null && data.Lifecycle.IsAlive(0))
                    for (int i = 0; i < 33; i++)
                    {
                        var joint = data.Get(0, i);
                        points[i] = new Vector2(front ? 1 - joint.Position.x : joint.Position.x, joint.Position.y);
                        confidence[i] = joint.Visibility;
                    }
            }
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var rect = rectTransform.rect;
            Vector2 P(int i) => rect.min + Vector2.Scale(points[i], rect.size);
            bool Valid(int i) => confidence[i] >= .3f && points[i].x >= 0 && points[i].x <= 1 && points[i].y >= 0 && points[i].y <= 1;
            Color32 tint = calibrating ? new Color(1, .7f, .12f, .95f) : new Color(.1f, 1, .28f, .90f);
            for (int i = 0; i < Edges.Length; i += 2)
            {
                int a = Edges[i], b = Edges[i + 1]; if (!Valid(a) || !Valid(b)) continue;
                Vector2 start = P(a), end = P(b), delta = (end - start).normalized;
                Vector2 width = new Vector2(-delta.y, delta.x) * .75f;
                int index = vh.currentVertCount;
                vh.AddVert(start - width, tint, Vector2.zero); vh.AddVert(start + width, tint, Vector2.zero);
                vh.AddVert(end + width, tint, Vector2.zero); vh.AddVert(end - width, tint, Vector2.zero);
                vh.AddTriangle(index, index + 1, index + 2); vh.AddTriangle(index, index + 2, index + 3);
            }
            foreach (int i in new[] { 0,11,12,13,14,15,16,23,24,25,26,27,28,31,32 })
            {
                if (!Valid(i)) continue; Vector2 center = P(i); int index = vh.currentVertCount;
                vh.AddVert(center, tint, Vector2.zero);
                for (int j = 0; j <= 8; j++)
                {
                    float a = j * Mathf.PI * .25f;
                    vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.3f, tint, Vector2.zero);
                    if (j > 0) vh.AddTriangle(index, index + j, index + j + 1);
                }
            }
        }
    }
}
