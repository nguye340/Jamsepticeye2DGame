using UnityEngine;
using UnityEngine.Events;
using Systems;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int currentHearts = 3;
    [SerializeField] private int maxHearts = 3;
    [SerializeField] private int baseMaxHearts = 3;
    [SerializeField] private int extraSlotsFromHealing = 0;
    public int BaseMaxHearts => baseMaxHearts;

    [Header("Spawn Points")]
    [SerializeField] private Transform startSpawn;
    [SerializeField] private Transform currentPermanentSpawn;
    [SerializeField] private Transform lastLocalSpawn;

    [Header("References")]
    [SerializeField] private PlayerAbilityController abilityController;
    [SerializeField] private FruitInventory fruitInventory;
    [Header("Events")]
    public UnityEvent onRespawn;
    public UnityEvent onDeath;
    public UnityEvent<DeathType, Vector3> onDied;

    [Header("References")]
    [SerializeField] private SimpleFruitChoiceUI fruitChoiceUI;
    // Temporary field for player's fruit choice (will be set by UI)
    private FruitDefinition pendingChosenFruit;
    
    private void Awake()
    {
        if (abilityController == null)
            abilityController = GetComponent<PlayerAbilityController>();
            
        if (fruitInventory == null)
            fruitInventory = GetComponent<FruitInventory>();
            
        // Subscribe to our own death event
        onDied.AddListener(HandleDeath);
    }

    private void OnDestroy()
    {
        // Clean up the subscription
        onDied.RemoveListener(HandleDeath);
    }

    private void HandleDeath(DeathType type, Vector3 deathPos)
    {
        bool isLastLife = currentHearts <= 0;
        
        // Spawn the corpse
        CorpseManager.SpawnCorpseForDeath(
            type, 
            deathPos, 
            fruitInventory, 
            isLastLife, 
            pendingChosenFruit
        );
        
        // Reset the pending choice
        pendingChosenFruit = null;

        // Handle hearts logic
        switch (type)
        {
            case DeathType.Unintentional:
                currentHearts = baseMaxHearts;
                extraSlotsFromHealing = 0;
                break;
                
            case DeathType.Intentional:
                if (currentHearts > 0)
                {
                    currentHearts--;
                }
                else
                {
                    currentHearts = baseMaxHearts;
                }
                break;
        }
    }

    public bool CanIntentionalDie()
    {
        return currentHearts > 0 || (currentHearts == 0 && fruitInventory != null && fruitInventory.HasAnyFruit());
    }

    public void TakeDamage(int amount)
    {
        currentHearts = Mathf.Max(0, currentHearts - amount);
        if (currentHearts <= 0)
        {
            DieInternal(DeathType.Unintentional);
        }
    }

    private void IntentionalSacrifice()
    {
        // Get the inventory component
        var inventory = GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("Inventory component not found on player!");
            return;
        }

        if (currentHearts > 0 && inventory.TotalCount() >= 2)
        {
            var fruitDB = FindObjectOfType<FruitDatabase>();
            if (fruitDB == null)
            {
                Debug.LogError("FruitDatabase not found in scene!");
                return;
            }

            var allFruits = fruitDB.GetAllFruits();
            
            if (allFruits.Count == 1)
            {
                // If only one fruit type, skip UI
                pendingChosenFruit = allFruits[0];
                DieInternal(DeathType.Intentional);
            }
            else
            {
                // Show UI for multiple fruits
                fruitChoiceUI.ShowFruitChoice(allFruits, "Choose a fruit to sacrifice:", (chosenFruit) => 
                {
                    pendingChosenFruit = chosenFruit;
                    DieInternal(DeathType.Intentional);
                });
            }
        }
        else
        {
            DieInternal(DeathType.Normal);
        }
    }

    private void DieInternal(DeathType type)
    {
        Vector3 deathPos = transform.position;
        
        // Determine respawn kind
        RespawnKind respawnKind = type switch
        {
            DeathType.Unintentional => RespawnKind.Start, // might need to add when currentHearts == 0
            DeathType.Intentional when currentHearts == 0 => RespawnKind.Start,
            DeathType.Intentional => RespawnKind.Local,
            _ => RespawnKind.Start
        };

        // Invoke death event
        onDeath?.Invoke();
        onDied?.Invoke(type, deathPos);

        // Handle respawn
        Respawn(respawnKind, deathPos);
    }

    private void Respawn(RespawnKind kind, Vector3 deathPos)
    {
        switch (kind)
        {
            case RespawnKind.Local:
                // For local respawn, store the death position and move there
                if (lastLocalSpawn == null)
                {
                    var spawnObj = new GameObject("LocalSpawnPoint");
                    lastLocalSpawn = spawnObj.transform;
                }
                lastLocalSpawn.position = deathPos;
                transform.position = lastLocalSpawn.position;
                break;

            case RespawnKind.Permanent:
                transform.position = GetCurrentPermanentSpawn().position;
                lastLocalSpawn = null;
                break;

            case RespawnKind.Start:
            default:
                transform.position = GetCurrentPermanentSpawn().position;
                lastLocalSpawn = null;
                break;
        }

        onRespawn?.Invoke();
    }

    private Transform GetCurrentPermanentSpawn()
    {
        return currentPermanentSpawn != null ? currentPermanentSpawn : startSpawn;
    }

    // For testing in the editor
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TakeDamage(1);
        }
    }

    // For the special case of healing tree death
    public void SetPermanentSpawn(Vector3 position)
    {
        if (currentPermanentSpawn == null)
        {
            var spawnObj = new GameObject("PermanentSpawn");
            currentPermanentSpawn = spawnObj.transform;
        }
        currentPermanentSpawn.position = position;
    }

    public void SetHearts(int amount)
    {
        currentHearts = Mathf.Clamp(amount, 0, maxHearts);
    }

    public void ClearExtraSlots()
    {
        extraSlotsFromHealing = 0;
    }
}