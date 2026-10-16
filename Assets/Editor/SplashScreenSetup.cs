using UnityEngine;
using UnityEditor;

public class SplashScreenSetup
{
    [MenuItem("FitFight/Setup Splash Screen")]
    public static void SetupSplashScreen()
    {
        // Load the FitFight logo
        Texture2D logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SplashScreen/splash_m.png");
        
        if (logoTexture != null)
        {
            // Convert texture to sprite for splash screen use
            string logoPath = AssetDatabase.GetAssetPath(logoTexture);
            TextureImporter importer = AssetImporter.GetAtPath(logoPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                AssetDatabase.ImportAsset(logoPath);
            }
            
            // Load as sprite after import
            Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SplashScreen/splash_m.png");
            
            if (logoSprite != null)
            {
                // Configure Player Settings for splash screen
                PlayerSettings.SplashScreen.show = true;
                
                // Try to disable Unity logo (only works with Pro license)
                try 
                {
                    PlayerSettings.SplashScreen.showUnityLogo = false;
                }
                catch (System.Exception)
                {
                    Debug.LogWarning("Cannot hide Unity logo - requires Unity Pro/Plus license");
                }
                
                PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
                
                // Set background color to black (like your image)
                PlayerSettings.SplashScreen.backgroundColor = Color.black;
                
                // Clear existing logos and add our custom logo
                var newLogos = new PlayerSettings.SplashScreenLogo[1];
                newLogos[0] = PlayerSettings.SplashScreenLogo.Create(2.0f, logoSprite);
                
                PlayerSettings.SplashScreen.logos = newLogos;
                
                // Set timing
                PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;
                
                Debug.Log("FitFight splash screen configured successfully!");
                Debug.Log("Logo sprite created and assigned to splash screen.");
                
                // Check license type
                if (Application.HasProLicense())
                {
                    Debug.Log("Unity Pro detected - Unity logo can be hidden.");
                }
                else
                {
                    Debug.Log("Unity Personal detected - Unity logo will appear alongside your logo.");
                }
            }
            else
            {
                Debug.LogError("Could not convert texture to sprite for splash screen use.");
            }
        }
        else
        {
            Debug.LogError("Could not find splash_m.png in Assets/SplashScreen/");
        }
        
        // Save the project to apply changes
        AssetDatabase.SaveAssets();
    }
}