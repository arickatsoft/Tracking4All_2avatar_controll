using UnityEngine;

public class LocalHitSystemSetup : MonoBehaviour
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
    
    [ContextMenu("Setup Local Hit System")]
    public void SetupHitSystem()
    {
        Debug.Log("Setting up Local Hit System (No Networking)...");
        
        // 1. Setup Adam's hit detection
        SetupCharacterHitDetection("Adam_Big", "Player");
        
        // 2. Setup Remy's hit detection  
        SetupCharacterHitDetection("Remy", "Enemy");
        
        // 3. Create hit indicator prefabs if they don't exist
        CreateHitIndicatorPrefabs();
        
        // 4. Link hit indicator systems
        LinkHitIndicatorSystems();
        
        Debug.Log("Local Hit System setup complete!");
        Debug.Log("✅ Now you'll see circular hit indicators instead of screen flashes!");
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
        
        // Add local hit trigger if not present (remove any networked ones)
        LocalHitTrigger hitTrigger = character.GetComponent<LocalHitTrigger>();
        if (hitTrigger == null)
        {
            hitTrigger = character.AddComponent<LocalHitTrigger>();
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
            
            // Make it a prefab (move off-screen and deactivate)
            circle.transform.position = new Vector3(100, 0, 0);
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
        // Find or create hit indicator systems
        LocalHitIndicator uiIndicator = FindObjectOfType<LocalHitIndicator>();
        if (uiIndicator == null)
        {
            GameObject uiManager = new GameObject("LocalUIHitIndicator");
            uiIndicator = uiManager.AddComponent<LocalHitIndicator>();
        }
        
        LocalWorldHitIndicator worldIndicator = FindObjectOfType<LocalWorldHitIndicator>();
        if (worldIndicator == null)
        {
            GameObject worldManager = new GameObject("LocalWorldHitIndicator");
            worldIndicator = worldManager.AddComponent<LocalWorldHitIndicator>();
        }
        
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
