using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Comprehensive setup script for hit detection on Remy and Adam avatars
/// This script automatically configures all necessary components for the hit system
///
/// SETUP INSTRUCTIONS:
/// 1. Create empty GameObject in scene called "HitSystemManager"
/// 2. Attach this script to it
/// 3. Assign Remy and Adam avatar references in Inspector
/// 4. Click "Setup Hit Detection" button in Inspector
/// 5. Optionally assign hit sound effects in SoundManager
/// 6. Play the game to test!
/// </summary>
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(CompleteHitDetectionSetup))]
public class CompleteHitDetectionSetupEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CompleteHitDetectionSetup setup = (CompleteHitDetectionSetup)target;

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.HelpBox(
            "Click the button below to automatically setup hit detection on Remy and Adam.\n\n" +
            "This will:\n" +
            "• Add colliders to body parts\n" +
            "• Setup hit detection triggers\n" +
            "• Configure visual indicators\n" +
            "• Create sound manager\n" +
            "• Setup hit feedback (screen flash + camera shake)",
            UnityEditor.MessageType.Info);

        if (GUILayout.Button("Setup Hit Detection", GUILayout.Height(40)))
        {
            setup.SetupHitDetection();
        }

        UnityEditor.EditorGUILayout.Space();

        if (GUILayout.Button("Remove Hit Detection", GUILayout.Height(30)))
        {
            if (UnityEditor.EditorUtility.DisplayDialog("Remove Hit Detection",
                "Are you sure you want to remove all hit detection components?", "Yes", "No"))
            {
                setup.RemoveHitDetection();
            }
        }
    }
}
#endif

public class CompleteHitDetectionSetup : MonoBehaviour
{
    [Header("Avatar References")]
    [Tooltip("Drag Remy avatar from hierarchy")]
    public GameObject remyAvatar;
    [Tooltip("Drag Adam avatar from hierarchy")]
    public GameObject adamAvatar;

    [Header("Hit Visual Prefabs")]
    [Tooltip("Prefab for UI hit indicator (circular image)")]
    public GameObject hitCircleUIPrefab;
    [Tooltip("Prefab for 3D world hit effect (sphere with material)")]
    public GameObject hitCircleWorldPrefab;
    [Tooltip("Prefab for critical hit effect")]
    public GameObject criticalHitPrefab;

    [Header("Setup Options")]
    public bool setupRemy = true;
    public bool setupAdam = true;
    public bool createVisualIndicators = true;
    public bool createSoundManager = true;

    [Header("Body Part Collider Settings")]
    public float headRadius = 0.15f;
    public Vector3 bodySize = new Vector3(0.4f, 0.6f, 0.3f);
    public float limbRadius = 0.1f;

    public void SetupHitDetection()
    {
        Debug.Log("=== Starting Hit Detection Setup ===");

        // Find or create SoundManager
        if (createSoundManager)
        {
            SetupSoundManager();
        }

        // Setup visual indicators
        if (createVisualIndicators)
        {
            SetupVisualIndicators();
        }

        // Setup avatars
        if (setupRemy && remyAvatar != null)
        {
            SetupAvatarHitDetection(remyAvatar, "Remy");
        }
        else if (setupRemy)
        {
            Debug.LogWarning("Remy avatar not assigned!");
        }

        if (setupAdam && adamAvatar != null)
        {
            SetupAvatarHitDetection(adamAvatar, "Adam");
        }
        else if (setupAdam)
        {
            Debug.LogWarning("Adam avatar not assigned!");
        }

        Debug.Log("=== Hit Detection Setup Complete! ===");
        Debug.Log("Test by making avatars collide or press 'T' key if HitFeedbackTester is active");
    }

    private void SetupSoundManager()
    {
        SoundManager existing = FindObjectOfType<SoundManager>();
        if (existing != null)
        {
            Debug.Log("✅ SoundManager already exists");
            return;
        }

        GameObject soundManagerObj = new GameObject("SoundManager");
        soundManagerObj.AddComponent<SoundManager>();
        Debug.Log("✅ Created SoundManager");
    }

    private void SetupVisualIndicators()
    {
        // Setup UI Hit Indicator
        LocalHitIndicator uiIndicator = FindObjectOfType<LocalHitIndicator>();
        if (uiIndicator == null)
        {
            GameObject indicatorObj = new GameObject("HitIndicatorSystem");
            uiIndicator = indicatorObj.AddComponent<LocalHitIndicator>();

            // Create canvas for UI hits
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("HitIndicatorCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            uiIndicator.hitIndicatorCanvas = canvas;

            // Create or assign UI prefab
            if (hitCircleUIPrefab != null)
            {
                uiIndicator.hitCircleUIPrefab = hitCircleUIPrefab;
            }
            else
            {
                uiIndicator.hitCircleUIPrefab = CreateDefaultUIHitPrefab(canvas);
            }

            Debug.Log("✅ Created LocalHitIndicator (UI)");
        }

        // Setup World Hit Indicator
        LocalWorldHitIndicator worldIndicator = FindObjectOfType<LocalWorldHitIndicator>();
        if (worldIndicator == null)
        {
            GameObject worldIndicatorObj = new GameObject("WorldHitIndicatorSystem");
            worldIndicator = worldIndicatorObj.AddComponent<LocalWorldHitIndicator>();

            // Create or assign world prefab
            if (hitCircleWorldPrefab != null)
            {
                worldIndicator.hitCirclePrefab = hitCircleWorldPrefab;
            }
            else
            {
                worldIndicator.hitCirclePrefab = CreateDefaultWorldHitPrefab();
            }

            if (criticalHitPrefab != null)
            {
                worldIndicator.criticalHitPrefab = criticalHitPrefab;
            }
            else
            {
                worldIndicator.criticalHitPrefab = CreateDefaultCriticalHitPrefab();
            }

            Debug.Log("✅ Created LocalWorldHitIndicator (3D)");
        }
    }

    private void SetupAvatarHitDetection(GameObject avatar, string avatarName)
    {
        Debug.Log($"Setting up hit detection for {avatarName}...");

        // Add HitFeedback component to avatar root
        HitFeedback hitFeedback = avatar.GetComponent<HitFeedback>();
        if (hitFeedback == null)
        {
            hitFeedback = avatar.AddComponent<HitFeedback>();
            Debug.Log($"  ✅ Added HitFeedback to {avatarName}");
        }

        // Setup body parts with colliders and hit detection
        SetupBodyPart(avatar, "Head", HitType.Head, headRadius);
        SetupBodyPart(avatar, "Spine", HitType.Body, bodySize, true);
        SetupBodyPart(avatar, "LeftUpperArm", HitType.Limb, limbRadius);
        SetupBodyPart(avatar, "RightUpperArm", HitType.Limb, limbRadius);
        SetupBodyPart(avatar, "LeftLowerArm", HitType.Limb, limbRadius);
        SetupBodyPart(avatar, "RightLowerArm", HitType.Limb, limbRadius);
        SetupBodyPart(avatar, "LeftUpperLeg", HitType.Limb, limbRadius);
        SetupBodyPart(avatar, "RightUpperLeg", HitType.Limb, limbRadius);
        SetupBodyPart(avatar, "LeftLowerLeg", HitType.Limb, limbRadius);
        SetupBodyPart(avatar, "RightLowerLeg", HitType.Limb, limbRadius);

        Debug.Log($"✅ {avatarName} hit detection setup complete!");
    }

    private void SetupBodyPart(GameObject avatar, string bodyPartName, HitType hitType, float radius)
    {
        SetupBodyPart(avatar, bodyPartName, hitType, Vector3.one * radius * 2f, false);
    }

    private void SetupBodyPart(GameObject avatar, string bodyPartName, HitType hitType, Vector3 size, bool isBox)
    {
        Transform bodyPart = FindInChildren(avatar.transform, bodyPartName);
        if (bodyPart == null)
        {
            Debug.LogWarning($"  ⚠️ Body part '{bodyPartName}' not found on {avatar.name}");
            return;
        }

        // Add collider if missing
        Collider col = bodyPart.GetComponent<Collider>();
        if (col == null)
        {
            if (isBox)
            {
                BoxCollider box = bodyPart.gameObject.AddComponent<BoxCollider>();
                box.size = size;
                box.isTrigger = true;
            }
            else
            {
                SphereCollider sphere = bodyPart.gameObject.AddComponent<SphereCollider>();
                sphere.radius = size.x / 2f;
                sphere.isTrigger = true;
            }
        }
        else
        {
            col.isTrigger = true;
        }

        // Add BodyPartIdentifier
        BodyPartIdentifier identifier = bodyPart.GetComponent<BodyPartIdentifier>();
        if (identifier == null)
        {
            identifier = bodyPart.gameObject.AddComponent<BodyPartIdentifier>();
        }
        identifier.hitType = hitType;

        // Add LocalHitTrigger
        LocalHitTrigger hitTrigger = bodyPart.GetComponent<LocalHitTrigger>();
        if (hitTrigger == null)
        {
            hitTrigger = bodyPart.gameObject.AddComponent<LocalHitTrigger>();
        }
        hitTrigger.bodyPartType = hitType;
        hitTrigger.attackerTag = avatar.CompareTag("Player") ? "Enemy" : "Player";

        Debug.Log($"  ✅ Setup {bodyPartName} as {hitType}");
    }

    private Transform FindInChildren(Transform parent, string name)
    {
        // Recursively search for child with name
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;

            Transform found = FindInChildren(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private GameObject CreateDefaultUIHitPrefab(Canvas parentCanvas)
    {
        GameObject prefab = new GameObject("HitCircleUI");
        prefab.transform.SetParent(parentCanvas.transform, false);

        Image image = prefab.AddComponent<Image>();
        image.color = Color.red;

        RectTransform rect = prefab.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 100);

        // Create circular sprite (approximately)
        prefab.SetActive(false);

        Debug.Log("  ✅ Created default UI hit prefab");
        return prefab;
    }

    private GameObject CreateDefaultWorldHitPrefab()
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        prefab.name = "HitCircleWorld";
        prefab.transform.localScale = Vector3.one * 0.5f;

        // Make it unlit and bright
        Renderer renderer = prefab.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = Color.red;
        renderer.material = mat;

        // Remove collider
        Destroy(prefab.GetComponent<Collider>());

        prefab.SetActive(false);

        Debug.Log("  ✅ Created default world hit prefab");
        return prefab;
    }

    private GameObject CreateDefaultCriticalHitPrefab()
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        prefab.name = "CriticalHitEffect";
        prefab.transform.localScale = Vector3.one * 0.7f;

        Renderer renderer = prefab.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = Color.white;
        renderer.material = mat;

        Destroy(prefab.GetComponent<Collider>());
        prefab.SetActive(false);

        Debug.Log("  ✅ Created default critical hit prefab");
        return prefab;
    }

    public void RemoveHitDetection()
    {
        Debug.Log("=== Removing Hit Detection ===");

        if (remyAvatar != null)
            RemoveHitComponentsFromAvatar(remyAvatar);

        if (adamAvatar != null)
            RemoveHitComponentsFromAvatar(adamAvatar);

        Debug.Log("=== Hit Detection Removed ===");
    }

    private void RemoveHitComponentsFromAvatar(GameObject avatar)
    {
        // Remove from root
        DestroyComponentIfExists<HitFeedback>(avatar);

        // Remove from all children
        LocalHitTrigger[] triggers = avatar.GetComponentsInChildren<LocalHitTrigger>();
        foreach (var trigger in triggers)
            DestroyImmediate(trigger);

        BodyPartIdentifier[] identifiers = avatar.GetComponentsInChildren<BodyPartIdentifier>();
        foreach (var identifier in identifiers)
            DestroyImmediate(identifier);

        Debug.Log($"Removed hit components from {avatar.name}");
    }

    private void DestroyComponentIfExists<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component != null)
            DestroyImmediate(component);
    }
}
