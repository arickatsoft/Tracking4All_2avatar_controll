// LivingProxyAdapterContainer
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    [DefaultExecutionOrder(-750)]
    public abstract class LivingProxyAdapterContainer : MonoBehaviour, ISolutionSettings
    {
        [SerializeField] protected SolutionSettingsProvider settingsProvider;

        public int Width => ((ISolutionSettings)settingsProvider).Width;

        public int Height => ((ISolutionSettings)settingsProvider).Height;

        public bool PerspectiveFlip => ((ISolutionSettings)settingsProvider).PerspectiveFlip;

        public bool Mirror => ((ISolutionSettings)settingsProvider).Mirror;

        protected virtual void Awake()
        {
            CreateProxyAdapters();
        }
        protected abstract void CreateProxyAdapters();

        protected virtual void OnDestroy()
        {
            DisposeProxyAdapters();
        }
        protected abstract void DisposeProxyAdapters();
    }

}