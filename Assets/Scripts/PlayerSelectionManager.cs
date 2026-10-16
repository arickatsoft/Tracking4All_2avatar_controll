using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Tracking4All;

public class PlayerSelectionManager : MonoBehaviour
{
    [Header("Player Avatars")]
    [SerializeField] private GameObject remyAvatar;
    [SerializeField] private GameObject adamAvatar;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraRig;
    [SerializeField] private GameObject cameraRigAdam; // Reference to CameraRig_Adam
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraTransitionTime = 1.5f;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject selectionUI;
    [SerializeField] private Button adamButton;
    [SerializeField] private Button remyButton;
    
    [Header("Tracking Settings")]
    [SerializeField] private GameObject tracking4AllObject;
    private Tracking4All.Avatar activeTrackingAvatar;
    AvatarSelectionManager manager = null;

    // Camera positions for different player configurations
    private readonly Vector3 remyDefaultCameraPos = new Vector3(1.7f, 1.5f, -2.2f);
    private readonly Vector3 remyDefaultCameraRot = new Vector3(0f, 0f, 0f);
    
    private readonly Vector3 adamActiveCameraPos = new Vector3(3f, 1.8f, -3.5f);
    private readonly Vector3 adamActiveCameraRot = new Vector3(10f, -5f, 0f);
    
    private enum PlayerType { Remy, Adam }
    private PlayerType currentPlayer = PlayerType.Remy;
    
    void Start()
    {
        Debug.Log("PlayerSelectionManager: Avatar selection UI enabled");
        InitializeScene();
        ShowSelectionUI();
    }
    
    void InitializeScene()
    {
        // Ensure both avatars exist in scene
        if (remyAvatar == null)
        {
            remyAvatar = GameObject.Find("Remy");
        }
        if (adamAvatar == null)
        {
            adamAvatar = GameObject.Find("Adam_Big");
        }
        
        // Get camera references if not set
        if (cameraRig == null)
        {
            cameraRig = GameObject.Find("CameraRig")?.transform;
        }
        if (cameraRigAdam == null)
        {
            cameraRigAdam = GameObject.Find("CameraRig_Adam");
        }
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // Get tracking system
        if (tracking4AllObject == null)
        {
            tracking4AllObject = GameObject.Find("Tracking4All");
        }
        
        // Initialize camera states - start with main camera enabled, Adam camera disabled
        if (cameraRig != null)
        {
            cameraRig.gameObject.SetActive(true);
        }
        if (cameraRigAdam != null)
        {
            cameraRigAdam.SetActive(false);
        }
        
        // Set initial state - Remy is default
        SetPlayerActive(PlayerType.Remy);
    }
    
    void ShowSelectionUI()
    {
        if (selectionUI != null)
        {
            selectionUI.SetActive(true);
        }
        else
        {
            CreateSelectionUI();
        }

        
        // Setup button listeners
        FitFight.BodyTracking.BodyTrackingSelector.Create(selectionUI.transform);
        var home = selectionUI.GetComponent<FitFightSelectionView>();
        if (home != null && home.PracticeButton != null)
        {
            home.PracticeButton.onClick.RemoveAllListeners();
            home.PracticeButton.onClick.AddListener(StartPractice);
        }
        if (adamButton != null)
        {
            adamButton.onClick.RemoveAllListeners();
            adamButton.onClick.AddListener(() => OnPlayerSelected(PlayerType.Adam));
        }
        
        if (remyButton != null)
        {
            remyButton.onClick.RemoveAllListeners();
            remyButton.onClick.AddListener(() => OnPlayerSelected(PlayerType.Remy));
        }
    }
    
    public void StartPractice()
    {
        if (GetComponent<FitFightPracticeMode>() != null) return;
        manager = FindFirstObjectByType<AvatarSelectionManager>();
        if (manager == null || !manager.SelectLocalPractice()) return;
        StopAllCoroutines();
        if (selectionUI != null) selectionUI.SetActive(false);
        gameObject.AddComponent<FitFightPracticeMode>().Begin(remyAvatar, adamAvatar, this);
    }

    void CreateSelectionUI()
    {
        selectionUI = FitFightSelectionView.Create(out adamButton, out remyButton);
    }

    void OnPlayerSelected(PlayerType playerType)
    {
        currentPlayer = playerType;

        // Find AvatarSelectionManager in the scene (MonoBehaviours must be attached to GameObjects)
        if(manager == null)
        {
            manager = FindObjectOfType<AvatarSelectionManager>();

            if (manager == null)
            {
                Debug.LogError("❌ AvatarSelectionManager not found in scene! Please add it to a GameObject.");
                return;
            }
        }

        if (playerType==PlayerType.Remy)
            manager.OnRemySelected();
        else
            manager.OnAdamSelected();

        // Hide selection UI
        if (selectionUI != null)
        {
            selectionUI.SetActive(false);
        }
        
        // Switch player
        SetPlayerActive(playerType);
        
        // Start the game
        StartCoroutine(TransitionToGameplay());
    }
    
    void SetPlayerActive(PlayerType playerType)
    {
        switch(playerType)
        {
            case PlayerType.Remy:
                // Remy is the tracked player
                if (remyAvatar != null)
                {
                    remyAvatar.SetActive(true);
                    remyAvatar.tag = "Player";
                }
                if (adamAvatar != null)
                {
                    adamAvatar.SetActive(true);
                    adamAvatar.tag = "Enemy";
                }
                
                // Enable main camera rig, disable Adam's camera rig
                if (cameraRig != null)
                {
                    cameraRig.gameObject.SetActive(true);
                    StartCoroutine(MoveCamera(remyDefaultCameraPos, remyDefaultCameraRot));
                }
                if (cameraRigAdam != null)
                {
                    cameraRigAdam.SetActive(false);
                }
                
                // Setup tracking for Remy
                SetupTrackingForAvatar(remyAvatar);

                // Apply rotation AFTER tracking is set up (delayed)
                // Keep the fighter upright while facing the opponent.
                if (adamAvatar != null)
                {
                    StartCoroutine(ApplyRotationDelayed(adamAvatar, new Vector3(0, 180, 0), 0.5f));
                }
                break;
                
            case PlayerType.Adam:
                // Adam is the tracked player
                if (adamAvatar != null)
                {
                    adamAvatar.SetActive(true);
                    adamAvatar.tag = "Player";
                }
                if (remyAvatar != null)
                {
                    remyAvatar.SetActive(true);
                    remyAvatar.tag = "Enemy";
                }
                
                // Disable main camera rig, enable Adam's camera rig
                if (cameraRig != null)
                {
                    cameraRig.gameObject.SetActive(false);
                }
                if (cameraRigAdam != null)
                {
                    cameraRigAdam.SetActive(true);
                    Debug.Log("CameraRig_Adam enabled for Adam player selection");
                }
                
                // Setup tracking for Adam
                SetupTrackingForAvatar(adamAvatar);

                // Apply rotation AFTER tracking is set up (delayed)
                // Keep the fighter upright while facing the opponent.
                if (adamAvatar != null)
                {
                    StartCoroutine(ApplyRotationDelayed(adamAvatar, new Vector3(0, 180, 0), 0.5f));
                }
                break;
                {
                    cameraRig.gameObject.SetActive(false);
                }
                if (cameraRigAdam != null)
                {
                    cameraRigAdam.SetActive(true);
                    Debug.Log("CameraRig_Adam enabled for Adam player selection");
                }
                
                // Setup tracking for Adam
                SetupTrackingForAvatar(adamAvatar);
                break;
        }
        if (remyAvatar != null && adamAvatar != null)
        {
            var presentation = GetComponent<FitFightArenaPresentation>();
            if (presentation == null) presentation = gameObject.AddComponent<FitFightArenaPresentation>();
            presentation.Configure(remyAvatar, adamAvatar, mainCamera, cameraRigAdam, playerType == PlayerType.Adam);
        }

    }
    
    void SetupTrackingForAvatar(GameObject avatar)
    {
        // Find the Avatar component in the tracking system
        if (tracking4AllObject != null)
        {
            Tracking4All.Avatar[] trackingAvatars = tracking4AllObject.GetComponentsInChildren<Tracking4All.Avatar>();
            
            foreach (Tracking4All.Avatar trackAvatar in trackingAvatars)
            {
                // Disable all tracking avatars first
                trackAvatar.enabled = false;
            }
            
            // Now setup tracking for the selected avatar
            if (avatar == adamAvatar)
            {
                // Find or create tracking components for Adam
                Tracking4All.Avatar adamTracking = avatar.GetComponent<Tracking4All.Avatar>();
                if (adamTracking == null)
                {
                    // Add tracking components to Adam
                    adamTracking = avatar.AddComponent<Tracking4All.Avatar>();
                    AvatarSkeleton adamSkeleton = avatar.GetComponent<AvatarSkeleton>();
                    if (adamSkeleton == null)
                    {
                        adamSkeleton = avatar.AddComponent<AvatarSkeleton>();
                    }
                }
                
                if (adamTracking != null)
                {
                    adamTracking.enabled = true;
                    activeTrackingAvatar = adamTracking;
                    
                    Debug.Log("Tracking enabled for Adam");
                }
            }
            else if (avatar == remyAvatar)
            {
                // Find existing tracking for Remy
                Tracking4All.Avatar remyTracking = avatar.GetComponent<Tracking4All.Avatar>();
                if (remyTracking != null)
                {
                    remyTracking.enabled = true;
                    activeTrackingAvatar = remyTracking;
                    
                    Debug.Log("Tracking enabled for Remy");
                }
            }
        }
    }
    
    IEnumerator MoveCamera(Vector3 targetPosition, Vector3 targetRotation)
    {
        if (cameraRig == null || !cameraRig.gameObject.activeInHierarchy) yield break;
        
        Vector3 startPos = cameraRig.position;
        Quaternion startRot = cameraRig.rotation;
        Quaternion targetRot = Quaternion.Euler(targetRotation);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < cameraTransitionTime)
        {
            float t = elapsedTime / cameraTransitionTime;
            t = Mathf.SmoothStep(0, 1, t); // Smooth interpolation
            
            cameraRig.position = Vector3.Lerp(startPos, targetPosition, t);
            cameraRig.rotation = Quaternion.Slerp(startRot, targetRot, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        cameraRig.position = targetPosition;
        cameraRig.rotation = targetRot;
    }
    
    IEnumerator TransitionToGameplay()
    {
        // Wait for camera transition
        yield return new WaitForSeconds(cameraTransitionTime);
        
        // Additional game start logic can go here
        Debug.Log($"Game started with player: {currentPlayer}");
    }
    
    // Coroutine to apply rotation with delay (lets tracking system initialize first)
    IEnumerator ApplyRotationDelayed(GameObject avatar, Vector3 eulerAngles, float delay)
    {
        Debug.Log($"Waiting {delay} seconds before applying rotation to {avatar.name}...");
        
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);
        
        // Apply the rotation
        avatar.transform.rotation = Quaternion.Euler(eulerAngles);
        Debug.Log($"Applied rotation {eulerAngles} to {avatar.name}");
    }
    
    // Method to show selection UI again (can be called from pause menu)
    public void ShowPlayerSelection()
    {
        var practice = GetComponent<FitFightPracticeMode>();
        if (practice != null) practice.End();
        ShowSelectionUI();
    }
}
