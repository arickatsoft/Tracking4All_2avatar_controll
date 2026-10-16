using UnityEngine;
using Tracking4All;
using System.Linq;

public class TrackingAvatarSwitcher : MonoBehaviour
{
    [Header("Avatar References")]
    [SerializeField] private GameObject remyAvatar;
    [SerializeField] private GameObject adamAvatar;
    [SerializeField] private Transform adamBigTransform;
    
    [Header("Tracking Configuration")]
    [SerializeField] private GameObject poseSolutionObject;
    [SerializeField] private bool mirrorTracking = false;
    
    private Tracking4All.Avatar currentTrackingTarget;
    private Transform[] remyBones;
    private Transform[] adamBones;
    private bool isTrackingAdam = false;
    
    // Bone mapping between avatars
    private readonly string[] boneNames = new string[]
    {
        "mixamorig:Hips",
        "mixamorig:Spine", 
        "mixamorig:Spine1",
        "mixamorig:Spine2",
        "mixamorig:Neck",
        "mixamorig:Head",
        "mixamorig:LeftShoulder",
        "mixamorig:LeftArm",
        "mixamorig:LeftForeArm",
        "mixamorig:LeftHand",
        "mixamorig:RightShoulder",
        "mixamorig:RightArm",
        "mixamorig:RightForeArm",
        "mixamorig:RightHand",
        "mixamorig:LeftUpLeg",
        "mixamorig:LeftLeg",
        "mixamorig:LeftFoot",
        "mixamorig:RightUpLeg",
        "mixamorig:RightLeg",
        "mixamorig:RightFoot"
    };
    
    void Start()
    {
        InitializeAvatars();
        SetupPoseSolution();
    }
    
    void InitializeAvatars()
    {
        if (remyAvatar == null)
            remyAvatar = GameObject.Find("Remy");
            
        if (adamAvatar == null)
            adamAvatar = GameObject.Find("Adam_Big");
            
        if (adamBigTransform == null && adamAvatar != null)
            adamBigTransform = adamAvatar.transform;
            
        // Cache bone references
        if (remyAvatar != null)
            remyBones = GetBonesFromAvatar(remyAvatar);
            
        if (adamAvatar != null)
            adamBones = GetBonesFromAvatar(adamAvatar);
    }
    
    Transform[] GetBonesFromAvatar(GameObject avatar)
    {
        Transform[] bones = new Transform[boneNames.Length];
        
        for (int i = 0; i < boneNames.Length; i++)
        {
            Transform bone = avatar.transform.Find(boneNames[i]);
            if (bone == null)
            {
                // Try recursive search
                bone = FindChildRecursive(avatar.transform, boneNames[i]);
            }
            bones[i] = bone;
        }
        
        return bones;
    }
    
    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
                
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
    
    void SetupPoseSolution()
    {
        if (poseSolutionObject == null)
        {
            poseSolutionObject = GameObject.Find("Pose Solution");
            if (poseSolutionObject == null)
                poseSolutionObject = GameObject.Find("Pose Tracking Solution");
        }
    }
    
    public void SwitchToAdam()
    {
        isTrackingAdam = true;
        
        // Disable Remy's Avatar component
        Tracking4All.Avatar remyAvatarComponent = remyAvatar?.GetComponent<Tracking4All.Avatar>();
        if (remyAvatarComponent != null)
            remyAvatarComponent.enabled = false;
        
        // Enable or add Avatar component to Adam
        Tracking4All.Avatar adamAvatarComponent = adamAvatar?.GetComponent<Tracking4All.Avatar>();
        if (adamAvatarComponent == null && adamAvatar != null)
        {
            adamAvatarComponent = adamAvatar.AddComponent<Tracking4All.Avatar>();
            
            // Also ensure AvatarSkeleton exists
            AvatarSkeleton adamSkeleton = adamAvatar.GetComponent<AvatarSkeleton>();
            if (adamSkeleton == null)
            {
                adamSkeleton = adamAvatar.AddComponent<AvatarSkeleton>();
                SetupAdamSkeleton(adamSkeleton);
            }
        }
        
        if (adamAvatarComponent != null)
        {
            adamAvatarComponent.enabled = true;
            currentTrackingTarget = adamAvatarComponent;
        }
        
        // Update pose solution target
        UpdatePoseSolutionTarget(adamAvatar);
        
        Debug.Log("Switched tracking to Adam");
    }
    
    public void SwitchToRemy()
    {
        isTrackingAdam = false;
        
        // Enable Remy's Avatar component
        Tracking4All.Avatar remyAvatarComponent = remyAvatar?.GetComponent<Tracking4All.Avatar>();
        if (remyAvatarComponent != null)
        {
            remyAvatarComponent.enabled = true;
            currentTrackingTarget = remyAvatarComponent;
        }
        
        // Disable Adam's Avatar component if it exists
        Tracking4All.Avatar adamAvatarComponent = adamAvatar?.GetComponent<Tracking4All.Avatar>();
        if (adamAvatarComponent != null)
            adamAvatarComponent.enabled = false;
        
        // Update pose solution target
        UpdatePoseSolutionTarget(remyAvatar);
        
        Debug.Log("Switched tracking to Remy");
    }
    
    void SetupAdamSkeleton(AvatarSkeleton skeleton)
    {
        // Setup the skeleton with Adam's bone structure
        if (skeleton != null && adamAvatar != null)
        {
            Animator adamAnimator = adamAvatar.GetComponent<Animator>();
            if (adamAnimator == null)
            {
                adamAnimator = adamAvatar.AddComponent<Animator>();
            }
            
            // The skeleton should automatically detect the humanoid rig
            skeleton.enabled = true;
        }
    }
    
    void UpdatePoseSolutionTarget(GameObject targetAvatar)
    {
        if (poseSolutionObject == null) return;
        
        // Find all PosePuppet components and update their target
        PosePuppet[] posePuppets = poseSolutionObject.GetComponentsInChildren<PosePuppet>();
        foreach (var puppet in posePuppets)
        {
            if (puppet != null && targetAvatar != null)
            {
                // Update puppet target to new avatar
                var avatarComponent = targetAvatar.GetComponent<Tracking4All.Avatar>();
                if (avatarComponent != null)
                {
                    // The puppet should now track the new avatar
                    puppet.enabled = false;
                    puppet.enabled = true; // Reset to apply changes
                }
            }
        }
    }
    
    void LateUpdate()
    {
        // Optional: Apply any post-processing or adjustments to tracking
        if (isTrackingAdam && mirrorTracking)
        {
            ApplyMirrorTracking();
        }
    }
    
    void ApplyMirrorTracking()
    {
        if (adamBones == null) return;
        
        foreach (var bone in adamBones)
        {
            if (bone != null)
            {
                // Mirror the X rotation and position for frontal camera tracking
                Vector3 pos = bone.localPosition;
                pos.x = -pos.x;
                bone.localPosition = pos;
                
                Quaternion rot = bone.localRotation;
                rot.y = -rot.y;
                rot.z = -rot.z;
                bone.localRotation = rot;
            }
        }
    }
    
    public bool IsTrackingAdam()
    {
        return isTrackingAdam;
    }
    
    public void ToggleTracking()
    {
        if (isTrackingAdam)
            SwitchToRemy();
        else
            SwitchToAdam();
    }
}
