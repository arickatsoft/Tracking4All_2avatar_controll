using UnityEngine;

/// <summary>
/// Diagnostic tool to check if hit detection is properly configured
/// Add this to any GameObject and check the console for the report
/// </summary>
public class HitDetectionDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("========================================");
        Debug.Log("🔍 HIT DETECTION DIAGNOSTIC REPORT");
        Debug.Log("========================================");

        // Check for Remy and Adam
        GameObject remy = GameObject.Find("Remy");
        GameObject adam = GameObject.Find("Adam_Big");

        if (remy != null)
        {
            Debug.Log("\n✅ Found Remy avatar");
            CheckAvatarSetup(remy, "Remy");
        }
        else
        {
            Debug.LogError("❌ Remy avatar not found!");
        }

        if (adam != null)
        {
            Debug.Log("\n✅ Found Adam avatar");
            CheckAvatarSetup(adam, "Adam");
        }
        else
        {
            Debug.LogError("❌ Adam avatar not found!");
        }

        // Check for global components
        CheckGlobalComponents();

        Debug.Log("\n========================================");
        Debug.Log("🔍 DIAGNOSTIC REPORT COMPLETE");
        Debug.Log("========================================\n");
    }

    void CheckAvatarSetup(GameObject avatar, string name)
    {
        // Check avatar root components
        Debug.Log($"\n--- {name} Root Components ---");

        HitFeedback hitFeedback = avatar.GetComponent<HitFeedback>();
        if (hitFeedback != null)
        {
            Debug.Log($"  ✅ HitFeedback component found");
            if (hitFeedback.cameraRig == null)
                Debug.LogWarning($"  ⚠️ HitFeedback.cameraRig is not assigned!");
            if (hitFeedback.hitFlashImage == null)
                Debug.LogWarning($"  ⚠️ HitFeedback.hitFlashImage is not assigned!");
        }
        else
        {
            Debug.LogWarning($"  ⚠️ HitFeedback component NOT found on {name}");
        }

        // Check for Rigidbody (needed for collision detection)
        Rigidbody rb = avatar.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"  ✅ Rigidbody found (isKinematic: {rb.isKinematic})");
        }
        else
        {
            Debug.LogWarning($"  ⚠️ Rigidbody NOT found on {name} - collisions may not work!");
        }

        // Check body parts
        Debug.Log($"\n--- {name} Body Parts ---");
        LocalHitTrigger[] hitTriggers = avatar.GetComponentsInChildren<LocalHitTrigger>();
        BodyPartIdentifier[] bodyParts = avatar.GetComponentsInChildren<BodyPartIdentifier>();
        Collider[] colliders = avatar.GetComponentsInChildren<Collider>();

        Debug.Log($"  LocalHitTrigger components: {hitTriggers.Length}");
        Debug.Log($"  BodyPartIdentifier components: {bodyParts.Length}");
        Debug.Log($"  Colliders: {colliders.Length}");

        if (hitTriggers.Length == 0)
        {
            Debug.LogError($"  ❌ NO LocalHitTrigger components found on {name}!");
            Debug.LogError($"     You need to add hit detection to body parts!");
        }
        else
        {
            Debug.Log($"  ✅ Found {hitTriggers.Length} hit triggers");
            foreach (var trigger in hitTriggers)
            {
                Debug.Log($"     • {trigger.gameObject.name} (Type: {trigger.bodyPartType}, Tag: {trigger.attackerTag})");
            }
        }

        if (colliders.Length == 0)
        {
            Debug.LogError($"  ❌ NO Colliders found on {name}!");
        }
        else
        {
            int triggerCount = 0;
            int normalCount = 0;
            foreach (var col in colliders)
            {
                if (col.isTrigger) triggerCount++;
                else normalCount++;
            }
            Debug.Log($"  ✅ Colliders: {triggerCount} triggers, {normalCount} normal");

            if (triggerCount == 0)
            {
                Debug.LogWarning($"  ⚠️ No trigger colliders found! Set colliders to isTrigger=true");
            }
        }

        // Check tags
        Debug.Log($"\n--- {name} Tags ---");
        Debug.Log($"  Avatar tag: {avatar.tag}");
        if (avatar.tag != "Player" && avatar.tag != "Enemy")
        {
            Debug.LogWarning($"  ⚠️ Avatar should have 'Player' or 'Enemy' tag!");
        }
        else
        {
            Debug.Log($"  ✅ Avatar tag is correct");
        }
    }

    void CheckGlobalComponents()
    {
        Debug.Log("\n--- Global Components ---");

        // Check SoundManager
        SoundManager soundManager = FindObjectOfType<SoundManager>();
        if (soundManager != null)
        {
            Debug.Log("  ✅ SoundManager found");
        }
        else
        {
            Debug.LogWarning("  ⚠️ SoundManager NOT found - no hit sounds will play!");
            Debug.LogWarning("     Create a GameObject with SoundManager component");
        }

        // Check LocalHitIndicator
        LocalHitIndicator hitIndicator = FindObjectOfType<LocalHitIndicator>();
        if (hitIndicator != null)
        {
            Debug.Log("  ✅ LocalHitIndicator found");
            if (hitIndicator.hitIndicatorCanvas == null)
                Debug.LogWarning("  ⚠️ LocalHitIndicator.hitIndicatorCanvas not assigned!");
            if (hitIndicator.hitCircleUIPrefab == null)
                Debug.LogWarning("  ⚠️ LocalHitIndicator.hitCircleUIPrefab not assigned!");
        }
        else
        {
            Debug.LogWarning("  ⚠️ LocalHitIndicator NOT found - no UI hit effects!");
        }

        // Check LocalWorldHitIndicator
        LocalWorldHitIndicator worldIndicator = FindObjectOfType<LocalWorldHitIndicator>();
        if (worldIndicator != null)
        {
            Debug.Log("  ✅ LocalWorldHitIndicator found");
            if (worldIndicator.hitCirclePrefab == null)
                Debug.LogWarning("  ⚠️ LocalWorldHitIndicator.hitCirclePrefab not assigned!");
        }
        else
        {
            Debug.LogWarning("  ⚠️ LocalWorldHitIndicator NOT found - no 3D hit effects!");
        }

        // Check Physics settings
        Debug.Log("\n--- Physics Settings ---");
        Debug.Log("  ⚠️ Check Edit → Project Settings → Physics");
        Debug.Log("     Verify layer collision matrix allows collisions between avatars");
    }
}
