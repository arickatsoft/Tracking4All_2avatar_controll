using UnityEngine;
public sealed class FitFightPoseReference : ScriptableObject
{
    public AnimationClip guard;
    public AnimationClip leftPunch, rightPunch;
    [Range(.05f,.9f)] public float leftContactTime = .22f;
    [Range(.05f,.9f)] public float rightContactTime = .60f;
}
