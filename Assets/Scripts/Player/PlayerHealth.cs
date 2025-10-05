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
    private FruitDatabase fruitDB;

    private void Awake()
    {
        if (abilityController == null)
            abilityController = GetComponent<PlayerAbilityController>();
                
        if (fruitInventory == null)
            fruitInventory = GetComponent<FruitInventory>();
        
        // Try to load the database
        fruitDB = Resources.Load<FruitDatabase>("FruitDatabase");
        
        // Debug information
        if (fruitDB != null)
        {
            Debug.Log("Successfully loaded FruitDatabase");
        }
        else
        {
            Debug.LogError("Failed to load FruitDatabase! Make sure there's a FruitDatabase.asset in a Resources folder.");
            
            // List all available resources for debugging
            var allResources = Resources.LoadAll<FruitDatabase>("");
            Debug.Log($"Found {allResources.Length} FruitDatabase assets in all Resources folders");
            foreach (var db in allResources)
            {
                Debug.Log($"Found FruitDatabase: {db.name}");
            }
        }
                
        // Try to find the UI if not assigned
        if (fruitChoiceUI == null)
        {
            fruitChoiceUI = FindObjectOfType<SimpleFruitChoiceUI>();
            if (fruitChoiceUI == null)
            {
                Debug.LogError("Could not find SimpleFruitChoiceUI in the scene! Make sure it exists and is active.");
            }
            else
            {
                Debug.Log("Found SimpleFruitChoiceUI in the scene");
            }
        }
        
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
        return currentHearts > 0 || (currentHearts == 0 && fruitInventory != null && fruitInventory.TotalCount() >= 2);
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
        Debug.Log("IntentionalSacrifice called");
        
        // Get the fruit inventory component
        if (fruitInventory == null)
        {
            fruitInventory = GetComponent<FruitInventory>();
            if (fruitInventory == null)
            {
                Debug.LogError("FruitInventory component not found on player!");
                return;
            }
        }

        Debug.Log($"Current hearts: {currentHearts}, Fruit count: {fruitInventory.TotalCount()}");

        if (currentHearts > 0 && fruitInventory.TotalCount() >= 1)
        {
            Debug.Log("Checking fruit database...");
            if (fruitDB == null)
            {
                Debug.LogError("FruitDatabase reference is null!");
                return;
            }

            // Get only the fruits that are in our inventory
            var fruitsInInventory = new List<FruitDefinition>();
            foreach (var fruitType in fruitInventory.GetAllTypes())
            {
                if (fruitInventory.GetCount(fruitType) > 0)
                {
                    fruitsInInventory.Add(fruitType);
                }
            }
            
            Debug.Log($"Found {fruitsInInventory.Count} fruits in inventory");
            
            if (fruitsInInventory.Count == 0)
            {
                Debug.LogError("No fruits found in inventory!");
                return;
            }
            
            if (fruitsInInventory.Count == 1)
            {
                Debug.Log("Only one fruit type in inventory, skipping UI");
                pendingChosenFruit = fruitsInInventory[0];
                DieInternal(DeathType.Intentional);
            }
            else
            {
                Debug.Log("Multiple fruits in inventory, showing UI");
                if (fruitChoiceUI == null)
                {
                    Debug.LogError("fruitChoiceUI is not assigned!");
                    return;
                }
                
                // Show UI for multiple fruits
                fruitChoiceUI.ShowFruitChoice(fruitsInInventory, "Sow your sacrifice?", (chosenFruit) => 
                {
                    Debug.Log($"Fruit chosen: {chosenFruit?.name ?? "null"}");
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
                // Add a small vertical offset to prevent clipping through the ground
                Vector3 spawnPosition = deathPos + Vector3.up * 0.5f;
                lastLocalSpawn.position = spawnPosition;
                transform.position = spawnPosition;
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
            if (CanIntentionalDie())
            {
                IntentionalSacrifice();
            }
            else
            {
                Debug.Log("Cannot sacrifice - need at least 2 fruits in inventory or be at 0 hearts with fruits");
            }
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