using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayerSelectionManager))]
public class PlayerSelectionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        PlayerSelectionManager manager = (PlayerSelectionManager)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Find Avatars"))
        {
            GameObject remy = GameObject.Find("Remy");
            GameObject adam = GameObject.Find("Adam_Big");
            
            if (remy != null)
            {
                SerializedProperty remyProp = serializedObject.FindProperty("remyAvatar");
                remyProp.objectReferenceValue = remy;
            }
            
            if (adam != null)
            {
                SerializedProperty adamProp = serializedObject.FindProperty("adamAvatar");
                adamProp.objectReferenceValue = adam;
            }
            
            serializedObject.ApplyModifiedProperties();
            Debug.Log("Auto-found avatars in scene");
        }
        
        if (GUILayout.Button("Auto-Find Camera"))
        {
            GameObject cameraRig = GameObject.Find("CameraRig");
            if (cameraRig != null)
            {
                SerializedProperty rigProp = serializedObject.FindProperty("cameraRig");
                rigProp.objectReferenceValue = cameraRig.transform;
            }
            
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                SerializedProperty camProp = serializedObject.FindProperty("mainCamera");
                camProp.objectReferenceValue = mainCam;
            }
            
            serializedObject.ApplyModifiedProperties();
            Debug.Log("Auto-found camera components");
        }
        
        if (GUILayout.Button("Create Selection UI"))
        {
            if (Application.isPlaying)
            {
                manager.ShowPlayerSelection();
            }
            else
            {
                Debug.LogWarning("Enter Play Mode to create UI");
            }
        }
    }
}
