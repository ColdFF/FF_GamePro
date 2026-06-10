using UnityEngine;

/// <summary>
/// Level04-only controller for switching between the left and right gameplay light phases.
/// It owns the phase input so the shared LightAngleController script can remain unchanged.
/// </summary>
public class Level04DualLightController : MonoBehaviour
{
    public enum LightPhase
    {
        PhaseA,
        PhaseB
    }

    [Header("Input")]
    public KeyCode switchKey = KeyCode.F;

    [Header("Phase A")]
    public Transform directionalLightA;
    public Light lightA;
    public GameObject stageLampRigA;
    public GameObject screenWashRigA;
    public LightUnlockPulse washPulseA;
    public LightAngleController legacyAngleControllerA;
    public float startXAngleA = -15f;
    public float startYAngleA = 10f;

    [Header("Phase B")]
    public Transform directionalLightB;
    public Light lightB;
    public GameObject stageLampRigB;
    public GameObject screenWashRigB;
    public LightUnlockPulse washPulseB;
    public LightAngleController legacyAngleControllerB;
    public float startXAngleB = -15f;
    public float startYAngleB = -10f;

    [Header("Rotation")]
    public float rotateSpeed = 25f;
    public bool useSeparatePhaseAngleLimits = true;
    public float minYAngle = -30f;
    public float maxYAngle = 30f;
    public float minXAngle = -20f;
    public float maxXAngle = 5f;

    [Header("Phase A Rotation Limits")]
    public float minYAngleA = 4f;
    public float maxYAngleA = 30f;
    public float minXAngleA = -20f;
    public float maxXAngleA = 5f;

    [Header("Phase B Rotation Limits")]
    public float minYAngleB = -30f;
    public float maxYAngleB = -4f;
    public float minXAngleB = -20f;
    public float maxXAngleB = 5f;

    [Header("Switch Feedback")]
    public AudioSource switchAudioSource;
    public AudioClip switchSound;
    [Range(0f, 1f)] public float switchSoundVolume = 0.7f;
    public bool pulseWashLightOnSwitch = true;
    public bool keepBothStageLampsVisible = true;

    [Header("Scene Shadow Users")]
    public bool autoRefreshSceneShadowUsers = true;
    public bool dropAttachedRopesOnLightSwitch = true;
    public ProjectedShadowAllEdgePlatform[] allEdgePlatforms;
    public ProjectedShadowCollider[] shadowColliders;
    public ShadowLadderClimbZone[] ladderZones;
    public ShadowRopeSwingZone[] ropeSwingZones;

    private bool usingPhaseB;
    private float currentXAngleA;
    private float currentYAngleA;
    private float currentXAngleB;
    private float currentYAngleB;

    /// <summary>
    /// Purpose: Initializes both independent light phases and applies the starting active phase.
    /// Input: Serialized phase references, starting angles, and phase-specific angle limits.
    /// Output: Legacy controllers are disabled, lights are clamped into their own ranges, and shadow users receive the active phase.
    /// </summary>
    private void Awake()
    {
        currentXAngleA = startXAngleA;
        currentYAngleA = startYAngleA;
        currentXAngleB = startXAngleB;
        currentYAngleB = startYAngleB;

        ClampStoredLightAngles();
        DisableLegacyAngleControllers();
        RefreshSceneShadowUsers();
        ApplyLightRotations();
        ApplyPhase(false, false);
    }

    /// <summary>
    /// Purpose: Handles phase switching and rotation input for the currently active light.
    /// Input: Player keyboard input for switchKey and arrow-key light adjustment.
    /// Output: The active light changes or rotates while staying inside that phase's allowed angle range.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            RefreshSceneShadowUsers();
            DropAttachedRopesBeforeLightSwitch();
            usingPhaseB = !usingPhaseB;
            ApplyPhase(true, true);
        }

        UpdateActiveLightAngle();
    }

    /// <summary>
    /// Purpose: Prevents the copied single-light controllers from fighting Level04's dual-light controller.
    /// Input: Optional legacy LightAngleController references on A and B lights.
    /// Output: Legacy angle controllers are disabled for this scene only.
    /// </summary>
    private void DisableLegacyAngleControllers()
    {
        if (legacyAngleControllerA != null)
        {
            legacyAngleControllerA.enabled = false;
        }

        if (legacyAngleControllerB != null)
        {
            legacyAngleControllerB.enabled = false;
        }
    }

    /// <summary>
    /// Purpose: Applies the current phase to light objects, shadow gameplay users, audio, and visual pulse feedback.
    /// Input: The stored usingPhaseB state plus feedback flags.
    /// Output: Only the active gameplay light drives shadow systems, and phase-locked users are enabled or hidden.
    /// </summary>
    private void ApplyPhase(bool playFeedback, bool pulse)
    {
        SetPhaseObjects(stageLampRigA, screenWashRigA, lightA, !usingPhaseB);
        SetPhaseObjects(stageLampRigB, screenWashRigB, lightB, usingPhaseB);
        ApplyActiveGameplayLightToShadowUsers();

        if (playFeedback)
        {
            PlaySwitchSound();
        }

        if (pulse && pulseWashLightOnSwitch)
        {
            LightUnlockPulse activePulse = usingPhaseB ? washPulseB : washPulseA;
            if (activePulse != null)
            {
                activePulse.PlayPulse();
            }
        }
    }

    /// <summary>
    /// Purpose: Shows the stage lamp rig while enabling only the active gameplay light and screen wash.
    /// Input: Stage lamp rig, screen wash rig, directional Light component, and active state.
    /// Output: Visible lamp props remain in the scene, while gameplay lighting follows the selected phase.
    /// </summary>
    private void SetPhaseObjects(GameObject stageLampRig, GameObject screenWashRig, Light directionalLight, bool active)
    {
        if (stageLampRig != null)
        {
            stageLampRig.SetActive(keepBothStageLampsVisible || active);
        }

        if (screenWashRig != null)
        {
            screenWashRig.SetActive(active);
        }

        if (directionalLight != null)
        {
            directionalLight.enabled = active;
        }
    }

    /// <summary>
    /// Purpose: Rotates the active phase light from input while preserving independent limits per phase.
    /// Input: Horizontal and vertical arrow-key input.
    /// Output: The active phase's stored X/Y angles are updated and clamped.
    /// </summary>
    private void UpdateActiveLightAngle()
    {
        float horizontalInput = GetHorizontalLightInput();
        float verticalInput = GetVerticalLightInput();

        if (Mathf.Approximately(horizontalInput, 0f) && Mathf.Approximately(verticalInput, 0f))
        {
            return;
        }

        if (usingPhaseB)
        {
            currentYAngleB += horizontalInput * rotateSpeed * Time.deltaTime;
            currentXAngleB -= verticalInput * rotateSpeed * Time.deltaTime;
            ClampLightAngles(ref currentXAngleB, ref currentYAngleB, LightPhase.PhaseB);
        }
        else
        {
            currentYAngleA += horizontalInput * rotateSpeed * Time.deltaTime;
            currentXAngleA -= verticalInput * rotateSpeed * Time.deltaTime;
            ClampLightAngles(ref currentXAngleA, ref currentYAngleA, LightPhase.PhaseA);
        }

        ApplyLightRotations();
    }

    /// <summary>
    /// Purpose: Pushes stored phase angles onto the actual Directional Light transforms.
    /// Input: Current A and B X/Y angle values.
    /// Output: Both light transforms match their stored phase rotations.
    /// </summary>
    private void ApplyLightRotations()
    {
        if (directionalLightA != null)
        {
            directionalLightA.rotation = Quaternion.Euler(currentXAngleA, currentYAngleA, 0f);
        }

        if (directionalLightB != null)
        {
            directionalLightB.rotation = Quaternion.Euler(currentXAngleB, currentYAngleB, 0f);
        }
    }

    /// <summary>
    /// Purpose: Reads horizontal light rotation input with the existing control feel.
    /// Input: LeftArrow and RightArrow key states.
    /// Output: Signed horizontal input for light yaw adjustment.
    /// </summary>
    private float GetHorizontalLightInput()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            return 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            return -1f;
        }

        return 0f;
    }

    /// <summary>
    /// Purpose: Reads vertical light rotation input with the existing control feel.
    /// Input: UpArrow and DownArrow key states.
    /// Output: Signed vertical input for light pitch adjustment.
    /// </summary>
    private float GetVerticalLightInput()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            return -1f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            return 1f;
        }

        return 0f;
    }

    /// <summary>
    /// Purpose: Plays the same switch sound used by the light-opening feedback.
    /// Input: Optional AudioSource and switch AudioClip references.
    /// Output: A one-shot switch sound is played if audio references are available.
    /// </summary>
    private void PlaySwitchSound()
    {
        if (switchSound == null)
        {
            return;
        }

        if (switchAudioSource == null)
        {
            switchAudioSource = GetComponent<AudioSource>();
        }

        if (switchAudioSource == null)
        {
            return;
        }

        switchAudioSource.PlayOneShot(switchSound, switchSoundVolume);
    }

    /// <summary>
    /// Purpose: Finds all current scene shadow users so copied or newly added objects participate without manual lists.
    /// Input: Active scene objects, including inactive shadow helper objects.
    /// Output: Cached arrays of all Level04-compatible shadow user components.
    /// </summary>
    private void RefreshSceneShadowUsers()
    {
        if (!autoRefreshSceneShadowUsers)
        {
            return;
        }

        allEdgePlatforms = FindObjectsOfType<ProjectedShadowAllEdgePlatform>(true);
        shadowColliders = FindObjectsOfType<ProjectedShadowCollider>(true);
        ladderZones = FindObjectsOfType<ShadowLadderClimbZone>(true);
        ropeSwingZones = FindObjectsOfType<ShadowRopeSwingZone>(true);
    }

    /// <summary>
    /// Purpose: Drops the player from any rope before the rope shadow snaps to the other phase.
    /// Input: Cached ShadowRopeSwingZone instances.
    /// Output: Attached ropes release the player cleanly before the phase swap.
    /// </summary>
    private void DropAttachedRopesBeforeLightSwitch()
    {
        if (!dropAttachedRopesOnLightSwitch)
        {
            return;
        }

        foreach (ShadowRopeSwingZone ropeSwingZone in ropeSwingZones)
        {
            if (ropeSwingZone != null)
            {
                ropeSwingZone.ForceDropFromRope();
            }
        }
    }

    /// <summary>
    /// Purpose: Assigns the active gameplay light only to shadow users allowed in the current phase.
    /// Input: Active phase, cached shadow user arrays, and optional Level04LightPhaseBinding components.
    /// Output: Allowed users rebuild from the active light, while disallowed users hide or disable their colliders.
    /// </summary>
    private void ApplyActiveGameplayLightToShadowUsers()
    {
        Light activeLight = usingPhaseB ? lightB : lightA;
        LightPhase activePhase = usingPhaseB ? LightPhase.PhaseB : LightPhase.PhaseA;

        if (activeLight == null)
        {
            return;
        }

        foreach (ProjectedShadowAllEdgePlatform platform in allEdgePlatforms)
        {
            if (platform == null)
            {
                continue;
            }

            bool allowed = IsShadowUserAllowed(platform, activePhase);
            platform.enabled = allowed;
            platform.directionalLight = allowed ? activeLight : null;
            platform.Rebuild();
        }

        foreach (ProjectedShadowCollider shadowCollider in shadowColliders)
        {
            if (shadowCollider == null)
            {
                continue;
            }

            bool allowed = IsShadowUserAllowed(shadowCollider, activePhase);
            shadowCollider.enabled = allowed;
            shadowCollider.directionalLight = allowed ? activeLight : null;
            SetColliderEnabled(shadowCollider, allowed);

            if (allowed)
            {
                shadowCollider.RebuildShadowCollider();
            }
        }

        foreach (ShadowLadderClimbZone ladderZone in ladderZones)
        {
            if (ladderZone != null)
            {
                bool allowed = IsShadowUserAllowed(ladderZone, activePhase);
                ladderZone.enabled = allowed;
                ladderZone.directionalLight = allowed ? activeLight : null;
                SetColliderEnabled(ladderZone, allowed);
            }
        }

        foreach (ShadowRopeSwingZone ropeSwingZone in ropeSwingZones)
        {
            if (ropeSwingZone != null)
            {
                bool allowed = IsShadowUserAllowed(ropeSwingZone, activePhase);
                ropeSwingZone.enabled = allowed;
                ropeSwingZone.directionalLight = allowed ? activeLight : null;
                SetColliderEnabled(ropeSwingZone, allowed);

                if (!allowed)
                {
                    ropeSwingZone.ForceDropFromRope();
                }
            }
        }
    }

    /// <summary>
    /// Purpose: Clamps both stored phase angles after loading serialized scene values.
    /// Input: Current stored A and B light angles.
    /// Output: Stored angles are guaranteed to fit their configured phase limits.
    /// </summary>
    private void ClampStoredLightAngles()
    {
        ClampLightAngles(ref currentXAngleA, ref currentYAngleA, LightPhase.PhaseA);
        ClampLightAngles(ref currentXAngleB, ref currentYAngleB, LightPhase.PhaseB);
    }

    /// <summary>
    /// Purpose: Clamps one phase's pitch and yaw using either separate or legacy shared limits.
    /// Input: Angle references and the phase that owns those angles.
    /// Output: The supplied X/Y values are clamped in place.
    /// </summary>
    private void ClampLightAngles(ref float xAngle, ref float yAngle, LightPhase phase)
    {
        GetAngleLimits(phase, out float minX, out float maxX, out float minY, out float maxY);
        xAngle = Mathf.Clamp(xAngle, minX, maxX);
        yAngle = Mathf.Clamp(yAngle, minY, maxY);
    }

    /// <summary>
    /// Purpose: Resolves the correct pitch and yaw range for the requested phase.
    /// Input: Phase A or Phase B.
    /// Output: Min/max pitch and yaw limits for that phase.
    /// </summary>
    private void GetAngleLimits(LightPhase phase, out float minX, out float maxX, out float minY, out float maxY)
    {
        if (!useSeparatePhaseAngleLimits)
        {
            minX = minXAngle;
            maxX = maxXAngle;
            minY = minYAngle;
            maxY = maxYAngle;
            return;
        }

        if (phase == LightPhase.PhaseB)
        {
            minX = minXAngleB;
            maxX = maxXAngleB;
            minY = minYAngleB;
            maxY = maxYAngleB;
            return;
        }

        minX = minXAngleA;
        maxX = maxXAngleA;
        minY = minYAngleA;
        maxY = maxYAngleA;
    }

    /// <summary>
    /// Purpose: Checks whether a shadow user is allowed to operate in the active phase.
    /// Input: A shadow user component and the active light phase.
    /// Output: True when no binding exists or when the nearest binding allows that phase.
    /// </summary>
    private bool IsShadowUserAllowed(Component shadowUser, LightPhase activePhase)
    {
        if (shadowUser == null)
        {
            return false;
        }

        Level04LightPhaseBinding binding = shadowUser.GetComponent<Level04LightPhaseBinding>();
        if (binding == null)
        {
            binding = shadowUser.GetComponentInParent<Level04LightPhaseBinding>();
        }

        if (binding == null)
        {
            return true;
        }

        return binding.AllowsPhase(activePhase);
    }

    /// <summary>
    /// Purpose: Enables or disables colliders attached to a phase-locked shadow user.
    /// Input: Component owning possible collider components and the desired collision state.
    /// Output: Local Collider components match the requested enabled state.
    /// </summary>
    private void SetColliderEnabled(Component owner, bool enabledState)
    {
        if (owner == null)
        {
            return;
        }

        foreach (Collider collider in owner.GetComponents<Collider>())
        {
            collider.enabled = enabledState;
        }
    }

}
