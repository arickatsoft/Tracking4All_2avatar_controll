using UnityEngine;

[System.Serializable]
public class AdamReferenceFix : MonoBehaviour 
{
    void Start()
    {
        FixAdamReferences();
    }
    
    public void FixAdamReferences()
    {
        Debug.Log("Starting Adam reference fix...");
        
        // Find the current Adam with Enemy tag (this should be Adam_Big)
        GameObject adamBig = GameObject.FindGameObjectWithTag("Enemy");
        if (adamBig == null)
        {
            Debug.LogError("No Adam found with Enemy tag!");
            return;
        }
        
        Debug.Log("Found Adam: " + adamBig.name + " at " + adamBig.transform.position);
        
        // Find and fix HorizontalCollisionDetector
        HorizontalCollisionDetector[] horizontalDetectors = FindObjectsOfType<HorizontalCollisionDetector>();
        Debug.Log("Found " + horizontalDetectors.Length + " HorizontalCollisionDetector(s)");
        foreach (var detector in horizontalDetectors)
        {
            detector.adam = adamBig.transform;
            Debug.Log("Updated HorizontalCollisionDetector on " + detector.gameObject.name);
        }
        
        // Find and fix RemyAdamCollisionDetector
        RemyAdamCollisionDetector[] collisionDetectors = FindObjectsOfType<RemyAdamCollisionDetector>();
        Debug.Log("Found " + collisionDetectors.Length + " RemyAdamCollisionDetector(s)");
        foreach (var detector in collisionDetectors)
        {
            detector.adamTransform = adamBig.transform;
            Debug.Log("Updated RemyAdamCollisionDetector on " + detector.gameObject.name);
        }
        
        // Find and fix MediaPipeLimbCollisionDetector using reflection
        MonoBehaviour[] mediaDetectors = FindObjectsOfType<MonoBehaviour>();
        foreach (var component in mediaDetectors)
        {
            if (component.GetType().Name == "MediaPipeLimbCollisionDetector")
            {
                var field = component.GetType().GetField("adam");
                if (field != null && field.FieldType == typeof(Transform))
                {
                    field.SetValue(component, adamBig.transform);
                    Debug.Log("Updated MediaPipeLimbCollisionDetector on " + component.gameObject.name);
                }
            }
        }
        
        // Find and fix GreenFlashHorizontalDetector using reflection
        foreach (var component in mediaDetectors)
        {
            if (component.GetType().Name == "GreenFlashHorizontalDetector")
            {
                var field = component.GetType().GetField("adam");
                if (field != null && field.FieldType == typeof(Transform))
                {
                    field.SetValue(component, adamBig.transform);
                    Debug.Log("Updated GreenFlashHorizontalDetector on " + component.gameObject.name);
                }
            }
        }
        
        Debug.Log("Adam reference fix complete!");
    }
}