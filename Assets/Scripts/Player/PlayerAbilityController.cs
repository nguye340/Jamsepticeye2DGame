using UnityEngine;

public class PlayerAbilityController : MonoBehaviour
{
    [Header("Current State")]
    [SerializeField] private FruitDefinition currentFruit;
    
    public FruitDefinition CurrentFruit
    {
        get => currentFruit;
        private set => currentFruit = value;
    }

    public AbilityType CurrentAbility => 
        currentFruit != null ? currentFruit.GrantsAbility : AbilityType.None;

    public void SetFruit(FruitDefinition fruit)
    {
        CurrentFruit = fruit;
    }

    public void ClearFruit()
    {
        CurrentFruit = null;
    }
}