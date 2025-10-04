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

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
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
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isJumping;

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
    }

    private void FixedUpdate()
    {
        Move();
        HandleGravity();
    }

    private void HandleInput()
    {
        moveInput = moveAction.ReadValue<Vector2>().x;
        
        if (jumpAction.triggered)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        
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
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            isJumping = true;
            isGrounded = false;
        }

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
        coyoteTimeCounter = isGrounded ? coyoteTime : coyoteTimeCounter - Time.deltaTime;
        jumpBufferCounter = Mathf.Max(0, jumpBufferCounter - Time.deltaTime);
    }

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = false;

        Vector2 colliderCenter = (Vector2)transform.position + col.offset;
        Vector2 colliderSize = col.size;
        groundCheckOrigin = new Vector2(colliderCenter.x, colliderCenter.y - colliderSize.y * 0.5f + groundCheckYOffset);
        groundCheckSize = new Vector2(colliderSize.x * groundCheckWidth, 0.1f);

        RaycastHit2D hit = Physics2D.BoxCast(
            groundCheckOrigin,
            groundCheckSize,
            0f,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        if (hit.collider != null && !hit.collider.isTrigger)
        {
            isGrounded = true;
            if (!wasGrounded)
            {
                isJumping = false;
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

    // Debug visualization
    private Vector2 groundCheckOrigin;
    private Vector2 groundCheckSize;

    private void OnDrawGizmos()
    {
        if (!showDebugRays || col == null) return;

        // Draw ground check box
        Gizmos.color = isGrounded ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireCube(
            groundCheckOrigin + Vector2.down * (groundCheckDistance * 0.5f), 
            new Vector2(groundCheckSize.x, groundCheckDistance)
        );
        
        // Draw player collider
        Vector2 colliderCenter = (Vector2)transform.position + col.offset;
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawCube(colliderCenter, col.size);
        
        // Draw velocity vector
        if (rb != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
        }
    }
}