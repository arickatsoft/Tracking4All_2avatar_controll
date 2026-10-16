using UnityEngine;

namespace FitFight.Practice
{
    /// <summary>Causal render-cadence smoothing. It never supplies detector evidence or impact permission.</summary>
    public static class AvatarMotionSmoothing
    {
        public static Quaternion Rotation(Quaternion current,Quaternion target,ref Vector3 velocity,float idleTime,float movingTime,float dt)
        {
            if(!FitFight.BodyTracking.OneEuroFilterQuaternion.TryNormalize(target,out target)) return current;
            Quaternion error=target*Quaternion.Inverse(current);
            error.ToAngleAxis(out float angle,out Vector3 axis);
            if(angle>180) angle-=360;
            if(!BoxingFeatures.Finite(axis) || Mathf.Abs(angle)<.0001f) { velocity=Vector3.zero;return target; }
            float time=Mathf.Lerp(idleTime,movingTime,Mathf.InverseLerp(2,18,Mathf.Abs(angle)));
            Vector3 step=Vector3.SmoothDamp(Vector3.zero,axis*angle,ref velocity,time,720,Mathf.Clamp(dt,0,.05f));
            float degrees=step.magnitude;
            return degrees<.000001f?current:Quaternion.AngleAxis(degrees,step/degrees)*current;
        }
        public static Vector3 Position(Vector3 current,Vector3 target,ref Vector3 velocity,float time,float speed,float dt)
            => Vector3.SmoothDamp(current,target,ref velocity,time,speed,Mathf.Clamp(dt,0,.05f));
    }
}
