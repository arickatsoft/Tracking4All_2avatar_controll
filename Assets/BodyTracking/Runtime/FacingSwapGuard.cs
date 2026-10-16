using UnityEngine;

namespace FitFight.BodyTracking
{
    /// <summary>
    /// App 1.23: MediaPipe occasionally labels a camera-facing player as facing away for a few frames (head turns,
    /// deep forward bends): every left/right landmark swaps sides, in the image and in 3-D. The player of this game
    /// always faces the camera, so when both the shoulder pair and the hip pair appear in the opposite image order to
    /// the established one, the labels are swapped back before anything else uses them.
    /// </summary>
    public sealed class FacingSwapGuard
    {
        public const float MinShoulderSpan=.03f, MinHipSpan=.02f, Learn=.03f, Established=.6f;
        public float Orientation { get; private set; }   // EMA of the image left-minus-right sign, -1..1
        public int Swaps { get; private set; }
        public void Reset() { Orientation=0; Swaps=0; }
        /// <summary>Returns true when the frame's left/right labels should be swapped back.</summary>
        public bool Observe(Vector2[] image,float[] scores,float confidence=.5f)
        {
            if(scores[5]<confidence || scores[6]<confidence || scores[11]<confidence || scores[12]<confidence) return false;
            float shoulders=image[5].x-image[6].x, hips=image[11].x-image[12].x;
            if(Mathf.Abs(shoulders)<MinShoulderSpan || Mathf.Abs(hips)<MinHipSpan || Mathf.Sign(shoulders)!=Mathf.Sign(hips)) return false;
            float sign=Mathf.Sign(shoulders);
            if(Mathf.Abs(Orientation)>=Established && sign!=Mathf.Sign(Orientation)) { Swaps++; return true; }
            Orientation=Mathf.Clamp(Orientation+(sign-Orientation)*Learn,-1,1);
            return false;
        }
    }
}
