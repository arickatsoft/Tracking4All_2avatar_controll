using UnityEngine;

public class QuickAdamFix : MonoBehaviour
{
    void Awake()
    {
        FixReferences();
    }
    
    void FixReferences()
    {
        GameObject adam = GameObject.FindGameObjectWithTag("Enemy");
        if (adam == null) return;
        
        // Fix collision detectors
        var detectors = FindObjectsOfType<MonoBehaviour>();
        foreach (var detector in detectors)
        {
            var type = detector.GetType();
            if (type.Name.Contains("Collision") || type.Name.Contains("Detection"))
            {
                var adamField = type.GetField("adam");
                if (adamField != null && adamField.FieldType == typeof(Transform))
                {
                    adamField.SetValue(detector, adam.transform);
                }
                
                var adamTransformField = type.GetField("adamTransform");
                if (adamTransformField != null && adamTransformField.FieldType == typeof(Transform))
                {
                    adamTransformField.SetValue(detector, adam.transform);
                }
            }
        }
        
        Debug.Log("Quick Adam fix applied!");
    }
}