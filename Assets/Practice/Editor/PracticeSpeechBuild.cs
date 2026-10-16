#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class PracticeSpeechBuild
{
    [PostProcessBuild(100)]
    public static void LinkSpeech(BuildTarget target, string path)
    {
        if(target!=BuildTarget.iOS) return;
        string projectPath=PBXProject.GetPBXProjectPath(path);
        var project=new PBXProject();project.ReadFromFile(projectPath);
        project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(),"AVFoundation.framework",false);
        File.WriteAllText(projectPath,project.WriteToString());
    }
}
#endif
