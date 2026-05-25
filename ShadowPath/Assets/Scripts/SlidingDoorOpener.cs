using System.Collections;
using UnityEngine;

/// <summary>
/// Purpose: Opens a sliding door when the player enters a trigger area.
/// Input: Player trigger contact and configured moving door parts.
/// Output: Moves the selected door parts from their closed local positions to open local positions.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class SlidingDoorOpener : MonoBehaviour
{
    [Header("Moving Parts")]
    public Transform[] movingParts;
    public Vector3 localOpenOffset = new Vector3(1.5f, 0f, 0f);

    [Header("Open Motion")]
    public float openDuration = 0.8f;
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool openOnlyOnce = true;

    [Header("Reveal Visual")]
    public GameObject revealObject;
    public Renderer revealRenderer;
    public Color revealColor = Color.black;
    [Range(0f, 1f)] public float revealMaxAlpha = 1f;
    public bool fadeRevealWithDoor = true;

    [Header("Entry Sequence")]
    public bool runEntrySequenceAfterOpen = false;
    public Transform entryTarget;
    public bool useEntryTargetXOnly = true;
    public float waitAfterOpen = 0.15f;
    public bool waitForGroundBeforeEntry = true;
    public float maxWaitForGroundTime = 1.5f;
    public float entryWalkDuration = 1f;
    public AnimationCurve entryWalkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool fadePlayerDuringEntry = false;
    [Range(0f, 1f)] public float fadePlayerAfterProgress = 0.25f;
    public float waitBeforeClose = 0.2f;
    public bool closeDoorAfterEntry = true;
    public float closeDuration = 0.8f;
    public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public GameObject playerVisualRoot;

    [Header("Entry Occluder")]
    public GameObject entryOccluderObject;
    public Renderer entryOccluderRenderer;
    public Color entryOccluderColor = Color.black;
    public bool hideEntryOccluderUntilEntry = true;

    [Header("Camera Return")]
    public bool returnCameraToOverviewAfterClose = true;
    public TutorialCameraController tutorialCameraController;

    [Header("Input Lock References")]
    public PlayerController playerController;
    public LightAngleController lightAngleController;
    public CameraLocalWashLightController localWashLightController;
    public Rigidbody playerRigidbody;
    public LayerMask entryGroundLayer = 1 << 3;
    public float entryGroundCheckDistance = 0.35f;

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool use2DProximityFallback = true;
    public Transform player;

    private BoxCollider triggerCollider;
    private Vector3[] closedLocalPositions;
    private Vector3[] closedWorldPositions;
    private Material revealMaterial;
    private Material entryOccluderMaterial;
    private Renderer[] playerFadeRenderers;
    private Material[] playerFadeMaterials;
    private Color[] playerFadeOriginalColors;
    private bool[] playerFadeUsesSpriteColor;
    private Animator playerAnimator;
    private Coroutine openRoutine;
    private Coroutine entrySequenceRoutine;
    private bool hasOpened;
    private bool hasStartedEntrySequence;

    /// <summary>
    /// Purpose: Prepares the trigger collider and remembers each moving part's closed local position.
    /// Input: Inspector-assigned moving parts.
    /// Output: Door is ready to open from its current placed position.
    /// </summary>
    void Awake()
    {
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        CacheClosedPositions();
        PrepareRevealVisual();
        PrepareEntryOccluderVisual();
    }

    /// <summary>
    /// Purpose: Provides a 2.5D fallback for doors placed at a different Z depth from the player.
    /// Input: Player world position and this trigger's local X/Y rectangle.
    /// Output: Opens the door when the player visually enters the trigger area.
    /// </summary>
    void Update()
    {
        if (!use2DProximityFallback || (openOnlyOnce && hasOpened))
        {
            return;
        }

        if (player == null)
        {
            FindPlayer();
        }

        if (player == null || triggerCollider == null)
        {
            return;
        }

        if (IsPlayerInsideTriggerXY(player.position))
        {
            Open();
        }
    }

    /// <summary>
    /// Purpose: Starts opening when the player reaches the door trigger.
    /// Input: Trigger collider contact.
    /// Output: Door opening animation starts once the player is close enough.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        Open();
    }

    /// <summary>
    /// Purpose: Opens the door even if the player was already overlapping when play mode started.
    /// Input: Trigger collider contact.
    /// Output: Door opening animation starts while the player remains in range.
    /// </summary>
    void OnTriggerStay(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        Open();
    }

    /// <summary>
    /// Purpose: Opens the door from code or UI events.
    /// Input: Current door state.
    /// Output: Starts or restarts the open animation.
    /// </summary>
    public void Open()
    {
        if (openOnlyOnce && hasOpened)
        {
            return;
        }

        if (movingParts == null || movingParts.Length == 0)
        {
            Debug.LogWarning("SlidingDoorOpener has no moving parts assigned.", this);
            return;
        }

        if (closedLocalPositions == null || closedLocalPositions.Length != movingParts.Length)
        {
            CacheClosedPositions();
        }

        if (runEntrySequenceAfterOpen && !hasStartedEntrySequence)
        {
            LockGameplayInput();
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(OpenDoorRoutine());
    }

    /// <summary>
    /// Purpose: Animates all moving parts from closed to open local positions.
    /// Input: Cached closed positions, configured offset, duration, and easing curve.
    /// Output: Door parts slide open smoothly.
    /// </summary>
    IEnumerator OpenDoorRoutine()
    {
        hasOpened = true;

        float elapsedTime = 0f;
        float safeDuration = Mathf.Max(0.01f, openDuration);

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / safeDuration);
            float curvedProgress = openCurve.Evaluate(progress);

            ApplyOpenProgress(curvedProgress);

            yield return null;
        }

        ApplyOpenProgress(1f);
        openRoutine = null;

        if (runEntrySequenceAfterOpen && !hasStartedEntrySequence)
        {
            entrySequenceRoutine = StartCoroutine(EntrySequenceRoutine());
        }
    }

    /// <summary>
    /// Purpose: Runs the post-open doorway sequence.
    /// Input: Player, target point, input components, visual renderers, and close settings.
    /// Output: Input is locked, player walks into the doorway, fades out, and the door closes.
    /// </summary>
    IEnumerator EntrySequenceRoutine()
    {
        hasStartedEntrySequence = true;

        LockGameplayInput();
        CachePlayerFadeRenderers();

        if (waitAfterOpen > 0f)
        {
            yield return new WaitForSeconds(waitAfterOpen);
        }

        if (waitForGroundBeforeEntry)
        {
            yield return WaitForPlayerGroundedRoutine();
        }

        FreezePlayerForScriptedEntry();
        SetEntryOccluderActive(true);
        yield return MovePlayerIntoDoorRoutine();

        if (waitBeforeClose > 0f)
        {
            yield return new WaitForSeconds(waitBeforeClose);
        }

        if (closeDoorAfterEntry)
        {
            yield return CloseDoorRoutine();
        }

        if (returnCameraToOverviewAfterClose)
        {
            ReturnCameraToOverview();
        }

        entrySequenceRoutine = null;
    }

    /// <summary>
    /// Purpose: Prevents manual control during the doorway exit sequence.
    /// Input: Assigned or auto-found gameplay input components.
    /// Output: Player and light controls are disabled, and player velocity is cleared.
    /// </summary>
    void LockGameplayInput()
    {
        CachePlayerReferences();

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (lightAngleController != null)
        {
            lightAngleController.enabled = false;
        }

        if (localWashLightController != null)
        {
            localWashLightController.acceptInput = false;
        }

        if (playerRigidbody != null)
        {
            Vector3 velocity = playerRigidbody.velocity;
            velocity.x = 0f;
            velocity.z = 0f;
            playerRigidbody.velocity = velocity;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        SetPlayerIdleAnimation();
    }

    /// <summary>
    /// Purpose: Moves the player into the black doorway area while fading the visual out.
    /// Input: Player position, entry target, entry timing, and fade timing.
    /// Output: Player reaches the doorway target and becomes invisible.
    /// </summary>
    IEnumerator MovePlayerIntoDoorRoutine()
    {
        CachePlayerReferences();

        if (player == null)
        {
            yield break;
        }

        Vector3 startPosition = player.position;
        Vector3 targetPosition = GetEntryTargetPosition(startPosition);
        float elapsedTime = 0f;
        float safeDuration = Mathf.Max(0.01f, entryWalkDuration);

        SetPlayerWalkingAnimation(true);

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / safeDuration);
            float curvedProgress = entryWalkCurve.Evaluate(progress);

            MovePlayerTo(Vector3.Lerp(startPosition, targetPosition, curvedProgress));

            if (fadePlayerDuringEntry)
            {
                SetPlayerFadeByEntryProgress(progress);
            }

            yield return null;
        }

        MovePlayerTo(targetPosition);
        SetPlayerWalkingAnimation(false);
        SetPlayerAlpha(0f);
        HidePlayerVisual();
    }

    /// <summary>
    /// Purpose: Closes the sliding door back to its starting position.
    /// Input: Close timing and easing curve.
    /// Output: Door moving parts return to their closed positions.
    /// </summary>
    IEnumerator CloseDoorRoutine()
    {
        float elapsedTime = 0f;
        float safeDuration = Mathf.Max(0.01f, closeDuration);

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / safeDuration);
            float curvedProgress = closeCurve.Evaluate(progress);

            ApplyOpenProgress(1f - curvedProgress);

            yield return null;
        }

        ApplyOpenProgress(0f);
    }

    /// <summary>
    /// Purpose: Applies one frame of the opening motion.
    /// Input: Normalized open progress from 0 to 1.
    /// Output: Updates each moving part's local position.
    /// </summary>
    void ApplyOpenProgress(float progress)
    {
        Vector3 worldOpenOffset = transform.TransformVector(localOpenOffset) * progress;

        for (int i = 0; i < movingParts.Length; i++)
        {
            if (movingParts[i] == null)
            {
                continue;
            }

            Vector3 targetWorldPosition = closedWorldPositions[i] + worldOpenOffset;

            if (movingParts[i].parent != null)
            {
                movingParts[i].localPosition =
                    movingParts[i].parent.InverseTransformPoint(targetWorldPosition);
            }
            else
            {
                movingParts[i].position = targetWorldPosition;
            }
        }

        if (fadeRevealWithDoor)
        {
            SetRevealProgress(progress);
        }
    }

    /// <summary>
    /// Purpose: Finds player-related references when they are not assigned in the Inspector.
    /// Input: Player transform and configured player tag.
    /// Output: Cached movement, physics, and input-lock references.
    /// </summary>
    void CachePlayerReferences()
    {
        if (player == null)
        {
            FindPlayer();
        }

        if (player == null)
        {
            return;
        }

        if (playerController == null)
        {
            playerController = player.GetComponent<PlayerController>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
        }

        if (playerVisualRoot == null)
        {
            Transform visual = player.Find("StickmanVisual");
            playerVisualRoot = visual != null ? visual.gameObject : player.gameObject;
        }

        if (playerAnimator == null && playerController != null)
        {
            playerAnimator = playerController.animator;
        }

        if (playerAnimator == null && playerVisualRoot != null)
        {
            playerAnimator = playerVisualRoot.GetComponentInChildren<Animator>(true);
        }

        if (playerAnimator == null)
        {
            playerAnimator = player.GetComponentInChildren<Animator>(true);
        }

        if (lightAngleController == null)
        {
            lightAngleController = FindObjectOfType<LightAngleController>();
        }

        if (localWashLightController == null)
        {
            localWashLightController = FindObjectOfType<CameraLocalWashLightController>();
        }

        if (tutorialCameraController == null)
        {
            tutorialCameraController = FindObjectOfType<TutorialCameraController>();
        }
    }

    /// <summary>
    /// Purpose: Returns the tutorial camera to its opening wide shot once the door sequence finishes.
    /// Input: TutorialCameraController reference or scene lookup fallback.
    /// Output: Camera smoothly transitions back to the overview composition.
    /// </summary>
    void ReturnCameraToOverview()
    {
        if (tutorialCameraController == null)
        {
            tutorialCameraController = FindObjectOfType<TutorialCameraController>();
        }

        if (tutorialCameraController != null)
        {
            tutorialCameraController.ReturnToOverview();
        }
    }

    /// <summary>
    /// Purpose: Resolves where the player should walk during the doorway sequence.
    /// Input: Current player position and optional entry target.
    /// Output: World-space target position that keeps player depth unless a target is assigned.
    /// </summary>
    Vector3 GetEntryTargetPosition(Vector3 startPosition)
    {
        if (entryTarget != null)
        {
            if (!useEntryTargetXOnly)
            {
                return entryTarget.position;
            }

            Vector3 target = startPosition;
            target.x = entryTarget.position.x;
            return target;
        }

        if (revealObject != null)
        {
            Vector3 target = startPosition;
            target.x = revealObject.transform.position.x;
            target.y = startPosition.y;
            return target;
        }

        return startPosition;
    }

    /// <summary>
    /// Purpose: Holds the player in an idle pose while the door opens.
    /// Input: Cached Animator.
    /// Output: Walking, running, and jumping animation states are cleared.
    /// </summary>
    void SetPlayerIdleAnimation()
    {
        if (playerAnimator == null)
        {
            return;
        }

        SetAnimatorBoolIfPresent("isWalking", false);
        SetAnimatorBoolIfPresent("isRunning", false);
        SetAnimatorBoolIfPresent("isJumping", false);
    }

    /// <summary>
    /// Purpose: Controls the scripted walk animation during the entry sequence.
    /// Input: Whether the player should appear to be walking.
    /// Output: Animator reflects the automated movement state.
    /// </summary>
    void SetPlayerWalkingAnimation(bool isWalking)
    {
        if (playerAnimator == null)
        {
            return;
        }

        SetAnimatorBoolIfPresent("isWalking", isWalking);
        SetAnimatorBoolIfPresent("isRunning", false);
        SetAnimatorBoolIfPresent("isJumping", false);
    }

    /// <summary>
    /// Purpose: Sets an Animator bool only when that parameter exists.
    /// Input: Animator parameter name and value.
    /// Output: Avoids missing-parameter warnings while updating animation state.
    /// </summary>
    void SetAnimatorBoolIfPresent(string parameterName, bool value)
    {
        if (!HasAnimatorBool(parameterName))
        {
            return;
        }

        playerAnimator.SetBool(parameterName, value);
    }

    /// <summary>
    /// Purpose: Checks whether the cached Animator has a bool parameter.
    /// Input: Animator parameter name.
    /// Output: True when the parameter exists and can be set safely.
    /// </summary>
    bool HasAnimatorBool(string parameterName)
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

    /// <summary>
    /// Purpose: Moves the player while respecting Rigidbody movement when available.
    /// Input: Desired world position.
    /// Output: Player reaches the sequence position.
    /// </summary>
    void MovePlayerTo(Vector3 worldPosition)
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.MovePosition(worldPosition);
            return;
        }

        if (player != null)
        {
            player.position = worldPosition;
        }
    }

    /// <summary>
    /// Purpose: Waits for an airborne player to land before the scripted doorway walk begins.
    /// Input: Player ground check, Rigidbody vertical velocity, and timeout.
    /// Output: Entry sequence starts from a natural grounded pose instead of midair.
    /// </summary>
    IEnumerator WaitForPlayerGroundedRoutine()
    {
        CachePlayerReferences();

        float elapsedTime = 0f;

        while (elapsedTime < maxWaitForGroundTime)
        {
            if (IsPlayerReadyForEntry())
            {
                yield break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Purpose: Checks whether the player has landed and is stable enough to walk into the doorway.
    /// Input: Rigidbody vertical velocity and a ground raycast from the player's ground check point.
    /// Output: True when the scripted entry can begin.
    /// </summary>
    bool IsPlayerReadyForEntry()
    {
        if (playerRigidbody != null && Mathf.Abs(playerRigidbody.velocity.y) > 0.05f)
        {
            return false;
        }

        return HasGroundBelowPlayer();
    }

    /// <summary>
    /// Purpose: Detects ground below the player without relying on PlayerController updates.
    /// Input: Player groundCheck transform when available, fallback player transform, and ground layer.
    /// Output: True when a ground surface is close below the player.
    /// </summary>
    bool HasGroundBelowPlayer()
    {
        if (player == null)
        {
            return true;
        }

        Transform groundProbe = playerController != null && playerController.groundCheck != null
            ? playerController.groundCheck
            : player;

        LayerMask groundLayer = entryGroundLayer;

        if (playerController != null && playerController.groundLayer.value != 0)
        {
            groundLayer = playerController.groundLayer;
        }

        return Physics.Raycast(
            groundProbe.position + Vector3.up * 0.1f,
            Vector3.down,
            entryGroundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    /// <summary>
    /// Purpose: Freezes physics only after the player is ready for the controlled doorway walk.
    /// Input: Player Rigidbody.
    /// Output: Scripted entry movement is stable and unaffected by gravity.
    /// </summary>
    void FreezePlayerForScriptedEntry()
    {
        if (playerRigidbody == null)
        {
            return;
        }

        playerRigidbody.velocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        playerRigidbody.useGravity = false;
        playerRigidbody.isKinematic = true;
    }

    /// <summary>
    /// Purpose: Caches player renderers and their original visual colors for fading.
    /// Input: Player visual root or player object.
    /// Output: Renderer arrays ready for alpha changes.
    /// </summary>
    void CachePlayerFadeRenderers()
    {
        CachePlayerReferences();

        GameObject root = playerVisualRoot != null
            ? playerVisualRoot
            : (player != null ? player.gameObject : null);

        if (root == null)
        {
            playerFadeRenderers = new Renderer[0];
            playerFadeMaterials = new Material[0];
            playerFadeOriginalColors = new Color[0];
            playerFadeUsesSpriteColor = new bool[0];
            return;
        }

        playerFadeRenderers = root.GetComponentsInChildren<Renderer>(true);
        playerFadeMaterials = new Material[playerFadeRenderers.Length];
        playerFadeOriginalColors = new Color[playerFadeRenderers.Length];
        playerFadeUsesSpriteColor = new bool[playerFadeRenderers.Length];

        for (int i = 0; i < playerFadeRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = playerFadeRenderers[i] as SpriteRenderer;

            if (spriteRenderer != null)
            {
                playerFadeUsesSpriteColor[i] = true;
                playerFadeOriginalColors[i] = spriteRenderer.color;
                continue;
            }

            Material material = playerFadeRenderers[i].material;
            playerFadeMaterials[i] = material;
            ConfigureRevealMaterialForFade(material);
            playerFadeOriginalColors[i] = material.HasProperty("_Color")
                ? material.color
                : Color.white;
        }
    }

    /// <summary>
    /// Purpose: Converts entry progress into player alpha.
    /// Input: Normalized entry progress from 0 to 1.
    /// Output: Player remains visible at first, then fades to invisible.
    /// </summary>
    void SetPlayerFadeByEntryProgress(float entryProgress)
    {
        float fadeRange = Mathf.Max(0.01f, 1f - fadePlayerAfterProgress);
        float fadeProgress = Mathf.Clamp01((entryProgress - fadePlayerAfterProgress) / fadeRange);

        SetPlayerAlpha(1f - fadeProgress);
    }

    /// <summary>
    /// Purpose: Applies player visual alpha.
    /// Input: Alpha from 0 to 1.
    /// Output: Player renderers fade smoothly.
    /// </summary>
    void SetPlayerAlpha(float alpha)
    {
        if (playerFadeRenderers == null)
        {
            return;
        }

        for (int i = 0; i < playerFadeRenderers.Length; i++)
        {
            if (playerFadeRenderers[i] == null)
            {
                continue;
            }

            Color color = playerFadeOriginalColors[i];
            color.a *= Mathf.Clamp01(alpha);

            if (playerFadeUsesSpriteColor[i])
            {
                SpriteRenderer spriteRenderer = playerFadeRenderers[i] as SpriteRenderer;

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = color;
                }

                continue;
            }

            Material material = playerFadeMaterials[i];

            if (material != null && material.HasProperty("_Color"))
            {
                material.color = color;
            }
        }
    }

    /// <summary>
    /// Purpose: Hides player visuals once the fade has completed.
    /// Input: Player visual renderers.
    /// Output: Player no longer appears in the doorway.
    /// </summary>
    void HidePlayerVisual()
    {
        if (playerVisualRoot != null)
        {
            playerVisualRoot.SetActive(false);
            return;
        }

        if (playerFadeRenderers == null)
        {
            return;
        }

        for (int i = 0; i < playerFadeRenderers.Length; i++)
        {
            if (playerFadeRenderers[i] != null)
            {
                playerFadeRenderers[i].enabled = false;
            }
        }
    }

    /// <summary>
    /// Purpose: Prepares the black reveal surface so it can fade in during opening.
    /// Input: Reveal object or renderer assigned in the Inspector.
    /// Output: Runtime material is transparent and starts hidden.
    /// </summary>
    void PrepareRevealVisual()
    {
        if (revealRenderer == null && revealObject != null)
        {
            revealRenderer = revealObject.GetComponentInChildren<Renderer>(true);
        }

        if (revealObject != null)
        {
            revealObject.SetActive(true);
        }

        if (revealRenderer == null)
        {
            return;
        }

        revealMaterial = revealRenderer.material;
        ConfigureRevealMaterialForFade(revealMaterial);
        SetRevealProgress(fadeRevealWithDoor ? 0f : 1f);
    }

    /// <summary>
    /// Purpose: Makes common built-in materials respond to alpha changes.
    /// Input: Runtime material used by the reveal renderer.
    /// Output: Material uses transparent blending when possible.
    /// </summary>
    void ConfigureRevealMaterialForFade(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
        }

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    /// <summary>
    /// Purpose: Applies the reveal fade amount.
    /// Input: Normalized open progress from 0 to 1.
    /// Output: Black reveal surface fades in behind the sliding door.
    /// </summary>
    void SetRevealProgress(float progress)
    {
        if (revealMaterial == null)
        {
            return;
        }

        Color color = revealColor;
        color.a = Mathf.Clamp01(progress) * revealMaxAlpha;

        if (revealMaterial.HasProperty("_Color"))
        {
            revealMaterial.color = color;
        }
    }

    /// <summary>
    /// Purpose: Prepares the foreground black occluder used to hide the player while entering.
    /// Input: Optional occluder object or renderer assigned in the Inspector.
    /// Output: Occluder is black and hidden until the entry sequence begins.
    /// </summary>
    void PrepareEntryOccluderVisual()
    {
        if (entryOccluderRenderer == null && entryOccluderObject != null)
        {
            entryOccluderRenderer = entryOccluderObject.GetComponentInChildren<Renderer>(true);
        }

        if (entryOccluderRenderer != null)
        {
            entryOccluderMaterial = entryOccluderRenderer.material;
            SetEntryOccluderColor();
        }

        if (entryOccluderObject != null && hideEntryOccluderUntilEntry)
        {
            entryOccluderObject.SetActive(false);
        }
    }

    /// <summary>
    /// Purpose: Shows or hides the entry occluder.
    /// Input: Desired active state.
    /// Output: The black foreground mask appears only during the player entry sequence.
    /// </summary>
    void SetEntryOccluderActive(bool isActive)
    {
        if (entryOccluderObject == null)
        {
            return;
        }

        entryOccluderObject.SetActive(isActive);

        if (isActive)
        {
            SetEntryOccluderColor();
        }
    }

    /// <summary>
    /// Purpose: Applies the configured black color to the entry occluder material.
    /// Input: Runtime occluder material.
    /// Output: Occluder renders as a solid shadow surface.
    /// </summary>
    void SetEntryOccluderColor()
    {
        if (entryOccluderMaterial == null || !entryOccluderMaterial.HasProperty("_Color"))
        {
            return;
        }

        entryOccluderMaterial.color = entryOccluderColor;
    }

    /// <summary>
    /// Purpose: Detects the player even when the trigger is touched by a child collider.
    /// Input: Collider entering the trigger.
    /// Output: True when the collider belongs to the configured player object.
    /// </summary>
    bool IsPlayerCollider(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            return true;
        }

        if (other.attachedRigidbody != null &&
            other.attachedRigidbody.CompareTag(playerTag))
        {
            return true;
        }

        Transform current = other.transform.parent;

        while (current != null)
        {
            if (current.CompareTag(playerTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    /// <summary>
    /// Purpose: Finds the player object by tag for the 2.5D fallback check.
    /// Input: Configured player tag.
    /// Output: Caches the player Transform when found.
    /// </summary>
    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    /// <summary>
    /// Purpose: Checks whether the player is inside this trigger's local X/Y rectangle while ignoring depth.
    /// Input: Player world position.
    /// Output: True when the player visually reaches the door trigger area.
    /// </summary>
    bool IsPlayerInsideTriggerXY(Vector3 playerWorldPosition)
    {
        Vector3 localPoint = transform.InverseTransformPoint(playerWorldPosition);
        Vector3 center = triggerCollider.center;
        Vector3 halfSize = triggerCollider.size * 0.5f;

        return localPoint.x >= center.x - halfSize.x &&
               localPoint.x <= center.x + halfSize.x &&
               localPoint.y >= center.y - halfSize.y &&
               localPoint.y <= center.y + halfSize.y;
    }

    /// <summary>
    /// Purpose: Stores the closed positions based on how the door is placed in the scene.
    /// Input: Current local positions of moving parts.
    /// Output: Closed-state positions are available for animation.
    /// </summary>
    void CacheClosedPositions()
    {
        if (movingParts == null)
        {
            closedLocalPositions = new Vector3[0];
            closedWorldPositions = new Vector3[0];
            return;
        }

        closedLocalPositions = new Vector3[movingParts.Length];
        closedWorldPositions = new Vector3[movingParts.Length];

        for (int i = 0; i < movingParts.Length; i++)
        {
            closedLocalPositions[i] =
                movingParts[i] != null ? movingParts[i].localPosition : Vector3.zero;

            closedWorldPositions[i] =
                movingParts[i] != null ? movingParts[i].position : Vector3.zero;
        }
    }

    /// <summary>
    /// Purpose: Gives the trigger a sensible default shape when the script is first added.
    /// Input: Newly added component in the Unity Editor.
    /// Output: Door root gets a trigger area in front of the door.
    /// </summary>
    void Reset()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.center = new Vector3(-0.7f, 0.8f, 0f);
        boxCollider.size = new Vector3(1.4f, 1.8f, 1.2f);
    }

    /// <summary>
    /// Purpose: Keeps inspector values in a valid range.
    /// Input: Inspector edits.
    /// Output: Prevents invalid animation timing.
    /// </summary>
    void OnValidate()
    {
        openDuration = Mathf.Max(0.01f, openDuration);
        entryWalkDuration = Mathf.Max(0.01f, entryWalkDuration);
        closeDuration = Mathf.Max(0.01f, closeDuration);
        waitAfterOpen = Mathf.Max(0f, waitAfterOpen);
        waitBeforeClose = Mathf.Max(0f, waitBeforeClose);
        maxWaitForGroundTime = Mathf.Max(0f, maxWaitForGroundTime);
        entryGroundCheckDistance = Mathf.Max(0.01f, entryGroundCheckDistance);
    }
}
