using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Displays real-time network communication logs in the top-left corner of the screen.
    /// Shows connection status, data transfer, and debugging information.
    /// </summary>
    public class NetworkLogDisplay : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject logPanel;
        [SerializeField] private Text logText;
        [SerializeField] private ScrollRect scrollRect;
        
        [Header("Log Settings")]
        [SerializeField] private int maxLogLines = 20;
        [SerializeField] private float logLineFadeTime = 10f;
        [SerializeField] private bool autoScroll = true;
        
        private List<string> logMessages = new List<string>();
        private Queue<float> logTimestamps = new Queue<float>();
        
        // Singleton instance
        public static NetworkLogDisplay Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLogDisplay();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Clear any existing logs and show startup message
            ClearLogs();
            AddLog("NetworkLogDisplay initialized", Color.green);
            
            // Start the log cleanup coroutine
            StartCoroutine(CleanupOldLogs());
        }
        
        private void InitializeLogDisplay()
        {
            // Create UI if not assigned
            if (logPanel == null || logText == null)
            {
                CreateLogUI();
            }
            
            // Ensure the log is visible
            if (logPanel != null)
            {
                logPanel.SetActive(true);
            }
        }
        
        private void CreateLogUI()
        {
            // Since you already have a programmatically created logging panel,
            // we'll find and use the existing one or create a simple fallback
            
            // Try to find existing log components first
            Text[] existingTexts = FindObjectsOfType<Text>();
            foreach (Text text in existingTexts)
            {
                if (text.name.ToLower().Contains("log") || text.name.ToLower().Contains("debug"))
                {
                    logText = text;
                    logPanel = text.transform.parent?.gameObject;
                    UnityEngine.Debug.Log($"[NetworkLogDisplay] Found existing log UI: {text.name}");
                    return;
                }
            }
            
            // If no existing log UI found, create a minimal one
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                UnityEngine.Debug.LogError("[NetworkLogDisplay] No Canvas found in scene!");
                return;
            }
            
            // Create minimal log panel
            GameObject panelObj = new GameObject("NetworkLogPanel");
            panelObj.transform.SetParent(canvas.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.sizeDelta = new Vector2(500, 300);
            panelRect.anchoredPosition = new Vector2(10, -10);
            
            // Add dark background with opacity (matching your existing style)
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f); // Dark black with opacity
            
            // Create text component
            GameObject textObj = new GameObject("LogText");
            textObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            logText = textObj.AddComponent<Text>();
            logText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            logText.fontSize = 12;
            logText.color = Color.white;
            logText.alignment = TextAnchor.UpperLeft;
            
            logPanel = panelObj;
        }
        
        /// <summary>
        /// Add a log message with default white color
        /// </summary>
        public void AddLog(string message)
        {
            AddLog(message, Color.white);
        }
        
        /// <summary>
        /// Add a log message with specified color
        /// </summary>
        public void AddLog(string message, Color color)
        {
            if (logText == null) return;
            
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            string formattedMessage = $"<color=#{colorHex}>[{timestamp}] {message}</color>";
            
            logMessages.Add(formattedMessage);
            logTimestamps.Enqueue(Time.time);
            
            // Remove old messages if we exceed max lines
            while (logMessages.Count > maxLogLines)
            {
                logMessages.RemoveAt(0);
                if (logTimestamps.Count > 0)
                    logTimestamps.Dequeue();
            }
            
            UpdateLogDisplay();
        }
        
        /// <summary>
        /// Add a network-specific log with formatted styling
        /// </summary>
        public void AddNetworkLog(string category, string message, bool isError = false)
        {
            Color color = isError ? Color.red : Color.cyan;
            string formattedMessage = $"[NET-{category}] {message}";
            AddLog(formattedMessage, color);
        }
        
        /// <summary>
        /// Add a host-specific log message
        /// </summary>
        public void AddHostLog(string message)
        {
            AddLog($"[HOST] {message}", Color.green);
        }
        
        /// <summary>
        /// Add a client-specific log message
        /// </summary>
        public void AddClientLog(string message)
        {
            AddLog($"[CLIENT] {message}", Color.yellow);
        }
        
        /// <summary>
        /// Add a data transfer log message
        /// </summary>
        public void AddDataLog(string direction, string dataType, int bytes = 0)
        {
            Color color = direction.ToUpper() == "SENT" ? Color.blue : Color.magenta;
            string sizeInfo = bytes > 0 ? $" ({bytes} bytes)" : "";
            AddLog($"[DATA-{direction.ToUpper()}] {dataType}{sizeInfo}", color);
        }
        
        /// <summary>
        /// Clear all log messages
        /// </summary>
        public void ClearLogs()
        {
            logMessages.Clear();
            logTimestamps.Clear();
            UpdateLogDisplay();
        }
        
        private void UpdateLogDisplay()
        {
            if (logText == null) return;
            
            logText.text = string.Join("\n", logMessages);
            
            // Auto-scroll to bottom if enabled
            if (autoScroll && scrollRect != null)
            {
                StartCoroutine(ScrollToBottom());
            }
        }
        
        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = new Vector2(0, 0);
            }
        }
        
        private IEnumerator CleanupOldLogs()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f); // Check every second
                
                float currentTime = Time.time;
                while (logTimestamps.Count > 0 && (currentTime - logTimestamps.Peek()) > logLineFadeTime)
                {
                    logTimestamps.Dequeue();
                    if (logMessages.Count > 0)
                    {
                        logMessages.RemoveAt(0);
                    }
                }
                
                if (logMessages.Count != logTimestamps.Count)
                {
                    // Sync the collections if they get out of sync
                    int minCount = Mathf.Min(logMessages.Count, logTimestamps.Count);
                    while (logMessages.Count > minCount) logMessages.RemoveAt(0);
                    while (logTimestamps.Count > minCount) logTimestamps.Dequeue();
                }
                
                UpdateLogDisplay();
            }
        }
        
        /// <summary>
        /// Static methods for easy access from other scripts
        /// </summary>
        public static void LogNetwork(string category, string message, bool isError = false)
        {
            if (Instance != null)
                Instance.AddNetworkLog(category, message, isError);
        }
        
        public static void LogHost(string message)
        {
            if (Instance != null)
                Instance.AddHostLog(message);
        }
        
        public static void LogClient(string message)
        {
            if (Instance != null)
                Instance.AddClientLog(message);
        }
        
        public static void LogData(string direction, string dataType, int bytes = 0)
        {
            if (Instance != null)
                Instance.AddDataLog(direction, dataType, bytes);
        }
        
        public static void Log(string message, Color color)
        {
            if (Instance != null)
                Instance.AddLog(message, color);
        }
        
        public static void Log(string message)
        {
            if (Instance != null)
                Instance.AddLog(message);
        }
    }
}