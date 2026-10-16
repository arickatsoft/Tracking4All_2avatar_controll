using UnityEngine;
using Unity.Netcode;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Manages avatar tags and roles based on Host/Client selection.
    /// Host: Remy = Player (local), Bryce = Enemy (remote)
    /// Client: Remy = Enemy (remote), Bryce = Player (local)
    /// </summary>
    public class AvatarTagManager : MonoBehaviour
    {
        [Header("Avatar References")]
        [SerializeField] private GameObject remyAvatar;
        [SerializeField] private GameObject bryceAvatar; // This should be Adam_Big in your case
        
        [Header("Tag Configuration")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private string enemyTag = "Enemy";
        
        [Header("Debug")]
        [SerializeField] private bool logTagChanges = true;
        
        // Singleton instance
        public static AvatarTagManager Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                InitializeAvatarReferences();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeAvatarReferences();
            LogCurrentTagStatus();
        }
        
        private void InitializeAvatarReferences()
        {
            // Find Remy avatar if not assigned
            if (remyAvatar == null)
            {
                remyAvatar = GameObject.Find("Remy");
                if (remyAvatar == null)
                {
                    // Try finding by current tag
                    GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
                    foreach (GameObject obj in playerObjects)
                    {
                        if (obj.name.Contains("Remy"))
                        {
                            remyAvatar = obj;
                            break;
                        }
                    }
                }
            }
            
            // Find Bryce/Adam avatar if not assigned
            if (bryceAvatar == null)
            {
                bryceAvatar = GameObject.Find("Adam_Big");
                if (bryceAvatar == null)
                {
                    bryceAvatar = GameObject.Find("Bryce");
                }
                if (bryceAvatar == null)
                {
                    // Try finding by current tag
                    GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
                    foreach (GameObject obj in enemyObjects)
                    {
                        if (obj.name.Contains("Adam") || obj.name.Contains("Bryce"))
                        {
                            bryceAvatar = obj;
                            break;
                        }
                    }
                }
            }
            
            // Log what we found
            if (logTagChanges)
            {
                NetworkLogDisplay.LogNetwork("AVATAR_TAGS", $"Remy avatar: {(remyAvatar != null ? remyAvatar.name : "Not found")}");
                NetworkLogDisplay.LogNetwork("AVATAR_TAGS", $"Bryce/Adam avatar: {(bryceAvatar != null ? bryceAvatar.name : "Not found")}");
            }
        }
        
        /// <summary>
        /// Configure tags for Host mode: Remy = Player, Bryce = Enemy
        /// </summary>
        public void SetupHostTags()
        {
            NetworkLogDisplay.LogHost("Setting up HOST avatar tags...");
            
            if (remyAvatar != null)
            {
                remyAvatar.tag = playerTag; // Remy is local player for host
                NetworkLogDisplay.LogHost($"✅ {remyAvatar.name} tagged as {playerTag} (local)");
            }
            else
            {
                NetworkLogDisplay.LogNetwork("ERROR", "Remy avatar not found for host setup!", true);
            }
            
            if (bryceAvatar != null)
            {
                bryceAvatar.tag = enemyTag; // Bryce is remote for host
                NetworkLogDisplay.LogHost($"✅ {bryceAvatar.name} tagged as {enemyTag} (remote)");
            }
            else
            {
                NetworkLogDisplay.LogNetwork("ERROR", "Bryce/Adam avatar not found for host setup!", true);
            }
            
            LogCurrentTagStatus();
        }
        
        /// <summary>
        /// Configure tags for Client mode: Remy = Enemy, Bryce = Player  
        /// </summary>
        public void SetupClientTags()
        {
            NetworkLogDisplay.LogClient("Setting up CLIENT avatar tags...");
            
            if (remyAvatar != null)
            {
                remyAvatar.tag = enemyTag; // Remy is remote for client
                NetworkLogDisplay.LogClient($"✅ {remyAvatar.name} tagged as {enemyTag} (remote)");
            }
            else
            {
                NetworkLogDisplay.LogNetwork("ERROR", "Remy avatar not found for client setup!", true);
            }
            
            if (bryceAvatar != null)
            {
                bryceAvatar.tag = playerTag; // Bryce is local player for client
                NetworkLogDisplay.LogClient($"✅ {bryceAvatar.name} tagged as {playerTag} (local)");
            }
            else
            {
                NetworkLogDisplay.LogNetwork("ERROR", "Bryce/Adam avatar not found for client setup!", true);
            }
            
            LogCurrentTagStatus();
        }
        
        /// <summary>
        /// Automatically detect and setup tags based on network role
        /// </summary>
        public void AutoSetupTags()
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                NetworkLogDisplay.LogNetwork("WARNING", "NetworkManager not found for auto tag setup", true);
                return;
            }
            
            if (networkManager.IsHost)
            {
                SetupHostTags();
            }
            else if (networkManager.IsClient)
            {
                SetupClientTags();
            }
            else
            {
                NetworkLogDisplay.LogNetwork("INFO", "Network not started yet - tags will be set when connection is established");
            }
        }
        
        /// <summary>
        /// Reset tags to default state
        /// </summary>
        public void ResetToDefaultTags()
        {
            NetworkLogDisplay.LogNetwork("AVATAR_TAGS", "Resetting to default tags...");
            
            // Default: Remy = Player, Adam = Enemy (original setup)
            if (remyAvatar != null)
            {
                remyAvatar.tag = playerTag;
                NetworkLogDisplay.LogNetwork("AVATAR_TAGS", $"Reset {remyAvatar.name} to {playerTag}");
            }
            
            if (bryceAvatar != null)
            {
                bryceAvatar.tag = enemyTag;
                NetworkLogDisplay.LogNetwork("AVATAR_TAGS", $"Reset {bryceAvatar.name} to {enemyTag}");
            }
            
            LogCurrentTagStatus();
        }
        
        private void LogCurrentTagStatus()
        {
            if (!logTagChanges) return;
            
            NetworkLogDisplay.LogNetwork("AVATAR_TAGS", "Current tag status:");
            
            if (remyAvatar != null)
            {
                NetworkLogDisplay.LogNetwork("AVATAR_TAGS", $"  {remyAvatar.name}: {remyAvatar.tag}");
            }
            
            if (bryceAvatar != null)
            {
                NetworkLogDisplay.LogNetwork("AVATAR_TAGS", $"  {bryceAvatar.name}: {bryceAvatar.tag}");
            }
        }
        
        /// <summary>
        /// Get the avatar that should be controlled locally based on current tags
        /// </summary>
        public GameObject GetLocalAvatar()
        {
            GameObject playerTaggedAvatar = GameObject.FindGameObjectWithTag(playerTag);
            return playerTaggedAvatar;
        }
        
        /// <summary>
        /// Get the avatar that should receive remote data based on current tags
        /// </summary>
        public GameObject GetRemoteAvatar()
        {
            GameObject enemyTaggedAvatar = GameObject.FindGameObjectWithTag(enemyTag);
            return enemyTaggedAvatar;
        }
        
        /// <summary>
        /// Static methods for easy access
        /// </summary>
        public static void SetupForHost()
        {
            if (Instance != null)
                Instance.SetupHostTags();
        }
        
        public static void SetupForClient()
        {
            if (Instance != null)
                Instance.SetupClientTags();
        }
        
        public static void AutoSetup()
        {
            if (Instance != null)
                Instance.AutoSetupTags();
        }
        
        public static GameObject GetLocalPlayerAvatar()
        {
            if (Instance != null)
                return Instance.GetLocalAvatar();
            return null;
        }
        
        public static GameObject GetRemotePlayerAvatar()
        {
            if (Instance != null)
                return Instance.GetRemoteAvatar();
            return null;
        }
    }
}