using UnityEngine;
using System;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(FruitInventory))]
public class HealingWhileAlive : MonoBehaviour
{
    [Header("Healing Settings")]
    [SerializeField] private float healTickRate = 0.1f;
    [SerializeField] private float timeBetweenHeals = 5f;
    [SerializeField] private int maxExtraHearts = 2;
    [SerializeField] private string healingFruitName = "Healing";

    private PlayerHealth playerHealth;
    private FruitInventory fruitInventory;
    private float healTickProgress = 0f;
    private bool hasHealingFruit = false;

    public event Action OnHeartsChanged;
    public event Action<float> OnHealTickChanged;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        fruitInventory = GetComponent<FruitInventory>();
    }

    private void Start()
    {
        var hud = FindObjectOfType<PlayerHUD>();
        if (hud != null)
        {
            playerHealth.OnHeartsChanged += () => OnHeartsChanged?.Invoke();
            OnHealTickChanged += hud.UpdateHealTick;
        }
    }

    private void Update()
    {
        if (playerHealth == null || fruitInventory == null) return;

        // Check if we have healing fruit
        bool hasHealing = fruitInventory.HasFruit(healingFruitName);

        // Handle healing fruit effects
        if (hasHealing && !hasHealingFruit)
        {
            hasHealingFruit = true;
            playerHealth.ModifyHearts(0); // This will trigger OnHeartsChanged
            OnHeartsChanged?.Invoke();
        }
        else if (!hasHealing && hasHealingFruit)
        {
            hasHealingFruit = false;
            healTickProgress = 0f;
            OnHeartsChanged?.Invoke();
            OnHealTickChanged?.Invoke(0f);
            return;
        }

        // Handle healing tick
        if (hasHealingFruit && playerHealth.CurrentHearts < playerHealth.MaxHearts)
        {
            healTickProgress += (1f / timeBetweenHeals) * Time.deltaTime;
            OnHealTickChanged?.Invoke(healTickProgress);

            if (healTickProgress >= 1f)
            {
                playerHealth.ModifyHearts(1);
                healTickProgress = 0f;
                OnHealTickChanged?.Invoke(0f);
            }
        }
    }

    public void OnHealingFruitSacrificed(Vector3 position)
    {
        if (hasHealingFruit)
        {
            // Find the healing fruit definition
            var healingFruit = fruitInventory.GetFruitDefinitionByName(healingFruitName);
            if (healingFruit != null && fruitInventory.RemoveOne(healingFruit))
            {
                healTickProgress = 0f;
                OnHealTickChanged?.Invoke(0f);
                // Add your checkpoint creation logic here
            }
        }
    }
}