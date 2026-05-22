using UnityEngine;

// Purpose: Keeps the visual screen light near its starting area, follows player movement relatively, and adds limited light-angle adjustment.
// Input: Player movement from the starting position and Directional Light angle changes.
// Output: Smoothly moves the visual light cue without sending it away at game start.
public class ScreenLightVisualController : MonoBehaviour
{
    [Header("References")]
    public Transform directionalLight;
    public Transform player;
    public Transform shadowScreen;

    [Header("Player Follow")]
    public float playerFollowXStrength = 0.6f;
    public float playerFollowYStrength = 0.2f;
    public float maxPlayerFollowXOffset = 0.35f;
    public float maxPlayerFollowYOffset = 0.12f;

    [Header("Light Angle Adjustment")]
    public float horizontalDegreesForFullRange = 10f;
    public float verticalDegreesForFullRange = 10f;
    public float maxAngleXOffset = 0.08f;
    public float maxAngleYOffset = 0.05f;

    [Header("Motion Smoothing")]
    public float smoothTime = 0.6f;
    public float maxMoveSpeed = 0.8f;

    [Header("Direction Adjustment")]
    public bool invertHorizontal;
    public bool invertVertical;

    private Vector3 startingRigLocalPosition;
    private Vector3 startingPlayerWorldPosition;
    private Vector3 smoothVelocity;

    private float startingLightYAngle;
    private float startingLightXAngle;

    private bool hasCapturedStartingState;

    // Purpose: Captures the visible starting state for the light rig, player, and gameplay light.
    // Input: Initial Transform positions and Directional Light rotation.
    // Output: Stores baselines so later movement is relative instead of absolute.
    void Start()
    {
        CaptureStartingState();
    }

    // Purpose: Updates the visual light after player and light movement have been processed.
    // Input: Player displacement and Directional Light angle changes.
    // Output: Smoothly updates the light rig local position.
    void LateUpdate()
    {
        if (!hasCapturedStartingState)
        {
            CaptureStartingState();
            return;
        }

        if (directionalLight == null || player == null || shadowScreen == null)
        {
            return;
        }

        UpdateScreenLightPosition();
    }

    // Purpose: Stores the initial visible position and light references at the beginning of play.
    // Input: This rig Transform, Player Transform, ShadowScreen Transform, and Directional Light Transform.
    // Output: Saves the starting state used by all later relative movement.
    void CaptureStartingState()
    {
        if (directionalLight == null || player == null || shadowScreen == null)
        {
            return;
        }

        startingRigLocalPosition = transform.localPosition;
        startingPlayerWorldPosition = player.position;

        startingLightYAngle = ConvertToSignedAngle(directionalLight.eulerAngles.y);
        startingLightXAngle = ConvertToSignedAngle(directionalLight.eulerAngles.x);

        hasCapturedStartingState = true;
    }

    // Purpose: Builds a bounded target position for the visual light and moves toward it smoothly.
    // Input: Relative player follow offset and bounded light-angle offset.
    // Output: Updates this visual rig local position.
    void UpdateScreenLightPosition()
    {
        Vector2 playerFollowOffset = GetPlayerFollowOffset();
        Vector2 angleOffset = GetLightAngleOffset();

        Vector3 targetLocalPosition = new Vector3(
            startingRigLocalPosition.x + playerFollowOffset.x + angleOffset.x,
            startingRigLocalPosition.y + playerFollowOffset.y + angleOffset.y,
            startingRigLocalPosition.z
        );

        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetLocalPosition,
            ref smoothVelocity,
            Mathf.Max(0.01f, smoothTime),
            Mathf.Max(0.01f, maxMoveSpeed)
        );
    }

    // Purpose: Converts player movement from the start point into a limited ShadowScreen-local follow offset.
    // Input: Player world displacement since the start of play.
    // Output: Returns a bounded local X and Y follow offset.
    Vector2 GetPlayerFollowOffset()
    {
        Vector3 playerWorldDelta = player.position - startingPlayerWorldPosition;
        Vector3 playerLocalDelta = shadowScreen.InverseTransformVector(playerWorldDelta);

        float followX = playerLocalDelta.x * playerFollowXStrength;
        float followY = playerLocalDelta.y * playerFollowYStrength;

        followX = Mathf.Clamp(
            followX,
            -maxPlayerFollowXOffset,
            maxPlayerFollowXOffset
        );

        followY = Mathf.Clamp(
            followY,
            -maxPlayerFollowYOffset,
            maxPlayerFollowYOffset
        );

        return new Vector2(followX, followY);
    }

    // Purpose: Converts gameplay light angle changes into a small bounded local visual offset.
    // Input: Current Directional Light angles compared with their starting angles.
    // Output: Returns a limited local X and Y light adjustment offset.
    Vector2 GetLightAngleOffset()
    {
        float currentYAngle = ConvertToSignedAngle(directionalLight.eulerAngles.y);
        float currentXAngle = ConvertToSignedAngle(directionalLight.eulerAngles.x);

        float horizontalAngleDelta = Mathf.DeltaAngle(startingLightYAngle, currentYAngle);
        float verticalAngleDelta = Mathf.DeltaAngle(startingLightXAngle, currentXAngle);

        float horizontalAmount = Mathf.Clamp(
            horizontalAngleDelta / Mathf.Max(0.01f, horizontalDegreesForFullRange),
            -1f,
            1f
        );

        float verticalAmount = Mathf.Clamp(
            verticalAngleDelta / Mathf.Max(0.01f, verticalDegreesForFullRange),
            -1f,
            1f
        );

        if (invertHorizontal)
        {
            horizontalAmount *= -1f;
        }

        if (invertVertical)
        {
            verticalAmount *= -1f;
        }

        return new Vector2(
            horizontalAmount * maxAngleXOffset,
            verticalAmount * maxAngleYOffset
        );
    }

    // Purpose: Converts a Unity Euler angle into a signed angle value.
    // Input: A raw Euler angle from 0 to 360 degrees.
    // Output: Returns the equivalent angle between -180 and 180 degrees.
    float ConvertToSignedAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}