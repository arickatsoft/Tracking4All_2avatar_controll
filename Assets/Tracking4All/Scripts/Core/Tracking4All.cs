using UnityEngine;

namespace Tracking4All
{
    [DefaultExecutionOrder(-1000)]
    /// <summary>
    /// Tracking4All manager. Exactly one should be in the scene at all times
    /// </summary>
    public class Tracking4All : MonoBehaviour
    {
        /// <summary>
        /// Access the singleton instance of Tracking4All.
        /// </summary>
        public static Tracking4All Instance;

        [SerializeField] private float providerAliveThreshold = 1f; // How long can a provider pause before its considered dead/asleep by default.
        /// <summary>
        /// Returns true if the provider is considered alive.
        /// </summary>
        /// <param name="timeSinceLastUpdate">The time since update value from the provider. Not absolute time.</param>
        /// <returns></returns>
        public bool IsProviderLive(float timeSinceLastUpdate)
        {
            return timeSinceLastUpdate < providerAliveThreshold;
        }
        public float ProviderAliveThreshold => providerAliveThreshold;

        // Screen orientation.
        public delegate void ScreenOrientationChanged(ScreenOrientation last, ScreenOrientation current);
        public event ScreenOrientationChanged OnScreenOrientationChanged;
        public ScreenOrientation ScreenOrientation { get; private set; }
        private ScreenOrientation lastOrientation;

        private static System.Diagnostics.Stopwatch watch;
        /// <summary>
        /// Get the elapsed time in seconds since Tracking4All was initialized.
        /// </summary>
        /// <returns></returns>
        public static float GetElapsedTime()
        {
            return (float)watch.Elapsed.TotalSeconds;
        }

        // REMOVED: Avatar generation property
        // public GameObject GENERATED { get; protected set; }

        public float FPS { get; protected set; } = 0;
        private int fpsCounter;
        private float secondTimer;
        public delegate void OnFPSUpdate(float fps);
        public event OnFPSUpdate FrameRateReadingUpdate;

        private void Awake()
        {
            Application.targetFrameRate = 120;

            if (Instance != null)
            {
                Logger.LogError("There should only be one Tracking4All script in a scene at a time.");
                return;
            }

            lastOrientation = UnityEngine.Screen.orientation;
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            // REMOVED: Avatar generation code
            // GENERATED = new GameObject("--Tracking4All Generated--");

            Instance = this;
            gameObject.transform.parent = null;
            DontDestroyOnLoad(gameObject);
            Logger.LogKey("Starting...");
        }

        private void Update()
        {
            ScreenOrientation current = UnityEngine.Screen.orientation;
            ScreenOrientation = current;

            if (lastOrientation != current)
            {
                OnScreenOrientationChanged?.Invoke(lastOrientation, current);
            }

            lastOrientation = current;

            ++fpsCounter;
            secondTimer += Time.deltaTime;
            if (secondTimer >= 1f)
            {
                FPS = fpsCounter / secondTimer;
                secondTimer = 0f;
                fpsCounter = 0;
                FrameRateReadingUpdate?.Invoke(FPS);
            }
        }
    }
}