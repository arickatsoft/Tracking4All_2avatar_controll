using UnityEngine;
using UnityEditor;

public class AdamReplacementController : MonoBehaviour
{
    [Header("Adam Replacement")]
    public GameObject currentAdam;
    public GameObject newAdamPrefab;
    
    [ContextMenu("Find Current Adam")]
    public void FindCurrentAdam()
    {
        currentAdam = GameObject.Find("Adam");
        if (currentAdam != null)
        {
            Debug.Log("Found current Adam at: " + currentAdam.transform.position);
            LogAdamComponents();
        }
        else
        {
            Debug.LogError("Could not find Adam GameObject");
        }
    }
    
    [ContextMenu("Load Adam Big from Assets")]
    public void LoadAdamBigFromAssets()
    {
#if UNITY_EDITOR
        string assetPath = "Assets/Adam_Big.fbx";
        GameObject adamBigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        
        if (adamBigAsset != null)
        {
            newAdamPrefab = adamBigAsset;
            Debug.Log("Adam_Big loaded from assets successfully!");
        }
        else
        {
            Debug.LogError("Could not load Adam_Big from: " + assetPath);
        }
#endif
    }
    
    [ContextMenu("Replace Adam")]
    public void ReplaceAdam()
    {
        if (currentAdam == null)
        {
            FindCurrentAdam();
        }
        
        if (newAdamPrefab == null)
        {
            LoadAdamBigFromAssets();
        }
        
        if (currentAdam == null || newAdamPrefab == null)
        {
            Debug.LogError("Missing current Adam or new Adam prefab");
            return;
        }
        
        PerformReplacement();
    }
    
    void PerformReplacement()
    {
        // Store original properties
        Vector3 position = currentAdam.transform.position;
        Quaternion rotation = currentAdam.transform.rotation;
        Vector3 scale = currentAdam.transform.localScale;
        string tag = currentAdam.tag;
        int layer = currentAdam.layer;
        
        // Get components to copy
        Animator originalAnimator = currentAdam.GetComponent<Animator>();
        CapsuleCollider originalCollider = currentAdam.GetComponent<CapsuleCollider>();
        Rigidbody originalRigidbody = currentAdam.GetComponent<Rigidbody>();
        
        // Instantiate new Adam
        GameObject newAdam = Instantiate(newAdamPrefab, position, rotation);
        newAdam.transform.localScale = scale;
        newAdam.tag = tag;
        newAdam.layer = layer;
        newAdam.name = "Adam";
        
        // Copy essential components
        CopyAnimator(originalAnimator, newAdam);
        CopyCollider(originalCollider, newAdam);
        CopyRigidbody(originalRigidbody, newAdam);
        
        // Disable old Adam
        currentAdam.name = "Adam_OLD";
        currentAdam.SetActive(false);
        
        Debug.Log("Adam replacement completed!");
    }
    
    void CopyAnimator(Animator original, GameObject target)
    {
        if (original != null)
        {
            Animator newAnimator = target.GetComponent<Animator>();
            if (newAnimator == null)
            {
                newAnimator = target.AddComponent<Animator>();
            }
            newAnimator.runtimeAnimatorController = original.runtimeAnimatorController;
            newAnimator.avatar = original.avatar;
        }
    }
    
    void CopyCollider(CapsuleCollider original, GameObject target)
    {
        if (original != null)
        {
            CapsuleCollider newCollider = target.GetComponent<CapsuleCollider>();
            if (newCollider == null)
            {
                newCollider = target.AddComponent<CapsuleCollider>();
            }
            newCollider.center = original.center;
            newCollider.radius = original.radius;
            newCollider.height = original.height;
            newCollider.direction = original.direction;
            newCollider.isTrigger = original.isTrigger;
        }
    }
    
    void CopyRigidbody(Rigidbody original, GameObject target)
    {
        if (original != null)
        {
            Rigidbody newRigidbody = target.GetComponent<Rigidbody>();
            if (newRigidbody == null)
            {
                newRigidbody = target.AddComponent<Rigidbody>();
            }
            newRigidbody.mass = original.mass;
            newRigidbody.linearDamping = original.linearDamping;
            newRigidbody.angularDamping = original.angularDamping;
            newRigidbody.useGravity = original.useGravity;
            newRigidbody.isKinematic = original.isKinematic;
        }
    }
    
    void LogAdamComponents()
    {
        if (currentAdam != null)
        {
            Component[] components = currentAdam.GetComponents<Component>();
            Debug.Log("Adam components:");
            foreach (Component comp in components)
            {
                if (comp != null)
                {
                    Debug.Log("- " + comp.GetType().Name);
                }
            }
        }
    }
}