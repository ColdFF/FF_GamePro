using UnityEngine;

/// <summary>
/// Purpose: Provides button-call entry points for the main menu.
/// Input: Main menu UI button clicks.
/// Output: Starts the game through the chapter transition system or exits the application.
/// </summary>
public class MainMenuNavigation : MonoBehaviour
{
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
}
