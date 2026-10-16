using UnityEngine;

public class AdamBigInstantiator : MonoBehaviour
{
    void Start()
    {
        InstantiateAdamBig();
    }

    public void InstantiateAdamBig()
    {
        GameObject adamBigPrefab = Resources.Load<GameObject>("Adam_Big");
        
        if (adamBigPrefab == null)
        {
            Debug.LogError("Adam_Big prefab not found in Resources");
            return;
        }

        GameObject instance = Instantiate(adamBigPrefab, transform.position, transform.rotation);
        instance.transform.localScale = transform.localScale;
        instance.name = "Adam_Big_Instance";
        
        Debug.Log("Adam_Big instantiated successfully!");
    }
}