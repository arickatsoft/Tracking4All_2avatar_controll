// SettingsMenu
// (C) 2024-2025 G8gaming Ltd.
using System.Collections.Generic;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Manages settings and populates appropriate debug menu window.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance;

        [SerializeField] private GameObject settingsMenuContent;
        [SerializeField] private DropdownSettingsUI dropdownPrefab;
        [SerializeField] private TextInputSettingsUI floatPrefab;
        [SerializeField] private TextInputSettingsUI intPrefab;
        [SerializeField] private ToggleSettingsUI boolPrefab;

        /// <summary>
        /// Map setting name to UI element.
        /// </summary>
        private static Dictionary<string, SettingsUIElement> settings = new();
        private static Dictionary<string, object> oldSettingValues = new();

        public delegate void SettingChanged(string setting);
        /// <summary>
        /// Called when any setting value is both changed and applied.
        /// </summary>
        public static event SettingChanged OnSettingChanged;



        private void Awake()
        {
            // Initialize static dictionaries if null
            if (settings == null)
            {
                settings = new Dictionary<string, SettingsUIElement>();
            }
            if (oldSettingValues == null)
            {
                oldSettingValues = new Dictionary<string, object>();
            }
            
            if (Instance != null)
            {
                Logger.LogError("Only 1 settings menu should ever exist at a time!", gameObject.name);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            settings.Clear();
            oldSettingValues.Clear();
        }

        /// <summary>
        /// Refresh all settings/sync.
        /// </summary>
        public void RefreshDisplay()
        {
            // NOTE: currently only respects visibility.
            foreach (var s in settings)
            {
                Refresh(s.Value);
            }
        }
        private void Refresh(SettingsUIElement e)
        {
            if (!e.SettingVisible) e.gameObject.SetActive(false);
            else e.gameObject.SetActive(true);
        }

        public void AddSetting<T>(EnumSetting<T> setting)
            where T : System.Enum
        {
            DropdownSettingsUI ui = (DropdownSettingsUI)Instantiate(dropdownPrefab);
            if (ui != null && TryAdd(ui, setting)) // Add null check
            {
                ui.Hook(setting, new List<string>(Helpers.GetNames(typeof(T))), System.Convert.ToInt32(setting.Value));
                Refresh(ui);
            }
        }

        public void AddSetting(FloatSetting setting)
        {
            TextInputSettingsUI ui = (TextInputSettingsUI)Instantiate(floatPrefab);
            if (ui != null && TryAdd(ui, setting)) // Add null check
            {
                ui.Hook(setting, setting.Value);
                Refresh(ui);
            }
        }

        public void AddSetting(BoolSetting setting)
        {
            ToggleSettingsUI ui = (ToggleSettingsUI)Instantiate(boolPrefab);
            if (ui != null && TryAdd(ui, setting)) // Add null check
            {
                ui.Hook(setting, setting.Value);
                Refresh(ui);
            }
        }

        public void AddSetting(IntByStringSetting setting, List<string> options)
        {
            DropdownSettingsUI ui = (DropdownSettingsUI)Instantiate(dropdownPrefab);
            if (ui != null) // Add null check
            {
                setting.UpdateOptions(options);
                if (TryAdd(ui, setting))
                {
                    ui.Hook(setting, options, setting.Value);
                    Refresh(ui);
                }
            }
        }

       
        public void AddSetting(IntSetting setting)
        {
            TextInputSettingsUI ui = (TextInputSettingsUI)Instantiate(intPrefab);
            if (ui!=null && setting!=null && TryAdd(ui, setting))
            {
                ui.Hook(setting, setting.Value);
                Refresh(ui);
            }

        }
     
        /// <summary>
        /// Add the set of settings. 
        /// </summary>
        /// <param name="registerableSettings"></param>
        /// <param name="user">Hook the set of settings with the input user.</param>
        public void AddSetting(ISettingsContainer registerableSettings, ISettingsUser user = null)
        {
            // Register a group of settings (probably calls the specific adds here).

            registerableSettings.Hook(user);
            registerableSettings.RegisterMenuSettings();
        }

        private bool TryAdd(SettingsUIElement e, RuntimeSetting s)
        {
            if (TryRegister(e, s)) return true;
            // A rejected row must never retain the prefab's Setting 1 / Option A UI.
            if (e != null) { e.gameObject.SetActive(false); Destroy(e.gameObject); }
            return false;
        }

        private bool TryRegister(SettingsUIElement e, RuntimeSetting s)
        {
            // FIRST: Check if s is null
            if (s == null)
            {
                Logger.LogError("TryAdd: RuntimeSetting 's' is NULL!");
                return false;
            }

            // SECOND: Check if e is null
            if (e == null)
            {
                Logger.LogError("TryAdd: SettingsUIElement 'e' is NULL!");
                return false;
            }

            // Ensure dictionaries are initialized
            if (settings == null)
            {
                settings = new Dictionary<string, SettingsUIElement>();
            }
            if (oldSettingValues == null)
            {
                oldSettingValues = new Dictionary<string, object>();
            }

            // Try to get name with error handling
            string settingName;
            try
            {
                settingName = s.Name;
                if (string.IsNullOrWhiteSpace(settingName))
                {
                    Logger.LogError($"Setting of type {s.GetType().Name} has null/empty name!");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Exception accessing s.Name for type {s.GetType().Name}: {ex.Message}");
                return false;
            }

            // Check for duplicates
            if (settings.ContainsKey(settingName))
            {
                Logger.LogWarning($"Duplicate setting '{settingName}'. Skipping.");
                return false;
            }

            // Add to settings dictionary
            settings.Add(settingName, e);

            // Try to register in oldSettingValues
            if (!oldSettingValues.ContainsKey(settingName))
            {
                try
                {
                    object value = s.GetValue();
                    oldSettingValues.Add(settingName, value);
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"Exception in GetValue/Add for '{settingName}': {ex.Message}\n{ex.StackTrace}");
                    return false;
                }
            }

            return true;
        }

        public void RemoveSetting(string name)
        {
            if (!settings.ContainsKey(name)) return;
            if (settings[name] != null) Destroy(settings[name].gameObject);
            settings.Remove(name);
        }
        public void RemoveSetting(RuntimeSetting setting)
        {
            // Another provider with the same name must not remove the accepted row.
            if (setting != null && settings.TryGetValue(setting.Name, out var row)
                && row != null && row.IsBoundTo(setting)) RemoveSetting(setting.Name);
        }
        public void RemoveSetting(ISettingsContainer registerableSettings)
        {
            registerableSettings.DeregisterMenuSettings();
        }

        private SettingsUIElement Instantiate(SettingsUIElement uiPrefab)
        {
            if (uiPrefab == null)
            {
                Logger.LogWarning("SettingsManager: UI prefab is null. Settings UI will not be created.");
                return null;
            }
            if (settingsMenuContent == null)
            {
                Logger.LogWarning("SettingsManager: settingsMenuContent is null. Settings UI will not be created.");
                return null;
            }
            SettingsUIElement s = Instantiate(uiPrefab, settingsMenuContent.transform, false);
            s.gameObject.transform.localScale = Vector3.one;
            return s;
        }

        /// <summary>
        /// Get the current settings state (can be quite expensive).
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, object> GetCompleteSettingsState()
        {
            Dictionary<string, object> o = new Dictionary<string, object>();
            foreach (var s in settings)
            {
                o.Add(s.Value.Name, s.Value.SettingValueObject);
            }

            return o;
        }
        /// <summary>
        /// Apply any changes in settings.
        /// </summary>
        public void Apply()
        {
            HashSet<ISettingsUser> restartableSolutions = new HashSet<ISettingsUser>();
            var current = GetCompleteSettingsState();

            // We restart applicable setting users only once. 
            foreach (var s in current)
            {
                bool dirty = settings.ContainsKey(s.Key) && oldSettingValues.ContainsKey(s.Key) && !oldSettingValues[s.Key].Equals(s.Value);
                if (dirty)
                {
                    OnSettingChanged?.Invoke(s.Key);
                    restartableSolutions.Add(settings[s.Key].Restartable);
                }
            }

            // Does not respect any ordering (by the way)...
            foreach (var s in restartableSolutions)
            {
                if (s == null) continue;

                s.OnSettingsApplied();
            }

            // Synchronize the old state
            oldSettingValues = GetCompleteSettingsState();
        }

        /// <summary>
        /// Returns true if the setting with the input name exists.
        /// </summary>
        /// <param name="settingName"></param>
        /// <returns></returns>
        public static bool HasSetting(string settingName)
        {
            return settings.ContainsKey(settingName);
        }

        public static float GetFloat(string settingName, float defaultValue)
        {
            return GetValue(settingName, defaultValue);
        }
        public static int GetInt(string settingName, int defaultValue)
        {
            return GetValue(settingName, defaultValue);
        }
        public static bool GetBool(string settingName, bool defaultValue)
        {
            return GetValue(settingName, defaultValue);
        }
        /// <summary>
        /// Returns the setting value given the input name.
        /// <para>Returns defaultValue if the setting does not exist or is null.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="settingName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetValue<T>(string settingName, T defaultValue)
        {
            if (HasSetting(settingName))
            {
                try
                {
                    object o = settings[settingName].SettingValueObject;
                    return o == null ? defaultValue : (T)o;
                }
                catch (System.InvalidCastException ex)
                {
                    UnityEngine.Debug.LogError(settingName + " is not of type " + typeof(T).Name + "\n" + ex.Message);
                }
            }

            return defaultValue;
        }

        public static void SetInt(string settingName, int value)
        {
            SetValue(settingName, value);
        }
        public static void SetFloat(string settingName, float value)
        {
            SetValue(settingName, value);
        }
        public static void SetBool(string settingName, bool value)
        {
            SetValue(settingName, value);
        }
        public static void SetValue(string settingName, object value)
        {
            if (HasSetting(settingName))
            {
                settings[settingName].SetValue(value);
            }
            else
            {
                Logger.LogWarning("Setting " + settingName + " does not currently exist in the SettingsManager...", value);
            }
        }

    }
}
