using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Purpose: Adds a polished hover, select, and press response to main menu buttons.
/// Input: Pointer events, selection events, optional background Graphic, and optional TextMeshPro label.
/// Output: Smoothly scales the button and tints its background and label to show the current menu choice.
/// </summary>
public class MainMenuButtonHoverEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("References")]
    public RectTransform targetTransform;
    public Graphic backgroundGraphic;
    public TMP_Text label;
    public bool disableBuiltInButtonTransition = true;

    [Header("Scale")]
    public float hoverScale = 1.06f;
    public float pressedScale = 0.98f;
    public float animationSpeed = 12f;

    [Header("Background")]
    public Color normalBackgroundColor = new Color(0.05f, 0.045f, 0.04f, 0.82f);
    public Color hoverBackgroundColor = new Color(0.15f, 0.11f, 0.075f, 0.92f);
    public Color pressedBackgroundColor = new Color(0.2f, 0.13f, 0.07f, 0.96f);

    [Header("Label")]
    public Color normalLabelColor = new Color(0.86f, 0.78f, 0.55f, 1f);
    public Color hoverLabelColor = new Color(1f, 0.92f, 0.68f, 1f);
    public Color pressedLabelColor = new Color(1f, 0.82f, 0.46f, 1f);

    [Header("Selection")]
    public bool selectOnPointerEnter = true;
    public bool keepSelectedHighlighted = true;

    private Vector3 baseScale = Vector3.one;
    private Selectable selectable;
    private bool isHovered;
    private bool isPressed;
    private bool isSelected;

    /// <summary>
    /// Purpose: Finds local UI references when they were not assigned manually.
    /// Input: Components on this button and its children.
    /// Output: Cached transform, background Graphic, and label references for animation.
    /// </summary>
    private void Awake()
    {
        CacheReferences();
        CaptureBaseState();
    }

    /// <summary>
    /// Purpose: Restores the cached neutral visual state whenever the button becomes active.
    /// Input: Current transform and configured normal colors.
    /// Output: Resets interaction flags and applies the normal scale and tint immediately.
    /// </summary>
    private void OnEnable()
    {
        CacheReferences();
        CaptureBaseState();
        isHovered = false;
        isPressed = false;
        isSelected = false;
        ApplyVisualsImmediate();
    }

    /// <summary>
    /// Purpose: Smoothly animates the button toward its current hover, selected, or pressed state.
    /// Input: Interaction flags and configured scale/color settings.
    /// Output: Interpolated transform scale, background tint, and label tint.
    /// </summary>
    private void Update()
    {
        float t = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);
        Vector3 targetScale = GetTargetScale();
        Color targetBackgroundColor = GetTargetBackgroundColor();
        Color targetLabelColor = GetTargetLabelColor();

        if (targetTransform != null)
        {
            targetTransform.localScale = Vector3.Lerp(targetTransform.localScale, targetScale, t);
        }

        if (backgroundGraphic != null)
        {
            backgroundGraphic.color = Color.Lerp(backgroundGraphic.color, targetBackgroundColor, t);
        }

        if (label != null)
        {
            label.color = Color.Lerp(label.color, targetLabelColor, t);
        }
    }

    /// <summary>
    /// Purpose: Marks the button as hovered when the pointer enters its hit area.
    /// Input: Unity pointer event data.
    /// Output: Enables the hover state and optionally makes this button the selected UI object.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        if (selectOnPointerEnter && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    /// <summary>
    /// Purpose: Clears the hover and press states when the pointer leaves the button.
    /// Input: Unity pointer event data.
    /// Output: Restores the button toward selected or normal visuals.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    /// <summary>
    /// Purpose: Marks the button as pressed when the pointer is held down.
    /// Input: Unity pointer event data.
    /// Output: Enables the pressed scale and tint response.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    /// <summary>
    /// Purpose: Clears the pressed state after the pointer is released.
    /// Input: Unity pointer event data.
    /// Output: Returns the button toward hover, selected, or normal visuals.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    /// <summary>
    /// Purpose: Marks the button as selected for keyboard or pointer navigation.
    /// Input: Unity selection event data.
    /// Output: Enables selected highlighting when configured.
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
    }

    /// <summary>
    /// Purpose: Clears the selected state when another UI object is selected.
    /// Input: Unity selection event data.
    /// Output: Returns the button toward hover or normal visuals.
    /// </summary>
    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }

    /// <summary>
    /// Purpose: Locates missing component references without requiring manual setup for every button.
    /// Input: The current GameObject, its RectTransform, child TextMeshPro label, local Graphic components, and optional Selectable.
    /// Output: Populated reference fields and disabled built-in Button tinting when requested.
    /// </summary>
    private void CacheReferences()
    {
        if (targetTransform == null)
        {
            targetTransform = transform as RectTransform;
        }

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }

        if (backgroundGraphic == null)
        {
            Graphic[] graphics = GetComponents<Graphic>();
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != label)
                {
                    backgroundGraphic = graphics[i];
                    break;
                }
            }
        }

        if (selectable == null)
        {
            selectable = GetComponent<Selectable>();
        }

        if (disableBuiltInButtonTransition && selectable != null)
        {
            selectable.transition = Selectable.Transition.None;
        }
    }

    /// <summary>
    /// Purpose: Captures the starting transform scale and current colors as the button's normal state.
    /// Input: Current RectTransform scale, background color, and label color.
    /// Output: Stored base scale and normal color values used by the animation.
    /// </summary>
    private void CaptureBaseState()
    {
        if (targetTransform != null)
        {
            baseScale = targetTransform.localScale;
        }

        if (backgroundGraphic != null)
        {
            normalBackgroundColor = backgroundGraphic.color;
        }

        if (label != null)
        {
            normalLabelColor = label.color;
        }
    }

    /// <summary>
    /// Purpose: Applies the current visual state without animation.
    /// Input: Current interaction flags and configured visual values.
    /// Output: Immediate scale, background tint, and label tint.
    /// </summary>
    private void ApplyVisualsImmediate()
    {
        if (targetTransform != null)
        {
            targetTransform.localScale = GetTargetScale();
        }

        if (backgroundGraphic != null)
        {
            backgroundGraphic.color = GetTargetBackgroundColor();
        }

        if (label != null)
        {
            label.color = GetTargetLabelColor();
        }
    }

    /// <summary>
    /// Purpose: Chooses the desired scale for the current button state.
    /// Input: Hovered, selected, and pressed flags.
    /// Output: Base, hover, or pressed scale.
    /// </summary>
    private Vector3 GetTargetScale()
    {
        if (isPressed)
        {
            return baseScale * pressedScale;
        }

        if (IsHighlighted())
        {
            return baseScale * hoverScale;
        }

        return baseScale;
    }

    /// <summary>
    /// Purpose: Chooses the desired background tint for the current button state.
    /// Input: Hovered, selected, and pressed flags.
    /// Output: Normal, hover, or pressed background color.
    /// </summary>
    private Color GetTargetBackgroundColor()
    {
        if (isPressed)
        {
            return pressedBackgroundColor;
        }

        return IsHighlighted() ? hoverBackgroundColor : normalBackgroundColor;
    }

    /// <summary>
    /// Purpose: Chooses the desired label tint for the current button state.
    /// Input: Hovered, selected, and pressed flags.
    /// Output: Normal, hover, or pressed label color.
    /// </summary>
    private Color GetTargetLabelColor()
    {
        if (isPressed)
        {
            return pressedLabelColor;
        }

        return IsHighlighted() ? hoverLabelColor : normalLabelColor;
    }

    /// <summary>
    /// Purpose: Determines whether the button should look like the current active menu choice.
    /// Input: Hovered state, selected state, and selected-highlight configuration.
    /// Output: True when hover or selected visuals should be shown.
    /// </summary>
    private bool IsHighlighted()
    {
        return isHovered || (keepSelectedHighlighted && isSelected);
    }

    /// <summary>
    /// Purpose: Keeps animation values in useful ranges while editing the menu.
    /// Input: Current serialized field values.
    /// Output: Clamped scale and animation-speed settings.
    /// </summary>
    private void OnValidate()
    {
        hoverScale = Mathf.Max(0.01f, hoverScale);
        pressedScale = Mathf.Max(0.01f, pressedScale);
        animationSpeed = Mathf.Max(0.01f, animationSpeed);
    }
}
