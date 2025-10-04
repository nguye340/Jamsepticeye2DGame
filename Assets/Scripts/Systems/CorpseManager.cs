using UnityEngine;

namespace Systems
{
    public static class CorpseManager
    {
        [Header("Prefabs")]
        [SerializeField] private static GameObject defaultCorpsePrefab;
        private const string DEFAULT_CORPSE_PATH = "Corpses/DefaultCorpse";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (defaultCorpsePrefab == null)
            {
                defaultCorpsePrefab = Resources.Load<GameObject>(DEFAULT_CORPSE_PATH);
                if (defaultCorpsePrefab == null)
                {
                    Debug.LogError($"Default corpse prefab not found at path: {DEFAULT_CORPSE_PATH}");
                }
            }
        }

        public static GameObject SpawnCorpseForDeath(
            DeathType type, 
            Vector3 pos, 
            FruitInventory inv, 
            bool isLastLife, 
            FruitDefinition chosenByPlayerOrNull)
        {
            if (inv == null)
            {
                Debug.LogError("FruitInventory is null in SpawnCorpseForDeath");
                return SpawnDefaultCorpse(pos);
            }

            // Determine if player specifically chose a fruit
            bool playerChoseSpecific = chosenByPlayerOrNull != null;

            // Let the resolver decide which fruit to use
            FruitDefinition fruitToUse = CorpseImbueResolver.ChooseFruitForCorpse(
                type, inv, isLastLife, playerChoseSpecific, chosenByPlayerOrNull);

            // Spawn the appropriate corpse
            if (fruitToUse != null && fruitToUse.CorpsePrefab != null)
            {
                return Object.Instantiate(fruitToUse.CorpsePrefab, pos, Quaternion.identity);
            }

            // Fall back to default corpse
            return SpawnDefaultCorpse(pos);
        }

        private static GameObject SpawnDefaultCorpse(Vector3 position)
        {
            if (defaultCorpsePrefab == null)
            {
                Debug.LogError("Default corpse prefab is not assigned and couldn't be loaded from resources!");
                return null;
            }

            return Object.Instantiate(defaultCorpsePrefab, position, Quaternion.identity);
        }

        // Kept for backward compatibility
        public static void SpawnCorpse(FruitDefinition fruit, Vector3 position)
        {
            if (fruit?.CorpsePrefab != null)
            {
                Object.Instantiate(fruit.CorpsePrefab, position, Quaternion.identity);
            }
            else
            {
                SpawnDefaultCorpse(position);
            }
        }
    }
}