using UnityEditor;
using UnityEngine;

public class CreateFireFruitAsset : EditorWindow
{
    [MenuItem("Tools/Create Fire Fruit Asset")]
    public static void CreateFireFruit()
    {
        // Create a new instance of the FruitDefinition
        var fireFruit = ScriptableObject.CreateInstance<FruitDefinition>();
        
        // Set the properties
        fireFruit.name = "Fire Fruit";
        fireFruit.Id = "fruit_fire";
        fireFruit.DisplayName = "Fire Fruit";
        fireFruit.GrantsAbility = AbilityType.FireShot;
        
        // Ensure the directory exists
        string directory = "Assets/ScriptableObjects/Fruits";
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        // Save the asset
        string path = $"{directory}/FireFruit.asset";
        AssetDatabase.CreateAsset(fireFruit, path);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Created Fire Fruit at: {path}");
        
        // Select the newly created asset
        Selection.activeObject = fireFruit;
    }
}
