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

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (fruitInventory == null)
            fruitInventory = FindAnyObjectByType<FruitInventory>();
            
        if (playerHealth != null)
        {
            playerHealth.OnHeartsChanged += UpdateHearts;
            UpdateHearts();
        }
        
        if (healBar != null)
        {
            healBar.minValue = 0f;
            healBar.maxValue = 1f;
            healBar.value = 0f;
            healBar.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHeartsChanged -= UpdateHearts;
    }

    public void UpdateHearts()
    {
        // Create hearts if needed
        while (hearts.Count < playerHealth.MaxHearts)
        {
            var heartObj = Instantiate(heartPrefab, heartContainer);
            hearts.Add(heartObj.GetComponent<HeartUI>());
        }

        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].SetFilled(i < playerHealth.CurrentHearts);
        }
    }

    private bool isNearHealingTree = false;
    
    private void Update()
    {
        if (healBar == null || playerHealth == null)
            return;

        // Always show heal bar when near healing tree, regardless of fruit inventory
        healBar.gameObject.SetActive(isNearHealingTree);
        
        // Update the heal bar's position to be above the player
        if (isNearHealingTree && playerHealth != null)
        {
            // Position the heal bar above the player
            Vector3 screenPos = Camera.main.WorldToScreenPoint(playerHealth.transform.position + Vector3.up * 1.5f);
            healBar.transform.position = screenPos;
        }
    }
    
    public void SetNearHealingTree(bool isNear)
    {
        isNearHealingTree = isNear;
    }

    public void UpdateHealTick(float progress)
    {
        if (healBar == null) 
            return;

        // Update the progress value
        float clampedProgress = Mathf.Clamp01(progress);
        healBar.value = clampedProgress;
        
        // Make sure the bar is active
        if (!healBar.gameObject.activeSelf)
        {
            healBar.gameObject.SetActive(true);
        }
        
        // Make sure the bar is properly layered in the UI
        healBar.transform.SetAsLastSibling();
        
        // Debug log to track progress updates (commented out to reduce log spam)
        // if (Time.frameCount % 60 == 0 && isNearHealingTree)
        // {
        //     Debug.Log($"[PlayerHUD] Heal bar updated: {progress:P0} (NearTree: {isNearHealingTree}, HasFruit: {fruitInventory?.HasFruit(healingFruitName) ?? false})");
        // }
    }
}