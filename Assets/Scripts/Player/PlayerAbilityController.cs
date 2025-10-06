using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(FruitInventory))]
public class PlayerAbilityController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FruitInventory inventory;

    [Header("Fire Shot Settings")]
    public Transform firePoint;
    public GameObject fireProjectilePrefab;
    public float fireCooldown = 0.5f;
    public AudioClip fireSound; // Assign the fire whoosh sound in the Inspector
    [Range(0f, 1f)] public float fireSoundVolume = 0.3f; // Default to 30% volume
    private float nextFireTime = 0f;
    private bool fireShotEnabled = false;

    public AbilityType CurrentAbility { get; private set; } = AbilityType.None;

    private void Update()
    {
        if (fireShotEnabled && Input.GetKeyDown(KeyCode.F) && Time.time >= nextFireTime)
        {
            FireShot();
        }
    }

    private void FireShot()
    {
        if (fireProjectilePrefab == null || firePoint == null) 
        {
            Debug.LogWarning("Fire projectile prefab or fire point not set!");
            return;
        }
        
        nextFireTime = Time.time + fireCooldown;
        
        // Get the direction the player is facing
        float direction = Mathf.Sign(transform.localScale.x);
        
        // Create the projectile
        GameObject projectile = Instantiate(
            fireProjectilePrefab, 
            firePoint.position, 
            Quaternion.identity  // Use identity rotation, we'll handle direction separately
        );
        
        // Set the projectile's scale to match direction
        Vector3 scale = projectile.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        projectile.transform.localScale = scale;
        
        // Get the FireProjectile component and set its direction
        FireProjectile fireProjectile = projectile.GetComponent<FireProjectile>();
        if (fireProjectile != null)
        {
            fireProjectile.SetDirection(direction);
        }
        else
        {
            Debug.LogError("FireProjectile component not found on the projectile prefab!");
        }
        
        // Play fire sound
        if (fireSound != null)
        {
            AudioSource.PlayClipAtPoint(fireSound, transform.position, fireSoundVolume);
        }
        
        Debug.Log($"Fired projectile in direction: {direction} from position: {firePoint.position}");
    }

    private void Start()
    {
        // Try to get the FruitInventory instance
        StartCoroutine(InitializeFruitInventory());
    }
    
    private IEnumerator InitializeFruitInventory()
    {
        // Wait for the FruitInventory to be initialized
        while (FruitInventory.Instance == null)
        {
            yield return null;
        }
        
        inventory = FruitInventory.Instance;
        
        // Subscribe to the OnFruitRemoved event
        if (inventory != null)
        {
            inventory.OnFruitRemoved += OnFruitRemoved;
            Debug.Log("PlayerAbilityController: Successfully subscribed to FruitInventory events");
        }
        else
        {
            Debug.LogError("PlayerAbilityController: Failed to get FruitInventory instance after initialization!");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (inventory != null)
        {
            inventory.OnFruitRemoved -= OnFruitRemoved;
        }
    }
    
    private void OnFruitRemoved(FruitDefinition fruit)
    {
        // If the removed fruit was granting the fire ability, disable fire
        if (fruit != null && fruit.GrantsAbility == AbilityType.FireShot)
        {
            // Check if we still have any fire fruits left
            bool hasFireFruit = false;
            var allFruits = inventory.GetAllTypes();
            foreach (var f in allFruits)
            {
                if (f.GrantsAbility == AbilityType.FireShot)
                {
                    hasFireFruit = true;
                    break;
                }
            }
            
            if (!hasFireFruit)
            {
                fireShotEnabled = false;
                if (CurrentAbility == AbilityType.FireShot)
                {
                    CurrentAbility = AbilityType.None;
                    Debug.Log("Fire ability disabled - no more fire fruits");
                }
            }
        }
    }

    public void SetFruit(FruitDefinition fruit)
    {
        if (fruit == null) 
        {
            ClearFruit();
            return;
        }

        // Make sure inventory is initialized
        if (inventory == null)
        {
            inventory = FruitInventory.Instance;
            if (inventory == null)
            {
                Debug.LogError("Cannot set fruit - FruitInventory not initialized!");
                return;
            }
        }

        // Enable the specific ability
        CurrentAbility = fruit.GrantsAbility;
        fireShotEnabled = (fruit.GrantsAbility == AbilityType.FireShot);
        
        Debug.Log($"Setting fruit: {fruit.name}, GrantsAbility: {fruit.GrantsAbility}, fireShotEnabled: {fireShotEnabled}");

        // Add to inventory
        inventory.AddFruit(fruit);
    }

    public void ClearFruit()
    {
        CurrentAbility = AbilityType.None;
        fireShotEnabled = false;
    }

    // Helper method to check for specific ability
    public bool HasAbility(AbilityType ability)
    {
        return CurrentAbility == ability;
    }
}