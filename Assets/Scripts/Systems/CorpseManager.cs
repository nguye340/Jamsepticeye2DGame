using UnityEngine;

public static class CorpseManager
{
    public static void SpawnCorpse(FruitDefinition fruit, Vector3 position)
    {
        if (fruit == null || fruit.CorpsePrefab == null)
            return;

        Object.Instantiate(fruit.CorpsePrefab, position, Quaternion.identity);
    }
}