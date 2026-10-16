using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Managers")]
    [SerializeField] private PlayerSelectionManager playerSelectionManager;
    [SerializeField] private TrackingAvatarSwitcher trackingSwitcher;
    [SerializeField] private DynamicCameraController cameraController;
    
    [Header("Game State")]
    [SerializeField] private bool gameStarted = false;
    [SerializeField] private PlayerType currentPlayer = PlayerType.Remy;
    
    public enum PlayerType { Remy, Adam }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeManagers();
    }
    
    void InitializeManagers()
    {
        // Find managers if not assigned
        if (playerSelectionManager == null)
            playerSelectionManager = FindObjectOfType<PlayerSelectionManager>();
            
        if (trackingSwitcher == null)
            trackingSwitcher = FindObjectOfType<TrackingAvatarSwitcher>();
            
        if (cameraController == null)
            cameraController = FindObjectOfType<DynamicCameraController>();
            
        // Create managers if they don't exist
        if (playerSelectionManager == null)
        {
            GameObject pmGO = new GameObject("PlayerSelectionManager");
            playerSelectionManager = pmGO.AddComponent<PlayerSelectionManager>();
        }
        
        if (trackingSwitcher == null)
        {
            GameObject tsGO = new GameObject("TrackingAvatarSwitcher");
            trackingSwitcher = tsGO.AddComponent<TrackingAvatarSwitcher>();
        }
        
        if (cameraController == null)
        {
            GameObject ccGO = new GameObject("DynamicCameraController");
            cameraController = ccGO.AddComponent<DynamicCameraController>();
        }
    }
    
    public void SelectPlayer(PlayerType player)
    {
        currentPlayer = player;
        
        switch (player)
        {
            case PlayerType.Remy:
                if (trackingSwitcher != null)
                    trackingSwitcher.SwitchToRemy();
                if (cameraController != null)
                    cameraController.SetFollowAdam(false);
                break;
                
            case PlayerType.Adam:
                if (trackingSwitcher != null)
                    trackingSwitcher.SwitchToAdam();
                if (cameraController != null)
                    cameraController.SetFollowAdam(true);
                break;
        }
        
        gameStarted = true;
        Debug.Log($"Player selected: {player}");
    }
    
    public void ResetGame()
    {
        gameStarted = false;
        currentPlayer = PlayerType.Remy;
        
        if (playerSelectionManager != null)
            playerSelectionManager.ShowPlayerSelection();
    }
    
    public PlayerType GetCurrentPlayer()
    {
        return currentPlayer;
    }
    
    public bool IsGameStarted()
    {
        return gameStarted;
    }
}
