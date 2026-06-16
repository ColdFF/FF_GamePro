using UnityEngine;

/// <summary>
/// Purpose: Controls the player character, including movement, jumping, animation, sound, and shadow-platform walking.
/// Input: A/D, Shift, W/Space, Rigidbody physics, and ground or shadow colliders.
/// Output: Moves the player reliably on normal platforms and generated shadow platforms.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    // How fast the player walks left or right.
    public float walkSpeed = 2.5f;
    // How fast the player moves when Shift is held.
    public float runSpeed = 5f;
    // How strongly the player jumps upward.
    public float jumpForce = 6f;

    [Header("Ground Check")]
    // Feet marker used as the place to check for floor.
    public Transform groundCheck;
    // Width of the feet check area.
    public float groundCheckRadius = 0.14f;
    // Which layers count as floor or shadow platforms.
    public LayerMask groundLayer;

    [Header("Slope Walking")]
    // Biggest slope the player can still walk on; steeper surfaces are treated like walls.
    public float maxWalkableSlopeAngle = 52f;
    // Starts the floor check a little above the feet, so it does not miss the ground.
    public float groundProbeStartOffset = 0.18f;
    // How far below the feet the script looks for ground.
    public float groundProbeDistance = 0.35f;
    // Tiny downward push that helps the player stay on a slope instead of floating.
    public float groundStickVelocity = 0.25f;
    // If the ground is farther than this from the feet, the player counts as in the air.
    public float maxGroundedFootDistance = 0.1f;
    [Header("Wall Slide Guard")]
    // How far left or right the script checks for a wall.
    public float wallCheckDistance = 0.22f;
    // How high on the player body the wall check is made.
    public float wallCheckHeightOffset = 0.45f;
    // Decides when a side surface is too vertical and should block movement.
    public float maxWallClimbNormalY = 0.35f;
    // Keeps the player from feeling slower just because they are walking on a slope.
    public bool preserveHorizontalSpeedOnSlopes = true;
    // Safety limit so slope movement cannot make the player too fast.
    public float maxSlopeSpeedMultiplier = 1.25f;

    [Header("Jump Feel")]
    // How long the game remembers a jump press made slightly early.
    public float jumpBufferTime = 0.12f;
    // How long the player can still jump after stepping off an edge.
    public float coyoteTime = 0.1f;
    // Short delay after jumping before the player can count as grounded again.
    public float postJumpGroundLockTime = 0.12f;

    [Header("External Launch")]
    // How long rope launch speed is kept before A/D can change air movement again.
    public float externalLaunchAirControlDelay = 0.22f;

    [Header("Jump Audio")]
    // Object that plays the player's sounds.
    public AudioSource playerAudioSource;
    // Sound used when a real jump starts.
    public AudioClip jumpSound;
    // Loudness of the jump sound.
    [Range(0f, 1f)] public float jumpSoundVolume = 0.7f;

    [Header("Footstep Audio")]
    // Footstep sound used on normal platforms.
    public AudioClip footstepSound;
    // Loudness of normal platform footsteps.
    [Range(0f, 1f)] public float footstepSoundVolume = 0.45f;
    // Delay between walking footstep sounds.
    public float walkFootstepInterval = 0.38f;
    // Delay between running footstep sounds.
    public float runFootstepInterval = 0.26f;

    [Header("Shadow Footstep Audio")]
    // Softer footstep sound used on shadow platforms.
    public AudioClip shadowFootstepSound;
    // Loudness of shadow walking footsteps.
    [Range(0f, 1f)] public float shadowFootstepSoundVolume = 0.28f;
    // Optional faster footstep sound used when running on shadow platforms.
    public AudioClip shadowRunFootstepSound;
    // Loudness of shadow running footsteps.
    [Range(0f, 1f)] public float shadowRunFootstepSoundVolume = 0.28f;

    [Header("Visual")]
    // Child visual object that flips left or right when the player changes direction.
    public Transform visual;

    [Header("Animation")]
    // Animator that plays the player's walk, run, and jump animations.
    public Animator animator;

    private Rigidbody rb;
    private bool isGrounded;
    private float moveInput;
    private float jumpBufferCounter;
    private float coyoteCounter;
    private float postJumpGroundLockCounter;
    private float externalLaunchAirControlCounter;
    private float footstepCounter;
    private bool shadowCarriedThisFixedStep;
    private Vector3 shadowCarryDeltaThisFixedStep;
    private Vector3 groundNormal = Vector3.up;
    private Collider currentGroundCollider;

    // Lets other scripts check if the player is currently standing on something.
    public bool IsGrounded => isGrounded;
    // Lets other scripts know which way the current ground is tilted.
    public Vector3 GroundNormal => groundNormal;
    // Lets other scripts know which collider the player is standing on.
    public Collider CurrentGroundCollider => currentGroundCollider;

    // Purpose: Gets the player's physics body when the level starts.
    // Input: The Rigidbody on this Player object.
    // Output: Saves it so the script can move and jump the player.
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Purpose: Resets the player before control is returned.
    /// Input: Current player physics and ground state.
    /// Output: Stops small shakes or wrong jump animation when input unlocks.
    /// </summary>
    public void PrepareForInputUnlock()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        moveInput = 0f;
        jumpBufferCounter = 0f;
        postJumpGroundLockCounter = 0f;

        CheckGround();

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;

            if (rb != null)
            {
                Vector3 velocity = rb.velocity;
                velocity.x = 0f;
                velocity.z = 0f;

                if (velocity.y < 0f)
                {
                    velocity.y = 0f;
                }

                rb.velocity = velocity;
            }
        }

        UpdateAnimation();
    }

    /// <summary>
    /// Purpose: Records that a moving shadow platform has carried the player.
    /// Input: How far the shadow platform moved.
    /// Output: Stops the player from being pulled downward during that carry.
    /// </summary>
    public void NotifyShadowCarry(Vector3 delta)
    {
        shadowCarriedThisFixedStep = true;
        shadowCarryDeltaThisFixedStep += delta;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb != null && rb.velocity.y < 0f)
        {
            Vector3 velocity = rb.velocity;
            velocity.y = 0f;
            rb.velocity = velocity;
        }
    }

    /// <summary>
    /// Purpose: Moves the player with a moving shadow platform right away.
    /// Input: How far the shadow platform moved.
    /// Output: Player follows the shadow platform instead of being left behind.
    /// </summary>
    public void ApplyShadowCarry(Vector3 delta)
    {
        NotifyShadowCarry(delta);

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            return;
        }

        Vector3 targetPosition = rb.position + delta;
        rb.position = targetPosition;
        transform.position = targetPosition;
        Physics.SyncTransforms();
    }

    /// <summary>
    /// Purpose: Lets another system throw or push the player.
    /// Input: Launch speed, usually from rope release.
    /// Output: Player keeps that thrown speed for a short time.
    /// </summary>
    public void ApplyExternalLaunch(Vector3 launchVelocity, float airControlDelay = -1f)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb != null)
        {
            rb.velocity = launchVelocity;
        }

        externalLaunchAirControlCounter = Mathf.Max(
            0f,
            airControlDelay >= 0f ? airControlDelay : externalLaunchAirControlDelay
        );

        postJumpGroundLockCounter = Mathf.Max(postJumpGroundLockCounter, postJumpGroundLockTime);
        ClearGroundState();
    }

    // Purpose: Handles things that need to be checked every frame.
    // Input: Keyboard keys and current player state.
    // Output: Reads movement, remembers jump presses, updates animation, and plays footsteps.
    void Update()
    {
        ReadInput();
        ReadJumpInput();
        UpdateAnimation();
        UpdateFootstepAudio();
    }

    /// <summary>
    /// Purpose: Handles the player's physics movement.
    /// Input: Stored input, physics body, and ground check result.
    /// Output: Updates ground state, jump, movement, and shadow-platform carry.
    /// </summary>
    void FixedUpdate()
    {
        UpdateGroundState();
        UpdateCoyoteCounter();
        ApplyBufferedJump();
        Move();
        UpdateJumpBufferCounter();
        ResetShadowCarryState();
    }

    /// <summary>
    /// Purpose: Clears the shadow-platform carry flag after one physics step.
    /// Input: None.
    /// Output: The next physics step starts normally.
    /// </summary>
    void ResetShadowCarryState()
    {
        shadowCarriedThisFixedStep = false;
        shadowCarryDeltaThisFixedStep = Vector3.zero;
    }

    // Purpose: Checks if the player wants to move left or right.
    // Input: A and D keys.
    // Output: Saves left, right, or no movement.
    void ReadInput()
    {
        moveInput = GetMoveInput();
    }

    // Purpose: Remembers a jump press for a very short time.
    // Input: W or Space.
    // Output: Allows a slightly early jump press to still work.
    void ReadJumpInput()
    {
        bool jumpPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);

        if (jumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    // Purpose: Decides whether the player is standing on something.
    // Input: Floor-check settings and the short delay after jumping.
    // Output: Marks the player as grounded only on a valid surface.
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

    // Purpose: Looks under the player's feet for floor or shadow platform.
    // Input: Feet position, check size, floor layer, and allowed slope angle.
    // Output: Saves the floor if it is close enough and not too steep.
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

    // Purpose: Clears old floor information.
    // Input: None.
    // Output: Treats the player as in the air until floor is found again.
    void ClearGroundState()
    {
        isGrounded = false;
        groundNormal = Vector3.up;
        currentGroundCollider = null;
    }

    // Purpose: Lets the player jump for a tiny moment after leaving an edge.
    // Input: Whether the player is standing on ground right now.
    // Output: Updates the short late-jump timer.
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

    // Purpose: Counts down the remembered jump press.
    // Input: Current remembered-jump time.
    // Output: Forgets the jump press after a short delay.
    void UpdateJumpBufferCounter()
    {
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.fixedDeltaTime);
        }
    }

    // Purpose: Makes the player jump when the timing is valid.
    // Input: Remembered jump press, late-jump timer, and player velocity.
    // Output: Gives the player upward speed.
    void ApplyBufferedJump()
    {
        if (jumpBufferCounter <= 0f || coyoteCounter <= 0f)
        {
            return;
        }

        Vector3 velocity = rb.velocity;
        velocity.y = jumpForce;
        rb.velocity = velocity;
        PlayJumpSound();

        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        postJumpGroundLockCounter = postJumpGroundLockTime;
        ClearGroundState();
    }

    // Purpose: Plays the jump sound only when the player really jumps.
    // Input: Jump sound settings from the Inspector.
    // Output: Plays one jump sound.
    void PlayJumpSound()
    {
        if (jumpSound == null)
        {
            return;
        }

        if (playerAudioSource == null)
        {
            playerAudioSource = GetComponent<AudioSource>();
        }

        if (playerAudioSource == null)
        {
            return;
        }

        playerAudioSource.PlayOneShot(jumpSound, jumpSoundVolume);
    }

    // Purpose: Plays footstep sounds when the player is moving on a surface.
    // Input: Ground state, movement, Shift key, and footstep sound settings.
    // Output: Plays normal or shadow footsteps at the right speed.
    void UpdateFootstepAudio()
    {
        if (!isGrounded || Mathf.Approximately(moveInput, 0f))
        {
            footstepCounter = 0f;
            return;
        }

        if (playerAudioSource == null)
        {
            playerAudioSource = GetComponent<AudioSource>();
        }

        if (playerAudioSource == null)
        {
            return;
        }

        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isOnShadowSurface = IsStandingOnShadowSurface();
        AudioClip clip = GetFootstepClip(isOnShadowSurface, isHoldingShift);

        if (clip == null)
        {
            footstepCounter = 0f;
            return;
        }

        footstepCounter -= Time.deltaTime;

        if (footstepCounter > 0f)
        {
            return;
        }

        playerAudioSource.PlayOneShot(clip, GetFootstepVolume(isOnShadowSurface, isHoldingShift));

        float interval = isHoldingShift ? runFootstepInterval : walkFootstepInterval;
        footstepCounter = Mathf.Max(0.05f, interval);
    }

    /// <summary>
    /// Purpose: Chooses the correct footstep sound.
    /// Input: Whether the player is on shadow ground and whether they are running.
    /// Output: The sound clip for the next footstep.
    /// </summary>
    AudioClip GetFootstepClip(bool isOnShadowSurface, bool isRunning)
    {
        if (!isOnShadowSurface)
        {
            return footstepSound;
        }

        if (isRunning && shadowRunFootstepSound != null)
        {
            return shadowRunFootstepSound;
        }

        return shadowFootstepSound != null ? shadowFootstepSound : footstepSound;
    }

    /// <summary>
    /// Purpose: Chooses how loud the footstep should be.
    /// Input: Whether the player is on shadow ground and whether they are running.
    /// Output: The footstep volume.
    /// </summary>
    float GetFootstepVolume(bool isOnShadowSurface, bool isRunning)
    {
        if (!isOnShadowSurface)
        {
            return footstepSoundVolume;
        }

        if (isRunning && shadowRunFootstepSound != null)
        {
            return shadowRunFootstepSoundVolume;
        }

        return shadowFootstepSoundVolume;
    }

    /// <summary>
    /// Purpose: Checks if the player is standing on generated shadow ground.
    /// Input: The current floor collider.
    /// Output: True if shadow footstep sounds should be used.
    /// </summary>
    bool IsStandingOnShadowSurface()
    {
        if (currentGroundCollider == null)
        {
            return false;
        }

        Transform groundTransform = currentGroundCollider.transform;

        if (groundTransform.GetComponentInParent<ProjectedShadowAllEdgePlatform>() != null)
        {
            return true;
        }

        string groundName = groundTransform.name;

        return groundName.Contains("GeneratedEdge") ||
               groundName.Contains("WalkableEdges") ||
               groundName.Contains("ProjectedShadow");
    }

    /// <summary>
    /// Purpose: Moves the player for this physics step.
    /// Input: A/D input, Shift key, ground state, and shadow carry state.
    /// Output: Updates the player's speed and direction.
    /// </summary>
    void Move()
    {
        if (rb == null)
        {
            return;
        }

        bool isMoving = moveInput != 0f;
        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isRunning = isMoving && isHoldingShift;
        bool suppressGroundStick = shadowCarriedThisFixedStep;

        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 velocity = rb.velocity;

        if (isGrounded)
        {
            externalLaunchAirControlCounter = 0f;

            if (isMoving)
            {
                Vector3 slopeVelocity = GetSlopeMoveVelocity(moveInput, currentSpeed);
                velocity.x = slopeVelocity.x;
                velocity.y = slopeVelocity.y;

                if (velocity.y <= 0f)
                {
                    if (!suppressGroundStick)
                    {
                        velocity.y -= groundStickVelocity;
                    }
                }
            }
            else
            {
                velocity.x = 0f;

                if (velocity.y <= 0f)
                {
                    velocity.y = suppressGroundStick ? 0f : -groundStickVelocity;
                }
            }
        }
        else
        {
            if (externalLaunchAirControlCounter > 0f)
            {
                externalLaunchAirControlCounter = Mathf.Max(
                    0f,
                    externalLaunchAirControlCounter - Time.fixedDeltaTime
                );
            }
            else if (IsPressingIntoWallOrSteepEdge(moveInput))
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

    // Purpose: Stops the player from pushing into a wall while in the air.
    // Input: Left/right input and a short side check.
    // Output: True if side movement should be stopped.
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

        if (IsWalkableGeneratedShadowCollider(hit.collider))
        {
            return false;
        }

        return normalY <= maxWallClimbNormalY;
    }

    // Purpose: Adjusts movement so the player can walk along a slope.
    // Input: Left/right input, speed, and slope direction.
    // Output: A movement speed that follows the slope.
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

        bool isOnShadowSurface = IsStandingOnShadowSurface();
        bool shouldPreserveHorizontalSpeed =
            preserveHorizontalSpeedOnSlopes ||
            isOnShadowSurface;

        if (!shouldPreserveHorizontalSpeed)
        {
            return slopeDirection * speed;
        }

        float safeHorizontalAmount = Mathf.Max(Mathf.Abs(slopeDirection.x), 0.2f);
        Vector3 targetVelocity = slopeDirection * (speed / safeHorizontalAmount);
        float effectiveSlopeSpeedMultiplier = isOnShadowSurface
            ? Mathf.Max(maxSlopeSpeedMultiplier, 1.35f)
            : maxSlopeSpeedMultiplier;
        float maxAllowedSpeed = speed * effectiveSlopeSpeedMultiplier;

        return Vector3.ClampMagnitude(targetVelocity, maxAllowedSpeed);
    }

    // Purpose: Checks if a collider is one of the generated walkable shadow edges.
    // Input: Collider found by the side check.
    // Output: True if it should still count as walkable shadow ground.
    bool IsWalkableGeneratedShadowCollider(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        string colliderName = hitCollider.name;

        return colliderName.Contains("GeneratedWalkableEdge") ||
               colliderName.Contains("GeneratedCurvedWalkableEdge");
    }

    // Purpose: Turns the character picture left or right.
    // Input: Left/right movement input.
    // Output: Flips only the visual child, not the physics body.
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

    // Purpose: Tells the Animator what the player is doing.
    // Input: Movement input, Shift key, and whether the player is on ground.
    // Output: Updates walk, run, and jump animation switches.
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

    // Purpose: Turns A/D key presses into a simple movement value.
    // Input: A and D keys.
    // Output: -1 means left, 1 means right, 0 means no movement.
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
