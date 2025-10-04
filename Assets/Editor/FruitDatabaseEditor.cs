// Assets/Editor/FruitDatabaseEditor.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Systems;

public class FruitDatabaseEditor : EditorWindow
{
    private Vector2 scrollPosition;
    private FruitDatabase database;
    private FruitDefinition selectedFruit;  // Store the selected fruit as a class field

    [MenuItem("Window/Fruit Database Manager")]
    public static void ShowWindow()
    {
        GetWindow<FruitDatabaseEditor>("Fruit Database");
    }

    private void OnGUI()
    {
        GUILayout.Label("Fruit Database Manager", EditorStyles.boldLabel);

        // Find or create the database
        if (database == null)
        {
            database = Resources.Load<FruitDatabase>("FruitDatabase");
            
            if (database == null)
            {
                if (GUILayout.Button("Create Fruit Database"))
                {
                    database = CreateInstance<FruitDatabase>();
                    if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }
                    string assetPath = "Assets/Resources/FruitDatabase.asset";
                    AssetDatabase.CreateAsset(database, assetPath);
                    AssetDatabase.SaveAssets();
                    database = AssetDatabase.LoadAssetAtPath<FruitDatabase>(assetPath);
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = database;
                }
                return;
            }
        }

        // Display current fruits
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Fruits", EditorStyles.boldLabel);
        
        // Start scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Make a copy of the list to avoid modifying while iterating
        var fruitsToRemove = new List<FruitDefinition>();
        
        foreach (var fruit in database.GetAllFruits())
        {
            if (fruit == null) continue;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(fruit, typeof(FruitDefinition), false);
            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                fruitsToRemove.Add(fruit);
            }
            EditorGUILayout.EndHorizontal();
        }

        // Remove selected fruits
        foreach (var fruit in fruitsToRemove)
        {
            database.RemoveFruit(fruit);
        }
        
        EditorGUILayout.EndScrollView();

        // Add new fruit
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Add New Fruit", EditorStyles.boldLabel);

        // Make the object field larger
        EditorGUILayout.BeginHorizontal();
        
        // Store the selection in the class field
        selectedFruit = (FruitDefinition)EditorGUILayout.ObjectField(
            GUIContent.none, 
            selectedFruit,  // Use the class field here
            typeof(FruitDefinition), 
            false,
            GUILayout.Height(20)
        );
        
        // Debug information about the current selection and database state
        bool hasSelectedFruit = selectedFruit != null;
        bool isInDatabase = false;
        
        if (hasSelectedFruit)
        {
            var allFruits = database.GetAllFruits();
            foreach (var fruit in allFruits)
            {
                if (fruit == selectedFruit)
                {
                    isInDatabase = true;
                    break;
                }
            }
        }
        
        bool canAddFruit = hasSelectedFruit && !isInDatabase;
        
        Debug.Log($"Selected Fruit: {selectedFruit?.name ?? "None"}, " +
                 $"Has Selected: {hasSelectedFruit}, " +
                 $"Is in DB: {isInDatabase}, " +
                 $"Can Add: {canAddFruit}");
        
        // Show the Add Fruit button but make it disabled when no fruit is selected or already in database
        GUI.enabled = canAddFruit;
        if (GUILayout.Button("Add Fruit", GUILayout.Width(100), GUILayout.Height(20)) && canAddFruit)
        {
            try
            {
                Debug.Log($"Attempting to add fruit: {selectedFruit.name}");
                
                // Force save any pending asset changes
                AssetDatabase.StartAssetEditing();
                
                // Add the fruit
                database.AddFruit(selectedFruit);
                
                // Clear the selection after adding
                var addedFruitName = selectedFruit.name;
                selectedFruit = null;
                
                // Mark the database as dirty and save
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"Successfully added {addedFruitName} to the database");
                
                // Force refresh the database reference
                string assetPath = AssetDatabase.GetAssetPath(database);
                database = AssetDatabase.LoadAssetAtPath<FruitDatabase>(assetPath);
                
                // Refresh the UI
                GUI.FocusControl(null);
                Repaint();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error adding fruit: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        // Add help box with instructions
        if (selectedFruit == null)
        {
            EditorGUILayout.HelpBox("1. Create a new FruitDefinition (Right-click in Project > Create > FruitDefinition)\n2. Select it in the field above\n3. Click 'Add Fruit' to add it to the database", MessageType.Info);
        }
        else if (database.GetAllFruits().Contains(selectedFruit))
        {
            EditorGUILayout.HelpBox("This fruit is already in the database!", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Save Database"))
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif