// Assets/Scripts/Corpse/TreeCheckpointCorpseEffect.cs
using UnityEngine;
using Systems;
using System.Collections.Generic;  

public class TreeCheckpointCorpseEffect : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private FruitDatabase fruitDatabase;
    [SerializeField] private FruitDefinition healingFruit; // Reference to the healing fruit

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
    }

    public void OnCorpseSpawn()
    {
        if (playerHealth == null) return;

        // Set new permanent spawn point
        playerHealth.SetPermanentSpawn(transform.position);

        // Heal to full
        playerHealth.SetHearts(playerHealth.BaseMaxHearts);
        playerHealth.ClearExtraSlots();

        // Remove all healing fruits from inventory
        var inventory = playerHealth.GetComponent<FruitInventory>();
        if (inventory != null && healingFruit != null)
        {
            while (inventory.GetCount(healingFruit) > 0)
            {
                inventory.RemoveOne(healingFruit);
            }
        }

        // Grant random fruit (excluding healing fruit)
        if (fruitDatabase != null)
        {
            var randomFruit = fruitDatabase.GetRandomFruit(healingFruit);

            if (randomFruit != null)
            {
                inventory?.AddFruit(randomFruit);
            }
        }
    }
}