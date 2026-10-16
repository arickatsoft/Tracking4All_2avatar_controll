using UnityEngine;
using UnityEngine.UI;

public class SimpleHitIndicator : MonoBehaviour
{
    [Header("Hit Circle Settings")]
    public Color hitColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange with opacity
    public float circleSize = 2.0f;
    public float duration = 1.0f;
    
    [Header("Animation")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.1f, 1, 1);
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    public static SimpleHitIndicator Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void ShowHitAtPosition(Vector3 worldPosition)
    {
        StartCoroutine(CreateHitCircle(worldPosition));
    }
    
    private System.Collections.IEnumerator CreateHitCircle(Vector3 worldPosition)
    {
        // Create a simple quad for the hit circle
        GameObject hitCircle = GameObject.CreatePrimitive(PrimitiveType.Quad);
        hitCircle.name = "HitIndicator";
        
        // Position at hit location
        hitCircle.transform.position = worldPosition;
        hitCircle.transform.localScale = Vector3.zero;
        
        // Create material with orange color and transparency
        Renderer renderer = hitCircle.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.color = hitColor;
        material.SetFloat("_Mode", 3); // Set to transparent mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        
        renderer.material = material;
        
        // Remove collider to avoid interference
        Collider collider = hitCircle.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        
        // Animate the circle
        float elapsedTime = 0f;
        Vector3 targetScale = Vector3.one * circleSize;
        Color initialColor = hitColor;
        
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            
            // Scale animation
            float scaleValue = scaleCurve.Evaluate(progress);
            hitCircle.transform.localScale = targetScale * scaleValue;
            
            // Fade animation
            float alphaValue = fadeCurve.Evaluate(progress);
            Color currentColor = initialColor;
            currentColor.a = initialColor.a * alphaValue;
            material.color = currentColor;
            
            // Make circle face camera
            if (Camera.main != null)
            {
                hitCircle.transform.LookAt(Camera.main.transform);
                hitCircle.transform.Rotate(0, 180, 0);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Clean up
        Destroy(hitCircle);
    }
}
