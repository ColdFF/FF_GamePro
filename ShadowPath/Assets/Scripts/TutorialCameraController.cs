using UnityEngine;

/// <summary>
/// Purpose: Controls the tutorial camera flow from an opening overview shot into a local player-follow view.
/// Input: Player transform and inspector camera settings.
/// Output: Smoothly moves and zooms the camera without changing the core shadow or movement mechanics.
/// </summary>
public class TutorialCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Overview Shot")]
    public Vector3 overviewPosition = new Vector3(8.5f, 3.9f, -10f);
    public float overviewSize = 7f;
    public float overviewHoldTime = 1.2f;

    [Header("Follow Shot")]
    public Vector3 followOffset = new Vector3(2.2f, 0.9f, -10f);
    public float followSize = 3.8f;
    public float zoomDuration = 1.4f;
    public float followSmoothTime = 0.25f;

    [Header("Camera Bounds")]
    public bool useBounds = true;
    public float minX = -2.5f;
    public float maxX = 14.5f;
    public float minY = 2.2f;
    public float maxY = 6.5f;

    [Header("Return Overview")]
    public float returnSmoothTime = 0.45f;
    public float returnZoomDuration = 0.8f;
    public bool stopFollowingAfterReturn = true;
    public bool playBlackoutAfterReturn = true;
    public LevelEndBlackoutController blackoutController;

    private Camera cameraComponent;
    private Vector3 followVelocity;
    private float zoomVelocity;
    private float timer;
    private bool isFollowing;
    private bool isReturningToOverview;
    private bool hasReturnedToOverview;

    // Purpose: Initializes the camera at the wide overview position when the level begins.
    // Input: Camera component and overview settings.
    // Output: Sets the starting camera position and orthographic size.
    void Start()
    {
        cameraComponent = GetComponent<Camera>();

        if (cameraComponent == null)
        {
            Debug.LogWarning("TutorialCameraController requires a Camera component.");
            enabled = false;
            return;
        }

        transform.position = overviewPosition;
        cameraComponent.orthographicSize = overviewSize;
    }

    // Purpose: Updates the camera state after all movement has been processed.
    // Input: Time, player position, and camera mode.
    // Output: Transitions from overview to player-follow camera behaviour.
    void LateUpdate()
    {
        if (isReturningToOverview)
        {
            UpdateReturnCamera();
            return;
        }

        if (hasReturnedToOverview && stopFollowingAfterReturn)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= overviewHoldTime)
        {
            isFollowing = true;
        }

        if (isFollowing)
        {
            UpdateFollowCamera();
        }
    }

    // Purpose: Smoothly moves and zooms the camera toward the local player-follow view.
    // Input: Player position, follow offset, bounds, and smoothing settings.
    // Output: Updates camera position and orthographic size.
    void UpdateFollowCamera()
    {
        Vector3 targetPosition = player.position + followOffset;
        targetPosition.z = followOffset.z;

        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            followSmoothTime
        );

        cameraComponent.orthographicSize = Mathf.SmoothDamp(
            cameraComponent.orthographicSize,
            followSize,
            ref zoomVelocity,
            zoomDuration
        );
    }

    /// <summary>
    /// Purpose: Starts a smooth camera return from the follow shot to the opening overview shot.
    /// Input: External call from level flow such as a completed doorway sequence.
    /// Output: Camera stops following and transitions back to the wide overview.
    /// </summary>
    public void ReturnToOverview()
    {
        if (cameraComponent == null)
        {
            cameraComponent = GetComponent<Camera>();
        }

        if (cameraComponent == null)
        {
            return;
        }

        isFollowing = false;
        isReturningToOverview = true;
        hasReturnedToOverview = false;
        followVelocity = Vector3.zero;
        zoomVelocity = 0f;
    }

    /// <summary>
    /// Purpose: Moves and zooms the camera back to the opening overview shot.
    /// Input: Overview position, overview size, and return smoothing settings.
    /// Output: Camera settles on the same wide composition used at level start.
    /// </summary>
    void UpdateReturnCamera()
    {
        transform.position = Vector3.SmoothDamp(
            transform.position,
            overviewPosition,
            ref followVelocity,
            Mathf.Max(0.01f, returnSmoothTime)
        );

        cameraComponent.orthographicSize = Mathf.SmoothDamp(
            cameraComponent.orthographicSize,
            overviewSize,
            ref zoomVelocity,
            Mathf.Max(0.01f, returnZoomDuration)
        );

        bool positionSettled = Vector3.Distance(transform.position, overviewPosition) < 0.02f;
        bool sizeSettled = Mathf.Abs(cameraComponent.orthographicSize - overviewSize) < 0.02f;

        if (positionSettled && sizeSettled)
        {
            transform.position = overviewPosition;
            cameraComponent.orthographicSize = overviewSize;
            isReturningToOverview = false;
            hasReturnedToOverview = true;
            timer = 0f;

            PlayReturnBlackout();
        }
    }

    /// <summary>
    /// Purpose: Starts the level-end blackout once the overview return has settled.
    /// Input: Assigned blackout controller or scene lookup fallback.
    /// Output: End-of-level lights and black overlay begin fading.
    /// </summary>
    void PlayReturnBlackout()
    {
        if (!playBlackoutAfterReturn)
        {
            return;
        }

        if (blackoutController == null)
        {
            blackoutController = FindObjectOfType<LevelEndBlackoutController>();
        }

        if (blackoutController != null)
        {
            blackoutController.PlayBlackout();
        }
    }
}
