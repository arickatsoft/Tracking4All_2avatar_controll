using UnityEngine;

public class AdamReferenceChecker : MonoBehaviour
{
    [ContextMenu("Check Adam References")]
    public void CheckReferences()
    {
        GameObject oldAdam = GameObject.Find("Adam_OLD");
        GameObject newAdam = GameObject.Find("Adam");
        
        Debug.Log("=== ADAM REFERENCE CHECK ===");
        
        if (oldAdam != null)
        {
            Debug.Log("Found Adam_OLD at: " + oldAdam.transform.position);
            Debug.Log("Adam_OLD active: " + oldAdam.activeSelf);
        }
        
        if (newAdam != null)
        {
            Debug.Log("Found new Adam at: " + newAdam.transform.position);
            Debug.Log("New Adam active: " + newAdam.activeSelf);
        }
        
        // Check for tracking system references
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        Debug.Log("Total scripts in scene: " + allScripts.Length);
        
        foreach (MonoBehaviour script in allScripts)
        {
            if (script != null)
            {
                string typeName = script.GetType().Name;
                if (typeName.Contains("Track") || typeName.Contains("Pose") || typeName.Contains("Avatar"))
                {
                    Debug.Log("Tracking component: " + script.gameObject.name + "." + typeName);
                }
            }
        }
    }
}