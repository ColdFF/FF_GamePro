using UnityEngine;

/// <summary>
/// Purpose: Keeps the stage wash light inside the current camera view and lets the player adjust it locally with arrow keys.
/// Input: Camera position, arrow keys, and Inspector settings for where the light should appear.
/// Output: Moves the wash light smoothly on screen without changing the playable shadow platforms.
/// </summary>
public class CameraLocalWashLightController : MonoBehaviour
{
    [Header("Input Lock")]
    // Allows other scripts or the Inspector to temporarily disable arrow-key wash-light adjustment.
    public bool acceptInput = true;
    
    [Header("References")]
    // Camera whose viewport is used to decide where the wash light should appear on screen.
    public Camera targetCamera;
    // Transform that will be moved; usually this light rig, so child light objects follow it.
    public Transform controlledTransform;

    [Header("Base Viewport Position")]
    [Range(0f, 1f)]
    // Default horizontal screen position for the light, from 0 on the left to 1 on the right.
    public float baseViewportX = 0.28f;

    [Range(0f, 1f)]
    // Default vertical screen position for the light, from 0 at the bottom to 1 at the top.
    public float baseViewportY = 0.34f;

    [Header("Local Light Adjustment")]
    // How fast left/right arrow input changes the viewport offset.
    public float horizontalInputSpeed = 0.18f;
    // How fast up/down arrow input changes the viewport offset.
    public float verticalInputSpeed = 0.16f;
    // Maximum left/right distance the player can move the light away from its base viewport position.
    public float maxHorizontalViewportOffset = 0.12f;
    // Maximum up/down distance the player can move the light away from its base viewport position.
    public float maxVerticalViewportOffset = 0.10f;

    [Header("Viewport Safety")]
    [Range(0f, 0.45f)]
    // Keeps the light away from the screen edges after input and base position are combined.
    public float viewportMargin = 0.08f;

    [Header("Motion Smoothing")]
    // How gently the screen offset catches up to player input; higher feels slower and smoother.
    public float viewportSmoothTime = 0.12f;
    // How gently the actual light object catches up to its target position.
    public float positionSmoothTime = 0.16f;

    [Header("Input Direction")]
    // Reverses left/right input if a scene needs the control direction flipped.
    public bool invertHorizontal = false;
    // Reverses up/down input if a scene needs the control direction flipped.
    public bool invertVertical = false;

    private Vector2 targetViewportOffset;
    private Vector2 currentViewportOffset;
    private Vector2 viewportOffsetVelocity;
    private Vector3 positionVelocity;
    private float lockedWorldZ;

    // Purpose: Sets up which camera to follow and which light object to move.
    // Input: Camera and light references from the Inspector, or automatic choices if they are empty.
    // Output: Saves the light's starting depth so it only moves around the screen, not toward or away from it.
    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (controlledTransform == null)
        {
            controlledTransform = transform;
        }

        if (controlledTransform != null)
        {
            lockedWorldZ = controlledTransform.position.z;
        }
    }

    // Purpose: Updates the wash light after the camera has finished moving for this frame.
    // Input: Current arrow-key input and current camera position.
    // Output: Keeps the wash light in the intended place on the game view.
    void LateUpdate()
    {
        if (targetCamera == null || controlledTransform == null)
        {
            return;
        }

        ReadLightInput();
        SmoothViewportOffset();
        MoveLightToCameraViewportPosition();
    }

    // Purpose: Checks whether the player is pressing arrow keys to nudge the wash light.
    // Input: Left, right, up, and down arrow keys.
    // Output: Stores how far the player wants to move the light on screen.
    void ReadLightInput()
    {

        if (!acceptInput)
        {
            return;
        }
        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            horizontalInput -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            horizontalInput += 1f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            verticalInput -= 1f;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            verticalInput += 1f;
        }

        if (invertHorizontal)
        {
            horizontalInput *= -1f;
        }

        if (invertVertical)
        {
            verticalInput *= -1f;
        }

        targetViewportOffset.x += horizontalInput * horizontalInputSpeed * Time.deltaTime;
        targetViewportOffset.y += verticalInput * verticalInputSpeed * Time.deltaTime;

        targetViewportOffset.x = Mathf.Clamp(
            targetViewportOffset.x,
            -maxHorizontalViewportOffset,
            maxHorizontalViewportOffset
        );

        targetViewportOffset.y = Mathf.Clamp(
            targetViewportOffset.y,
            -maxVerticalViewportOffset,
            maxVerticalViewportOffset
        );
    }

    // Purpose: Makes the arrow-key offset change gradually instead of jumping instantly.
    // Input: The wanted screen offset.
    // Output: A softer version of that offset, so the light does not jump.
    void SmoothViewportOffset()
    {
        currentViewportOffset = Vector2.SmoothDamp(
            currentViewportOffset,
            targetViewportOffset,
            ref viewportOffsetVelocity,
            viewportSmoothTime
        );
    }

    // Purpose: Turns the desired screen position into a real Unity scene position.
    // Input: The wanted screen position and the saved light depth.
    // Output: Moves the actual light object to the matching place in the Unity scene.
    void MoveLightToCameraViewportPosition()
    {
        float viewportX = baseViewportX + currentViewportOffset.x;
        float viewportY = baseViewportY + currentViewportOffset.y;

        viewportX = Mathf.Clamp(viewportX, viewportMargin, 1f - viewportMargin);
        viewportY = Mathf.Clamp(viewportY, viewportMargin, 1f - viewportMargin);

        float cameraDepth = lockedWorldZ - targetCamera.transform.position.z;

        Vector3 targetWorldPosition = targetCamera.ViewportToWorldPoint(
            new Vector3(viewportX, viewportY, cameraDepth)
        );

        targetWorldPosition.z = lockedWorldZ;

        controlledTransform.position = Vector3.SmoothDamp(
            controlledTransform.position,
            targetWorldPosition,
            ref positionVelocity,
            positionSmoothTime
        );
    }

    // Purpose: Fixes unsafe Inspector values while editing.
    // Input: Values typed or changed in the Inspector.
    // Output: Keeps those values valid so the script behaves safely.
    void OnValidate()
    {
        horizontalInputSpeed = Mathf.Max(0f, horizontalInputSpeed);
        verticalInputSpeed = Mathf.Max(0f, verticalInputSpeed);
        maxHorizontalViewportOffset = Mathf.Max(0f, maxHorizontalViewportOffset);
        maxVerticalViewportOffset = Mathf.Max(0f, maxVerticalViewportOffset);
        viewportSmoothTime = Mathf.Max(0.01f, viewportSmoothTime);
        positionSmoothTime = Mathf.Max(0.01f, positionSmoothTime);
    }
}
