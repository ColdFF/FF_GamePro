using UnityEngine;

/// <summary>
/// Purpose: Pauses gameplay while a manually built tutorial popup is open.
/// Input: Tutorial popup state and UI button click events.
/// Output: Gameplay time scale pauses when instructions are visible and resumes when they close.
/// </summary>
public class TutorialPopupPauseController : MonoBehaviour
{
    [Header("Popup")]
    public GameObject tutorialPopupRoot;
    public bool pauseOnStartIfPopupIsVisible = true;

    private float previousTimeScale = 1f;
    private bool isPausedByTutorial;

    // Purpose: Pauses gameplay automatically when the level starts with the tutorial popup visible.
    // Input: Assigned tutorial popup root and pause-on-start setting.
    // Output: Gameplay is paused before the player can move if the instruction popup is open.
    void Start()
    {
        if (!pauseOnStartIfPopupIsVisible)
        {
            return;
        }

        if (tutorialPopupRoot == null || tutorialPopupRoot.activeInHierarchy)
        {
            PauseGame();
        }
    }

    // Purpose: Synchronizes gameplay pause state with the tutorial popup visibility.
    // Input: Current tutorial popup active state and any other script changing Time.timeScale.
    // Output: Gameplay pauses whenever the popup is visible and resumes when the popup is hidden.
    void LateUpdate()
    {
        if (tutorialPopupRoot == null)
        {
            return;
        }

        if (tutorialPopupRoot.activeInHierarchy)
        {
            if (!isPausedByTutorial)
            {
                PauseGame();
            }

            Time.timeScale = 0f;
            return;
        }

        if (isPausedByTutorial)
        {
            ResumeGame();
        }
    }

    // Purpose: Pauses gameplay when the tutorial popup is opened.
    // Input: UI button click or scene-start popup state.
    // Output: Stores the current time scale and sets gameplay time to zero.
    public void PauseGame()
    {
        if (isPausedByTutorial)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPausedByTutorial = true;
    }

    // Purpose: Resumes gameplay when the tutorial popup is closed.
    // Input: OK button click from the tutorial popup.
    // Output: Restores the previous time scale or normal speed if the previous value was already paused.
    public void ResumeGame()
    {
        if (!isPausedByTutorial)
        {
            return;
        }

        Time.timeScale = Mathf.Approximately(previousTimeScale, 0f) ? 1f : previousTimeScale;
        isPausedByTutorial = false;
    }
}
