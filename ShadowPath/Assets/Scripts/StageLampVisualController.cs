using UnityEngine;

// Purpose: Rotates the visible stage lamp head when the gameplay light angle changes.
// Input: The current rotation of the Directional Light.
// Output: Smoothly tilts the lamp head to reinforce the light-and-shadow interaction.
public class StageLampVisualController : MonoBehaviour
{
    [Header("Reference")]
    public Transform directionalLight;

    [Header("Startup")]
    public int settleFramesBeforeCapture = 2;

    [Header("Angle Range")]
    public float horizontalDegreesForFullTilt = 10f;
    public float verticalDegreesForFullTilt = 10f;

    [Header("Lamp Head Tilt")]
    public float maxHorizontalTilt = 18f;
    public float maxVerticalTilt = 6f;

    [Header("Motion Smoothing")]
    public float smoothTime = 0.25f;
    public float maxTiltSpeed = 90f;

    [Header("Direction Adjustment")]
    public bool invertHorizontal;
    public bool invertVertical;

    private Vector3 startingLocalEulerAngles;
    private float currentZAngle;
    private float zAngleVelocity;

    private float startingLightYAngle;
    private float startingLightXAngle;

    private int settledFrameCount;
    private bool hasCapturedStartingState;

    // Purpose: Waits for scene light setup to settle before rotating the visible lamp head.
    // Input: Startup frame count and Directional Light reference.
    // Output: Captures a stable starting state, then updates the lamp tilt.
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

        UpdateLampTilt();
    }

    // Purpose: Delays the initial capture until other light scripts finish their startup state.
    // Input: The configured number of settle frames.
    // Output: Captures the lamp and gameplay light starting state after the delay.
    void WaitAndCaptureStartingState()
    {
        settledFrameCount++;

        if (settledFrameCount <= settleFramesBeforeCapture)
        {
            return;
        }

        CaptureStartingState();
    }

    // Purpose: Stores the initial lamp head rotation and Directional Light angles.
    // Input: Lamp head Transform and Directional Light rotation.
    // Output: Saves reference values used for later visual tilt.
    void CaptureStartingState()
    {
        startingLocalEulerAngles = transform.localEulerAngles;
        currentZAngle = startingLocalEulerAngles.z;

        startingLightYAngle = ConvertToSignedAngle(directionalLight.eulerAngles.y);
        startingLightXAngle = ConvertToSignedAngle(directionalLight.eulerAngles.x);

        hasCapturedStartingState = true;
    }

    // Purpose: Calculates and applies a smoothed lamp-head tilt.
    // Input: Directional Light angle changes from the starting rotation.
    // Output: Rotates the lamp head around its local Z axis.
    void UpdateLampTilt()
    {
        float tiltOffset = GetTiltOffset();
        float targetZAngle = startingLocalEulerAngles.z + tiltOffset;

        currentZAngle = Mathf.SmoothDampAngle(
            currentZAngle,
            targetZAngle,
            ref zAngleVelocity,
            Mathf.Max(0.01f, smoothTime),
            Mathf.Max(0.01f, maxTiltSpeed)
        );

        transform.localRotation = Quaternion.Euler(
            startingLocalEulerAngles.x,
            startingLocalEulerAngles.y,
            currentZAngle
        );
    }

    // Purpose: Converts gameplay light rotation changes into a limited lamp-head tilt.
    // Input: Current Directional Light X and Y angle differences.
    // Output: Returns a bounded visual tilt amount for the lamp head.
    float GetTiltOffset()
    {
        float currentYAngle = ConvertToSignedAngle(directionalLight.eulerAngles.y);
        float currentXAngle = ConvertToSignedAngle(directionalLight.eulerAngles.x);

        float horizontalAngleDelta = Mathf.DeltaAngle(startingLightYAngle, currentYAngle);
        float verticalAngleDelta = Mathf.DeltaAngle(startingLightXAngle, currentXAngle);

        float horizontalAmount = Mathf.Clamp(
            horizontalAngleDelta / Mathf.Max(0.01f, horizontalDegreesForFullTilt),
            -1f,
            1f
        );

        float verticalAmount = Mathf.Clamp(
            verticalAngleDelta / Mathf.Max(0.01f, verticalDegreesForFullTilt),
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

        return horizontalAmount * maxHorizontalTilt
            + verticalAmount * maxVerticalTilt;
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