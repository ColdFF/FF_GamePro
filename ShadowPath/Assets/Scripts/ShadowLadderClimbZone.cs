using UnityEngine;

/// <summary>
/// Purpose: Lets the player climb a shadow ladder when the light angle is correct.
/// Input: Player trigger contact, climb keys, ladder points, and light angle.
/// Output: The player is moved along the ladder path and exits at the top.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShadowLadderClimbZone : MonoBehaviour
{
    [Header("Ladder Path")]
    // Bottom position of the ladder path.
    public Transform bottomPoint;
    // Top position of the ladder path.
    public Transform topPoint;
    // Position where the player is placed after reaching the top.
    public Transform exitPoint;

    [Header("Light Alignment")]
    // Light that must point at the correct angle for the ladder to work.
    public Light directionalLight;
    // If true, the ladder only works when the light is aligned.
    public bool requireLightAlignment = true;
    // Required up/down light angle.
    public float requiredXAngle = -18f;
    // Required left/right light angle.
    public float requiredYAngle = -15f;
    // Allowed angle difference before the ladder stops working.
    public float angleTolerance = 4f;

    [Header("Climb Motion")]
    // How fast the player moves along the ladder.
    public float climbSpeed = 2f;
    // How long the player takes to snap from the top of the ladder to the exit point.
    public float exitSnapDuration = 0.12f;
    // If true, the player can climb down with the descend key.
    public bool allowClimbDown = true;
    // If true, climbing down near the bottom makes the player leave the ladder.
    public bool detachAtBottomWhenDescending = true;
    // Distance from the bottom where descending can detach the player.
    public float bottomDetachDistance = 0.35f;
    // If true, tapping the descend key near the bottom detaches the player.
    public bool tapDescendToDetachAtBottom = true;
    // How close to the bottom the player must be for tap-to-detach.
    [Range(0f, 0.5f)] public float bottomTapDetachAmount = 0.25f;

    [Header("Auto Grab")]
    // If true, the player starts climbing automatically when entering the trigger.
    public bool autoGrabOnEnter = true;
    // If true, auto-grab places the player exactly at the bottom point first.
    public bool snapAutoGrabToBottomPoint = true;

    [Header("Animation")]
    // If true, forces the climb animation while the player is on the ladder.
    public bool forceClimbAnimationState = true;
    // Name of the climb state inside the Animator.
    public string climbStateName = "Stickman_Climb";
    // Speed of the climb animation.
    public float climbAnimatorSpeed = 1f;
    // Where the climb animation starts, from 0 to 1.
    [Range(0f, 1f)] public float climbStartNormalizedTime = 0f;
    // If true, the climb animation pauses when the player is not pressing up or down.
    public bool pauseClimbAnimationWithoutInput = true;

    [Header("Climb Audio")]
    // AudioSource used to play climb step sounds.
    public AudioSource climbAudioSource;
    // Step sound played while climbing.
    public AudioClip climbStepSound;
    // Volume of the climb step sound.
    [Range(0f, 1f)] public float climbStepVolume = 0.32f;
    // Time between step sounds while climbing.
    public float climbStepInterval = 0.18f;
    // Input must be stronger than this before step sounds play.
    public float climbStepInputThreshold = 0.05f;

    [Header("Input")]
    // Main key for climbing up.
    public KeyCode climbKey = KeyCode.W;
    // Backup key for climbing up.
    public KeyCode alternateClimbKey = KeyCode.UpArrow;
    // Space can also start climbing.
    public KeyCode jumpClimbKey = KeyCode.Space;
    // Main key for climbing down.
    public KeyCode descendKey = KeyCode.S;
    // Backup key for climbing down.
    public KeyCode alternateDescendKey = KeyCode.DownArrow;

    [Header("Player")]
    // Tag used to recognise the player.
    public string playerTag = "Player";

    private Transform player;
    private Rigidbody playerRigidbody;
    private PlayerController playerController;
    private Animator playerAnimator;

    private bool playerInside;
    private bool isClimbing;
    private bool previousUseGravity;
    private bool previousIsKinematic;
    private bool previousPlayerControllerEnabled;
    private float previousAnimatorSpeed = 1f;
    private float climbAmount;
    private float exitSnapTimer;
    private Vector3 exitSnapStart;
    private float climbStepCounter;

    // Purpose: Makes sure this object's collider is a trigger when the component is reset.
    // Input: Collider on this GameObject.
    // Output: The ladder zone can detect the player without physically blocking them.
    private void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    // Purpose: Makes sure this object's collider is a trigger when the level starts.
    // Input: Collider on this GameObject.
    // Output: The ladder zone is ready to detect the player.
    private void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    // Purpose: Starts or updates climbing based on player input.
    // Input: Player position, climb keys, and light alignment.
    // Output: The player either begins climbing or continues moving on the ladder.
    private void Update()
    {
        if (isClimbing)
        {
            UpdateClimb();
            return;
        }

        if (!playerInside || !IsClimbPressed() || !IsLightAligned())
        {
            return;
        }

        BeginClimb(false);
    }

    // Purpose: Detects when the player enters the ladder area.
    // Input: Trigger contact with the player.
    // Output: The player is cached and may auto-grab the ladder.
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        CachePlayer(other);
        playerInside = true;

        if (!isClimbing && autoGrabOnEnter && IsLightAligned())
        {
            BeginClimb(snapAutoGrabToBottomPoint);
        }
    }

    // Purpose: Detects when the player leaves the ladder area.
    // Input: Trigger exit with the player.
    // Output: The ladder stops treating the player as nearby if they are not climbing.
    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        if (!isClimbing)
        {
            playerInside = false;
        }
    }

    // Purpose: Checks if a collider belongs to the player.
    // Input: The collider that entered or exited the trigger.
    // Output: True means this collider is the player.
    private bool IsPlayerCollider(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            return true;
        }

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
        {
            return true;
        }

        return false;
    }

    // Purpose: Saves player-related components for climbing.
    // Input: The player's collider.
    // Output: Player Transform, Rigidbody, PlayerController, and Animator are remembered.
    private void CachePlayer(Collider playerCollider)
    {
        if (player != null)
        {
            return;
        }

        if (playerCollider.attachedRigidbody != null)
        {
            player = playerCollider.attachedRigidbody.transform;
        }
        else
        {
            player = playerCollider.transform;
        }

        playerRigidbody = player.GetComponent<Rigidbody>();
        playerController = player.GetComponent<PlayerController>();

        if (playerController != null && playerController.animator != null)
        {
            playerAnimator = playerController.animator;
        }
        else
        {
            playerAnimator = player.GetComponentInChildren<Animator>(true);
        }
    }

    // Purpose: Checks if the player pressed a climb/start key.
    // Input: W, Up Arrow, or Space.
    // Output: True means the player wants to start climbing.
    private bool IsClimbPressed()
    {
        return Input.GetKeyDown(climbKey) ||
               Input.GetKeyDown(alternateClimbKey) ||
               Input.GetKeyDown(jumpClimbKey);
    }

    // Purpose: Checks if the player tapped a descend key.
    // Input: S or Down Arrow.
    // Output: True means the player wants to descend or detach.
    private bool IsDescendPressed()
    {
        return Input.GetKeyDown(descendKey) || Input.GetKeyDown(alternateDescendKey);
    }

    // Purpose: Reads continuous climb direction.
    // Input: Up/down climb keys.
    // Output: 1 for up, -1 for down, 0 for no movement.
    private float GetClimbInput()
    {
        float input = 0f;

        if (Input.GetKey(climbKey) || Input.GetKey(alternateClimbKey))
        {
            input += 1f;
        }

        if (allowClimbDown && (Input.GetKey(descendKey) || Input.GetKey(alternateDescendKey)))
        {
            input -= 1f;
        }

        return Mathf.Clamp(input, -1f, 1f);
    }

    // Purpose: Checks whether the light is pointing correctly for this ladder.
    // Input: Current light angle and required angle settings.
    // Output: True means the shadow ladder is usable.
    private bool IsLightAligned()
    {
        if (!requireLightAlignment)
        {
            return true;
        }

        if (directionalLight == null)
        {
            return false;
        }

        float currentX = ToSignedAngle(directionalLight.transform.eulerAngles.x);
        float currentY = ToSignedAngle(directionalLight.transform.eulerAngles.y);

        float xDifference = Mathf.Abs(Mathf.DeltaAngle(requiredXAngle, currentX));
        float yDifference = Mathf.Abs(Mathf.DeltaAngle(requiredYAngle, currentY));

        return xDifference <= angleTolerance && yDifference <= angleTolerance;
    }

    // Purpose: Converts Unity's 0-360 angle into a more readable -180 to 180 angle.
    // Input: One Unity angle value.
    // Output: The same angle in signed form.
    private float ToSignedAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    // Purpose: Puts the player onto the ladder and takes over their movement.
    // Input: Whether the player should snap to the ladder bottom first.
    // Output: Normal player control is paused and climb mode starts.
    private void BeginClimb(bool snapToBottomPoint)
    {
        if (isClimbing)
        {
            return;
        }

        if (player == null || bottomPoint == null || topPoint == null)
        {
            return;
        }

        isClimbing = true;
        exitSnapTimer = 0f;
        climbStepCounter = 0f;
        climbAmount = snapToBottomPoint ? 0f : GetClosestAmountOnLadder(player.position);

        if (playerController != null)
        {
            previousPlayerControllerEnabled = playerController.enabled;
            playerController.enabled = false;
        }

        if (playerRigidbody != null)
        {
            previousUseGravity = playerRigidbody.useGravity;
            previousIsKinematic = playerRigidbody.isKinematic;

            if (!playerRigidbody.isKinematic)
            {
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            playerRigidbody.useGravity = false;
            playerRigidbody.isKinematic = true;
        }

        if (playerAnimator != null)
        {
            previousAnimatorSpeed = playerAnimator.speed;
            playerAnimator.speed = Mathf.Max(0.01f, climbAnimatorSpeed);
        }

        SetAnimatorBool("isWalking", false);
        SetAnimatorBool("isRunning", false);
        SetAnimatorBool("isJumping", false);
        SetAnimatorBool("isClimbing", true);
        PlayClimbAnimationState();
        UpdateClimbAnimationSpeed(0f);

        MovePlayerTo(GetLadderPosition(climbAmount));
    }

    // Purpose: Finds where the player currently is along the ladder.
    // Input: Player world position.
    // Output: A value from 0 at the bottom to 1 at the top.
    private float GetClosestAmountOnLadder(Vector3 worldPosition)
    {
        Vector3 start = bottomPoint.position;
        Vector3 end = topPoint.position;
        Vector3 ladder = end - start;

        if (ladder.sqrMagnitude < 0.0001f)
        {
            return 0f;
        }

        float amount = Vector3.Dot(worldPosition - start, ladder) / ladder.sqrMagnitude;
        return Mathf.Clamp01(amount);
    }

    // Purpose: Moves the player up or down the ladder while climbing.
    // Input: Climb input and current ladder progress.
    // Output: The player moves, exits at the top, or detaches at the bottom.
    private void UpdateClimb()
    {
        if (player == null || bottomPoint == null || topPoint == null)
        {
            FinishClimb(false);
            return;
        }

        if (exitSnapTimer > 0f)
        {
            UpdateExitSnap();
            return;
        }

        if (tapDescendToDetachAtBottom && IsDescendPressed() && ShouldTapDetachAtBottom())
        {
            FinishClimb(false);
            return;
        }

        float input = GetClimbInput();
        UpdateClimbAnimationSpeed(input);
        UpdateClimbAudio(input);

        if (input < -0.01f && ShouldDetachAtBottom())
        {
            FinishClimb(false);
            return;
        }

        float ladderLength = Vector3.Distance(bottomPoint.position, topPoint.position);

        if (ladderLength < 0.0001f)
        {
            FinishClimb(false);
            return;
        }

        climbAmount += input * climbSpeed * Time.deltaTime / ladderLength;
        climbAmount = Mathf.Clamp01(climbAmount);

        MovePlayerTo(GetLadderPosition(climbAmount));

        if (input < -0.01f && ShouldDetachAtBottom())
        {
            FinishClimb(false);
            return;
        }

        if (climbAmount >= 1f)
        {
            StartExitSnap();
        }
    }

    // Purpose: Gets the exact world position on the ladder path.
    // Input: A value from 0 at the bottom to 1 at the top.
    // Output: The matching position between bottomPoint and topPoint.
    private Vector3 GetLadderPosition(float amount)
    {
        return Vector3.Lerp(bottomPoint.position, topPoint.position, amount);
    }

    // Purpose: Checks if climbing down should release the player at the bottom.
    // Input: Current ladder progress and detach settings.
    // Output: True means finish climbing and restore normal control.
    private bool ShouldDetachAtBottom()
    {
        if (!detachAtBottomWhenDescending || bottomPoint == null || topPoint == null)
        {
            return false;
        }

        float ladderLength = Vector3.Distance(bottomPoint.position, topPoint.position);

        if (ladderLength < 0.0001f)
        {
            return true;
        }

        return climbAmount <= GetBottomDetachAmount(ladderLength);
    }

    // Purpose: Checks if a quick descend tap should release the player near the bottom.
    // Input: Current ladder progress and tap-detach settings.
    // Output: True means the player should leave the ladder.
    private bool ShouldTapDetachAtBottom()
    {
        if (bottomPoint == null || topPoint == null)
        {
            return false;
        }

        float ladderLength = Vector3.Distance(bottomPoint.position, topPoint.position);

        if (ladderLength < 0.0001f)
        {
            return true;
        }

        return climbAmount <= GetBottomDetachAmount(ladderLength);
    }

    // Purpose: Converts bottom detach distance into ladder progress amount.
    // Input: Total ladder length.
    // Output: How close to the bottom counts as detachable.
    private float GetBottomDetachAmount(float ladderLength)
    {
        float distanceDetachAmount = Mathf.Clamp01(bottomDetachDistance / ladderLength);
        return Mathf.Max(distanceDetachAmount, bottomTapDetachAmount);
    }

    // Purpose: Starts the short move from ladder top to exit point.
    // Input: Current player position and exit snap duration.
    // Output: Exit snapping begins.
    private void StartExitSnap()
    {
        exitSnapStart = player.position;
        exitSnapTimer = Mathf.Max(0.01f, exitSnapDuration);
    }

    // Purpose: Moves the player from the ladder top to the exit point.
    // Input: Time passing during the exit snap.
    // Output: The player reaches the exit point and climbing ends.
    private void UpdateExitSnap()
    {
        exitSnapTimer -= Time.deltaTime;

        float duration = Mathf.Max(0.01f, exitSnapDuration);
        float progress = 1f - Mathf.Clamp01(exitSnapTimer / duration);
        Vector3 targetPosition = exitPoint != null ? exitPoint.position : topPoint.position;

        MovePlayerTo(Vector3.Lerp(exitSnapStart, targetPosition, progress));

        if (exitSnapTimer <= 0f)
        {
            FinishClimb(true);
        }
    }

    // Purpose: Ends ladder climbing and restores normal player control.
    // Input: Whether the player should be placed at the exit point.
    // Output: Rigidbody, PlayerController, and Animator are restored.
    private void FinishClimb(bool placeAtExit)
    {
        if (placeAtExit && player != null)
        {
            Vector3 targetPosition = exitPoint != null ? exitPoint.position : topPoint.position;
            MovePlayerTo(targetPosition);
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = previousIsKinematic;
            playerRigidbody.useGravity = previousUseGravity;

            if (!playerRigidbody.isKinematic)
            {
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
        }

        if (playerController != null)
        {
            playerController.enabled = previousPlayerControllerEnabled;
            playerController.PrepareForInputUnlock();
        }

        SetAnimatorBool("isClimbing", false);

        if (playerAnimator != null)
        {
            playerAnimator.speed = previousAnimatorSpeed;
        }

        isClimbing = false;
        playerInside = false;
        climbStepCounter = 0f;
    }

    // Purpose: Places the player at a specific world position.
    // Input: Target position in the Unity scene.
    // Output: Player Transform or Rigidbody moves there.
    private void MovePlayerTo(Vector3 worldPosition)
    {
        if (playerRigidbody != null)
        {
            if (playerRigidbody.isKinematic)
            {
                playerRigidbody.position = worldPosition;
                player.position = worldPosition;
                return;
            }

            playerRigidbody.MovePosition(worldPosition);
            return;
        }

        if (player != null)
        {
            player.position = worldPosition;
        }
    }

    // Purpose: Sets a bool on the player's Animator if it exists.
    // Input: Animator parameter name and true/false value.
    // Output: The animation parameter changes safely.
    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (playerAnimator == null || !HasAnimatorBool(parameterName))
        {
            return;
        }

        playerAnimator.SetBool(parameterName, value);
    }

    // Purpose: Starts the climb animation state directly.
    // Input: Animator and climb state name.
    // Output: The character shows the climb pose/animation.
    private void PlayClimbAnimationState()
    {
        if (!forceClimbAnimationState || playerAnimator == null || string.IsNullOrEmpty(climbStateName))
        {
            return;
        }

        playerAnimator.Play(climbStateName, 0, climbStartNormalizedTime);
        playerAnimator.Update(0f);
    }

    // Purpose: Controls whether the climb animation plays or pauses.
    // Input: Current climb input.
    // Output: Animation moves while climbing and can pause when idle.
    private void UpdateClimbAnimationSpeed(float climbInput)
    {
        if (playerAnimator == null)
        {
            return;
        }

        if (!pauseClimbAnimationWithoutInput)
        {
            playerAnimator.speed = Mathf.Max(0.01f, climbAnimatorSpeed);
            return;
        }

        playerAnimator.speed = Mathf.Abs(climbInput) > 0.01f ? Mathf.Max(0.01f, climbAnimatorSpeed) : 0f;
    }

    // Purpose: Plays climbing step sounds while the player is moving.
    // Input: Climb input and step sound settings.
    // Output: Step sounds play at intervals.
    private void UpdateClimbAudio(float climbInput)
    {
        if (climbStepSound == null || Mathf.Abs(climbInput) < climbStepInputThreshold)
        {
            climbStepCounter = 0f;
            return;
        }

        climbStepCounter -= Time.deltaTime;

        if (climbStepCounter > 0f)
        {
            return;
        }

        AudioSource audioSource = GetClimbAudioSource();
        if (audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(climbStepSound, climbStepVolume);
        climbStepCounter = Mathf.Max(0.03f, climbStepInterval);
    }

    // Purpose: Gets or creates the AudioSource used for ladder sounds.
    // Input: Optional assigned AudioSource.
    // Output: An AudioSource ready to play climb sounds.
    private AudioSource GetClimbAudioSource()
    {
        if (climbAudioSource != null)
        {
            return climbAudioSource;
        }

        if (!Application.isPlaying || climbStepSound == null)
        {
            return null;
        }

        climbAudioSource = gameObject.AddComponent<AudioSource>();
        climbAudioSource.playOnAwake = false;
        climbAudioSource.loop = false;
        climbAudioSource.spatialBlend = 0f;
        return climbAudioSource;
    }

    // Purpose: Checks if the Animator has a bool with this name.
    // Input: Animator parameter name.
    // Output: True means it is safe to set that bool.
    private bool HasAnimatorBool(string parameterName)
    {
        if (playerAnimator == null)
        {
            return false;
        }

        for (int i = 0; i < playerAnimator.parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = playerAnimator.parameters[i];

            if (parameter.type == AnimatorControllerParameterType.Bool &&
                parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Draws the ladder path in the Scene view.
    // Input: Bottom, top, and exit points.
    // Output: Colored gizmos help place the ladder while editing.
    private void OnDrawGizmosSelected()
    {
        if (bottomPoint == null || topPoint == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(bottomPoint.position, topPoint.position);
        Gizmos.DrawSphere(bottomPoint.position, 0.12f);
        Gizmos.DrawSphere(topPoint.position, 0.12f);

        if (exitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(exitPoint.position, 0.14f);
            Gizmos.DrawLine(topPoint.position, exitPoint.position);
        }
    }
}
