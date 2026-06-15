using UnityEngine;

/// <summary>
/// Purpose: Lets the player grab and swing from a rope shadow.
/// Input: Rope endpoints, shadow screen, light direction, player input, and swing settings.
/// Output: The player attaches to the projected rope, swings, and releases with momentum.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class ShadowRopeSwingZone : MonoBehaviour
{
    [Header("Projection References")]
    // Real rope start point in the scene.
    public Transform ropeStart;
    // Real rope end point in the scene.
    public Transform ropeEnd;
    // Wall/screen where the rope shadow appears.
    public Transform shadowScreen;
    // Light that projects the rope shadow.
    public Light directionalLight;

    [Header("Grab Zone")]
    // Tag used to recognise the player.
    public string playerTag = "Player";
    // Width of the trigger area around the rope shadow.
    public float grabWidth = 0.45f;
    // Depth of the trigger area in 3D space.
    public float triggerDepth = 8f;
    // Small offset from the shadow screen to avoid overlap.
    public float shadowSurfaceOffset = -0.02f;
    // If true, the player can grab the rope automatically on entering the trigger.
    public bool autoGrabOnEnter = false;

    [Header("Player Hand Grab")]
    // Player root object used for moving the character while swinging.
    public Transform playerRoot;
    // Optional hand point used to check if the hand is close enough to the rope.
    public Transform playerHandPoint;
    // Backup hand position if no hand point is assigned.
    public Vector3 fallbackHandLocalOffset = new Vector3(0.22f, 0.65f, 0f);
    // Maximum distance from the hand to the rope before grabbing is allowed.
    public float handGrabDistance = 0.38f;
    // If true, grabbing checks hand distance instead of only trigger contact.
    public bool useHandDistanceGrab = true;
    // If true, player visual turns toward the rope when attaching.
    public bool faceRopeOnAttach = true;
    // If true, auto-grab only happens while the player is not grounded.
    public bool autoGrabOnlyWhenAirborne = true;
    // Short delay after release before auto-grab can happen again.
    public float releaseAutoGrabCooldown = 0.25f;

    [Header("Rope End Attachment")]
    // If true, the player's hand attaches to the projected rope end.
    public bool attachHandToProjectedRopeEnd = true;
    // If true, the visible rope end follows the swing motion.
    public bool driveRopeEndWhileSwinging = true;
    // Time used to smoothly snap the hand onto the rope.
    public float attachSnapDuration = 0.12f;
    // If true, the rope keeps moving after the player releases it.
    public bool keepRopeSwingingAfterRelease = true;
    // Angle close enough to rest for the rope to stop settling.
    public float settleAngleTolerance = 1f;
    // Swing speed low enough for the rope to stop settling.
    public float settleAngularSpeedTolerance = 0.08f;

    [Header("Hand Pinning")]
    // If true, the hand is corrected again in LateUpdate after animation updates.
    public bool pinHandInLateUpdate = true;
    // Small world offset for fine-tuning the hand attach position.
    public Vector3 handAttachmentWorldOffset = Vector3.zero;

    [Header("Wall Slide Pose")]
    // Sprite frames used while the player is attached to the rope.
    public Sprite[] swingPoseSprites;
    // How fast the swing pose sprite frames change.
    public float swingPoseFrameRate = 6f;
    // If true, the Animator is disabled while using manual swing sprites.
    public bool disableAnimatorForSwingPose = true;
    // If true, input can flip the swing pose left/right.
    public bool flipSwingPoseWithInput = true;
    // If true, the swing pose sprite originally faces right.
    public bool swingPoseFacesRightByDefault = false;
    // If true, facing direction becomes fixed when the rope is nearly still.
    public bool lockFacingWhenSwingSettled = true;
    // Facing direction used when the rope is settled.
    public bool settledFacingRight = true;
    // Angle range where the rope counts as settled for facing.
    public float settledFacingAngleRange = 8f;
    // Swing speed below this counts as settled for facing.
    public float settledFacingAngularSpeed = 0.25f;
    // Input must be stronger than this before facing can change.
    public float facingInputThreshold = 0.15f;
    // Swing speed must be stronger than this before motion can change facing.
    public float facingAngularSpeedThreshold = 0.45f;
    // Delay between facing-direction changes.
    public float facingSwitchCooldown = 0.12f;

    [Header("Swing Motion")]
    // Strength of gravity used for the rope swing.
    public float gravity = 13f;
    // How strongly A/D input pushes the swing.
    public float inputAngularAcceleration = 3.5f;
    // Overall strength of swing input.
    [Range(0.1f, 1f)] public float swingInputSensitivity = 0.78f;
    // Extra swing input multiplier while holding Shift.
    [Range(1f, 2f)] public float shiftSwingBoostMultiplier = 1.25f;
    // Extra help from Shift based on the swing angle.
    [Range(0f, 25f)] public float shiftAssistAngleBonus = 8f;
    // If true, input works better when timed with the current swing direction.
    public bool useMomentumBasedSwingInput = true;
    // Angle range near the bottom where the script helps the player build swing.
    [Range(0f, 45f)] public float bottomAssistAngleRange = 18f;
    // Angle range where normal swing input still gives useful help.
    [Range(5f, 85f)] public float inputAssistAngleRange = 52f;
    // Soft area that makes swing input fade instead of stopping suddenly.
    [Range(0f, 30f)] public float inputAssistSoftZone = 14f;
    // Minimum swing speed before momentum input is considered meaningful.
    public float minAssistAngularSpeed = 0.18f;
    // Small slowdown applied each physics step so the rope does not swing forever.
    [Range(0.8f, 1f)] public float angularDamping = 0.992f;
    // Maximum allowed swing speed.
    public float maxAngularSpeed = 5.5f;
    // Maximum angle away from the resting rope position.
    [Range(15f, 89f)] public float maxSwingAngleFromRest = 72f;
    // Area near the swing limit where braking begins gently.
    [Range(0f, 30f)] public float swingLimitSoftZone = 12f;
    // If true, the rope slows down before hitting the max swing angle.
    public bool useSoftSwingLimitBraking = true;
    // Strength of braking near the swing angle limit.
    public float softLimitBrakeStrength = 7f;
    // Strength that pushes the rope back from the angle limit.
    public float softLimitReturnStrength = 10f;
    // If true, pushing farther outward gets weaker near the swing limit.
    public bool fadeOutwardInputNearLimits = true;
    // Minimum rope length used for stable swing math.
    public float minSwingRadius = 0.55f;
    // Multiplier for velocity when the player releases the rope.
    public float releaseSpeedMultiplier = 1f;
    // Minimum sideways release speed.
    public float minReleaseTangentialSpeed = 3.2f;
    // If true, release speed uses a fixed manual value.
    public bool useManualReleaseSpeed = true;
    // Fixed sideways speed given when releasing.
    public float manualReleaseTangentialSpeed = 5.2f;
    // If true, release help depends on current swing angle and speed.
    public bool scaleReleaseAssistBySwingMotion = true;
    // Smallest angle where release help starts.
    public float releaseAssistMinAngle = 4f;
    // Angle where release help reaches full strength.
    public float releaseAssistFullAngle = 18f;
    // Smallest swing speed where release help starts.
    public float releaseAssistMinSpeed = 0.6f;
    // Swing speed where release help reaches full strength.
    public float releaseAssistFullSpeed = 3f;
    // How long normal air control waits after rope release.
    public float releaseAirControlDelay = 0.24f;
    // Extra release speed based on A/D input direction.
    public float releaseInputSpeedBoost = 0.8f;
    // Extra release speed while holding Shift.
    public float releaseShiftSpeedBoost = 0.8f;
    // If true, Shift release boost depends on current swing speed.
    public bool scaleShiftReleaseBoostByCurrentSpeed = true;
    // Swing speed where Shift release boost begins.
    public float shiftReleaseBoostMinSpeed = 2.5f;
    // Swing speed where Shift release boost reaches full strength.
    public float shiftReleaseBoostFullSpeed = 5f;
    // Extra speed near the top of the release arc.
    public float releaseApexSpeedBoost = 0.9f;
    // Small upward push added when releasing.
    public float releaseUpwardBoost = 0.4f;
    // Maximum total speed after releasing the rope.
    public float maxReleaseSpeed = 8f;

    [Header("Input")]
    // Main key for grabbing the rope.
    public KeyCode grabKey = KeyCode.W;
    // Backup key for grabbing the rope.
    public KeyCode alternateGrabKey = KeyCode.Space;
    // Main key for releasing the rope.
    public KeyCode releaseKey = KeyCode.Space;
    // Backup key for releasing the rope.
    public KeyCode alternateReleaseKey = KeyCode.S;
    // If true, the player must press a grab key instead of only auto-grabbing.
    public bool allowManualGrabInput = false;
    // Key for pushing swing left.
    public KeyCode leftKey = KeyCode.A;
    // Key for pushing swing right.
    public KeyCode rightKey = KeyCode.D;
    // Main key for stronger swing/release boost.
    public KeyCode boostKey = KeyCode.LeftShift;
    // Backup key for stronger swing/release boost.
    public KeyCode alternateBoostKey = KeyCode.RightShift;

    [Header("Animation")]
    // If true, forces the climb/hang animation state while attached.
    public bool forceClimbAnimationState = true;
    // Animator state name used while the player is attached.
    public string climbStateName = "Stickman_Climb";
    // Animator speed while attached; 0 can freeze the pose.
    public float attachedAnimatorSpeed = 0f;

    [Header("Swing Audio")]
    // AudioSource used to play the swing wind sound.
    public AudioSource swingAudioSource;
    // Wind sound played during swinging.
    public AudioClip swingWindSound;
    // Loudest wind volume.
    [Range(0f, 1f)] public float swingWindMaxVolume = 1f;
    // Quietest wind volume while sound is active.
    [Range(0f, 1f)] public float swingWindMinVolume = 0.55f;
    // Swing angle that makes wind volume reach maximum.
    public float swingWindFullAngle = 45f;
    // Swing speed that makes wind volume reach maximum.
    public float swingWindFullAngularSpeed = 3.2f;
    // How quickly the wind sound fades louder or quieter.
    public float swingWindFadeSpeed = 7f;
    // Lowest wind sound pitch.
    public float swingWindMinPitch = 0.85f;
    // Highest wind sound pitch.
    public float swingWindMaxPitch = 1.15f;
    // Start time inside the wind clip for looping.
    public float swingWindClipStartTime = 0.52f;
    // End time inside the wind clip for looping.
    public float swingWindClipEndTime = 1.05f;
    // Motion amount needed before wind sound starts.
    public float swingWindStartThreshold = 0.12f;
    // Motion amount below this makes wind sound stop.
    public float swingWindStopThreshold = 0.06f;

    [Header("Read Only Status")]
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
    private Transform initialRopeEndParent;
    private Vector3 initialRopeEndLocalPosition;
    private Vector3 initialRopeEndWorldPosition;
    private bool hasInitialRopeRestState;
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
    private bool swingAudioActive;
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
    private bool blockAutoGrabUntilPlayerLeavesRange;

    // Purpose: Sets up the trigger collider when the script is reset in Unity.
    // Input: BoxCollider on this GameObject.
    // Output: The rope grab zone is a trigger.
    private void Reset()
    {
        ConfigureCollider();
    }

    // Purpose: Sets up the trigger and remembers the rope's original rest position.
    // Input: Rope end and BoxCollider references.
    // Output: The rope zone is ready when play mode starts.
    private void Awake()
    {
        ConfigureCollider();
        CacheInitialRopeRestState();
    }

    // Purpose: Refreshes the rope rest position while editing.
    // Input: Current rope end position in the editor.
    // Output: Gizmo/trigger preview stays correct outside play mode.
    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            CacheInitialRopeRestState();
        }
    }

    // Purpose: Handles grab, release request, hand range checks, pose animation, and swing audio.
    // Input: Player input and current trigger/hand state.
    // Output: Player can attach to the rope or request release.
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
        UpdateAutoGrabReleaseBlock();
        UpdateSwingAudio(Time.deltaTime);
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

    // Purpose: Updates rope physics at a steady rate.
    // Input: Current swing state and release request.
    // Output: The rope swings, moves the player, or settles after release.
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

    // Purpose: Corrects the player's hand after animation updates.
    // Input: Current hand target position.
    // Output: The hand stays pinned to the rope more accurately.
    private void LateUpdate()
    {
        if (!Application.isPlaying || !pinHandInLateUpdate || !playerAttached || !hasCurrentHandTargetPosition)
        {
            return;
        }

        MovePlayerHandTo(currentHandTargetPosition);
        handPinError = Vector3.Distance(GetHandWorldPosition(), currentHandTargetPosition);
    }

    // Purpose: Detects the player entering the rope grab trigger.
    // Input: Trigger contact with the player.
    // Output: The player becomes a grab candidate or auto-attaches.
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

    // Purpose: Keeps track of the player while they stay inside the grab trigger.
    // Input: Ongoing trigger contact.
    // Output: The player remains available for rope grabbing.
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

    // Purpose: Detects the player leaving the rope grab trigger.
    // Input: Trigger exit.
    // Output: The player is no longer a grab candidate.
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

        if (!useHandDistanceGrab)
        {
            blockAutoGrabUntilPlayerLeavesRange = false;
        }
    }

    // Purpose: Cleans up if this rope object is disabled.
    // Input: Current attached/audio state.
    // Output: The player is released and swing audio stops.
    private void OnDisable()
    {
        if (Application.isPlaying && playerAttached)
        {
            FinishSwing(false);
        }

        StopSwingAudio();
    }

    /// <summary>
    /// Purpose: Forces the player to let go of the rope.
    /// Input: A call from another script, usually when the light phase changes.
    /// Output: The player detaches and cannot instantly regrab the same rope.
    /// </summary>
    public void ForceDropFromRope()
    {
        if (!Application.isPlaying || !playerAttached)
        {
            return;
        }

        FinishSwing(false);
        autoGrabCooldownTimer = Mathf.Max(autoGrabCooldownTimer, releaseAutoGrabCooldown);
        blockAutoGrabUntilPlayerLeavesRange = true;
    }

    // Purpose: Sets the BoxCollider as the rope grab trigger.
    // Input: BoxCollider on this GameObject.
    // Output: The collider detects the player without blocking movement.
    private void ConfigureCollider()
    {
        grabCollider = GetComponent<BoxCollider>();
        grabCollider.isTrigger = true;
        grabCollider.center = Vector3.zero;
    }

    // Purpose: Moves and resizes the grab trigger to match the projected rope shadow.
    // Input: Rope projection result, grab width, and trigger depth.
    // Output: The trigger sits over the visible rope shadow.
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

    // Purpose: Checks whether the player's hand is close enough to grab the rope.
    // Input: Hand position and projected rope position.
    // Output: Inspector status values show distance and grab range state.
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

    // Purpose: Projects the rope start and end onto the shadow screen.
    // Input: Rope endpoints, shadow screen, and light direction.
    // Output: Projected rope start/end points if projection succeeds.
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

    // Purpose: Projects one world point onto the shadow screen.
    // Input: World point, screen plane, and light direction.
    // Output: Projected point on the screen, if it hits.
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

    // Purpose: Checks if a collider belongs to the player.
    // Input: Collider entering or staying in the trigger.
    // Output: True means this collider is the player or part of the player.
    private bool IsPlayerCollider(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            return true;
        }

        return other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag);
    }

    // Purpose: Checks if the manual grab key was pressed.
    // Input: Grab key settings.
    // Output: True means the player requested a grab.
    private bool IsGrabPressed()
    {
        return allowManualGrabInput &&
               (Input.GetKeyDown(grabKey) || Input.GetKeyDown(alternateGrabKey));
    }

    // Purpose: Checks if the player pressed a release key.
    // Input: Release key settings.
    // Output: True means the player wants to let go.
    private bool IsReleasePressed()
    {
        return Input.GetKeyDown(releaseKey) || Input.GetKeyDown(alternateReleaseKey);
    }

    // Purpose: Decides if automatic grabbing is allowed right now.
    // Input: Auto-grab settings, cooldown, player grounded state, and optional player collider.
    // Output: True means the rope can attach automatically.
    private bool CanAutoGrab(Collider playerCollider = null)
    {
        if (!autoGrabOnEnter || autoGrabCooldownTimer > 0f || blockAutoGrabUntilPlayerLeavesRange)
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

    // Purpose: Keeps auto-grab blocked until the player leaves the rope range after release.
    // Input: Current hand/trigger range.
    // Output: Auto-grab becomes available again only after leaving range.
    private void UpdateAutoGrabReleaseBlock()
    {
        if (!blockAutoGrabUntilPlayerLeavesRange)
        {
            return;
        }

        if (useHandDistanceGrab)
        {
            if (!handInGrabRange)
            {
                blockAutoGrabUntilPlayerLeavesRange = false;
            }

            return;
        }

        if (candidatePlayerCollider == null)
        {
            blockAutoGrabUntilPlayerLeavesRange = false;
        }
    }

    // Purpose: Reads left/right swing input.
    // Input: A and D keys.
    // Output: -1 for left, 1 for right, 0 for no input.
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

    // Purpose: Checks whether the boost key is held.
    // Input: Left Shift or Right Shift.
    // Output: True means boost is active.
    private bool IsBoostHeld()
    {
        return Input.GetKey(boostKey) || Input.GetKey(alternateBoostKey);
    }

    // Purpose: Attaches the player to the rope and starts swing mode.
    // Input: Player collider or cached player reference.
    // Output: Normal player control is paused and rope swinging begins.
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
        blockAutoGrabUntilPlayerLeavesRange = false;
        playerPlaneZ = player.position.z;
        bool wasSettlingAfterRelease = ropeSettlingAfterRelease;
        float inheritedAngularVelocity = wasSettlingAfterRelease ? angularVelocity : 0f;
        ropeSettlingAfterRelease = false;

        if (!wasSettlingAfterRelease)
        {
            RestoreRopeEndToRestPose();
        }

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
        angularVelocity = inheritedAngularVelocity;
        attachSnapTimer = Mathf.Max(0f, attachSnapDuration);
        attachSnapStartHandPosition = handPosition;
        previousHandTargetPosition = handPosition;
        handTargetVelocity = Vector3.zero;
        hasPreviousHandTargetPosition = false;

        BeginSwingPose();
        DriveRopeEndFromSwingAngle();
        UpdateGrabZone();
        MovePlayerHandTo(GetCurrentSwingHandTarget());
        UpdateSwingAudio(0f);
    }

    // Purpose: Saves player components needed for rope swinging.
    // Input: Player collider.
    // Output: Player Transform, Rigidbody, PlayerController, Animator, visual, and SpriteRenderer are cached.
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

    // Purpose: Finds the player if this script does not already have a player reference.
    // Input: Player tag or assigned playerRoot.
    // Output: True means player and Rigidbody are available.
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

    // Purpose: Calculates one swing step.
    // Input: Player input, gravity, current angle, and current speed.
    // Output: Rope angle changes, rope end moves, and the player may move with it.
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
                RestoreRopeEndToRestPose();
                swingAngle = ropeRestAngle;
                angularVelocity = 0f;
                ropeSettlingAfterRelease = false;
                UpdateGrabZone();
            }
        }
    }

    // Purpose: Updates the wind sound while swinging.
    // Input: Swing angle, swing speed, and time passed.
    // Output: Wind sound volume and pitch match the swing motion.
    private void UpdateSwingAudio(float deltaTime)
    {
        if (!Application.isPlaying || swingWindSound == null || !playerAttached)
        {
            StopSwingAudio();
            return;
        }

        AudioSource audioSource = GetSwingAudioSource();
        if (audioSource == null)
        {
            return;
        }

        if (audioSource.clip != swingWindSound)
        {
            audioSource.clip = swingWindSound;
        }

        ConfigureSwingAudioSource(audioSource);

        float angleAmount = Mathf.InverseLerp(4f, Mathf.Max(4.01f, swingWindFullAngle), Mathf.Abs(GetAngleFromRestRadians()) * Mathf.Rad2Deg);
        float speedAmount = Mathf.InverseLerp(0.15f, Mathf.Max(0.16f, swingWindFullAngularSpeed), Mathf.Abs(angularVelocity));
        float motionAmount = Mathf.Clamp01(speedAmount * Mathf.Lerp(0.45f, 1f, angleAmount));
        float startThreshold = Mathf.Clamp01(swingWindStartThreshold);
        float stopThreshold = Mathf.Min(startThreshold, Mathf.Clamp01(swingWindStopThreshold));

        if (swingAudioActive)
        {
            swingAudioActive = motionAmount > stopThreshold;
        }
        else
        {
            swingAudioActive = motionAmount >= startThreshold;
        }

        float targetVolume = 0f;
        if (swingAudioActive)
        {
            EnsureSwingAudioPlaying(audioSource);
            UpdateSwingAudioLoopPoint(audioSource);

            float activeAmount = Mathf.InverseLerp(startThreshold, 1f, motionAmount);
            float curvedAmount = Mathf.Sqrt(Mathf.Clamp01(activeAmount));
            float maxVolume = Mathf.Max(swingWindMaxVolume, swingWindMinVolume);
            targetVolume = Mathf.Lerp(Mathf.Clamp01(swingWindMinVolume), Mathf.Clamp01(maxVolume), curvedAmount);
        }

        float fade = Mathf.Max(0.01f, swingWindFadeSpeed);

        audioSource.volume = deltaTime > 0f
            ? Mathf.MoveTowards(audioSource.volume, targetVolume, fade * deltaTime)
            : targetVolume;
        audioSource.pitch = Mathf.Lerp(swingWindMinPitch, swingWindMaxPitch, Mathf.Clamp01(motionAmount));

        if (!swingAudioActive && audioSource.isPlaying && audioSource.volume <= 0.001f)
        {
            audioSource.Stop();
        }
    }

    // Purpose: Starts or keeps the wind sound playing.
    // Input: AudioSource and wind clip timing.
    // Output: Wind audio plays from the loop start point.
    private void EnsureSwingAudioPlaying(AudioSource audioSource)
    {
        if (swingWindSound.loadState == AudioDataLoadState.Unloaded)
        {
            swingWindSound.LoadAudioData();
        }

        float startTime = GetClampedSwingAudioStartTime();

        if (!audioSource.isPlaying)
        {
            audioSource.volume = 0f;
            audioSource.time = startTime;
            audioSource.Play();
            return;
        }

        if (audioSource.time < startTime || audioSource.time >= GetClampedSwingAudioEndTime())
        {
            audioSource.time = startTime;
        }
    }

    // Purpose: Keeps the wind sound inside the chosen loop range.
    // Input: AudioSource playback time.
    // Output: Playback jumps back to the loop start when it reaches the loop end.
    private void UpdateSwingAudioLoopPoint(AudioSource audioSource)
    {
        if (!audioSource.isPlaying)
        {
            return;
        }

        float startTime = GetClampedSwingAudioStartTime();
        float endTime = GetClampedSwingAudioEndTime();
        if (audioSource.time < startTime || audioSource.time >= endTime)
        {
            audioSource.time = startTime;
        }
    }

    // Purpose: Gets a safe loop start time for the wind clip.
    // Input: Wind clip length and start-time setting.
    // Output: A valid start time inside the clip.
    private float GetClampedSwingAudioStartTime()
    {
        if (swingWindSound == null)
        {
            return 0f;
        }

        return Mathf.Clamp(swingWindClipStartTime, 0f, Mathf.Max(0f, swingWindSound.length - 0.05f));
    }

    // Purpose: Gets a safe loop end time for the wind clip.
    // Input: Wind clip length, start time, and end-time setting.
    // Output: A valid end time after the start time.
    private float GetClampedSwingAudioEndTime()
    {
        if (swingWindSound == null)
        {
            return 0f;
        }

        float startTime = GetClampedSwingAudioStartTime();
        return Mathf.Clamp(swingWindClipEndTime, startTime + 0.05f, swingWindSound.length);
    }

    // Purpose: Gets or creates the AudioSource for swing wind sound.
    // Input: Optional assigned AudioSource and wind sound clip.
    // Output: An AudioSource ready for swing audio.
    private AudioSource GetSwingAudioSource()
    {
        if (swingAudioSource != null)
        {
            return swingAudioSource;
        }

        if (!Application.isPlaying || swingWindSound == null)
        {
            return null;
        }

        swingAudioSource = gameObject.AddComponent<AudioSource>();
        ConfigureSwingAudioSource(swingAudioSource);
        return swingAudioSource;
    }

    // Purpose: Sets basic AudioSource options for swing wind sound.
    // Input: AudioSource to configure.
    // Output: Audio plays as a clean 2D gameplay sound.
    private void ConfigureSwingAudioSource(AudioSource audioSource)
    {
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.mute = false;
        audioSource.priority = 64;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.ignoreListenerPause = true;
    }

    // Purpose: Stops the swing wind sound.
    // Input: Current swing AudioSource.
    // Output: Wind sound is stopped and volume resets.
    private void StopSwingAudio()
    {
        if (swingAudioSource == null)
        {
            return;
        }

        if (swingAudioSource.isPlaying)
        {
            swingAudioSource.Stop();
        }

        swingAudioSource.volume = 0f;
        swingAudioActive = false;
    }

    // Purpose: Finds how far the rope is from its resting angle.
    // Input: Rest angle and current swing angle.
    // Output: Angle difference used by the swing math.
    private float GetAngleFromRestRadians()
    {
        return Mathf.DeltaAngle(
            ropeRestAngle * Mathf.Rad2Deg,
            swingAngle * Mathf.Rad2Deg
        ) * Mathf.Deg2Rad;
    }

    // Purpose: Makes swing input depend on timing and momentum.
    // Input: Raw left/right input, current angle, and boost state.
    // Output: Adjusted input that rewards pushing with the swing.
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

    // Purpose: Adds braking near the maximum swing angle.
    // Input: Current angle away from rest.
    // Output: Extra acceleration that slows and pulls the rope back.
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

    // Purpose: Weakens input that tries to push past the swing limit.
    // Input: Swing input and current angle.
    // Output: Safer input near the maximum swing angle.
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

    // Purpose: Stops the rope from going past the maximum swing angle.
    // Input: Current swing angle and speed.
    // Output: Angle is clamped and outward speed may be stopped.
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

    // Purpose: Saves the rope's current rest position for swing math.
    // Input: Rope start and rope end positions.
    // Output: Rest angle, rest offset, and swing radius are stored.
    private void CaptureRopeRestState()
    {
        if (ropeStart == null || ropeEnd == null)
        {
            hasRopeRestState = false;
            return;
        }

        if (!hasInitialRopeRestState)
        {
            CacheInitialRopeRestState();
        }

        ropeRestEndWorldPosition = hasInitialRopeRestState
            ? GetInitialRopeEndWorldPosition()
            : ropeEnd.position;
        ropeRestEndOffset = ropeRestEndWorldPosition - ropeStart.position;
        ropeRestZOffset = ropeRestEndOffset.z;

        float planarLength = new Vector2(ropeRestEndOffset.x, ropeRestEndOffset.y).magnitude;
        swingRadius = Mathf.Max(minSwingRadius, planarLength);
        ropeRestAngle = Mathf.Atan2(ropeRestEndOffset.x, -ropeRestEndOffset.y);
        hasRopeRestState = true;
    }

    // Purpose: Remembers the rope end's original position from the editor.
    // Input: Rope end Transform.
    // Output: The rope can return to its original rest pose later.
    private void CacheInitialRopeRestState()
    {
        if (ropeEnd == null)
        {
            hasInitialRopeRestState = false;
            return;
        }

        initialRopeEndParent = ropeEnd.parent;
        initialRopeEndLocalPosition = ropeEnd.localPosition;
        initialRopeEndWorldPosition = ropeEnd.position;
        hasInitialRopeRestState = true;
    }

    // Purpose: Gets the original rope end position in world space.
    // Input: Saved local/world rope end data.
    // Output: Original rope end world position.
    private Vector3 GetInitialRopeEndWorldPosition()
    {
        if (!hasInitialRopeRestState)
        {
            return ropeEnd != null ? ropeEnd.position : Vector3.zero;
        }

        if (ropeEnd != null && ropeEnd.parent == initialRopeEndParent)
        {
            return initialRopeEndParent != null
                ? initialRopeEndParent.TransformPoint(initialRopeEndLocalPosition)
                : initialRopeEndLocalPosition;
        }

        return initialRopeEndWorldPosition;
    }

    // Purpose: Puts the rope end back to its original rest pose.
    // Input: Saved rope end position.
    // Output: Visible rope returns to its starting position.
    private void RestoreRopeEndToRestPose()
    {
        if (!driveRopeEndWhileSwinging || ropeEnd == null)
        {
            return;
        }

        if (!hasInitialRopeRestState)
        {
            CacheInitialRopeRestState();
        }

        if (!hasInitialRopeRestState)
        {
            return;
        }

        if (ropeEnd.parent == initialRopeEndParent)
        {
            ropeEnd.localPosition = initialRopeEndLocalPosition;
        }
        else
        {
            ropeEnd.position = initialRopeEndWorldPosition;
        }
    }

    // Purpose: Moves the visible rope end based on the current swing angle.
    // Input: Rope pivot, swing radius, and swing angle.
    // Output: Rope end follows the pendulum swing.
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

    // Purpose: Finds where the player's hand should be right now.
    // Input: Projected rope end, swing angle, snap timer, and hand offset.
    // Output: Target hand position for this frame.
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

    // Purpose: Gets the projected rope end on the player's Z plane.
    // Input: Projected rope end and player depth.
    // Output: A hand target position aligned with the player.
    private Vector3 GetProjectedRopeEndOnPlayerPlane()
    {
        return new Vector3(projectedEnd.x, projectedEnd.y, playerPlaneZ);
    }

    // Purpose: Calculates hand position from swing angle instead of projected rope end.
    // Input: Projected rope start, swing angle, and rope length.
    // Output: Hand target position along the swing arc.
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

    // Purpose: Calculates how fast the hand target is moving.
    // Input: Current and previous hand target positions.
    // Output: Hand target velocity used for release speed.
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

    // Purpose: Remembers which direction the player likely wants to release.
    // Input: Current swing input and boost state.
    // Output: Last release direction and boost state are stored.
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

    // Purpose: Finds the closest point on the projected rope to a world point.
    // Input: A world point, usually the player's hand.
    // Output: Closest point on the rope shadow.
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

    // Purpose: Copies a point but forces it onto a chosen Z depth.
    // Input: World point and target Z value.
    // Output: Same X/Y with the requested Z.
    private Vector3 GetPointOnWorldZ(Vector3 point, float z)
    {
        return new Vector3(point.x, point.y, z);
    }

    // Purpose: Gets the player's hand position.
    // Input: Hand point, player visual, or fallback hand offset.
    // Output: Best available world position for the hand.
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

    // Purpose: Moves a point onto the player's Z plane.
    // Input: World point.
    // Output: Same X/Y at the player's depth.
    private Vector3 GetPlayerPlanePoint(Vector3 worldPoint)
    {
        return new Vector3(worldPoint.x, worldPoint.y, playerPlaneZ);
    }

    // Purpose: Moves the player so their hand reaches the rope target.
    // Input: Desired hand world position.
    // Output: Player body moves while keeping the hand offset.
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

    // Purpose: Moves the player to a world position.
    // Input: Target world position.
    // Output: Player Transform and Rigidbody position are updated.
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

    // Purpose: Turns the player visual toward a target X position.
    // Input: Target X position, usually the rope.
    // Output: Player visual faces the rope.
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

    // Purpose: Starts the hanging/swing pose.
    // Input: Animator, SpriteRenderer, and swing pose settings.
    // Output: Player animation changes to a rope-hanging pose.
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

    // Purpose: Updates manual swing sprites while attached.
    // Input: Time passed and swing pose frames.
    // Output: Player sprite changes frame and facing direction.
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

    // Purpose: Updates which way the swing pose faces.
    // Input: Player input and rope motion.
    // Output: Player visual flips left or right when needed.
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

    // Purpose: Chooses the first facing direction when swing pose begins.
    // Input: Player input, rope angle, and player position.
    // Output: True means face right; false means face left.
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

    // Purpose: Checks if settled-rope facing should be used.
    // Input: Player input, rope angle, and swing speed.
    // Output: True means use the fixed settled facing direction.
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

    // Purpose: Applies the chosen facing direction to the player visual.
    // Input: True for facing right, false for facing left.
    // Output: Player visual scale flips if needed.
    private void ApplySwingPoseFacing(bool shouldFaceRight)
    {
        float desiredSign = shouldFaceRight == swingPoseFacesRightByDefault ? 1f : -1f;
        Vector3 scale = playerVisual.localScale;
        scale.x = Mathf.Abs(scale.x) * desiredSign;
        playerVisual.localScale = scale;
    }

    // Purpose: Restores the player's normal pose/animation after swinging.
    // Input: Saved Animator, sprite, and visual scale values.
    // Output: Player visuals return to normal.
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

    // Purpose: Ends rope swinging.
    // Input: Whether to give the player release velocity.
    // Output: Player control is restored and the rope may keep settling.
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
        StopSwingAudio();

        playerAttached = false;
        releaseRequested = false;
        candidatePlayerCollider = null;
        hasCurrentHandTargetPosition = false;
        handPinError = 0f;

        if (applyReleaseVelocity)
        {
            autoGrabCooldownTimer = Mathf.Max(0f, releaseAutoGrabCooldown);
            blockAutoGrabUntilPlayerLeavesRange = true;
            ropeSettlingAfterRelease = keepRopeSwingingAfterRelease && driveRopeEndWhileSwinging;
        }
        else
        {
            ropeSettlingAfterRelease = false;

            if (driveRopeEndWhileSwinging && hasRopeRestState && ropeEnd != null)
            {
                RestoreRopeEndToRestPose();
                UpdateGrabZone();
            }
        }
    }

    // Purpose: Calculates the velocity the player gets when releasing.
    // Input: Swing direction, hand target speed, input boost, and release settings.
    // Output: A launch velocity for the player.
    private Vector3 GetReleaseVelocity()
    {
        Vector3 baseTangent = new Vector3(
            Mathf.Cos(swingAngle),
            Mathf.Sin(swingAngle),
            0f
        ).normalized;

        Vector3 releaseVelocity = handTargetVelocity * releaseSpeedMultiplier;
        float signedTangentialSpeed = Vector3.Dot(releaseVelocity, baseTangent);
        float releaseDirectionSign = GetReleaseDirectionSign(signedTangentialSpeed);
        Vector3 tangent = baseTangent * releaseDirectionSign;
        float tangentialSpeed = Vector3.Dot(releaseVelocity, tangent);
        float angleFromRestDegrees = Mathf.Abs(GetAngleFromRestRadians()) * Mathf.Rad2Deg;
        float releaseAssistAmount = GetReleaseAssistAmount(Mathf.Abs(signedTangentialSpeed), angleFromRestDegrees);
        float apexAmount = Mathf.InverseLerp(25f, Mathf.Max(26f, maxSwingAngleFromRest), angleFromRestDegrees);
        float inputBoost = Mathf.Abs(lastRawSwingInput) >= facingInputThreshold ? releaseInputSpeedBoost : 0f;
        float shiftBoost = GetShiftReleaseBoost(Mathf.Abs(tangentialSpeed));
        float baseTangentialSpeed = useManualReleaseSpeed
            ? manualReleaseTangentialSpeed
            : minReleaseTangentialSpeed;
        float targetTangentialSpeed = (
            baseTangentialSpeed +
            inputBoost +
            shiftBoost +
            releaseApexSpeedBoost * apexAmount
        ) * releaseAssistAmount;

        if (tangentialSpeed < targetTangentialSpeed)
        {
            releaseVelocity += tangent * (targetTangentialSpeed - tangentialSpeed);
        }

        releaseVelocity.y += releaseUpwardBoost * releaseAssistAmount;

        if (releaseVelocity.magnitude > maxReleaseSpeed)
        {
            releaseVelocity = releaseVelocity.normalized * maxReleaseSpeed;
        }

        return releaseVelocity;
    }

    // Purpose: Decides which direction the release should go.
    // Input: Player input, rope speed, and current sideways release speed.
    // Output: -1 or 1 release direction.
    private float GetReleaseDirectionSign(float signedTangentialSpeed)
    {
        if (Mathf.Abs(lastRawSwingInput) >= facingInputThreshold)
        {
            return Mathf.Sign(lastRawSwingInput);
        }

        if (Mathf.Abs(angularVelocity) >= minAssistAngularSpeed)
        {
            return Mathf.Sign(angularVelocity);
        }

        if (Mathf.Abs(signedTangentialSpeed) > 0.05f)
        {
            return Mathf.Sign(signedTangentialSpeed);
        }

        return Mathf.Sign(lastReleaseDirectionSign);
    }

    // Purpose: Decides how much release assistance should apply.
    // Input: Current swing speed and angle.
    // Output: A value from 0 to 1 for release boost strength.
    private float GetReleaseAssistAmount(float currentTangentialSpeed, float angleFromRestDegrees)
    {
        if (!scaleReleaseAssistBySwingMotion)
        {
            return 1f;
        }

        float fullAngle = Mathf.Max(0.01f, releaseAssistFullAngle);
        float minAngle = Mathf.Clamp(releaseAssistMinAngle, 0f, fullAngle - 0.01f);
        float angleAmount = Mathf.InverseLerp(minAngle, fullAngle, angleFromRestDegrees);

        float fullSpeed = Mathf.Max(0.01f, releaseAssistFullSpeed);
        float minSpeed = Mathf.Clamp(releaseAssistMinSpeed, 0f, fullSpeed - 0.01f);
        float speedAmount = Mathf.InverseLerp(minSpeed, fullSpeed, currentTangentialSpeed);

        return Mathf.Max(angleAmount, speedAmount);
    }

    // Purpose: Calculates extra release speed from holding Shift.
    // Input: Current sideways release speed and Shift state.
    // Output: Extra release speed amount.
    private float GetShiftReleaseBoost(float currentTangentialSpeed)
    {
        if (!lastBoostInputHeld || releaseShiftSpeedBoost <= 0f)
        {
            return 0f;
        }

        if (!scaleShiftReleaseBoostByCurrentSpeed)
        {
            return releaseShiftSpeedBoost;
        }

        float fullSpeed = Mathf.Max(0.01f, shiftReleaseBoostFullSpeed);
        float minSpeed = Mathf.Clamp(shiftReleaseBoostMinSpeed, 0f, fullSpeed - 0.01f);
        float speedAmount = Mathf.InverseLerp(minSpeed, fullSpeed, currentTangentialSpeed);

        return releaseShiftSpeedBoost * speedAmount;
    }

    // Purpose: Sets an Animator bool safely.
    // Input: Animator parameter name and true/false value.
    // Output: The value changes only if that bool exists.
    private void SetAnimatorBool(string parameterName, bool value)
    {
        if (playerAnimator == null || !HasAnimatorBool(parameterName))
        {
            return;
        }

        playerAnimator.SetBool(parameterName, value);
    }

    // Purpose: Plays the climb/hang animation state directly.
    // Input: Animator and state name.
    // Output: Player shows the rope hanging pose.
    private void PlayClimbAnimationState()
    {
        if (!forceClimbAnimationState || playerAnimator == null || string.IsNullOrEmpty(climbStateName))
        {
            return;
        }

        playerAnimator.Play(climbStateName, 0, 0f);
        playerAnimator.Update(0f);
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

    // Purpose: Draws rope projection helper lines in the Scene view.
    // Input: Projected rope points and hand range state.
    // Output: Gizmos help debug where the rope grab area is.
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
