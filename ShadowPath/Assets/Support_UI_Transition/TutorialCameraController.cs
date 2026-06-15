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

    [Header("End Overview Shot")]
    public bool useSeparateEndOverviewShot = false;
    public Vector3 endOverviewPosition = new Vector3(8.5f, 3.9f, -10f);
    public float endOverviewSize = 7f;

    [Header("Follow Shot")]
    public Vector3 followOffset = new Vector3(2.2f, 0.9f, -10f);
    public float followSize = 3.8f;
    public float zoomDuration = 1.4f;
    public float followSmoothTime = 0.25f;

    [Header("Scripted Focus")]
    public float scriptedFocusSmoothTime = 0.45f;
    public float scriptedFocusZoomDuration = 0.35f;

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
    private bool isManualFocusLocked;
    private Vector3 manualFocusPosition;
    private float manualFocusSize;

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
        if (isManualFocusLocked)
        {
            UpdateManualFocusCamera();
            return;
        }

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
        Vector3 targetPosition = GetFollowTargetPosition();

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
    /// Purpose: Calculates the current player-follow camera target.
    /// Input: Player position, follow offset, and optional camera bounds.
    /// Output: Bounded camera target position for follow and failure recovery shots.
    /// </summary>
    Vector3 GetFollowTargetPosition()
    {
        return GetFollowTargetPosition(player.position);
    }

    /// <summary>
    /// Purpose: Calculates a player-follow camera target around a specific world point.
    /// Input: World point, follow offset, and optional camera bounds.
    /// Output: Bounded camera target position for scripted focus shots.
    /// </summary>
    Vector3 GetFollowTargetPosition(Vector3 worldPoint)
    {
        Vector3 targetPosition = worldPoint + followOffset;
        targetPosition.z = followOffset.z;

        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        return targetPosition;
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
        isManualFocusLocked = false;
        followVelocity = Vector3.zero;
        zoomVelocity = 0f;
    }

    /// <summary>
    /// Purpose: Restores player-follow behaviour for failure recovery sequences.
    /// Input: External call after the hidden player has been moved back to the respawn point.
    /// Output: Camera follows the player again even if a previous level flow had stopped following.
    /// </summary>
    public void ResumePlayerFollow()
    {
        if (cameraComponent == null)
        {
            cameraComponent = GetComponent<Camera>();
        }

        if (cameraComponent == null)
        {
            return;
        }

        isReturningToOverview = false;
        hasReturnedToOverview = false;
        isManualFocusLocked = false;
        isFollowing = true;
        timer = overviewHoldTime;
        followVelocity = Vector3.zero;
        zoomVelocity = 0f;
    }

    /// <summary>
    /// Purpose: Immediately restores the player-follow composition for failure feedback.
    /// Input: Current player transform and follow camera settings.
    /// Output: Camera is placed on the same focused view used during normal play.
    /// </summary>
    public void SnapToPlayerFollow()
    {
        ResumePlayerFollow();

        if (player == null || cameraComponent == null)
        {
            return;
        }

        transform.position = GetFollowTargetPosition();
        cameraComponent.orthographicSize = followSize;
        followVelocity = Vector3.zero;
        zoomVelocity = 0f;
    }

    /// <summary>
    /// Purpose: Locks the camera on the normal follow composition around a fixed world point.
    /// Input: World point such as the level respawn position.
    /// Output: Camera stays on that composition without following a moving hidden player.
    /// </summary>
    public void LockToFollowFocusAt(Vector3 worldPoint)
    {
        if (cameraComponent == null)
        {
            cameraComponent = GetComponent<Camera>();
        }

        if (cameraComponent == null)
        {
            return;
        }

        manualFocusPosition = GetFollowTargetPosition(worldPoint);
        manualFocusSize = followSize;

        isManualFocusLocked = true;
        isReturningToOverview = false;
        hasReturnedToOverview = false;
        isFollowing = false;
        timer = overviewHoldTime;
        followVelocity = Vector3.zero;
        zoomVelocity = 0f;
    }

    /// <summary>
    /// Purpose: Smoothly moves and zooms the camera toward a fixed scripted focus point.
    /// Input: Cached manual focus target, focus smoothing, and zoom duration.
    /// Output: Camera eases into the focus instead of snapping there.
    /// </summary>
    void UpdateManualFocusCamera()
    {
        transform.position = Vector3.SmoothDamp(
            transform.position,
            manualFocusPosition,
            ref followVelocity,
            Mathf.Max(0.01f, scriptedFocusSmoothTime)
        );

        if (cameraComponent == null)
        {
            return;
        }

        cameraComponent.orthographicSize = Mathf.SmoothDamp(
            cameraComponent.orthographicSize,
            manualFocusSize,
            ref zoomVelocity,
            Mathf.Max(0.01f, scriptedFocusZoomDuration)
        );
    }

    /// <summary>
    /// Purpose: Moves and zooms the camera back to the opening overview shot.
    /// Input: Overview position, overview size, and return smoothing settings.
    /// Output: Camera settles on the same wide composition used at level start.
    /// </summary>
    void UpdateReturnCamera()
    {
        Vector3 returnOverviewPosition = GetReturnOverviewPosition();
        float returnOverviewSize = GetReturnOverviewSize();

        transform.position = Vector3.SmoothDamp(
            transform.position,
            returnOverviewPosition,
            ref followVelocity,
            Mathf.Max(0.01f, returnSmoothTime)
        );

        cameraComponent.orthographicSize = Mathf.SmoothDamp(
            cameraComponent.orthographicSize,
            returnOverviewSize,
            ref zoomVelocity,
            Mathf.Max(0.01f, returnZoomDuration)
        );

        bool positionSettled = Vector3.Distance(transform.position, returnOverviewPosition) < 0.02f;
        bool sizeSettled = Mathf.Abs(cameraComponent.orthographicSize - returnOverviewSize) < 0.02f;

        if (positionSettled && sizeSettled)
        {
            transform.position = returnOverviewPosition;
            cameraComponent.orthographicSize = returnOverviewSize;
            isReturningToOverview = false;
            hasReturnedToOverview = true;
            timer = 0f;

            PlayReturnBlackout();
        }
    }

    Vector3 GetReturnOverviewPosition()
    {
        return useSeparateEndOverviewShot ? endOverviewPosition : overviewPosition;
    }

    float GetReturnOverviewSize()
    {
        return useSeparateEndOverviewShot ? endOverviewSize : overviewSize;
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
