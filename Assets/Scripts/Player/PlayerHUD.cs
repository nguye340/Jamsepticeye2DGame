using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private FruitInventory fruitInventory;
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Slider healBar;
    [SerializeField] private Transform heartContainer;
    
    private List<HeartUI> hearts = new List<HeartUI>();
    private const string HEALING_FRUIT_NAME = "Healing";

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
        if (fruitInventory == null)
            fruitInventory = FindObjectOfType<FruitInventory>();
            
        playerHealth.OnHeartsChanged += UpdateHearts;
        UpdateHearts();
        
        if (healBar != null)
            healBar.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHeartsChanged -= UpdateHearts;
    }

    private void UpdateHearts()
    {
        // Create hearts if needed
        while (hearts.Count < playerHealth.MaxHearts)
        {
            var heartObj = Instantiate(heartPrefab, heartContainer);
            hearts.Add(heartObj.GetComponent<HeartUI>());
        }

        // Update heart states
        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].SetFilled(i < playerHealth.CurrentHearts);
        }

        // Show/hide heal bar based on healing fruit
        bool hasHealing = fruitInventory != null && fruitInventory.HasFruit(HEALING_FRUIT_NAME);
        if (healBar != null)
            healBar.gameObject.SetActive(hasHealing && playerHealth.CurrentHearts < playerHealth.MaxHearts);
    }

    public void UpdateHealTick(float progress)
    {
        if (healBar != null)
            healBar.value = progress;
    }
}