using UnityEditor;
using UnityEngine;

public class FixFruitAssets : EditorWindow
{
    [MenuItem("Tools/Fix Fruit Assets")]
    public static void FixFruits()
    {
        // Fix Double Jump Fruit
        var doubleJump = AssetDatabase.LoadAssetAtPath<FruitDefinition>("Assets/Prefabs/Fruits/Double Jump.asset");
        if (doubleJump != null)
        {
            Undo.RecordObject(doubleJump, "Fix Double Jump Fruit");
            doubleJump.Id = "fruit_doublejump";
            doubleJump.DisplayName = "Double Jump";
            doubleJump.GrantsAbility = AbilityType.DoubleJump;
            EditorUtility.SetDirty(doubleJump);
            Debug.Log("Fixed Double Jump Fruit");
        }

        // Fix Fire Fruit
        var fireFruit = AssetDatabase.LoadAssetAtPath<FruitDefinition>("Assets/Prefabs/Fruits/Fireshot.asset");
        if (fireFruit != null)
        {
            Undo.RecordObject(fireFruit, "Fix Fire Fruit");
            fireFruit.Id = "fruit_fire";
            fireFruit.DisplayName = "Fire Shot";
            fireFruit.GrantsAbility = AbilityType.FireShot;
            EditorUtility.SetDirty(fireFruit);
            Debug.Log("Fixed Fire Fruit");
        }

        // Save all changes
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        if (doubleJump == null && fireFruit == null)
        {
            Debug.LogWarning("Could not find fruit assets. Make sure they exist at the expected paths.");
        }
    }
}
