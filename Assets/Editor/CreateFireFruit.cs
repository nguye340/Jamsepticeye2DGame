using UnityEditor;
using UnityEngine;

public class CreateFireFruit : EditorWindow
{
    [MenuItem("Tools/Create Fire Fruit")]
    public static void CreateFireFruitAsset()
    {
        // Create a new instance of the FruitDefinition
        var fireFruit = ScriptableObject.CreateInstance<FruitDefinition>();
        
        // Set the properties
        fireFruit.name = "Fire Fruit";
        fireFruit.Id = "fruit_fire";
        fireFruit.DisplayName = "Fire Fruit";
        fireFruit.GrantsAbility = AbilityType.FireShot;
        
        // Save the asset
        string path = "Assets/ScriptableObjects/Fruits/Fruit_Fire.asset";
        AssetDatabase.CreateAsset(fireFruit, path);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Created Fire Fruit at: {path}");
        
        // Select the newly created asset
        Selection.activeObject = fireFruit;
    }
}
