using UnityEngine;

/// <summary>
/// Purpose: Controls player movement, jumping, animation, and slope-aware shadow walking.
/// Input: Keyboard movement input, jump input, Rigidbody physics, and Ground layer collision.
/// Output: Moves the player across flat and sloped shadow surfaces without snapping to new shadows.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5f;
    public float jumpForce = 6f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.14f;
    public LayerMask groundLayer;

    [Header("Slope Walking")]
    public float maxWalkableSlopeAngle = 52f;
    public float groundProbeStartOffset = 0.18f;
    public float groundProbeDistance = 0.35f;
    public float groundStickVelocity = 0.25f;
    public float maxGroundedFootDistance = 0.1f;
    [Header("Wall Slide Guard")]
    public float wallCheckDistance = 0.22f;
    public float wallCheckHeightOffset = 0.45f;
    public float maxWallClimbNormalY = 0.35f;
    public bool preserveHorizontalSpeedOnSlopes = true;
    public float maxSlopeSpeedMultiplier = 1.25f;

    [Header("Jump Feel")]
    public float jumpBufferTime = 0.12f;
    public float coyoteTime = 0.1f;
    public float postJumpGroundLockTime = 0.12f;

    [Header("Visual")]
    public Transform visual;

    [Header("Animation")]
    public Animator animator;

    private Rigidbody rb;
    private bool isGrounded;
    private float moveInput;
    private float jumpBufferCounter;
    private float coyoteCounter;
    private float postJumpGroundLockCounter;
    private Vector3 groundNormal = Vector3.up;
    private Collider currentGroundCollider;

    public bool IsGrounded => isGrounded;
    public Vector3 GroundNormal => groundNormal;
    public Collider CurrentGroundCollider => currentGroundCollider;

    // Purpose: Initializes required physics references when the game starts.
    // Input: Rigidbody component on the Player object.
    // Output: Stores the Rigidbody reference for movement and jumping.
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Purpose: Reads frame-based player input and updates animation state.
    // Input: Keyboard input and the latest physics state.
    // Output: Stores movement intent, stores jump input briefly, and updates Animator parameters.
    void Update()
    {
        ReadInput();
        ReadJumpInput();
        UpdateAnimation();
    }

    // Purpose: Runs physics-based movement and jump logic at a stable timestep.
    // Input: Stored movement input, stored jump input, Rigidbody state, and ground probe result.
    // Output: Applies slope-aware movement and reliable jumping to the Rigidbody.
    void FixedUpdate()
    {
        UpdateGroundState();
        UpdateCoyoteCounter();
        ApplyBufferedJump();
        Move();
        UpdateJumpBufferCounter();
    }

    // Purpose: Reads horizontal movement input from the keyboard.
    // Input: A key and D key.
    // Output: Stores -1 for left, 1 for right, and 0 for no movement.
    void ReadInput()
    {
        moveInput = GetMoveInput();
    }

    // Purpose: Records jump input for a short time so it is not lost between Update and FixedUpdate.
    // Input: W key or Space key.
    // Output: Refreshes the jump buffer timer.
    void ReadJumpInput()
    {
        bool jumpPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);

        if (jumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    // Purpose: Updates grounded state while briefly ignoring ground immediately after jumping.
    // Input: Ground probe settings and post-jump lock timer.
    // Output: Updates whether the player is standing on a valid walkable surface.
    void UpdateGroundState()
    {
        if (postJumpGroundLockCounter > 0f)
        {
            postJumpGroundLockCounter -= Time.fixedDeltaTime;
            ClearGroundState();
            return;
        }

        CheckGround();
    }

    // Purpose: Checks whether the player is standing on a walkable ground surface close enough to the feet.
    // Input: GroundCheck position, probe radius, probe distance, Ground layer, surface normal, and foot distance tolerance.
    // Output: Updates grounded state only when the player is truly close to a valid walkable surface.
    void CheckGround()
    {
        ClearGroundState();

        if (groundCheck == null)
        {
            return;
        }

        Vector3 probeOrigin = groundCheck.position + Vector3.up * groundProbeStartOffset;
        float probeDistance = groundProbeStartOffset + groundProbeDistance;

        bool hasHit = Physics.SphereCast(
            probeOrigin,
            groundCheckRadius,
            Vector3.down,
            out RaycastHit hit,
            probeDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (!hasHit)
        {
            hasHit = Physics.Raycast(
                probeOrigin,
                Vector3.down,
                out hit,
                probeDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
        }

        if (!hasHit)
        {
            return;
        }

        float footDistance = Vector3.Dot(
            groundCheck.position - hit.point,
            Vector3.up
        );

        if (footDistance > maxGroundedFootDistance)
        {
            return;
        }

        float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

        if (slopeAngle <= maxWalkableSlopeAngle)
        {
            isGrounded = true;
            groundNormal = hit.normal;
            currentGroundCollider = hit.collider;
        }
    }

    // Purpose: Clears the current ground contact information.
    // Input: None.
    // Output: Resets grounded state, ground normal, and current ground collider.
    void ClearGroundState()
    {
        isGrounded = false;
        groundNormal = Vector3.up;
        currentGroundCollider = null;
    }

    // Purpose: Keeps the player jumpable for a short time after leaving a surface.
    // Input: Current grounded state.
    // Output: Updates the coyote-time counter.
    void UpdateCoyoteCounter()
    {
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter = Mathf.Max(0f, coyoteCounter - Time.fixedDeltaTime);
        }
    }

    // Purpose: Counts down stored jump input if it has not been used.
    // Input: Current jump buffer timer.
    // Output: Reduces the jump buffer timer over time.
    void UpdateJumpBufferCounter()
    {
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.fixedDeltaTime);
        }
    }

    // Purpose: Applies jump velocity when buffered jump input and valid ground timing overlap.
    // Input: Jump buffer timer, coyote timer, and Rigidbody velocity.
    // Output: Starts a jump without being cancelled by immediate ground re-detection.
    void ApplyBufferedJump()
    {
        if (jumpBufferCounter <= 0f || coyoteCounter <= 0f)
        {
            return;
        }

        Vector3 velocity = rb.velocity;
        velocity.y = jumpForce;
        rb.velocity = velocity;

        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        postJumpGroundLockCounter = postJumpGroundLockTime;
        ClearGroundState();
    }

    // Purpose: Applies walking and running movement while respecting slopes.
    // Input: Stored move input, Shift key state, grounded state, and ground normal.
    // Output: Updates Rigidbody velocity so the player walks along walkable shadow surfaces.
    void Move()
    {
        if (rb == null)
        {
            return;
        }

        bool isMoving = moveInput != 0f;
        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isRunning = isMoving && isHoldingShift;

        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 velocity = rb.velocity;

        if (isGrounded)
        {
            if (isMoving)
            {
                Vector3 slopeVelocity = GetSlopeMoveVelocity(moveInput, currentSpeed);
                velocity.x = slopeVelocity.x;
                velocity.y = slopeVelocity.y;

                if (velocity.y <= 0f)
                {
                    velocity.y -= groundStickVelocity;
                }
            }
            else
            {
                velocity.x = 0f;

                if (velocity.y <= 0f)
                {
                    velocity.y = -groundStickVelocity;
                }
            }
        }
        else
        {
            if (IsPressingIntoWallOrSteepEdge(moveInput))
            {
                velocity.x = 0f;
            }
            else
            {
                velocity.x = moveInput * currentSpeed;
            }
        }

        velocity.z = 0f;
        rb.velocity = velocity;

        UpdateVisualDirection(moveInput);
    }

    // Purpose: Checks whether the player is pressing horizontally into a steep edge or wall while airborne.
    // Input: Horizontal movement input, player position, Ground layer, wall check distance, and wall normal threshold.
    // Output: Returns true when air movement should not push the player into the wall or steep edge.
    bool IsPressingIntoWallOrSteepEdge(float input)
    {
        if (Mathf.Approximately(input, 0f))
        {
            return false;
        }

        Vector3 direction = input > 0f ? Vector3.right : Vector3.left;
        Vector3 origin = transform.position + Vector3.up * wallCheckHeightOffset;

        bool hasHit = Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            wallCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (!hasHit)
        {
            return false;
        }

        float normalY = hit.normal.y;

        return normalY <= maxWallClimbNormalY;
    }

    // Purpose: Calculates movement direction along the current slope.
    // Input: Horizontal input, movement speed, and the current ground normal.
    // Output: Returns a velocity that follows the walkable surface instead of forcing flat movement.
    Vector3 GetSlopeMoveVelocity(float input, float speed)
    {
        Vector3 horizontalDirection = new Vector3(input, 0f, 0f);
        Vector3 slopeDirection = Vector3.ProjectOnPlane(horizontalDirection, groundNormal);

        if (slopeDirection.sqrMagnitude < 0.0001f)
        {
            return new Vector3(input * speed, 0f, 0f);
        }

        slopeDirection.Normalize();

        if (Mathf.Sign(slopeDirection.x) != Mathf.Sign(input))
        {
            slopeDirection = -slopeDirection;
        }

        if (!preserveHorizontalSpeedOnSlopes)
        {
            return slopeDirection * speed;
        }

        float safeHorizontalAmount = Mathf.Max(Mathf.Abs(slopeDirection.x), 0.2f);
        Vector3 targetVelocity = slopeDirection * (speed / safeHorizontalAmount);
        float maxAllowedSpeed = speed * maxSlopeSpeedMultiplier;

        return Vector3.ClampMagnitude(targetVelocity, maxAllowedSpeed);
    }

    // Purpose: Flips the visual sprite based on movement direction.
    // Input: Horizontal movement input.
    // Output: Updates the visual local scale without changing physics scale.
    void UpdateVisualDirection(float input)
    {
        if (visual == null)
        {
            return;
        }

        if (input > 0f)
        {
            visual.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (input < 0f)
        {
            visual.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    // Purpose: Updates Animator parameters based on current player state.
    // Input: Movement input, Shift key state, and grounded state.
    // Output: Sets isWalking, isRunning, and isJumping in the Animator.
    void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        bool isMoving = moveInput != 0f;
        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isRunning = isMoving && isHoldingShift;
        bool isWalking = isMoving && !isRunning;
        bool isJumping = !isGrounded;

        animator.SetBool("isWalking", isWalking);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isJumping", isJumping);
    }

    // Purpose: Reads horizontal movement input from the keyboard.
    // Input: A key and D key.
    // Output: Returns -1 for left, 1 for right, and 0 for no movement.
    float GetMoveInput()
    {
        if (Input.GetKey(KeyCode.A))
        {
            return -1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            return 1f;
        }

        return 0f;
    }
}