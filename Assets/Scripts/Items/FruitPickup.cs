using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[ExecuteInEditMode]
public class FruitPickup : MonoBehaviour
{
    [Header("Fruit Settings")]
    [SerializeField] private FruitDefinition fruit;
    public FruitDefinition Fruit
    {
        get => fruit;
        set
        {
            if (fruit != value)
            {
                fruit = value;
                UpdateFruitVisuals();
            }
        }
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnValidate()
    {
        // Only update in editor, not during play mode
        if (!Application.isPlaying)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            // Use EditorApplication.delayCall to defer the sprite update
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => 
            {
                if (this != null) // Check if object still exists
                    UpdateFruitVisuals();
            };
            #endif
        }
    }

    private void UpdateFruitVisuals()
    {
        if (spriteRenderer != null && Fruit != null)
        {
            spriteRenderer.sprite = Fruit.Icon;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Fruit == null)
        {
            Debug.LogWarning("Fruit is not assigned in the inspector!", this);
            return;
        }

        var abilityController = other.GetComponent<PlayerAbilityController>();
        var fruitInventory = other.GetComponent<FruitInventory>();
        
        if (abilityController != null)
        {
            // This will also add to the inventory since PlayerAbilityController has a reference to the same inventory
            abilityController.SetFruit(Fruit);
            
            // Get the updated count from the inventory
            int totalFruits = abilityController.GetComponent<FruitInventory>().TotalCount();
            Debug.Log($"Added {Fruit.name} to inventory. Total fruits: {totalFruits}");
            
            if (Fruit.PickupSfx != null)
                AudioSource.PlayClipAtPoint(Fruit.PickupSfx, transform.position);
            
            Destroy(gameObject);
        }
    }

    private void Reset()
    {
        // Auto-setup for new instances
        var collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;
    }
}