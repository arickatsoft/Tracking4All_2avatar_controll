using System.Collections.Generic;
using UnityEngine;

namespace FitFight.Practice
{
    /// <summary>Dynamic foreground for a fixed gym image: leather bag, lighting, and a transparent shadow floor.</summary>
    public sealed class PracticeGym : MonoBehaviour
    {
        public PracticeHeavyBag Bag { get; private set; }
        private Mesh leatherMesh;
        public static Material Material(Color color, float metal = 0)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color; material.SetFloat("_Metallic", metal);
            material.SetFloat("_Glossiness", metal > 0 ? .65f : .40f);
            return material;
        }
        public void Build(float height, float reach)
        {
            float s = height / 1.7f;
            var leather = Material(new Color(.50f, .055f, .07f));
            var binding = Material(new Color(.13f, .065f, .065f));
            var metal = Material(new Color(.65f, .65f, .65f), .8f);
            var floor = Shape("Transparent shadow floor", PrimitiveType.Plane, Vector3.zero, Vector3.one * 3,
                new Material(Resources.Load<Shader>("PracticeShadow")), transform);
            floor.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var bagObject = new GameObject("Red heavy bag");
            bagObject.transform.SetParent(transform, false);
            bagObject.transform.localPosition = new Vector3(0, 1.09f * s, reach);
            Bag = bagObject.AddComponent<PracticeHeavyBag>(); Bag.Configure(height);
            var body = new GameObject("Rounded leather heavy bag", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(bagObject.transform, false);
            body.transform.localScale = Vector3.one * s;
            leatherMesh = RoundedBagMesh();
            body.GetComponent<MeshFilter>().sharedMesh = leatherMesh;
            body.GetComponent<MeshRenderer>().sharedMaterial = leather;
            foreach (float y in new[] { -.50f, .50f })
                Shape("Stitched leather binding", PrimitiveType.Cylinder, new Vector3(0, y, 0) * s,
                    new Vector3(.402f, .009f, .402f) * s, binding, bagObject.transform);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * .5f;
                Vector3 position = new Vector3(Mathf.Sin(a) * .199f, .47f, Mathf.Cos(a) * .199f) * s;
                var tab = Shape("Leather suspension tab", PrimitiveType.Cube, position, new Vector3(.047f, .09f, .008f) * s, binding, bagObject.transform);
                tab.transform.localRotation = Quaternion.Euler(0, i * 90, 0);
                // Small steel buckle, rather than oversized black rectangles.
                foreach (float dy in new[] { -.027f, .027f })
                {
                    var buckle = Shape("Steel buckle", PrimitiveType.Cube, position + Vector3.up * dy * s,
                        new Vector3(.048f, .004f, .014f) * s, metal, bagObject.transform);
                    buckle.transform.localRotation = tab.transform.localRotation;
                }
            }
            var key = new GameObject("Neutral overhead gym key").AddComponent<Light>();
            key.transform.SetParent(transform, false); key.type = LightType.Directional;
            key.transform.localRotation = Quaternion.Euler(48, -125, 0);
            key.intensity = 1.05f; key.color = new Color(1, .98f, .95f);
            key.shadows = LightShadows.Soft; key.shadowStrength = .85f; key.shadowBias = .025f;
            var fill = new GameObject("Neutral front fill").AddComponent<Light>();
            fill.transform.SetParent(transform, false); fill.type = LightType.Point;
            fill.transform.localPosition = new Vector3(2, 2.2f, -2) * s;
            fill.range = 9; fill.intensity = 1.05f; fill.color = Color.white;
        }
        private static Mesh RoundedBagMesh()
        {
            const int segments = 48;
            Vector2[] profile = { new Vector2(-.61f, 0), new Vector2(-.604f, .10f), new Vector2(-.585f, .18f),
                new Vector2(-.55f, .225f), new Vector2(-.50f, .237f), new Vector2(.48f, .237f),
                new Vector2(.535f, .222f), new Vector2(.575f, .18f), new Vector2(.60f, .10f), new Vector2(.61f, 0) };
            var vertices = new Vector3[profile.Length * (segments + 1)];
            var uv = new Vector2[vertices.Length]; var triangles = new int[(profile.Length - 1) * segments * 6];
            for (int row = 0; row < profile.Length; row++) for (int j = 0; j <= segments; j++)
            {
                int index = row * (segments + 1) + j; float a = j * Mathf.PI * 2 / segments;
                vertices[index] = new Vector3(Mathf.Sin(a) * profile[row].y * .84f, profile[row].x, Mathf.Cos(a) * profile[row].y * .84f);
                uv[index] = new Vector2(j / (float)segments, (profile[row].x + .61f) / 1.22f);
                if (row == profile.Length - 1 || j == segments) continue;
                int t = (row * segments + j) * 6, next = index + segments + 1;
                triangles[t] = index; triangles[t + 1] = index + 1; triangles[t + 2] = next;
                triangles[t + 3] = index + 1; triangles[t + 4] = next + 1; triangles[t + 5] = next;
            }
            var mesh = new Mesh { name = "Smooth leather bag" }; mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private static GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            var item = GameObject.CreatePrimitive(type); item.name = name;
            item.transform.SetParent(parent, false); item.transform.localPosition = position; item.transform.localScale = size;
            item.GetComponent<Renderer>().sharedMaterial = material;
            var collider = item.GetComponent<Collider>(); collider.enabled = false; Destroy(collider); return item;
        }
        private void OnDestroy()
        {
            var owned = new HashSet<Material>();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                if (!(renderer is ParticleSystemRenderer)) foreach (var material in renderer.sharedMaterials) if (material != null) owned.Add(material);
            foreach (var material in owned) Destroy(material);
            if (leatherMesh != null) Destroy(leatherMesh);
        }
    }
}
