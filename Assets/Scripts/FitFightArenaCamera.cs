using UnityEngine;

/// <summary>Fits both fighters inside the safe gameplay area, independently of camera-rig offsets.</summary>
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(Camera))]
public sealed class FitFightArenaCamera : MonoBehaviour
{
    private Transform localFighter, opponent;
    private Renderer[] localRenderers, opponentRenderers;
    private Camera view;
    private float distance;
    private bool snap = true;
    private Transform[] trackedBones;
    private float[] boneMargins;
    private Vector3 finalPosition;
    private Quaternion finalRotation;
    private Rect framedSafeArea;
    private Vector2Int framedScreen;
    private float framedAspect;

    // Called after the arena has positioned both fighters, never by pose updates.
    public void ResetFraming() { snap = true; }

    public void Configure(Transform local, Transform remote)
    {
        if (localFighter == local && opponent == remote) return;
        localFighter = local;
        opponent = remote;
        localRenderers = local.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        opponentRenderers = remote.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        view = GetComponent<Camera>();
        view.orthographic = false;
        view.fieldOfView = 36f;
        view.rect = new Rect(0, 0, 1, 1);
        view.clearFlags = CameraClearFlags.Skybox;
        view.backgroundColor = new Color(.32f, .72f, .79f);
        var bones = new System.Collections.Generic.List<Transform>();
        var margins = new System.Collections.Generic.List<float>();
        foreach (var root in new[] { local, remote })
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                if (animator.avatar != null && animator.isHuman)
                    for (int bone = 0; bone < (int)HumanBodyBones.LastBone; bone++)
                    {
                        var t = animator.GetBoneTransform((HumanBodyBones)bone);
                        if (t != null)
                        {
                            bones.Add(t);
                            var kind = (HumanBodyBones)bone;
                            margins.Add(kind == HumanBodyBones.Head ? .07f
                                : kind == HumanBodyBones.LeftFoot || kind == HumanBodyBones.RightFoot
                                  || kind == HumanBodyBones.LeftToes || kind == HumanBodyBones.RightToes ? .05f : .03f);
                        }
                    }
        trackedBones = bones.ToArray();
        boneMargins = margins.ToArray();
        snap = true;
    }

    private void OnPreCull()
    {
        // Legacy rig scripts can move a parent after LateUpdate. Commit the computed
        // world pose immediately before this specific camera renders.
        if (view != null && !snap) transform.SetPositionAndRotation(finalPosition, finalRotation);
    }

    public static Bounds FighterBounds(Transform fighter, Renderer[] renderers)
    {
        // Imported animation bounds can be several times larger than the actual body.
        // Fit the humanoid joints with flesh/shoe padding instead of that animation envelope.
        var animator = fighter.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            bool initialized = false;
            Bounds body = new Bounds(fighter.position, Vector3.zero);
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var bone = animator.GetBoneTransform((HumanBodyBones)i);
                if (bone == null) continue;
                if (!initialized) { body = new Bounds(bone.position, Vector3.zero); initialized = true; }
                else body.Encapsulate(bone.position);
            }
            if (initialized)
            {
                float padding = Mathf.Max(.05f, body.size.y * .065f);
                body.Expand(padding * 2);
                return body;
            }
        }
        Bounds bounds = new Bounds(fighter.position + Vector3.up, new Vector3(1, 2, 1));
        bool found = false;
        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    private void LateUpdate()
    {
        if (view == null || localFighter == null || opponent == null) return;
        if (!snap && framedScreen == new Vector2Int(Screen.width, Screen.height)
            && framedSafeArea == Screen.safeArea && Mathf.Approximately(framedAspect, view.aspect))
        {
            // Keep a world-space lock even when the legacy parent rig follows a body.
            transform.SetPositionAndRotation(finalPosition, finalRotation);
            return;
        }
        var a = FighterBounds(localFighter, localRenderers);
        var b = FighterBounds(opponent, opponentRenderers);
        var bounds = a;
        bounds.Encapsulate(b);
        float height = Mathf.Max(Mathf.Max(a.size.y, b.size.y), .5f);
        foreach (var bone in trackedBones) if (bone != null) bounds.Encapsulate(bone.position);
        bounds.Expand(height * .22f);
        Vector3 heading = Vector3.ProjectOnPlane(opponent.position - localFighter.position, Vector3.up);
        if (heading.sqrMagnitude < .001f) heading = Vector3.forward;
        // Offset the view sideways so the near fighter does not hide the opponent.
        heading = Quaternion.AngleAxis(-34f, Vector3.up) * heading.normalized;
        Quaternion rotation = Quaternion.LookRotation(heading, Vector3.up) * Quaternion.Euler(5f, 0, 0);
        Quaternion inverse = Quaternion.Inverse(rotation);
        float tanY = Mathf.Tan(view.fieldOfView * .5f * Mathf.Deg2Rad);
        float tanX = tanY * view.aspect;
        Rect safe = Screen.safeArea;
        float width = Mathf.Max(1, Screen.width), screenHeight = Mathf.Max(1, Screen.height);
        float left = Mathf.Max(.1f, 1 - 2 * (safe.xMin / width + .022f));
        float right = Mathf.Max(.1f, 2 * (safe.xMax / width - .022f) - 1);
        float bottom = Mathf.Max(.1f, 1 - 2 * (safe.yMin / screenHeight + .055f));
        // Reference composition: heads below the open sky band; shoes near the
        // lower edge. Fit inside this band rather than centering bodies on screen.
        float top = Mathf.Max(.1f, 2 * ((safe.yMin + safe.height * .79f) / screenHeight) - 1);
        var fit = new FitFightFramingMath.FitWindow(tanX, tanY, left, right, bottom, top);
        // Fit actual joint positions with a volume margin. Corners of the combined
        // world AABB include empty space between the two fighters and over-zoom badly.
        if (trackedBones.Length > 0)
        {
            for (int boneIndex = 0; boneIndex < trackedBones.Length; boneIndex++)
            {
                var bone = trackedBones[boneIndex];
                if (bone == null) continue;
                float padding = height * boneMargins[boneIndex];
                Vector3 p = inverse * (bone.position - bounds.center);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 q = p + new Vector3((i & 1) == 0 ? -padding : padding,
                        (i & 2) == 0 ? -padding : padding, (i & 4) == 0 ? -padding : padding);
                    fit.Include(q.x, q.y, q.z);
                }
            }
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = inverse * (corner - bounds.center);
                fit.Include(p.x, p.y, p.z);
            }
        }
        // Frame only on selection/staging or a viewport change. Body motion must
        // not pan, rotate or zoom the gameplay camera.
        float required = Mathf.Max(height * 1.8f, fit.Distance);
        distance = required * 1.06f;
        snap = false;
        framedScreen = new Vector2Int(Screen.width, Screen.height);
        framedSafeArea = Screen.safeArea;
        framedAspect = view.aspect;
        view.nearClipPlane = Mathf.Max(.02f, height * .025f);
        view.farClipPlane = Mathf.Max(100, distance + height * 60);
        finalPosition = bounds.center + rotation * new Vector3(fit.OffsetX(distance), fit.OffsetY(distance), -distance);
        finalRotation = rotation;
        transform.SetPositionAndRotation(finalPosition, finalRotation);
    }
}
