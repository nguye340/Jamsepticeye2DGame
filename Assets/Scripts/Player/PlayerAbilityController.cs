using UnityEngine;

[RequireComponent(typeof(FruitInventory))]
public class PlayerAbilityController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FruitInventory inventory;

    public AbilityType CurrentAbility { get; private set; } = AbilityType.None;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<FruitInventory>();
    }

    public void SetFruit(FruitDefinition fruit)
    {
        if (fruit == null) return;
        inventory.AddFruit(fruit);
        CurrentAbility = fruit.GrantsAbility;
    }

    public void ClearFruit()
    {
        CurrentAbility = AbilityType.None;
    }

    // Helper method to check for specific ability
    public bool HasAbility(AbilityType ability)
    {
        return CurrentAbility == ability;
    }
}