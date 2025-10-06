using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Systems;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    public enum RespawnKind { Start, Local, Permanent }
    
    [Header("Health Settings")]
    [SerializeField] private int currentHearts = 3;
    [SerializeField] private int baseMaxHearts = 3;
    [SerializeField] private int extraSlotsFromHealing = 0;
    [SerializeField] private bool isDying = false;
    
    // Public properties
    public int CurrentHearts => currentHearts;
    public int BaseMaxHearts => baseMaxHearts;
    public int MaxHearts => baseMaxHearts + extraSlotsFromHealing;
    public int ExtraSlotsFromHealing => extraSlotsFromHealing;
    public bool IsDying => isDying;
    
    // Public event
    public event Action OnHeartsChanged = delegate { };

    [Header("Spawn Points")]
    [SerializeField] private Transform startSpawn;
    [SerializeField] private Transform currentPermanentSpawn;
    [SerializeField] private Transform lastLocalSpawn;

    [Header("References")]
    [SerializeField] private PlayerAbilityController abilityController;
    [SerializeField] private FruitInventory fruitInventory;
    [SerializeField] private PlayerHUD playerHUD;
    [SerializeField] private SpriteRenderer playerRenderer;
    
    [Header("Healing")]
    [SerializeField] private float healPulseIntensity = 0.3f;
    [SerializeField] private float healPulseSpeed = 2f;
    
    private Color originalPlayerColor;
    private bool isHealing = false;
    private float accumulatedHealing = 0f; // Track partial healing amounts
    
    [Header("Events")]
    public UnityEvent onRespawn;
    public UnityEvent onDeath;
    public UnityEvent<DeathType, Vector3> onDied;

    [Header("UI")]
    [SerializeField] private SimpleFruitChoiceUI fruitChoiceUI;
    
    [Header("Corpse Settings")]
    [SerializeField] private GameObject defaultCorpsePrefab;
    [SerializeField] private float corpseSpawnOffset = 0.5f;
    
    // Private fields
    private FruitDefinition pendingChosenFruit;
    private FruitDatabase fruitDB;
    private Vector3 respawnPosition;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize player renderer and color if not set
        if (playerRenderer == null)
            playerRenderer = GetComponentInChildren<SpriteRenderer>();
            
        if (playerRenderer != null)
            originalPlayerColor = playerRenderer.color;

        // Initialize references
        if (playerHUD == null)
            playerHUD = FindAnyObjectByType<PlayerHUD>();

        // Get or create FruitInventory
        if (fruitInventory == null)
        {
            var fruitInventoryObj = new GameObject("FruitInventory");
            fruitInventory = fruitInventoryObj.AddComponent<FruitInventory>();
            DontDestroyOnLoad(fruitInventoryObj);
        }

        if (abilityController == null)
            abilityController = GetComponent<PlayerAbilityController>();

        if (fruitChoiceUI == null)
            fruitChoiceUI = FindAnyObjectByType<SimpleFruitChoiceUI>();
            
        // Initialize event
        OnHeartsChanged = null;
        
        // Ensure we're using the singleton instance
        if (FruitInventory.Instance != null)
        {
            fruitInventory = FruitInventory.Instance;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isDying && CanIntentionalDie())
        {
            Debug.Log("Q key pressed - Starting sacrifice process");
            isDying = true; // Set this immediately to prevent multiple triggers
            StartCoroutine(ProcessSacrifice());
        }
        
        // Handle healing visual effects
        if (playerRenderer != null)
        {
            if (!isHealing && playerRenderer.color != originalPlayerColor)
            {
                playerRenderer.color = Color.Lerp(playerRenderer.color, originalPlayerColor, Time.deltaTime * 5f);
            }
        }
    }
    
    private IEnumerator ProcessSacrifice()
    {
        Debug.Log("ProcessSacrifice started");
        
        // Set dying state immediately to prevent multiple sacrifices
        isDying = true;
        
        try
        {
            // Wait one frame to ensure all state is consistent
            yield return null;
            
            // Process the sacrifice and wait for it to complete
            bool sacrificeCompleted = false;
            
            // Process the sacrifice
            IEnumerator sacrificeRoutine = IntentionalSacrifice(() => {
                Debug.Log("Sacrifice completed successfully");
                sacrificeCompleted = true;
            });
            
            if (sacrificeRoutine == null)
            {
                Debug.LogError("Failed to create sacrifice routine");
                isDying = false;
                yield break;
            }
            
            // Start the sacrifice routine and wait for it to complete
            yield return StartCoroutine(sacrificeRoutine);
            
            // If we get here, the sacrifice completed successfully
            Debug.Log($"Sacrifice completed. Hearts: {currentHearts}");
            
            // If we still have hearts, we're done
            if (currentHearts > 0)
            {
                Debug.Log("Player still has hearts, not respawning");
                isDying = false;
                yield break;
            }
            
            // Otherwise, handle respawn
            Debug.Log("Handling respawn after sacrifice...");
            
            // Wait for respawn to complete
            IEnumerator respawnRoutine = RespawnAfterDelay(DeathType.Intentional, transform.position);
            if (respawnRoutine != null)
            {
                yield return StartCoroutine(respawnRoutine);
            }
            
            Debug.Log("Respawn after sacrifice complete");
        }
        finally
        {
            // Only reset dying state if we still have hearts left
            if (currentHearts > 0)
            {
                isDying = false;
                Debug.Log("Reset isDying to false in ProcessSacrifice");
            }
        }
    }

    private void HandleDeath(DeathType type, Vector3 deathPos)
    {
        Debug.Log($"HandleDeath called with type: {type} at position: {deathPos}");
        
        // Invoke death events
        onDied?.Invoke(type, deathPos);
        onDeath?.Invoke();
        
        try
        {
            // Handle corpse spawning based on death type
            switch (type)
            {
                case DeathType.Intentional:
                    Debug.Log($"Intentional death - Pending fruit: {pendingChosenFruit?.name ?? "null"}");
                    
                    // Set the checkpoint to the tree's position when sacrificing a fruit
                    var treeCheckpoint = FindAnyObjectByType<TreeCheckpointCorpseEffect>();
                    if (treeCheckpoint != null)
                    {
                        Debug.Log($"Setting checkpoint to tree at position: {treeCheckpoint.transform.position}");
                        SetPermanentSpawn(treeCheckpoint.transform.position);
                    }
                    
                    // Try to spawn the specific fruit's corpse if available
                    if (pendingChosenFruit != null)
                    {
                        if (pendingChosenFruit.CorpsePrefab != null)
                        {
                            // Spawn the specific fruit's corpse
                            SpawnCorpse(pendingChosenFruit.CorpsePrefab, deathPos);
                            Debug.Log($"Spawned {pendingChosenFruit.name} corpse at {deathPos}");
                            return; // Successfully spawned specific corpse
                        }
                        else
                        {
                            Debug.LogWarning($"No corpse prefab assigned to fruit: {pendingChosenFruit.name}");
                        }
                    }
                    // Fall through to spawn default corpse if specific one couldn't be spawned
                    Debug.Log("Falling back to default corpse for intentional death");
                    goto case DeathType.Unintentional;
                    
                case DeathType.Unintentional:
                case DeathType.Normal:
                default:
                    // Always spawn default corpse for non-intentional deaths or fallback
                    SpawnDefaultCorpse(deathPos);
                    Debug.Log($"Spawned default corpse (death type: {type})");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in HandleDeath: {ex.Message}\n{ex.StackTrace}");
            // Try to spawn default corpse even if there was an error
            try
            {
                SpawnDefaultCorpse(deathPos);
                Debug.Log("Spawned default corpse after error");
            }
            catch (Exception ex2)
            {
                Debug.LogError($"Failed to spawn default corpse: {ex2.Message}");
            }
        }

        // Handle different death types
        switch (type)
        {
            case DeathType.Intentional:
                // For intentional deaths, we've already handled fruit selection
                break;
            case DeathType.Unintentional:
                // For unintentional deaths, drop a random fruit if available
                if (fruitInventory != null)
                {
                    var randomFruit = fruitInventory.RemoveOneRandomFruit();
                    if (randomFruit != null)
                    {
                        //Debug.Log($"Dropping fruit on death: {randomFruit.name}");
                        // TODO: Implement fruit drop logic
                    }
                }
                break;
        }

        // Handle respawn
        Respawn(RespawnKind.Local);
    }

    public bool CanIntentionalDie()
    {
        // Player can always intentionally die, but will get different results based on fruit
        return true;
    }

    public void TakeDamage(int amount)
    {
        if (isDying) return; // Prevent multiple damage calls during death sequence
        
        Debug.Log($"TakeDamage called. Current hearts: {currentHearts}, Amount: {amount}");
        
        // Always remove exactly 1 heart per damage instance
        currentHearts = Mathf.Max(0, currentHearts - amount);
        OnHeartsChanged?.Invoke();
        
        Debug.Log($"Hearts after damage: {currentHearts}");
        
        // Check if player has no hearts left
        if (currentHearts <= 0)
        {
            Debug.Log("Player has no hearts left, triggering death");
            isDying = true;
            DieInternal(DeathType.Unintentional);
        }
        else
        {
            // For non-fatal damage, just take the hit and continue
            Debug.Log($"Player took {amount} damage. Hearts remaining: {currentHearts}");
        }
    }
    private IEnumerator IntentionalSacrifice(Action onComplete)
    {
        Debug.Log("=== INTENTIONAL SACRIFICE STARTED ===");
        
        try
        {
            if (fruitInventory == null)
            {
                fruitInventory = GetComponent<FruitInventory>();
                if (fruitInventory == null)
                {
                    Debug.LogError("FruitInventory component not found on player!");
                    isDying = false; // Reset dying state
                    onComplete?.Invoke();
                    yield break;
                }
            }

        // Log all fruits in inventory for debugging
        Debug.Log("Checking fruit inventory...");
        var allFruits = fruitInventory.GetAllTypes();
        Debug.Log($"Found {allFruits.Count} fruit types in inventory");
        
        foreach (var fruit in allFruits)
        {
            int count = fruitInventory.GetCount(fruit);
            Debug.Log($"- {fruit.name}: {count}");
        }

        // Store death position for corpse spawning
        Vector3 deathPos = transform.position;

        // Check if we have any fruits available for sacrifice
        if (allFruits.Count == 0 || fruitInventory.TotalCount() == 0)
        {
            Debug.Log("No fruits available for sacrifice, using default death");
            
            Debug.Log("No fruits - taking damage and spawning default corpse");
            
            // Take damage for the sacrifice
            TakeDamage(1);
            
            // Trigger death effects
            onDeath?.Invoke();
            
            // Spawn default corpse
            SpawnDefaultCorpse(deathPos);
            
            // Start respawn coroutine
            StartCoroutine(RespawnAfterDelay(DeathType.Unintentional, deathPos));
            onComplete?.Invoke();
            yield break;
        }
        
        // Get all available fruits (all fruits can be sacrificed in this implementation)
        var availableFruits = allFruits.ToList();
            
        // Handle single fruit case
        if (availableFruits.Count == 1)
        {
            Debug.Log("Only one fruit type, auto-selecting it");
            pendingChosenFruit = availableFruits[0];
            
            // Remove the fruit from inventory
            fruitInventory.RemoveOne(pendingChosenFruit);
            
            // Take damage (only once)
            TakeDamage(1);
            
            // Trigger death effects
            onDeath?.Invoke();
            
            // Spawn corpse and handle death
            HandleDeath(DeathType.Intentional, deathPos);
            
            // Start respawn coroutine
            StartCoroutine(RespawnAfterDelay(DeathType.Intentional, deathPos));
        }
        // Handle multiple fruits with UI
        else if (fruitChoiceUI != null)
        {
            Debug.Log("Showing fruit choice UI");
            fruitChoiceUI.ShowFruitChoice(availableFruits, "Sow your sacrifice?", (chosenFruit) => 
            {
                Debug.Log($"Fruit selected: {chosenFruit?.name}");
                if (chosenFruit != null)
                {
                    pendingChosenFruit = chosenFruit;
                    // Remove the fruit from inventory
                    fruitInventory.RemoveOne(pendingChosenFruit);
                    
                    // Take damage (only once)
                    TakeDamage(1);
                    
                    // Trigger death effects
                    onDeath?.Invoke();
                    
                    // Spawn corpse and handle death
                    HandleDeath(DeathType.Intentional, deathPos);
                    onComplete?.Invoke();
                    
                    // Start respawn coroutine
                    StartCoroutine(RespawnAfterDelay(DeathType.Intentional, deathPos));
                }
                else
                {
                    Debug.LogError("No fruit was selected!");
                    isDying = false; // Reset dying state if no fruit was selected
                }
            });
        }
        // Fallback if no UI is available
        else
        {
            Debug.Log("No fruit choice UI available, using first fruit");
            pendingChosenFruit = availableFruits[0];
            // Remove the fruit from inventory
            
            // Only proceed with death if we're out of hearts
            Debug.Log("No hearts left, proceeding with death sequence");
            
            // Trigger death effects
            onDeath?.Invoke();
            
            // Spawn corpse and handle death
            HandleDeath(DeathType.Intentional, transform.position);
            
            // Start respawn coroutine and wait for it to complete
            yield return StartCoroutine(RespawnAfterDelay(DeathType.Intentional, transform.position));
            
            // Only complete after respawn is done
            onComplete?.Invoke();
        }
    }

    public void Die(DeathType type = DeathType.Normal)
    {
        DieInternal(type);
    }

    private void DieInternal(DeathType type)
    {
        if (isDying) return;
        isDying = true;

        Debug.Log($"Player died (Type: {type})");
        
        // Store death position for respawn
        Vector3 deathPos = transform.position;
        
        // For intentional deaths, we'll handle the death effects in IntentionalSacrifice
        if (type != DeathType.Intentional)
        {
            // Handle death effects, animations, etc.
            onDeath?.Invoke();
            
            // Handle death logic
            HandleDeath(type, deathPos);
            
            // Start respawn coroutine
            StartCoroutine(RespawnAfterDelay(type, deathPos));
        }
    }

    IEnumerator RespawnAfterDelay(DeathType type, Vector3 deathPos)
    {
        bool isFinalDeath = (currentHearts <= 0);
        
        try
        {
            // Wait for death animation/effects to play
            yield return new WaitForSeconds(1f);
            
            Debug.Log($"[RESPAWN] Starting respawn. Hearts: {currentHearts}, Type: {type}, isFinalDeath: {isFinalDeath}");
            
            // Handle final death (player is out of lives)
            if (isFinalDeath)
            {
                Debug.Log("[RESPAWN] Final death - resetting to base max hearts");
                currentHearts = baseMaxHearts;
                OnHeartsChanged?.Invoke();
                
                // For final death, try to use permanent spawn or start position
                if (currentPermanentSpawn != null)
                {
                    Debug.Log($"[RESPAWN] Respawning at permanent spawn: {currentPermanentSpawn.position}");
                    Respawn(RespawnKind.Permanent);
                }
                else
                {
                    Debug.Log("[RESPAWN] No permanent spawn, respawning at start");
                    Respawn(RespawnKind.Start);
                }
            }
            // Normal respawn (player still has lives left)
            else
            {
                Debug.Log($"[RESPAWN] Normal respawn with {currentHearts} hearts remaining");
                
                // For normal respawns, try to use the last checkpoint or local spawn
                if (currentPermanentSpawn != null)
                {
                    Debug.Log($"[RESPAWN] Respawning at checkpoint: {currentPermanentSpawn.position}");
                    Respawn(RespawnKind.Permanent);
                }
                else if (lastLocalSpawn != null)
                {
                    Debug.Log($"[RESPAWN] Respawning at local position: {lastLocalSpawn.position}");
                    Respawn(RespawnKind.Local);
                }
                else
                {
                    Debug.Log("[RESPAWN] No spawn points found, respawning at start");
                    Respawn(RespawnKind.Start);
                }
            }
        }
        finally
        {
            isDying = false;
            Debug.Log($"Respawn complete. Current hearts: {currentHearts}, isDying set to false");
            
            // Force update HUD to ensure it matches our current state
            OnHeartsChanged?.Invoke();
        }
    }

    void Respawn(RespawnKind kind = RespawnKind.Permanent)
    {
        Debug.Log($"Respawning with kind: {kind}");
        
        // Always check permanent spawn first if it's set
        if (currentPermanentSpawn != null)
        {
            Debug.Log($"Respawning at permanent spawn: {currentPermanentSpawn.position}");
            transform.position = currentPermanentSpawn.position;
        }
        else
        {
            switch (kind)
            {
                case RespawnKind.Start:
                    Debug.Log("Respawning at start position");
                    RespawnAtStart();
                    break;
                    
                case RespawnKind.Local:
                    Vector3 localPos = lastLocalSpawn != null ? lastLocalSpawn.position : transform.position;
                    Debug.Log($"Respawning at local position: {localPos}");
                    transform.position = localPos;
                    break;
                    
                case RespawnKind.Permanent:
                    // This should never happen due to the initial check, but just in case
                    Debug.LogWarning("No permanent spawn point set! Respawning at start.");
                    RespawnAtStart();
                    break;
            }
        }
        
        // Reset necessary state
        isDying = false;
        
        // Trigger respawn event
        onRespawn?.Invoke();
    }
    public void SetPermanentSpawn(Vector3 position)
    {
        if (currentPermanentSpawn == null)
        {
            var spawnObj = new GameObject("PermanentSpawn");
            currentPermanentSpawn = spawnObj.transform;
        }
        currentPermanentSpawn.position = position;
    }
    
    public Vector3 GetCurrentPermanentSpawn()
    {
        if (currentPermanentSpawn != null)
            return currentPermanentSpawn.position;
            
        // Fallback to start spawn if no permanent spawn is set
        if (startSpawn != null)
            return startSpawn.position;
            
        // If no spawn points are set, use the player's current position
        return transform.position;
    }
    
    public void RespawnAtStart()
    {
        if (startSpawn != null)
        {
            transform.position = startSpawn.position;
        }
        else
        {
            transform.position = Vector3.zero;
            // Debug.LogWarning("No start spawn point set! Respawning at (0,0,0)");
        }
        isDying = false;
        // Don't reset hearts here - they should only be reset when completely out of lives
        // HUD is updated via OnHeartsChanged event
        onRespawn?.Invoke();
    }
    
    public void SpawnCorpse(GameObject corpsePrefab, Vector3 position)
    {
        if (corpsePrefab == null)
        {
            //Debug.LogError("Cannot spawn corpse: No corpse prefab provided!");
            return;
        }
        
        // Calculate spawn position slightly below the death position
        Vector3 spawnPos = position + Vector3.down * corpseSpawnOffset;
        
        // Instantiate the corpse
        Instantiate(corpsePrefab, spawnPos, Quaternion.identity);
    }
    
    public void SpawnDefaultCorpse(Vector3 position)
    {
        if (defaultCorpsePrefab == null)
        {
            // Create a simple default corpse if none is assigned
            GameObject defaultCorpse = GameObject.CreatePrimitive(PrimitiveType.Cube);
            defaultCorpse.name = "DefaultCorpse";
            defaultCorpse.transform.position = position; // Spawn at exact position, not below
            defaultCorpse.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            
            // Add a collider if needed
            var collider = defaultCorpse.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
            
            //Debug.LogWarning("No default corpse prefab assigned, created a simple one");
            return;
        }
        
        // Spawn at exact position without offset for default corpse
        Instantiate(defaultCorpsePrefab, position, Quaternion.identity);
    }

    public void AddExtraHeartSlot(int maxExtraHearts = 2)
    {
        if (extraSlotsFromHealing < maxExtraHearts)
        {
            extraSlotsFromHealing++;
            currentHearts++;
            OnHeartsChanged?.Invoke();
            
            if (playerHUD != null)
            {
                playerHUD.UpdateHearts();
            }
        }
        OnHeartsChanged?.Invoke();
        //Debug.Log($"Added extra heart slot. Total: {MaxHearts}, Extra: {extraSlotsFromHealing}");
    }
    
    public void ClearExtraSlots()
    {
        if (extraSlotsFromHealing > 0)
        {
            extraSlotsFromHealing = 0;
            currentHearts = Mathf.Min(currentHearts, MaxHearts);
            OnHeartsChanged?.Invoke();
        }
    }
    public void HealOverTime(float amount)
    {
        if (currentHearts >= MaxHearts) 
        {
            if (isHealing)
            {
                isHealing = false;
                if (playerRenderer != null)
                    playerRenderer.color = originalPlayerColor;
            }
            return;
        }
        
        isHealing = true;
        
        // Accumulate healing
        accumulatedHealing += amount;
        
        // Only heal when we've accumulated at least 1 point of healing
        if (accumulatedHealing >= 1f)
        {
            int healAmount = Mathf.FloorToInt(accumulatedHealing);
            accumulatedHealing -= healAmount; // Keep the remainder
            ModifyHearts(healAmount);
        }
        
        // Handle player pulsing effect
        if (playerRenderer != null)
        {
            float pulse = (Mathf.Sin(Time.time * healPulseSpeed) + 1f) * 0.5f * healPulseIntensity;
            playerRenderer.color = Color.Lerp(originalPlayerColor, Color.green, pulse);
        }
    }

    private void ModifyHearts(int amount)
    {
        if (amount == 0) return;
        
        int oldHearts = currentHearts;
        currentHearts = Mathf.Clamp(currentHearts + amount, 0, MaxHearts);
        
        //Debug.Log($"Hearts modified from {oldHearts} to {currentHearts}");
        
        // Update HUD
        OnHeartsChanged?.Invoke();
    }
}