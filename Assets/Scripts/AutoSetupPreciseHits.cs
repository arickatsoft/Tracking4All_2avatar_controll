using UnityEngine;

public class AutoSetupPreciseHits : MonoBehaviour
{
    [Header("🚀 AUTO SETUP PRECISE HITS")]
    [Space(10)]
    
    [Header("Character Names")]
    public string adamName = "Adam_Big";
    public string remyName = "Remy";
    
    [Header("Position Adjustment")]
    public float heightOffset = 1f; // Add this to make body parts higher
    
    void Start()
    {
        AutoSetupBothCharacters();
    }
    
    [ContextMenu("🚀 Auto Setup Both Characters")]
    public void AutoSetupBothCharacters()
    {
        Debug.Log("🚀 AUTO SETTING UP PRECISE HIT DETECTION...");
        Debug.Log("=============================================");
        
        // Setup Adam (can be hit by Remy/Enemy)
        SetupCharacterPreciseHits(adamName, "Enemy");
        
        // Setup Remy (can be hit by Adam/Player)  
        SetupCharacterPreciseHits(remyName, "Player");
        
        Debug.Log("=============================================");
        Debug.Log("✅ AUTO SETUP COMPLETE!");
        Debug.Log("🎯 Both characters now have precise body part detection!");
    }
    
    void SetupCharacterPreciseHits(string characterName, string attackerTag)
    {
        Debug.Log($"\n🎯 Setting up {characterName} precise hits...");
        
        GameObject character = GameObject.Find(characterName);
        if (character == null)
        {
            Debug.LogError($"❌ {characterName} not found in scene!");
            return;
        }
        
        // Create or find the detector GameObject
        GameObject detectorObj = GameObject.Find($"{characterName}_HitDetector");
        if (detectorObj == null)
        {
            detectorObj = new GameObject($"{characterName}_HitDetector");
            Debug.Log($"   ✅ Created {characterName}_HitDetector GameObject");
        }
        
        // Add the precise hit detector component
        FixedPreciseBodyPartHitDetector detector = detectorObj.GetComponent<FixedPreciseBodyPartHitDetector>();
        if (detector == null)
        {
            detector = detectorObj.AddComponent<FixedPreciseBodyPartHitDetector>();
            Debug.Log($"   ✅ Added FixedPreciseBodyPartHitDetector component");
        }
        
        // Configure the detector
        detector.characterName = characterName;
        detector.attackerTag = attackerTag;
        detector.heightOffset = heightOffset;
        detector.enableDebugLogs = true;
        detector.showBodyPartNames = true;
        
        // Force setup the body parts
        detector.ForceSetupBodyParts();
        
        Debug.Log($"   ✅ {characterName} precise hit detection configured!");
        Debug.Log($"      👀 Watching for attacks from: '{attackerTag}'");
        Debug.Log($"      📏 Height offset: +{heightOffset} (makes body parts higher)");
    }
    
    [ContextMenu("🧪 Test All Body Parts on Both Characters")]
    public void TestAllBodyPartsOnBothCharacters()
    {
        Debug.Log("🧪 TESTING ALL BODY PARTS ON BOTH CHARACTERS...");
        
        // Test Adam
        GameObject adamDetector = GameObject.Find($"{adamName}_HitDetector");
        if (adamDetector != null)
        {
            FixedPreciseBodyPartHitDetector detector = adamDetector.GetComponent<FixedPreciseBodyPartHitDetector>();
            if (detector != null)
            {
                detector.TestAllBodyParts();
                Debug.Log($"✅ Tested {adamName} body parts");
            }
        }
        
        // Test Remy
        GameObject remyDetector = GameObject.Find($"{remyName}_HitDetector");
        if (remyDetector != null)
        {
            FixedPreciseBodyPartHitDetector detector = remyDetector.GetComponent<FixedPreciseBodyPartHitDetector>();
            if (detector != null)
            {
                detector.TestAllBodyParts();
                Debug.Log($"✅ Tested {remyName} body parts");
            }
        }
        
        Debug.Log("🎯 You should now see colorful circles on BOTH characters!");
        Debug.Log("🔴 Head, 🟠 Chest, 🟡 Stomach, 🔵 Arms, 🟦 Hands, 🟢 Legs, 🟣 Feet");
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 980, 400, 100), "🚀 AUTO SETUP PRECISE HITS");
        
        if (GUI.Button(new Rect(20, 1005, 150, 25), "🚀 Auto Setup Both"))
        {
            AutoSetupBothCharacters();
        }
        
        if (GUI.Button(new Rect(180, 1005, 150, 25), "🧪 Test All Parts"))
        {
            TestAllBodyPartsOnBothCharacters();
        }
        
        GUI.Label(new Rect(20, 1040, 380, 20), "✅ Creates GameObjects and scripts automatically");
        GUI.Label(new Rect(20, 1060, 380, 20), $"📏 Height offset: +{heightOffset} (adjustable in inspector)");
    }
}
