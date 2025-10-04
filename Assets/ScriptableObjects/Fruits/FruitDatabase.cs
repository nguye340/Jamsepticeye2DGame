// Assets/ScriptableObjects/Fruits/FruitDatabase.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Systems
{
    [CreateAssetMenu(fileName = "FruitDatabase", menuName = "Game/Fruit Database")]
    public class FruitDatabase : ScriptableObject
    {
        [SerializeField] private List<FruitDefinition> allFruits = new List<FruitDefinition>();
        public IReadOnlyList<FruitDefinition> AllFruits => allFruits.AsReadOnly();

        // Simple method to get a random fruit
        public FruitDefinition GetRandomFruit(FruitDefinition exclude = null)
        {
            if (allFruits.Count == 0)
            {
                Debug.LogError("No fruits in the database!");
                return null;
            }

            var availableFruits = exclude != null 
                ? allFruits.Where(f => f != exclude).ToList() 
                : allFruits.ToList();

            return availableFruits.Count > 0 
                ? availableFruits[Random.Range(0, availableFruits.Count)] 
                : null;
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Only clean up in the editor
            allFruits = allFruits
                .Where(fruit => fruit != null)
                .Distinct()
                .ToList();
        }

        public void AddFruit(FruitDefinition fruit)
        {
            if (fruit == null)
            {
                Debug.LogError("Cannot add null fruit to database");
                return;
            }

            // Check if the fruit is already in the database by reference and by name
            bool alreadyExists = false;
            foreach (var f in allFruits)
            {
                if (f == fruit || (f != null && f.name == fruit.name))
                {
                    alreadyExists = true;
                    break;
                }
            }
            
            if (alreadyExists)
            {
                Debug.LogWarning($"Fruit '{fruit.name}' is already in the database");
                return;
            }

            try
            {
                Debug.Log($"Adding fruit to database: {fruit.name}");
                allFruits.Add(fruit);
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                Debug.Log($"Successfully added '{fruit.name}' to the database. Total fruits: {allFruits.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to add fruit '{fruit.name}' to database: {e.Message}");
                throw;
            }
        }

        public void RemoveFruit(FruitDefinition fruit)
        {
            if (fruit != null && allFruits.Contains(fruit))
            {
                allFruits.Remove(fruit);
                EditorUtility.SetDirty(this);
            }
        }

        public List<FruitDefinition> GetAllFruits()
        {
            // Ensure we return a new list to prevent external modifications
            return new List<FruitDefinition>(allFruits);
        }
        #endif
    }
}