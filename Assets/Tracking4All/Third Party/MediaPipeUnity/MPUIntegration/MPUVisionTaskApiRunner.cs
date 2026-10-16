// MPUVisionTaskApiRunner
// (C) 2024-2025 G8gaming Ltd.
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using System.Collections;
using UnityEngine;

namespace Tracking4All
{
    [DefaultExecutionOrder(-900)]
    /// <summary>
    /// Base wrapper class for new vision API (similar in purpose to MPUImageSourceSolution), subject to massive changes.
    /// </summary>
    /// <typeparam name="TTask"></typeparam>
    public abstract class MPUVisionTaskApiRunner<TTask, MODEL_SETTINGS> : VisionTaskApiRunner<TTask>,
        ISettingsUser, ISolutionSettings
         where TTask : Mediapipe.Tasks.Vision.Core.BaseVisionTaskApi
        where MODEL_SETTINGS : MPUModelSettings
    {
        [SerializeField] protected ImageFrameProvider imageFrameProvider;
        [SerializeField] protected MODEL_SETTINGS modelSettings;

        private bool stopped = true;
        protected bool isRestarting = false;
        private string originalName;

        public IVideoSourceProperties VideoSourceProperties { get { return this.videoSourceProperties; } }
        private IVideoSourceProperties videoSourceProperties;
        public int Width => VideoSourceProperties.Width;
        public int Height => VideoSourceProperties.Height;
        public bool PerspectiveFlip => modelSettings.perspectiveFlip.Value;
        public bool Mirror => modelSettings.mirror.Value;

        protected MPUImageSourceAdapter sourceAdapter;

        protected virtual void Awake()
        {
            sourceAdapter = new MPUImageSourceAdapter(1, imageFrameProvider.Provider, -1);
            originalName = gameObject.name;
        }
        protected void OnEnable()
        {
            SettingsManager.Instance.AddSetting(modelSettings, this);

            sourceAdapter.OnReady += SourceAdapter_OnReady;
            sourceAdapter.OnDisposed += SourceAdapter_OnDisposed;
        }
        protected void OnDisable()
        {
            SettingsManager.Instance.RemoveSetting(modelSettings);

            sourceAdapter.OnReady -= SourceAdapter_OnReady;
            sourceAdapter.OnDisposed -= SourceAdapter_OnDisposed;
        }

        protected ImageSource GetImageSource()
        {
            return sourceAdapter.Get(0, 0);
        }

        private void SourceAdapter_OnReady(int group)
        {
            videoSourceProperties = imageFrameProvider.Provider.Get(group, 0).SourceProperties;
            if (stopped)
            {
                Play();
            }
        }
        private void SourceAdapter_OnDisposed(int group)
        {
            Logger.LogInfo("Stopping solution due to source webcam disposal.", this);
            if (!stopped) Stop();
        }

        /// <summary>
        /// Runs when the solution is starting.
        /// </summary>
        public override void Play()
        {
            if (videoSourceProperties?.IsPrepared != true) return;

            stopped = false;

            ApplySettings(modelSettings);

            base.Play();

            Tracking4All.Instance.OnScreenOrientationChanged += Instance_OnScreenOrientationChanged;
        }
        /// <summary>
        /// Runs when the solution is stopping.
        /// </summary>
        public override void Stop()
        {
            stopped = true;

            base.Stop();

            Tracking4All.Instance.OnScreenOrientationChanged -= Instance_OnScreenOrientationChanged;
        }

        /// <summary>
        /// Called when we want to apply settings.
        /// </summary>
        protected virtual void ApplySettings(MODEL_SETTINGS modelSettings)
        {
            UpdateModel(modelSettings);
        }
        /// <summary>
        /// Given the settings, update the graph values.
        /// </summary>
        /// <param name="modelSettings"></param>
        /// <param name="cameraSettings"></param>
        protected abstract void UpdateModel(MODEL_SETTINGS modelSettings);

        /// <summary>
        /// Apply all changes to the various settings/restart the solution.
        /// </summary>
        public virtual bool Restart()
        {
            if (isRestarting) return false;

            Logger.LogInfo("Restarting with configuration update.", gameObject.name);
            StartCoroutine(RestartInternal());

            return true;
        }
        private IEnumerator RestartInternal()
        {
            isRestarting = true;
            Stop();

            Logger.LogInfo("Waiting for image source to dispose…");
            yield return new WaitUntil(() => Mediapipe.Unity.Sample.ImageSourceProvider.ImageSource.isPrepared == false); // value that indicates we have successfully disposed
            yield return new WaitForSeconds(1f);
            Logger.LogInfo("Resuming detection.");

            Play();
            isRestarting = false;
        }

        private void Instance_OnScreenOrientationChanged(ScreenOrientation last, ScreenOrientation current)
        {
            Restart();
        }

        void ISettingsUser.OnSettingsApplied()
        {
            this.Restart();
        }

    }
}