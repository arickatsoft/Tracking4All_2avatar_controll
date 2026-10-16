// NETWORKING SCRIPT - DISABLED FOR LOCAL PLAY
// Uncomment and use this when you add Unity Netcode packages back

/*
using UnityEngine;
using Unity.Netcode;

public class HitSystemSetup : MonoBehaviour
{
    [Header("Setup Options")]
    public bool setupOnStart = true;
    
    [Header("Prefab References")]
    public GameObject hitCirclePrefab;
    public GameObject uiHitCirclePrefab;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupHitSystem();
        }
    }
    
    [ContextMenu("Setup Hit System")]
    public void SetupHitSystem()
    {
        Debug.Log("Setting up Enhanced Hit System...");
        
        // 1. Setup Adam's hit detection
        SetupCharacterHitDetection("Adam_Big", "Player");
        
        // 2. Setup Remy's hit detection
        SetupCharacterHitDetection("Remy", "Enemy");
        
        // 3. Create hit indicator prefabs if they don't exist
        CreateHitIndicatorPrefabs();
        
        // 4. Link hit indicator systems
        LinkHitIndicatorSystems();
        
        Debug.Log("Enhanced Hit System setup complete!");
    }
    
    private void SetupCharacterHitDetection(string characterName, string attackerTag)
    {
        GameObject character = GameObject.Find(characterName);
        if (character == null)
        {
            Debug.LogWarning($"Character '{characterName}' not found!");
            return;
        }
        
        Debug.Log($"Setting up hit detection for {characterName}");
        
        // Add body part identifier if not present
        BodyPartIdentifier bodyPart = character.GetComponent<BodyPartIdentifier>();
        if (bodyPart == null)
        {
            bodyPart = character.AddComponent<BodyPartIdentifier>();
            bodyPart.hitType = HitType.Body;
        }
        
        // Add enhanced hit trigger if not present
        EnhancedNetworkHitTrigger hitTrigger = character.GetComponent<EnhancedNetworkHitTrigger>();
        if (hitTrigger == null)
        {
            hitTrigger = character.AddComponent<EnhancedNetworkHitTrigger>();
            hitTrigger.attackerTag = attackerTag;
            hitTrigger.bodyPartType = HitType.Body;
            hitTrigger.baseDamage = 25f;
        }
        
        // Ensure character has a collider for hit detection
        Collider col = character.GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = character.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.height = 2.0f;
            capsule.radius = 0.5f;
        }
        else
        {
            col.isTrigger = true;
        }
    }
    
    private void CreateHitIndicatorPrefabs()
    {
        if (hitCirclePrefab == null)
        {
            // Create a simple 3D hit circle prefab
            GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            circle.name = "HitCirclePrefab";
            
            // Add the HitCirclePrefab component
            HitCirclePrefab hitCircleComponent = circle.AddComponent<HitCirclePrefab>();
            hitCircleComponent.circleColor = Color.red;
            hitCircleComponent.circleSize = 1.0f;
            
            // Make it a prefab (move to Resources or a prefab folder)
            circle.transform.position = new Vector3(100, 0, 0); // Move off-screen
            circle.SetActive(false);
            
            hitCirclePrefab = circle;
            Debug.Log("Created 3D hit circle prefab");
        }
        
        if (uiHitCirclePrefab == null)
        {
            // Create UI hit circle prefab
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                GameObject uiCircle = new GameObject("UIHitCirclePrefab");
                uiCircle.transform.SetParent(canvas.transform, false);
                
                // Add Image component
                UnityEngine.UI.Image image = uiCircle.AddComponent<UnityEngine.UI.Image>();
                image.color = Color.red;
                
                // Add UIHitCirclePrefab component
                UIHitCirclePrefab uiComponent = uiCircle.AddComponent<UIHitCirclePrefab>();
                uiComponent.circleImage = image;
                uiComponent.defaultSize = 100f;
                
                // Set up RectTransform
                RectTransform rectTransform = uiCircle.GetComponent<RectTransform>();
                rectTransform.sizeDelta = Vector2.one * 100f;
                
                uiCircle.SetActive(false);
                uiHitCirclePrefab = uiCircle;
                Debug.Log("Created UI hit circle prefab");
            }
        }
    }
    
    private void LinkHitIndicatorSystems()
    {
        // Find hit indicator systems
        UIHitIndicator uiIndicator = FindObjectOfType<UIHitIndicator>();
        NetworkedHitIndicator worldIndicator = FindObjectOfType<NetworkedHitIndicator>();
        
        if (uiIndicator != null)
        {
            // Link UI prefab
            uiIndicator.hitCircleUIPrefab = uiHitCirclePrefab;
            
            // Find or create canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("HitIndicatorCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            uiIndicator.hitIndicatorCanvas = canvas;
        }
        
        if (worldIndicator != null)
        {
            // Link 3D prefabs
            worldIndicator.hitCirclePrefab = hitCirclePrefab;
            worldIndicator.criticalHitPrefab = hitCirclePrefab; // Use same for now
        }
        
        Debug.Log("Hit indicator systems linked successfully");
    }
}
*/

// FOR NOW: Use LocalHitSystemSetup.cs instead!