using UnityEngine;
using System.Collections.Generic;

public class GreenFlashHorizontalDetector : MonoBehaviour
{
    [Header("Avatars")]
    [Tooltip("Remy avatar reference")]
    public Transform remy;

    [Tooltip("Adam avatar reference")]
    public Transform adam;

    public enum SelectedAvatar { Remy, Adam }

    [Header("Detection Settings")]
    [Tooltip("Which avatar is selected (gets visual feedback when THEY hit)")]
    public SelectedAvatar selectedAvatar = SelectedAvatar.Remy;

    [Tooltip("Distance threshold for limb-to-limb contact (in meters)")]
    public float contactDistance = 0.3f;

    [Tooltip("Minimum limb velocity (m/s) to count as a hit")]
    public float minimumHitVelocity = 1.5f;

    // Cached squared distance (calculated once in Start)
    private float sqrContactDistance;

    [Tooltip("Cooldown between triggers")]
    public float cooldownTime = 1.5f;

    [Tooltip("Delay before starting hit detection (seconds)")]
    public float startupDelay = 2.0f;

    [Header("Attacking Limbs (for SELECTED avatar)")]
    [Tooltip("Detect hits from left hand")]
    public bool detectLeftHand = true;

    [Tooltip("Detect hits from right hand")]
    public bool detectRightHand = true;

    [Tooltip("Detect hits from left foot")]
    public bool detectLeftFoot = true;

    [Tooltip("Detect hits from right foot")]
    public bool detectRightFoot = true;

    [Tooltip("Detect hits from head")]
    public bool detectHead = false;

    [Tooltip("Detect hits from left elbow")]
    public bool detectLeftElbow = false;

    [Tooltip("Detect hits from right elbow")]
    public bool detectRightElbow = false;

    [Tooltip("Detect hits from left knee")]
    public bool detectLeftKnee = false;

    [Tooltip("Detect hits from right knee")]
    public bool detectRightKnee = false;

    [Header("Visual Feedback")]
    public HitFeedback hitFeedback;

    [Header("Flash Settings")]
    [Tooltip("Color of the screen flash")]
    public Color flashColor = Color.green;

    [Tooltip("Duration of the flash")]
    public float flashDuration = 0.5f;

    [Tooltip("Maximum flash opacity")]
    public float maxFlashAlpha = 0.8f;

    [Header("Sound Settings")]
    [Tooltip("Hit sound effect (assign an AudioClip)")]
    public AudioClip hitSound;

    [Tooltip("Volume of hit sound (0-1)")]
    [Range(0f, 1f)]
    public float hitSoundVolume = 0.8f;

    [Header("Target Body Parts (opponent's body)")]
    [Tooltip("Track opponent's torso for hit detection")]
    public bool trackTorso = true;

    [Tooltip("Track opponent's head for hit detection")]
    public bool trackHead = true;

    [Tooltip("Track opponent's arms for hit detection")]
    public bool trackArms = true;

    [Tooltip("Track opponent's legs for hit detection")]
    public bool trackLegs = true;

    [Header("Debug")]
    public bool debugMode = false;

    private bool canTrigger = true;
    private List<Transform> attackingLimbs = new List<Transform>();
    private List<Transform> targetBodyParts = new List<Transform>();
    private Dictionary<Transform, Vector3> previousLimbPositions = new Dictionary<Transform, Vector3>();
    private float gameStartTime;

    void Start()
    {
        gameStartTime = Time.time;

        // Pre-calculate squared distance for performance (avoids sqrt in Update)
        sqrContactDistance = contactDistance * contactDistance;

        // Auto-find avatars if not assigned
        if (remy == null)
        {
            GameObject remyObj = GameObject.FindGameObjectWithTag("Player");
            if (remyObj != null) remy = remyObj.transform;
        }

        if (adam == null)
        {
            GameObject adamObj = GameObject.FindGameObjectWithTag("Enemy");
            if (adamObj != null) adam = adamObj.transform;
        }

        // Find HitFeedback if not assigned
        if (hitFeedback == null)
        {
            hitFeedback = FindObjectOfType<HitFeedback>();
        }

        // Find attacking limbs and target body parts based on selected avatar
        FindAttackingLimbs();
        FindTargetBodyParts();

        // Initialize previous positions for velocity calculation
        foreach (Transform limb in attackingLimbs)
        {
            if (limb != null)
            {
                previousLimbPositions[limb] = limb.position;
            }
        }

        string attackerName = selectedAvatar == SelectedAvatar.Remy ? "Remy" : "Adam";
        string targetName = selectedAvatar == SelectedAvatar.Remy ? "Adam" : "Remy";

        #if UNITY_EDITOR
        Debug.Log($"🎮 Selected Avatar: {attackerName}");
        Debug.Log($"🎯 {attackerName} attacking with {attackingLimbs.Count} limb(s)");
        Debug.Log($"🎯 {targetName} has {targetBodyParts.Count} body part(s) to hit");
        Debug.Log($"📏 Contact distance: {contactDistance}m");
        Debug.Log($"⚡ Minimum hit velocity: {minimumHitVelocity} m/s");
        Debug.Log($"⏱️ Startup delay: {startupDelay}s");
        #endif
    }

    void FindAttackingLimbs()
    {
        attackingLimbs.Clear();

        // Get the selected avatar's transform
        Transform attacker = selectedAvatar == SelectedAvatar.Remy ? remy : adam;

        if (attacker == null)
        {
            Debug.LogError($"❌ Could not find {selectedAvatar} avatar!");
            return;
        }

        // Build list of attacking limbs based on settings
        List<string> limbNamesToDetect = new List<string>();

        if (detectLeftHand) limbNamesToDetect.Add("mixamorig:LeftHand");
        if (detectRightHand) limbNamesToDetect.Add("mixamorig:RightHand");
        if (detectLeftFoot) limbNamesToDetect.Add("mixamorig:LeftFoot");
        if (detectRightFoot) limbNamesToDetect.Add("mixamorig:RightFoot");
        if (detectHead) limbNamesToDetect.Add("mixamorig:Head");
        if (detectLeftElbow) limbNamesToDetect.Add("mixamorig:LeftForeArm");
        if (detectRightElbow) limbNamesToDetect.Add("mixamorig:RightForeArm");
        if (detectLeftKnee) limbNamesToDetect.Add("mixamorig:LeftLeg");
        if (detectRightKnee) limbNamesToDetect.Add("mixamorig:RightLeg");

        foreach (string limbName in limbNamesToDetect)
        {
            Transform limbTransform = FindChildRecursive(attacker, limbName);
            if (limbTransform != null)
            {
                attackingLimbs.Add(limbTransform);
                #if UNITY_EDITOR
                if (debugMode) Debug.Log($"✅ {selectedAvatar} attacking limb: {limbName}");
                #endif
            }
        }
    }

    void FindTargetBodyParts()
    {
        targetBodyParts.Clear();

        // Get the opponent's transform
        Transform target = selectedAvatar == SelectedAvatar.Remy ? adam : remy;

        if (target == null)
        {
            Debug.LogError($"❌ Target avatar not assigned!");
            return;
        }

        // Build list of target's body parts based on settings
        List<string> bodyPartNames = new List<string>();

        if (trackHead) bodyPartNames.Add("mixamorig:Head");
        if (trackTorso)
        {
            bodyPartNames.Add("mixamorig:Spine");
            bodyPartNames.Add("mixamorig:Spine1");
            bodyPartNames.Add("mixamorig:Spine2");
        }
        if (trackArms)
        {
            bodyPartNames.Add("mixamorig:LeftArm");
            bodyPartNames.Add("mixamorig:RightArm");
            bodyPartNames.Add("mixamorig:LeftForeArm");
            bodyPartNames.Add("mixamorig:RightForeArm");
            bodyPartNames.Add("mixamorig:LeftHand");
            bodyPartNames.Add("mixamorig:RightHand");
        }
        if (trackLegs)
        {
            bodyPartNames.Add("mixamorig:LeftUpLeg");
            bodyPartNames.Add("mixamorig:RightUpLeg");
            bodyPartNames.Add("mixamorig:LeftLeg");
            bodyPartNames.Add("mixamorig:RightLeg");
            bodyPartNames.Add("mixamorig:LeftFoot");
            bodyPartNames.Add("mixamorig:RightFoot");
        }

        foreach (string bodyPartName in bodyPartNames)
        {
            Transform bodyPart = FindChildRecursive(target, bodyPartName);
            if (bodyPart != null)
            {
                targetBodyParts.Add(bodyPart);
                #if UNITY_EDITOR
                if (debugMode) Debug.Log($"✅ Target body part: {bodyPartName}");
                #endif
            }
        }
    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
            {
                return child;
            }
            Transform result = FindChildRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    void Update()
    {
        if (!canTrigger) return;

        // Skip hit detection during startup delay
        if (Time.time - gameStartTime < startupDelay)
        {
            // Still update previous positions during startup
            foreach (Transform limb in attackingLimbs)
            {
                if (limb != null)
                {
                    previousLimbPositions[limb] = limb.position;
                }
            }
            return;
        }

        // Check each attacking limb against each target body part
        foreach (Transform attackerLimb in attackingLimbs)
        {
            if (attackerLimb == null) continue;

            // Calculate velocity of attacking limb (using magnitude for performance)
            Vector3 currentPosition = attackerLimb.position;
            Vector3 previousPosition = previousLimbPositions.ContainsKey(attackerLimb)
                ? previousLimbPositions[attackerLimb]
                : currentPosition;

            float velocity = (currentPosition - previousPosition).magnitude / Time.deltaTime;

            // Update previous position for next frame
            previousLimbPositions[attackerLimb] = currentPosition;

            // Only check for hits if limb is moving fast enough
            if (velocity < minimumHitVelocity)
            {
                #if UNITY_EDITOR
                if (debugMode)
                {
                    Debug.Log($"⚪ {attackerLimb.name} too slow: {velocity:F2} m/s (need {minimumHitVelocity} m/s)");
                }
                #endif
                continue;
            }

            // Check distance to each target body part
            foreach (Transform targetBodyPart in targetBodyParts)
            {
                if (targetBodyPart == null) continue;

                // Calculate squared distance (avoids expensive sqrt operation)
                float sqrDistance = (attackerLimb.position - targetBodyPart.position).sqrMagnitude;

                #if UNITY_EDITOR
                if (debugMode && sqrDistance < (contactDistance + 0.2f) * (contactDistance + 0.2f))
                {
                    // Only calculate actual distance for debug logging
                    float distance = Mathf.Sqrt(sqrDistance);
                    Debug.Log($"🔵 {attackerLimb.name} → {targetBodyPart.name}: {distance:F3}m (vel: {velocity:F2} m/s)");
                }
                #endif

                // HIT DETECTED! (using squared distance comparison)
                if (sqrDistance <= sqrContactDistance)
                {
                    string attackerName = selectedAvatar == SelectedAvatar.Remy ? "Remy" : "Adam";
                    string targetName = selectedAvatar == SelectedAvatar.Remy ? "Adam" : "Remy";

                    #if UNITY_EDITOR
                    // Only calculate actual distance for logging
                    float actualDistance = Mathf.Sqrt(sqrDistance);
                    Debug.Log($"💥🟢 {attackerName}'s {attackerLimb.name} HIT {targetName}'s {targetBodyPart.name}!");
                    Debug.Log($"   Distance: {actualDistance:F3}m, Velocity: {velocity:F2} m/s");
                    #endif

                    TriggerGreenFlash(attackerLimb.name, targetBodyPart.name);
                    StartCooldown();
                    return; // Exit after first hit
                }
            }
        }
    }

    void TriggerGreenFlash(string attackerLimbName, string targetBodyPartName)
    {
        #if UNITY_EDITOR
        string attackerName = selectedAvatar == SelectedAvatar.Remy ? "Remy" : "Adam";
        string targetName = selectedAvatar == SelectedAvatar.Remy ? "Adam" : "Remy";
        Debug.Log($"🎯🟢 {attackerName}'s {attackerLimbName} HIT {targetName}'s {targetBodyPartName}!");
        #endif

        // Play hit sound
        if (hitSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound(hitSound);
            #if UNITY_EDITOR
            Debug.Log("🔊 Playing hit sound!");
            #endif
        }
        #if UNITY_EDITOR
        else if (hitSound == null)
        {
            Debug.LogWarning("⚠️ No hit sound assigned in GreenFlashHorizontalDetector!");
        }
        #endif

        // Trigger visual flash
        if (hitFeedback != null)
        {
            StartCoroutine(GreenScreenFlash());
        }
    }

    private System.Collections.IEnumerator GreenScreenFlash()
    {
        if (hitFeedback == null || hitFeedback.hitFlashImage == null) yield break;

        #if UNITY_EDITOR
        Debug.Log("🟢 Starting green screen flash...");
        #endif

        // Fade in to green
        float half = flashDuration * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, maxFlashAlpha, t / half);
            hitFeedback.hitFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }

        // Fade out from green
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(maxFlashAlpha, 0f, t / half);
            hitFeedback.hitFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }

        // Ensure fully transparent
        hitFeedback.hitFlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        #if UNITY_EDITOR
        Debug.Log("🟢 Green screen flash completed!");
        #endif
    }

    void StartCooldown()
    {
        canTrigger = false;
        #if UNITY_EDITOR
        Debug.Log($"🕐 Starting {cooldownTime}s cooldown...");
        #endif
        Invoke("ResetCooldown", cooldownTime);
    }

    void ResetCooldown()
    {
        canTrigger = true;
        #if UNITY_EDITOR
        Debug.Log("✅ Ready for next hit detection");
        #endif
    }

    /// <summary>
    /// Call this method to change which avatar is selected (useful when switching during gameplay)
    /// </summary>
    public void SetSelectedAvatar(SelectedAvatar avatar)
    {
        selectedAvatar = avatar;
        #if UNITY_EDITOR
        Debug.Log($"🔄 Switched to {avatar}");
        #endif

        // Rebuild limb and body part lists
        FindAttackingLimbs();
        FindTargetBodyParts();

        // Reset velocity tracking
        previousLimbPositions.Clear();
        foreach (Transform limb in attackingLimbs)
        {
            if (limb != null)
            {
                previousLimbPositions[limb] = limb.position;
            }
        }
    }
}
