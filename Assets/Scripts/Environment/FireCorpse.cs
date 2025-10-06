using UnityEngine;

public class FireCorpse : MonoBehaviour
{
    [Header("Fire Effects")]
    [SerializeField] private float burnDuration = 5f;
    [SerializeField] private float burnDamage = 1f;
    [SerializeField] private float damageInterval = 0.5f;
    [SerializeField] private GameObject fireParticles;

    private float currentBurnTime = 0f;
    private float damageTimer = 0f;

    private void Start()
    {
        // Instantiate fire particle effect if assigned
        if (fireParticles != null)
        {
            Instantiate(fireParticles, transform.position, Quaternion.identity, transform);
        }
        
        // Destroy after burn duration
        Destroy(gameObject, burnDuration);
    }

    private void Update()
    {
        currentBurnTime += Time.deltaTime;
        damageTimer += Time.deltaTime;

        // Fade out effect (optional)
        if (currentBurnTime > burnDuration * 0.7f)
        {
            // Add fade out effect here if desired
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Check if enough time has passed to deal damage
        if (damageTimer >= damageInterval)
        {
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(burnDamage);
                damageTimer = 0f;
            }
        }
    }
}
