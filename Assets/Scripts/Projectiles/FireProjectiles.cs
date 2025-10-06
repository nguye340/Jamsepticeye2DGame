using UnityEngine;
using System.Collections;

public class FireProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public float lifetime = 2f;
    public int damage = 1;
    public GameObject impactEffect;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right; // Default to right
    private bool isInitialized = false;
    private float spawnTime;  // Add this line

    public void SetDirection(float xDirection)
    {
        // Set the direction and flip the sprite if needed
        direction = new Vector2(xDirection, 0);
        
        // Flip the sprite based on direction
        if (xDirection < 0)
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    private void Start()
    {
        spawnTime = Time.time;  // Initialize spawnTime
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody2D found on the projectile!");
            return;
        }
        
        // Make sure we have a collider
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Debug.Log($"Projectile has collider: {collider.name}, isTrigger: {collider.isTrigger}");
        }
        else
        {
            Debug.LogError("No Collider2D found on the projectile!");
        }
        
        // Set initial velocity
        rb.linearVelocity = direction.normalized * speed;
        Debug.Log($"Projectile created at {transform.position} with velocity: {rb.linearVelocity}");
        
        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
        isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!isInitialized || rb == null) return;
        
        // Keep the velocity consistent
        rb.linearVelocity = direction.normalized * speed;
        
        // Draw a longer debug ray in the direction of movement
        float rayLength = 2f; // Make the ray 2 units long
        Debug.DrawRay(transform.position, direction * rayLength, Color.red, 0.1f);
        
        // Add a cross at the projectile's position for better visibility
        float crossSize = 0.3f;
        Debug.DrawRay(transform.position - new Vector3(crossSize, 0, 0), Vector2.right * crossSize * 2, Color.yellow, 0.1f);
        Debug.DrawRay(transform.position - new Vector3(0, crossSize, 0), Vector2.up * crossSize * 2, Color.yellow, 0.1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Projectile triggered with: {collision.name} (tag: {collision.tag}, isTrigger: {collision.isTrigger}) at {Time.time - spawnTime:F2}s");
        
        // Check if the collided object has the IDamageable interface
        var damageable = collision.GetComponent<IDamageable>();
        if (damageable != null)
        {
            Debug.Log($"Damageable object hit: {collision.name}");
            damageable.TakeDamage(damage);
            
            // Only destroy the projectile if it hits a damageable object
            Debug.Log($"Destroying projectile after hitting: {collision.name}");
            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }
            StartCoroutine(DestroyAfterDelay(0.05f));
            return;
        }
        
        // For non-damageable objects, just log the collision but don't destroy
        Debug.Log($"Projectile passed through: {collision.name} (tag: {collision.tag})");
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this != null)
        {
            Debug.Log($"Destroying projectile at {Time.time - spawnTime:F2}s");
            Destroy(gameObject);
        }
    }
}