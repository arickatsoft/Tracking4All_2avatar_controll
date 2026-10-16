using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Simple script to hide avatar renderers while keeping pose tracking active.
    /// Attach this to the Puppet-StandardT GameObject.
    /// </summary>
    public class HideAvatarRenderers : MonoBehaviour
    {
        void Start()
        {
            // Hide all renderers on this GameObject and its children
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            
            foreach (var renderer in meshRenderers)
            {
                renderer.enabled = false;
            }
            
            foreach (var renderer in skinnedRenderers)
            {
                renderer.enabled = false;
            }
            
            Logger.LogKey($"Hidden {meshRenderers.Length + skinnedRenderers.Length} renderers on {gameObject.name} while keeping tracking active");
        }
    }
}
