using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Manages character selection in the game
    /// </summary>
    public class CharacterSelectionManager : MonoBehaviour
    {
        [Header("Character Selection Settings")]
        public GameObject[] characterPrefabs;
        public Transform spawnPoint;
        
        [Header("UI References")]
        public GameObject characterSelectionUI;
        
        private int selectedCharacterIndex = 0;
        private GameObject currentCharacterInstance;
        
        void Start()
        {
            // Initialize character selection
            if (characterPrefabs != null && characterPrefabs.Length > 0)
            {
                SpawnSelectedCharacter();
            }
        }
        
        public void SelectCharacter(int index)
        {
            if (index >= 0 && index < characterPrefabs.Length)
            {
                selectedCharacterIndex = index;
                SpawnSelectedCharacter();
            }
        }
        
        private void SpawnSelectedCharacter()
        {
            // Destroy current character if exists
            if (currentCharacterInstance != null)
            {
                DestroyImmediate(currentCharacterInstance);
            }
            
            // Spawn new character
            if (characterPrefabs[selectedCharacterIndex] != null && spawnPoint != null)
            {
                currentCharacterInstance = Instantiate(characterPrefabs[selectedCharacterIndex], spawnPoint.position, spawnPoint.rotation);
            }
        }
        
        public void ConfirmSelection()
        {
            // Hide character selection UI
            if (characterSelectionUI != null)
            {
                characterSelectionUI.SetActive(false);
            }
        }
        
        public void ShowCharacterSelection()
        {
            // Show character selection UI
            if (characterSelectionUI != null)
            {
                characterSelectionUI.SetActive(true);
            }
        }
    }
}