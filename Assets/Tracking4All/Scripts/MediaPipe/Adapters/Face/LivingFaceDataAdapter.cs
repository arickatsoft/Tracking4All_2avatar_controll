// LivingFaceBlendshapeDataAdapter
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    public class LivingFaceDataAdapter : LivingProxyAdapterContainer,
        IFaceDataProvider
    {
        // Basically just an empty shell for redirection.

        [SerializeField] private FaceDataProvider faceDataProvider;

        public IProviderLifecycle Lifecycle => faceDataProvider.Provider.Lifecycle;

        public int GroupSize => faceDataProvider.Provider.GroupSize;

        public int DataCount => faceDataProvider.Provider.DataCount;

        public FaceData Get(int group, FaceDataIdentifier index)
        {
            return faceDataProvider.Provider.Get(group, index);
        }

        public FaceData Get(int group, int index)
        {
            return faceDataProvider.Provider.Get(group, index);
        }

        protected override void CreateProxyAdapters()
        {

        }

        protected override void DisposeProxyAdapters()
        {

        }
    }
}