using UnityEngine;
using Tracking4All;

/// <summary>
/// Prevents avatar legs from floating when leg tracking data is unreliable.
/// Keeps feet grounded and legs in a natural standing position when visibility is low.
/// </summary>
public class LegGroundLock : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The pose provider that supplies landmark data")]
    public LandmarkProvider<PoseLandmarks> poseProvider;

    [Tooltip("The avatar's Animator component")]
    public Animator animator;

    [Header("Ground Lock Settings")]
    [Tooltip("Minimum visibility threshold to use tracked leg data (0-1). Below this, legs will be grounded.")]
    [Range(0f, 1f)]
    public float minLegVisibility = 0.5f;

    [Tooltip("Minimum presence threshold to use tracked leg data (0-1). Below this, legs will be grounded.")]
    [Range(0f, 1f)]
    public float minLegPresence = 0.5f;

    [Tooltip("Require BOTH legs to be visible before unlocking (recommended)")]
    public bool requireBothLegs = true;

    [Tooltip("Enable ground locking for legs")]
    public bool enableGroundLock = true;

    [Tooltip("Ground height (Y position where feet should be)")]
    public float groundHeight = 0f;

    [Tooltip("Delay before starting leg detection (seconds) - prevents immediate unlock on startup")]
    [Range(0f, 5f)]
    public float startupDelay = 1.0f;

    [Tooltip("How smoothly to transition between tracked and grounded poses")]
    [Range(0.01f, 1f)]
    public float transitionSpeed = 0.1f;

    [Header("Default Standing Pose")]
    [Tooltip("Use the avatar's current pose at startup as the default standing pose")]
    public bool captureInitialPose = true;

    [Tooltip("Default rotation for upper legs when grounded (local rotation in degrees). Used if captureInitialPose is false.")]
    public Vector3 defaultUpperLegRotation = new Vector3(0, 0, 0);

    [Tooltip("Default rotation for lower legs when grounded (local rotation in degrees). Used if captureInitialPose is false.")]
    public Vector3 defaultLowerLegRotation = new Vector3(0, 0, 0);

    [Tooltip("Default rotation for feet when grounded (local rotation in degrees). Used if captureInitialPose is false.")]
    public Vector3 defaultFootRotation = new Vector3(0, 0, 0);

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Cached transforms
    private Transform leftUpperLeg;
    private Transform leftLowerLeg;
    private Transform leftFoot;
    private Transform rightUpperLeg;
    private Transform rightLowerLeg;
    private Transform rightFoot;

    // Smoothing state
    private float leftLegBlend = 1f;  // Start locked
    private float rightLegBlend = 1f; // Start locked
    private float startTime;

    // Original rotations for blending
    private Quaternion leftUpperLegTracked;
    private Quaternion leftLowerLegTracked;
    private Quaternion leftFootTracked;
    private Quaternion rightUpperLegTracked;
    private Quaternion rightLowerLegTracked;
    private Quaternion rightFootTracked;

    // Captured initial pose
    private Quaternion leftUpperLegDefault;
    private Quaternion leftLowerLegDefault;
    private Quaternion leftFootDefault;
    private Quaternion rightUpperLegDefault;
    private Quaternion rightLowerLegDefault;
    private Quaternion rightFootDefault;
    private Vector3 leftFootInitialPos;
    private Vector3 rightFootInitialPos;

    void Start()
    {
        // Auto-find components if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (poseProvider == null)
        {
            // Try to find PosePuppet and get its provider
            var posePuppet = FindObjectOfType<PosePuppet>();
            if (posePuppet != null)
            {
                var providerField = typeof(PosePuppet).GetField("poseProvider",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (providerField != null)
                {
                    poseProvider = (LandmarkProvider<PoseLandmarks>)providerField.GetValue(posePuppet);
                }
            }
        }

        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("LegGroundLock: Animator with humanoid rig is required!");
            enabled = false;
            return;
        }

        // Cache bone transforms
        leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        // Capture initial standing pose
        if (captureInitialPose)
        {
            CaptureInitialPose();
        }
        else
        {
            // Use manual default rotations
            leftUpperLegDefault = rightUpperLegDefault = Quaternion.Euler(defaultUpperLegRotation);
            leftLowerLegDefault = rightLowerLegDefault = Quaternion.Euler(defaultLowerLegRotation);
            leftFootDefault = rightFootDefault = Quaternion.Euler(defaultFootRotation);
        }

        // Record start time for startup delay
        startTime = Time.time;

        Debug.Log("✅ LegGroundLock initialized - legs will be locked for first " + startupDelay + " seconds");
    }

    void CaptureInitialPose()
    {
        if (leftUpperLeg != null && leftLowerLeg != null && leftFoot != null)
        {
            leftUpperLegDefault = leftUpperLeg.localRotation;
            leftLowerLegDefault = leftLowerLeg.localRotation;
            leftFootDefault = leftFoot.localRotation;
            leftFootInitialPos = leftFoot.position;

            Debug.Log($"📸 Captured LEFT leg initial pose: Upper={leftUpperLegDefault.eulerAngles}, Lower={leftLowerLegDefault.eulerAngles}, Foot={leftFootDefault.eulerAngles}");
        }

        if (rightUpperLeg != null && rightLowerLeg != null && rightFoot != null)
        {
            rightUpperLegDefault = rightUpperLeg.localRotation;
            rightLowerLegDefault = rightLowerLeg.localRotation;
            rightFootDefault = rightFoot.localRotation;
            rightFootInitialPos = rightFoot.position;

            Debug.Log($"📸 Captured RIGHT leg initial pose: Upper={rightUpperLegDefault.eulerAngles}, Lower={rightLowerLegDefault.eulerAngles}, Foot={rightFootDefault.eulerAngles}");
        }

        // Set ground height to average of foot positions
        if (leftFoot != null && rightFoot != null)
        {
            groundHeight = (leftFoot.position.y + rightFoot.position.y) / 2f;
            Debug.Log($"📏 Ground height set to: {groundHeight}");
        }
    }

    void LateUpdate()
    {
        if (!enableGroundLock || animator == null) return;
        // FitFight already has per-leg visibility handling and an authored guard.
        // A second bone writer with a both-legs gate causes snapping during kicks.
        var guard = GetComponent<FitFightGuardPose>();
        if (guard != null && guard.isActiveAndEnabled) return;

        // Keep legs locked during startup delay
        float timeSinceStart = Time.time - startTime;
        if (timeSinceStart < startupDelay)
        {
            // Force legs to stay locked during startup
            leftLegBlend = 1f;
            rightLegBlend = 1f;
            ApplyGroundedLeg(leftUpperLeg, leftLowerLeg, leftFoot, leftLegBlend);
            ApplyGroundedLeg(rightUpperLeg, rightLowerLeg, rightFoot, rightLegBlend);

            if (showDebugInfo)
            {
                Debug.Log($"⏰ Startup delay: {(startupDelay - timeSinceStart):F1}s remaining - legs LOCKED");
            }
            return;
        }

        // Check leg landmark visibility
        bool leftLegVisible = IsLegVisible(true);
        bool rightLegVisible = IsLegVisible(false);

        // Determine target blend based on both legs requirement
        float targetBlend;
        if (requireBothLegs)
        {
            // Only unlock when BOTH legs are visible
            bool bothLegsVisible = leftLegVisible && rightLegVisible;

            // Once both legs are detected, disable this script permanently
            if (bothLegsVisible)
            {
                // Reset blend to 0 (fully tracking) before disabling
                leftLegBlend = 0f;
                rightLegBlend = 0f;

                Debug.Log("🔓 Both legs detected - DISABLING LegGroundLock permanently for this session");
                enabled = false;
                return;
            }

            targetBlend = 1f; // Keep locked

            // Apply same blend to both legs
            leftLegBlend = Mathf.Lerp(leftLegBlend, targetBlend, transitionSpeed);
            rightLegBlend = leftLegBlend; // Keep in sync

            if (showDebugInfo)
            {
                Debug.Log($"👀 Left: {(leftLegVisible ? "✅" : "❌")}, Right: {(rightLegVisible ? "✅" : "❌")} → Both visible: {(bothLegsVisible ? "YES" : "NO")} → Blend: {leftLegBlend:F2} (LOCKED - waiting for both legs)");
            }
        }
        else
        {
            // Independent leg tracking
            float targetLeftBlend = leftLegVisible ? 0f : 1f;
            float targetRightBlend = rightLegVisible ? 0f : 1f;

            leftLegBlend = Mathf.Lerp(leftLegBlend, targetLeftBlend, transitionSpeed);
            rightLegBlend = Mathf.Lerp(rightLegBlend, targetRightBlend, transitionSpeed);

            if (showDebugInfo)
            {
                Debug.Log($"Left Leg: {(leftLegVisible ? "✅ VISIBLE" : "❌ NOT VISIBLE")}, Blend={leftLegBlend:F2} | Right Leg: {(rightLegVisible ? "✅ VISIBLE" : "❌ NOT VISIBLE")}, Blend={rightLegBlend:F2}");
            }
        }

        // Apply ground-locked pose when legs are not fully visible
        if (leftLegBlend > 0.01f)
        {
            ApplyGroundedLeg(leftUpperLeg, leftLowerLeg, leftFoot, leftLegBlend);
        }

        if (rightLegBlend > 0.01f)
        {
            ApplyGroundedLeg(rightUpperLeg, rightLowerLeg, rightFoot, rightLegBlend);
        }
    }

    bool IsLegVisible(bool isLeft)
    {
        if (poseProvider == null) return true; // If no provider, assume visible

        try
        {
            IProvider<PoseLandmarks, Landmark> provider = (IProvider<PoseLandmarks, Landmark>)poseProvider;

            // Check hip and knee landmarks (NOT ankle/foot since you don't need feet tracked)
            PoseLandmarks hip = isLeft ? PoseLandmarks.LEFT_HIP : PoseLandmarks.RIGHT_HIP;
            PoseLandmarks knee = isLeft ? PoseLandmarks.LEFT_KNEE : PoseLandmarks.RIGHT_KNEE;

            Landmark hipLandmark = provider.Get(0, hip);
            Landmark kneeLandmark = provider.Get(0, knee);

            if (showDebugInfo)
            {
                string side = isLeft ? "LEFT" : "RIGHT";
                Debug.Log($"{side} LEG - Hip: V={hipLandmark.Visibility:F2} P={hipLandmark.Presence:F2}, " +
                          $"Knee: V={kneeLandmark.Visibility:F2} P={kneeLandmark.Presence:F2}");
            }

            // Leg is visible if BOTH hip and knee meet minimum thresholds
            bool hipVisible = hipLandmark.Visibility >= minLegVisibility && hipLandmark.Presence >= minLegPresence;
            bool kneeVisible = kneeLandmark.Visibility >= minLegVisibility && kneeLandmark.Presence >= minLegPresence;

            bool legVisible = hipVisible && kneeVisible;

            if (showDebugInfo)
            {
                string side = isLeft ? "LEFT" : "RIGHT";
                Debug.Log($"{side} LEG RESULT: {(legVisible ? "✅ VISIBLE" : "❌ NOT VISIBLE")} (will {(legVisible ? "track normally" : "ground lock")})");
            }

            return legVisible;
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"Error checking leg visibility: {e.Message}");
            }
            return true; // If error accessing landmarks, assume visible
        }
    }

    void ApplyGroundedLeg(Transform upperLeg, Transform lowerLeg, Transform foot, float blend)
    {
        if (upperLeg == null || lowerLeg == null || foot == null) return;

        bool isLeftLeg = (upperLeg == leftUpperLeg);

        // Store tracked rotations when legs are being tracked
        if (blend < 0.1f)
        {
            if (isLeftLeg)
            {
                leftUpperLegTracked = upperLeg.localRotation;
                leftLowerLegTracked = lowerLeg.localRotation;
                leftFootTracked = foot.localRotation;
            }
            else
            {
                rightUpperLegTracked = upperLeg.localRotation;
                rightLowerLegTracked = lowerLeg.localRotation;
                rightFootTracked = foot.localRotation;
            }
        }

        // Get tracked rotations
        Quaternion trackedUpper = isLeftLeg ? leftUpperLegTracked : rightUpperLegTracked;
        Quaternion trackedLower = isLeftLeg ? leftLowerLegTracked : rightLowerLegTracked;
        Quaternion trackedFoot = isLeftLeg ? leftFootTracked : rightFootTracked;

        // Get default standing pose rotations
        Quaternion defaultUpper = isLeftLeg ? leftUpperLegDefault : rightUpperLegDefault;
        Quaternion defaultLower = isLeftLeg ? leftLowerLegDefault : rightLowerLegDefault;
        Quaternion defaultFootRot = isLeftLeg ? leftFootDefault : rightFootDefault;

        // Blend between tracked and grounded poses
        upperLeg.localRotation = Quaternion.Slerp(trackedUpper, defaultUpper, blend);
        lowerLeg.localRotation = Quaternion.Slerp(trackedLower, defaultLower, blend);
        foot.localRotation = Quaternion.Slerp(trackedFoot, defaultFootRot, blend);

        // Lock foot to ground height
        if (blend > 0.5f && foot != null)
        {
            Vector3 footPos = foot.position;
            footPos.y = Mathf.Lerp(footPos.y, groundHeight, blend);
            foot.position = footPos;
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying) return;

        if (leftFoot != null)
        {
            Gizmos.color = leftLegBlend > 0.5f ? Color.red : Color.green;
            Gizmos.DrawSphere(leftFoot.position, 0.05f);
        }

        if (rightFoot != null)
        {
            Gizmos.color = rightLegBlend > 0.5f ? Color.red : Color.green;
            Gizmos.DrawSphere(rightFoot.position, 0.05f);
        }

        // Draw ground plane
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        center.y = groundHeight;
        Gizmos.DrawLine(center + Vector3.left, center + Vector3.right);
        Gizmos.DrawLine(center + Vector3.forward, center + Vector3.back);
    }
}
