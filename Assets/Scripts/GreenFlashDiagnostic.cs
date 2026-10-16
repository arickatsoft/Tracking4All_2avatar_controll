using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diagnostic tool for GreenFlashHorizontalDetector setup
/// Add to any GameObject and press Play to see what's configured
/// </summary>
public class GreenFlashDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("========================================");
        Debug.Log("🟢 GREEN FLASH DIAGNOSTIC REPORT");
        Debug.Log("========================================");

        // Find GreenFlashHorizontalDetector
        GreenFlashHorizontalDetector greenFlash = FindObjectOfType<GreenFlashHorizontalDetector>();

        if (greenFlash == null)
        {
            Debug.LogError("❌ GreenFlashHorizontalDetector NOT FOUND in scene!");
            return;
        }

        Debug.Log($"✅ Found GreenFlashHorizontalDetector on: {greenFlash.gameObject.name}");

        // Check HitFeedback assignment
        var hitFeedbackField = typeof(GreenFlashHorizontalDetector).GetField("hitFeedback",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (hitFeedbackField != null)
        {
            HitFeedback hitFeedback = hitFeedbackField.GetValue(greenFlash) as HitFeedback;

            if (hitFeedback == null)
            {
                Debug.LogError("❌ HitFeedback is NOT ASSIGNED on GreenFlashHorizontalDetector!");
                Debug.LogError("   → Assign a HitFeedback component in Inspector");
            }
            else
            {
                Debug.Log($"✅ HitFeedback assigned: {hitFeedback.gameObject.name}");

                // Check hitFlashImage
                var imageField = typeof(HitFeedback).GetField("hitFlashImage",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (imageField != null)
                {
                    Image flashImage = imageField.GetValue(hitFeedback) as Image;

                    if (flashImage == null)
                    {
                        Debug.LogError("❌ HitFeedback.hitFlashImage is NOT ASSIGNED!");
                        Debug.LogError("   → Assign the HitFlash UI Image in Inspector");
                    }
                    else
                    {
                        Debug.Log($"✅ HitFlashImage assigned: {flashImage.gameObject.name}");
                        Debug.Log($"   Image color: {flashImage.color}");
                        Debug.Log($"   Image active: {flashImage.gameObject.activeSelf}");
                        Debug.Log($"   Canvas: {flashImage.canvas?.name ?? "NULL"}");

                        // Check if image is full screen
                        RectTransform rect = flashImage.GetComponent<RectTransform>();
                        if (rect != null)
                        {
                            Debug.Log($"   RectTransform anchors: Min({rect.anchorMin.x}, {rect.anchorMin.y}) Max({rect.anchorMax.x}, {rect.anchorMax.y})");
                            Debug.Log($"   RectTransform size: {rect.sizeDelta}");

                            if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one)
                            {
                                Debug.LogWarning("⚠️ HitFlashImage is NOT stretched to full screen!");
                                Debug.LogWarning("   → Set anchors to Stretch-Stretch (0,0 to 1,1)");
                            }
                        }
                    }
                }

                // Check cameraRig
                var cameraRigField = typeof(HitFeedback).GetField("cameraRig",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (cameraRigField != null)
                {
                    Transform cameraRig = cameraRigField.GetValue(hitFeedback) as Transform;

                    if (cameraRig == null)
                    {
                        Debug.LogWarning("⚠️ HitFeedback.cameraRig is NOT ASSIGNED!");
                        Debug.LogWarning("   → Camera shake won't work (but flash should still work)");
                    }
                    else
                    {
                        Debug.Log($"✅ CameraRig assigned: {cameraRig.gameObject.name}");
                    }
                }
            }
        }

        // Check Adam assignment
        var adamField = typeof(GreenFlashHorizontalDetector).GetField("adam",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (adamField != null)
        {
            Transform adam = adamField.GetValue(greenFlash) as Transform;

            if (adam == null)
            {
                Debug.LogWarning("⚠️ Adam is NOT ASSIGNED on GreenFlashHorizontalDetector!");
                Debug.LogWarning("   It will try to find GameObject with 'Enemy' tag");
            }
            else
            {
                Debug.Log($"✅ Adam assigned: {adam.gameObject.name}");
            }
        }

        // Check for Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ NO CANVAS found in scene!");
        }
        else
        {
            Debug.Log($"✅ Canvas found: {canvas.gameObject.name}");
            Debug.Log($"   Render Mode: {canvas.renderMode}");
            Debug.Log($"   Sort Order: {canvas.sortingOrder}");
        }

        // Look for HitFlash Image
        Image[] allImages = FindObjectsOfType<Image>();
        Debug.Log($"\n📊 Found {allImages.Length} UI Images in scene:");
        foreach (var img in allImages)
        {
            if (img.gameObject.name.Contains("Hit") || img.gameObject.name.Contains("Flash"))
            {
                Debug.Log($"   • {img.gameObject.name} (Color: {img.color}, Active: {img.gameObject.activeSelf})");
            }
        }

        Debug.Log("\n========================================");
        Debug.Log("🟢 DIAGNOSTIC COMPLETE");
        Debug.Log("========================================\n");
    }

    [ContextMenu("Test Green Flash")]
    public void TestGreenFlash()
    {
        GreenFlashHorizontalDetector greenFlash = FindObjectOfType<GreenFlashHorizontalDetector>();
        if (greenFlash != null)
        {
            Debug.Log("🧪 Manually triggering green flash...");
            // Use reflection to call TriggerGreenFlash
            var method = typeof(GreenFlashHorizontalDetector).GetMethod("TriggerGreenFlash",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(greenFlash, new object[] { "TEST" });
            }
        }
    }
}
