using UnityEngine;

namespace FitFight.Practice
{
    /// <summary>
    /// Both arms held straight overhead.
    /// App 1.22 used MediaPipe's raw 3-D elevation only (both arms >= 78 degrees). In the 1.24 session (H) real overhead
    /// raises measured only 59-73 degrees in 3-D, so 90% of overhead frames were drawn as forward punches.
    /// App 1.25 adds the camera image: raised arms put both wrists >= 0.88 torso lengths above the shoulders with the arm
    /// almost fully extended in the image (>= 0.93 torso lengths); forward arms from a floor camera foreshorten
    /// (<= 0.90). Labelled frames from sessions D, G and H: 80% of H overhead frames detected (1.22: 10%), 0 of 5597
    /// punch/kick/guard/arms-forward frames. Bilateral, two consecutive frames, hysteresis on exit.
    /// </summary>
    public sealed class OverheadArms
    {
        public const float EnterDegrees=76, ExitDegrees=62, MinStraightness=.82f;
        public const float ImageEnterDegrees=48, ImageMinLength=.93f, ImageMinRise=.88f, ImageExitRise=.6f, ImageExitDegrees=35;
        public bool Active { get; private set; }
        public float BothArmElevation { get; private set; }
        public float BothArmImageRise { get; private set; }
        private int run;
        public void Reset() { Active=false;run=0;BothArmElevation=0;BothArmImageRise=0; }
        public static float Elevation(Vector3 shoulder,Vector3 wrist)
        {
            Vector3 d=wrist-shoulder;
            return Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;
        }
        public static float Straightness(Vector3 shoulder,Vector3 elbow,Vector3 wrist)
            => Vector3.Distance(shoulder,wrist)/Mathf.Max(1e-6f,Vector3.Distance(shoulder,elbow)+Vector3.Distance(elbow,wrist));
        /// <summary>Image arm features in torso lengths (image y up): length shoulder->wrist and wrist rise above shoulder.</summary>
        public struct ImageArms
        {
            public Vector2 ls,lw,rs,rw,lh,rh;
            public bool Valid => (ls-lh).sqrMagnitude>1e-6f;
            public float Torso => Vector2.Distance((ls+rs)*.5f,(lh+rh)*.5f);
            public float MinLength => Mathf.Min(Vector2.Distance(ls,lw),Vector2.Distance(rs,rw))/Mathf.Max(1e-4f,Torso);
            public float MinRise => Mathf.Min(lw.y-ls.y,rw.y-rs.y)/Mathf.Max(1e-4f,Torso);
        }
        /// <param name="valid">All six arm joints confidently observed in this accepted pose.</param>
        public void Observe(bool valid,Vector3 ls,Vector3 le,Vector3 lw,Vector3 rs,Vector3 re,Vector3 rw)
            => Observe(valid,ls,le,lw,rs,re,rw,default);
        public void Observe(bool valid,Vector3 ls,Vector3 le,Vector3 lw,Vector3 rs,Vector3 re,Vector3 rw,ImageArms image)
        {
            if(!valid || !BoxingFeatures.Finite(ls+le+lw+rs+re+rw)) { Active=false;run=0;return; }
            BothArmElevation=Mathf.Min(Elevation(ls,lw),Elevation(rs,rw));
            float straight=Mathf.Min(Straightness(ls,le,lw),Straightness(rs,re,rw));
            bool haveImage=image.Valid && image.Torso>1e-3f;
            BothArmImageRise=haveImage ? image.MinRise : 0;
            if(!Active)
            {
                bool enter=straight>=MinStraightness && (BothArmElevation>=EnterDegrees
                    || haveImage && BothArmElevation>=ImageEnterDegrees && image.MinLength>=ImageMinLength && image.MinRise>=ImageMinRise);
                run=enter ? run+1 : 0;
                if(run>=2) Active=true;
            }
            else if(haveImage ? BothArmElevation<ImageExitDegrees || image.MinRise<ImageExitRise : BothArmElevation<ExitDegrees)
            { Active=false;run=0; }
        }
    }
}
