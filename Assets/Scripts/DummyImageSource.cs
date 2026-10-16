using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Provides a minimal dummy texture to keep a pose tracking graph running
    /// without actually processing camera data. Useful when pose data comes from UDP.
    /// 
    /// This creates a simple WebCamTexture-like interface without needing a real camera.
    /// </summary>
    public class DummyImageSource : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("How many FPS to pretend we're providing")]
        [SerializeField] private float targetFPS = 30f;
        
        [Tooltip("Dummy texture dimensions (keep small to save memory)")]
        [SerializeField] private int textureWidth = 16;
        [SerializeField] private int textureHeight = 16;
        
        [Header("Output")]
        [Tooltip("The dummy texture (created automatically)")]
        public Texture2D outputTexture;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        // Frame tracking
        private float lastFrameTime;
        private int frameCount;
        
        // WebCamTexture-like properties for compatibility
        public int width => textureWidth;
        public int height => textureHeight;
        public bool isPlaying { get; private set; }
        
        private void Start()
        {
            CreateDummyTexture();
            Play();
            
            if (showDebugInfo)
            {
                UnityEngine.Debug.Log($"<color=yellow>[DummyImageSource] Started with {textureWidth}x{textureHeight} dummy texture</color>");
            }
        }
        
        private void CreateDummyTexture()
        {
            // Create a minimal texture (16x16 uses very little memory)
            outputTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
            
            // Fill with gray (neutral color)
            Color[] pixels = new Color[textureWidth * textureHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 0.5f); // Gray
            }
            outputTexture.SetPixels(pixels);
            outputTexture.Apply();
            
            // Don't destroy this texture
            DontDestroyOnLoad(outputTexture);
        }
        
        public void Play()
        {
            isPlaying = true;
            UnityEngine.Debug.Log("[DummyImageSource] Started playing");
        }
        
        public void Stop()
        {
            isPlaying = false;
            UnityEngine.Debug.Log("[DummyImageSource] Stopped playing");
        }
        
        private void Update()
        {
            if (!isPlaying) return;
            
            // Simulate frame timing
            float targetInterval = 1f / targetFPS;
            
            if (Time.time - lastFrameTime >= targetInterval)
            {
                lastFrameTime = Time.time;
                frameCount++;
                
                if (showDebugInfo && frameCount % 60 == 0)
                {
                    UnityEngine.Debug.Log($"<color=yellow>[DummyImageSource] Dummy frames: {frameCount}</color>");
                }
            }
        }
        
        /// <summary>
        /// Get the dummy texture (compatible with WebCamTexture usage)
        /// </summary>
        public Texture2D GetTexture()
        {
            return outputTexture;
        }
        
        /// <summary>
        /// Get raw pixel data (if needed by pose tracking)
        /// </summary>
        public Color[] GetPixels()
        {
            return outputTexture.GetPixels();
        }
        
        /// <summary>
        /// Get raw byte data (if needed by pose tracking)
        /// </summary>
        public byte[] GetRawTextureData()
        {
            return outputTexture.GetRawTextureData();
        }
        
        private void OnDestroy()
        {
            // Clean up
            if (outputTexture != null)
            {
                Destroy(outputTexture);
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 430, 300, 120));
            GUILayout.Label("<b>Dummy Image Source</b>", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"Frames provided: {frameCount}");
            GUILayout.Label($"Target FPS: {targetFPS:F1}");
            GUILayout.Label($"Status: {(isPlaying ? "Running" : "Stopped")}");
            GUILayout.Label($"Texture: {textureWidth}x{textureHeight}");
            
            if (GUILayout.Button(isPlaying ? "Stop" : "Start"))
            {
                if (isPlaying) Stop();
                else Play();
            }
            
            GUILayout.EndArea();
        }
    }
}
