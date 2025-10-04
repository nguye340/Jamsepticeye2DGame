using UnityEngine;

[CreateAssetMenu(fileName = "New Fruit", menuName = "Fruit/New Fruit Definition")]
public class FruitDefinition : ScriptableObject
{
    [Header("Identification")]
    public string Id;
    public string DisplayName;

    [Header("Visuals")]
    public Sprite Icon;

    [Header("Gameplay")]
    public AbilityType GrantsAbility;
    public GameObject CorpsePrefab;

    [Header("Audio")]
    public AudioClip PickupSfx;
}