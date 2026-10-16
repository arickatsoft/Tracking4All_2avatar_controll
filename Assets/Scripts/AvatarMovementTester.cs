using UnityEngine;
using Tracking4All;

namespace Tracking4All
{
    /// <summary>
    /// Manual avatar movement tester - paste recorded data and see avatar move
    /// </summary>
    public class AvatarMovementTester : MonoBehaviour
    {
        [Header("Avatar References")]
        public GameObject bryceAvatar;
        public GameObject remyAvatar;
        
        [Header("Test Data")]
        [TextArea(5, 15)]
        public string testNetworkData = "Paste your JSON data here";
        
        [Header("Test Controls")]
        public KeyCode applyTestDataKey = KeyCode.Space;
        public bool showTestInstructions = true;
        
        private NetworkDataLogger logger;
        
        void Start()
        {
            // Find the logger
            logger = FindObjectOfType<NetworkDataLogger>();
            
            // Auto-find avatars if not assigned
            if (bryceAvatar == null)
                bryceAvatar = GameObject.Find("Bryce");
            if (remyAvatar == null)
                remyAvatar = GameObject.Find("Remy");
                
            UnityEngine.Debug.Log("🧪 AVATAR MOVEMENT TESTER READY");
            UnityEngine.Debug.Log($"Press {applyTestDataKey} to apply test data");
            UnityEngine.Debug.Log("Or use the Inspector buttons for testing");
        }
        
        void Update()
        {
            if (Input.GetKeyDown(applyTestDataKey))
            {
                ApplyTestData();
            }
        }
        
        /// <summary>
        /// Apply the JSON data from the inspector text area
        /// </summary>
        [ContextMenu("Apply Test Data")]
        public void ApplyTestData()
        {
            if (string.IsNullOrEmpty(testNetworkData))
            {
                UnityEngine.Debug.LogWarning("No test data provided! Paste JSON in the testNetworkData field.");
                return;
            }
            
            if (logger == null)
            {
                UnityEngine.Debug.LogError("NetworkDataLogger not found!");
                return;
            }
            
            UnityEngine.Debug.Log("🎯 APPLYING TEST DATA TO AVATAR");
            UnityEngine.Debug.Log($"Data: {testNetworkData.Substring(0, Mathf.Min(100, testNetworkData.Length))}...");
            
            // Apply the data using the logger
            logger.ApplyNetworkDataToAvatar(testNetworkData);
        }
        
        /// <summary>
        /// Create animated test movement - makes avatar wave
        /// </summary>
        [ContextMenu("Test Wave Movement")]
        public void TestWaveMovement()
        {
            StartCoroutine(WaveMovementSequence());
        }
        
        /// <summary>
        /// Create walking test movement
        /// </summary>
        [ContextMenu("Test Walking Movement")]
        public void TestWalkingMovement()
        {
            StartCoroutine(WalkingMovementSequence());
        }
        
        /// <summary>
        /// Test specific pose - arms raised
        /// </summary>
        [ContextMenu("Test Arms Raised Pose")]
        public void TestArmsRaisedPose()
        {
            var testPose = CreateArmsRaisedPose();
            var jsonData = JsonUtility.ToJson(testPose);
            
            UnityEngine.Debug.Log("🙋 TESTING ARMS RAISED POSE");
            
            if (logger != null)
            {
                logger.ApplyNetworkDataToAvatar(jsonData);
            }
        }
        
        /// <summary>
        /// Test specific pose - crouching
        /// </summary>
        [ContextMenu("Test Crouching Pose")]
        public void TestCrouchingPose()
        {
            var testPose = CreateCrouchingPose();
            var jsonData = JsonUtility.ToJson(testPose);
            
            UnityEngine.Debug.Log("🧎 TESTING CROUCHING POSE");
            
            if (logger != null)
            {
                logger.ApplyNetworkDataToAvatar(jsonData);
            }
        }
        
        System.Collections.IEnumerator WaveMovementSequence()
        {
            UnityEngine.Debug.Log("👋 STARTING WAVE ANIMATION TEST");
            
            for (int i = 0; i < 10; i++)
            {
                var wavePose = CreateWavePose(i * 0.5f);
                var jsonData = JsonUtility.ToJson(wavePose);
                
                if (logger != null)
                {
                    logger.ApplyNetworkDataToAvatar(jsonData);
                }
                
                yield return new WaitForSeconds(0.2f);
            }
            
            UnityEngine.Debug.Log("✅ Wave animation test completed");
        }
        
        System.Collections.IEnumerator WalkingMovementSequence()
        {
            UnityEngine.Debug.Log("🚶 STARTING WALKING ANIMATION TEST");
            
            for (int i = 0; i < 20; i++)
            {
                var walkPose = CreateWalkingPose(i * 0.1f);
                var jsonData = JsonUtility.ToJson(walkPose);
                
                if (logger != null)
                {
                    logger.ApplyNetworkDataToAvatar(jsonData);
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            UnityEngine.Debug.Log("✅ Walking animation test completed");
        }
        
        NetworkDataLogger.SimulatedNetworkFrame CreateWavePose(float time)
        {
            return new NetworkDataLogger.SimulatedNetworkFrame
            {
                avatarName = "REMY", // Change to BRYCE if testing as host
                bodyPosition = new Vector3(0, 1.0f, 0),
                bodyRotation = Quaternion.Euler(0, Mathf.Sin(time) * 10f, 0),
                muscles = CreateWaveMuscles(time),
                timestamp = Time.time
            };
        }
        
        NetworkDataLogger.SimulatedNetworkFrame CreateWalkingPose(float time)
        {
            return new NetworkDataLogger.SimulatedNetworkFrame
            {
                avatarName = "REMY",
                bodyPosition = new Vector3(Mathf.Sin(time) * 0.1f, 0.9f + Mathf.Sin(time * 4) * 0.05f, time * 0.1f),
                bodyRotation = Quaternion.Euler(Mathf.Sin(time * 2) * 5f, 0, 0),
                muscles = CreateWalkingMuscles(time),
                timestamp = Time.time
            };
        }
        
        NetworkDataLogger.SimulatedNetworkFrame CreateArmsRaisedPose()
        {
            return new NetworkDataLogger.SimulatedNetworkFrame
            {
                avatarName = "REMY",
                bodyPosition = new Vector3(0, 1.0f, 0),
                bodyRotation = Quaternion.identity,
                muscles = CreateArmsRaisedMuscles(),
                timestamp = Time.time
            };
        }
        
        NetworkDataLogger.SimulatedNetworkFrame CreateCrouchingPose()
        {
            return new NetworkDataLogger.SimulatedNetworkFrame
            {
                avatarName = "REMY",
                bodyPosition = new Vector3(0, 0.5f, 0),
                bodyRotation = Quaternion.Euler(20f, 0, 0),
                muscles = CreateCrouchingMuscles(),
                timestamp = Time.time
            };
        }
        
        float[] CreateWaveMuscles(float time)
        {
            float[] muscles = new float[95];
            
            // Initialize all muscles to neutral
            for (int i = 0; i < muscles.Length; i++)
            {
                muscles[i] = 0f;
            }
            
            // Right arm wave motion (approximate muscle indices)
            muscles[19] = Mathf.Sin(time * 3) * 0.8f; // Right shoulder up/down
            muscles[20] = Mathf.Sin(time * 3) * 0.6f; // Right arm forward/back
            muscles[21] = Mathf.Sin(time * 3 + 1) * 0.7f; // Right forearm
            
            return muscles;
        }
        
        float[] CreateWalkingMuscles(float time)
        {
            float[] muscles = new float[95];
            
            // Walking motion - alternating legs
            muscles[55] = Mathf.Sin(time * 4) * 0.5f; // Left hip
            muscles[56] = Mathf.Sin(time * 4 + Mathf.PI) * 0.5f; // Right hip
            muscles[57] = Mathf.Sin(time * 4) * 0.3f; // Left knee
            muscles[58] = Mathf.Sin(time * 4 + Mathf.PI) * 0.3f; // Right knee
            
            return muscles;
        }
        
        float[] CreateArmsRaisedMuscles()
        {
            float[] muscles = new float[95];
            
            // Arms raised high
            muscles[19] = 0.9f; // Right shoulder up
            muscles[24] = 0.9f; // Left shoulder up
            muscles[20] = 0.3f; // Right arm forward
            muscles[25] = 0.3f; // Left arm forward
            
            return muscles;
        }
        
        float[] CreateCrouchingMuscles()
        {
            float[] muscles = new float[95];
            
            // Crouching pose
            muscles[55] = -0.8f; // Left hip bend
            muscles[56] = -0.8f; // Right hip bend
            muscles[57] = -0.9f; // Left knee bend
            muscles[58] = -0.9f; // Right knee bend
            
            return muscles;
        }
        
        void OnGUI()
        {
            if (!showTestInstructions) return;
            
            GUI.BeginGroup(new Rect(Screen.width - 320, 10, 300, 200));
            GUI.Box(new Rect(0, 0, 300, 200), "Avatar Movement Tester");
            
            GUI.Label(new Rect(10, 25, 280, 20), $"Space - Apply Test Data");
            GUI.Label(new Rect(10, 45, 280, 20), "Or use Inspector buttons:");
            GUI.Label(new Rect(10, 65, 280, 20), "• Test Wave Movement");
            GUI.Label(new Rect(10, 85, 280, 20), "• Test Walking Movement");
            GUI.Label(new Rect(10, 105, 280, 20), "• Test Arms Raised Pose");
            GUI.Label(new Rect(10, 125, 280, 20), "• Test Crouching Pose");
            GUI.Label(new Rect(10, 145, 280, 20), "Paste JSON in Inspector!");
            
            GUI.EndGroup();
        }
    }
}