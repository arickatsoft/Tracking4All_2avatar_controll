using UnityEngine;
using UnityEditor;

public class AdamCopyUtility : MonoBehaviour
{
    [ContextMenu("Copy Adam GameObject")]
    public void CopyAdamGameObject()
    {
        #if UNITY_EDITOR
        GameObject adam = GameObject.Find("Adam");
        if (adam != null)
        {
            // Store the current Adam's properties
            Vector3 position = adam.transform.position;
            Quaternion rotation = adam.transform.rotation;
            Vector3 scale = adam.transform.localScale;
            string tag = adam.tag;
            int layer = adam.layer;
            
            Debug.Log("Found Adam GameObject with properties:");
            Debug.Log("Position: " + position);
            Debug.Log("Rotation: " + rotation.eulerAngles);
            Debug.Log("Scale: " + scale);
            Debug.Log("Tag: " + tag);
            Debug.Log("Layer: " + layer);
            Debug.Log("Children count: " + adam.transform.childCount);
            
            // List all components on Adam
            Component[] components = adam.GetComponents<Component>();
            Debug.Log("Adam has " + components.Length + " components:");
            foreach (Component comp in components)
            {
                if (comp != null)
                {
                    Debug.Log("- " + comp.GetType().Name);
                }
            }
            
            // List all child objects
            Debug.Log("Adam's children:");
            for (int i = 0; i < adam.transform.childCount; i++)
            {
                Transform child = adam.transform.GetChild(i);
                Debug.Log("- " + child.name);
            }
            
            // Copy the Adam GameObject to clipboard
            Selection.activeGameObject = adam;
            EditorGUIUtility.systemCopyBuffer = "Adam_GameObject_Copied";
            Debug.Log("Adam GameObject selected. You can now copy it with Ctrl+C");
        }
        else
        {
            Debug.LogError("Adam GameObject not found in scene!");
        }
        #endif
    }
}