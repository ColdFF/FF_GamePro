using UnityEngine;

/// <summary>
/// Purpose: Moves a block between two local-space endpoints for reusable moving shadow casters.
/// Input: Inspector endpoints, duration, wait times, and movement mode.
/// Output: Updates the Transform position so its shadow platform can move without per-block animation clips.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MovingBlock : MonoBehaviour
{
    public enum MovementMode
    {
        PingPong,
        Loop
    }

    [Header("Movement Space")]
    public bool useLocalSpace = true;

    [Header("Path")]
    public Vector3 startOffset = Vector3.zero;
    public Vector3 endOffset = new Vector3(3f, 0f, 0f);

    [Header("Timing")]
    [Min(0.05f)] public float moveDuration = 2f;
    [Min(0f)] public float waitAtStart = 0f;
    [Min(0f)] public float waitAtEnd = 0f;
    public MovementMode movementMode = MovementMode.PingPong;
    public AnimationCurve moveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Runtime")]
    public bool playOnStart = true;

    [Header("Debug")]
    public bool drawPathGizmo = true;
    public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 1f);

    private Vector3 baseLocalPosition;
    private Vector3 baseWorldPosition;
    private float timer;
    private int direction = 1;
    private bool isMoving;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseWorldPosition = transform.position;
    }

    private void Start()
    {
        isMoving = playOnStart;
        ApplyPosition(0f);
    }

    private void FixedUpdate()
    {
        if (!isMoving)
        {
            return;
        }

        float totalDuration = moveDuration + waitAtStart + waitAtEnd;
        if (totalDuration <= 0f)
        {
            ApplyPosition(direction > 0 ? 1f : 0f);
            return;
        }

        timer += Time.fixedDeltaTime;

        if (timer >= totalDuration)
        {
            timer -= totalDuration;

            if (movementMode == MovementMode.PingPong)
            {
                direction *= -1;
            }
        }

        float t = GetPathT(timer);
        ApplyPosition(t);
    }

    public void Play()
    {
        isMoving = true;
    }

    public void Pause()
    {
        isMoving = false;
    }

    public void ResetToStart()
    {
        timer = 0f;
        direction = 1;
        ApplyPosition(0f);
    }

    private float GetPathT(float time)
    {
        float t;

        if (time < waitAtStart)
        {
            t = 0f;
        }
        else if (time < waitAtStart + moveDuration)
        {
            t = Mathf.InverseLerp(waitAtStart, waitAtStart + moveDuration, time);
        }
        else
        {
            t = 1f;
        }

        if (movementMode == MovementMode.PingPong && direction < 0)
        {
            t = 1f - t;
        }

        return moveCurve != null ? moveCurve.Evaluate(t) : t;
    }

    private void ApplyPosition(float t)
    {
        Vector3 offset = Vector3.Lerp(startOffset, endOffset, t);

        if (useLocalSpace)
        {
            transform.localPosition = baseLocalPosition + offset;
        }
        else
        {
            transform.position = baseWorldPosition + offset;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPathGizmo)
        {
            return;
        }

        Vector3 basePosition = Application.isPlaying
            ? (useLocalSpace && transform.parent != null ? transform.parent.TransformPoint(baseLocalPosition) : baseWorldPosition)
            : transform.position;

        Vector3 worldStart = useLocalSpace && transform.parent != null
            ? transform.parent.TransformPoint((Application.isPlaying ? baseLocalPosition : transform.localPosition) + startOffset)
            : basePosition + startOffset;

        Vector3 worldEnd = useLocalSpace && transform.parent != null
            ? transform.parent.TransformPoint((Application.isPlaying ? baseLocalPosition : transform.localPosition) + endOffset)
            : basePosition + endOffset;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(worldStart, worldEnd);
        Gizmos.DrawWireSphere(worldStart, 0.15f);
        Gizmos.DrawWireSphere(worldEnd, 0.15f);
    }
}

