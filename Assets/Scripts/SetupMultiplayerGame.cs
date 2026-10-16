using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Simple setup script to initialize multiplayer - just drag this onto any GameObject in the scene
    /// This should be used INSTEAD of InstantPlayerSelector for multiplayer functionality
    /// </summary>
    public class SetupMultiplayerGame : MonoBehaviour
    {
        void Start()
        {
            UnityEngine.Debug.Log("=== SETTING UP MULTIPLAYER GAME ===");
            
            // 1. First disable old systems
            DisableOldSinglePlayerSystems disabler = FindObjectOfType<DisableOldSinglePlayerSystems>();
            if (disabler == null)
            {
                GameObject disablerGO = new GameObject("DisableOldSinglePlayerSystems");
                disabler = disablerGO.AddComponent<DisableOldSinglePlayerSystems>();
            }
            
            // 2. Create the multiplayer setup
            MultiplayerGameSetup setup = FindObjectOfType<MultiplayerGameSetup>();
            if (setup == null)
            {
                GameObject setupGO = new GameObject("MultiplayerGameSetup");
                setup = setupGO.AddComponent<MultiplayerGameSetup>();
            }
            
            // 3. Initialize multiplayer
            setup.InitializeMultiplayerGame();
            
            UnityEngine.Debug.Log("=== MULTIPLAYER SETUP COMPLETE ===");
            
            // 4. Remove this script - it's only needed once
            Destroy(this.gameObject, 1f);
        }
    }
}