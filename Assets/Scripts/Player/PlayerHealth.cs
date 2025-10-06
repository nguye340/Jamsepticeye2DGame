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
    [SerializeField] private Color healingZoneColor = new Color(0.2f, 1f, 0.2f, 1f); // Bright green color for healing zone
    
    private Color originalPlayerColor;
    private bool isHealing = false;
    private bool isInHealingZone = false;
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
        
        // Wait one frame to ensure all state is consistent
        yield return null;
        
        // Store the current hearts before sacrifice for logging
        int heartsBefore = currentHearts;
        
        try
        {
            // Process the sacrifice and wait for it to complete
            bool sacrificeCompleted = false;
            void OnSacrificeComplete() { 
                Debug.Log("OnSacrificeComplete called");
                sacrificeCompleted = true; 
            }
            
            Debug.Log("Starting IntentionalSacrifice coroutine...");
            var coroutine = IntentionalSacrifice(OnSacrificeComplete);
            
            if (coroutine == null)
            {
                Debug.LogError("IntentionalSacrifice returned null coroutine");
                isDying = false;
                yield break;
            }
            
            // Start the coroutine and store the reference
            var sacrificeCoroutine = StartCoroutine(coroutine);
            
            if (sacrificeCoroutine == null)
            {
                Debug.LogError("Failed to start sacrifice coroutine - StartCoroutine returned null");
                isDying = false;
                yield break;
            }
            
            Debug.Log("Waiting for sacrifice to complete...");
            
            // Wait for the sacrifice to complete with a timeout
            float timeout = 5f; // 5 second timeout
            float startTime = Time.time;
            
            while (!sacrificeCompleted && (Time.time - startTime) < timeout)
            {
                yield return null;
            }
            
            if (!sacrificeCompleted)
            {
                Debug.LogError("Sacrifice timed out!");
                // Stop the coroutine if it's still running
                StopCoroutine(sacrificeCoroutine);
            }
            
            // Log the result
            Debug.Log($"Sacrifice processed. Hearts before: {heartsBefore}, after: {currentHearts}");
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
                        Debug.Log($"Dropping fruit on death: {randomFruit.name}");
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
        Debug.Log($"TakeDamage called. Current hearts: {currentHearts}, Amount: {amount}");
        
        // Always remove exactly 1 heart per sacrifice
        int newHearts = Mathf.Max(0, currentHearts - 1);
        bool died = (newHearts <= 0);
        
        // Update hearts
        currentHearts = newHearts;
        OnHeartsChanged?.Invoke();
        
        Debug.Log($"Hearts after damage: {currentHearts}");
        
        // Only trigger death if we're out of hearts
        if (died)
        {
            Debug.Log("Player has no hearts left, triggering death");
            DieInternal(DeathType.Unintentional);
        }
        else if (currentHearts > 0)
        {
            // Only set isDying to false here if we're not actually dying
            // This allows the respawn logic to complete
            isDying = false;
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
        }
        finally
        {
            Debug.Log("IntentionalSacrifice finally block - invoking completion callback");
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

    private IEnumerator RespawnAfterDelay(DeathType type, Vector3 deathPos)
    {
        bool isFinalDeath = currentHearts <= 0; // Check if this is the final death (no hearts left)
        
        try
        {
            yield return new WaitForSeconds(1f);
            
            if (isFinalDeath)
            {
                // Reset hearts to full when respawning at start
                currentHearts = baseMaxHearts;
                OnHeartsChanged?.Invoke();
                Respawn(RespawnKind.Start);
            }
            else
            {
                // For normal deaths, respawn near the death position with a small offset
                RespawnAtPosition(deathPos + new Vector3(0, 1f, 0)); // 1 unit above death position
            }
        }
        finally
        {
            isDying = false;
            OnHeartsChanged?.Invoke();
        }
    }

    // New helper method to respawn at a specific position
    private void RespawnAtPosition(Vector3 position)
    {
        transform.position = position;
        isDying = false;
        onRespawn?.Invoke();
    }

    public void Respawn(RespawnKind kind = RespawnKind.Local)
    {
        switch (kind)
        {
            case RespawnKind.Start:
                // Only reset position, don't modify hearts here
                if (startSpawn != null)
                {
                    transform.position = startSpawn.position;
                }
                else
                {
                    transform.position = Vector3.zero;
                }
                break;
                
            case RespawnKind.Local:
                // Try to use the last local spawn point
                if (lastLocalSpawn != null)
                {
                    transform.position = lastLocalSpawn.position;
                }
                // Fall back to permanent spawn if no local spawn exists
                else if (currentPermanentSpawn != null)
                {
                    transform.position = currentPermanentSpawn.position;
                }
                // Fall back to start if no other spawn points exist
                else if (startSpawn != null)
                {
                    transform.position = startSpawn.position;
                }
                // Last resort: stay at current position
                break;
                
            case RespawnKind.Permanent:
                if (currentPermanentSpawn != null)
                {
                    transform.position = currentPermanentSpawn.position;
                }
                else if (startSpawn != null)
                {
                    transform.position = startSpawn.position;
                }
                break;
        }
        
        isDying = false;
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
    
    // This method is no longer needed as its logic is now in Respawn
    private void RespawnAtStart()
    {
        Respawn(RespawnKind.Start);
    }
    
    private void SpawnCorpse(GameObject corpsePrefab, Vector3 position)
    {
        if (corpsePrefab == null)
        {
            Debug.LogError("Cannot spawn corpse: No corpse prefab provided!");
            return;
        }
        
        // Calculate spawn position slightly below the death position
        Vector3 spawnPos = position + Vector3.down * corpseSpawnOffset;
        
        // Instantiate the corpse
        Instantiate(corpsePrefab, spawnPos, Quaternion.identity);
    }
    
    private void SpawnDefaultCorpse(Vector3 position)
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
            
            Debug.LogWarning("No default corpse prefab assigned, created a simple one");
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
        Debug.Log($"Added extra heart slot. Total: {MaxHearts}, Extra: {extraSlotsFromHealing}");
    }
    
    public void ClearExtraSlots()
    {
        if (extraSlotsFromHealing > 0)
        {
            extraSlotsFromHealing = 0;
            currentHearts = Mathf.Min(currentHearts, MaxHearts);
            OnHeartsChanged?.Invoke();
            Debug.Log("Cleared all extra heart slots");
        }
    }
    
    public void HealOverTime(float amountPerSecond)
    {
        if (isDying || amountPerSecond <= 0) 
        {
            if (isHealing && !isInHealingZone)
            {
                isHealing = false;
                if (playerRenderer != null && !isInHealingZone)
                    playerRenderer.color = originalPlayerColor;
            }
            return;
        }
        
        // Track healing over time to handle partial hearts
        accumulatedHealing += amountPerSecond * Time.deltaTime;
        
        // Only heal when we've accumulated enough for at least 1 heart
        if (Mathf.Abs(accumulatedHealing) >= 1f)
        {
            int healAmount = Mathf.FloorToInt(accumulatedHealing);
            ModifyHearts(healAmount);
            accumulatedHealing -= healAmount;
        }
        
        // Visual feedback for healing
        if (!isHealing && !isInHealingZone)
        {
            isHealing = true;
            if (playerRenderer != null && originalPlayerColor == default(Color))
            {
                originalPlayerColor = playerRenderer.color;
            }
        }
    }
    
    // Call this when entering a healing zone
    public void SetInHealingZone(bool inZone)
    {
        if (isInHealingZone == inZone) return;
        
        isInHealingZone = inZone;
        
        if (playerRenderer == null) return;
        
        // Store original color if not already stored
        if (originalPlayerColor == default(Color))
        {
            originalPlayerColor = playerRenderer.color;
        }
        
        if (inZone)
        {
            // Apply healing zone visual effect
            playerRenderer.color = healingZoneColor;
            isHealing = false; // Disable healing pulse effect while in healing zone
        }
        else
        {
            // Restore original color when leaving healing zone
            playerRenderer.color = originalPlayerColor;
        }
    }
    
    void HandleHealingPulse()
    {
        if (playerRenderer == null) return;
        
        if (isInHealingZone)
        {
            // In healing zone - solid green color
            if (playerRenderer.color != healingZoneColor)
            {
                playerRenderer.color = healingZoneColor;
            }
        }
        else if (isHealing)
        {
            // Regular healing pulse effect (pink/red)
            float pulse = (Mathf.Sin(Time.time * healPulseSpeed) + 1) * 0.5f * healPulseIntensity + (1 - healPulseIntensity);
            playerRenderer.color = new Color(1, pulse, pulse, playerRenderer.color.a);
        }
        else if (playerRenderer.color != originalPlayerColor && playerRenderer.color != healingZoneColor)
        {
            // Smoothly transition back to original color
            playerRenderer.color = Color.Lerp(playerRenderer.color, originalPlayerColor, Time.deltaTime * 5f);
        }
    }

    public void ModifyHearts(int amount)
    {
        int oldHearts = currentHearts;
        currentHearts = Mathf.Clamp(currentHearts + amount, 0, MaxHearts);
        
        Debug.Log($"Hearts modified from {oldHearts} to {currentHearts}");
        
        // Update HUD
        OnHeartsChanged?.Invoke();
    }
}