using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class FitFightPresentationCapture
{
    [MenuItem("Tools/FitFight/Diagnostics/Ground contact")]
    public static void GroundContact()
    {
        var report = new StringBuilder();
        foreach (var ground in Object.FindObjectsByType<FitFightFighterGrounding>(FindObjectsSortMode.None))
        {
            report.AppendLine(ground.name + " root=" + ground.transform.position + " config=" + JsonUtility.ToJson(ground));
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            foreach (var field in new[] { "floorY", "lift", "clearance", "configured" })
                report.AppendLine(field + "=" + typeof(FitFightFighterGrounding).GetField(field, flags).GetValue(ground));
            foreach (var skin in ground.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!skin.name.ToLowerInvariant().Contains("shoe") && !skin.name.ToLowerInvariant().Contains("sneaker")) continue;
                var mesh = new Mesh(); skin.BakeMesh(mesh, true);
                float scaled = float.PositiveInfinity, unscaled = float.PositiveInfinity, cpu = float.PositiveInfinity;
                foreach (var v in mesh.vertices)
                {
                    scaled = Mathf.Min(scaled, skin.transform.TransformPoint(v).y);
                    unscaled = Mathf.Min(unscaled, (skin.transform.position + skin.transform.rotation * v).y);
                }
                var source = skin.sharedMesh; var positions = source.vertices; var weights = source.boneWeights; var binds = source.bindposes;
                foreach (int i in source.triangles)
                {
                    var w = weights[i]; Vector3 v = positions[i];
                    Vector3 p = (skin.bones[w.boneIndex0].localToWorldMatrix * binds[w.boneIndex0]).MultiplyPoint3x4(v) * w.weight0
                        + (skin.bones[w.boneIndex1].localToWorldMatrix * binds[w.boneIndex1]).MultiplyPoint3x4(v) * w.weight1
                        + (skin.bones[w.boneIndex2].localToWorldMatrix * binds[w.boneIndex2]).MultiplyPoint3x4(v) * w.weight2
                        + (skin.bones[w.boneIndex3].localToWorldMatrix * binds[w.boneIndex3]).MultiplyPoint3x4(v) * w.weight3;
                    cpu = Mathf.Min(cpu, p.y);
                }
                report.AppendLine(skin.name + " scale=" + skin.transform.lossyScale + " bakedScaled=" + scaled + " bakedUnscaled=" + unscaled + " skinWorld=" + cpu);
                Object.Destroy(mesh);
            }
        }
        File.WriteAllText("/tmp/fitfight-ground-contact.txt", report.ToString());
    }
    [MenuItem("Tools/FitFight/Build Android tracking verification")]
    public static void BuildAndroidVerification()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode before building.");
        Directory.CreateDirectory("Builds");
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes) if (scene.enabled) scenes.Add(scene.path);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = scenes.ToArray(), target = BuildTarget.Android,
            locationPathName = "Builds/FitFight-tracking-debug.apk",
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        });
        File.WriteAllText("/tmp/fitfight-android-build-result.txt", report.summary.result + " errors=" + report.summary.totalErrors + " path=" + report.summary.outputPath);
    }

    [MenuItem("Tools/FitFight/Diagnostics/Select Remy")]
    public static void SelectRemy() { Select("Remy"); }
    [MenuItem("Tools/FitFight/Diagnostics/Select Adam")]
    public static void SelectAdam() { Select("Adam"); }
    private static void Select(string name)
    {
        if (!EditorApplication.isPlaying) return;
        var player = Object.FindFirstObjectByType<PlayerSelectionManager>();
        var type = typeof(PlayerSelectionManager).GetNestedType("PlayerType", System.Reflection.BindingFlags.NonPublic);
        typeof(PlayerSelectionManager).GetMethod("OnPlayerSelected", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(player, new[] { System.Enum.Parse(type, name) });
    }

    private static object Field(object target, string name)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
            if (field != null) return field.GetValue(target);
        }
        return null;
    }

    [MenuItem("Tools/FitFight/Diagnostics/Sample tracking")]
    public static void SampleTracking()
    {
        if (!EditorApplication.isPlaying) return;
        var text = new StringBuilder();
        var puppets = Object.FindObjectsByType<Tracking4All.PosePuppet>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var counts = new int[puppets.Length];
        var graphs = Object.FindObjectsByType<MPUPoseTrackingSolution>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var rawCounts = new int[graphs.Length];
        var rawCallbacks = new Tracking4All.GroupEvent[graphs.Length];
        for (int i = 0; i < graphs.Length; i++)
        {
            int index = i;
            rawCallbacks[i] = group => System.Threading.Interlocked.Increment(ref rawCounts[index]);
            ((Tracking4All.ILandmarkProvider<Tracking4All.MPPoseLandmarks>)graphs[i]).Lifecycle.OnUpdated += rawCallbacks[i];
        }
        var callbacks = new Tracking4All.GroupEvent[puppets.Length];
        for (int i = 0; i < puppets.Length; i++)
        {
            int index = i;
            callbacks[i] = group => counts[index]++;
            puppets[i].OnUpdated += callbacks[i];
        }
        double deadline = EditorApplication.timeSinceStartup + 2;
        EditorApplication.CallbackFunction finish = null;
        finish = () => {
            if (EditorApplication.timeSinceStartup < deadline) return;
            EditorApplication.update -= finish;
            for (int i = 0; i < graphs.Length; i++)
            {
                var graph = graphs[i];
                if (graph == null) continue;
                ((Tracking4All.ILandmarkProvider<Tracking4All.MPPoseLandmarks>)graph).Lifecycle.OnUpdated -= rawCallbacks[i];
                text.AppendLine($"GRAPH {graph.name} active={graph.isActiveAndEnabled} stopped={Field(graph, "stopped")} cameraReady={graph.VideoSourceProperties?.IsPrepared} camera={graph.VideoSourceProperties?.Name} rawPosesIn2s={rawCounts[i]} waiting={graph.OptionalWait()}");
            }
            for (int i = 0; i < puppets.Length; i++)
            {
                if (puppets[i] == null) continue;
                puppets[i].OnUpdated -= callbacks[i];
                var provider = Field(puppets[i], "poseProvider") as Tracking4All.LandmarkProvider<Tracking4All.PoseLandmarks>;
                text.AppendLine($"PUPPET {puppets[i].name} active={puppets[i].isActiveAndEnabled} source={provider?.Source?.name} updatesIn2s={counts[i]}");
            }
            foreach (var avatar in Object.FindObjectsByType<Tracking4All.Avatar>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                text.AppendLine($"AVATAR {avatar.name} active={avatar.isActiveAndEnabled} freshPose={avatar.HasFreshPose}");
            File.WriteAllText("/tmp/fitfight-tracking-diagnostics.txt", text.ToString());
        };
        EditorApplication.update += finish;
    }

    [MenuItem("Tools/FitFight/Capture presentation preview")]
    public static void Capture()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Enter Play Mode before capturing."); return; }
        var selection = GameObject.Find("FitFight Selection");
        if (selection != null) selection.SetActive(false);
        var details = new StringBuilder();
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            details.AppendLine(camera.name + " enabled=" + camera.enabled + " framing=" + (camera.GetComponent<FitFightArenaCamera>() != null)
                + " position=" + camera.transform.position + " aspect=" + camera.aspect);
        foreach (var avatar in Object.FindObjectsByType<Tracking4All.Avatar>(FindObjectsSortMode.None))
        {
            details.AppendLine("AVATAR " + avatar.name + " position=" + avatar.transform.position + " scale=" + avatar.transform.lossyScale + " live=" + avatar.HasFreshPose);
            foreach (var renderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
                details.AppendLine(" MESH " + renderer.name + " bounds=" + renderer.bounds);
            var animator = avatar.GetComponent<Animator>();
            if (animator != null && animator.isHuman)
                foreach (var bone in new[] { HumanBodyBones.Head, HumanBodyBones.LeftFoot, HumanBodyBones.RightHand })
                    details.AppendLine(" BONE " + bone + "=" + animator.GetBoneTransform(bone).position);
        }
        File.WriteAllText("/tmp/fitfight-reference-preview.txt", details.ToString());
        ScreenCapture.CaptureScreenshot("/tmp/fitfight-reference-preview.png", 2);
        // Preview only; restore selection after the frame has been saved.
        double deadline = EditorApplication.timeSinceStartup + 2;
        EditorApplication.CallbackFunction restore = null;
        restore = () => {
            if (EditorApplication.timeSinceStartup < deadline) return;
            EditorApplication.update -= restore;
            if (selection != null) selection.SetActive(true);
        };
        EditorApplication.update += restore;
    }
}
