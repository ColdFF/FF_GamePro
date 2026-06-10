using UnityEngine;

/// <summary>
/// Purpose: Drives the main-menu hanging stickman from the projected shadow end of a decorative rope.
/// Input: Rope start/end transforms, the shadow screen, directional light, and a stickman visual pivot.
/// Output: Gently swings the rope end and keeps the stickman's hand aligned to the rope's projected shadow end.
/// </summary>
[DefaultExecutionOrder(-200)]
public class MenuProjectedRopeHanger : MonoBehaviour
{
    [Header("Projection References")]
    public Transform ropeStart;
    public Transform ropeEnd;
    public Transform shadowScreen;
    public Light directionalLight;

    [Header("Stickman")]
    public Transform stickmanPivot;
    public bool autoCaptureHandLocalOffset = true;
    public Vector3 handLocalOffset = new Vector3(0f, 0f, 0f);
    public Vector3 projectedHandWorldOffset = new Vector3(-0.05f, -0.05f, 0f);
    public float stickmanPlaneZ = 0f;
    public bool useStickmanInitialZ = true;
    public bool rotateStickmanWithShadowRope = true;
    public float maxStickmanRotationDegrees = 6f;

    [Header("Rope End Swing")]
    public bool driveRopeEnd = true;
    public float swingAmplitudeDegrees = 2f;
    public float period = 2.8f;
    public float phaseOffset;
    public float startupDelay = 0.15f;
    public float startupRampDuration = 2.4f;
    public bool useUnscaledTime = true;

    [Header("Projection")]
    public float shadowSurfaceOffset = -0.02f;
    public bool keepStickmanOnProjectedPlane = false;

    private Vector3 ropeEndRestLocalPosition;
    private Quaternion stickmanRestLocalRotation;
    private float enabledTime;
    private bool hasRestPose;

    /// <summary>
    /// Purpose: Captures the starting rope and stickman transforms before the menu animation begins.
    /// Input: Current Inspector references and scene transform values.
    /// Output: Stores rest positions, rest rotation, and the initial timing baseline.
    /// </summary>
    private void Awake()
    {
        CaptureRestPose();
    }

    /// <summary>
    /// Purpose: Resets the animation clock and refreshes cached rest data whenever the component is enabled.
    /// Input: Current time and current referenced transforms.
    /// Output: Stores a new enabled time and ensures rest-pose data is available.
    /// </summary>
    private void OnEnable()
    {
        CaptureRestPose();
        enabledTime = GetTime();
    }

    /// <summary>
    /// Purpose: Updates the decorative menu rope and keeps the stickman attached to the projected shadow endpoint.
    /// Input: Time, rope endpoints, shadow screen plane, and directional light direction.
    /// Output: Moves the rope end, projects the rope shadow, and aligns the stickman's hand to the projected end.
    /// </summary>
    private void Update()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        if (!hasRestPose)
        {
            CaptureRestPose();
        }

        float angle = GetCurrentSwingAngle();

        if (driveRopeEnd)
        {
            DriveRopeEnd(angle);
        }

        if (!TryGetProjectedRope(out Vector3 projectedStart, out Vector3 projectedEnd))
        {
            return;
        }

        AlignStickmanToProjectedEnd(projectedStart, projectedEnd);
    }

    /// <summary>
    /// Purpose: Stores the current transform values as the menu animation's neutral pose.
    /// Input: Rope end and stickman transform values from the scene.
    /// Output: Cached rest local position, rest local rotation, and stickman depth information.
    /// </summary>
    public void CaptureRestPose()
    {
        if (ropeEnd != null)
        {
            ropeEndRestLocalPosition = ropeEnd.localPosition;
        }

        if (stickmanPivot != null)
        {
            stickmanRestLocalRotation = stickmanPivot.localRotation;

            if (useStickmanInitialZ)
            {
                stickmanPlaneZ = stickmanPivot.position.z;
            }

            TryAutoCaptureHandLocalOffset();
        }

        hasRestPose = ropeEnd != null && stickmanPivot != null;
    }

    /// <summary>
    /// Purpose: Uses the current scene pose to infer where the stickman's hand is relative to its pivot.
    /// Input: Current stickman pivot position, projected rope endpoint, and the configured hand world offset.
    /// Output: Fills handLocalOffset once so the pivot is not moved directly onto the rope shadow endpoint.
    /// </summary>
    private void TryAutoCaptureHandLocalOffset()
    {
        if (!autoCaptureHandLocalOffset || handLocalOffset.sqrMagnitude > 0.0001f)
        {
            return;
        }

        if (ropeStart == null || ropeEnd == null || shadowScreen == null || directionalLight == null)
        {
            return;
        }

        if (!TryGetProjectedRope(out _, out Vector3 projectedEnd))
        {
            return;
        }

        Vector3 targetHandPosition = keepStickmanOnProjectedPlane
            ? projectedEnd
            : new Vector3(projectedEnd.x, projectedEnd.y, stickmanPlaneZ);

        targetHandPosition += projectedHandWorldOffset;
        handLocalOffset = stickmanPivot.InverseTransformVector(targetHandPosition - stickmanPivot.position);
    }

    /// <summary>
    /// Purpose: Checks that the component has enough scene references to calculate the projected rope endpoint.
    /// Input: Inspector reference fields.
    /// Output: True when all required references are assigned; otherwise false.
    /// </summary>
    private bool HasRequiredReferences()
    {
        return ropeStart != null &&
               ropeEnd != null &&
               shadowScreen != null &&
               directionalLight != null &&
               stickmanPivot != null;
    }

    /// <summary>
    /// Purpose: Calculates the current menu sway angle with startup easing.
    /// Input: Current time, configured period, phase, delay, ramp, and amplitude.
    /// Output: A signed angle in degrees for this frame.
    /// </summary>
    private float GetCurrentSwingAngle()
    {
        float safePeriod = Mathf.Max(0.01f, period);
        float elapsed = Mathf.Max(0f, GetTime() - enabledTime - startupDelay);
        float ramp = startupRampDuration <= 0f
            ? 1f
            : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / startupRampDuration));

        return Mathf.Sin((elapsed + phaseOffset) * Mathf.PI * 2f / safePeriod)
            * swingAmplitudeDegrees
            * ramp;
    }

    /// <summary>
    /// Purpose: Moves the physical rope end around the rope start so the real rope and its shadow both animate.
    /// Input: Cached rope-end rest offset and the current swing angle.
    /// Output: Updates the rope end world position while preserving its original depth offset.
    /// </summary>
    private void DriveRopeEnd(float angleDegrees)
    {
        Vector3 restWorldPosition = ropeEnd.parent != null
            ? ropeEnd.parent.TransformPoint(ropeEndRestLocalPosition)
            : ropeEndRestLocalPosition;

        Vector3 restOffset = restWorldPosition - ropeStart.position;
        Quaternion swingRotation = Quaternion.AngleAxis(angleDegrees, shadowScreen.forward.normalized);
        ropeEnd.position = ropeStart.position + swingRotation * restOffset;
    }

    /// <summary>
    /// Purpose: Projects the rope start and end points onto the shadow screen using the current light direction.
    /// Input: Rope endpoint world positions, shadow screen plane, surface offset, and directional light forward vector.
    /// Output: Projected world-space start and end positions on the shadow screen plane.
    /// </summary>
    private bool TryGetProjectedRope(out Vector3 projectedStart, out Vector3 projectedEnd)
    {
        projectedStart = Vector3.zero;
        projectedEnd = Vector3.zero;

        Plane screenPlane = new Plane(
            shadowScreen.forward,
            shadowScreen.position + shadowScreen.forward.normalized * shadowSurfaceOffset
        );

        Vector3 lightDirection = directionalLight.transform.forward.normalized;

        return TryProjectPoint(ropeStart.position, screenPlane, lightDirection, out projectedStart) &&
               TryProjectPoint(ropeEnd.position, screenPlane, lightDirection, out projectedEnd);
    }

    /// <summary>
    /// Purpose: Casts a point along the light direction until it hits the shadow screen plane.
    /// Input: A world point, the shadow screen plane, and the normalized light direction.
    /// Output: The projected point on the plane when the projection ray intersects it.
    /// </summary>
    private bool TryProjectPoint(Vector3 worldPoint, Plane screenPlane, Vector3 lightDirection, out Vector3 projectedPoint)
    {
        projectedPoint = Vector3.zero;

        Ray projectionRay = new Ray(worldPoint, lightDirection);
        if (!screenPlane.Raycast(projectionRay, out float distance))
        {
            return false;
        }

        projectedPoint = projectionRay.GetPoint(distance);
        return true;
    }

    /// <summary>
    /// Purpose: Places the stickman so its configured hand point sits on the rope shadow's projected endpoint.
    /// Input: Projected rope start/end positions.
    /// Output: Updates the stickman pivot position and optional visual rotation.
    /// </summary>
    private void AlignStickmanToProjectedEnd(Vector3 projectedStart, Vector3 projectedEnd)
    {
        Vector3 targetHandPosition = keepStickmanOnProjectedPlane
            ? projectedEnd
            : new Vector3(projectedEnd.x, projectedEnd.y, stickmanPlaneZ);

        targetHandPosition += projectedHandWorldOffset;

        if (rotateStickmanWithShadowRope)
        {
            float shadowRopeAngle = GetProjectedRopeAngle(projectedStart, projectedEnd);
            float clampedAngle = Mathf.Clamp(shadowRopeAngle, -maxStickmanRotationDegrees, maxStickmanRotationDegrees);
            stickmanPivot.localRotation = stickmanRestLocalRotation * Quaternion.Euler(0f, 0f, clampedAngle);
        }
        else
        {
            stickmanPivot.localRotation = stickmanRestLocalRotation;
        }

        Vector3 currentHandOffset = stickmanPivot.TransformVector(handLocalOffset);
        stickmanPivot.position = targetHandPosition - currentHandOffset;
    }

    /// <summary>
    /// Purpose: Calculates the rope shadow's signed lean angle on the shadow screen.
    /// Input: Projected rope start/end positions and the shadow screen axes.
    /// Output: A signed angle in degrees relative to the screen's downward direction.
    /// </summary>
    private float GetProjectedRopeAngle(Vector3 projectedStart, Vector3 projectedEnd)
    {
        Vector3 projectedAxis = Vector3.ProjectOnPlane(projectedEnd - projectedStart, shadowScreen.forward);
        if (projectedAxis.sqrMagnitude < 0.0001f)
        {
            return 0f;
        }

        return Vector3.SignedAngle(-shadowScreen.up, projectedAxis.normalized, shadowScreen.forward);
    }

    /// <summary>
    /// Purpose: Keeps Inspector values in safe ranges while editing the scene.
    /// Input: Current serialized field values.
    /// Output: Clamped timing, amplitude, and rotation settings.
    /// </summary>
    private void OnValidate()
    {
        swingAmplitudeDegrees = Mathf.Max(0f, swingAmplitudeDegrees);
        period = Mathf.Max(0.01f, period);
        startupDelay = Mathf.Max(0f, startupDelay);
        startupRampDuration = Mathf.Max(0f, startupRampDuration);
        maxStickmanRotationDegrees = Mathf.Max(0f, maxStickmanRotationDegrees);
    }

    /// <summary>
    /// Purpose: Selects the time source used by the menu animation.
    /// Input: The configured unscaled-time option.
    /// Output: Current scaled or unscaled Unity time.
    /// </summary>
    private float GetTime()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }
}
