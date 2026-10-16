using UnityEngine;

public class SimpleAdamReplacer : MonoBehaviour
{
    public void ReplaceAdam()
    {
        Debug.Log("Starting Adam replacement process...");
        
        // Find current Adam
        GameObject currentAdam = GameObject.Find("Adam");
        if (currentAdam == null)
        {
            Debug.LogError("Current Adam not found!");
            return;
        }
        
        Debug.Log("Found current Adam. Proceeding with replacement...");
        
        // Get current Adam properties
        Vector3 pos = currentAdam.transform.position;
        Quaternion rot = currentAdam.transform.rotation;
        Vector3 scale = currentAdam.transform.localScale;
        
        Debug.Log("Position: " + pos);
        Debug.Log("Rotation: " + rot.eulerAngles);
        Debug.Log("Scale: " + scale);
        
        // Disable current Adam
        currentAdam.name = "Adam_OLD";
        currentAdam.SetActive(false);
        
        Debug.Log("Current Adam disabled. Ready for manual prefab placement.");
    }
}