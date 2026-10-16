using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AdamReplacementManager : MonoBehaviour
{
    [Header("Step 1: Analyze current Adam")]
    public bool analyzeCurrentAdam = false;
    
    [Header("Step 2: Replace with new Adam")]
    public GameObject newAdamPrefab;
    public bool performReplacement = false;
    
    private void Update()
    {
        if (analyzeCurrentAdam)
        {
            analyzeCurrentAdam = false;
            AnalyzeCurrentAdam();
        }
        
        if (performReplacement && newAdamPrefab != null)
        {
            performReplacement = false;
            ReplaceAdam();
        }
    }
    
    public void AnalyzeCurrentAdam()
    {
        GameObject currentAdam = GameObject.Find("Adam");
        if (currentAdam == null)
        {
            Debug.LogError("Current Adam not found!");
            return;
        }
        
        Debug.Log("=== CURRENT ADAM ANALYSIS ===");
        LogGameObjectInfo(currentAdam, "Current Adam");
    }
    
    public void ReplaceAdam()
    {
        GameObject currentAdam = GameObject.Find("Adam");
        if (currentAdam == null)
        {
            Debug.LogError("Current Adam not found!");
            return;
        }
        
        // Store original properties
        Vector3 originalPos = currentAdam.transform.position;
        Quaternion originalRot = currentAdam.transform.rotation;
        Vector3 originalScale = currentAdam.transform.localScale;
        string originalTag = currentAdam.tag;
        int originalLayer = currentAdam.layer;
        
        // Get original components for reference
        Component[] originalComponents = currentAdam.GetComponents<Component>();
        
        Debug.Log("=== REPLACING ADAM ===");
        Debug.Log("Original Adam properties:");
        Debug.Log("Position: " + originalPos);
        Debug.Log("Rotation: " + originalRot.eulerAngles);
        Debug.Log("Scale: " + originalScale);
        Debug.Log("Tag: " + originalTag);
        Debug.Log("Layer: " + originalLayer);
        
        // Deactivate old Adam
        currentAdam.name = "Adam_OLD";
        currentAdam.SetActive(false);
        
        // Instantiate new Adam
        GameObject newAdam = Instantiate(newAdamPrefab);
        newAdam.name = "Adam";
        newAdam.transform.position = originalPos;
        newAdam.transform.rotation = originalRot;
        newAdam.transform.localScale = originalScale;
        newAdam.tag = originalTag;
        newAdam.layer = originalLayer;
        
        Debug.Log("New Adam created successfully!");
        LogGameObjectInfo(newAdam, "New Adam");
        
        // List what components need to be added manually
        Debug.Log("=== COMPONENTS TO ADD MANUALLY ===");
        foreach (Component comp in originalComponents)
        {
            if (comp != null && comp.GetType() != typeof(Transform))
            {
                Debug.Log("Add component: " + comp.GetType().Name);
            }
        }
    }
    
    private void LogGameObjectInfo(GameObject go, string label)
    {
        Debug.Log("=== " + label + " INFO ===");
        Debug.Log("Name: " + go.name);
        Debug.Log("Position: " + go.transform.position);
        Debug.Log("Rotation: " + go.transform.eulerAngles);
        Debug.Log("Scale: " + go.transform.localScale);
        Debug.Log("Tag: " + go.tag);
        Debug.Log("Layer: " + go.layer);
        Debug.Log("Active: " + go.activeSelf);
        Debug.Log("Children: " + go.transform.childCount);
        
        // List components
        Component[] components = go.GetComponents<Component>();
        Debug.Log("Components (" + components.Length + "):");
        foreach (Component comp in components)
        {
            if (comp != null)
            {
                Debug.Log("- " + comp.GetType().Name);
            }
        }
        
        // List children
        if (go.transform.childCount > 0)
        {
            Debug.Log("Children:");
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform child = go.transform.GetChild(i);
                Debug.Log("- " + child.name);
            }
        }
    }
}