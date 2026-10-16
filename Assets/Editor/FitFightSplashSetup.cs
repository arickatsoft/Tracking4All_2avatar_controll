using UnityEngine;
using UnityEditor;

public class FitFightSplashSetup
{
    [MenuItem("FitFight/Configure Splash Screen")]
    public static void ConfigureSplashScreen()
    {
        Debug.Log("Setting up FitFight splash screen...");
        
        // Enable splash screen
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.backgroundColor = Color.black;
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
        PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;
        
        Debug.Log("Basic splash screen settings configured!");
        Debug.Log("Now manually add your logo in Project Settings > Player > Splash Image");
        Debug.Log("1. Open Edit > Project Settings");
        Debug.Log("2. Click Player in the left panel");  
        Debug.Log("3. Scroll to Splash Image section");
        Debug.Log("4. Click + to add logo");
        Debug.Log("5. Drag Assets/SplashScreen/splash_m.png to the Logo field");
        
        // Try to open Project Settings
        SettingsService.OpenProjectSettings("Project/Player");
    }
    
    [MenuItem("FitFight/Test Splash Assets")]
    public static void TestSplashAssets()
    {
        string[] guids = AssetDatabase.FindAssets("splash_m t:Texture2D", new[] {"Assets/SplashScreen"});
        
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            
            if (texture != null)
            {
                Debug.Log($"✅ Found FitFight logo: {path}");
                Debug.Log($"📐 Size: {texture.width}x{texture.height}");
                Debug.Log($"📁 Format: {texture.format}");
            }
        }
        else
        {
            Debug.LogError("❌ Could not find splash_m.png in Assets/SplashScreen/");
        }
    }
}