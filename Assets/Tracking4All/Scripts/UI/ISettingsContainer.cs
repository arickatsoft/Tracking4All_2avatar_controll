// ISettingsContainer
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    /// <summary>
    /// Interface for a set of registerable settings (used with SettingsManager).
    /// </summary>
    public interface ISettingsContainer
    {
        /// <summary>
        /// Hook all the settings with the input.
        /// </summary>
        public void Hook(ISettingsUser restartable);

        /// <summary>
        /// Register/add all the respective settings with the SettingsManager.
        /// </summary>
        public void RegisterMenuSettings();

        /// <summary>
        /// Unregister/remove all the respective settings with the SettingsManager.
        /// </summary>
        public void DeregisterMenuSettings();
    }
}