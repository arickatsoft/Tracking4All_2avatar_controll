// SolutionSettingsProvider
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine.Scripting;

namespace Tracking4All
{
    [System.Serializable]
    [Preserve]
    public class SolutionSettingsProvider : InterfaceProvider<ISolutionSettings>, ISolutionSettings
    {
        public bool PerspectiveFlip => Provider.PerspectiveFlip;
        public bool Mirror => Provider.Mirror;

        public int Width => Provider.Width;

        public int Height => Provider.Height;

    }
}