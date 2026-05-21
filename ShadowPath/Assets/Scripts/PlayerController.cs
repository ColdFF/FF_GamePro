using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5f;
    public float jumpForce = 6f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("Visual")]
    public Transform visual;

    [Header("Animation")]
    public Animator animator;

    private Rigidbody rb;
    private bool isGrounded;

    // Purpose: Initializes required components when the game starts.
    // Input: None.
    // Output: Stores the Rigidbody component for movement and jumping.
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Purpose: Runs all player logic every frame.
    // Input: Keyboard input and current physics state.
    // Output: Updates ground detection, movement, jump behavior, and animation state.
    void Update()
    {
        CheckGround();
        Move();
        Jump();
        UpdateAnimation();
    }

    // Purpose: Checks whether the player is standing on a valid ground object.
    // Input: GroundCheck position, check radius, and Ground Layer.
    // Output: Updates the isGrounded boolean.
    void CheckGround()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    // Purpose: Handles horizontal walking and running movement.
    // Input: A/D keys for movement and Shift key for running.
    // Output: Updates Rigidbody horizontal velocity and flips the visual direction.
    void Move()
    {
        float moveInput = GetMoveInput();

        bool isMoving = moveInput != 0f;
        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isRunning = isMoving && isHoldingShift;

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 velocity = rb.velocity;
        velocity.x = moveInput * currentSpeed;
        rb.velocity = velocity;

        if (visual != null)
        {
            if (moveInput > 0f)
            {
                visual.localScale = new Vector3(1f, 1f, 1f);
            }
            else if (moveInput < 0f)
            {
                visual.localScale = new Vector3(-1f, 1f, 1f);
            }
        }
    }

    // Purpose: Handles player jumping from the ground.
    // Input: W key or Space key while the player is grounded.
    // Output: Applies upward velocity to the Rigidbody.
    void Jump()
    {
        bool jumpPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);

        if (jumpPressed && isGrounded)
        {
            Vector3 velocity = rb.velocity;
            velocity.y = jumpForce;
            rb.velocity = velocity;
        }
    }

    // Purpose: Updates Animator parameters based on current player state.
    // Input: Movement keys, Shift key, and grounded state.
    // Output: Sets isWalking, isRunning, and isJumping in the Animator.
    void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        float moveInput = GetMoveInput();

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