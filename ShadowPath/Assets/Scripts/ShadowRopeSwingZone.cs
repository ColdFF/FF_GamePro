using UnityEngine;

/// <summary>
/// Creates a grab trigger from a rope's projected shadow and controls a simple pendulum swing while the player is attached.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class ShadowRopeSwingZone : MonoBehaviour
{
    [Header("Projection References")]
    public Transform ropeStart;
    public Transform ropeEnd;
    public Transform shadowScreen;
    public Light directionalLight;

    [Header("Grab Zone")]
    public string playerTag = "Player";
    public float grabWidth = 0.45f;
    public float triggerDepth = 8f;
    public float shadowSurfaceOffset = -0.02f;
    public bool autoGrabOnEnter = false;

    [Header("Player Hand Grab")]
    public Transform playerRoot;
    public Transform playerHandPoint;
    public Vector3 fallbackHandLocalOffset = new Vector3(0.22f, 0.65f, 0f);
    public float handGrabDistance = 0.38f;
    public bool useHandDistanceGrab = true;
    public bool faceRopeOnAttach = true;
    public bool autoGrabOnlyWhenAirborne = true;
    public float releaseAutoGrabCooldown = 0.25f;

    [Header("Rope End Attachment")]
    public bool attachHandToProjectedRopeEnd = true;
    public bool driveRopeEndWhileSwinging = true;
    public float attachSnapDuration = 0.12f;
    public bool keepRopeSwingingAfterRelease = true;
    public float settleAngleTolerance = 1f;
    public float settleAngularSpeedTolerance = 0.08f;

    [Header("Hand Pinning")]
    public bool pinHandInLateUpdate = true;
    public Vector3 handAttachmentWorldOffset = Vector3.zero;

    [Header("Wall Slide Pose")]
    public Sprite[] swingPoseSprites;
    public float swingPoseFrameRate = 6f;
    public bool disableAnimatorForSwingPose = true;
    public bool flipSwingPoseWithInput = true;
    public bool swingPoseFacesRightByDefault = false;
    public bool lockFacingWhenSwingSettled = true;
    public bool settledFacingRight = true;
    public float settledFacingAngleRange = 8f;
    public float settledFacingAngularSpeed = 0.25f;
    public float facingInputThreshold = 0.15f;
    public float facingAngularSpeedThreshold = 0.45f;
    public float facingSwitchCooldown = 0.12f;

    [Header("Swing Motion")]
    public float gravity = 13f;
    public float inputAngularAcceleration = 3.5f;
    [Range(0.1f, 1f)] public float swingInputSensitivity = 0.78f;
    [Range(1f, 2f)] public float shiftSwingBoostMultiplier = 1.25f;
    [Range(0f, 25f)] public float shiftAssistAngleBonus = 8f;
    public bool useMomentumBasedSwingInput = true;
    [Range(0f, 45f)] public float bottomAssistAngleRange = 18f;
    [Range(5f, 85f)] public float inputAssistAngleRange = 52f;
    [Range(0f, 30f)] public float inputAssistSoftZone = 14f;
    public float minAssistAngularSpeed = 0.18f;
    [Range(0.8f, 1f)] public float angularDamping = 0.992f;
    public float maxAngularSpeed = 5.5f;
    [Range(15f, 89f)] public float maxSwingAngleFromRest = 72f;
    [Range(0f, 30f)] public float swingLimitSoftZone = 12f;
    public bool useSoftSwingLimitBraking = true;
    public float softLimitBrakeStrength = 7f;
    public float softLimitReturnStrength = 10f;
    public bool fadeOutwardInputNearLimits = true;
    public float minSwingRadius = 0.55f;
    public float releaseSpeedMultiplier = 1f;
    public float minReleaseTangentialSpeed = 3.2f;
    public bool useManualReleaseSpeed = true;
    public float manualReleaseTangentialSpeed = 5.2f;
    public float releaseAirControlDelay = 0.24f;
    public float releaseInputSpeedBoost = 0.8f;
    public float releaseShiftSpeedBoost = 0.8f;
    public float releaseApexSpeedBoost = 0.9f;
    public float releaseUpwardBoost = 0.4f;
    public float maxReleaseSpeed = 8f;

    [Header("Input")]
    public KeyCode grabKey = KeyCode.W;
    public KeyCode alternateGrabKey = KeyCode.Space;
    public KeyCode releaseKey = KeyCode.Space;
    public KeyCode alternateReleaseKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode boostKey = KeyCode.LeftShift;
    public KeyCode alternateBoostKey = KeyCode.RightShift;

    [Header("Animation")]
    public bool forceClimbAnimationState = true;
    public string climbStateName = "Stickman_Climb";
    public float attachedAnimatorSpeed = 0f;

    [Header("Read Only Diagnostics")]
    [SerializeField] private bool projectionValid;
    [SerializeField] private bool playerAttached;
    [SerializeField] private float currentProjectedLength;
    [SerializeField] private float handDistanceToRope;
    [SerializeField] private bool handInGrabRange;
    [SerializeField] private Vector3 projectedStart;
    [SerializeField] private Vector3 projectedEnd;
    [SerializeField] private Vector3 closestHandGrabPoint;
    [SerializeField] private Vector3 currentHandTargetPosition;
    [SerializeField] private float handPinError;

    private BoxCollider grabCollider;
    private Collider candidatePlayerCollider;
    private Transform player;
    private Rigidbody playerRigidbody;
    private PlayerController playerController;
    private Animator playerAnimator;
    private Transform playerVisual;
    private SpriteRenderer playerSpriteRenderer;

    private bool previousUseGravity;
    private bool previousIsKinematic;
    private bool previousPlayerControllerEnabled;
    private float previousAnimatorSpeed = 1f;
    private bool previousAnimatorEnabled = true;
    private Sprite previousSprite;
    private Vector3 previousVisualScale;
    private float swingRadius;
    private float swingAngle;
    private float angularVelocity;
    private float playerPlaneZ;
    private Vector3 attachedHandOffsetFromPlayer;
    private Vector3 ropeRestEndWorldPosition;
    private Vector3 ropeRestEndOffset;
    private float ropeRestAngle;
    private float ropeRestZOffset;
    private bool hasRopeRestState;
    private bool ropeSettlingAfterRelease;
    private float attachSnapTimer;
    private Vector3 attachSnapStartHandPosition;
    private Vector3 previousHandTargetPosition;
    private Vector3 handTargetVelocity;
    private bool hasPreviousHandTargetPosition;
    private bool hasCurrentHandTargetPosition;
    private float lastReleaseDirectionSign = 1f;
    private float lastRawSwingInput;
    private bool lastBoostInputHeld;
    private bool usingManualSwingPose;
    private bool hasSwingPoseFacing;
    private bool currentSwingPoseFacingRight;
    private float facingSwitchCooldownTimer;
    private float swingPoseTimer;
    private int swingPoseIndex;
    private bool releaseRequested;
    private float autoGrabCooldownTimer;

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        ConfigureCollider();
    }

    private void Update()
    {
        UpdateGrabZone();

        if (!Application.isPlaying)
        {
            return;
        }

        if (autoGrabCooldownTimer > 0f)
        {
            autoGrabCooldownTimer = Mathf.Max(0f, autoGrabCooldownTimer - Time.deltaTime);
        }

        UpdateHandGrabDiagnostics();
        bool autoGrabAllowed = CanAutoGrab();

        if (playerAttached)
        {
            UpdateManualSwingPose(Time.deltaTime);

            if (IsReleasePressed())
            {
                releaseRequested = true;
            }

            return;
        }

        if (!useHandDistanceGrab && candidatePlayerCollider != null && IsGrabPressed())
        {
            BeginSwing(candidatePlayerCollider);
            return;
        }

        if (!useHandDistanceGrab && candidatePlayerCollider != null && autoGrabAllowed)
        {
            BeginSwing(candidatePlayerCollider);
            return;
        }

        if (useHandDistanceGrab && handInGrabRange && (IsGrabPressed() || autoGrabAllowed))
        {
            BeginSwing(null);
        }
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UpdateGrabZone();

        if (!playerAttached)
        {
            if (ropeSettlingAfterRelease)
            {
                UpdateSwing(false);
            }

            return;
        }

        if (releaseRequested)
        {
            FinishSwing(true);
            return;
        }

        UpdateSwing(true);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || !pinHandInLateUpdate || !playerAttached || !hasCurrentHandTargetPosition)
        {
            return;
        }

        MovePlayerHandTo(currentHandTargetPosition);
        handPinError = Vector3.Distance(GetHandWorldPosition(), currentHandTargetPosition);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        candidatePlayerCollider = other;

        if (!useHandDistanceGrab && !playerAttached && CanAutoGrab(other))
        {
            BeginSwing(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        candidatePlayerCollider = other;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (candidatePlayerCollider == other)
        {
            candidatePlayerCollider = null;
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying && playerAttached)
        {
            FinishSwing(false);
        }
    }

    private void ConfigureCollider()
    {
        grabCollider = GetComponent<BoxCollider>();
        grabCollider.isTrigger = true;
        grabCollider.center = Vector3.zero;
    }

    private void UpdateGrabZone()
    {
        if (grabCollider == null)
        {
            ConfigureCollider();
        }

        projectionValid = TryGetProjectedRope(out projectedStart, out projectedEnd);

        if (!projectionValid)
        {
            if (grabCollider != null)
            {
                grabCollider.enabled = false;
            }

            return;
        }

        Vector3 ropeAxis = projectedEnd - projectedStart;
        currentProjectedLength = ropeAxis.magnitude;

        if (currentProjectedLength < 0.05f)
        {
            grabCollider.enabled = false;
            projectionValid = false;
            return;
        }

        grabCollider.enabled = true;

        Vector3 axisDirection = ropeAxis / currentProjectedLength;
        Vector3 screenForward = shadowScreen.forward.normalized;
        Vector3 right = Vector3.Cross(axisDirection, screenForward);

        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.Cross(axisDirection, Vector3.forward);
        }

        right.Normalize();
        screenForward = Vector3.Cross(right, axisDirection).normalized;

        transform.position = (projectedStart + projectedEnd) * 0.5f;
        transform.rotation = Quaternion.LookRotation(screenForward, axisDirection);
        grabCollider.size = new Vector3(grabWidth, currentProjectedLength, triggerDepth);
    }

    private void UpdateHandGrabDiagnostics()
    {
        handDistanceToRope = float.PositiveInfinity;
        handInGrabRange = false;

        if (!useHandDistanceGrab || !projectionValid || !TryAcquirePlayerReference())
        {
            return;
        }

        Vector3 handPosition = GetHandWorldPosition();
        closestHandGrabPoint = GetClosestProjectedRopePoint(handPosition);
        handDistanceToRope = Vector3.Distance(handPosition, closestHandGrabPoint);
        handInGrabRange = handDistanceToRope <= handGrabDistance;
    }

    private bool TryGetProjectedRope(out Vector3 start, out Vector3 end)
    {
        start = Vector3.zero;
        end = Vector3.zero;

        if (ropeStart == null || ropeEnd == null || shadowScreen == null || directionalLight == null)
        {
            return false;
        }

        Plane screenPlane = new Plane(
            shadowScreen.forward,
            shadowScreen.position + shadowScreen.forward.normalized * shadowSurfaceOffset
        );

        Vector3 lightDirection = directionalLight.transform.forward.normalized;

        return TryProjectPoint(ropeStart.position, screenPlane, lightDirection, out start) &&
               TryProjectPoint(ropeEnd.position, screenPlane, lightDirection, out end);
    }

    private bool TryProjectPoint(Vector3 worldPoint, Plane screenPlane, Vector3 lightDirection, out Vector3 projectedPoint)
    {
        Ray projectionRay = new Ray(worldPoint, lightDirection);

        if (!screenPlane.Raycast(projectionRay, out float distance))
        {
            projectedPoint = Vector3.zero;
            return false;
        }

        projectedPoint = projectionRay.GetPoint(distance);
        return true;
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            return true;
        }

        return other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag);
    }

    private bool IsGrabPressed()
    {
        return Input.GetKeyDown(grabKey) || Input.GetKeyDown(alternateGrabKey);
    }

    private bool IsReleasePressed()
    {
        return Input.GetKeyDown(releaseKey) || Input.GetKeyDown(alternateReleaseKey);
    }

    private bool CanAutoGrab(Collider playerCollider = null)
    {
        if (!autoGrabOnEnter || autoGrabCooldownTimer > 0f)
        {
            return false;
        }

        if (playerCollider != null)
        {
            CachePlayer(playerCollider);
        }
        else if (!TryAcquirePlayerReference())
        {
            return false;
        }

        if (!autoGrabOnlyWhenAirborne)
        {
            return true;
        }

        if (playerController != null)
        {
            return !playerController.IsGrounded;
        }

        return playerRigidbody != null && Mathf.Abs(playerRigidbody.velocity.y) > 0.05f;
    }

    private float GetSwingInput()
    {
        float input = 0f;

        if (Input.GetKey(leftKey))
        {
            input -= 1f;
        }

        if (Input.GetKey(rightKey))
        {
            input += 1f;
        }

        return Mathf.Clamp(input, -1f, 1f);
    }

    private bool IsBoostHeld()
    {
        return Input.GetKey(boostKey) || Input.GetKey(alternateBoostKey);
    }

    private void BeginSwing(Collider playerCollider)
    {
        if (playerAttached || !projectionValid)
        {
            return;
        }

        if (playerCollider != null)
        {
            CachePlayer(playerCollider);
        }
        else if (!TryAcquirePlayerReference())
        {
            return;
        }

        if (player == null || playerRigidbody == null)
        {
            return;
        }

        playerAttached = true;
        releaseRequested = false;
        playerPlaneZ = player.position.z;
        ropeSettlingAfterRelease = false;
        CaptureRopeRestState();

        if (faceRopeOnAttach)
        {
            FaceVisualTowards(projectedStart.x);
        }

        Vector3 handPosition = GetHandWorldPosition();
        attachedHandOffsetFromPlayer = handPosition - player.position;

        previousUseGravity = playerRigidbody.useGravity;
        previousIsKinematic = playerRigidbody.isKinematic;

        if (playerController != null)
        {
            previousPlayerControllerEnabled = playerController.enabled;
            playerController.enabled = false;
        }

        if (!playerRigidbody.isKinematic)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        playerRigidbody.useGravity = false;
        playerRigidbody.isKinematic = true;

        Vector3 ropeOffset = ropeEnd.position - ropeStart.position;
        float initialSwingRadius = new Vector2(ropeOffset.x, ropeOffset.y).magnitude;

        if (initialSwingRadius < minSwingRadius)
        {
            initialSwingRadius = Mathf.Max(minSwingRadius, currentProjectedLength);
        }

        swingRadius = Mathf.Max(minSwingRadius, initialSwingRadius);
        swingAngle = Mathf.Atan2(ropeOffset.x, -ropeOffset.y);
        angularVelocity = 0f;
        attachSnapTimer = Mathf.Max(0f, attachSnapDuration);
        attachSnapStartHandPosition = handPosition;
        previousHandTargetPosition = handPosition;
        handTargetVelocity = Vector3.zero;
        hasPreviousHandTargetPosition = false;

        BeginSwingPose();
        DriveRopeEndFromSwingAngle();
        UpdateGrabZone();
        MovePlayerHandTo(GetCurrentSwingHandTarget());
    }

    private void CachePlayer(Collider playerCollider)
    {
        if (playerCollider.attachedRigidbody != null)
        {
            player = playerCollider.attachedRigidbody.transform;
            playerRigidbody = playerCollider.attachedRigidbody;
        }
        else
        {
            player = playerCollider.transform;
            playerRigidbody = player.GetComponent<Rigidbody>();
        }

        playerController = player.GetComponent<PlayerController>();
        playerRoot = player;

        if (playerController != null && playerController.animator != null)
        {
            playerAnimator = playerController.animator;
            playerVisual = playerController.visual;
        }
        else
        {
            playerAnimator = player.GetComponentInChildren<Animator>(true);
        }

        if (playerVisual == null && playerAnimator != null)
        {
            playerVisual = playerAnimator.transform;
        }

        playerSpriteRenderer = player.GetComponentInChildren<SpriteRenderer>(true);
    }

    private bool TryAcquirePlayerReference()
    {
        if (player != null && playerRigidbody != null)
        {
            return true;
        }

        if (playerRoot == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
            {
                playerRoot = playerObject.transform;
            }
        }

        if (playerRoot == null)
        {
            return false;
        }

        player = playerRoot;
        playerRigidbody = player.GetComponent<Rigidbody>();
        playerController = player.GetComponent<PlayerController>();

        if (playerController != null && playerController.animator != null)
        {
            playerAnimator = playerController.animator;
            playerVisual = playerController.visual;
        }
        else
        {
            playerAnimator = player.GetComponentInChildren<Animator>(true);
        }

        if (playerVisual == null && playerAnimator != null)
        {
            playerVisual = playerAnimator.transform;
        }

        playerSpriteRenderer = player.GetComponentInChildren<SpriteRenderer>(true);

        return playerRigidbody != null;
    }

    private void UpdateSwing(bool movePlayer)
    {
        if (ropeStart == null || ropeEnd == null)
        {
            FinishSwing(false);
            return;
        }

        if (!hasRopeRestState)
        {
            CaptureRopeRestState();
        }

        swingRadius = Mathf.Max(minSwingRadius, swingRadius);

        float rawInput = movePlayer ? GetSwingInput() : 0f;
        bool boostHeld = movePlayer && IsBoostHeld();
        UpdateReleaseDirection(rawInput, boostHeld);
        float angleFromRest = GetAngleFromRestRadians();
        float input = GetMomentumBasedSwingInput(rawInput, angleFromRest, boostHeld);
        input = GetLimitAwareSwingInput(input, angleFromRest);
        float boostMultiplier = boostHeld ? shiftSwingBoostMultiplier : 1f;

        float angularAcceleration = (-gravity / Mathf.Max(0.01f, swingRadius)) * Mathf.Sin(angleFromRest);
        angularAcceleration += GetSoftLimitAcceleration(angleFromRest);
        angularAcceleration += input * inputAngularAcceleration * swingInputSensitivity * boostMultiplier;

        angularVelocity += angularAcceleration * Time.fixedDeltaTime;
        angularVelocity *= Mathf.Pow(angularDamping, Time.fixedDeltaTime * 60f);
        angularVelocity = Mathf.Clamp(angularVelocity, -maxAngularSpeed, maxAngularSpeed);

        swingAngle += angularVelocity * Time.fixedDeltaTime;
        EnforceSwingAngleLimits();
        DriveRopeEndFromSwingAngle();
        UpdateGrabZone();

        if (movePlayer)
        {
            UpdateSwingPoseFacing(rawInput);
            MovePlayerHandTo(GetCurrentSwingHandTarget());
        }
        else
        {
            float remainingAngle = Mathf.Abs(Mathf.DeltaAngle(ropeRestAngle * Mathf.Rad2Deg, swingAngle * Mathf.Rad2Deg));

            if (remainingAngle <= settleAngleTolerance && Mathf.Abs(angularVelocity) <= settleAngularSpeedTolerance)
            {
                ropeEnd.position = ropeRestEndWorldPosition;
                swingAngle = ropeRestAngle;
                angularVelocity = 0f;
                ropeSettlingAfterRelease = false;
                UpdateGrabZone();
            }
        }
    }

    private float GetAngleFromRestRadians()
    {
        return Mathf.DeltaAngle(
            ropeRestAngle * Mathf.Rad2Deg,
            swingAngle * Mathf.Rad2Deg
        ) * Mathf.Deg2Rad;
    }

    private float GetMomentumBasedSwingInput(float input, float angleFromRest, bool boostHeld)
    {
        if (!useMomentumBasedSwingInput || Mathf.Abs(input) < 0.01f)
        {
            return input;
        }

        float inputDirection = Mathf.Sign(input);
        float angleFromRestDegrees = Mathf.Abs(angleFromRest) * Mathf.Rad2Deg;
        bool nearBottom = angleFromRestDegrees <= bottomAssistAngleRange;
        bool movingWithInput =
            Mathf.Abs(angularVelocity) >= minAssistAngularSpeed &&
            Mathf.Sign(angularVelocity) == inputDirection;

        if (!nearBottom && !movingWithInput)
        {
            return 0f;
        }

        float assistAngle = Mathf.Max(
            1f,
            inputAssistAngleRange + (boostHeld ? shiftAssistAngleBonus : 0f)
        );

        if (angleFromRestDegrees >= assistAngle)
        {
            return 0f;
        }

        float softZone = Mathf.Clamp(inputAssistSoftZone, 0f, assistAngle);

        if (softZone <= 0.001f)
        {
            return input;
        }

        float softZoneStart = assistAngle - softZone;

        if (angleFromRestDegrees <= softZoneStart)
        {
            return input;
        }

        float remainingSoftRange = Mathf.Clamp01((assistAngle - angleFromRestDegrees) / softZone);
        return input * remainingSoftRange;
    }

    private float GetSoftLimitAcceleration(float angleFromRest)
    {
        if (!useSoftSwingLimitBraking)
        {
            return 0f;
        }

        float maxAngle = Mathf.Clamp(maxSwingAngleFromRest, 1f, 89f) * Mathf.Deg2Rad;
        float softZone = Mathf.Clamp(swingLimitSoftZone, 0f, maxSwingAngleFromRest) * Mathf.Deg2Rad;

        if (softZone <= 0.001f)
        {
            return 0f;
        }

        float absoluteAngle = Mathf.Abs(angleFromRest);
        float softZoneStart = maxAngle - softZone;

        if (absoluteAngle <= softZoneStart)
        {
            return 0f;
        }

        float direction = Mathf.Sign(angleFromRest);
        float limitAmount = Mathf.Clamp01((absoluteAngle - softZoneStart) / softZone);
        float outwardSpeed = Mathf.Max(0f, angularVelocity * direction);
        float brakeAcceleration = outwardSpeed * softLimitBrakeStrength * limitAmount;
        float returnAcceleration = softLimitReturnStrength * limitAmount * limitAmount;

        return -direction * (brakeAcceleration + returnAcceleration);
    }

    private float GetLimitAwareSwingInput(float input, float angleFromRest)
    {
        if (!fadeOutwardInputNearLimits || Mathf.Abs(input) < 0.01f)
        {
            return input;
        }

        float maxAngle = Mathf.Clamp(maxSwingAngleFromRest, 1f, 89f) * Mathf.Deg2Rad;
        float softZone = Mathf.Clamp(swingLimitSoftZone, 0f, maxSwingAngleFromRest) * Mathf.Deg2Rad;

        if (softZone <= 0.001f)
        {
            return input;
        }

        float absoluteAngle = Mathf.Abs(angleFromRest);
        float softZoneStart = maxAngle - softZone;

        if (absoluteAngle <= softZoneStart)
        {
            return input;
        }

        bool pushingOutward = Mathf.Sign(input) == Mathf.Sign(angleFromRest);

        if (!pushingOutward)
        {
            return input;
        }

        float remainingSoftRange = Mathf.Clamp01((maxAngle - absoluteAngle) / softZone);
        return input * remainingSoftRange;
    }

    private void EnforceSwingAngleLimits()
    {
        float maxAngle = Mathf.Clamp(maxSwingAngleFromRest, 1f, 89f) * Mathf.Deg2Rad;
        float angleFromRest = GetAngleFromRestRadians();

        if (Mathf.Abs(angleFromRest) <= maxAngle)
        {
            return;
        }

        float clampedAngleFromRest = Mathf.Sign(angleFromRest) * maxAngle;
        swingAngle = ropeRestAngle + clampedAngleFromRest;

        bool movingFartherOut = Mathf.Sign(angularVelocity) == Mathf.Sign(angleFromRest);

        if (movingFartherOut)
        {
            angularVelocity = 0f;
        }
    }

    private void CaptureRopeRestState()
    {
        if (ropeStart == null || ropeEnd == null)
        {
            hasRopeRestState = false;
            return;
        }

        ropeRestEndWorldPosition = ropeEnd.position;
        ropeRestEndOffset = ropeEnd.position - ropeStart.position;
        ropeRestZOffset = ropeRestEndOffset.z;

        float planarLength = new Vector2(ropeRestEndOffset.x, ropeRestEndOffset.y).magnitude;
        swingRadius = Mathf.Max(minSwingRadius, planarLength);
        ropeRestAngle = Mathf.Atan2(ropeRestEndOffset.x, -ropeRestEndOffset.y);
        hasRopeRestState = true;
    }

    private void DriveRopeEndFromSwingAngle()
    {
        if (!driveRopeEndWhileSwinging || ropeStart == null || ropeEnd == null || !hasRopeRestState)
        {
            return;
        }

        Vector3 pivot = ropeStart.position;
        Vector3 offset = new Vector3(
            Mathf.Sin(swingAngle) * swingRadius,
            -Mathf.Cos(swingAngle) * swingRadius,
            ropeRestZOffset
        );

        ropeEnd.position = pivot + offset;
    }

    private Vector3 GetCurrentSwingHandTarget()
    {
        Vector3 target = attachHandToProjectedRopeEnd
            ? GetProjectedRopeEndOnPlayerPlane()
            : GetSwingHandPositionFromAngle();

        target += handAttachmentWorldOffset;

        if (attachSnapTimer > 0f)
        {
            attachSnapTimer = Mathf.Max(0f, attachSnapTimer - Time.fixedDeltaTime);
            float duration = Mathf.Max(0.01f, attachSnapDuration);
            float progress = 1f - Mathf.Clamp01(attachSnapTimer / duration);
            progress = progress * progress * (3f - 2f * progress);
            target = Vector3.Lerp(attachSnapStartHandPosition, target, progress);
        }

        UpdateHandTargetVelocity(target);
        currentHandTargetPosition = target;
        hasCurrentHandTargetPosition = true;

        return target;
    }

    private Vector3 GetProjectedRopeEndOnPlayerPlane()
    {
        return new Vector3(projectedEnd.x, projectedEnd.y, playerPlaneZ);
    }

    private Vector3 GetSwingHandPositionFromAngle()
    {
        Vector3 pivot = GetPlayerPlanePoint(projectedStart);
        Vector3 offset = new Vector3(
            Mathf.Sin(swingAngle) * Mathf.Max(minSwingRadius, currentProjectedLength),
            -Mathf.Cos(swingAngle) * Mathf.Max(minSwingRadius, currentProjectedLength),
            0f
        );

        return pivot + offset;
    }

    private void UpdateHandTargetVelocity(Vector3 target)
    {
        if (hasPreviousHandTargetPosition && Time.fixedDeltaTime > 0f)
        {
            handTargetVelocity = (target - previousHandTargetPosition) / Time.fixedDeltaTime;
        }
        else
        {
            handTargetVelocity = Vector3.zero;
        }

        previousHandTargetPosition = target;
        hasPreviousHandTargetPosition = true;
    }

    private void UpdateReleaseDirection(float rawInput, bool boostHeld)
    {
        lastRawSwingInput = rawInput;
        lastBoostInputHeld = boostHeld;

        if (Mathf.Abs(rawInput) >= facingInputThreshold)
        {
            lastReleaseDirectionSign = Mathf.Sign(rawInput);
            return;
        }

        if (Mathf.Abs(angularVelocity) >= minAssistAngularSpeed)
        {
            lastReleaseDirectionSign = Mathf.Sign(angularVelocity);
        }
    }

    private Vector3 GetClosestProjectedRopePoint(Vector3 worldPoint)
    {
        Vector3 start = GetPointOnWorldZ(projectedStart, worldPoint.z);
        Vector3 end = GetPointOnWorldZ(projectedEnd, worldPoint.z);
        Vector3 rope = end - start;

        if (rope.sqrMagnitude < 0.0001f)
        {
            return start;
        }

        float amount = Vector3.Dot(worldPoint - start, rope) / rope.sqrMagnitude;
        amount = Mathf.Clamp01(amount);

        return Vector3.Lerp(start, end, amount);
    }

    private Vector3 GetPointOnWorldZ(Vector3 point, float z)
    {
        return new Vector3(point.x, point.y, z);
    }

    private Vector3 GetHandWorldPosition()
    {
        if (playerHandPoint != null)
        {
            return playerHandPoint.position;
        }

        if (playerVisual != null)
        {
            return playerVisual.TransformPoint(fallbackHandLocalOffset);
        }

        if (player != null)
        {
            return player.TransformPoint(fallbackHandLocalOffset);
        }

        return Vector3.zero;
    }

    private Vector3 GetPlayerPlanePoint(Vector3 worldPoint)
    {
        return new Vector3(worldPoint.x, worldPoint.y, playerPlaneZ);
    }

    private void MovePlayerHandTo(Vector3 handWorldPosition)
    {
        Vector3 currentHandOffset = attachedHandOffsetFromPlayer;

        if (player != null)
        {
            Vector3 liveHandOffset = GetHandWorldPosition() - player.position;

            if (liveHandOffset.sqrMagnitude > 0.0001f)
            {
                currentHandOffset = liveHandOffset;
                attachedHandOffsetFromPlayer = liveHandOffset;
            }
        }

        MovePlayerTo(handWorldPosition - currentHandOffset);
    }

    private void MovePlayerTo(Vector3 worldPosition)
    {
        if (playerRigidbody != null && playerRigidbody.isKinematic)
        {
            playerRigidbody.position = worldPosition;
        }

        if (player != null)
        {
            player.position = worldPosition;
        }
    }

    private void FaceVisualTowards(float targetX)
    {
        if (playerVisual == null || player == null)
        {
            return;
        }

        previousVisualScale = playerVisual.localScale;

        float sign = targetX >= player.position.x ? 1f : -1f;
        Vector3 scale = playerVisual.localScale;
        scale.x = Mathf.Abs(scale.x) * sign;
        playerVisual.localScale = scale;
    }

    private void BeginSwingPose()
    {
        if (playerAnimator != null)
        {
            previousAnimatorSpeed = playerAnimator.speed;
            previousAnimatorEnabled = playerAnimator.enabled;
        }

        if (playerSpriteRenderer != null)
        {
            previousSprite = playerSpriteRenderer.sprite;
        }

        usingManualSwingPose = swingPoseSprites != null && swingPoseSprites.Length > 0 && playerSpriteRenderer != null;

        SetAnimatorBool("isWalking", false);
        SetAnimatorBool("isRunning", false);
        SetAnimatorBool("isJumping", false);
        SetAnimatorBool("isClimbing", !usingManualSwingPose);

        if (usingManualSwingPose)
        {
            if (playerAnimator != null && disableAnimatorForSwingPose)
            {
                playerAnimator.enabled = false;
            }

            swingPoseIndex = 0;
            swingPoseTimer = 0f;
            playerSpriteRenderer.sprite = swingPoseSprites[0];
            hasSwingPoseFacing = false;
            facingSwitchCooldownTimer = 0f;
            UpdateSwingPoseFacing(GetSwingInput());
            return;
        }

        if (playerAnimator != null)
        {
            playerAnimator.speed = Mathf.Max(0f, attachedAnimatorSpeed);
        }

        PlayClimbAnimationState();
    }

    private void UpdateManualSwingPose(float deltaTime)
    {
        if (!usingManualSwingPose || playerSpriteRenderer == null || swingPoseSprites == null || swingPoseSprites.Length == 0)
        {
            return;
        }

        if (facingSwitchCooldownTimer > 0f)
        {
            facingSwitchCooldownTimer = Mathf.Max(0f, facingSwitchCooldownTimer - deltaTime);
        }

        UpdateSwingPoseFacing(GetSwingInput());

        if (swingPoseSprites.Length == 1 || swingPoseFrameRate <= 0f)
        {
            playerSpriteRenderer.sprite = swingPoseSprites[0];
            return;
        }

        swingPoseTimer += deltaTime;
        float frameDuration = 1f / swingPoseFrameRate;

        while (swingPoseTimer >= frameDuration)
        {
            swingPoseTimer -= frameDuration;
            swingPoseIndex = (swingPoseIndex + 1) % swingPoseSprites.Length;
            playerSpriteRenderer.sprite = swingPoseSprites[swingPoseIndex];
        }
    }

    private void UpdateSwingPoseFacing(float input)
    {
        if (!flipSwingPoseWithInput || playerVisual == null)
        {
            return;
        }

        if (!hasSwingPoseFacing)
        {
            currentSwingPoseFacingRight = GetInitialSwingPoseFacing(input);
            hasSwingPoseFacing = true;
            ApplySwingPoseFacing(currentSwingPoseFacingRight);
            return;
        }

        bool shouldFaceRight = currentSwingPoseFacingRight;
        bool hasExplicitInput = Mathf.Abs(input) >= facingInputThreshold;
        bool canSwitchFromMotion = facingSwitchCooldownTimer <= 0f;

        if (ShouldUseSettledFacing(input))
        {
            shouldFaceRight = settledFacingRight;
        }
        else if (input > facingInputThreshold)
        {
            shouldFaceRight = true;
        }
        else if (input < -facingInputThreshold)
        {
            shouldFaceRight = false;
        }
        else if (canSwitchFromMotion && Mathf.Abs(angularVelocity) > facingAngularSpeedThreshold)
        {
            shouldFaceRight = angularVelocity > 0f;
        }

        if (shouldFaceRight == currentSwingPoseFacingRight)
        {
            return;
        }

        if (!hasExplicitInput && !ShouldUseSettledFacing(input) && facingSwitchCooldownTimer > 0f)
        {
            return;
        }

        currentSwingPoseFacingRight = shouldFaceRight;
        facingSwitchCooldownTimer = Mathf.Max(0f, facingSwitchCooldown);
        ApplySwingPoseFacing(currentSwingPoseFacingRight);
    }

    private bool GetInitialSwingPoseFacing(float input)
    {
        if (input > facingInputThreshold)
        {
            return true;
        }

        if (input < -facingInputThreshold)
        {
            return false;
        }

        if (lockFacingWhenSwingSettled && Mathf.Abs(GetAngleFromRestRadians()) * Mathf.Rad2Deg <= settledFacingAngleRange)
        {
            return settledFacingRight;
        }

        if (player != null)
        {
            return projectedEnd.x >= player.position.x;
        }

        return swingPoseFacesRightByDefault;
    }

    private bool ShouldUseSettledFacing(float input)
    {
        if (!lockFacingWhenSwingSettled || Mathf.Abs(input) >= facingInputThreshold)
        {
            return false;
        }

        float angleFromRestDegrees = Mathf.Abs(GetAngleFromRestRadians()) * Mathf.Rad2Deg;

        return angleFromRestDegrees <= settledFacingAngleRange &&
               Mathf.Abs(angularVelocity) <= settledFacingAngularSpeed;
    }

    private void ApplySwingPoseFacing(bool shouldFaceRight)
    {
        float desiredSign = shouldFaceRight == swingPoseFacesRightByDefault ? 1f : -1f;
        Vector3 scale = playerVisual.localScale;
        scale.x = Mathf.Abs(scale.x) * desiredSign;
        playerVisual.localScale = scale;
    }

    private void RestoreSwingPose()
    {
        if (playerAnimator != null)
        {
            playerAnimator.enabled = previousAnimatorEnabled;
            playerAnimator.speed = previousAnimatorSpeed;
        }

        if (usingManualSwingPose && playerSpriteRenderer != null && previousSprite != null)
        {
            playerSpriteRenderer.sprite = previousSprite;
        }

        if (faceRopeOnAttach && playerVisual != null && previousVisualScale != Vector3.zero)
        {
            playerVisual.localScale = previousVisualScale;
        }

        usingManualSwingPose = false;
        hasSwingPoseFacing = false;
        facingSwitchCooldownTimer = 0f;
    }

    private void FinishSwing(bool applyReleaseVelocity)
    {
        Vector3 releaseVelocity = Vector3.zero;

        if (applyReleaseVelocity)
        {
            releaseVelocity = GetReleaseVelocity();
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = previousIsKinematic;
            playerRigidbody.useGravity = previousUseGravity;

            if (!playerRigidbody.isKinematic)
            {
                playerRigidbody.velocity = releaseVelocity;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
        }

        if (playerController != null)
        {
            playerController.enabled = previousPlayerControllerEnabled;

            if (applyReleaseVelocity && previousPlayerControllerEnabled)
            {
                playerController.ApplyExternalLaunch(releaseVelocity, releaseAirControlDelay);
            }
        }

        RestoreSwingPose();
        SetAnimatorBool("isClimbing", false);

        playerAttached = false;
        releaseRequested = false;
        candidatePlayerCollider = null;
        hasCurrentHandTargetPosition = false;
        handPinError = 0f;

        if (applyReleaseVelocity)
        {
            autoGrabCooldownTimer = Mathf.Max(0f, releaseAutoGrabCooldown);
            ropeSettlingAfterRelease = keepRopeSwingingAfterRelease && driveRopeEndWhileSwinging;
        }
        else
        {
            ropeSettlingAfterRelease = false;

            if (driveRopeEndWhileSwinging && hasRopeRestState && ropeEnd != null)
            {
                ropeEnd.position = ropeRestEndWorldPosition;
                UpdateGrabZone();
            }
        }
    }

    private Vector3 GetReleaseVelocity()
    {
        Vector3 tangent = new Vector3(
            Mathf.Cos(swingAngle),
            Mathf.Sin(swingAngle),
            0f
        ).normalized * Mathf.Sign(lastReleaseDirectionSign);

        Vector3 releaseVelocity = handTargetVelocity * releaseSpeedMultiplier;
        float tangentialSpeed = Vector3.Dot(releaseVelocity, tangent);
        float angleFromRestDegrees = Mathf.Abs(GetAngleFromRestRadians()) * Mathf.Rad2Deg;
        float apexAmount = Mathf.InverseLerp(25f, Mathf.Max(26f, maxSwingAngleFromRest), angleFromRestDegrees);
        float inputBoost = Mathf.Abs(lastRawSwingInput) >= facingInputThreshold ? releaseInputSpeedBoost : 0f;
        float shiftBoost = lastBoostInputHeld ? releaseShiftSpeedBoost : 0f;
        float baseTangentialSpeed = useManualReleaseSpeed
            ? manualReleaseTangentialSpeed
            : minReleaseTangentialSpeed;
        float targetTangentialSpeed = baseTangentialSpeed + inputBoost + shiftBoost + releaseApexSpeedBoost * apexAmount;

        if (tangentialSpeed < targetTangentialSpeed)
        {
            releaseVelocity += tangent * (targetTangentialSpeed - tangentialSpeed);
        }

        releaseVelocity.y += releaseUpwardBoost;

        if (releaseVelocity.magnitude > maxReleaseSpeed)
        {
            releaseVelocity = releaseVelocity.normalized * maxReleaseSpeed;
        }

        return releaseVelocity;
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

        playerAnimator.Play(climbStateName, 0, 0f);
        playerAnimator.Update(0f);
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
        if (!projectionValid)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(projectedStart, projectedEnd);
        Gizmos.DrawSphere(projectedStart, 0.08f);
        Gizmos.DrawSphere(projectedEnd, 0.08f);

        if (handInGrabRange)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(closestHandGrabPoint, 0.1f);
        }
    }
}
