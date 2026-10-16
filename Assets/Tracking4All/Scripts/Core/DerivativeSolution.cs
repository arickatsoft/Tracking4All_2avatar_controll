// DerivativeSolution
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    [DefaultExecutionOrder(-200)]
    public abstract class DerivativeSolution : MonoBehaviour, ISolutionSettings
    {
        [SerializeField] protected SolutionSettingsProvider adapterSettingsProvider;

        public int Width => ((ISolutionSettings)adapterSettingsProvider).Width;
        public int Height => ((ISolutionSettings)adapterSettingsProvider).Height;
        public bool PerspectiveFlip => ((ISolutionSettings)adapterSettingsProvider).PerspectiveFlip;
        public bool Mirror => ((ISolutionSettings)adapterSettingsProvider).Mirror;

        protected virtual void Awake()
        {
            CreateAdapters();
        }
        protected virtual void OnDestroy()
        {
            DisposeAdapters();
        }

        protected abstract void CreateAdapters();
        protected abstract void DisposeAdapters();
    }

}