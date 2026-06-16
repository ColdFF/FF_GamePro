using UnityEngine;

/// <summary>
/// Purpose: Controls the two switchable lights in Level 4.
/// Input: F key, arrow keys, light references, and shadow objects in the scene.
/// Output: Only the selected light controls the current shadow puzzle objects.
/// </summary>
public class Level04DualLightController : MonoBehaviour
{
    public enum LightPhase
    {
        // The first light setup, usually one side of the puzzle.
        PhaseA,
        // The second light setup, usually the other side of the puzzle.
        PhaseB
    }

    [Header("Input")]
    // Key used to switch between the two light phases.
    public KeyCode switchKey = KeyCode.F;

    [Header("Phase A")]
    // Transform of the Phase A directional light; this is what the script rotates.
    public Transform directionalLightA;
    // Phase A Light component; this is turned on when Phase A is active.
    public Light lightA;
    // Visible lamp model for Phase A.
    public GameObject stageLampRigA;
    // Screen/local wash light object for Phase A.
    public GameObject screenWashRigA;
    // Optional flash effect played when Phase A becomes active.
    public LightUnlockPulse washPulseA;
    // Old single-light controller; this script disables it so it does not fight the dual-light system.
    public LightAngleController legacyAngleControllerA;
    // Starting up/down angle for Phase A.
    public float startXAngleA = -15f;
    // Starting left/right angle for Phase A.
    public float startYAngleA = 10f;

    [Header("Phase B")]
    // Transform of the Phase B directional light; this is what the script rotates.
    public Transform directionalLightB;
    // Phase B Light component; this is turned on when Phase B is active.
    public Light lightB;
    // Visible lamp model for Phase B.
    public GameObject stageLampRigB;
    // Screen/local wash light object for Phase B.
    public GameObject screenWashRigB;
    // Optional flash effect played when Phase B becomes active.
    public LightUnlockPulse washPulseB;
    // Old single-light controller; this script disables it so it does not fight the dual-light system.
    public LightAngleController legacyAngleControllerB;
    // Starting up/down angle for Phase B.
    public float startXAngleB = -15f;
    // Starting left/right angle for Phase B.
    public float startYAngleB = -10f;

    [Header("Rotation")]
    // How fast the active light rotates when the player presses arrow keys.
    public float rotateSpeed = 25f;
    // If true, Phase A and Phase B use their own rotation limits.
    public bool useSeparatePhaseAngleLimits = true;
    // Shared left limit, used only when separate phase limits are turned off.
    public float minYAngle = -30f;
    // Shared right limit, used only when separate phase limits are turned off.
    public float maxYAngle = 30f;
    // Shared downward/upward limit, used only when separate phase limits are turned off.
    public float minXAngle = -20f;
    // Shared downward/upward limit, used only when separate phase limits are turned off.
    public float maxXAngle = 5f;

    [Header("Phase A Rotation Limits")]
    // Furthest left/right value Phase A can rotate to on one side.
    public float minYAngleA = 4f;
    // Furthest left/right value Phase A can rotate to on the other side.
    public float maxYAngleA = 30f;
    // Furthest down/up value Phase A can rotate to on one side.
    public float minXAngleA = -20f;
    // Furthest down/up value Phase A can rotate to on the other side.
    public float maxXAngleA = 5f;

    [Header("Phase B Rotation Limits")]
    // Furthest left/right value Phase B can rotate to on one side.
    public float minYAngleB = -30f;
    // Furthest left/right value Phase B can rotate to on the other side.
    public float maxYAngleB = -4f;
    // Furthest down/up value Phase B can rotate to on one side.
    public float minXAngleB = -20f;
    // Furthest down/up value Phase B can rotate to on the other side.
    public float maxXAngleB = 5f;

    [Header("Switch Feedback")]
    // AudioSource used to play the switching sound.
    public AudioSource switchAudioSource;
    // Sound played when the player switches light phase.
    public AudioClip switchSound;
    // Volume of the switching sound.
    [Range(0f, 1f)] public float switchSoundVolume = 0.7f;
    // If true, the active wash light flashes when switching phase.
    public bool pulseWashLightOnSwitch = true;
    // If true, both lamp models stay visible even though only one gameplay light is active.
    public bool keepBothStageLampsVisible = true;

    [Header("Scene Shadow Users")]
    // If true, the script finds shadow objects in the scene automatically.
    public bool autoRefreshSceneShadowUsers = true;
    // If true, the player lets go of any rope before the light phase changes.
    public bool dropAttachedRopesOnLightSwitch = true;
    // Shadow platform scripts controlled by the active Level 4 light.
    public ProjectedShadowAllEdgePlatform[] allEdgePlatforms;
    // Shadow ladder scripts controlled by the active Level 4 light.
    public ShadowLadderClimbZone[] ladderZones;
    // Shadow rope scripts controlled by the active Level 4 light.
    public ShadowRopeSwingZone[] ropeSwingZones;

    private bool usingPhaseB;
    private float currentXAngleA;
    private float currentYAngleA;
    private float currentXAngleB;
    private float currentYAngleB;

    /// <summary>
    /// Purpose: Sets up both lights when Level 4 starts.
    /// Input: Light references and starting angles from the Inspector.
    /// Output: The correct starting light is active and shadow objects use that light.
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
    /// Purpose: Checks player input every frame.
    /// Input: F key for switching, arrow keys for rotating the active light.
    /// Output: The selected light can switch or rotate.
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
    /// Purpose: Turns off the old single-light scripts in Level 4.
    /// Input: Old LightAngleController references, if they exist.
    /// Output: Only this dual-light script controls the lights.
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
    /// Purpose: Makes the current light phase take effect.
    /// Input: Which phase is active, plus whether to play sound or flash.
    /// Output: The right light and matching shadow objects become active.
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
    /// Purpose: Shows or hides one phase's light objects.
    /// Input: The lamp model, wash light, real Light component, and whether this phase is active.
    /// Output: The phase looks correct in the scene and only the active gameplay light is enabled.
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
    /// Purpose: Rotates the light that is currently selected.
    /// Input: Arrow key direction.
    /// Output: The active light angle changes but stays inside its allowed range.
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
    /// Purpose: Applies the saved angle numbers to the real light objects.
    /// Input: Current Phase A and Phase B angle values.
    /// Output: The two lights rotate to match those values.
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
    /// Purpose: Reads left/right light control.
    /// Input: Left Arrow and Right Arrow keys.
    /// Output: A simple number saying rotate left, rotate right, or do nothing.
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
    /// Purpose: Reads up/down light control.
    /// Input: Up Arrow and Down Arrow keys.
    /// Output: A simple number saying rotate up, rotate down, or do nothing.
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
    /// Purpose: Plays the phase switch sound once.
    /// Input: The sound clip and AudioSource from the Inspector.
    /// Output: The player hears feedback when switching lights.
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
    /// Purpose: Finds the shadow objects in the scene.
    /// Input: All scene objects, including inactive helper objects.
    /// Output: The script has lists of platforms, ladders, and ropes to control.
    /// </summary>
    private void RefreshSceneShadowUsers()
    {
        if (!autoRefreshSceneShadowUsers)
        {
            return;
        }

        allEdgePlatforms = FindObjectsOfType<ProjectedShadowAllEdgePlatform>(true);
        ladderZones = FindObjectsOfType<ShadowLadderClimbZone>(true);
        ropeSwingZones = FindObjectsOfType<ShadowRopeSwingZone>(true);
    }

    /// <summary>
    /// Purpose: Makes the player let go of a rope before switching lights.
    /// Input: The rope scripts found in the scene.
    /// Output: The player does not stay attached to a rope that may disappear.
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
    /// Purpose: Gives the active light to the shadow gameplay objects.
    /// Input: Current phase and the lists of shadow objects.
    /// Output: Only objects allowed in this phase make usable shadows.
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
    /// Purpose: Fixes both starting light angles if they are outside the allowed range.
    /// Input: Saved Phase A and Phase B angles.
    /// Output: Both angles are kept inside their limits.
    /// </summary>
    private void ClampStoredLightAngles()
    {
        ClampLightAngles(ref currentXAngleA, ref currentYAngleA, LightPhase.PhaseA);
        ClampLightAngles(ref currentXAngleB, ref currentYAngleB, LightPhase.PhaseB);
    }

    /// <summary>
    /// Purpose: Keeps one light's angle inside its allowed range.
    /// Input: One light's up/down and left/right angle values.
    /// Output: Any too-large or too-small angle is pulled back into the allowed range.
    /// </summary>
    private void ClampLightAngles(ref float xAngle, ref float yAngle, LightPhase phase)
    {
        GetAngleLimits(phase, out float minX, out float maxX, out float minY, out float maxY);
        xAngle = Mathf.Clamp(xAngle, minX, maxX);
        yAngle = Mathf.Clamp(yAngle, minY, maxY);
    }

    /// <summary>
    /// Purpose: Chooses which angle limits should be used.
    /// Input: Phase A or Phase B.
    /// Output: The correct min/max values for that phase.
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
    /// Purpose: Checks whether one shadow object belongs to the current phase.
    /// Input: The shadow object and the current light phase.
    /// Output: True means this object can be used now.
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
    /// Purpose: Turns collision on or off for one shadow object.
    /// Input: The object that owns colliders, and whether collision should be enabled.
    /// Output: Its colliders match the current phase state.
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
