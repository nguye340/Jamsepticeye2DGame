using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FruitPickup : MonoBehaviour
{
    [Header("Fruit Settings")]
    public FruitDefinition Fruit;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && Fruit != null)
            spriteRenderer.sprite = Fruit.Icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Fruit == null)
        {
            Debug.LogWarning("Fruit is not assigned in the inspector!", this);
            return;
        }

        var abilityController = other.GetComponent<PlayerAbilityController>();
        if (abilityController != null)
        {
            abilityController.SetFruit(Fruit);
            
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