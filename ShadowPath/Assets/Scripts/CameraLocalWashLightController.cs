using UnityEngine;

/// <summary>
/// Purpose: Keeps the stage wash light inside the current camera view and lets the player adjust it locally with arrow keys.
/// Input: Main camera position, arrow key input, and viewport-based light settings.
/// Output: Moves the wash light smoothly within the camera view without changing the core shadow collision system.
/// </summary>
public class CameraLocalWashLightController : MonoBehaviour
{
    [Header("Input Lock")]
    public bool acceptInput = true;
    
    [Header("References")]
    public Camera targetCamera;
    public Transform controlledTransform;

    [Header("Base Viewport Position")]
    [Range(0f, 1f)]
    public float baseViewportX = 0.28f;

    [Range(0f, 1f)]
    public float baseViewportY = 0.34f;

    [Header("Local Light Adjustment")]
    public float horizontalInputSpeed = 0.18f;
    public float verticalInputSpeed = 0.16f;
    public float maxHorizontalViewportOffset = 0.12f;
    public float maxVerticalViewportOffset = 0.10f;

    [Header("Viewport Safety")]
    [Range(0f, 0.45f)]
    public float viewportMargin = 0.08f;

    [Header("Motion Smoothing")]
    public float viewportSmoothTime = 0.12f;
    public float positionSmoothTime = 0.16f;

    [Header("Input Direction")]
    public bool invertHorizontal = false;
    public bool invertVertical = false;

    private Vector2 targetViewportOffset;
    private Vector2 currentViewportOffset;
    private Vector2 viewportOffsetVelocity;
    private Vector3 positionVelocity;
    private float lockedWorldZ;

    // Purpose: Initializes camera and controlled transform references.
    // Input: Inspector references, or fallback scene references.
    // Output: Stores the starting Z position so the light stays on its original depth plane.
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

    // Purpose: Updates the local light position after camera movement has happened.
    // Input: Arrow keys and current camera position.
    // Output: Keeps the light inside the current camera view.
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

    // Purpose: Reads player light-control input from arrow keys.
    // Input: Left, right, up, and down arrow keys.
    // Output: Updates the target viewport offset within a limited local range.
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

    // Purpose: Smooths the local light adjustment so the wash light does not snap.
    // Input: Target viewport offset and smoothing time.
    // Output: Updates the current viewport offset.
    void SmoothViewportOffset()
    {
        currentViewportOffset = Vector2.SmoothDamp(
            currentViewportOffset,
            targetViewportOffset,
            ref viewportOffsetVelocity,
            viewportSmoothTime
        );
    }

    // Purpose: Converts the viewport light position into a world position.
    // Input: Camera viewport coordinates and the locked light depth.
    // Output: Moves the controlled light transform smoothly in world space.
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

    // Purpose: Keeps inspector values within sensible ranges while editing.
    // Input: Inspector value changes.
    // Output: Prevents negative smoothing or invalid movement ranges.
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