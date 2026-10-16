using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class AdamBigReferenceUpdater : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private GameObject currentAdamBig;
    [SerializeField] private int referencesFound = 0;
    [SerializeField] private int referencesFixed = 0;
    
    [ContextMenu("Fix All Adam References to Adam_Big")]
    public void FixAllAdamReferences()
    {
        Debug.Log("=== FIXING ALL ADAM REFERENCES TO USE ADAM_BIG ===");
        
        // Find the current Adam (Adam_Big) in the scene
        currentAdamBig = GameObject.FindGameObjectWithTag("Enemy");
        if (currentAdamBig == null)
        {
            Debug.LogError("Could not find Adam with Enemy tag!");
            return;
        }
        
        // Verify this is the Adam_Big by checking its children structure
        if (!VerifyAdamBigStructure(currentAdamBig))
        {
            Debug.LogError("Found Adam doesn't have Adam_Big structure!");
            return;
        }
        
        Debug.Log($"Found Adam_Big: {currentAdamBig.name} at {currentAdamBig.transform.position}");
        Debug.Log($"Adam_Big has correct structure with {currentAdamBig.transform.childCount} children");
        
        referencesFound = 0;
        referencesFixed = 0;
        
        // Fix all script references
        FixScriptReferences();
        
        // Fix prefab references if any
        FixPrefabReferences();
        
        Debug.Log($"=== COMPLETE: Found {referencesFound} references, fixed {referencesFixed} ===");
        
        // Mark scene as dirty for saving
        #if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        #endif
    }
    
    private bool VerifyAdamBigStructure(GameObject adam)
    {
        // Check for Adam_Big specific children
        string[] expectedChildren = {
            "Ch08_Beard", "Ch08_Body", "Ch08_Eyelashes", "Ch08_Hair", 
            "Ch08_Hoodie", "Ch08_Pants", "Ch08_Sneakers", "mixamorig7:Hips"
        };
        
        int foundChildren = 0;
        foreach (string childName in expectedChildren)
        {
            Transform child = adam.transform.Find(childName);
            if (child != null)
            {
                foundChildren++;
            }
        }
        
        return foundChildren >= 6; // At least 6 out of 8 children should be present
    }
    
    private void FixScriptReferences()
    {
        Debug.Log("Fixing script references...");
        
        // Get all MonoBehaviour components in the scene
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        
        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null) continue;
            FixScriptAdamReferences(script);
        }
    }
    
    private void FixScriptAdamReferences(MonoBehaviour script)
    {
        FieldInfo[] fields = script.GetType().GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        foreach (FieldInfo field in fields)
        {
            bool fieldUpdated = false;
            
            // Handle GameObject references
            if (field.FieldType == typeof(GameObject))
            {
                GameObject currentValue = (GameObject)field.GetValue(script);
                if (ShouldUpdateReference(currentValue, field.Name))
                {
                    field.SetValue(script, currentAdamBig);
                    fieldUpdated = true;
                    referencesFound++;
                    referencesFixed++;
                }
            }
            // Handle Transform references
            else if (field.FieldType == typeof(Transform))
            {
                Transform currentValue = (Transform)field.GetValue(script);
                if (currentValue != null && ShouldUpdateReference(currentValue.gameObject, field.Name))
                {
                    field.SetValue(script, currentAdamBig.transform);
                    fieldUpdated = true;
                    referencesFound++;
                    referencesFixed++;
                }
            }
            // Handle Animator references
            else if (field.FieldType == typeof(Animator))
            {
                Animator currentValue = (Animator)field.GetValue(script);
                if (currentValue != null && ShouldUpdateReference(currentValue.gameObject, field.Name))
                {
                    Animator newAnimator = currentAdamBig.GetComponent<Animator>();
                    if (newAnimator != null)
                    {
                        field.SetValue(script, newAnimator);
                        fieldUpdated = true;
                        referencesFound++;
                        referencesFixed++;
                    }
                }
            }
            
            if (fieldUpdated)
            {
                Debug.Log($"UPDATED: {script.gameObject.name}.{script.GetType().Name}.{field.Name} -> Adam_Big");
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(script);
                #endif
            }
        }
    }
    
    private bool ShouldUpdateReference(GameObject obj, string fieldName)
    {
        if (obj == null) return false;
        if (obj == currentAdamBig) return false; // Already correct
        
        // Check if this is an old Adam reference that needs updating
        bool isAdamRelated = obj.name.ToLower().Contains("adam") || 
                            fieldName.ToLower().Contains("adam") ||
                            obj.CompareTag("Enemy");
        
        // Also check if it's pointing to an object that doesn't have the Adam_Big structure
        if (isAdamRelated && !VerifyAdamBigStructure(obj))
        {
            return true;
        }
        
        return false;
    }
    
    private void FixPrefabReferences()
    {
        Debug.Log("Checking for prefab references...");
        
        // This would handle prefab references if there were any
        // For now, we focus on scene references since that's the main issue
    }
    
    [ContextMenu("Verify Current Adam Setup")]
    public void VerifyCurrentSetup()
    {
        GameObject adam = GameObject.FindGameObjectWithTag("Enemy");
        if (adam == null)
        {
            Debug.LogError("No Adam found with Enemy tag!");
            return;
        }
        
        Debug.Log("=== CURRENT ADAM VERIFICATION ===");
        Debug.Log($"Name: {adam.name}");
        Debug.Log($"Position: {adam.transform.position}");
        Debug.Log($"Rotation: {adam.transform.rotation.eulerAngles}");
        Debug.Log($"Scale: {adam.transform.localScale}");
        Debug.Log($"Instance ID: {adam.GetEntityId()}");
        Debug.Log($"Children count: {adam.transform.childCount}");
        
        // List all children
        Debug.Log("Children:");
        for (int i = 0; i < adam.transform.childCount; i++)
        {
            Transform child = adam.transform.GetChild(i);
            Debug.Log($"  - {child.name}");
        }
        
        // Check if it has Adam_Big structure
        bool isAdamBig = VerifyAdamBigStructure(adam);
        Debug.Log($"Has Adam_Big structure: {isAdamBig}");
        
        // Check components
        Component[] components = adam.GetComponents<Component>();
        Debug.Log($"Components ({components.Length}):");
        foreach (Component comp in components)
        {
            Debug.Log($"  - {comp.GetType().Name}");
        }
    }
    
    [ContextMenu("Find All Adam References (Debug)")]
    public void FindAllAdamReferencesDebug()
    {
        Debug.Log("=== SEARCHING FOR ALL ADAM REFERENCES ===");
        
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        int totalFound = 0;
        
        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null) continue;
            
            FieldInfo[] fields = script.GetType().GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (FieldInfo field in fields)
            {
                bool foundReference = false;
                GameObject referencedObject = null;
                
                if (field.FieldType == typeof(GameObject))
                {
                    GameObject value = (GameObject)field.GetValue(script);
                    if (value != null && (value.name.ToLower().Contains("adam") || field.Name.ToLower().Contains("adam")))
                    {
                        referencedObject = value;
                        foundReference = true;
                    }
                }
                else if (field.FieldType == typeof(Transform))
                {
                    Transform value = (Transform)field.GetValue(script);
                    if (value != null && (value.gameObject.name.ToLower().Contains("adam") || field.Name.ToLower().Contains("adam")))
                    {
                        referencedObject = value.gameObject;
                        foundReference = true;
                    }
                }
                
                if (foundReference)
                {
                    totalFound++;
                    bool isCorrect = referencedObject == currentAdamBig;
                    string status = isCorrect ? "[CORRECT]" : "[NEEDS UPDATE]";
                    Debug.Log($"{status} {script.gameObject.name}.{script.GetType().Name}.{field.Name} -> {referencedObject.name} (ID: {referencedObject.GetEntityId()})");
                }
            }
        }
        
        Debug.Log($"Total Adam references found: {totalFound}");
    }
}