using System.Collections;
using UnityEngine;

/// <summary>
/// Purpose: Opens a sliding door when the player reaches it.
/// Input: Player trigger contact, door parts, and optional entry-sequence settings.
/// Output: The door slides open, and it can optionally move the player into the doorway.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class SlidingDoorOpener : MonoBehaviour
{
    [Header("Moving Parts")]
    // Door pieces that should slide when the door opens.
    public Transform[] movingParts;
    // How far each door piece moves from its closed position.
    public Vector3 localOpenOffset = new Vector3(1.5f, 0f, 0f);

    [Header("Open Motion")]
    // Time the door takes to open.
    public float openDuration = 0.8f;
    // Controls the feel of the opening motion, such as slow start and slow end.
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    // If true, the door cannot be opened again after the first time.
    public bool openOnlyOnce = true;

    [Header("Door Audio")]
    // AudioSource used to play door sounds.
    public AudioSource doorAudioSource;
    // Sound played when the door opens.
    public AudioClip openSound;
    // Sound played when the door closes.
    public AudioClip closeSound;
    // Volume of the door sounds.
    [Range(0f, 1f)] public float doorSoundVolume = 0.7f;

    [Header("Reveal Visual")]
    // Optional black/reveal object shown behind the door as it opens.
    public GameObject revealObject;
    // Renderer for the reveal object.
    public Renderer revealRenderer;
    // Color used for the reveal object.
    public Color revealColor = Color.black;
    // Strongest alpha/visibility of the reveal object.
    [Range(0f, 1f)] public float revealMaxAlpha = 1f;
    // If true, the reveal object fades in as the door opens.
    public bool fadeRevealWithDoor = true;

    [Header("Entry Sequence")]
    // If true, the player is automatically moved into the doorway after opening.
    public bool runEntrySequenceAfterOpen = false;
    // Target point the player walks toward during the entry sequence.
    public Transform entryTarget;
    // If true, only the target X changes and the player's Y/Z stay mostly unchanged.
    public bool useEntryTargetXOnly = true;
    // Short pause after the door opens before the player walks in.
    public float waitAfterOpen = 0.15f;
    // If true, waits until the player is on the ground before the scripted walk starts.
    public bool waitForGroundBeforeEntry = true;
    // Longest time to wait for the player to land.
    public float maxWaitForGroundTime = 1.5f;
    // Time the player takes to walk into the doorway.
    public float entryWalkDuration = 1f;
    // Controls the feel of the scripted player walk.
    public AnimationCurve entryWalkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    // If true, the player fades out while walking into the doorway.
    public bool fadePlayerDuringEntry = false;
    // Entry progress before player fading begins.
    [Range(0f, 1f)] public float fadePlayerAfterProgress = 0.25f;
    // Pause before the door closes after the player enters.
    public float waitBeforeClose = 0.2f;
    // If true, the door closes after the entry sequence.
    public bool closeDoorAfterEntry = true;
    // Time the door takes to close.
    public float closeDuration = 0.8f;
    // Controls the feel of the closing motion.
    public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    // Visual root of the player, used for fading/hiding.
    public GameObject playerVisualRoot;

    [Header("Entry Occluder")]
    // Optional black object in front of the doorway to hide the player during entry.
    public GameObject entryOccluderObject;
    // Renderer for the entry occluder.
    public Renderer entryOccluderRenderer;
    // Color used by the entry occluder.
    public Color entryOccluderColor = Color.black;
    // If true, the occluder starts hidden and only appears during entry.
    public bool hideEntryOccluderUntilEntry = true;

    [Header("Camera Return")]
    // If true, camera returns to the overview shot after the door finishes closing.
    public bool returnCameraToOverviewAfterClose = true;
    // Camera controller used to return to overview.
    public TutorialCameraController tutorialCameraController;

    [Header("Input Lock References")]
    // Player controller disabled during the scripted entry sequence.
    public PlayerController playerController;
    // Light controller disabled during the scripted entry sequence.
    public LightAngleController lightAngleController;
    // Local wash light controller disabled during the scripted entry sequence.
    public CameraLocalWashLightController localWashLightController;
    // Player Rigidbody used to stop physics during the scripted entry sequence.
    public Rigidbody playerRigidbody;
    // Ground layers used to check whether the player has landed.
    public LayerMask entryGroundLayer = 1 << 3;
    // Distance used to check for ground below the player.
    public float entryGroundCheckDistance = 0.35f;

    [Header("Trigger")]
    // Tag used to recognise the player.
    public string playerTag = "Player";
    // If true, trigger detection can ignore Z depth for 2.5D scenes.
    public bool use2DProximityFallback = true;
    // Player Transform used by the 2D fallback check.
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
    /// Purpose: Prepares the door when the level starts.
    /// Input: Trigger collider, moving door parts, reveal visuals, and occluder settings.
    /// Output: The door knows its closed position and is ready to open.
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
    /// Purpose: Checks the player position for 2.5D trigger detection.
    /// Input: Player position and this trigger's X/Y area.
    /// Output: The door opens when the player visually enters the area.
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
    /// Purpose: Opens the door when the player enters the trigger.
    /// Input: Trigger contact.
    /// Output: Door opening starts if the collider belongs to the player.
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
    /// Purpose: Opens the door if the player is already inside the trigger.
    /// Input: Trigger contact that continues for more than one frame.
    /// Output: Door opening can still start.
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
    /// Purpose: Starts the door opening.
    /// Input: Current door state and assigned moving parts.
    /// Output: The opening coroutine begins.
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
    /// Purpose: Plays the opening movement over time.
    /// Input: Closed positions, open offset, duration, and open curve.
    /// Output: Door parts slide from closed to open.
    /// </summary>
    IEnumerator OpenDoorRoutine()
    {
        hasOpened = true;
        PlayDoorSound(openSound);

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
    /// Purpose: Runs the optional sequence after the door opens.
    /// Input: Player, entry target, input components, fade visuals, and close settings.
    /// Output: The player walks into the doorway, can fade out, and the door may close.
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
    /// Purpose: Stops the player from controlling the character during the entry sequence.
    /// Input: Player, light, wash-light, and Rigidbody references.
    /// Output: Player/light controls are disabled and player movement is stopped.
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
    /// Purpose: Moves the player into the doorway automatically.
    /// Input: Player start position, entry target, walk duration, and fade settings.
    /// Output: The player reaches the doorway and may become invisible.
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
    /// Purpose: Plays the closing movement over time.
    /// Input: Close duration and close curve.
    /// Output: Door parts return to their closed positions.
    /// </summary>
    IEnumerator CloseDoorRoutine()
    {
        PlayDoorSound(closeSound);

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
    /// Purpose: Sets the door position for one moment in the animation.
    /// Input: Progress from 0 closed to 1 open.
    /// Output: Each moving part moves to the matching position.
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
    /// Purpose: Plays a door sound once.
    /// Input: Open or close sound clip.
    /// Output: The sound plays through the door AudioSource.
    /// </summary>
    void PlayDoorSound(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (doorAudioSource == null)
        {
            doorAudioSource = GetComponent<AudioSource>();
        }

        if (doorAudioSource == null)
        {
            return;
        }

        doorAudioSource.PlayOneShot(clip, doorSoundVolume);
    }

    /// <summary>
    /// Purpose: Finds player-related components if they were not assigned manually.
    /// Input: Player Transform or player tag.
    /// Output: PlayerController, Rigidbody, visual root, and related controllers are cached.
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
    /// Purpose: Sends the camera back to the overview view.
    /// Input: TutorialCameraController reference, or a scene search if missing.
    /// Output: Camera returns to the wider tutorial view.
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
    /// Purpose: Decides where the player should walk during entry.
    /// Input: Current player position and optional entry target.
    /// Output: Target world position for the scripted walk.
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
    /// Purpose: Puts the player animation into idle.
    /// Input: Player Animator.
    /// Output: Walking, running, and jumping are turned off.
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
    /// Purpose: Shows or stops the scripted walking animation.
    /// Input: Whether the player should look like they are walking.
    /// Output: Animator walking state is updated.
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
    /// Purpose: Sets an Animator bool safely.
    /// Input: Animator parameter name and true/false value.
    /// Output: The value changes only if that bool exists.
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
    /// Purpose: Checks if the Animator has a bool with this name.
    /// Input: Animator parameter name.
    /// Output: True means it is safe to set that bool.
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
    /// Purpose: Moves the player to a target position.
    /// Input: Desired world position.
    /// Output: Player Transform or Rigidbody moves there.
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
    /// Purpose: Waits for the player to land before walking into the doorway.
    /// Input: Ground check, vertical speed, and timeout.
    /// Output: Entry starts from the ground when possible.
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
    /// Purpose: Checks if the player is ready for the scripted entry walk.
    /// Input: Player vertical speed and ground check.
    /// Output: True means the player is stable enough to start.
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
    /// Purpose: Checks if there is ground below the player.
    /// Input: Player ground check point or player position.
    /// Output: True means ground is close below.
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
    /// Purpose: Freezes the player for the automatic doorway walk.
    /// Input: Player Rigidbody.
    /// Output: Gravity and physics movement stop during the scripted move.
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
    /// Purpose: Saves the player's renderers before fading.
    /// Input: Player visual root or player object.
    /// Output: The script knows which materials/colors to fade.
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
    /// Purpose: Converts entry progress into player visibility.
    /// Input: Entry progress from 0 to 1.
    /// Output: The player stays visible first, then fades out.
    /// </summary>
    void SetPlayerFadeByEntryProgress(float entryProgress)
    {
        float fadeRange = Mathf.Max(0.01f, 1f - fadePlayerAfterProgress);
        float fadeProgress = Mathf.Clamp01((entryProgress - fadePlayerAfterProgress) / fadeRange);

        SetPlayerAlpha(1f - fadeProgress);
    }

    /// <summary>
    /// Purpose: Applies the player fade value.
    /// Input: Alpha from 1 fully visible to 0 invisible.
    /// Output: Player renderers change visibility.
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
    /// Purpose: Hides the player after the entry fade.
    /// Input: Player visual root or player object.
    /// Output: The player no longer appears in the doorway.
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
    /// Purpose: Prepares the black reveal object behind the door.
    /// Input: Reveal object or renderer from the Inspector.
    /// Output: The reveal object starts hidden and ready to fade in.
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
    /// Purpose: Makes a material support fading.
    /// Input: Runtime material used by reveal or player fading.
    /// Output: Alpha changes can make the material transparent.
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
    /// Purpose: Sets how visible the reveal object is.
    /// Input: Door open progress from 0 to 1.
    /// Output: The black reveal fades in behind the door.
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
    /// Purpose: Prepares the black object that hides the player during entry.
    /// Input: Optional occluder object or renderer.
    /// Output: The occluder is black and starts hidden if requested.
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
    /// Input: True to show it, false to hide it.
    /// Output: The black mask appears only during the doorway entry.
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
    /// Purpose: Applies the chosen color to the entry occluder.
    /// Input: Occluder material and color setting.
    /// Output: The occluder renders as the intended black mask.
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
    /// Purpose: Checks if a collider belongs to the player.
    /// Input: Collider entering or staying in the trigger.
    /// Output: True means this is the player or part of the player.
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
    /// Purpose: Finds the player by tag.
    /// Input: Player tag.
    /// Output: Player Transform is cached if found.
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
    /// Purpose: Checks if the player is inside the trigger area in X/Y only.
    /// Input: Player world position.
    /// Output: True means the player visually reaches the door.
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
    /// Purpose: Saves where the door parts start.
    /// Input: Current positions of moving parts.
    /// Output: The script knows the closed door positions.
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
    /// Purpose: Sets a useful default trigger when the script is first added.
    /// Input: Newly added component in Unity.
    /// Output: The door gets a trigger area in front of it.
    /// </summary>
    void Reset()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.center = new Vector3(-0.7f, 0.8f, 0f);
        boxCollider.size = new Vector3(1.4f, 1.8f, 1.2f);
    }

    /// <summary>
    /// Purpose: Keeps Inspector timing values valid.
    /// Input: Inspector edits.
    /// Output: Durations cannot become invalid.
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
