using UnityEngine;

/// <summary>
/// Prevents the device from going to sleep and keeps screen brightness at full.
/// Essential for continuous UDP pose streaming on mobile devices.
/// IMPORTANT: This requires WAKE_LOCK permission on Android (added via AndroidPermissionsPostProcessor)
///
/// This script uses [RuntimeInitializeOnLoadMethod] to automatically run at app startup,
/// similar to how commercial games handle screen wake management.
/// No need to manually attach to a GameObject!
/// </summary>
public class KeepScreenAwake : MonoBehaviour
{
    private static KeepScreenAwake instance;

    [Header("Screen Settings")]
    [Tooltip("Prevent device from going to sleep")]
    [SerializeField] private bool preventSleep = true;

    [Tooltip("Keep screen brightness at maximum (mobile only)")]
    [SerializeField] private bool keepFullBrightness = true;

    [Header("Status (Read-Only)")]
    [SerializeField] private bool isAwakeModeActive = false;

    /// <summary>
    /// Automatically initializes at app startup (similar to commercial games)
    /// This runs BEFORE any scene loads, ensuring screen stays awake from the start
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        Debug.Log("[KeepScreenAwake] 🚀 Auto-initializing screen wake system...");

        // Create a persistent GameObject that survives scene changes
        GameObject screenAwakeObj = new GameObject("ScreenAwakeManager");
        instance = screenAwakeObj.AddComponent<KeepScreenAwake>();
        DontDestroyOnLoad(screenAwakeObj);

        Debug.Log("[KeepScreenAwake] ✅ Screen wake system initialized and set to persist across scenes");
    }

    #if UNITY_ANDROID
    private AndroidJavaObject currentActivity;
    private AndroidJavaObject window;
    private bool androidBrightnessSet = false;
    #endif

    private void Awake()
    {
        // Apply settings as early as possible
        Debug.Log("[KeepScreenAwake] Awake() called - applying settings immediately");
        ApplyScreenSettings();
    }

    private void Start()
    {
        // Reapply to ensure settings stick
        Debug.Log("[KeepScreenAwake] Start() called - reapplying settings");
        ApplyScreenSettings();
    }

    private void OnEnable()
    {
        Debug.Log("[KeepScreenAwake] OnEnable() called - applying settings");
        ApplyScreenSettings();
    }

    private void ApplyScreenSettings()
    {
        if (preventSleep)
        {
            // Prevent screen from dimming or sleeping
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            isAwakeModeActive = true;

            Debug.Log("[KeepScreenAwake] ✅ Sleep mode DISABLED - Screen will stay awake");
            Debug.Log("[KeepScreenAwake] Screen.sleepTimeout = NeverSleep");

            #if UNITY_ANDROID
            Debug.Log("[KeepScreenAwake] 📱 Mobile platform detected - WAKE_LOCK permission required");
            #endif
        }
        else
        {
            // Restore default behavior
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            isAwakeModeActive = false;

            Debug.Log("[KeepScreenAwake] Sleep mode restored to system default");
        }

        if (keepFullBrightness)
        {
            // Set brightness to maximum (1.0 = 100%)
            Screen.brightness = 1.0f;
            Debug.Log("[KeepScreenAwake] ✅ Screen brightness set to 100%");

            #if UNITY_ANDROID
            // Only call Android-specific code when actually running on Android device
            if (Application.platform == RuntimePlatform.Android)
            {
                SetAndroidScreenBrightness();
            }
            #elif UNITY_IOS
            Debug.Log("[KeepScreenAwake] 📱 iOS: Brightness setting is supported");
            #endif
        }
    }

    #if UNITY_ANDROID
    /// <summary>
    /// Sets screen brightness using Android native API via FLAG_KEEP_SCREEN_ON and WindowManager.LayoutParams
    /// This is more reliable than Unity's Screen.brightness API on Android
    /// </summary>
    private void SetAndroidScreenBrightness()
    {
        try
        {
            Debug.Log("[KeepScreenAwake] 📱 Android: Applying native screen brightness settings...");

            // Get the current Unity activity using the proper method
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }

            if (currentActivity == null)
            {
                Debug.LogError("[KeepScreenAwake] ❌ Failed to get current Android activity");
                return;
            }

            // Run on UI thread (required for window operations)
            Debug.Log("[KeepScreenAwake] 📱 Android: About to call runOnUiThread...");
            currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    Debug.Log("[KeepScreenAwake] 📱 Android: Inside UI thread");

                    // Get the window
                    window = currentActivity.Call<AndroidJavaObject>("getWindow");

                    if (window == null)
                    {
                        Debug.LogError("[KeepScreenAwake] ❌ Failed to get window");
                        return;
                    }

                    Debug.Log("[KeepScreenAwake] ✅ Android: Got window object");

                    // Get window flags constant
                    AndroidJavaClass layoutParamsClass = new AndroidJavaClass("android.view.WindowManager$LayoutParams");
                    int FLAG_KEEP_SCREEN_ON = layoutParamsClass.GetStatic<int>("FLAG_KEEP_SCREEN_ON");

                    Debug.Log($"[KeepScreenAwake] 📱 Android: FLAG_KEEP_SCREEN_ON = {FLAG_KEEP_SCREEN_ON}");

                    // Add FLAG_KEEP_SCREEN_ON to keep screen awake and prevent dimming
                    window.Call("addFlags", FLAG_KEEP_SCREEN_ON);
                    Debug.Log("[KeepScreenAwake] ✅ Android: Added FLAG_KEEP_SCREEN_ON to window");

                    // Get layout parameters
                    AndroidJavaObject layoutParams = window.Call<AndroidJavaObject>("getAttributes");

                    if (layoutParams != null)
                    {
                        Debug.Log("[KeepScreenAwake] ✅ Android: Got layout params");

                        // Get current brightness for debugging
                        float currentBrightness = layoutParams.Get<float>("screenBrightness");
                        Debug.Log($"[KeepScreenAwake] 📱 Android: Current brightness = {currentBrightness}");

                        // Set screen brightness to maximum (1.0f = 100%, -1.0f = system default)
                        // BRIGHTNESS_OVERRIDE_FULL = 1.0f
                        layoutParams.Set<float>("screenBrightness", 1.0f);
                        Debug.Log("[KeepScreenAwake] ✅ Android: Set screenBrightness to 1.0 (100%)");

                        // Apply the updated layout parameters
                        window.Call("setAttributes", layoutParams);
                        Debug.Log("[KeepScreenAwake] ✅ Android: Applied window attributes via setAttributes()");

                        // Verify it was set
                        float newBrightness = layoutParams.Get<float>("screenBrightness");
                        Debug.Log($"[KeepScreenAwake] ✅ Android: Verified new brightness = {newBrightness}");

                        androidBrightnessSet = true;
                    }
                    else
                    {
                        Debug.LogError("[KeepScreenAwake] ❌ Failed to get window layout parameters");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[KeepScreenAwake] ❌ Error in UI thread: {e.Message}\n{e.StackTrace}");
                }
            }));

            Debug.Log("[KeepScreenAwake] 📱 Android: runOnUiThread call completed");

            Debug.Log("[KeepScreenAwake] ✅ Android: Native brightness settings applied successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[KeepScreenAwake] ❌ Failed to set Android brightness: {e.Message}\n{e.StackTrace}");
            Debug.LogWarning("[KeepScreenAwake] Falling back to Unity's Screen.brightness API");
        }
    }

    /// <summary>
    /// Restores default Android screen brightness settings
    /// </summary>
    private void ClearAndroidScreenBrightness()
    {
        if (!androidBrightnessSet) return;

        try
        {
            if (currentActivity == null || window == null)
            {
                Debug.LogWarning("[KeepScreenAwake] Cannot clear Android brightness - references null");
                return;
            }

            currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    // Get window flags constant
                    AndroidJavaClass layoutParamsClass = new AndroidJavaClass("android.view.WindowManager$LayoutParams");
                    int FLAG_KEEP_SCREEN_ON = layoutParamsClass.GetStatic<int>("FLAG_KEEP_SCREEN_ON");

                    // Remove FLAG_KEEP_SCREEN_ON
                    window.Call("clearFlags", FLAG_KEEP_SCREEN_ON);
                    Debug.Log("[KeepScreenAwake] ✅ Android: Removed FLAG_KEEP_SCREEN_ON");

                    // Get layout parameters
                    AndroidJavaObject layoutParams = window.Call<AndroidJavaObject>("getAttributes");

                    if (layoutParams != null)
                    {
                        // Set brightness to system default (-1.0f)
                        layoutParams.Set<float>("screenBrightness", -1.0f);
                        window.Call("setAttributes", layoutParams);
                        Debug.Log("[KeepScreenAwake] ✅ Android: Restored default brightness");
                    }

                    androidBrightnessSet = false;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[KeepScreenAwake] ❌ Error clearing brightness: {e.Message}");
                }
            }));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[KeepScreenAwake] ❌ Failed to clear Android brightness: {e.Message}");
        }
    }
    #endif

    private void OnDisable()
    {
        // Restore default sleep behavior when component is disabled
        if (isAwakeModeActive)
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Debug.Log("[KeepScreenAwake] Component disabled - Sleep mode restored to system default");
        }

        #if UNITY_ANDROID
        ClearAndroidScreenBrightness();
        #endif
    }

    private void OnDestroy()
    {
        // Clean up when destroyed
        if (isAwakeModeActive)
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Debug.Log("[KeepScreenAwake] Component destroyed - Sleep mode restored to system default");
        }

        #if UNITY_ANDROID
        ClearAndroidScreenBrightness();
        #endif
    }

    // Public API for runtime control
    public void SetPreventSleep(bool prevent)
    {
        preventSleep = prevent;
        ApplyScreenSettings();
    }

    public void SetKeepFullBrightness(bool keepFull)
    {
        keepFullBrightness = keepFull;
        ApplyScreenSettings();
    }

    public bool IsAwakeModeActive => isAwakeModeActive;
}
