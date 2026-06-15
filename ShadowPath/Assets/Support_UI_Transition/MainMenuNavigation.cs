using TMPro;
using UnityEngine;

/// <summary>
/// Purpose: Provides button-call entry points for the main menu.
/// Input: Main menu UI button clicks and level-select UI button clicks.
/// Output: Switches between menu panels, starts the game through chapter transitions, or exits the application.
/// </summary>
public class MainMenuNavigation : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuRoot;
    public GameObject levelSelectRoot;
    public GameObject instructionsPopupRoot;

    [Header("Instructions Pages")]
    public GameObject instructionsGoalPageRoot;
    public GameObject instructionsControlsPageRoot;
    public TMP_Text instructionsPageButtonLabel;

    [Header("Menu-Only Objects")]
    public GameObject[] hideWhenLevelSelectOpens;
    public GameObject[] showOnlyWhenLevelSelectOpens;

    [Header("Scene Names")]
    public string level01SceneName = ChapterTransitionManager.Level01SceneName;
    public string level02SceneName = ChapterTransitionManager.Level02SceneName;
    public string level03SceneName = ChapterTransitionManager.Level03SceneName;
    public string level04SceneName = ChapterTransitionManager.Level04SceneName;

    private bool showingInstructionsControls;

    /// <summary>
    /// Purpose: Ensures the main menu starts on the primary button panel.
    /// Input: Optional main menu and level-select panel references.
    /// Output: Main menu is visible and level select is hidden when the scene starts.
    /// </summary>
    void Start()
    {
        ShowMainMenu();
    }

    /// <summary>
    /// Purpose: Lets Esc close the instructions popup without changing menu flow.
    /// Input: Keyboard Escape while the instructions popup is visible.
    /// Output: Closes the instructions popup and returns to the primary main menu.
    /// </summary>
    void Update()
    {
        if (instructionsPopupRoot != null && instructionsPopupRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInstructions();
        }
    }

    /// <summary>
    /// Purpose: Starts the full game flow from the first level.
    /// Input: Start button click.
    /// Output: Plays the Level 01 chapter transition and loads the first scene.
    /// </summary>
    public void StartGame()
    {
        ChapterTransitionManager.StartGame();
    }

    /// <summary>
    /// Purpose: Opens the level-select panel from the main menu.
    /// Input: Level Select button click.
    /// Output: Hides the primary menu buttons and shows the four level choices.
    /// </summary>
    public void OpenLevelSelect()
    {
        SetPanelActive(instructionsPopupRoot, false);
        SetPanelActive(mainMenuRoot, false);
        SetPanelActive(levelSelectRoot, true);
        SetObjectsActive(hideWhenLevelSelectOpens, false);
        SetObjectsActive(showOnlyWhenLevelSelectOpens, true);
    }

    /// <summary>
    /// Purpose: Returns from level select to the primary main menu.
    /// Input: Back button click.
    /// Output: Hides the level-select panel and shows the primary menu buttons.
    /// </summary>
    public void CloseLevelSelect()
    {
        ShowMainMenu();
    }

    /// <summary>
    /// Purpose: Opens the instructions popup from the main menu.
    /// Input: Instructions button click.
    /// Output: Shows the instructions overlay while keeping the main menu in the background.
    /// </summary>
    public void OpenInstructions()
    {
        SetPanelActive(mainMenuRoot, true);
        SetPanelActive(levelSelectRoot, false);
        SetPanelActive(instructionsPopupRoot, true);
        ShowInstructionsGoalPage();
        SetObjectsActive(hideWhenLevelSelectOpens, true);
        SetObjectsActive(showOnlyWhenLevelSelectOpens, false);
    }

    /// <summary>
    /// Purpose: Switches the instructions popup between the gameplay overview and controls pages.
    /// Input: Page-turn button click.
    /// Output: Shows the other instructions page and updates the page-turn button label.
    /// </summary>
    public void ToggleInstructionsPage()
    {
        if (showingInstructionsControls)
        {
            ShowInstructionsGoalPage();
            return;
        }

        ShowInstructionsControlsPage();
    }

    /// <summary>
    /// Purpose: Shows the Goal and Core Mechanic page.
    /// Input: Instructions open or page-turn button from the controls page.
    /// Output: First instructions page is visible and the page-turn button reads NEXT.
    /// </summary>
    public void ShowInstructionsGoalPage()
    {
        showingInstructionsControls = false;
        SetPanelActive(instructionsGoalPageRoot, true);
        SetPanelActive(instructionsControlsPageRoot, false);
        SetInstructionsPageButtonLabel("NEXT");
    }

    /// <summary>
    /// Purpose: Shows the Controls page.
    /// Input: Page-turn button from the Goal and Core Mechanic page.
    /// Output: Second instructions page is visible and the page-turn button reads BACK.
    /// </summary>
    public void ShowInstructionsControlsPage()
    {
        showingInstructionsControls = true;
        SetPanelActive(instructionsGoalPageRoot, false);
        SetPanelActive(instructionsControlsPageRoot, true);
        SetInstructionsPageButtonLabel("BACK");
    }

    /// <summary>
    /// Purpose: Updates the page-turn button text if it has been assigned.
    /// Input: Desired button label.
    /// Output: Page-turn button label matches the visible page state.
    /// </summary>
    void SetInstructionsPageButtonLabel(string labelText)
    {
        if (instructionsPageButtonLabel != null)
        {
            instructionsPageButtonLabel.text = labelText;
        }
    }

    /// <summary>
    /// Purpose: Closes the instructions popup.
    /// Input: Close button click or Escape key.
    /// Output: Returns to the normal main menu without loading or changing scenes.
    /// </summary>
    public void CloseInstructions()
    {
        SetPanelActive(instructionsPopupRoot, false);
        SetPanelActive(mainMenuRoot, true);
    }

    /// <summary>
    /// Purpose: Shows the primary main menu panel.
    /// Input: Scene start or Back button click.
    /// Output: Main menu is visible and level select is hidden.
    /// </summary>
    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuRoot, true);
        SetPanelActive(levelSelectRoot, false);
        SetPanelActive(instructionsPopupRoot, false);
        SetObjectsActive(hideWhenLevelSelectOpens, true);
        SetObjectsActive(showOnlyWhenLevelSelectOpens, false);
    }

    /// <summary>
    /// Purpose: Loads the first level from the level-select panel.
    /// Input: Level 1 button click.
    /// Output: Plays the Level 01 chapter transition and loads the tutorial scene.
    /// </summary>
    public void LoadLevel01()
    {
        LoadLevel(level01SceneName);
    }

    /// <summary>
    /// Purpose: Loads the second level from the level-select panel.
    /// Input: Level 2 button click.
    /// Output: Plays the Level 02 chapter transition and loads the hidden-door scene.
    /// </summary>
    public void LoadLevel02()
    {
        LoadLevel(level02SceneName);
    }

    /// <summary>
    /// Purpose: Loads the third level from the level-select panel.
    /// Input: Level 3 button click.
    /// Output: Plays the Level 03 chapter transition and loads the rope-tower scene.
    /// </summary>
    public void LoadLevel03()
    {
        LoadLevel(level03SceneName);
    }

    /// <summary>
    /// Purpose: Loads the fourth level from the level-select panel.
    /// Input: Level 4 button click.
    /// Output: Plays the Level 04 chapter transition and loads the dual-light scene.
    /// </summary>
    public void LoadLevel04()
    {
        LoadLevel(level04SceneName);
    }

    /// <summary>
    /// Purpose: Loads a requested level through the existing chapter transition flow.
    /// Input: Scene name from a level-select button.
    /// Output: Starts the matching chapter transition if the scene is configured.
    /// </summary>
    public void LoadLevel(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Cannot load a level because the scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("Cannot load level because it is not in Build Settings: " + sceneName);
            return;
        }

        ChapterTransitionManager.LoadSceneWithChapter(sceneName);
    }

    /// <summary>
    /// Purpose: Handles the Exit button from the main menu.
    /// Input: Exit button click.
    /// Output: Stops play mode in the Editor or quits the built application.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Purpose: Safely toggles an optional menu panel reference.
    /// Input: Panel GameObject and target visibility.
    /// Output: Assigned panels are activated or hidden without requiring every field to be set.
    /// </summary>
    void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    /// <summary>
    /// Purpose: Toggles a group of optional menu decoration objects.
    /// Input: GameObject array and target visibility.
    /// Output: Assigned objects are activated or hidden together with the menu state.
    /// </summary>
    void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }
}
