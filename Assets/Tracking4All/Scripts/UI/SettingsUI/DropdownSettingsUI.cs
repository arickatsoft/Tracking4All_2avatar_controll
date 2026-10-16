// DropdownSettings
// (C) 2024-2025 G8gaming Ltd.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tracking4All
{
    public class DropdownSettingsUI : SettingsUIElement
    {
        [SerializeField] protected Dropdown dropdown;

        public void Hook(RuntimeSetting setting, List<string> options, int defaultValue)
        {
            base.Hook(setting);
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(defaultValue);
            dropdown.onValueChanged.RemoveListener(Changed);
            dropdown.onValueChanged.AddListener(Changed);
        }

        protected void Changed(int value)
        {
            if (setting == null || value < 0 || value >= dropdown.options.Count) return;
            if (setting is IntByStringSetting) setting.OnSettingUIChanged(value);
            else setting.OnSettingUIChanged(dropdown.options[value].text);
        }
    }
}
