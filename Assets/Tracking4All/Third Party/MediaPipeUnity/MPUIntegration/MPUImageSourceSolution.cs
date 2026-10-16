using System.Collections;
using Mediapipe.Unity;
using UnityEngine;

namespace Tracking4All
{
    // TODO: abstract out a generic IBackendSolution...

    [DefaultExecutionOrder(-900)]
    /// <summary>
    /// A custom graph runner to interface with MPU.
    /// </summary>
    /// <typeparam name="GRAPH_RUNNER">The concrete type of graph runner.</typeparam>
    /// <typeparam name="MODEL_SETTINGS">The concrete type of model settings for the graph.</typeparam>
    public abstract class MPUImageSourceSolution<GRAPH_RUNNER, MODEL_SETTINGS> : Mediapipe.Unity.Sample.ImageSourceSolution<GRAPH_RUNNER>,
        ISettingsUser, ISolutionSettings
        where GRAPH_RUNNER : Mediapipe.Unity.Sample.GraphRunner
        where MODEL_SETTINGS : MPUModelSettings
    {
        [SerializeField] protected LifecycleProvider waitForLifecycle;
        [SerializeField] protected ImageFrameProvider imageFrameProvider;
        [SerializeField] protected MODEL_SETTINGS modelSettings;

        private bool stopped = true;
        protected bool isRestarting = false;
        private string originalName;

        private bool shouldWaitForCustomLifecycle = false;

        public IVideoSourceProperties VideoSourceProperties { get { return this.videoSourceProperties; } }
        private IVideoSourceProperties videoSourceProperties;
        public int Width => VideoSourceProperties.Width;
        public int Height => VideoSourceProperties.Height;
        public bool PerspectiveFlip => modelSettings.perspectiveFlip.Value;
        public bool Mirror => modelSettings.mirror.Value;

        protected MPUImageSourceAdapter sourceAdapter;

        // Allows alternative inference to share the selected camera without opening it twice.
        public IImageFrameProvider CameraFrames => imageFrameProvider.Provider;

        protected virtual void Awake()
        {
            sourceAdapter = new MPUImageSourceAdapter(1, imageFrameProvider.Provider, -1);
            originalName = gameObject.name;
        }


        protected void OnEnable()
        {
            flag = false;
            isRestarting = false;
            shouldWaitForCustomLifecycle = waitForLifecycle.HasInterface;
            if (shouldWaitForCustomLifecycle)
                waitForLifecycle.Provider.OnUpdated += Provider_OnUpdated;

            // Only add settings if SettingsManager exists
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.AddSetting(modelSettings, this);
            }

            sourceAdapter.OnReady += SourceAdapter_OnReady;
            sourceAdapter.OnDisposed += SourceAdapter_OnDisposed;

        }
        protected void OnDisable()
        {
            // Unity cancels this object's coroutines when its root is disabled.
            // Reset our state too, so a ready camera can restart the graph next time.
            StopAllCoroutines();
            Stop();
            flag = false;
            isRestarting = false;
            if (shouldWaitForCustomLifecycle)
                waitForLifecycle.Provider.OnUpdated -= Provider_OnUpdated;
            // Only remove settings if SettingsManager exists
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.RemoveSetting(modelSettings);
            }

            sourceAdapter.OnReady -= SourceAdapter_OnReady;
            sourceAdapter.OnDisposed -= SourceAdapter_OnDisposed;
        }

        private bool flag;
        private float waitStarted;
        public override bool OptionalWait()
        {
            // A disconnected consumer must never freeze camera rendering/detection.
            return flag && Time.unscaledTime - waitStarted < .1f;
        }
        private void Provider_OnUpdated(int group)
        {
            flag = false;
        }
        protected override void OnDetectionComplete()
        {
            base.OnDetectionComplete();
            if (shouldWaitForCustomLifecycle) { flag = true; waitStarted = Time.unscaledTime; }
        }

        public float lastDetectTime;
        protected override void OnStartSyncDetection()
        {
            base.OnStartSyncDetection();
            lastDetectTime = Helpers.GetTime();
        }
        protected override ImageSource GetImageSource()
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

        protected override void ReadyToRun()
        {
            base.ReadyToRun();
        }

        /// <summary>
        /// Runs when the solution is starting.
        /// </summary>
        public override void Play()
        {
            if (!isActiveAndEnabled || !stopped || videoSourceProperties?.IsPrepared != true
                || bootstrap == null || !bootstrap.isFinished) return;

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

            graphRunner.Stop();
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
            // This solution owns an adapter, not the sample's global ImageSource.
            // Stop() leaves its webcam alive, so waiting for that webcam to dispose
            // would never finish. Restart against the same ready source next frame.
            flag = false;
            yield return null;
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
