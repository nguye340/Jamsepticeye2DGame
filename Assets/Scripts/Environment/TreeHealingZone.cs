using UnityEngine;

public class TreeHealingZone : MonoBehaviour
{
    [Header("Healing Settings")]
    [SerializeField] private float healRadius = 3f;
    [SerializeField] private float healAmountPerSecond = 10f; // Significantly increased healing rate
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private LayerMask playerLayer = ~0; // Default to all layers
    
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer treeRenderer;
    [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private float maxPulseIntensity = 0.7f;
    
    private Color originalColor;
    private bool isPlayerInRange = false;
    private PlayerHealth playerHealth;
    private PlayerHUD playerHUD;
    private float groundYPosition;
    private float pulseTimer = 0f;
    
    private void Start()
    {
        // Find the ground position
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 10f, LayerMask.GetMask("Ground"));
        if (hit.collider != null)
        {
            groundYPosition = hit.point.y;
            // Position the tree slightly above the ground
            Vector3 newPos = transform.position;
            newPos.y = groundYPosition + GetComponent<Collider2D>().bounds.extents.y;
            transform.position = newPos;
        }
        
        // Find the tree's SpriteRenderer if not assigned
        if (treeRenderer == null)
        {
            treeRenderer = GetComponentInChildren<SpriteRenderer>(true); // Include inactive renderers
            if (treeRenderer == null)
            {
                enabled = false;
                return;
            }
        }
        
        // Store the original color
        if (treeRenderer == null)
        {
            enabled = false;
            return;
        }
        originalColor = treeRenderer.color;
            
        // Find player health and HUD references
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        playerHUD = FindAnyObjectByType<PlayerHUD>();
    }
    
    private void Update()
    {
        // Try to find missing references
        if (playerHealth == null) 
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth == null) 
            {
                Debug.LogWarning("PlayerHealth not found! Make sure the player has a PlayerHealth component.");
                return;
            }
        }
        
        if (playerHUD == null)
        {
            playerHUD = FindAnyObjectByType<PlayerHUD>();
            if (playerHUD == null)
            {
                Debug.LogWarning("PlayerHUD not found! Make sure there's a PlayerHUD in the scene.");
                return;
            }
        }
        
        if (treeRenderer == null)
        {
            treeRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (treeRenderer == null)
            {
                Debug.LogError("Tree Renderer not found! Please assign a SpriteRenderer to the TreeHealingZone component.");
                enabled = false;
                return;
            }
            originalColor = treeRenderer.color;
        }
        
        // Check if player is in range using overlap circle
        bool playerInRange = false;
        
        // Get player's collider directly from the PlayerHealth component
        Collider2D playerCollider = playerHealth.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            // Calculate distance to player
            float distance = Vector2.Distance(transform.position, playerCollider.transform.position);
            playerInRange = (distance <= healRadius);
        }
        
        // Update HUD and handle healing
        playerHUD.SetNearHealingTree(playerInRange);
        
        bool wasInRange = isPlayerInRange;
        isPlayerInRange = playerInRange && !playerHealth.IsDying;
        
        // Handle healing and visual effects
        if (isPlayerInRange)
        {
            // Update HUD with heal progress
            float healProgress = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            playerHUD.UpdateHealTick(healProgress);
            
            // Only heal if player is not at max health
            if (playerHealth.CurrentHearts < playerHealth.MaxHearts)
            {
                playerHealth.HealOverTime(healAmountPerSecond);
            }
            
            // Visual pulse effect for the tree
            if (treeRenderer != null)
            {
                try 
                {
                    // Update pulse timer
                    pulseTimer += Time.deltaTime * pulseSpeed;
                    
                    // Calculate pulse value using sine wave
                    float pulseValue = (Mathf.Sin(pulseTimer) + 1f) * 0.5f; // 0 to 1 range
                    
                    // Apply the pulse to the color
                    Color targetColor = Color.Lerp(originalColor, healColor, pulseValue * maxPulseIntensity);
                    
                    // Ensure alpha is preserved
                    targetColor.a = originalColor.a;
                    
                    // Apply the color
                    treeRenderer.color = targetColor;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Error in tree pulse effect: " + e.Message);
                }
            }
        }
        else if (wasInRange && !isPlayerInRange)
        {
            // Reset tree color when player leaves
            if (treeRenderer != null)
            {
                treeRenderer.color = originalColor;
            }
            
            // Reset HUD
            playerHUD.UpdateHealTick(0f);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}
