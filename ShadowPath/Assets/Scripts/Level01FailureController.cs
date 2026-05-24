using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Purpose: Controls the Level 01 failure sequence when the player falls below the playable area.
/// Input: Player position, respawn point, death visual, and UI button calls.
/// Output: Locks gameplay input, returns the camera to the start area, plays a death visual, and shows the failure menu.
/// </summary>
public class Level01FailureController : MonoBehaviour
{
    [Header("Player References")]
    public Transform player;
    public Rigidbody playerRigidbody;
    public PlayerController playerController;
    public GameObject playerVisualRoot;

    [Header("Light Input References")]
    public LightAngleController lightAngleController;
    public CameraLocalWashLightController localWashLightController;

    [Header("Respawn")]
    public Transform respawnPoint;
    public float fallYThreshold = -3.5f;

    [Header("Death Visual")]
    public GameObject deathVisualRoot;
    public Animator deathAnimator;
    public string deathAnimationStateName = "Stickman_Death";
    public Vector3 deathSpawnOffset = new Vector3(0f, 2.4f, 0f);
    public Vector3 deathLandOffset = new Vector3(0f, 0.5f, 0f);
    public float cameraReturnDelay = 0.65f;
    public float deathDropDuration = 0.75f;
    public float deathHoldAfterLanding = 0.9f;
    public AnimationCurve deathDropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Failure Menu")]
    public GameObject failureMenuRoot;
    public bool pauseWhenMenuOpens = true;
    public string mainMenuSceneName = "MainMenu";

    private Vector3 cachedRespawnPosition;
    private Quaternion cachedRespawnRotation;
    private bool isFailureSequenceRunning;

    /// <summary>
    /// Purpose: Initializes the failure controller when the scene starts.
    /// Input: Scene references assigned in the Inspector.
    /// Output: Hides failure-only objects and stores the respawn transform.
    /// </summary>
    void Start()
    {
        Time.timeScale = 1f;
        CacheRespawnTransform();

        if (deathVisualRoot != null)
        {
            deathVisualRoot.SetActive(false);
        }

        if (failureMenuRoot != null)
        {
            failureMenuRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Purpose: Checks whether the player has fallen out of the playable area.
    /// Input: Current player world position.
    /// Output: Starts the failure sequence once when the player falls below the threshold.
    /// </summary>
    void Update()
    {
        if (isFailureSequenceRunning || player == null)
        {
            return;
        }

        if (player.position.y < fallYThreshold)
        {
            StartCoroutine(HandleFailureSequence());
        }
    }

    /// <summary>
    /// Purpose: Stores the intended respawn position and rotation.
    /// Input: RespawnPoint transform if assigned; otherwise the player's initial transform.
    /// Output: Cached respawn position and rotation used during failure recovery.
    /// </summary>
    void CacheRespawnTransform()
    {
        if (respawnPoint != null)
        {
            cachedRespawnPosition = respawnPoint.position;
            cachedRespawnRotation = respawnPoint.rotation;
            return;
        }

        if (player != null)
        {
            cachedRespawnPosition = player.position;
            cachedRespawnRotation = player.rotation;
        }
    }

    /// <summary>
    /// Purpose: Runs the full failure sequence from fall detection to menu display.
    /// Input: Current player state and assigned scene references.
    /// Output: Locks input, resets the hidden player, plays death visual feedback, waits briefly, and opens the failure menu.
    /// </summary>
    IEnumerator HandleFailureSequence()
    {
        isFailureSequenceRunning = true;

        LockGameplayInput();
        HidePlayerVisual();
        ResetPlayerToRespawn();

        yield return new WaitForSeconds(cameraReturnDelay);
        yield return PlayDeathDropVisual();
        yield return new WaitForSeconds(deathHoldAfterLanding);

        ShowFailureMenu();
    }

    /// <summary>
    /// Purpose: Prevents the player and light controls from continuing during the failure sequence.
    /// Input: PlayerController, LightAngleController, and CameraLocalWashLightController references.
    /// Output: Gameplay input is disabled until the scene is restarted or changed.
    /// </summary>
    void LockGameplayInput()
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

    /// <summary>
    /// Purpose: Moves the hidden player back to the start position so the camera follows back naturally.
    /// Input: Cached respawn position and player Rigidbody.
    /// Output: Player transform and velocity are reset.
    /// </summary>
    void ResetPlayerToRespawn()
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        if (player != null)
        {
            player.position = cachedRespawnPosition;
            player.rotation = cachedRespawnRotation;
        }
    }

    /// <summary>
    /// Purpose: Hides the normal player sprite during the death feedback.
    /// Input: Player visual root GameObject.
    /// Output: Normal player visual is hidden while the player object remains active for camera follow.
    /// </summary>
    void HidePlayerVisual()
    {
        if (playerVisualRoot != null)
        {
            playerVisualRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Purpose: Plays the separate death stickman visual falling from above the respawn point.
    /// Input: Death visual object, death animator, respawn position, and timing settings.
    /// Output: Death visual appears, falls into place, and plays its death animation.
    /// </summary>
    IEnumerator PlayDeathDropVisual()
    {
        if (deathVisualRoot == null)
        {
            yield break;
        }

        Transform deathTransform = deathVisualRoot.transform;
        Vector3 endPosition = cachedRespawnPosition + deathLandOffset;
        Vector3 startPosition = endPosition + deathSpawnOffset;

        deathTransform.position = startPosition;
        deathTransform.rotation = Quaternion.identity;
        deathTransform.localScale = Vector3.one;

        deathVisualRoot.SetActive(true);

        if (deathAnimator != null && !string.IsNullOrWhiteSpace(deathAnimationStateName))
        {
            deathAnimator.Play(deathAnimationStateName, 0, 0f);
        }

        float elapsedTime = 0f;

        while (elapsedTime < deathDropDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / deathDropDuration);
            float curvedProgress = deathDropCurve.Evaluate(progress);

            deathTransform.position = Vector3.Lerp(startPosition, endPosition, curvedProgress);

            yield return null;
        }

        deathTransform.position = endPosition;
    }

    /// <summary>
    /// Purpose: Displays the failure menu after the death visual sequence finishes.
    /// Input: Failure menu root GameObject.
    /// Output: Restart and Main Menu choices become visible.
    /// </summary>
    void ShowFailureMenu()
    {
        if (failureMenuRoot != null)
        {
            failureMenuRoot.SetActive(true);
        }

        if (pauseWhenMenuOpens)
        {
            Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// Purpose: Restarts the current level from the beginning.
    /// Input: Restart button click.
    /// Output: Reloads the active scene.
    /// </summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Purpose: Returns the player to the main menu scene.
    /// Input: Main Menu button click.
    /// Output: Loads the configured main menu scene if one has been set.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning("Main menu scene name is empty. Set it in Level01FailureController before using this button.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}