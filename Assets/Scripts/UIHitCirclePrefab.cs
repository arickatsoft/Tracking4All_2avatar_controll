using UnityEngine;
using UnityEngine.UI;

public class UIHitCirclePrefab : MonoBehaviour
{
    [Header("UI Settings")]
    public Image circleImage;
    public float defaultSize = 100f;
    
    [Header("Circle Sprite")]
    public Sprite circleSprite; // Assign a circle sprite in inspector
    
    void Awake()
    {
        // Get Image component if not assigned
        if (circleImage == null)
        {
            circleImage = GetComponent<Image>();
        }
        
        // Set up the circle image
        if (circleImage != null)
        {
            // Create a simple circle sprite if none assigned
            if (circleSprite != null)
            {
                circleImage.sprite = circleSprite;
            }
            else
            {
                // Use UI/Default sprite (you may want to create a better one)
                circleImage.sprite = Resources.Load<Sprite>("UI/Skin/Knob");
            }
            
            // Set to preserve aspect ratio
            circleImage.preserveAspect = true;
            
            // Set initial size
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = Vector2.one * defaultSize;
            }
        }
    }
    
    public void SetColor(Color color)
    {
        if (circleImage != null)
        {
            circleImage.color = color;
        }
    }
    
    public void SetSize(float size)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = Vector2.one * size;
        }
    }
    
    public void SetAlpha(float alpha)
    {
        if (circleImage != null)
        {
            Color currentColor = circleImage.color;
            currentColor.a = alpha;
            circleImage.color = currentColor;
        }
    }
}
