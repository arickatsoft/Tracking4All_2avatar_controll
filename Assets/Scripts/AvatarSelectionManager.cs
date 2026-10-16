using UnityEngine;
using Tracking4All;
using System.Reflection;
using System.Linq;
using System.Collections;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.PoseTracking;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AvatarSelectionManager : MonoBehaviour
{
    [Header("Pose Solutions")]
    
    [Header("Avatar Activator")]
    [SerializeField] private Tracking4All.SimpleAvatarActivator avatarActivator;

    private static GameObject FindPoseSolution(string name)
    {
        // A previously unselected solution is inactive and GameObject.Find misses it.
        foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            if (candidate.name == name && candidate.scene.IsValid() && candidate.scene.isLoaded)
                return candidate;
        return null;
    }

    //private void setPoseProvider(PosePuppet posePuppet, GameObject providerObject)
    //{
    //    // Get the poseProvider field
    //    var poseProviderField = typeof(PosePuppet).GetField("poseProvider",
    //        BindingFlags.NonPublic | BindingFlags.Instance);

    //    if (poseProviderField == null)
    //    {
    //        Debug.LogError("❌ Could not find 'poseProvider' field!");
    //        return;
    //    }

    //    // Get or create the LandmarkProvider wrapper
    //    var currentProvider = poseProviderField.GetValue(posePuppet) as LandmarkProvider<PoseLandmarks>;
    //    if (currentProvider == null)
    //    {
    //        currentProvider = new LandmarkProvider<PoseLandmarks>();
    //        poseProviderField.SetValue(posePuppet, currentProvider);
    //    }

    //    // Set the internal provider field to the GAMEOBJECT (not component!)
    //    var providerField = typeof(LandmarkProvider<PoseLandmarks>).GetField("provider",
    //        BindingFlags.NonPublic | BindingFlags.Instance);

    //    if (providerField != null)
    //    {
    //        providerField.SetValue(currentProvider, providerObject); // GameObject, not component!
    //        Debug.Log("✅ Automatically reassigned Pose Provider!");
    //    }
    //    else
    //    {
    //        Debug.LogError("❌ Could not find internal 'provider' field");
    //    }
    //}

    private void setPoseProvider(PosePuppet posePuppet, GameObject providerObject)
    {
        if (posePuppet == null)
        {
            Debug.LogError("❌ PosePuppet is null!");
            return;
        }

        if (providerObject == null)
        {
            Debug.LogError("❌ Provider GameObject is null!");
            return;
        }

        // Create a new LandmarkProvider wrapper
        var newProvider = new LandmarkProvider<PoseLandmarks>();
        
        // Set the internal provider field to the GAMEOBJECT using reflection
        var providerField = typeof(LandmarkProvider<PoseLandmarks>).GetField("provider",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (providerField != null)
        {
            providerField.SetValue(newProvider, providerObject);
            Debug.Log($"✅ Created new provider wrapper for {providerObject.name}");
            
            // Use the new SetPoseProvider method to properly switch providers
            posePuppet.SetPoseProvider(newProvider);
            Debug.Log($"✅ Successfully set provider on {posePuppet.gameObject.name}");
        }
        else
        {
            Debug.LogError("❌ Could not find internal 'provider' field");
        }
    }

    private void setUDPPoseProvider(string avatar)
    {
        GameObject udpPoseProvider = GameObject.Find("UDP Pose Provider");
        GameObject puppetAvatar;
        string avat = "Puppet-StandardT Remy";
        int port = 52733;
        if (avatar == "Remy")
        {
            puppetAvatar = GameObject.Find("Puppet-StandardT Remy");
        }else
        {
            port = 52734;
            puppetAvatar = GameObject.Find("Puppet-StandardT");
            avat = "Puppet-StandardT";
        }

        if (udpPoseProvider != null && puppetAvatar != null)
        {
            var simpleAvatarActivator = udpPoseProvider.GetComponent<SimpleAvatarActivator>();
            var udpPoseLandmarkProvider = udpPoseProvider.GetComponent<UDPPoseLandmarkProvider>();

            if (udpPoseLandmarkProvider != null)
            {
                var poseLandmarkField = typeof(UDPPoseLandmarkProvider).GetField("listenPort",
                    BindingFlags.Public | BindingFlags.Instance);

                if (poseLandmarkField == null)
                {
                    Debug.LogError("❌ Could not find 'poseLandmark' field!");
                    return;
                }

                // Simply set the GameObject directly - no wrapper involved!
                poseLandmarkField.SetValue(udpPoseLandmarkProvider, port);
                Debug.Log("✅ Automatically reassigned Pose Landmark! " + avat);
            }
            else
            {
                Debug.LogError("❌ PoseLandmark component not found!");
            }

            if (simpleAvatarActivator != null)
            {
                var avatarPuppetField = typeof(SimpleAvatarActivator).GetField("avatarPuppet",
                    BindingFlags.Public | BindingFlags.Instance);

                if (avatarPuppetField == null)
                {
                    Debug.LogError("❌ Could not find 'avatar Puppet' field!");
                    return;
                }


                if (avatarPuppetField == null)
                {
                    Debug.LogError("❌ Could not find 'avatarPuppet' field!");
                    return;
                }

                // Simply set the GameObject directly - no wrapper involved!
                avatarPuppetField.SetValue(simpleAvatarActivator, puppetAvatar);
                Debug.Log("✅ Automatically reassigned Puppet Avatar! " + avat);
            }
            else
            {
                Debug.LogError("❌ simpleAvatarActivator component not found!");
            }
        }
    }

    private void setPoseToWebcamStream(GameObject poseSolutionObj, string avatar)
    {
        int port = 52733;
        if (avatar != "Remy")
            port = 52734;

        GameObject webcamControllerObj = GameObject.Find("WebcamStreamController");

        if (webcamControllerObj != null && poseSolutionObj != null)
        {
            var webcamStreamController = webcamControllerObj.GetComponent<IPWebcamStreamController>();

            if (webcamStreamController != null)
            {
                var poseProviderField = typeof(IPWebcamStreamController).GetField("poseProvider",
                    BindingFlags.Public | BindingFlags.Instance);

                var targetPortField = typeof(IPWebcamStreamController).GetField("targetPort",
                    BindingFlags.Public | BindingFlags.Instance);

                var webcamPortField = typeof(IPWebcamStreamController).GetField("webcamPort",
                    BindingFlags.Public | BindingFlags.Instance);

                if (poseProviderField == null)
                {
                    Debug.LogError("❌ Could not find 'poseProvider' field!");
                    return;
                }
                if (targetPortField == null)
                {
                    Debug.LogError("❌ Could not find 'targetPortField' field!");
                    return;
                }
                if (webcamPortField == null)
                {
                    Debug.LogError("❌ Could not find 'webcamPortField' field!");
                    return;
                }

                // Get or create the LandmarkProvider wrapper
                var currentProvider = poseProviderField.GetValue(webcamStreamController) as LandmarkProvider<PoseLandmarks>;
                if (currentProvider == null)
                {
                    currentProvider = new LandmarkProvider<PoseLandmarks>();
                    poseProviderField.SetValue(webcamStreamController, currentProvider);
                }

                // Set the internal provider field to the GAMEOBJECT (not component!)
                var providerField = typeof(LandmarkProvider<PoseLandmarks>).GetField("provider",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                targetPortField.SetValue(webcamStreamController, port);
                webcamPortField.SetValue(webcamStreamController, port);

                if (providerField != null)
                {
                    providerField.SetValue(currentProvider, poseSolutionObj); // GameObject, not component!
                    Debug.Log("✅ Automatically reassigned Pose Provider!");
                }
                else
                {
                    Debug.LogError("❌ Could not find internal 'provider' field");
                }
            }
            else
            {
                Debug.LogError("❌ WebcamStreamController component not found!");
            }
        }
    }

    private void setWebCam2MPU(string avatar)
    {
        // Each selectable pose solution owns its own MediaPipe graph and camera
        // backend. Resolve both through that root so the Remy path cannot reuse
        // Adam's WebCamTexture when the device exposes only one camera.
        var solutionRoot = GameObject.Find(avatar == "Adam" ? "Pose Solution" : "Pose Solution Remy");
        var mpuComponent = solutionRoot != null
            ? solutionRoot.GetComponentInChildren<MPUPoseTrackingSolution>(true)
            : null;
        var localBackend = mpuComponent != null
            ? mpuComponent.GetComponentInChildren<UnityWebCamBackend>(true)
            : null;
        var defaultWebCam = localBackend != null ? localBackend.gameObject : null;

        if (mpuComponent != null && defaultWebCam != null)
        {
            if (!defaultWebCam.activeSelf) defaultWebCam.SetActive(true);

            // Get the imageFrameProvider field
            var mpuType = mpuComponent.GetType();
            var imageFrameProviderField = mpuType.GetField("imageFrameProvider",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (imageFrameProviderField == null)
            {
                Debug.LogError("❌ Could not find 'imageFrameProvider' field!");
                return;
            }

            Debug.Log($"✓ Found field type: {imageFrameProviderField.FieldType.FullName}");

            // Get or create the wrapper object
            var currentFrameProvider = imageFrameProviderField.GetValue(mpuComponent);
            if (currentFrameProvider == null)
            {
                // Create new instance of the wrapper type
                currentFrameProvider = System.Activator.CreateInstance(imageFrameProviderField.FieldType);
                imageFrameProviderField.SetValue(mpuComponent, currentFrameProvider);
            }

            // Set the internal provider field to the GameObject
            var providerField = imageFrameProviderField.FieldType.GetField("provider",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (providerField != null)
            {
                providerField.SetValue(currentFrameProvider, defaultWebCam);
                Debug.Log($"✅ Connected {avatar} pose graph to {defaultWebCam.name}.");
            }
            else
            {
                Debug.LogError("❌ Could not find internal 'provider' field");
            }
        }
        else
        {
            Debug.LogError($"Could not connect {avatar} pose tracking: pose solution or local camera backend is missing.");
        }
    }

    private void enableDelayedActivation(GameObject avatarGameObject, int port)
    {
        Debug.Log($"[enableDelayedActivation] Called for {avatarGameObject?.name ?? "NULL"} with port {port}");

        if (avatarGameObject)
        {
            // Find the Delayed Puppet Activation component
            DelayedPuppetActivation delayedPuppet = (DelayedPuppetActivation)avatarGameObject.GetComponent(System.Type.GetType("Tracking4All.DelayedPuppetActivation"));

            if (delayedPuppet == null)
            {
                Debug.LogError($"❌ Delayed Puppet component not found on {avatarGameObject.name}!");
                return;
            }

            Debug.Log($"✅ Delayed Puppet component found on {avatarGameObject.name}!");

            // CRITICAL: Set the port BEFORE enabling
            var portField = typeof(DelayedPuppetActivation).GetField("newPort", BindingFlags.Public | BindingFlags.Instance);
            if (portField != null)
            {
                int oldPort = (int)portField.GetValue(delayedPuppet);
                portField.SetValue(delayedPuppet, port);
                Debug.Log($"✅ Set port on {avatarGameObject.name}'s DelayedPuppetActivation: {oldPort} → {port}");
            }
            else
            {
                Debug.LogError("❌ Field 'newPort' not found on DelayedPuppetActivation!");
            }

            // CRITICAL: Disable first, then enable to force OnEnable() to run
            var behaviour = delayedPuppet as Behaviour;
            if (behaviour != null)
            {
                Debug.Log($"[enableDelayedActivation] Current enabled state: {behaviour.enabled}");
                behaviour.enabled = false;  // Disable first
                behaviour.enabled = true;   // Then enable to trigger OnEnable()
                Debug.Log($"✅ Delayed Puppet component re-enabled on {avatarGameObject.name} (port {port} will be configured in OnEnable)");
            }
            else
            {
                Debug.LogError("❌ Delayed Puppet component is not a Behaviour component!");
            }
        }
        else
        {
            Debug.LogError("❌ Avatar GameObject is NULL!");
        }
    }

    private void setUDPPoseStreamer(GameObject avatarGameObject)
    {
        if (avatarGameObject)
        {
            // Find the UDP Pose Streamer component
            UDPPoseStreamer udpStreamer = avatarGameObject.GetComponent<UDPPoseStreamer>();

            if (udpStreamer == null)
            {
                Debug.LogError("❌ UDP Pose Streamer component not found!");
                return;
            }

            // Find the appropriate pose solution GameObject
            GameObject poseSolutionObj = avatarGameObject.name == "Remy"
                ? GameObject.Find("Pose Solution Remy")
                : GameObject.Find("Pose Solution");

            if (poseSolutionObj != null)
            {
                // Create LandmarkProvider wrapper pointing to the pose solution GameObject
                var sourcePoseProvider = new LandmarkProvider<PoseLandmarks>();
                var sourceNormalizedProvider = new NormalizedLandmarkProvider<PoseLandmarks>();

                // Set the internal provider field to the GameObject (this is the pattern used in your codebase)
                var providerField = typeof(LandmarkProvider<PoseLandmarks>).GetField("provider",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var normalizedProviderField = typeof(NormalizedLandmarkProvider<PoseLandmarks>).GetField("provider",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (providerField != null)
                {
                    providerField.SetValue(sourcePoseProvider, poseSolutionObj);
                    Debug.Log($"✅ Set sourcePoseProvider to {poseSolutionObj.name}");
                }

                if (normalizedProviderField != null)
                {
                    normalizedProviderField.SetValue(sourceNormalizedProvider, poseSolutionObj);
                    Debug.Log($"✅ Set sourceNormalizedProvider to {poseSolutionObj.name}");
                }

                // Now assign the wrapped providers to UDPPoseStreamer
                var poseProviderField = typeof(UDPPoseStreamer).GetField("sourcePoseProvider",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var normProviderField = typeof(UDPPoseStreamer).GetField("sourceNormalizedProvider",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                poseProviderField?.SetValue(udpStreamer, sourcePoseProvider);
                normProviderField?.SetValue(udpStreamer, sourceNormalizedProvider);

                Debug.Log($"✅ Assigned pose providers to UDP streamer on {avatarGameObject.name}");
            }
            else
            {
                Debug.LogError($"❌ Pose Solution GameObject not found for {avatarGameObject.name}");
            }

            // Enable the component
            var behaviour = udpStreamer as Behaviour;
            if (behaviour != null)
            {
                behaviour.enabled = true;
                Debug.Log($"✅ UDP Pose Streamer enabled on {avatarGameObject.name}!");
            }

            PromptForIP(udpStreamer);
        }
    }
    //private void setUDPPoseStreamer(GameObject avatarGameObject)
    //{
    //    if (avatarGameObject)
    //    {
    //        // Find the UDP Pose Streamer component
    //        UDPPoseStreamer udpStreamer = (UDPPoseStreamer)avatarGameObject.GetComponent(System.Type.GetType("Tracking4All.UDPPoseStreamer"));

    //        if (udpStreamer == null)
    //        {
    //            Debug.LogError("❌ UDP Pose Streamer component not found on Remy!");
    //            return;
    //        }

    //        // Enable the component
    //        var behaviour = udpStreamer as Behaviour;
    //        if (behaviour != null)
    //        {
    //            behaviour.enabled = true;
    //            Debug.Log("✅ UDP Pose Streamer enabled on Remy!");
    //        }
    //        else
    //        {
    //            Debug.LogError("❌ UDP Pose Streamer is not a Behaviour component!");
    //        }
    //        PromptForIP(udpStreamer);
    //    }
    //    else
    //    {
    //        Debug.LogError("❌ Remy GameObject not found!");
    //    }
    //}

    public bool SelectLocalPractice()
    {
        var solution = FindPoseSolution("Pose Solution Remy");
        var other = FindPoseSolution("Pose Solution");
        var puppet = FindPoseSolution("Puppet-StandardT Remy");
        if (solution == null || puppet == null) { Debug.LogError("Practice requires the Remy pose solution and puppet."); return false; }
        // Practice uses the camera directly; multiplayer selection is a separate action.
        foreach (var stream in FindObjectsByType<UDPPoseStreamer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) stream.enabled = false;
        foreach (var stream in FindObjectsByType<IPWebcamStreamController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) stream.enabled = false;
        if (other != null) other.SetActive(false);
        solution.SetActive(true);
        setPoseProvider(puppet.GetComponent<PosePuppet>(), solution);
        setWebCam2MPU("Remy");
        return true;
    }

    public void OnRemySelected()
    {
        Debug.Log("========================================");
        Debug.Log("🎯 Remy avatar selected");
        Debug.Log("========================================");

        // CRITICAL: Set static port override for REMOTE avatar (Adam on this device)
        // Device B selects Remy (camera-controlled) → UDP Pose Provider receives Adam's data on port 52734
        Debug.Log($"[OnRemySelected] ⚡ Setting STATIC port override to 52734 (to receive Adam's data)...");
        UDPPoseServer.SetPortOverride(52734);

        // DIAGNOSTIC: Verify the override was set
        int? overrideValue = UDPPoseServer.GetPortOverride();
        Debug.Log($"[OnRemySelected] 🔍 DIAGNOSTIC: Verification - GetPortOverride() returned: {(overrideValue.HasValue ? overrideValue.Value.ToString() : "NULL")}");

        Debug.Log($"[OnRemySelected] ⚡ Static override set. Any UDPPoseServer Start() will now use 52734");

        GameObject remy = GameObject.Find("Remy");
        GameObject adam = GameObject.Find("Adam_Big");
        var puppetAdam = GameObject.Find("Puppet-StandardT");
        var poseSolution = FindPoseSolution("Pose Solution");
        var puppetRemy = GameObject.Find("Puppet-StandardT Remy");
        var poseSolutionRemy = FindPoseSolution("Pose Solution Remy");

        var udpPoseProvider = GameObject.Find("UDP Pose Provider");

        if (puppetAdam && udpPoseProvider)
        {
            var posePuppet = puppetAdam.GetComponent<PosePuppet>();

            if (posePuppet == null)
            {
                Debug.LogError("❌ PosePuppet component not found!");
                return;
            }

            setPoseProvider(posePuppet, udpPoseProvider);
        }


        if (poseSolution != null) poseSolution.SetActive(false);

        if (poseSolutionRemy != null) poseSolutionRemy.SetActive(true);
        if (puppetRemy != null && poseSolutionRemy != null)
            setPoseProvider(puppetRemy.GetComponent<PosePuppet>(), poseSolutionRemy);



        setWebCam2MPU("Remy");

        // CRITICAL: Disable Adam's streamer before enabling Remy's
        if (adam != null)
        {
            UDPPoseStreamer adamStreamer = adam.GetComponent<UDPPoseStreamer>();
            if (adamStreamer != null)
            {
                adamStreamer.enabled = false;
                Debug.Log("✅ Disabled Adam's UDPPoseStreamer");
            }
        }

        setUDPPoseStreamer(remy);

        // NOTE: Static port override was already set at the very beginning (line 448)
        // The UDP server will pick it up when Pose Tracking Solution is toggled later
        Debug.Log($"[OnRemySelected] ℹ️ Static port override (52734) was set at start. Will be applied when server starts.");

        setUDPPoseProvider("Adam");

        setPoseToWebcamStream(poseSolution, "Adam");


        //// Set SimpleAvatarActivator to Puppet-StandardT Remy
        //if (avatarActivator != null && puppetStandardTRemy != null)
        //{
        //    var field = avatarActivator.GetType().GetField("AvatarPuppet");
        //    if (field != null) field.SetValue(avatarActivator, puppetStandardTRemy);
        //}


        ResetAllNetworkConnections();

        var poseTrackingSolution = GameObject.Find("Pose Tracking Solution");

        // CRITICAL: Before toggling, disable ALL DelayedPuppetActivation components to prevent interference
        Debug.Log($"[OnRemySelected] Disabling all DelayedPuppetActivation components before toggle...");
        DelayedPuppetActivation[] allDelayedActivations = FindObjectsOfType<DelayedPuppetActivation>();
        foreach (var delayed in allDelayedActivations)
        {
            delayed.enabled = false;
            Debug.Log($"[OnRemySelected] Disabled DelayedPuppetActivation on {delayed.gameObject.name}");
        }

        // DIAGNOSTIC: Check if UDP Pose Provider is a child of Pose Tracking Solution
        Transform udpPoseProviderTransform = udpPoseProvider?.transform;
        if (udpPoseProviderTransform != null)
        {
            Transform parent = udpPoseProviderTransform.parent;
            Debug.Log($"🔍 [OnRemySelected] UDP Pose Provider parent: {(parent != null ? parent.name : "NULL/ROOT")}");
            Debug.Log($"🔍 [OnRemySelected] Is UDP Pose Provider a child of Pose Tracking Solution? {(parent == poseTrackingSolution.transform)}");
        }

        Debug.Log($"[OnRemySelected] Toggling Pose Tracking Solution...");
        poseTrackingSolution.SetActive(false);
        poseTrackingSolution.SetActive(true);
        Debug.Log($"✅ [OnRemySelected] Pose Tracking Solution toggled.");

        // CRITICAL: UDP Pose Provider is NOT a child of Pose Tracking Solution, so we must restart it manually
        // This will trigger UDPPoseServer.Start() which will auto-detect the correct port
        if (udpPoseProvider != null)
        {
            Debug.Log($"[OnRemySelected] Restarting UDP Pose Provider to trigger auto-detection...");
            UDPPoseServer serverComponent = udpPoseProvider.GetComponent<UDPPoseServer>();
            if (serverComponent != null)
            {
                Debug.Log($"[OnRemySelected] Found UDPPoseServer component, restarting via disable/enable...");

                // Use MonoBehaviour disable/enable instead of GameObject SetActive
                // This ensures Start() runs again without Unity resetting serialized fields
                var serverBehaviour = serverComponent as MonoBehaviour;
                if (serverBehaviour != null)
                {
                    serverBehaviour.enabled = false;
                    serverBehaviour.enabled = true;
                    Debug.Log($"✅ [OnRemySelected] UDPPoseServer component restarted. Auto-detection should have run.");
                }
            }
            else
            {
                Debug.LogError($"❌ [OnRemySelected] UDPPoseServer component not found on UDP Pose Provider!");
            }
        }
    }

    public void ResetAllNetworkConnections()
    {
        UnityEngine.Debug.Log("========================================");
        UnityEngine.Debug.Log("🔄 RESETTING ALL NETWORK CONNECTIONS");
        UnityEngine.Debug.Log("========================================");

        // CRITICAL: Only reconnect ENABLED streamers, not all of them
        GameObject remy = GameObject.Find("Remy");
        GameObject adam = GameObject.Find("Adam_Big");

        if (remy != null)
        {
            UDPPoseStreamer remyStreamer = remy.GetComponent<UDPPoseStreamer>();
            if (remyStreamer != null && remyStreamer.enabled)  // Only if ENABLED
            {
                UnityEngine.Debug.Log($"🔄 Reconnecting Remy's UDPPoseStreamer (enabled={remyStreamer.enabled})");
                remyStreamer.Reconnect();
            }
            else if (remyStreamer != null)
            {
                UnityEngine.Debug.Log($"⏭️ Skipping Remy's streamer (disabled)");
            }
        }

        if (adam != null)
        {
            UDPPoseStreamer adamStreamer = adam.GetComponent<UDPPoseStreamer>();
            if (adamStreamer != null && adamStreamer.enabled)  // Only if ENABLED
            {
                UnityEngine.Debug.Log($"🔄 Reconnecting Adam's UDPPoseStreamer (enabled={adamStreamer.enabled})");
                adamStreamer.Reconnect();
            }
            else if (adamStreamer != null)
            {
                UnityEngine.Debug.Log($"⏭️ Skipping Adam's streamer (disabled)");
            }
        }

        // Also restart the listening server
        // IMPORTANT: Don't restart the server here - let DelayedPuppetActivation handle it
        // The server port was already set by the enabled DelayedPuppetActivation component
        UnityEngine.Debug.Log("ℹ️ Server port already configured by DelayedPuppetActivation, skipping server restart");

        UnityEngine.Debug.Log("✅ Network reset complete!");
        UnityEngine.Debug.Log("========================================");
    }

    public void OnAdamSelected()
    {
        Debug.Log("========================================");
        Debug.Log("🎯 Adam avatar selected");
        Debug.Log("========================================");

        // CRITICAL: Set static port override for REMOTE avatar (Remy on this device)
        // Device A selects Adam (camera-controlled) → UDP Pose Provider receives Remy's data on port 52733
        Debug.Log($"[OnAdamSelected] ⚡ Setting STATIC port override to 52733 (to receive Remy's data)...");
        UDPPoseServer.SetPortOverride(52733);

        // DIAGNOSTIC: Verify the override was set
        int? overrideValue = UDPPoseServer.GetPortOverride();
        Debug.Log($"[OnAdamSelected] 🔍 DIAGNOSTIC: Verification - GetPortOverride() returned: {(overrideValue.HasValue ? overrideValue.Value.ToString() : "NULL")}");

        Debug.Log($"[OnAdamSelected] ⚡ Static override set. Any UDPPoseServer Start() will now use 52733");

        GameObject remy = GameObject.Find("Remy");
        GameObject adam = GameObject.Find("Adam_Big");
        var puppetAdam = GameObject.Find("Puppet-StandardT");
        var poseSolution = FindPoseSolution("Pose Solution");
        var puppetRemy = GameObject.Find("Puppet-StandardT Remy");
        var poseSolutionRemy = FindPoseSolution("Pose Solution Remy");

        var udpPoseProvider = GameObject.Find("UDP Pose Provider");

        if (puppetRemy && udpPoseProvider)
        {
            var posePuppet = puppetRemy.GetComponent<PosePuppet>();

            if (posePuppet == null)
            {
                Debug.LogError("❌ PosePuppet component not found!");
                return;
            }

            setPoseProvider(posePuppet, udpPoseProvider);
        }


        if (poseSolutionRemy != null) poseSolutionRemy.SetActive(false);

        if (poseSolution != null) poseSolution.SetActive(true);

        if (puppetAdam != null && poseSolution != null)
            setPoseProvider(puppetAdam.GetComponent<PosePuppet>(), poseSolution);


        setWebCam2MPU("Adam");

        // CRITICAL: Disable Remy's streamer before enabling Adam's
        if (remy != null)
        {
            UDPPoseStreamer remyStreamer = remy.GetComponent<UDPPoseStreamer>();
            if (remyStreamer != null)
            {
                remyStreamer.enabled = false;
                Debug.Log("✅ Disabled Remy's UDPPoseStreamer");
            }
        }

        setUDPPoseStreamer(adam);

        // NOTE: Static port override was already set at the very beginning (line 546)
        // The UDP server will pick it up when Pose Tracking Solution is toggled later
        Debug.Log($"[OnAdamSelected] ℹ️ Static port override (52733) was set at start. Will be applied when server starts.");

        setUDPPoseProvider("Remy");

        setPoseToWebcamStream(poseSolutionRemy, "Remy");

        var poseTrackingSolution = GameObject.Find("Pose Tracking Solution");

        ResetAllNetworkConnections();

        // CRITICAL: Before toggling, disable ALL DelayedPuppetActivation components to prevent interference
        Debug.Log($"[OnAdamSelected] Disabling all DelayedPuppetActivation components before toggle...");
        DelayedPuppetActivation[] allDelayedActivations = FindObjectsOfType<DelayedPuppetActivation>();
        foreach (var delayed in allDelayedActivations)
        {
            delayed.enabled = false;
            Debug.Log($"[OnAdamSelected] Disabled DelayedPuppetActivation on {delayed.gameObject.name}");
        }

        // DIAGNOSTIC: Check if UDP Pose Provider is a child of Pose Tracking Solution
        Transform udpPoseProviderTransform = udpPoseProvider?.transform;
        if (udpPoseProviderTransform != null)
        {
            Transform parent = udpPoseProviderTransform.parent;
            Debug.Log($"🔍 [OnAdamSelected] UDP Pose Provider parent: {(parent != null ? parent.name : "NULL/ROOT")}");
            Debug.Log($"🔍 [OnAdamSelected] Is UDP Pose Provider a child of Pose Tracking Solution? {(parent == poseTrackingSolution.transform)}");
        }

        Debug.Log($"[OnAdamSelected] Toggling Pose Tracking Solution...");
        poseTrackingSolution.SetActive(false);
        poseTrackingSolution.SetActive(true);
        Debug.Log($"✅ [OnAdamSelected] Pose Tracking Solution toggled.");

        // CRITICAL: UDP Pose Provider is NOT a child of Pose Tracking Solution, so we must restart it manually
        // This will trigger UDPPoseServer.Start() which will auto-detect the correct port
        if (udpPoseProvider != null)
        {
            Debug.Log($"[OnAdamSelected] Restarting UDP Pose Provider to trigger auto-detection...");
            UDPPoseServer serverComponent = udpPoseProvider.GetComponent<UDPPoseServer>();
            if (serverComponent != null)
            {
                Debug.Log($"[OnAdamSelected] Found UDPPoseServer component, restarting via disable/enable...");

                // Use MonoBehaviour disable/enable instead of GameObject SetActive
                // This ensures Start() runs again without Unity resetting serialized fields
                var serverBehaviour = serverComponent as MonoBehaviour;
                if (serverBehaviour != null)
                {
                    serverBehaviour.enabled = false;
                    serverBehaviour.enabled = true;
                    Debug.Log($"✅ [OnAdamSelected] UDPPoseServer component restarted. Auto-detection should have run.");
                }
            }
            else
            {
                Debug.LogError($"❌ [OnAdamSelected] UDPPoseServer component not found on UDP Pose Provider!");
            }
        }
    }

    private void PromptForIP(UDPPoseStreamer streamer)
    {
        // Use runtime UI dialog for both editor and mobile
        // This ensures consistent behavior and shows device IP on all platforms
        Debug.Log("📱 Creating runtime IP input dialog...");

        // Safety check: Ensure this MonoBehaviour is valid and enabled
        if (this == null)
        {
            Debug.LogError("❌ AvatarSelectionManager instance is null, cannot start coroutine!");
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("❌ AvatarSelectionManager GameObject is not active, cannot start coroutine!");
            return;
        }

        if (!enabled)
        {
            Debug.LogError("❌ AvatarSelectionManager component is disabled, cannot start coroutine!");
            return;
        }

        var coroutine = ShowRuntimeIPDialog(streamer);
        if (coroutine == null)
        {
            Debug.LogError("❌ ShowRuntimeIPDialog returned null!");
            return;
        }

        Debug.Log("✅ Starting ShowRuntimeIPDialog coroutine...");
        StartCoroutine(coroutine);
    }

    private void SetStreamerTarget(UDPPoseStreamer streamer, string ipAddress)
    {
        // IMPORTANT: Each avatar uses its OWN port on the remote device
        // Adam always uses port 52734 (whether sending or receiving)
        // Remy always uses port 52733 (whether sending or receiving)
        int targetPort = 52733; // Default to Remy's port

        if (streamer.gameObject.name == "Adam_Big")
        {
            // Adam's streamer sends to the remote device's port 52734
            // (where Adam on the remote device will be listening)
            targetPort = 52734;
            Debug.Log($"🎯 Adam's streamer will target port {targetPort} (Adam's port on remote device)");
        }
        else if (streamer.gameObject.name == "Remy")
        {
            // Remy's streamer sends to the remote device's port 52733
            // (where Remy on the remote device will be listening)
            targetPort = 52733;
            Debug.Log($"🎯 Remy's streamer will target port {targetPort} (Remy's port on remote device)");
        }

        // CRITICAL: Use SetTarget() method which properly sets both IP and port
        streamer.SetTarget(ipAddress, targetPort);
        Debug.Log($"✅ Set target to: {ipAddress}:{targetPort}");
    }

    private IEnumerator ShowRuntimeIPDialog(UDPPoseStreamer streamer)
    {
        // Find or create RuntimeIPInputDialog
        RuntimeIPInputDialog dialog = FindObjectOfType<RuntimeIPInputDialog>();

        if (dialog == null)
        {
            // Create a new Canvas for the dialog
            GameObject canvasObj = new GameObject("IP Input Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Make sure it's on top

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create dialog on the canvas
            dialog = canvasObj.AddComponent<RuntimeIPInputDialog>();
        }

        // Show the dialog and wait for result
        bool dialogComplete = false;
        bool wasCancelled = false;
        string resultIP = null;

        dialog.Show("192.168.0.100", (ipAddress) =>
        {
            resultIP = ipAddress;
            dialogComplete = true;
        }, () =>
        {
            // Cancel callback - re-show avatar selection
            wasCancelled = true;
            dialogComplete = true;
        });

        // Wait for dialog to complete
        while (!dialogComplete)
        {
            yield return null;
        }

        // Process the result
        if (wasCancelled)
        {
            Debug.Log("🔄 IP input cancelled - showing avatar selection dialog again");

            // IMPORTANT: Reset pose solutions to initial state when cancelling
            ResetPoseSolutionsToDefault();

            ShowAvatarSelectionDialog();
        }
        else if (!string.IsNullOrEmpty(resultIP))
        {
            SetStreamerTarget(streamer, resultIP);
        }
        else
        {
            Debug.LogWarning("⚠️ IP input cancelled or empty");
        }
    }

    /// <summary>
    /// Resets pose solutions to default state (used when cancelling IP input)
    /// </summary>
    private void ResetPoseSolutionsToDefault()
    {
        Debug.Log("🔄 Resetting pose solutions to default state...");

        // Find ALL GameObjects in scene, including inactive ones
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        GameObject poseSolution = null;
        GameObject poseSolutionRemy = null;

        // Search through all GameObjects to find pose solutions (including inactive)
        foreach (GameObject obj in allObjects)
        {
            // Only include scene objects (not prefabs or assets)
            if (obj.scene.IsValid())
            {
                if (obj.name == "Pose Solution Remy" && poseSolutionRemy == null)
                {
                    poseSolutionRemy = obj;
                    Debug.Log($"  🔍 Found 'Pose Solution Remy' (Active: {obj.activeSelf})");
                }
                else if (obj.name == "Pose Solution" && poseSolution == null)
                {
                    poseSolution = obj;
                    Debug.Log($"  🔍 Found 'Pose Solution' (Active: {obj.activeSelf})");
                }

                // Break early if we found both
                if (poseSolution != null && poseSolutionRemy != null)
                    break;
            }
        }

        // Keep both pose solutions active so they can be found by subsequent code
        if (poseSolutionRemy != null)
        {
            poseSolutionRemy.SetActive(true);

            // Disable the UDP streamer on Pose Solution Remy
            UDPPoseStreamer remyPoseStreamer = poseSolutionRemy.GetComponent<UDPPoseStreamer>();
            if (remyPoseStreamer != null)
            {
                remyPoseStreamer.enabled = false;
                Debug.Log("  ✅ Disabled UDP streamer on Pose Solution Remy");
            }
        }
        else
        {
            Debug.LogWarning("  ⚠️ Could not find 'Pose Solution Remy' GameObject!");
        }

        if (poseSolution != null)
        {
            poseSolution.SetActive(true);

            // Disable the UDP streamer on Pose Solution (Adam's)
            UDPPoseStreamer adamPoseStreamer = poseSolution.GetComponent<UDPPoseStreamer>();
            if (adamPoseStreamer != null)
            {
                adamPoseStreamer.enabled = false;
                Debug.Log("  ✅ Disabled UDP streamer on Pose Solution (Adam's)");
            }
        }
        else
        {
            Debug.LogWarning("  ⚠️ Could not find 'Pose Solution' GameObject!");
        }

        // Also disable any active UDP streamers on the avatar GameObjects themselves
        GameObject remy = GameObject.Find("Remy");
        GameObject adam = GameObject.Find("Adam_Big");

        if (remy != null)
        {
            UDPPoseStreamer remyStreamer = remy.GetComponent<UDPPoseStreamer>();
            if (remyStreamer != null)
            {
                remyStreamer.enabled = false;
                Debug.Log("  ✅ Disabled Remy's UDPPoseStreamer");
            }
        }

        if (adam != null)
        {
            UDPPoseStreamer adamStreamer = adam.GetComponent<UDPPoseStreamer>();
            if (adamStreamer != null)
            {
                adamStreamer.enabled = false;
                Debug.Log("  ✅ Disabled Adam's UDPPoseStreamer");
            }
        }

        Debug.Log("✅ Pose solutions reset complete!");
    }

    /// <summary>
    /// Shows the avatar selection dialog again (called when IP input is cancelled)
    /// </summary>
    private void ShowAvatarSelectionDialog()
    {
        // Try to find PlayerSelectionManager and show its UI
        PlayerSelectionManager playerSelectionManager = FindObjectOfType<PlayerSelectionManager>();
        if (playerSelectionManager != null)
        {
            playerSelectionManager.ShowPlayerSelection();
            Debug.Log("✅ Re-showed PlayerSelectionManager dialog");
            return;
        }

        // Try to find PlayerSelectionUI and show its UI
        PlayerSelectionUI playerSelectionUI = FindObjectOfType<PlayerSelectionUI>();
        if (playerSelectionUI != null)
        {
            playerSelectionUI.ShowSelectionUI();
            Debug.Log("✅ Re-showed PlayerSelectionUI dialog");
            return;
        }

        Debug.LogWarning("⚠️ No avatar selection UI found to re-show");
    }
}

#if UNITY_EDITOR
public class IPInputDialog : EditorWindow
{
    private string ipAddress = "192.168.1.100";
    private System.Action<string> callback;

    public static void Show(string title, string defaultIP, System.Action<string> onConfirm)
    {
        IPInputDialog window = GetWindow<IPInputDialog>(true, title, true);
        window.ipAddress = defaultIP;
        window.callback = onConfirm;
        window.minSize = new Vector2(300, 100);
        window.ShowUtility();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Enter IP Address (numbers and dots only):");
        ipAddress = GUILayout.TextField(ipAddress);
        
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("OK"))
        {
            callback?.Invoke(ipAddress);
            Close();
        }
        
        if (GUILayout.Button("Cancel"))
        {
            Close();
        }
        
        GUILayout.EndHorizontal();
    }
}
#endif
