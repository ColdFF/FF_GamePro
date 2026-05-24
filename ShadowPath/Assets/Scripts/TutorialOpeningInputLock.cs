using UnityEngine;

/// <summary>
/// Purpose: Locks player and light-control input during the tutorial opening camera movement.
/// Input: Player controller, light controller, local wash-light controller, and unlock timing.
/// Output: Prevents early gameplay input until the opening camera focus is complete.
/// </summary>
public class TutorialOpeningInputLock : MonoBehaviour
{
    [Header("Unlock Feedback")]
    public LightUnlockPulse unlockPulse;

    [Header("Input Components")]
    public PlayerController playerController;
    public LightAngleController lightAngleController;
    public CameraLocalWashLightController localWashLightController;

    [Header("Timing")]
    public float unlockDelay = 2.8f;

    private float timer;
    private bool hasUnlocked;
    private bool originalPlayerControllerState;
    private bool originalLightControllerState;

    // Purpose: Locks gameplay input when the level starts.
    // Input: Assigned input-related components.
    // Output: Disables player movement, disables light-angle input, and blocks local wash-light input.
    void Start()
    {
        CacheOriginalStates();
        LockOpeningInput();
    }

    // Purpose: Counts opening time and unlocks input after the camera focus delay.
    // Input: Time since level start.
    // Output: Restores player and light controls once the opening has finished.
    void Update()
    {
        if (hasUnlocked)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= unlockDelay)
        {
            UnlockOpeningInput();
        }
    }

    // Purpose: Stores component enabled states before locking.
    // Input: Current component enabled values.
    // Output: Saves states so they can be restored safely.
    void CacheOriginalStates()
    {
        if (playerController != null)
        {
            originalPlayerControllerState = playerController.enabled;
        }

        if (lightAngleController != null)
        {
            originalLightControllerState = lightAngleController.enabled;
        }
    }

    // Purpose: Prevents the player from controlling the level during the opening shot.
    // Input: Player, light, and wash-light controller references.
    // Output: Temporarily blocks gameplay input.
    void LockOpeningInput()
    {
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
    }

    // Purpose: Re-enables gameplay input after the opening camera focus is complete.
    // Input: Previously stored component states.
    // Output: Allows the player to move, jump, and adjust light again.
    void UnlockOpeningInput()
    {
        hasUnlocked = true;

        if (playerController != null)
        {
            playerController.enabled = originalPlayerControllerState;
        }

        if (lightAngleController != null)
        {
            lightAngleController.enabled = originalLightControllerState;
        }

        if (localWashLightController != null)
        {
            localWashLightController.acceptInput = true;
        }

        if (unlockPulse != null)
        {
            unlockPulse.PlayPulse();
        }
    }
}