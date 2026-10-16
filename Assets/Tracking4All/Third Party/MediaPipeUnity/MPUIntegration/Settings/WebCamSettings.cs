using System.Collections;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// The camera settings to configure mpu.
    /// </summary>
    [System.Serializable]
    public class WebCamSettings : ISettingsContainer, IWebcamSettings
    {
        public IntByStringSetting cameraIndex; // 0
        public IntSetting width; // 1920
        public IntSetting height; // 1080
        public FloatSetting frameRate; // 30

        int IWebcamSettings.WebcamDeviceIndex => cameraIndex.Value;
        int IVideoSourceSettings.RequestedWidth => width.Value;
        int IVideoSourceSettings.RequestedHeight => height.Value;
        int IVideoSourceSettings.RequestedFrameRate => (int)frameRate.Value;

        private Coroutine delayedAdd = null;

        public virtual void RegisterMenuSettings()
        {
            deactivated = false;
            delayedAdd = Tracking4All.Instance.StartCoroutine(WaitForCameras());
            SettingsManager.Instance.AddSetting(width);
            SettingsManager.Instance.AddSetting(height);
            SettingsManager.Instance.AddSetting(frameRate);
        }
        public virtual void DeregisterMenuSettings()
        {
            deactivated = true;
            if (delayedAdd != null)
            {
                // The coroutine owner can be destroyed before the camera backend on shutdown.
                if (Tracking4All.Instance != null) Tracking4All.Instance.StopCoroutine(delayedAdd);
                delayedAdd = null;
            }
            SettingsManager.Instance.RemoveSetting(cameraIndex);
            SettingsManager.Instance.RemoveSetting(width);
            SettingsManager.Instance.RemoveSetting(height);
            SettingsManager.Instance.RemoveSetting(frameRate);
        }

        private bool deactivated = false;
        private IEnumerator WaitForCameras()
        {
            yield return new WaitUntil(() => deactivated || MPUHelpers.HasCameraOptions());
            if (deactivated == false)
            {
                var options = MPUHelpers.GetCameraOptions();
                if (options.Count == 0)
                {
                    Logger.LogWarning("Failed to find any camera options!");
                }
                SettingsManager.Instance.AddSetting(cameraIndex, options);
            }

            delayedAdd = null;
        }

        public void Hook(ISettingsUser restartable)
        {
            cameraIndex.Hook(restartable);
            width.Hook(restartable);
            height.Hook(restartable);
            frameRate.Hook(restartable);
        }
    }
}