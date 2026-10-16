// SettingsUIElement
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;
using UnityEngine.UI;

namespace Tracking4All
{
    /// <summary>
    /// Base class for a UI element that works as a setting.
    /// </summary>
    public abstract class SettingsUIElement : MonoBehaviour
    {
        [SerializeField] protected Text nameText;

        /// <summary>
        /// Unique name of the setting.
        /// </summary>
        public string Name => setting.Name;
        /// <summary>
        /// The underlying value of the setting.
        /// </summary>
        public object SettingValueObject => setting.GetValue();
        public bool SettingVisible => setting.Visible;
        /// <summary>
        /// Get the underlying restartable (can be null).
        /// </summary>
        public ISettingsUser Restartable => setting.Restartable;

        protected RuntimeSetting setting;
        public bool IsBoundTo(RuntimeSetting value) => ReferenceEquals(setting, value);

        public void Hook(RuntimeSetting setting)
        {
            nameText.text = setting.Name;
            this.setting = setting;
        }

        /// <summary>
        /// Tries to set the underlying setting value with the object.
        /// <para>Will silently fail if the value is a mismatch.</para>
        /// </summary>
        /// <param name="value">Often can be the string representing the value OR the value itself.</param>
        public void SetValue(object value)
        {
            if (setting == null || value == null) return;
            if (value.ToString().Equals(this.setting.GetValue().ToString())) return;

            setting.OnSettingUIChanged(value);
        }
    }
}
