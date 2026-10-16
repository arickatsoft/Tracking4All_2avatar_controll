using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Hide avatar visually while keeping pose tracking functionality.
    /// Attach this script to any avatar GameObject to make it invisible.
    /// </summary>
    public class HideAvatar : MonoBehaviour
    {
        [SerializeField] private bool hideOnStart = true;
        [SerializeField] private bool disableRenderers = true;
        [SerializeField] private bool disableGameObject = false;
        
        private MeshRenderer[] renderers;
        private SkinnedMeshRenderer[] skinnedRenderers;
        
        void Start()
        {
            if (hideOnStart)
            {
                HideAvatarVisual();
            }
        }
        
        /// <summary>
        /// Hide the avatar while keeping tracking functionality
        /// </summary>
        public void HideAvatarVisual()
        {
            if (disableGameObject)
            {
                // Option 1: Disable entire GameObject (most effective)
                gameObject.SetActive(false);
            }
            else if (disableRenderers)
            {
                // Option 2: Disable only renderers (keeps tracking active)
                renderers = GetComponentsInChildren<MeshRenderer>();
                skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
                
                foreach (var renderer in renderers)
                {
                    renderer.enabled = false;
                }
                
                foreach (var skinnedRenderer in skinnedRenderers)
                {
                    skinnedRenderer.enabled = false;
                }
                
                Logger.LogKey($"Hidden {renderers.Length + skinnedRenderers.Length} renderers on {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Show the avatar again
        /// </summary>
        public void ShowAvatarVisual()
        {
            if (disableGameObject)
            {
                gameObject.SetActive(true);
            }
            else if (disableRenderers && renderers != null && skinnedRenderers != null)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer != null) renderer.enabled = true;
                }
                
                foreach (var skinnedRenderer in skinnedRenderers)
                {
                    if (skinnedRenderer != null) skinnedRenderer.enabled = true;
                }
                
                Logger.LogKey($"Showed {renderers.Length + skinnedRenderers.Length} renderers on {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Toggle avatar visibility
        /// </summary>
        public void ToggleAvatarVisibility()
        {
            if (disableGameObject)
            {
                gameObject.SetActive(!gameObject.activeSelf);
            }
            else if (renderers != null && renderers.Length > 0)
            {
                bool isVisible = renderers[0].enabled;
                if (isVisible)
                    HideAvatarVisual();
                else
                    ShowAvatarVisual();
            }
        }
    }
}
