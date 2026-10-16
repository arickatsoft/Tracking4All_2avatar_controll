using UnityEngine;

public class AdamReplacer : MonoBehaviour
{
    [ContextMenu("Replace Adam")]
    public void ReplaceAdam()
    {
        GameObject currentAdam = GameObject.Find("Adam");
        if (currentAdam != null)
        {
            Debug.Log("Found Adam at position: " + currentAdam.transform.position);
            Debug.Log("Adam rotation: " + currentAdam.transform.rotation.eulerAngles);
            Debug.Log("Adam scale: " + currentAdam.transform.localScale);
            Debug.Log("Adam tag: " + currentAdam.tag);
        }
    }
}