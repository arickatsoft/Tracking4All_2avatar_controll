using UnityEngine;

public class TagSetup : MonoBehaviour
{
    [Header("Auto-Create Missing Tags")]
    public bool createTagsOnStart = true;
    
    [Header("Required Tags")]
    public string[] requiredTags = new string[]
    {
        "LeftFoot",
        "RightFoot", 
        "LeftHand",
        "RightHand",
        "Head",
        "Body"
    };
    
    void Start()
    {
        if (createTagsOnStart)
        {
            CreateMissingTags();
        }
    }
    
    [ContextMenu("Create Missing Tags")]
    public void CreateMissingTags()
    {
        Debug.Log("🏷️ Checking for missing tags...");
        
        foreach (string tagName in requiredTags)
        {
            if (!TagExists(tagName))
            {
                Debug.LogWarning($"❌ Tag '{tagName}' is missing!");
                Debug.Log($"💡 Please manually add tag '{tagName}' in Edit → Project Settings → Tags and Layers");
            }
            else
            {
                Debug.Log($"✅ Tag '{tagName}' exists");
            }
        }
        
        Debug.Log("🏷️ Tag check complete!");
        Debug.Log("📝 To add missing tags: Edit → Project Settings → Tags and Layers → Tags → Plus button");
    }
    
    private bool TagExists(string tagName)
    {
        try
        {
            GameObject.FindWithTag(tagName);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }
}
