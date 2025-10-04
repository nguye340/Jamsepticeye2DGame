using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform checkpoint;
    [SerializeField] private PlayerAbilityController abilityController;

    [Header("Events")]
    public UnityEvent onRespawn;
    public UnityEvent onDeath;

    private void Awake()
    {
        if (abilityController == null)
            abilityController = GetComponent<PlayerAbilityController>();
    }

    public void Kill(Vector3 deathPos)
    {
        onDeath?.Invoke();
        
        // Spawn corpse if player had a fruit
        if (abilityController.CurrentFruit != null)
        {
            CorpseManager.SpawnCorpse(abilityController.CurrentFruit, deathPos);
            abilityController.ClearFruit();
        }
        
        Respawn();
    }

    public void Respawn()
    {
        if (checkpoint != null)
        {
            transform.position = checkpoint.position;
        }
        else
        {
            Debug.LogWarning("No checkpoint set! Respawning at origin.");
            transform.position = Vector3.zero;
        }
        
        onRespawn?.Invoke();
    }

    // For testing in the editor
    private void OnValidate()
    {
        if (abilityController == null)
            abilityController = GetComponent<PlayerAbilityController>();
    }
}