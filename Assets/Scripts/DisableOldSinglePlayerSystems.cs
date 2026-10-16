using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Disables old single-player systems to prevent conflicts with multiplayer
    /// This runs early to prevent the old Adam/Remy chooser from appearing
    /// Also prevents camera positioning issues that occurred in the network solution
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before other systems
    public class DisableOldSinglePlayerSystems : MonoBehaviour
    {
        void Awake()
        {
            UnityEngine.Debug.Log("=== DisableOldSinglePlayerSystems Running ===");
            
            // Disable InstantPlayerSelector to prevent Adam/Remy dialog
            DisableOldPlayerSelector();
            
            // Disable any other conflicting systems
            DisableConflictingSystems();
        }
        
        void DisableOldPlayerSelector()
        {
            // Find and disable InstantPlayerSelector immediately
            InstantPlayerSelector[] oldSelectors = FindObjectsOfType<InstantPlayerSelector>();
            foreach (InstantPlayerSelector selector in oldSelectors)
            {
                UnityEngine.Debug.Log($"Disabling InstantPlayerSelector on {selector.gameObject.name}");
                selector.enabled = false;
                selector.gameObject.SetActive(false);
            }
            
            // Find and disable old player selection UI
            GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allGameObjects)
            {
                if (obj.name.Contains("PlayerSelection") || 
                    obj.name.Contains("PlayerChoose") ||
                    obj.name.Contains("PlayerSelector"))
                {
                    UnityEngine.Debug.Log($"Disabling old player selection UI: {obj.name}");
                    obj.SetActive(false);
                }
            }
        }
        
        void DisableConflictingSystems()
        {
            // Disable any camera control scripts that might interfere
            MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();
            foreach (MonoBehaviour component in allComponents)
            {
                string componentName = component.GetType().Name;
                
                // Disable camera positioning scripts from the old system
                if (componentName.Contains("CameraRig") && 
                    componentName.Contains("Startup") || 
                    componentName.Contains("CameraPosition"))
                {
                    UnityEngine.Debug.Log($"Disabling potential camera conflict script: {componentName}");
                    component.enabled = false;
                }
            }
        }
    }
}