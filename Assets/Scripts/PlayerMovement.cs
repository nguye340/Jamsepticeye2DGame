using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxMoveSpeed = 12f;
    [SerializeField] private float groundAcceleration = 100f;
    [SerializeField] private float airAcceleration = 50f;
    [SerializeField] private float groundDeceleration = 70f;
    // [SerializeField] private float airDeceleration = 30f;
    // [SerializeField] [Range(0, 1)] private float airControl = 0.8f;
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f; // Reduced from 16f for better control
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 1.8f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundCheckWidth = 0.8f;
    [SerializeField] private float groundCheckYOffset = 0.1f;
    [SerializeField] private bool showDebugRays = true;
    // [SerializeField] private float groundSnapDistance = 0.3f;
    
    // Slope handling variables (kept for future implementation)
    // [Header("Slope Handling")]
    // [SerializeField] private float maxSlopeAngle = 45f;
    // [SerializeField] private float slopeCheckDistance = 0.5f;
    
    // Components
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    // State
    private float moveInput;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool isOnSlope;
    private Vector2 slopeNormalPerp;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float lastGroundedTime;
    private float lastJumpTime;
    private bool isJumping;
    private bool isFalling => rb.linearVelocity.y < -0.1f;
    
    // Constants
    private const float GROUNDED_RADIUS = 0.2f;
    private const float SHELL_RADIUS = 0.01f;
    private const float SLOPE_CHECK_DISTANCE = 1.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        playerInput = GetComponent<PlayerInput>();
        
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Update()
    {
        HandleInput();
        UpdateTimers();
        CheckGrounded();
        HandleJump();
        
        // Update jump state
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            isJumping = false;
        }
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        Move();
        HandleGravity();
    }

    private void HandleInput()
    {
        moveInput = moveAction.ReadValue<Vector2>().x;
        
        // Jump input handling with buffering
        if (jumpAction.triggered)
        {
            jumpBufferCounter = jumpBufferTime;
            lastJumpTime = Time.time;
        }
        
        // Jump cut - reduce jump height when jump button is released
        if (jumpAction.WasReleasedThisFrame() && rb.linearVelocity.y > 0 && isJumping)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private void Move()
    {
        float targetSpeed = moveInput * maxMoveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? 
            (isGrounded ? groundAcceleration : airAcceleration) : 
            groundDeceleration;
            
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, 0.96f) * Mathf.Sign(speedDiff);
        rb.AddForce(movement * Vector2.right);

        // Flip character if needed
        if ((moveInput > 0 && !isFacingRight) || (moveInput < 0 && isFacingRight))
        {
            Flip();
        }
    }

    private void HandleJump()
    {
        bool canJump = (isGrounded || coyoteTimeCounter > 0) && jumpBufferCounter > 0;
        
        if (canJump)
        {
            // Reset vertical velocity before applying jump force
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            // Apply jump force directly to velocity for more consistent results
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            // Reset counters and set jump state
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            isJumping = true;
            isGrounded = false; // Ensure we're not grounded after jumping
            
            Debug.Log("Jumped!"); // Debug log to confirm jump is triggered
        }

        // Variable jump height
        if (rb.linearVelocity.y > 0 && !jumpAction.IsPressed())
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime);
        }
    }

    private void HandleGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
        }
    }

    private void UpdateTimers()
    {
        // Update coyote time counter
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        // Update jump buffer counter
        jumpBufferCounter = Mathf.Max(0, jumpBufferCounter - Time.deltaTime);
        
        // Update last grounded time
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }
    }

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = false;

        // Calculate the bottom of the collider
        Vector2 colliderCenter = (Vector2)transform.position + col.offset;
        Vector2 colliderSize = col.size;
        
        // Calculate the start position for ground check (slightly above the bottom of the collider)
        Vector2 rayStart = new Vector2(colliderCenter.x, colliderCenter.y - colliderSize.y * 0.5f + groundCheckYOffset);
        
        // Cast a box that's slightly wider than the collider but not as tall
        Vector2 boxSize = new Vector2(colliderSize.x * groundCheckWidth, 0.1f);
        float rayLength = groundCheckDistance;
        
        // Perform the box cast
        RaycastHit2D hit = Physics2D.BoxCast(
            rayStart,
            boxSize,
            0f,
            Vector2.down,
            rayLength,
            groundLayer
        );
        
        // Debug visualization
        if (showDebugRays)
        {
            // Draw the box cast
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(rayStart + Vector2.down * (rayLength * 0.5f), 
                              new Vector2(boxSize.x, rayLength));
            
            // Draw the ground check area
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(rayStart + Vector2.down * (rayLength * 0.5f), 
                              new Vector2(boxSize.x, rayLength));
        }

        // If we hit something, we're grounded
        if (hit.collider != null && !hit.collider.isTrigger)
        {
            isGrounded = true;
            
            // Optional: Snap to ground if very close
            float bottomOfCollider = transform.position.y - (colliderSize.y * 0.5f) + col.offset.y;
            float distanceToGround = hit.point.y - bottomOfCollider;
            
            if (distanceToGround > 0 && distanceToGround <= groundCheckDistance)
            {
                transform.position += Vector3.up * distanceToGround;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
        }

        // Ground state changed
        if (isGrounded != wasGrounded)
        {
            lastGroundedTime = Time.time;
            if (isGrounded) 
            {
                isJumping = false;
                Debug.Log("Landed!");
            }
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (col == null) return;
        
        // Cache values
        Vector2 colliderCenter = (Vector2)transform.position + col.offset;
        Vector2 colliderSize = col.size;
        float halfWidth = colliderSize.x * 0.5f * groundCheckWidth;
        
        // Draw ground check box
        Vector2 boxSize = new Vector2(colliderSize.x * groundCheckWidth, 0.1f);
        Vector2 boxOrigin = new Vector2(
            colliderCenter.x, 
            colliderCenter.y - colliderSize.y * 0.5f + groundCheckYOffset
        );
        
        float rayLength = groundCheckDistance + colliderSize.y * 0.5f;
        
        // Draw ground check box
        Gizmos.color = isGrounded ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
        Gizmos.DrawCube(
            boxOrigin + Vector2.down * (rayLength * 0.5f - colliderSize.y * 0.25f), 
            new Vector2(boxSize.x, rayLength)
        );
        
        // Draw collider bounds
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawCube(colliderCenter, colliderSize);
        
        // Draw slope normal if on slope
        if (isOnSlope)
        {
            Gizmos.color = Color.magenta;
            Vector2 slopeStart = new Vector2(
                transform.position.x,
                transform.position.y - colliderSize.y * 0.5f + groundCheckYOffset
            );
            Gizmos.DrawRay(slopeStart, slopeNormalPerp * 2f);
        }
        
        // Draw velocity vector
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, rb != null ? rb.linearVelocity.normalized * 2f : Vector3.zero);
        
        // Draw state info
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        Vector3 textPos = transform.position + Vector3.up * (colliderSize.y * 0.5f + 0.5f);
        
        #if UNITY_EDITOR
        string stateInfo = $"State: {(isGrounded ? "Grounded" : "Airborne")}\n" +
                         $"Speed: {rb?.linearVelocity.magnitude:F1}\n" +
                         $"Velocity: {rb?.linearVelocity}";
        UnityEditor.Handles.Label(textPos, stateInfo, style);
        #endif
    }
}