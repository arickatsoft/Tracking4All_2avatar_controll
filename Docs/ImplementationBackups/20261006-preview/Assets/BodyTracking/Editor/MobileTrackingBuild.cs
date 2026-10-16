using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MobileTrackingBuild
{
    [MenuItem("Tools/FitFight/RTMPose/Build Android camera fix")]
    public static void Build() => BuildTo("FitFight-mobile-camera-fix.apk", "mobile-camera-build.txt");
    [MenuItem("Tools/FitFight/RTMPose/Build Android stable motion")]
    public static void BuildStableMotion() => BuildTo("FitFight-stable-motion.apk", "stable-motion-build.txt");
    [MenuItem("Tools/FitFight/Practice/Build Android spoken test")]
    public static void BuildSpokenTest() => BuildTo("FitFight-spoken-punch-test.apk", "spoken-punch-test-build.txt");
    public static void BuildSpokenTestBatch()
    {
        try { PunchValidation.Run();BuildSpokenTest();EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
    [MenuItem("Tools/FitFight/Practice/Build Android punch response")]
    public static void BuildPunchResponse() => BuildTo("FitFight-punch-response.apk", "punch-response-build.txt");
    public static void BuildPunchResponseBatch()
    {
        try { PunchValidation.Run(); BuildPunchResponse(); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    [MenuItem("Tools/FitFight/Practice/Build Android full body diagnostics")]
    public static void BuildFullBodyDiagnostics() => BuildTo("FitFight-full-body-test.apk", "full-body-test-build.txt");
    public static void BuildFullBodyDiagnosticsBatch()
    {
        try { PunchValidation.Run(); FullBodyTestValidation.Run(); BuildFullBodyDiagnostics(); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void BuildRecordedBodyFixesBatch()
    {
        try { PunchValidation.Run(); FullBodyTestValidation.Run(); BuildTo("FitFight-standing-body-fix.apk", "standing-body-fix-build.txt"); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void BuildResponsiveMovesBatch()
    {
        try { PunchValidation.Run(); FullBodyTestValidation.Run(); ResponsiveMoveValidation.Run(); BuildTo("FitFight-responsive-moves.apk", "responsive-moves-build.txt"); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void BuildEngineComparisonBatch()
    {
        try { BuildTo("FitFight-engine-comparison.apk", "engine-comparison-build.txt"); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void BuildMediaPipeImprovementsBatch()
    {
        try { BuildTo("FitFight-mediapipe-improvements.apk", "mediapipe-improvements-build.txt"); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void BuildMediaPipeIdleFixBatch()
    {
        try { BuildTo("FitFight-mediapipe-idle-fix.apk", "mediapipe-idle-fix-build.txt"); EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void BuildMediaPipeBoxingBatch()
    {
        try
        {
            MediaPipeBoxingValidation.Run();EngineComparisonValidation.Run();
            FullBodyTestValidation.Run();ResponsiveMoveValidation.Run();
            BuildTo("FitFight-mediapipe-boxing.apk", "mediapipe-boxing-build.txt");EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    private static void BuildTo(string filename, string report)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building.");
        Directory.CreateDirectory("Builds"); Directory.CreateDirectory("Logs");
        string path = Path.GetFullPath(Path.Combine("Builds", filename));
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path, target = BuildTarget.Android, options = BuildOptions.None
        });
        string message = $"{result.summary.result}; errors={result.summary.totalErrors}; warnings={result.summary.totalWarnings}; path={path}";
        File.WriteAllText(Path.Combine("Logs", report), message);
        Debug.Log(message);
        if(result.summary.result!=BuildResult.Succeeded) throw new Exception(message);
    }
}
