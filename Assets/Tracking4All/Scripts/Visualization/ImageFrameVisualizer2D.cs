// ImageFrameVisualizer2D
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;
using UnityEngine.UI;

namespace Tracking4All
{
    public class ImageFrameVisualizer2D : MonoBehaviour
    {
        [SerializeField] protected InterfaceProvider<IImageFrameProvider> imageProvider;
        [SerializeField] protected RawImage image;
        [SerializeField] protected Texture noDataTexture;

        private void OnEnable()
        {
            image.texture = noDataTexture;

            imageProvider.Provider.Lifecycle.OnReady += Lifecycle_OnReady;
            imageProvider.Provider.Lifecycle.OnDisposed += Lifecycle_OnDisposed;
        }
        private void OnDisable()
        {
            imageProvider.Provider.Lifecycle.OnReady -= Lifecycle_OnReady;
            imageProvider.Provider.Lifecycle.OnDisposed -= Lifecycle_OnDisposed;

            Lifecycle_OnDisposed(0);
        }

        private void Lifecycle_OnDisposed(int group)
        {
            image.texture = noDataTexture;
        }

        private void Lifecycle_OnReady(int group)
        {
            image.texture = imageProvider.Provider.Get(group, 0).Texture;

        }

    }
}