using UnityEditor;
using UnityEngine;

public class CreateDefaultFruits : EditorWindow
{
    [MenuItem("Tools/Create Default Fruits")]
    public static void CreateDefaultFruitAssets()
    {
        CreateDoubleJumpFruit();
        CreateFireFruit();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created default fruit assets");
    }

    private static void CreateDoubleJumpFruit()
    {
        var fruit = ScriptableObject.CreateInstance<FruitDefinition>();
        
        // Basic info
        fruit.name = "DoubleJump";
        fruit.Id = "fruit_doublejump";
        fruit.DisplayName = "Double Jump";
        
        // Set ability
        fruit.GrantsAbility = AbilityType.DoubleJump;
        
        // Save the asset
        string path = "Assets/Prefabs/Fruits/DoubleJump.asset";
        AssetDatabase.CreateAsset(fruit, path);
        
        // Try to find and assign the icon if it exists
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Fruits/doublejump_icon.png");
        if (icon != null)
        {
            fruit.Icon = icon;
            EditorUtility.SetDirty(fruit);
        }
        
        Debug.Log($"Created Double Jump fruit at: {path}");
    }

    private static void CreateFireFruit()
    {
        var fruit = ScriptableObject.CreateInstance<FruitDefinition>();
        
        // Basic info
        fruit.name = "FireFruit";
        fruit.Id = "fruit_fire";
        fruit.DisplayName = "Fire Shot";
        
        // Set ability
        fruit.GrantsAbility = AbilityType.FireShot;
        
        // Save the asset
        string path = "Assets/Prefabs/Fruits/FireFruit.asset";
        AssetDatabase.CreateAsset(fruit, path);
        
        // Try to find and assign the icon if it exists
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Fruits/fire_icon.png");
        if (icon != null)
        {
            fruit.Icon = icon;
            EditorUtility.SetDirty(fruit);
        }
        
        Debug.Log($"Created Fire Fruit at: {path}");
    }
}
