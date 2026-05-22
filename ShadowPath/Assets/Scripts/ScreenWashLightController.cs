using UnityEngine;

// Purpose: Moves the screen wash light according to gameplay light angle changes after the scene has settled.
// Input: The current rotation of the Directional Light.
// Output: Smoothly shifts the broad wash light on the ShadowScreen without following the player.
public class ScreenWashLightController : MonoBehaviour
{
    [Header("Reference")]
    public Transform directionalLight;

    [Header("Startup")]
    public int settleFramesBeforeCapture = 2;

    [Header("Angle Range")]
    public float horizontalDegreesForFullShift = 10f;
    public float verticalDegreesForFullShift = 10f;

    [Header("Wash Light Shift")]
    public float maxHorizontalOffset = 0.12f;
    public float maxVerticalOffset = 0.08f;

    [Header("Motion Smoothing")]
    public float smoothTime = 0.7f;
    public float maxMoveSpeed = 0.25f;

    [Header("Direction Adjustment")]
    public bool invertHorizontal;
    public bool invertVertical;

    private Vector3 startingLocalPosition;
    private Vector3 smoothVelocity;

    private float startingLightYAngle;
    private float startingLightXAngle;

    private int settledFrameCount;
    private bool hasCapturedStartingState;

    // Purpose: Waits for startup light controllers to settle before moving the wash light.
    // Input: The current startup frame count and Directional Light reference.
    // Output: Captures the stable start state first, then updates the wash light movement.
    void LateUpdate()
    {
        if (directionalLight == null)
        {
            return;
        }

        if (!hasCapturedStartingState)
        {
            WaitAndCaptureStartingState();
            return;
        }

        UpdateWashLightPosition();
    }

    // Purpose: Delays start-state capture so other scene light scripts can finish their initial setup.
    // Input: The configured number of startup settle frames.
    // Output: Captures the wash light and gameplay light starting state after the delay.
    void WaitAndCaptureStartingState()
    {
        settledFrameCount++;

        if (settledFrameCount <= settleFramesBeforeCapture)
        {
            return;
        }

        CaptureStartingState();
    }

    // Purpose: Stores the wash light starting position and the gameplay light starting angles.
    // Input: This rig local position and Directional Light rotation.
    // Output: Saves reference values used for bounded wash-light movement.
    void CaptureStartingState()
    {
        startingLocalPosition = transform.localPosition;
        startingLightYAngle = ConvertToSignedAngle(directionalLight.eulerAngles.y);
        startingLightXAngle = ConvertToSignedAngle(directionalLight.eulerAngles.x);

        hasCapturedStartingState = true;
    }

    // Purpose: Calculates the bounded light-angle offset and moves the wash light smoothly.
    // Input: Directional Light angle deltas from the starting rotation.
    // Output: Updates this wash light rig local position.
    void UpdateWashLightPosition()
    {
        Vector2 angleOffset = GetAngleOffset();

        Vector3 targetLocalPosition = new Vector3(
            startingLocalPosition.x + angleOffset.x,
            startingLocalPosition.y + angleOffset.y,
            startingLocalPosition.z
        );

        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetLocalPosition,
            ref smoothVelocity,
            Mathf.Max(0.01f, smoothTime),
            Mathf.Max(0.01f, maxMoveSpeed)
        );
    }

    // Purpose: Converts gameplay light rotation changes into a limited wash-light position offset.
    // Input: Current Directional Light X and Y angle differences.
    // Output: Returns a bounded horizontal and vertical local offset.
    Vector2 GetAngleOffset()
    {
        float currentYAngle = ConvertToSignedAngle(directionalLight.eulerAngles.y);
        float currentXAngle = ConvertToSignedAngle(directionalLight.eulerAngles.x);

        float horizontalAngleDelta = Mathf.DeltaAngle(startingLightYAngle, currentYAngle);
        float verticalAngleDelta = Mathf.DeltaAngle(startingLightXAngle, currentXAngle);

        float horizontalAmount = Mathf.Clamp(
            horizontalAngleDelta / Mathf.Max(0.01f, horizontalDegreesForFullShift),
            -1f,
            1f
        );

        float verticalAmount = Mathf.Clamp(
            verticalAngleDelta / Mathf.Max(0.01f, verticalDegreesForFullShift),
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
            horizontalAmount * maxHorizontalOffset,
            verticalAmount * maxVerticalOffset
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