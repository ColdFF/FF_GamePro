using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Purpose: Shows educational chapter text between scenes and loads the requested level under a black transition.
/// Input: Menu button calls, level-complete button calls, target scene names, and built-in chapter messages.
/// Output: A full-screen black interlude presents the level theme before the next scene becomes playable.
/// </summary>
public class ChapterTransitionManager : MonoBehaviour
{
    public const string MainMenuSceneName = "MainMenu";
    public const string Level01SceneName = "Level01_Tutorial";
    public const string Level02SceneName = "Level02_HiddenDoor";
    public const string Level03SceneName = "Level03_RopeTower";
    public const string Level04SceneName = "Level04_DualLight";

    private const float DefaultFadeDuration = 0.55f;
    private const float TextFadeDuration = 0.8f;
    private const float LineHoldDuration = 2.5f;
    private const float BetweenLineDelay = 0.18f;

    private static ChapterTransitionManager activeTransition;

    private CanvasGroup overlayGroup;
    private TextMeshProUGUI messageText;
    private Coroutine transitionRoutine;

    /// <summary>
    /// Purpose: Reports whether a black chapter transition is currently active.
    /// Input: Current runtime transition manager state.
    /// Output: True while the interlude overlay is still controlling the screen.
    /// </summary>
    public static bool IsTransitionPlaying => activeTransition != null;

    /// <summary>
    /// Purpose: Starts a new game from the main menu.
    /// Input: Start button click.
    /// Output: Plays the Level 01 chapter interlude and loads the first level.
    /// </summary>
    public static void StartGame()
    {
        LoadSceneWithChapter(Level01SceneName);
    }

    /// <summary>
    /// Purpose: Loads the next scene in the designed level order.
    /// Input: Current active scene name.
    /// Output: Plays the next level's chapter interlude or logs when no next level exists.
    /// </summary>
    public static void LoadNextLevelFromCurrentScene()
    {
        string nextSceneName = GetNextSceneName(SceneManager.GetActiveScene().name);
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.Log("No next level is configured for the current scene.");
            return;
        }

        LoadSceneWithChapter(nextSceneName);
    }

    /// <summary>
    /// Purpose: Loads a scene through its educational chapter interlude.
    /// Input: Target scene name.
    /// Output: Creates a transition overlay, shows the target scene's message sequence, and loads the scene.
    /// </summary>
    public static void LoadSceneWithChapter(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Cannot start a chapter transition because the target scene name is empty.");
            return;
        }

        ChapterTransitionManager manager = CreateTransitionManager();
        manager.BeginTransition(sceneName, GetChapterLines(sceneName));
    }

    /// <summary>
    /// Purpose: Returns the next level in the game's intended scene order.
    /// Input: Current scene name.
    /// Output: Next scene name, or an empty string when the current scene is the final level.
    /// </summary>
    public static string GetNextSceneName(string currentSceneName)
    {
        switch (currentSceneName)
        {
            case Level01SceneName:
                return Level02SceneName;
            case Level02SceneName:
                return Level03SceneName;
            case Level03SceneName:
                return Level04SceneName;
            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// Purpose: Chooses the educational message sequence for the target scene.
    /// Input: Target scene name.
    /// Output: Natural English lines shown during the black-screen interlude.
    /// </summary>
    public static string[] GetChapterLines(string sceneName)
    {
        switch (sceneName)
        {
            case Level01SceneName:
                return new[]
                {
                    "'Shadow Path' is more than a game...",
                    "It is a way to experience life.",
                    "Try to see things from different perspectives...",
                    "Level 1 - Tutorial"
                };
            case Level02SceneName:
                return new[]
                {
                    "Before you chase success, find your destination...",
                    "When the path appears, be brave enough to step through...",
                    "Level 2 - Hidden Door"
                };
            case Level03SceneName:
                return new[]
                {
                    "Not every path stands still...",
                    "Move with change. Think dynamically...",
                    "Level 3 - Rope Tower"
                };
            case Level04SceneName:
                return new[]
                {
                    "You have crossed shadows, doors, ropes, and ladders.",
                    "What once felt difficult can become simple.",
                    "But remember: keep seeing things from different perspectives...",
                    "Level 4 - Dual Light"
                };
            default:
                return new[] { "Step into the shadow. Find the path." };
        }
    }

    /// <summary>
    /// Purpose: Creates or replaces the runtime object that owns the transition coroutine.
    /// Input: Existing active transition state.
    /// Output: A persistent manager with an initialized overlay.
    /// </summary>
    private static ChapterTransitionManager CreateTransitionManager()
    {
        if (activeTransition != null)
        {
            Destroy(activeTransition.gameObject);
        }

        GameObject managerObject = new GameObject("ChapterTransitionManager");
        DontDestroyOnLoad(managerObject);
        activeTransition = managerObject.AddComponent<ChapterTransitionManager>();
        activeTransition.CreateOverlay();
        return activeTransition;
    }

    /// <summary>
    /// Purpose: Starts the transition coroutine for a target scene.
    /// Input: Target scene name and the lines to display before loading.
    /// Output: Any previous transition coroutine is stopped and the new one begins.
    /// </summary>
    private void BeginTransition(string sceneName, string[] chapterLines)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(PlayTransition(sceneName, chapterLines));
    }

    /// <summary>
    /// Purpose: Plays the black-screen message sequence, loads the scene, and fades gameplay back in.
    /// Input: Target scene name and chapter message lines.
    /// Output: The target scene is loaded after the educational interlude completes.
    /// </summary>
    private IEnumerator PlayTransition(string sceneName, string[] chapterLines)
    {
        Time.timeScale = 0f;
        overlayGroup.alpha = 0f;
        messageText.alpha = 0f;
        messageText.text = string.Empty;

        yield return FadeOverlay(0f, 1f, DefaultFadeDuration);
        yield return PlayChapterLines(chapterLines);
        yield return LoadTargetScene(sceneName);
        yield return FadeOverlay(1f, 0f, DefaultFadeDuration);

        Time.timeScale = 1f;
        activeTransition = null;
        Destroy(gameObject);
    }

    /// <summary>
    /// Purpose: Displays each chapter line one at a time.
    /// Input: Ordered chapter text lines.
    /// Output: Text fades in, holds, fades out, and clears before the next line.
    /// </summary>
    private IEnumerator PlayChapterLines(string[] chapterLines)
    {
        if (chapterLines == null || chapterLines.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < chapterLines.Length; i++)
        {
            messageText.text = chapterLines[i];
            yield return FadeText(0f, 1f, TextFadeDuration);
            yield return WaitUnscaled(LineHoldDuration);
            yield return FadeText(1f, 0f, TextFadeDuration);
            messageText.text = string.Empty;
            yield return WaitUnscaled(BetweenLineDelay);
        }
    }

    /// <summary>
    /// Purpose: Loads the requested scene while the screen remains black.
    /// Input: Target scene name.
    /// Output: The target scene becomes active, or the transition safely exits if the scene is unavailable.
    /// </summary>
    private IEnumerator LoadTargetScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("Scene is not available in Build Settings: " + sceneName);
            yield break;
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (loadOperation != null && !loadOperation.isDone)
        {
            yield return null;
        }
    }

    /// <summary>
    /// Purpose: Fades the full-screen black overlay.
    /// Input: Starting alpha, ending alpha, and fade duration.
    /// Output: CanvasGroup alpha reaches the requested value using unscaled time.
    /// </summary>
    private IEnumerator FadeOverlay(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));
            overlayGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            yield return null;
        }

        overlayGroup.alpha = toAlpha;
    }

    /// <summary>
    /// Purpose: Fades the message text on the black transition overlay.
    /// Input: Starting alpha, ending alpha, and fade duration.
    /// Output: Text alpha reaches the requested value using unscaled time.
    /// </summary>
    private IEnumerator FadeText(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));
            messageText.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            yield return null;
        }

        messageText.alpha = toAlpha;
    }

    /// <summary>
    /// Purpose: Waits without depending on gameplay time scale.
    /// Input: Duration in seconds.
    /// Output: Coroutine resumes after the requested unscaled duration.
    /// </summary>
    private IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Purpose: Builds the runtime full-screen transition UI.
    /// Input: None.
    /// Output: A persistent black overlay canvas with centered TextMeshPro text.
    /// </summary>
    private void CreateOverlay()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        overlayGroup = gameObject.AddComponent<CanvasGroup>();
        overlayGroup.blocksRaycasts = true;
        overlayGroup.interactable = false;

        CreateBlackBackground(transform);
        messageText = CreateMessageText(transform);
    }

    /// <summary>
    /// Purpose: Creates the black image that covers the screen during transitions.
    /// Input: Parent transform for the overlay canvas.
    /// Output: A stretched black Image behind the message text.
    /// </summary>
    private void CreateBlackBackground(Transform parent)
    {
        GameObject backgroundObject = new GameObject("BlackBackground");
        backgroundObject.transform.SetParent(parent, false);

        RectTransform rectTransform = backgroundObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = backgroundObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
    }

    /// <summary>
    /// Purpose: Creates the centered text used for chapter messages.
    /// Input: Parent transform for the overlay canvas.
    /// Output: Configured TextMeshProUGUI component.
    /// </summary>
    private TextMeshProUGUI CreateMessageText(Transform parent)
    {
        GameObject textObject = new GameObject("ChapterMessage");
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(1500f, 480f);
        rectTransform.anchoredPosition = Vector2.zero;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.fontSize = 88f;
        text.color = new Color(0.92f, 0.86f, 0.72f, 1f);
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }
}
