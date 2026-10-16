// SolutionBase
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    [DefaultExecutionOrder(-500)]
    public abstract class SolutionBase : LivingUnityAdapterContainer, ISolutionSettings
    {
        [SerializeField] protected SolutionSettingsProvider adapterSettingsProvider;

        public int Width => ((ISolutionSettings)adapterSettingsProvider).Width;

        public int Height => ((ISolutionSettings)adapterSettingsProvider).Height;

        public bool PerspectiveFlip => ((ISolutionSettings)adapterSettingsProvider).PerspectiveFlip;

        public bool Mirror => ((ISolutionSettings)adapterSettingsProvider).Mirror;
    }

}