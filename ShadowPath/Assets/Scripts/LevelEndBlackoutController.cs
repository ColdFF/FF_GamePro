using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Purpose: Plays the level-end blackout after the camera returns to the overview shot.
/// Input: Scene lights, blackout timing, and an optional CanvasGroup.
/// Output: Fades gameplay lights to black and fades a full-screen black overlay in.
/// </summary>
public class LevelEndBlackoutController : MonoBehaviour
{
    [Header("Lights")]
    public Light[] lightsToFade;
    public AnimationCurve lightFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float lightFadeDuration = 0.8f;

    [Header("Black Screen")]
    public bool createRuntimeBlackoutCanvas = true;
    public CanvasGroup blackoutCanvasGroup;
    public Color blackoutColor = Color.black;
    public float screenFadeDelay = 0.2f;
    public float screenFadeDuration = 0.6f;
    public AnimationCurve screenFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Timing")]
    public float delayAfterOverviewReturn = 0.4f;
    public bool playOnlyOnce = true;

    [Header("Blackout Audio")]
    public AudioSource blackoutAudioSource;
    public AudioClip lightsOffSound;
    [Range(0f, 1f)] public float lightsOffSoundVolume = 0.7f;

    [Header("Success Menu")]
    public bool showSuccessMenuAfterBlackout = true;
    public string successTitle = "LEVEL COMPLETE";
    public string restartButtonLabel = "Restart";
    public string nextLevelButtonLabel = "Next Level";
    public string mainMenuButtonLabel = "Main Menu";
    public string nextLevelSceneName = "";
    public string mainMenuSceneName = "MainMenu";
    public bool useChapterTransitionForNextLevel = true;
    public UnityEvent onRestartSelected;
    public UnityEvent onNextLevelSelected;
    public UnityEvent onMainMenuSelected;

    private float[] originalLightIntensities;
    private Canvas blackoutCanvas;
    private GameObject successMenuObject;
    private Coroutine blackoutRoutine;
    private bool hasPlayed;

    /// <summary>
    /// Purpose: Initializes light intensity memory and the optional black overlay.
    /// Input: Assigned lights and optional CanvasGroup.
    /// Output: Blackout is ready but invisible.
    /// </summary>
    void Awake()
    {
        CacheOriginalLightIntensities();
        PrepareBlackoutCanvas();
        SetBlackoutAlpha(0f);
    }

    /// <summary>
    /// Purpose: Starts the blackout sequence from camera or level flow scripts.
    /// Input: Current light intensities and timing settings.
    /// Output: Lights fade down and black overlay fades in.
    /// </summary>
    public void PlayBlackout()
    {
        if (playOnlyOnce && hasPlayed)
        {
            return;
        }

        if (blackoutRoutine != null)
        {
            StopCoroutine(blackoutRoutine);
        }

        blackoutRoutine = StartCoroutine(PlayBlackoutRoutine());
    }

    /// <summary>
    /// Purpose: Runs the blackout sequence.
    /// Input: Delay, light fade duration, screen fade delay, and screen fade duration.
    /// Output: Scene reaches a stable black-screen ending.
    /// </summary>
    IEnumerator PlayBlackoutRoutine()
    {
        hasPlayed = true;
        CacheOriginalLightIntensities();
        PrepareBlackoutCanvas();

        if (delayAfterOverviewReturn > 0f)
        {
            yield return new WaitForSeconds(delayAfterOverviewReturn);
        }

        PlayBlackoutSound();

        float totalDuration = Mathf.Max(
            Mathf.Max(0.01f, lightFadeDuration),
            Mathf.Max(0f, screenFadeDelay) + Mathf.Max(0.01f, screenFadeDuration)
        );

        float elapsedTime = 0f;

        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;

            UpdateLightFade(elapsedTime);
            UpdateScreenFade(elapsedTime);

            yield return null;
        }

        SetLightFade(1f);
        SetBlackoutAlpha(1f);
        ShowSuccessMenu();
        blackoutRoutine = null;
    }

    /// <summary>
    /// Purpose: Plays a short sound when the level-end blackout begins.
    /// Input: Audio clip and AudioSource assigned in the Inspector.
    /// Output: Lights-off sound plays once before the lights fade down.
    /// </summary>
    void PlayBlackoutSound()
    {
        if (lightsOffSound == null)
        {
            return;
        }

        if (blackoutAudioSource == null)
        {
            blackoutAudioSource = GetComponent<AudioSource>();
        }

        if (blackoutAudioSource == null)
        {
            return;
        }

        blackoutAudioSource.PlayOneShot(lightsOffSound, lightsOffSoundVolume);
    }

    /// <summary>
    /// Purpose: Updates light intensity for the current blackout time.
    /// Input: Elapsed blackout time.
    /// Output: Assigned lights move toward zero intensity.
    /// </summary>
    void UpdateLightFade(float elapsedTime)
    {
        float progress = Mathf.Clamp01(elapsedTime / Mathf.Max(0.01f, lightFadeDuration));
        SetLightFade(lightFadeCurve.Evaluate(progress));
    }

    /// <summary>
    /// Purpose: Updates black overlay alpha for the current blackout time.
    /// Input: Elapsed blackout time and configured screen fade delay.
    /// Output: Overlay fades in after its delay.
    /// </summary>
    void UpdateScreenFade(float elapsedTime)
    {
        if (elapsedTime < screenFadeDelay)
        {
            SetBlackoutAlpha(0f);
            return;
        }

        float progress = Mathf.Clamp01(
            (elapsedTime - screenFadeDelay) / Mathf.Max(0.01f, screenFadeDuration)
        );

        SetBlackoutAlpha(screenFadeCurve.Evaluate(progress));
    }

    /// <summary>
    /// Purpose: Applies normalized fade amount to assigned lights.
    /// Input: Fade progress from 0 to 1.
    /// Output: Light intensities are lerped from original values to zero.
    /// </summary>
    void SetLightFade(float progress)
    {
        if (lightsToFade == null || originalLightIntensities == null)
        {
            return;
        }

        int count = Mathf.Min(lightsToFade.Length, originalLightIntensities.Length);

        for (int i = 0; i < count; i++)
        {
            if (lightsToFade[i] == null)
            {
                continue;
            }

            lightsToFade[i].intensity = Mathf.Lerp(
                originalLightIntensities[i],
                0f,
                Mathf.Clamp01(progress)
            );
        }
    }

    /// <summary>
    /// Purpose: Applies alpha to the black overlay.
    /// Input: Alpha from 0 to 1.
    /// Output: Overlay becomes invisible or fully black.
    /// </summary>
    void SetBlackoutAlpha(float alpha)
    {
        if (blackoutCanvasGroup == null)
        {
            return;
        }

        blackoutCanvasGroup.alpha = Mathf.Clamp01(alpha);
        blackoutCanvasGroup.blocksRaycasts = alpha > 0.99f;
    }

    /// <summary>
    /// Purpose: Stores the light intensities at the start of the blackout.
    /// Input: Assigned Light references.
    /// Output: Intensities can be faded relative to their current values.
    /// </summary>
    void CacheOriginalLightIntensities()
    {
        if (lightsToFade == null)
        {
            originalLightIntensities = new float[0];
            return;
        }

        originalLightIntensities = new float[lightsToFade.Length];

        for (int i = 0; i < lightsToFade.Length; i++)
        {
            originalLightIntensities[i] =
                lightsToFade[i] != null ? lightsToFade[i].intensity : 0f;
        }
    }

    /// <summary>
    /// Purpose: Creates a runtime black overlay when no CanvasGroup is assigned.
    /// Input: Canvas creation setting and blackout color.
    /// Output: Full-screen black overlay is ready to fade in.
    /// </summary>
    void PrepareBlackoutCanvas()
    {
        if (blackoutCanvasGroup != null || !createRuntimeBlackoutCanvas)
        {
            return;
        }

        GameObject canvasObject = new GameObject("RuntimeBlackoutCanvas");
        canvasObject.transform.SetParent(transform, false);

        blackoutCanvas = canvasObject.AddComponent<Canvas>();
        blackoutCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        blackoutCanvas.sortingOrder = 30000;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        blackoutCanvasGroup = canvasGroup;

        GameObject imageObject = new GameObject("BlackoutImage");
        imageObject.transform.SetParent(canvasObject.transform, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = blackoutColor;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        EnsureEventSystem();
    }

    /// <summary>
    /// Purpose: Shows the success menu after blackout finishes.
    /// Input: Blackout canvas and menu labels.
    /// Output: Title and three buttons appear above the black screen.
    /// </summary>
    void ShowSuccessMenu()
    {
        if (!showSuccessMenuAfterBlackout)
        {
            return;
        }

        EnsureSuccessMenu();
        successMenuObject.SetActive(true);
    }

    /// <summary>
    /// Purpose: Creates the runtime success menu when needed.
    /// Input: Blackout canvas.
    /// Output: Menu hierarchy with title, Restart, optional Next Level, and Main Menu buttons.
    /// </summary>
    void EnsureSuccessMenu()
    {
        if (successMenuObject != null)
        {
            return;
        }

        PrepareBlackoutCanvas();

        if (blackoutCanvas == null && blackoutCanvasGroup != null)
        {
            blackoutCanvas = blackoutCanvasGroup.GetComponent<Canvas>();
        }

        if (blackoutCanvas == null)
        {
            return;
        }

        successMenuObject = new GameObject("SuccessMenu");
        successMenuObject.transform.SetParent(blackoutCanvas.transform, false);

        RectTransform rootRect = successMenuObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = successMenuObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateTitle(successMenuObject.transform);
        CreateButton(successMenuObject.transform, restartButtonLabel, RestartCurrentLevel);
        if (!string.IsNullOrWhiteSpace(nextLevelSceneName))
        {
            CreateButton(successMenuObject.transform, nextLevelButtonLabel, GoToNextLevel);
        }
        CreateButton(successMenuObject.transform, mainMenuButtonLabel, GoToMainMenu);

        successMenuObject.SetActive(false);
    }

    /// <summary>
    /// Purpose: Creates the success title text.
    /// Input: Parent transform.
    /// Output: White title text on the blackout screen.
    /// </summary>
    void CreateTitle(Transform parent)
    {
        GameObject titleObject = new GameObject("SuccessTitle");
        titleObject.transform.SetParent(parent, false);

        Text text = titleObject.AddComponent<Text>();
        text.text = successTitle;
        text.font = GetBuiltinUiFont();
        text.fontSize = 48;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.92f, 0.92f, 0.9f, 1f);

        Shadow shadow = titleObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(2f, -2f);

        RectTransform rectTransform = titleObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(620f, 80f);
    }

    /// <summary>
    /// Purpose: Creates one menu button.
    /// Input: Parent transform, visible label, and click action.
    /// Output: Clickable UI button on the success menu.
    /// </summary>
    void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label + "Button");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.02f, 0.02f, 0.02f, 0.86f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.32f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.enabled = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);

        SuccessMenuButtonHoverFeedback hoverFeedback =
            buttonObject.AddComponent<SuccessMenuButtonHoverFeedback>();
        hoverFeedback.target = buttonObject.GetComponent<RectTransform>();
        hoverFeedback.background = image;
        hoverFeedback.outline = outline;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(330f, 54f);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        Text text = labelObject.AddComponent<Text>();
        text.text = label;
        text.font = GetBuiltinUiFont();
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.9f, 0.9f, 0.86f, 1f);

        Shadow labelShadow = labelObject.AddComponent<Shadow>();
        labelShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        labelShadow.effectDistance = new Vector2(1f, -1f);

        hoverFeedback.label = text;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Purpose: Handles Restart button clicks.
    /// Input: Button click and optional UnityEvent listeners.
    /// Output: Restarts current scene by default.
    /// </summary>
    public void RestartCurrentLevel()
    {
        onRestartSelected?.Invoke();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Purpose: Handles Next Level button clicks.
    /// Input: Button click, optional UnityEvent listeners, and the configured next level scene name.
    /// Output: Loads the next level directly or through the educational chapter transition.
    /// </summary>
    public void GoToNextLevel()
    {
        onNextLevelSelected?.Invoke();

        string targetSceneName = nextLevelSceneName;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("Next Level selected, but nextLevelSceneName is empty. Set it on this LevelEndBlackoutController.", this);
            return;
        }

        Time.timeScale = 1f;

        if (useChapterTransitionForNextLevel)
        {
            ChapterTransitionManager.LoadSceneWithChapter(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    /// <summary>
    /// Purpose: Handles Main Menu button clicks.
    /// Input: Button click, optional UnityEvent listeners, and optional scene name.
    /// Output: Loads configured main menu scene when available.
    /// </summary>
    public void GoToMainMenu()
    {
        onMainMenuSelected?.Invoke();

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.Log("Main Menu selected. Set mainMenuSceneName when the menu scene exists.", this);
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Purpose: Ensures runtime UI buttons can receive pointer input.
    /// Input: Current scene EventSystem state.
    /// Output: Existing EventSystem is reused, or one is created if missing.
    /// </summary>
    void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("RuntimeEventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    /// <summary>
    /// Purpose: Gets a Unity built-in font that works in current Unity versions.
    /// Input: None.
    /// Output: Built-in runtime UI font.
    /// </summary>
    Font GetBuiltinUiFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    /// <summary>
    /// Purpose: Keeps timing values in a valid range.
    /// Input: Inspector edits.
    /// Output: Fade durations remain usable.
    /// </summary>
    void OnValidate()
    {
        lightFadeDuration = Mathf.Max(0.01f, lightFadeDuration);
        screenFadeDelay = Mathf.Max(0f, screenFadeDelay);
        screenFadeDuration = Mathf.Max(0.01f, screenFadeDuration);
        delayAfterOverviewReturn = Mathf.Max(0f, delayAfterOverviewReturn);
    }
}

/// <summary>
/// Purpose: Adds menu-style visual and audio feedback to generated success menu buttons.
/// Input: Pointer hover, press, click, and optional UI submit events.
/// Output: Button tint, label color, outline, scale, and UI sounds respond to interaction without staying selected.
/// </summary>
public class SuccessMenuButtonHoverFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler
{
    public RectTransform target;
    public Image background;
    public Text label;
    public Outline outline;
    public float hoverScale = 1.045f;
    public float pressedScale = 0.98f;
    public float animationSpeed = 14f;

    public Color normalBackgroundColor = new Color(0.02f, 0.02f, 0.02f, 0.86f);
    public Color hoverBackgroundColor = new Color(0.18f, 0.18f, 0.17f, 0.92f);
    public Color pressedBackgroundColor = new Color(0.32f, 0.32f, 0.3f, 0.96f);
    public Color normalLabelColor = new Color(0.9f, 0.9f, 0.86f, 1f);
    public Color hoverLabelColor = new Color(1f, 1f, 0.96f, 1f);
    public Color pressedLabelColor = new Color(0.82f, 0.82f, 0.78f, 1f);
    public Color normalOutlineColor = new Color(1f, 1f, 1f, 0.32f);
    public Color hoverOutlineColor = new Color(1f, 1f, 1f, 0.72f);
    public Color pressedOutlineColor = new Color(1f, 1f, 1f, 0.9f);

    public bool playSounds = true;
    public AudioClip hoverSound;
    public AudioClip clickSound;
    [Range(0f, 1f)] public float hoverVolume = 0.28f;
    [Range(0f, 1f)] public float clickVolume = 0.48f;
    public string hoverSoundResourcePath = "Audio/UI/MenuHoverSoft";
    public string clickSoundResourcePath = "Audio/UI/MenuClickConfirm";

    private bool isHovered;
    private bool isPressed;
    private bool suppressHoverUntilPointerExit;
    private Vector3 normalScale = Vector3.one;

    private static AudioSource sharedAudioSource;
    private static AudioClip sharedHoverSound;
    private static AudioClip sharedClickSound;
    private static bool loadedDefaultSounds;

    void Awake()
    {
        CacheReferences();
        CaptureNormalState();
        ApplyVisualsImmediate();
    }

    void OnEnable()
    {
        CacheReferences();
        CaptureNormalState();
        isHovered = false;
        isPressed = false;
        suppressHoverUntilPointerExit = false;
        ApplyVisualsImmediate();
    }

    void OnDisable()
    {
        isHovered = false;
        isPressed = false;
        suppressHoverUntilPointerExit = false;
    }

    void Update()
    {
        float t = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);

        if (target != null)
        {
            target.localScale = Vector3.Lerp(target.localScale, GetTargetScale(), t);
        }

        if (background != null)
        {
            background.color = Color.Lerp(background.color, GetTargetBackgroundColor(), t);
        }

        if (label != null)
        {
            label.color = Color.Lerp(label.color, GetTargetLabelColor(), t);
        }

        if (outline != null)
        {
            outline.effectColor = Color.Lerp(outline.effectColor, GetTargetOutlineColor(), t);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ChapterTransitionManager.IsTransitionPlaying)
        {
            return;
        }

        isHovered = !suppressHoverUntilPointerExit;
        PlayHoverSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        suppressHoverUntilPointerExit = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isPressed = false;
        isHovered = false;
        suppressHoverUntilPointerExit = true;
        PlayClickSound();
        ApplyVisualsImmediate();
    }

    public void OnSelect(BaseEventData eventData)
    {
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        isPressed = false;
        isHovered = false;
        suppressHoverUntilPointerExit = true;
        PlayClickSound();
        ApplyVisualsImmediate();
    }

    void CacheReferences()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }

        if (background == null)
        {
            background = GetComponent<Image>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<Text>(true);
        }

        if (outline == null)
        {
            outline = GetComponent<Outline>();
        }

        if (outline != null)
        {
            outline.enabled = true;
        }
    }

    void CaptureNormalState()
    {
        if (target != null)
        {
            normalScale = target.localScale;
        }

        if (background != null)
        {
            normalBackgroundColor = background.color;
        }

        if (label != null)
        {
            normalLabelColor = label.color;
        }

        if (outline != null)
        {
            normalOutlineColor = outline.effectColor;
        }
    }

    void ApplyVisualsImmediate()
    {
        if (target != null)
        {
            target.localScale = GetTargetScale();
        }

        if (background != null)
        {
            background.color = GetTargetBackgroundColor();
        }

        if (label != null)
        {
            label.color = GetTargetLabelColor();
        }

        if (outline != null)
        {
            outline.effectColor = GetTargetOutlineColor();
        }
    }

    Vector3 GetTargetScale()
    {
        if (isPressed)
        {
            return normalScale * pressedScale;
        }

        return isHovered ? normalScale * hoverScale : normalScale;
    }

    Color GetTargetBackgroundColor()
    {
        if (isPressed)
        {
            return pressedBackgroundColor;
        }

        return isHovered ? hoverBackgroundColor : normalBackgroundColor;
    }

    Color GetTargetLabelColor()
    {
        if (isPressed)
        {
            return pressedLabelColor;
        }

        return isHovered ? hoverLabelColor : normalLabelColor;
    }

    Color GetTargetOutlineColor()
    {
        if (isPressed)
        {
            return pressedOutlineColor;
        }

        return isHovered ? hoverOutlineColor : normalOutlineColor;
    }

    void PlayHoverSound()
    {
        PlaySound(GetHoverSound(), hoverVolume);
    }

    void PlayClickSound()
    {
        PlaySound(GetClickSound(), clickVolume, true);
    }

    void PlaySound(AudioClip clip, float volume, bool stopCurrentSound = false)
    {
        if (!playSounds || clip == null || volume <= 0f)
        {
            return;
        }

        AudioSource audioSource = GetSharedAudioSource();
        if (audioSource == null)
        {
            return;
        }

        if (stopCurrentSound)
        {
            audioSource.Stop();
        }

        audioSource.PlayOneShot(clip, volume);
    }

    AudioClip GetHoverSound()
    {
        LoadDefaultSoundsIfNeeded();
        return hoverSound != null ? hoverSound : sharedHoverSound;
    }

    AudioClip GetClickSound()
    {
        LoadDefaultSoundsIfNeeded();
        return clickSound != null ? clickSound : sharedClickSound;
    }

    void LoadDefaultSoundsIfNeeded()
    {
        if (loadedDefaultSounds)
        {
            return;
        }

        loadedDefaultSounds = true;
        sharedHoverSound = Resources.Load<AudioClip>(hoverSoundResourcePath);
        sharedClickSound = Resources.Load<AudioClip>(clickSoundResourcePath);
    }

    AudioSource GetSharedAudioSource()
    {
        if (sharedAudioSource != null)
        {
            return sharedAudioSource;
        }

        GameObject audioObject = new GameObject("SuccessMenu_UI_Audio");
        DontDestroyOnLoad(audioObject);
        sharedAudioSource = audioObject.AddComponent<AudioSource>();
        sharedAudioSource.playOnAwake = false;
        sharedAudioSource.loop = false;
        sharedAudioSource.spatialBlend = 0f;
        return sharedAudioSource;
    }

    void OnValidate()
    {
        hoverScale = Mathf.Max(0.01f, hoverScale);
        pressedScale = Mathf.Max(0.01f, pressedScale);
        animationSpeed = Mathf.Max(0.01f, animationSpeed);
        hoverVolume = Mathf.Clamp01(hoverVolume);
        clickVolume = Mathf.Clamp01(clickVolume);
    }
}
