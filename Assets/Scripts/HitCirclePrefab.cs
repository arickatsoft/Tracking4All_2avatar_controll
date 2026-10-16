using UnityEngine;
using System.Collections;

public class HitCirclePrefab : MonoBehaviour
{
    [Header("Circle Settings")]
    public float circleSize = 1.0f;
    public Color circleColor = Color.red;
    
    [Header("Animation")]
    public bool animateOnStart = true;
    public float animationDuration = 1.0f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    private Renderer circleRenderer;
    private Material circleMaterial;
    
    void Start()
    {
        SetupCircle();
        
        if (animateOnStart)
        {
            StartCoroutine(AnimateCircle());
        }
    }
    
    void SetupCircle()
    {
        // Get or create renderer
        circleRenderer = GetComponent<Renderer>();
        if (circleRenderer == null)
        {
            // Add MeshRenderer and MeshFilter if not present
            gameObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            
            // Create a simple quad mesh for the circle
            meshFilter.mesh = CreateQuadMesh();
            circleRenderer = GetComponent<Renderer>();
        }
        
        // Create material if needed
        if (circleRenderer.material == null || circleRenderer.material.name.Contains("Default"))
        {
            circleMaterial = new Material(Shader.Find("Sprites/Default"));
            circleMaterial.color = circleColor;
            circleRenderer.material = circleMaterial;
        }
        else
        {
            circleMaterial = circleRenderer.material;
        }
        
        // Set initial scale
        transform.localScale = Vector3.one * circleSize;
    }
    
    private Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "HitCircleQuad";
        
        // Vertices for a quad
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };
        
        // UV coordinates
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        // Triangles
        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    private IEnumerator AnimateCircle()
    {
        float elapsedTime = 0f;
        Vector3 initialScale = Vector3.zero;
        Vector3 targetScale = Vector3.one * circleSize;
        Color initialColor = circleColor;
        
        // Set initial state
        transform.localScale = initialScale;
        
        while (elapsedTime < animationDuration)
        {
            float progress = elapsedTime / animationDuration;
            
            // Scale animation
            float scaleValue = scaleCurve.Evaluate(progress);
            transform.localScale = Vector3.Lerp(initialScale, targetScale, scaleValue);
            
            // Alpha animation
            float alphaValue = alphaCurve.Evaluate(progress);
            Color currentColor = initialColor;
            currentColor.a = alphaValue;
            
            if (circleMaterial != null)
            {
                circleMaterial.color = currentColor;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Animation complete - destroy the object
        Destroy(gameObject);
    }
    
    public void SetColor(Color newColor)
    {
        circleColor = newColor;
        if (circleMaterial != null)
        {
            circleMaterial.color = newColor;
        }
    }
    
    public void SetSize(float newSize)
    {
        circleSize = newSize;
        if (!animateOnStart)
        {
            transform.localScale = Vector3.one * circleSize;
        }
    }
}