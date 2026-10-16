using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Reference-inspired court, built as three combined meshes for mobile rendering.</summary>
[DefaultExecutionOrder(12000)]
public sealed class FitFightStreetArena : MonoBehaviour
{
    private readonly List<Material> materials = new List<Material>();
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private readonly List<Light> hiddenLights = new List<Light>();
    private AmbientMode previousAmbient;
    private Color previousSky, previousEquator, previousGround;
    private bool initialized;
    private Material previousSkybox;
    private Transform localFighter, remoteFighter;
    private Light arenaSun;
    private Light arenaFill;
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    public void Build(Transform first, Transform second)
    {
        if (initialized) return;
        Material template = Resources.Load<Material>("FitFightStreet");
        if (template == null) { Debug.LogError("FitFightStreet material is missing."); return; }
        initialized = true;
        if (localFighter == null) { localFighter = first; remoteFighter = second; }
        TuneHair(first, new Color(.62f, .46f, .32f));
        TuneHair(second, new Color(.25f, .18f, .13f));
        var a = FitFightArenaCamera.FighterBounds(first, first.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        var b = FitFightArenaCamera.FighterBounds(second, second.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        float unit = Mathf.Max(.5f, Mathf.Max(a.size.y, b.size.y)) / 2f;
        Vector3 midpoint = (a.center + b.center) * .5f;
        transform.position = new Vector3(midpoint.x, Mathf.Min(FootLevel(first, a.min.y), FootLevel(second, b.min.y)), midpoint.z);
        transform.localScale = Vector3.one * unit;

        // The old road is not consistently parented under Environment. Exclude fighter
        // meshes, UI, and Tracking4All's layer-5 live camera screen; hide the rest.
        foreach (var renderer in FindObjectsOfType<MeshRenderer>())
        {
            if (!renderer.enabled || renderer.gameObject.scene != gameObject.scene
                || renderer.transform.IsChildOf(first) || renderer.transform.IsChildOf(second)
                || renderer.GetComponent<RectTransform>() != null
                || renderer.GetComponentInParent<Canvas>() != null
                || renderer.gameObject.layer == 5) continue;
            hiddenRenderers.Add(renderer);
            renderer.enabled = false;
        }

        var cubeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cube = cubeObject.GetComponent<MeshFilter>().sharedMesh;
        cubeObject.SetActive(false);
        var paving = new List<CombineInstance>();
        var metal = new List<CombineInstance>();
        var walls = new List<CombineInstance>();
        Add(paving, cube, new Vector3(0, -.12f, 0), new Vector3(40, .24f, 40));
        // Three-sided court. Its entrance stays behind the selected player's camera.
        for (int side = 0; side < 3; side++)
        {
            Quaternion turn = Quaternion.Euler(0, side == 0 ? 0 : side == 1 ? 90 : -90, 0);
            Add(walls, cube, turn * new Vector3(0, .55f, 7f), new Vector3(14, 1.1f, .16f), turn);
            // Recessed lower panels and cap rails give the fence depth in sunlight.
            for (int panel = -6; panel <= 6; panel++)
                Add(metal, cube, turn * new Vector3(panel * 1.1f, .55f, 6.89f), new Vector3(.035f, 1.1f, .045f), turn);
            Add(metal, cube, turn * new Vector3(0, 1.1f, 6.89f), new Vector3(14, .06f, .09f), turn);
            for (int post = -3; post <= 3; post++)
                Add(metal, cube, turn * new Vector3(post * 2.3f, 1.3f, 7f), new Vector3(.10f, 2.6f, .10f));
            for (int rail = 0; rail < 3; rail++)
                Add(metal, cube, turn * new Vector3(0, .8f + rail * .85f, 7f), new Vector3(14, .055f, .055f), turn);
            for (int wire = -27; wire <= 27; wire++)
                Add(metal, cube, turn * new Vector3(wire * .25f, 1.65f, 7f), new Vector3(.016f, 1.7f, .016f));
            for (int wire = 0; wire <= 6; wire++)
                Add(metal, cube, turn * new Vector3(0, 1f + wire * .25f, 7f), new Vector3(14, .016f, .016f), turn);
        }
        var court = Resources.Load<Material>("FitFightCourt");
        Combine("Warm concrete", paving, court != null ? court : template, new Color(.43f, .37f, .29f), true);
        materials[materials.Count - 1].SetFloat("_Unit", unit * .85f);
        Combine("Court boundary", walls, template, new Color(.075f, .105f, .11f), true);
        Combine("Steel court fence", metal, template, new Color(.08f, .12f, .13f), true);
        var sphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereObject.SetActive(false);
        var foliage = new List<CombineInstance>();
        var random = new System.Random(42);
        for (int side = 0; side < 3; side++)
        for (int tree = 0; tree < 9; tree++)
        {
            Quaternion turn = Quaternion.Euler(0, side == 0 ? 0 : side == 1 ? 90 : -90, 0);
            float height = 1.1f + (float)random.NextDouble() * .7f;
            Add(foliage, sphereObject.GetComponent<MeshFilter>().sharedMesh,
                turn * new Vector3((tree - 4) * 2.8f, height * .55f, 11f + (float)random.NextDouble() * 3),
                new Vector3(3.4f, height, 2.5f));
        }
        Combine("Distant greenery", foliage, template, new Color(.27f, .33f, .16f), false);
        Destroy(sphereObject);
        Destroy(cubeObject);

        foreach (var light in FindObjectsOfType<Light>())
        {
            if (light.type != LightType.Directional || !light.enabled) continue;
            hiddenLights.Add(light);
            light.enabled = false;
        }
        var sunObject = new GameObject("FitFight Afternoon Sun");
        sunObject.transform.SetParent(transform, false);
        sunObject.transform.rotation = Quaternion.Euler(38, -35, 0);
        var sun = sunObject.AddComponent<Light>();
        arenaSun = sun;
        sun.type = LightType.Directional;
        sun.color = new Color(1f, .94f, .84f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = .85f;
        sun.shadowBias = .008f;
        sun.shadowNormalBias = .015f;
        sun.shadowCustomResolution = 2048;
        var fillObject = new GameObject("FitFight Soft Camera Fill");
        fillObject.transform.SetParent(transform, false);
        arenaFill = fillObject.AddComponent<Light>();
        arenaFill.type = LightType.Directional;
        arenaFill.color = new Color(.90f, .95f, 1f);
        arenaFill.intensity = .38f;
        arenaFill.shadows = LightShadows.None;
        previousSkybox = RenderSettings.skybox;
        var sky = Resources.Load<Material>("FitFightSky");
        if (sky != null) RenderSettings.skybox = sky;
        previousAmbient = RenderSettings.ambientMode;
        previousSky = RenderSettings.ambientSkyColor;
        previousEquator = RenderSettings.ambientEquatorColor;
        previousGround = RenderSettings.ambientGroundColor;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.52f, .63f, .72f);
        RenderSettings.ambientEquatorColor = new Color(.37f, .41f, .44f);
        RenderSettings.ambientGroundColor = new Color(.26f, .23f, .20f);
        SetView(localFighter, remoteFighter);
    }

    public static float FootLevel(Transform fighter, float fallback)
    {
        float lowest = float.PositiveInfinity;
        var mesh = new Mesh();
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        foreach (var skin in fighter.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            string name = skin.name.ToLowerInvariant();
            if (!name.Contains("shoe") && !name.Contains("sneaker")) continue;
            skin.BakeMesh(mesh, true);
            mesh.GetVertices(vertices);
            for (int submesh = 0; submesh < Mathf.Min(mesh.subMeshCount, skin.sharedMaterials.Length); submesh++)
            {
                mesh.GetTriangles(indices, submesh);
                foreach (int index in indices) lowest = Mathf.Min(lowest, skin.transform.TransformPoint(vertices[index]).y);
            }
            mesh.Clear();
        }
        Destroy(mesh);
        return float.IsPositiveInfinity(lowest) ? fallback : lowest;
    }

    public void SetView(Transform local, Transform remote)
    {
        localFighter = local; remoteFighter = remote;
        if (!initialized || local == null || remote == null) return;
        Vector3 direction = Vector3.ProjectOnPlane(remote.position - local.position, Vector3.up);
        if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(direction);
        Quaternion view = transform.rotation * Quaternion.Euler(0, -34, 0);
        if (arenaSun != null)
            arenaSun.transform.rotation = Quaternion.LookRotation(view * (Vector3.right * .65f - Vector3.forward * .75f) + Vector3.down);
        if (arenaFill != null)
            arenaFill.transform.rotation = Quaternion.LookRotation(view * Vector3.forward + Vector3.down * .25f);
    }

    private void LateUpdate()
    {
        foreach (var renderer in hiddenRenderers) if (renderer != null) renderer.enabled = false;
    }

    private void TuneHair(Transform fighter, Color tint)
    {
        foreach (var renderer in fighter.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Material[] source = renderer.sharedMaterials;
            Material[] tuned = (Material[])source.Clone();
            bool changed = false;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null || !(renderer.name + source[i].name).ToLowerInvariant().Contains("hair")) continue;
                var material = new Material(source[i]);
                if (material.HasProperty("_Color")) material.color = tint;
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .15f);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .15f);
                if (material.HasProperty("_GlossMapScale")) material.SetFloat("_GlossMapScale", .12f);
                if (material.HasProperty("_SpecColor")) material.SetColor("_SpecColor", new Color(.025f, .025f, .025f));
                if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 0);
                if (material.HasProperty("_GlossyReflections")) material.SetFloat("_GlossyReflections", 0);
                material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0);
                materials.Add(material); tuned[i] = material; changed = true;
            }
            if (changed) { originalMaterials.Add(renderer, source); renderer.sharedMaterials = tuned; }
        }
    }

    private static void Add(List<CombineInstance> list, Mesh cube, Vector3 position, Vector3 scale, Quaternion? rotation = null)
    {
        list.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(position, rotation ?? Quaternion.identity, scale) });
    }

    private void Combine(string name, List<CombineInstance> parts, Material template, Color color, bool shadows)
    {
        var mesh = new Mesh { name = name };
        mesh.CombineMeshes(parts.ToArray(), true, true);
        meshes.Add(mesh);
        var material = new Material(template) { name = name, color = color };
        material.SetFloat("_Glossiness", .12f);
        material.SetFloat("_Metallic", 0);
        materials.Add(material);
        var child = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        child.transform.SetParent(transform, false);
        child.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = child.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private void OnDestroy()
    {
        foreach (var pair in originalMaterials) if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
        foreach (var renderer in hiddenRenderers) if (renderer != null) renderer.enabled = true;
        foreach (var light in hiddenLights) if (light != null) light.enabled = true;
        foreach (var mesh in meshes) Destroy(mesh);
        foreach (var material in materials) Destroy(material);
        if (!initialized) return;
        RenderSettings.skybox = previousSkybox;
        RenderSettings.ambientMode = previousAmbient;
        RenderSettings.ambientSkyColor = previousSky;
        RenderSettings.ambientEquatorColor = previousEquator;
        RenderSettings.ambientGroundColor = previousGround;
    }
}
