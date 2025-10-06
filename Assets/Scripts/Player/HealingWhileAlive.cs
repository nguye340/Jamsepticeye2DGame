using UnityEngine;
using System;
using System.Linq;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(FruitInventory))]
public class HealingWhileAlive : MonoBehaviour
{
    [Header("Healing Settings")]
    [SerializeField] private float timeBetweenHeals = 5f;
    [SerializeField] private int maxExtraHearts = 2;
    [SerializeField] private string healingFruitName = "Healing";
    
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private PlayerHealth playerHealth;
    private FruitInventory fruitInventory;
    private float healTickProgress = 0f;
    private bool hasHealingFruit = false;
    private FruitDefinition healingFruitDefinition;

    public event Action OnHeartsChanged;
    public event Action<float> OnHealTickChanged;

    private PlayerHUD hud;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        fruitInventory = GetComponent<FruitInventory>();
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
        if (fruitInventory == null)
            fruitInventory = GetComponent<FruitInventory>();
        
        // if (debugLogs) Debug.Log($"[Healing] Starting HealingWhileAlive. PlayerHealth: {playerHealth != null}, FruitInventory: {fruitInventory != null}");
        
        // Initialize HUD reference
        hud = FindAnyObjectByType<PlayerHUD>();
        //if (debugLogs) Debug.Log($"[Healing] HUD reference: {hud != null}");
        
        // Initialize events
        if (playerHealth != null)
        {
            playerHealth.OnHeartsChanged += () => 
            {
                // if (debugLogs) Debug.Log($"[Healing] Hearts changed to {playerHealth.CurrentHearts}/{playerHealth.MaxHearts}");
                OnHeartsChanged?.Invoke();
            };
        }
        
        if (hud != null)
        {
            // Clear any existing subscriptions to prevent duplicates
            OnHealTickChanged = null;
            // Add the new subscription
            OnHealTickChanged += (progress) => 
            {
                if (debugLogs && Time.frameCount % 60 == 0) // Log once per second
                    Debug.Log($"[Healing] Updating heal tick: {progress:P0}");
                hud.UpdateHealTick(progress);
            };
            
            // Initial check for healing fruit
            CheckForHealingFruit();
            
            // Force initial update of the heal bar
            OnHealTickChanged?.Invoke(healTickProgress);
            //if (debugLogs) Debug.Log($"[Healing] Initial heal tick update: {healTickProgress:P0}");
        }
    }

    private void Update()
    {
        if (playerHealth == null || fruitInventory == null || hud == null) 
        {
            if (playerHealth == null) 
            playerHealth = GetComponent<PlayerHealth>();
            if (fruitInventory == null) 
                fruitInventory = GetComponent<FruitInventory>();
            if (hud == null) 
                hud = FindAnyObjectByType<PlayerHUD>();
            
            if (playerHealth == null || fruitInventory == null || hud == null)
            {
                // Debug logging removed
                return;
            }
        }

        bool hasHealingNow = HasHealingFruitInInventory();
        if (hasHealingNow != hasHealingFruit)
        {
            hasHealingFruit = hasHealingNow;
            OnHeartsChanged?.Invoke();
            
            if (!hasHealingFruit)
            {
                healTickProgress = 0f;
                OnHealTickChanged?.Invoke(0f);
                return;
            }
            else
            {
                // Reset progress when we first get healing fruit
                healTickProgress = 0f;
                OnHealTickChanged?.Invoke(0f);
            }
        }

        // Handle healing tick when we have healing fruit
        if (hasHealingFruit)
        {
            bool shouldHeal = false;
            float healRate = 1f / timeBetweenHeals; // Heals per second
            
            // Check if we need to heal or add extra hearts
            if (playerHealth.CurrentHearts < playerHealth.MaxHearts)
            {
                // Heal normal hearts
                healTickProgress += Time.deltaTime * healRate;
                shouldHeal = true;
                
                if (debugLogs && healTickProgress > 0 && healTickProgress < 1f)
                {
                    //Debug.Log($"[Healing] Healing progress: {healTickProgress:P0}");
                }
            }
            else if (playerHealth.ExtraSlotsFromHealing < maxExtraHearts)
            {
                // Add extra heart slots (slightly slower)
                healTickProgress += (Time.deltaTime * healRate) / 2f;
                shouldHeal = true;
                
                if (debugLogs && healTickProgress > 0 && healTickProgress < 1f)
                {
                    //Debug.Log($"[Healing] Extra heart progress: {healTickProgress:P0}");
                }
            }
            
            if (shouldHeal)
            {
                float progress = Mathf.Clamp01(healTickProgress);
                OnHealTickChanged?.Invoke(progress);

                if (healTickProgress >= 1f)
                {
                    if (playerHealth.CurrentHearts < playerHealth.MaxHearts)
                    {
                        playerHealth.ModifyHearts(1);
                        //if (debugLogs) Debug.Log($"[Healing] Healed to {playerHealth.CurrentHearts}/{playerHealth.MaxHearts} hearts");
                    }
                    else if (playerHealth.ExtraSlotsFromHealing < maxExtraHearts)
                    {
                        playerHealth.AddExtraHeartSlot(maxExtraHearts);
                            //if (debugLogs) Debug.Log($"[Healing] Added extra heart slot. Total: {playerHealth.ExtraSlotsFromHealing}/{maxExtraHearts}");
                    }
                    
                    healTickProgress = 0f;
                    OnHealTickChanged?.Invoke(0f);
                }
            }
        }
    }

    private bool HasHealingFruitInInventory()
    {
        if (fruitInventory == null) 
        {
            //if (debugLogs) Debug.LogError("[Healing] FruitInventory is null!");
            return false;
        }

        // First check if we already have a valid healing fruit reference
        if (healingFruitDefinition != null)
        {
            int count = fruitInventory.GetCount(healingFruitDefinition);
            if (count > 0)
            {
                //if (debugLogs) Debug.Log($"[Healing] Using cached healing fruit: {healingFruitDefinition.name} (x{count})");
                return true;
            }
            else
            {
                healingFruitDefinition = null; // Clear the cache if the fruit is no longer in inventory
            }
        }

        // If not, try to find it in the inventory
        var allFruits = fruitInventory.GetAllTypes();
            
        foreach (var fruit in allFruits)
        {
            if (fruit != null && !string.IsNullOrEmpty(fruit.name) && 
                fruit.name.IndexOf(healingFruitName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                healingFruitDefinition = fruit;
                int count = fruitInventory.GetCount(fruit);
                return true;
            }
        }
        
        //if (debugLogs) Debug.Log("[Healing] No healing fruit found in inventory");
        healingFruitDefinition = null;
        return false;
    }

    private void CheckForHealingFruit()
    {
        hasHealingFruit = HasHealingFruitInInventory();
        //if (hasHealingFruit && debugLogs)
            //Debug.Log($"[Healing] Found initial healing fruit: {healingFruitDefinition?.name}");
    }

    public void OnHealingFruitSacrificed(Vector3 position)
    {
        if (hasHealingFruit && healingFruitDefinition != null)
        {
            if (fruitInventory.RemoveOne(healingFruitDefinition))
            {
                healTickProgress = 0f;
                OnHealTickChanged?.Invoke(0f);
                hasHealingFruit = HasHealingFruitInInventory();
            }
        }
    }
}