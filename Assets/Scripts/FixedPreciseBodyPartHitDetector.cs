using UnityEngine;
using System.Collections.Generic;

public class FixedPreciseBodyPartHitDetector : MonoBehaviour
{
    [Header("🎯 FIXED PRECISE BODY PART HIT DETECTION")]
    [Space(10)]
    
    [Header("Character Setup")]
    public string characterName = "Adam_Big";
    public string attackerTag = "Enemy";
    public float heightOffset = 1f; // ADDED: To fix low positioning
    
    [Header("Body Part Colors")]
    public Color headColor = Color.red;
    public Color chestColor = new Color(1f, 0.5f, 0f); // Orange
    public Color stomachColor = Color.yellow;
    public Color armColor = Color.blue;
    public Color handColor = Color.cyan;
    public Color legColor = Color.green;
    public Color footColor = Color.magenta;
    
    [Header("Settings")]
    public bool enableDebugLogs = true;
    public bool showBodyPartNames = true;
    public float hitEffectDuration = 2f;
    
    private Dictionary<BodyPart, Color> bodyPartColors;
    private GameObject character;
    
    void Start()
    {
        SetupBodyPartColors();
        ForceSetupBodyParts();
    }
    
    public void ForceSetupBodyParts()
    {
        SetupBodyPartColors();
        SetupFixedBodyPartDetection();
    }
    
    void SetupBodyPartColors()
    {
        bodyPartColors = new Dictionary<BodyPart, Color>
        {
            { BodyPart.Head, headColor },
            { BodyPart.Chest, chestColor },
            { BodyPart.Stomach, stomachColor },
            { BodyPart.LeftArm, armColor },
            { BodyPart.RightArm, armColor },
            { BodyPart.LeftHand, handColor },
            { BodyPart.RightHand, handColor },
            { BodyPart.LeftLeg, legColor },
            { BodyPart.RightLeg, legColor },
            { BodyPart.LeftFoot, footColor },
            { BodyPart.RightFoot, footColor }
        };
    }
    
    void SetupFixedBodyPartDetection()
    {
        Debug.Log($"🎯 SETTING UP FIXED BODY PART DETECTION for {characterName}...");
        
        character = GameObject.Find(characterName);
        if (character == null)
        {
            Debug.LogError($"❌ {characterName} not found!");
            return;
        }
        
        // Remove any existing hit detection
        RemoveExistingHitDetection(character);
        
        // Create fixed positioned body part colliders
        CreateFixedBodyPartColliders(character);
        
        Debug.Log($"✅ Fixed body part detection setup complete for {characterName}!");
        Debug.Log($"📏 Height offset applied: +{heightOffset} (body parts moved higher)");
    }
    
    void RemoveExistingHitDetection(GameObject character)
    {
        Debug.Log($"   🗑️ Removing existing hit detection...");
        
        // Remove from character and all children
        Transform[] allTransforms = character.GetComponentsInChildren<Transform>();
        foreach (Transform t in allTransforms)
        {
            // Remove hit components
            Component[] hitComponents = t.GetComponents<Component>();
            foreach (Component comp in hitComponents)
            {
                if (comp == null) continue;
                string compName = comp.GetType().Name;
                if (compName.Contains("Hit") && compName != "FixedPreciseBodyPartHitDetector")
                {
                    DestroyImmediate(comp);
                }
            }
            
            // Remove existing colliders (we'll create new precise ones)
            Collider[] colliders = t.GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                DestroyImmediate(col);
            }
        }
    }
    
    void CreateFixedBodyPartColliders(GameObject character)
    {
        Debug.Log($"   🔲 Creating FIXED positioned body part colliders...");
        Debug.Log($"   📏 Applying height offset: +{heightOffset}");
        
        Vector3 basePos = character.transform.position;
        
        // FIXED POSITIONS - Much higher than before
        // HEAD - Top of character (FIXED: was 2.5, now higher)
        CreateBodyPartCollider(character, BodyPart.Head, 
            new Vector3(0, 3.0f + heightOffset, 0), 0.3f, 0.4f);
        
        // CHEST - Upper torso (FIXED: was 2.0, now higher)
        CreateBodyPartCollider(character, BodyPart.Chest, 
            new Vector3(0, 2.4f + heightOffset, 0), 0.4f, 0.6f);
        
        // STOMACH - Lower torso (FIXED: was 1.4, now higher)
        CreateBodyPartCollider(character, BodyPart.Stomach, 
            new Vector3(0, 1.8f + heightOffset, 0), 0.35f, 0.5f);
        
        // ARMS (FIXED: was 1.8, now higher)
        CreateBodyPartCollider(character, BodyPart.LeftArm, 
            new Vector3(-0.6f, 2.2f + heightOffset, 0), 0.15f, 0.6f);
        CreateBodyPartCollider(character, BodyPart.RightArm, 
            new Vector3(0.6f, 2.2f + heightOffset, 0), 0.15f, 0.6f);
        
        // HANDS (FIXED: was 1.2, now higher)
        CreateBodyPartCollider(character, BodyPart.LeftHand, 
            new Vector3(-0.8f, 1.6f + heightOffset, 0), 0.1f, 0.2f);
        CreateBodyPartCollider(character, BodyPart.RightHand, 
            new Vector3(0.8f, 1.6f + heightOffset, 0), 0.1f, 0.2f);
        
        // LEGS (FIXED: was 0.8, now higher)
        CreateBodyPartCollider(character, BodyPart.LeftLeg, 
            new Vector3(-0.2f, 1.2f + heightOffset, 0), 0.15f, 0.8f);
        CreateBodyPartCollider(character, BodyPart.RightLeg, 
            new Vector3(0.2f, 1.2f + heightOffset, 0), 0.15f, 0.8f);
        
        // FEET (Keep these at ground level - don't add height offset)
        CreateBodyPartCollider(character, BodyPart.LeftFoot, 
            new Vector3(-0.2f, 0.1f, 0.1f), 0.12f, 0.3f);
        CreateBodyPartCollider(character, BodyPart.RightFoot, 
            new Vector3(0.2f, 0.1f, 0.1f), 0.12f, 0.3f);
    }
    
    void CreateBodyPartCollider(GameObject character, BodyPart bodyPart, Vector3 localPosition, float radius, float height)
    {
        // Create a child GameObject for this body part
        GameObject bodyPartObj = new GameObject($"{bodyPart}_Collider");
        bodyPartObj.transform.SetParent(character.transform);
        bodyPartObj.transform.localPosition = localPosition;
        
        // Add collider
        CapsuleCollider collider = bodyPartObj.AddComponent<CapsuleCollider>();
        collider.radius = radius;
        collider.height = height;
        collider.isTrigger = true;
        
        // Add body part hit component
        FixedBodyPartHitComponent hitComponent = bodyPartObj.AddComponent<FixedBodyPartHitComponent>();
        hitComponent.bodyPart = bodyPart;
        hitComponent.attackerTag = attackerTag;
        hitComponent.parentDetector = this;
        
        if (enableDebugLogs)
        {
            Debug.Log($"      ✅ Created {bodyPart} collider at {localPosition} (r:{radius}, h:{height})");
        }
    }
    
    public void OnBodyPartHit(BodyPart bodyPart, Vector3 hitPosition, string attackerName)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 ✅ FIXED PRECISE HIT DETECTED!");
            Debug.Log($"   📍 Body Part: {bodyPart}");
            Debug.Log($"   👊 Attacker: {attackerName}");
            Debug.Log($"   🎯 Target: {characterName}");
            Debug.Log($"   📍 Hit Position: {hitPosition}");
            Debug.Log($"   📏 Height from ground: {hitPosition.y}");
        }
        
        // Show precise hit effect
        ShowFixedHitEffect(bodyPart, hitPosition);
        
        // Call damage calculation based on body part
        CalculateBodyPartDamage(bodyPart);
    }
    
    void ShowFixedHitEffect(BodyPart bodyPart, Vector3 position)
    {
        Color hitColor = bodyPartColors.ContainsKey(bodyPart) ? bodyPartColors[bodyPart] : Color.white;
        
        StartCoroutine(CreateFixedHitEffect(bodyPart, position, hitColor));
        
        // Also use SimpleHitIndicator if available
        if (SimpleHitIndicator.Instance != null)
        {
            SimpleHitIndicator.Instance.ShowHitAtPosition(position);
        }
    }
    
    System.Collections.IEnumerator CreateFixedHitEffect(BodyPart bodyPart, Vector3 position, Color color)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = $"FIXED_{bodyPart}_Hit";
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 0.4f; // Slightly larger for visibility
        
        // Set body part color
        Renderer renderer = effect.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        renderer.material = mat;
        
        // Remove collider
        Destroy(effect.GetComponent<Collider>());
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎨 Created FIXED {bodyPart} hit effect at {position} (Color: {color})");
        }
        
        // Create floating text if enabled
        if (showBodyPartNames)
        {
            CreateFloatingBodyPartText(bodyPart, position + Vector3.up * 0.5f);
        }
        
        // Animate with better visibility
        float elapsed = 0f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 targetScale = startScale * GetBodyPartEffectSize(bodyPart);
        
        while (elapsed < hitEffectDuration && effect != null)
        {
            float progress = elapsed / hitEffectDuration;
            effect.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            
            // Fade more slowly for better visibility
            Color currentColor = color;
            currentColor.a = (1f - progress * 0.7f) * 0.9f; // Slower fade
            mat.color = currentColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (effect != null)
        {
            Destroy(effect);
        }
    }
    
    float GetBodyPartEffectSize(BodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case BodyPart.Head: return 4f; // Larger for critical hits
            case BodyPart.Chest: return 3.5f;
            case BodyPart.Stomach: return 3f;
            case BodyPart.LeftArm:
            case BodyPart.RightArm: return 2.5f;
            case BodyPart.LeftHand:
            case BodyPart.RightHand: return 2f;
            case BodyPart.LeftLeg:
            case BodyPart.RightLeg: return 2.5f;
            case BodyPart.LeftFoot:
            case BodyPart.RightFoot: return 2f;
            default: return 3f;
        }
    }
    
    void CreateFloatingBodyPartText(BodyPart bodyPart, Vector3 position)
    {
        // Create a simple text effect (using a scaled cube as placeholder)
        GameObject textObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        textObj.name = $"{bodyPart}_Text";
        textObj.transform.position = position;
        textObj.transform.localScale = Vector3.one * 0.15f; // Larger for visibility
        
        // Make it bright yellow for visibility
        Renderer renderer = textObj.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.yellow;
        renderer.material = mat;
        
        // Remove collider
        Destroy(textObj.GetComponent<Collider>());
        
        StartCoroutine(AnimateFloatingText(textObj, bodyPart.ToString()));
    }
    
    System.Collections.IEnumerator AnimateFloatingText(GameObject textObj, string text)
    {
        Vector3 startPos = textObj.transform.position;
        Vector3 endPos = startPos + Vector3.up * 1.5f; // Float higher
        
        float duration = 3f; // Longer duration
        float elapsed = 0f;
        
        if (enableDebugLogs)
        {
            Debug.Log($"📝 Floating text: {text}");
        }
        
        while (elapsed < duration && textObj != null)
        {
            float progress = elapsed / duration;
            textObj.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (textObj != null)
        {
            Destroy(textObj);
        }
    }
    
    void CalculateBodyPartDamage(BodyPart bodyPart)
    {
        float damageMultiplier = GetBodyPartDamageMultiplier(bodyPart);
        float baseDamage = 10f;
        float finalDamage = baseDamage * damageMultiplier;
        
        if (enableDebugLogs)
        {
            Debug.Log($"💥 DAMAGE CALCULATION:");
            Debug.Log($"   🎯 Body Part: {bodyPart}");
            Debug.Log($"   📊 Multiplier: {damageMultiplier}x");
            Debug.Log($"   💥 Final Damage: {finalDamage}");
        }
    }
    
    float GetBodyPartDamageMultiplier(BodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case BodyPart.Head: return 3.0f; // Critical damage
            case BodyPart.Chest: return 1.5f; // High damage
            case BodyPart.Stomach: return 1.2f; // Medium-high damage
            case BodyPart.LeftArm:
            case BodyPart.RightArm: return 0.8f; // Reduced damage
            case BodyPart.LeftHand:
            case BodyPart.RightHand: return 0.5f; // Low damage
            case BodyPart.LeftLeg:
            case BodyPart.RightLeg: return 0.9f; // Medium damage
            case BodyPart.LeftFoot:
            case BodyPart.RightFoot: return 0.6f; // Low damage
            default: return 1.0f; // Normal damage
        }
    }
    
    [ContextMenu("🧪 Test All Body Parts")]
    public void TestAllBodyParts()
    {
        Debug.Log($"🧪 TESTING ALL BODY PARTS for {characterName}...");
        
        if (character == null)
        {
            character = GameObject.Find(characterName);
        }
        
        if (character == null) 
        {
            Debug.LogError($"❌ {characterName} not found!");
            return;
        }
        
        Vector3 basePos = character.transform.position;
        
        // Test each body part with FIXED positions
        foreach (BodyPart bodyPart in System.Enum.GetValues(typeof(BodyPart)))
        {
            Vector3 testPos = GetFixedBodyPartTestPosition(basePos, bodyPart);
            OnBodyPartHit(bodyPart, testPos, "TEST");
        }
        
        Debug.Log($"✅ All body parts tested for {characterName}! Check for colored circles:");
        Debug.Log("   🔴 RED = Head, 🟠 ORANGE = Chest, 🟡 YELLOW = Stomach");
        Debug.Log("   🔵 BLUE = Arms, 🟦 CYAN = Hands, 🟢 GREEN = Legs, 🟣 MAGENTA = Feet");
        Debug.Log($"   📏 All positions adjusted +{heightOffset} higher");
    }
    
    Vector3 GetFixedBodyPartTestPosition(Vector3 basePos, BodyPart bodyPart)
    {
        // FIXED POSITIONS - Much higher than before
        switch (bodyPart)
        {
            case BodyPart.Head: return basePos + new Vector3(0, 3.0f + heightOffset, 0);
            case BodyPart.Chest: return basePos + new Vector3(0, 2.4f + heightOffset, 0);
            case BodyPart.Stomach: return basePos + new Vector3(0, 1.8f + heightOffset, 0);
            case BodyPart.LeftArm: return basePos + new Vector3(-0.6f, 2.2f + heightOffset, 0);
            case BodyPart.RightArm: return basePos + new Vector3(0.6f, 2.2f + heightOffset, 0);
            case BodyPart.LeftHand: return basePos + new Vector3(-0.8f, 1.6f + heightOffset, 0);
            case BodyPart.RightHand: return basePos + new Vector3(0.8f, 1.6f + heightOffset, 0);
            case BodyPart.LeftLeg: return basePos + new Vector3(-0.2f, 1.2f + heightOffset, 0);
            case BodyPart.RightLeg: return basePos + new Vector3(0.2f, 1.2f + heightOffset, 0);
            case BodyPart.LeftFoot: return basePos + new Vector3(-0.2f, 0.1f, 0);
            case BodyPart.RightFoot: return basePos + new Vector3(0.2f, 0.1f, 0);
            default: return basePos + Vector3.up * (2f + heightOffset);
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(420, 500, 350, 100), $"🎯 FIXED PRECISE HITS - {characterName}");
        
        if (GUI.Button(new Rect(430, 525, 120, 25), "🧪 Test All Parts"))
        {
            TestAllBodyParts();
        }
        
        GUI.Label(new Rect(430, 555, 330, 20), $"📏 Height offset: +{heightOffset} (body parts higher)");
        GUI.Label(new Rect(430, 575, 330, 20), $"👀 Watching for: {attackerTag} hitting {characterName}");
    }
}

// FIXED Component that goes on each body part collider
public class FixedBodyPartHitComponent : MonoBehaviour
{
    [Header("🎯 FIXED BODY PART HIT COMPONENT")]
    public BodyPart bodyPart;
    public string attackerTag = "Player";
    public FixedPreciseBodyPartHitDetector parentDetector;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(attackerTag) && parentDetector != null)
        {
            Vector3 hitPosition = transform.position;
            parentDetector.OnBodyPartHit(bodyPart, hitPosition, other.name);
        }
    }
}
