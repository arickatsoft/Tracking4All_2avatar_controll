using UnityEngine;

public class SimpleTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🎯 SimpleTest: Script is running!");
        
        // Find Adam
        GameObject adam = GameObject.FindGameObjectWithTag("Enemy");
        if (adam != null)
        {
            Debug.Log($"✅ Found Adam at: {adam.transform.position}");
        }
        else
        {
            Debug.Log("❌ Could not find Adam");
        }
        
        // Find Remy
        GameObject remy = GameObject.FindGameObjectWithTag("Player");
        if (remy != null)
        {
            Debug.Log($"✅ Found Remy at: {remy.transform.position}");
            
            // Look for hands
            Transform leftHand = FindChildRecursive(remy.transform, "LeftHand");
            Transform rightHand = FindChildRecursive(remy.transform, "RightHand");
            
            if (leftHand != null)
            {
                Debug.Log($"✅ Found LeftHand: {leftHand.name} at {leftHand.position}");
            }
            
            if (rightHand != null)
            {
                Debug.Log($"✅ Found RightHand: {rightHand.name} at {rightHand.position}");
            }
        }
    }
    
    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
            {
                return child;
            }
            Transform result = FindChildRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), "SimpleTest is running!");
    }
}