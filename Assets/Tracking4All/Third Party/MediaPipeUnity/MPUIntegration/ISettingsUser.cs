// ISettingsUser
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    /// <summary>
    /// Allows for generic restarting of an object.
    /// </summary>
    public interface ISettingsUser
    {
        /// <summary>
        /// Called when a setting has been changed and applied. The user should update using the new set of settings.
        /// </summary>
        /// <returns></returns>
        public void OnSettingsApplied();
    }
}