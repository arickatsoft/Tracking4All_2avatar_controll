using UnityEngine;

namespace FitFight.Practice
{
    /// <summary>Camera-locked image plate. Foreground meshes and contact shadows render on top.</summary>
    public sealed class PracticeBackdrop : MonoBehaviour
    {
        private Camera view;
        private Transform plate;
        private Material material;
        private Texture texture;
        private void Awake()
        {
            view = GetComponent<Camera>();
            texture = Resources.Load<Texture2D>("PracticeGymBackdrop");
            var surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "Fixed gym image";
            plate = surface.transform; plate.SetParent(transform, false);
            var collider = surface.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            material = new Material(Resources.Load<Shader>("PracticeBackdrop")); material.mainTexture = texture;
            var renderer = surface.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        }
        private void OnPreCull()
        {
            const float depth = 25;
            float height = 2 * depth * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
            plate.localPosition = Vector3.forward * depth;
            plate.localScale = new Vector3(height * view.aspect, height, 1);
            float imageAspect = texture.width / (float)texture.height;
            Vector2 tiling = view.aspect < imageAspect ? new Vector2(view.aspect / imageAspect, 1) : new Vector2(1, imageAspect / view.aspect);
            material.mainTextureScale = tiling; material.mainTextureOffset = (Vector2.one - tiling) * .5f;
        }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
