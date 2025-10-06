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
            playerHealth = FindAnyObjectByType<PlayerHealth>();
    }

    public void OnCorpseSpawn()
    {
        if (playerHealth == null) 
        {
            Debug.LogError("[TreeCheckpoint] PlayerHealth reference is missing!");
            return;
        }

        try
        {
            // Only set the spawn point if we don't already have a valid one
            var currentSpawn = playerHealth.GetCurrentPermanentSpawn();
            if (currentSpawn == Vector3.zero || currentSpawn == playerHealth.transform.position)
            {
                // Position the spawn point to the right of the tree
                Vector3 spawnPosition = transform.position + (Vector3.right * 1.5f); // 1.5 units to the right
                
                // Raycast down to find the ground
                RaycastHit2D hit = Physics2D.Raycast(spawnPosition, Vector2.down, 10f, LayerMask.GetMask("Ground"));
                if (hit.collider != null)
                {
                    spawnPosition = hit.point;
                    spawnPosition.y += 0.5f; // Small offset above ground
                }
                
                playerHealth.SetPermanentSpawn(spawnPosition);
            }
            else
            {
                Debug.Log($"[TreeCheckpoint] Keeping existing spawn point at: {currentSpawn}");
            }

            // Heal to full health
            if (playerHealth.CurrentHearts < playerHealth.MaxHearts)
            {
                int healAmount = playerHealth.MaxHearts - playerHealth.CurrentHearts;
                playerHealth.ModifyHearts(healAmount);
            }

            if (playerHealth.CurrentHearts > 1)
            {
                playerHealth.ClearExtraSlots();
            }

            // Get inventory reference
            var inventory = playerHealth.GetComponent<FruitInventory>();
            
            // Remove all healing fruits from inventory
            if (inventory != null && healingFruit != null)
            {
                // Remove all healing fruits one by one
                int fruitCount = inventory.GetCount(healingFruit);
                if (fruitCount > 0)
                {
                    for (int i = 0; i < fruitCount; i++)
                    {
                        inventory.RemoveOne(healingFruit);
                    }
                    Debug.Log($"[TreeCheckpoint] Removed {fruitCount} {healingFruit.name} from inventory");
                }

                // Grant random fruit (excluding healing fruit)
                if (fruitDatabase != null)
                {
                    var randomFruit = fruitDatabase.GetRandomFruit(healingFruit);
                    if (randomFruit != null)
                    {
                        inventory.AddFruit(randomFruit);
                        Debug.Log($"[TreeCheckpoint] Added {randomFruit.name} to inventory");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TreeCheckpoint] Error in OnCorpseSpawn: {e.Message}");
            Debug.LogError(e.StackTrace);
        }
    }
}