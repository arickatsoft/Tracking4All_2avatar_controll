using UnityEngine;

public class SplashScreenTester : MonoBehaviour
{
    [Header("FitFight Splash Screen")]
    [SerializeField] private Texture2D splashLogo;
    
    void Start()
    {
        // Load the splash screen logo
        splashLogo = Resources.Load<Texture2D>("splash_m");
        if (splashLogo != null)
        {
            Debug.Log("FitFight splash screen logo loaded successfully!");
        }
        else
        {
            Debug.LogWarning("Could not load FitFight splash screen logo from Resources.");
        }
    }
}