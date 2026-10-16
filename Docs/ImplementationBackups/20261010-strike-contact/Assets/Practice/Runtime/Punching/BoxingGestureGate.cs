using UnityEngine;

namespace FitFight.Practice
{
    // Paired raise/lower evidence precedes the final overhead posture. A single
    // camera-facing punch retains the boxing prior regardless of projected height.
    public sealed class BoxingGestureGate
    {
        public bool Blocked { get; private set; }
        private PunchCameraInput.Frame previous;
        private bool havePrevious;
        public void Reset() { Blocked=havePrevious=false; }
        public void Observe(PunchCameraInput.Frame frame)
        {
            if(havePrevious && (frame.sequence<=previous.sequence || frame.time<=previous.time)) return;
            float dt=(float)(frame.time-previous.time);
            bool valid=BoxingFeatures.HandVisible(frame.left) && BoxingFeatures.HandVisible(frame.right);
            if(!valid || !havePrevious || dt>.25f)
            { Blocked=valid && BoxingFeatures.Overhead(frame.left,frame.right);previous=frame;havePrevious=valid;return; }
            Vector2 left=(frame.left.wrist-previous.left.wrist)/dt,right=(frame.right.wrist-previous.right.wrist)/dt;
            bool symmetric=Mathf.Min(left.magnitude,right.magnitude)>.35f*Mathf.Max(left.magnitude,right.magnitude);
            bool vertical=Mathf.Abs(left.y)>Mathf.Abs(left.x)*1.2f && Mathf.Abs(right.y)>Mathf.Abs(right.x)*1.2f;
            bool raising=symmetric && vertical && left.y>.75f && right.y>.75f;
            bool lowering=symmetric && vertical && left.y<-.75f && right.y<-.75f;
            if(raising || lowering || BoxingFeatures.Overhead(frame.left,frame.right)) Blocked=true;
            else if(!symmetric && Mathf.Max(left.magnitude,right.magnitude)>1.1f ||
                BoxingFeatures.Guard(frame.left) && BoxingFeatures.Guard(frame.right) && left.magnitude<.8f && right.magnitude<.8f)
                Blocked=false;
            previous=frame;havePrevious=true;
        }
    }
}
