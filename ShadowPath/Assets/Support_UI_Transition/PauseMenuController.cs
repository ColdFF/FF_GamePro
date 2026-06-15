using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Purpose: Controls a manually built pause menu for gameplay scenes.
/// Input: R key, Continue button click, Main Menu button click, and optional tutorial popup state.
/// Output: Gameplay pauses while the pause menu is visible, resumes on request, or returns to the main menu.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pauseMenuRoot;
    public GameObject pauseIconButton;
    public GameObject instructionIconButton;
    public KeyCode pauseKey = KeyCode.R;

    [Header("Scene Flow")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Optional Blocking UI")]
    public GameObject tutorialPopupRoot;
    public bool ignorePauseKeyWhenTutorialIsOpen = true;

    private float previousTimeScale = 1f;
    private bool isPausedByMenu;

    // Purpose: Prepares the pause menu state when the level starts.
    // Input: Assigned pause menu root.
    // Output: Pause menu starts hidden and gameplay starts unpaused by this controller.
    void Start()
    {
        AutoFindOptionalIconButtons();

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
        }

        UpdateOverlayIconVisibility();
    }

    // Purpose: Reads the pause key even while gameplay time is stopped.
    // Input: Keyboard input and current menu/tutorial state.
    // Output: Toggles the pause menu when the R key is pressed.
    void Update()
    {
        if (!Input.GetKeyDown(pauseKey))
        {
            return;
        }

        TogglePause();
    }

    // Purpose: Keeps gameplay paused while the pause menu remains visible.
    // Input: Current pause menu state and any other script changing Time.timeScale.
    // Output: Time scale stays at zero until the pause menu is closed.
    void LateUpdate()
    {
        ResolveOverlayConflict();
        UpdateOverlayIconVisibility();

        if (!isPausedByMenu)
        {
            return;
        }

        if (pauseMenuRoot != null && !pauseMenuRoot.activeInHierarchy)
        {
            ResumeGame();
            return;
        }

        Time.timeScale = 0f;
    }

    // Purpose: Switches between paused and resumed gameplay.
    // Input: R key press.
    // Output: Opens the pause menu if gameplay can pause, or closes it if it is already open.
    public void TogglePause()
    {
        if (isPausedByMenu)
        {
            ResumeGame();
            return;
        }

        if (!CanOpenPauseMenu())
        {
            return;
        }

        PauseGame();
    }

    // Purpose: Opens the pause menu and freezes gameplay.
    // Input: Pause key or external UI event.
    // Output: Pause menu is visible and Time.timeScale is set to zero.
    public void PauseGame()
    {
        if (isPausedByMenu)
        {
            return;
        }

        if (!CanOpenPauseMenu())
        {
            return;
        }

        previousTimeScale = Time.timeScale;

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(true);
        }

        Time.timeScale = 0f;
        isPausedByMenu = true;
    }

    // Purpose: Closes the pause menu and restores gameplay.
    // Input: Continue button click or R key while paused.
    // Output: Pause menu is hidden and Time.timeScale returns to its previous nonzero value.
    public void ResumeGame()
    {
        if (!isPausedByMenu)
        {
            return;
        }

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
        }

        Time.timeScale = Mathf.Approximately(previousTimeScale, 0f) ? 1f : previousTimeScale;
        isPausedByMenu = false;
    }

    // Purpose: Leaves the current level and returns to the main menu scene.
    // Input: Main Menu button click.
    // Output: Gameplay time is restored and the configured main menu scene is loaded.
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        isPausedByMenu = false;

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning("Cannot return to main menu because mainMenuSceneName is empty.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Purpose: Checks whether the pause menu should open right now.
    // Input: Optional tutorial popup visibility and pause menu assignment.
    // Output: True when the R key is allowed to open the pause menu.
    private bool CanOpenPauseMenu()
    {
        if (pauseMenuRoot == null)
        {
            Debug.LogWarning("Pause menu cannot open because pauseMenuRoot is not assigned.");
            return false;
        }

        if (ignorePauseKeyWhenTutorialIsOpen &&
            tutorialPopupRoot != null &&
            tutorialPopupRoot.activeInHierarchy)
        {
            return false;
        }

        return true;
    }

    // Purpose: Prevents the instruction popup and pause menu from staying open at the same time.
    // Input: Current pause menu state and tutorial popup visibility.
    // Output: Pause menu wins while paused, and tutorial popup wins when pause was not requested.
    private void ResolveOverlayConflict()
    {
        if (pauseMenuRoot == null || tutorialPopupRoot == null)
        {
            return;
        }

        if (!pauseMenuRoot.activeInHierarchy || !tutorialPopupRoot.activeInHierarchy)
        {
            return;
        }

        if (isPausedByMenu)
        {
            tutorialPopupRoot.SetActive(false);
        }
        else
        {
            pauseMenuRoot.SetActive(false);
        }
    }

    // Purpose: Shows only the icon that is allowed in the current overlay state.
    // Input: Pause icon, instruction icon, tutorial popup visibility, and pause menu state.
    // Output: R is hidden during instructions or pause, and ? is hidden during instructions or pause.
    private void UpdateOverlayIconVisibility()
    {
        bool tutorialIsOpen = tutorialPopupRoot != null && tutorialPopupRoot.activeInHierarchy;
        bool pauseMenuIsOpen = isPausedByMenu || (pauseMenuRoot != null && pauseMenuRoot.activeInHierarchy);
        bool shouldShowGameplayIcons = !tutorialIsOpen && !pauseMenuIsOpen;

        if (pauseIconButton == null)
        {
            pauseIconButton = FindSceneObjectByName("PauseIconButton");
        }

        if (instructionIconButton == null)
        {
            instructionIconButton = FindSceneObjectByName("HintIconButton");
        }

        if (pauseIconButton != null)
        {
            pauseIconButton.SetActive(shouldShowGameplayIcons);
        }

        if (instructionIconButton != null)
        {
            instructionIconButton.SetActive(shouldShowGameplayIcons);
        }
    }

    // Purpose: Finds optional icon buttons when they were not assigned in the Inspector.
    // Input: Current scene objects and expected icon object names.
    // Output: Pause and instruction icon references are filled when matching objects exist.
    private void AutoFindOptionalIconButtons()
    {
        if (pauseIconButton == null)
        {
            pauseIconButton = FindSceneObjectByName("PauseIconButton");
        }

        if (instructionIconButton == null)
        {
            instructionIconButton = FindSceneObjectByName("HintIconButton");
        }
    }

    // Purpose: Finds an active or inactive GameObject in the loaded scene by name.
    // Input: Object name to search for.
    // Output: Matching scene object, or null if it cannot be found.
    private GameObject FindSceneObjectByName(string objectName)
    {
        Transform[] sceneTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform candidate = sceneTransforms[i];
            if (candidate == null || candidate.gameObject == null)
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (candidate.name == objectName)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }
}
