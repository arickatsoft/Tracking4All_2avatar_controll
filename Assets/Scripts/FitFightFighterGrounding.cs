using System.Collections.Generic;
using UnityEngine;

/// <summary>Resolve shoe penetration after tracking and idle animation, before camera framing.</summary>
[DefaultExecutionOrder(9500)]
public sealed class FitFightFighterGrounding : MonoBehaviour
{
    private readonly List<SkinnedMeshRenderer> shoes = new List<SkinnedMeshRenderer>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangleIndices = new List<int>();
    private Mesh bakedShoe;
    private Transform leftFoot, rightFoot;
    private float floorY, clearance, lift;
    public float FloorY => floorY;
    private bool configured;

    private void OnEnable() { Camera.onPreCull += BeforeCamera; }
    private void BeforeCamera(Camera camera)
    {
        if (camera.gameObject.scene == gameObject.scene) ResolveContact();
    }

    public void Configure(float courtY, float bodyHeight)
    {
        floorY = courtY;
        clearance = Mathf.Max(.001f, bodyHeight * .0005f);
        if (configured) return;
        foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            string label = skin.name.ToLowerInvariant();
            if (label.Contains("shoe") || label.Contains("sneaker")) shoes.Add(skin);
        }
        bakedShoe = new Mesh { name = "FitFight shoe contact sample" };
        foreach (var shoe in shoes)
            if (shoe.sharedMesh != null) vertices.Capacity = Mathf.Max(vertices.Capacity, shoe.sharedMesh.vertexCount);
        var animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }
        configured = true;
    }

    private void Update()
    {
        KeepRootUpright();
    }

    private void KeepRootUpright()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private void LateUpdate()
    {
        ResolveContact();
    }

    private void ResolveContact()
    {
        if (!configured) return;
        KeepRootUpright();
        float bottom = float.PositiveInfinity;
        // Reuse the mesh/list and skin only footwear. Toe and heel edges are measured
        // in their actual animated pose, including their pitch during a kick.
        foreach (var shoe in shoes)
        {
            if (shoe == null || !shoe.enabled || !shoe.gameObject.activeInHierarchy) continue;
            // Compensate renderer scale before TransformPoint applies it. Adam's
            // 2x scale was otherwise counted twice, reporting a false floor contact.
            shoe.BakeMesh(bakedShoe, true);
            bakedShoe.GetVertices(vertices);
            // Imported meshes can retain vertices not used by a visible submesh.
            // Those vertices must not determine contact with the ground.
            int submeshes = Mathf.Min(bakedShoe.subMeshCount, shoe.sharedMaterials.Length);
            for (int submesh = 0; submesh < submeshes; submesh++)
            {
                bakedShoe.GetTriangles(triangleIndices, submesh);
                foreach (int index in triangleIndices)
                    bottom = Mathf.Min(bottom, shoe.transform.TransformPoint(vertices[index]).y);
            }
            bakedShoe.Clear();
        }
        if (float.IsPositiveInfinity(bottom))
        {
            if (leftFoot != null) bottom = leftFoot.position.y - clearance * 4f;
            if (rightFoot != null) bottom = Mathf.Min(bottom, rightFoot.position.y - clearance * 4f);
        }
        if (float.IsInfinity(bottom) || float.IsNaN(bottom)) return;
        // Anchor the supporting shoe in BOTH directions. Previously only upward
        // correction was allowed, so a tracked pelvis offset left both feet airborne.
        float correction = floorY + clearance - bottom;
        // Translate the whole pose: a raised kicking foot keeps its relative height.
        transform.position += Vector3.up * correction;
        lift = correction;
        // Persist contact instead of undoing it on every Update. The idle graph
        // must also retain this root, otherwise rendering sees alternating heights.
        foreach (var guard in GetComponentsInChildren<FitFightGuardPose>()) guard.SetStageAnchor();
    }

    private void OnDisable() { Camera.onPreCull -= BeforeCamera; }
    private void OnDestroy() { if (bakedShoe != null) Destroy(bakedShoe); }
}
