using UnityEngine;

/// <summary>
/// Enables a scripted ladder climb when the player reaches a trigger and the shadow-ladder light angle is aligned.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShadowLadderClimbZone : MonoBehaviour
{
    [Header("Ladder Path")]
    public Transform bottomPoint;
    public Transform topPoint;
    public Transform exitPoint;

    [Header("Light Alignment")]
    public Light directionalLight;
    public bool requireLightAlignment = true;
    public float requiredXAngle = -18f;
    public float requiredYAngle = -15f;
    public float angleTolerance = 4f;

    [Header("Climb Motion")]
    public float climbSpeed = 2f;
    public float exitSnapDuration = 0.12f;
    public bool allowClimbDown = true;
    public bool detachAtBottomWhenDescending = true;
    public float bottomDetachDistance = 0.35f;
    public bool tapDescendToDetachAtBottom = true;
    [Range(0f, 0.5f)] public float bottomTapDetachAmount = 0.25f;

    [Header("Auto Grab")]
    public bool autoGrabOnEnter = true;
    public bool snapAutoGrabToBottomPoint = true;

    [Header("Animation")]
    public bool forceClimbAnimationState = true;
    public string climbStateName = "Stickman_Climb";
    public float climbAnimatorSpeed = 1f;
    [Range(0f, 1f)] public float climbStartNormalizedTime = 0f;
    public bool pauseClimbAnimationWithoutInput = true;

    [Header("Input")]
    public KeyCode climbKey = KeyCode.W;
    public KeyCode alternateClimbKey = KeyCode.UpArrow;
    public KeyCode jumpClimbKey = KeyCode.Space;
    public KeyCode descendKey = KeyCode.S;
    public KeyCode alternateDescendKey = KeyCode.DownArrow;

    [Header("Player")]
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

    private void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

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

    private bool IsClimbPressed()
    {
        return Input.GetKeyDown(climbKey) ||
               Input.GetKeyDown(alternateClimbKey) ||
               Input.GetKeyDown(jumpClimbKey);
    }

    private bool IsDescendPressed()
    {
        return Input.GetKeyDown(descendKey) || Input.GetKeyDown(alternateDescendKey);
    }

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

    private float ToSignedAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

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

    private Vector3 GetLadderPosition(float amount)
    {
        return Vector3.Lerp(bottomPoint.position, topPoint.position, amount);
    }

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

    private float GetBottomDetachAmount(float ladderLength)
    {
        float distanceDetachAmount = Mathf.Clamp01(bottomDetachDistance / ladderLength);
        return Mathf.Max(distanceDetachAmount, bottomTapDetachAmount);
    }

    private void StartExitSnap()
    {
        exitSnapStart = player.position;
        exitSnapTimer = Mathf.Max(0.01f, exitSnapDuration);
    }

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
    }

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

    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (playerAnimator == null || !HasAnimatorBool(parameterName))
        {
            return;
        }

        playerAnimator.SetBool(parameterName, value);
    }

    private void PlayClimbAnimationState()
    {
        if (!forceClimbAnimationState || playerAnimator == null || string.IsNullOrEmpty(climbStateName))
        {
            return;
        }

        playerAnimator.Play(climbStateName, 0, climbStartNormalizedTime);
        playerAnimator.Update(0f);
    }

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
