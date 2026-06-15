using UnityEngine;

/// <summary>
/// Purpose: Moves a block back and forth so its shadow can become a moving platform.
/// Input: Start/end positions, movement time, waiting time, and movement style.
/// Output: The block moves automatically during play mode.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MovingBlock : MonoBehaviour
{
    public enum MovementMode
    {
        // Moves from start to end, then goes back again.
        PingPong,
        // Moves from start to end, then jumps back to start and repeats.
        Loop
    }

    [Header("Movement Space")]
    // If true, offsets are based on the parent object; if false, offsets use the whole scene position.
    public bool useLocalSpace = true;

    [Header("Path")]
    // Where the block starts, measured from its original position.
    public Vector3 startOffset = Vector3.zero;
    // Where the block moves to, measured from its original position.
    public Vector3 endOffset = new Vector3(3f, 0f, 0f);

    [Header("Timing")]
    // Time needed to move from start to end.
    [Min(0.05f)] public float moveDuration = 2f;
    // How long the block waits at the start point.
    [Min(0f)] public float waitAtStart = 0f;
    // How long the block waits at the end point.
    [Min(0f)] public float waitAtEnd = 0f;
    // Chooses whether the block goes back and forth or loops from the start.
    public MovementMode movementMode = MovementMode.PingPong;
    // Controls the movement feel, for example steady movement or slow-in/slow-out.
    public AnimationCurve moveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Runtime")]
    // If true, the block starts moving as soon as the level begins.
    public bool playOnStart = true;

    [Header("Debug")]
    // If true, Unity shows the movement line in the Scene view when the object is selected.
    public bool drawPathGizmo = true;
    // Color of the movement line shown in the Scene view.
    public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 1f);

    private Vector3 baseLocalPosition;
    private Vector3 baseWorldPosition;
    private float timer;
    private int direction = 1;
    private bool isMoving;

    // Purpose: Saves the block's original position before movement starts.
    // Input: The block's starting Transform position.
    // Output: The script knows what position the offsets should be added to.
    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseWorldPosition = transform.position;
    }

    // Purpose: Starts the block in the correct place.
    // Input: The playOnStart setting.
    // Output: The block either begins moving or waits.
    private void Start()
    {
        isMoving = playOnStart;
        ApplyPosition(0f);
    }

    // Purpose: Updates the block movement at a steady physics rate.
    // Input: Time passing during play mode.
    // Output: The block moves along its path.
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

    // Purpose: Turns movement on.
    // Input: A call from another script or Unity event.
    // Output: The block starts moving.
    public void Play()
    {
        isMoving = true;
    }

    // Purpose: Turns movement off.
    // Input: A call from another script or Unity event.
    // Output: The block stops where it currently is.
    public void Pause()
    {
        isMoving = false;
    }

    // Purpose: Sends the block back to the start of its path.
    // Input: A call from another script or Unity event.
    // Output: The block returns to the start position and movement timing resets.
    public void ResetToStart()
    {
        timer = 0f;
        direction = 1;
        ApplyPosition(0f);
    }

    // Purpose: Works out where the block should be on the path right now.
    // Input: The current movement timer.
    // Output: A value from 0 to 1, where 0 is start and 1 is end.
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

    // Purpose: Places the block at one point along the path.
    // Input: A value from 0 to 1.
    // Output: The block's Transform position changes.
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

    // Purpose: Draws the movement path in the Scene view for easier level editing.
    // Input: Gizmo settings and the block path.
    // Output: A line and two endpoint circles appear when the object is selected.
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

