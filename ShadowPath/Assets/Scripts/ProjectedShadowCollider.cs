using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a projected collider that matches the shadow silhouette
/// of a shadow caster on the shadow screen.
/// The collider is rebuilt when the light changes and can carry
/// a player standing on the upper silhouette edge.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class ProjectedShadowCollider : MonoBehaviour
{
    [Header("Scene References")]
    public Renderer shadowCasterRenderer;
    public Transform shadowScreen;
    public Light directionalLight;

    [Header("Collider Shape")]
    [Min(0.1f)]
    public float colliderDepth = 6f;

    [Min(0f)]
    public float outlinePadding = 0f;

    public bool rebuildEveryFrame = true;

    [Header("Passenger Carry")]
    public bool carryPlayerWithShadow = true;

    [Min(0.01f)]
    public float topContactTolerance = 0.2f;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh generatedMesh;

    private readonly List<Vector2> currentHull = new List<Vector2>();

    private Transform currentPassenger;
    private Rigidbody currentPassengerRigidbody;
    private PlayerController currentPassengerController;

    // Purpose: Prepares the generated mesh and required components.
    // Input: MeshFilter and MeshCollider attached to this GameObject.
    // Output: A reusable dynamic mesh for the projected shadow collider.
    void Awake()
    {
        CacheComponents();
        CreateGeneratedMesh();
    }

    // Purpose: Builds the first projected shadow collider at the start of gameplay.
    // Input: Current shadow caster, screen, and directional light references.
    // Output: A generated collider mesh matching the current shadow outline.
    void Start()
    {
        RebuildShadowCollider();
    }

    // Purpose: Keeps the projected collider aligned with light changes.
    // Input: Updated directional light direction each frame.
    // Output: Rebuilds the projected collider after light movement.
    void LateUpdate()
    {
        if (rebuildEveryFrame)
        {
            RebuildShadowCollider();
        }
    }

    // Purpose: Rebuilds a 3D collider from the 2D projection of the caster mesh.
    // Input: Shadow caster mesh vertices projected onto the shadow screen.
    // Output: A convex extruded mesh collider that follows the shadow outline.
    [ContextMenu("Rebuild Shadow Collider")]
    public void RebuildShadowCollider()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        CacheComponents();
        CreateGeneratedMesh();

        MeshFilter casterMeshFilter = shadowCasterRenderer.GetComponent<MeshFilter>();

        if (casterMeshFilter == null || casterMeshFilter.sharedMesh == null)
        {
            return;
        }

        List<Vector2> previousHull = new List<Vector2>(currentHull);

        Mesh casterMesh = casterMeshFilter.sharedMesh;
        Vector3 screenPoint = shadowScreen.position;
        Vector3 screenNormal = shadowScreen.forward.normalized;
        Vector3 lightDirection = directionalLight.transform.forward.normalized;

        List<Vector2> projectedPoints = new List<Vector2>();

        for (int i = 0; i < casterMesh.vertices.Length; i++)
        {
            Vector3 worldVertex =
                shadowCasterRenderer.transform.TransformPoint(casterMesh.vertices[i]);

            if (!TryProjectPointToScreen(
                    worldVertex,
                    lightDirection,
                    screenPoint,
                    screenNormal,
                    out Vector3 projectedWorldPoint))
            {
                continue;
            }

            Vector2 screenLocalPoint = GetScreenLocalPoint(projectedWorldPoint);
            projectedPoints.Add(screenLocalPoint);
        }

        List<Vector2> newHull = BuildConvexHull(projectedPoints);

        if (newHull.Count < 3)
        {
            return;
        }

        if (outlinePadding > 0f)
        {
            ExpandHullFromCenter(newHull, outlinePadding);
        }

        transform.SetPositionAndRotation(shadowScreen.position, shadowScreen.rotation);
        transform.localScale = Vector3.one;

        BuildExtrudedColliderMesh(newHull);

        if (carryPlayerWithShadow &&
            currentPassenger != null &&
            previousHull.Count >= 3)
        {
            CarryPassengerWithHullChange(previousHull, newHull);
        }

        currentHull.Clear();
        currentHull.AddRange(newHull);
    }

    // Purpose: Projects one caster vertex onto the screen plane along the light direction.
    // Input: A world-space vertex, light direction, and shadow screen plane.
    // Output: The projected world-space point on the shadow screen.
    bool TryProjectPointToScreen(
        Vector3 worldPoint,
        Vector3 lightDirection,
        Vector3 screenPoint,
        Vector3 screenNormal,
        out Vector3 projectedPoint)
    {
        float denominator = Vector3.Dot(lightDirection, screenNormal);

        if (Mathf.Abs(denominator) < 0.0001f)
        {
            projectedPoint = worldPoint;
            return false;
        }

        float distanceAlongLight =
            Vector3.Dot(screenPoint - worldPoint, screenNormal) / denominator;

        projectedPoint = worldPoint + lightDirection * distanceAlongLight;
        return true;
    }

    // Purpose: Builds a convex outline around projected mesh points.
    // Input: Projected 2D points on the shadow screen.
    // Output: Ordered hull points around the shadow silhouette.
    List<Vector2> BuildConvexHull(List<Vector2> points)
    {
        List<Vector2> sortedPoints = new List<Vector2>(points);

        sortedPoints.Sort(ComparePoints);

        List<Vector2> uniquePoints = new List<Vector2>();

        for (int i = 0; i < sortedPoints.Count; i++)
        {
            if (uniquePoints.Count == 0)
            {
                uniquePoints.Add(sortedPoints[i]);
                continue;
            }

            Vector2 previousPoint = uniquePoints[uniquePoints.Count - 1];

            if ((sortedPoints[i] - previousPoint).sqrMagnitude > 0.000001f)
            {
                uniquePoints.Add(sortedPoints[i]);
            }
        }

        if (uniquePoints.Count <= 1)
        {
            return uniquePoints;
        }

        List<Vector2> lowerHull = new List<Vector2>();

        for (int i = 0; i < uniquePoints.Count; i++)
        {
            while (lowerHull.Count >= 2 &&
                   Cross(
                       lowerHull[lowerHull.Count - 2],
                       lowerHull[lowerHull.Count - 1],
                       uniquePoints[i]) <= 0f)
            {
                lowerHull.RemoveAt(lowerHull.Count - 1);
            }

            lowerHull.Add(uniquePoints[i]);
        }

        List<Vector2> upperHull = new List<Vector2>();

        for (int i = uniquePoints.Count - 1; i >= 0; i--)
        {
            while (upperHull.Count >= 2 &&
                   Cross(
                       upperHull[upperHull.Count - 2],
                       upperHull[upperHull.Count - 1],
                       uniquePoints[i]) <= 0f)
            {
                upperHull.RemoveAt(upperHull.Count - 1);
            }

            upperHull.Add(uniquePoints[i]);
        }

        lowerHull.RemoveAt(lowerHull.Count - 1);
        upperHull.RemoveAt(upperHull.Count - 1);

        lowerHull.AddRange(upperHull);

        return lowerHull;
    }

    // Purpose: Creates a thin 3D prism from the projected shadow outline.
    // Input: Ordered shadow hull points on the shadow screen.
    // Output: MeshFilter and MeshCollider using the generated silhouette mesh.
    void BuildExtrudedColliderMesh(List<Vector2> hull)
    {
        int pointCount = hull.Count;
        float halfDepth = colliderDepth * 0.5f;

        Vector3[] vertices = new Vector3[pointCount * 2];

        for (int i = 0; i < pointCount; i++)
        {
            vertices[i] = new Vector3(hull[i].x, hull[i].y, -halfDepth);
            vertices[i + pointCount] = new Vector3(hull[i].x, hull[i].y, halfDepth);
        }

        List<int> triangles = new List<int>();

        for (int i = 1; i < pointCount - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);

            triangles.Add(pointCount);
            triangles.Add(pointCount + i + 1);
            triangles.Add(pointCount + i);
        }

        for (int i = 0; i < pointCount; i++)
        {
            int nextIndex = (i + 1) % pointCount;

            int frontCurrent = i;
            int frontNext = nextIndex;
            int backCurrent = i + pointCount;
            int backNext = nextIndex + pointCount;

            triangles.Add(frontCurrent);
            triangles.Add(frontNext);
            triangles.Add(backNext);

            triangles.Add(frontCurrent);
            triangles.Add(backNext);
            triangles.Add(backCurrent);
        }

        generatedMesh.Clear();
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles.ToArray();
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        meshFilter.sharedMesh = generatedMesh;

        meshCollider.sharedMesh = null;
        meshCollider.convex = true;
        meshCollider.sharedMesh = generatedMesh;
    }

    // Purpose: Moves the standing player with the upper shadow edge deformation.
    // Input: The previous and current shadow hull outlines.
    // Output: Moves the player by the delta between matching top-edge support points.
    void CarryPassengerWithHullChange(List<Vector2> previousHull, List<Vector2> newHull)
    {
        Vector2 passengerFootLocalPoint = GetPassengerScreenFootPoint();

        if (!TryGetMappedTopSupportPoints(
                previousHull,
                newHull,
                passengerFootLocalPoint.x,
                out Vector2 previousSupportPoint,
                out Vector2 newSupportPoint))
        {
            return;
        }

        Vector3 previousSupportWorld = GetScreenWorldPoint(previousSupportPoint);
        Vector3 newSupportWorld = GetScreenWorldPoint(newSupportPoint);

        Vector3 supportDelta = newSupportWorld - previousSupportWorld;

        MovePassenger(supportDelta);
    }

    // Purpose: Finds the previous top support point and maps it to the matching new hull edge.
    // Input: Previous hull, new hull, and passenger foot X position on the screen.
    // Output: Matching old and new support points on the shadow upper edge.
    bool TryGetMappedTopSupportPoints(
        List<Vector2> previousHull,
        List<Vector2> newHull,
        float passengerFootX,
        out Vector2 previousSupportPoint,
        out Vector2 newSupportPoint)
    {
        previousSupportPoint = Vector2.zero;
        newSupportPoint = Vector2.zero;

        if (previousHull.Count != newHull.Count)
        {
            return false;
        }

        if (!TryGetTopEdgeAtX(
                previousHull,
                passengerFootX,
                out int previousEdgeIndex,
                out float previousEdgeT,
                out previousSupportPoint))
        {
            return false;
        }

        if (previousEdgeIndex < 0 || previousEdgeIndex >= newHull.Count)
        {
            return false;
        }

        Vector2 newEdgeStart = newHull[previousEdgeIndex];
        Vector2 newEdgeEnd = newHull[(previousEdgeIndex + 1) % newHull.Count];

        newSupportPoint = Vector2.Lerp(newEdgeStart, newEdgeEnd, previousEdgeT);

        if (!TryGetTopYAtX(newHull, newSupportPoint.x, out float mappedTopY))
        {
            return false;
        }

        if (Mathf.Abs(newSupportPoint.y - mappedTopY) > topContactTolerance)
        {
            return false;
        }

        return true;
    }

    // Purpose: Finds the top hull edge directly under a given screen X position.
    // Input: Hull outline and passenger foot X coordinate.
    // Output: Edge index, interpolation amount, and support point on the upper edge.
    bool TryGetTopEdgeAtX(
        List<Vector2> hull,
        float x,
        out int edgeIndex,
        out float edgeT,
        out Vector2 supportPoint)
    {
        edgeIndex = -1;
        edgeT = 0f;
        supportPoint = Vector2.zero;

        float highestY = float.NegativeInfinity;
        bool foundEdge = false;

        for (int i = 0; i < hull.Count; i++)
        {
            Vector2 a = hull[i];
            Vector2 b = hull[(i + 1) % hull.Count];

            float minX = Mathf.Min(a.x, b.x);
            float maxX = Mathf.Max(a.x, b.x);

            if (x < minX - 0.0001f || x > maxX + 0.0001f)
            {
                continue;
            }

            if (Mathf.Abs(b.x - a.x) < 0.0001f)
            {
                if (Mathf.Abs(x - a.x) > 0.0001f)
                {
                    continue;
                }

                float verticalEdgeT = a.y >= b.y ? 0f : 1f;
                Vector2 verticalPoint = Vector2.Lerp(a, b, verticalEdgeT);

                if (verticalPoint.y > highestY)
                {
                    highestY = verticalPoint.y;
                    edgeIndex = i;
                    edgeT = verticalEdgeT;
                    supportPoint = verticalPoint;
                    foundEdge = true;
                }

                continue;
            }

            float t = (x - a.x) / (b.x - a.x);

            if (t < -0.0001f || t > 1.0001f)
            {
                continue;
            }

            float clampedT = Mathf.Clamp01(t);
            Vector2 candidatePoint = Vector2.Lerp(a, b, clampedT);

            if (candidatePoint.y > highestY)
            {
                highestY = candidatePoint.y;
                edgeIndex = i;
                edgeT = clampedT;
                supportPoint = candidatePoint;
                foundEdge = true;
            }
        }

        return foundEdge;
    }

    // Purpose: Gets the current passenger foot position on the shadow screen.
    // Input: Current passenger Transform and optional Collider bounds.
    // Output: Screen-local foot point used for shadow carrying.
    Vector2 GetPassengerScreenFootPoint()
    {
        if (currentPassenger == null)
        {
            return Vector2.zero;
        }

        Collider passengerCollider = currentPassenger.GetComponent<Collider>();

        if (passengerCollider == null &&
            currentPassengerRigidbody != null)
        {
            passengerCollider = currentPassengerRigidbody.GetComponent<Collider>();
        }

        if (passengerCollider != null)
        {
            Vector3 footWorldPoint = new Vector3(
                passengerCollider.bounds.center.x,
                passengerCollider.bounds.min.y,
                passengerCollider.bounds.center.z
            );

            return GetScreenLocalPoint(footWorldPoint);
        }

        return GetScreenLocalPoint(currentPassenger.position);
    }

    // Purpose: Moves the carried player without parenting or scaling them.
    // Input: World-space support point movement.
    // Output: Updates the passenger position by the same delta.
    void MovePassenger(Vector3 supportDelta)
    {
        if (supportDelta.sqrMagnitude <= 0.0000001f)
        {
            return;
        }

        if (currentPassengerController == null && currentPassenger != null)
        {
            currentPassengerController = currentPassenger.GetComponent<PlayerController>();
        }

        if (currentPassengerController != null && !currentPassengerController.enabled)
        {
            return;
        }

        if (currentPassengerRigidbody != null)
        {
            currentPassengerRigidbody.position += supportDelta;
        }
        else
        {
            currentPassenger.position += supportDelta;
        }
    }

    // Purpose: Finds the highest hull edge at a given horizontal screen position.
    // Input: Hull outline and an X coordinate on the shadow screen.
    // Output: Top Y coordinate of the silhouette at that X.
    bool TryGetTopYAtX(List<Vector2> hull, float x, out float topY)
    {
        topY = float.NegativeInfinity;
        bool foundIntersection = false;

        for (int i = 0; i < hull.Count; i++)
        {
            Vector2 a = hull[i];
            Vector2 b = hull[(i + 1) % hull.Count];

            float minX = Mathf.Min(a.x, b.x);
            float maxX = Mathf.Max(a.x, b.x);

            if (x < minX - 0.0001f || x > maxX + 0.0001f)
            {
                continue;
            }

            if (Mathf.Abs(b.x - a.x) < 0.0001f)
            {
                if (Mathf.Abs(x - a.x) <= 0.0001f)
                {
                    topY = Mathf.Max(topY, Mathf.Max(a.y, b.y));
                    foundIntersection = true;
                }

                continue;
            }

            float t = (x - a.x) / (b.x - a.x);

            if (t < -0.0001f || t > 1.0001f)
            {
                continue;
            }

            float y = Mathf.Lerp(a.y, b.y, Mathf.Clamp01(t));

            topY = Mathf.Max(topY, y);
            foundIntersection = true;
        }

        return foundIntersection;
    }

    // Purpose: Gets the local screen-space X and Y of a world point.
    // Input: World-space point near the shadow screen.
    // Output: 2D coordinate using the screen right and up directions.
    Vector2 GetScreenLocalPoint(Vector3 worldPoint)
    {
        Vector3 offsetFromScreenOrigin = worldPoint - shadowScreen.position;

        float localX = Vector3.Dot(offsetFromScreenOrigin, shadowScreen.right);
        float localY = Vector3.Dot(offsetFromScreenOrigin, shadowScreen.up);

        return new Vector2(localX, localY);
    }

    // Purpose: Converts a local screen-space point back into world space.
    // Input: Local X and Y coordinate on the shadow screen.
    // Output: World-space point on the shadow screen plane.
    Vector3 GetScreenWorldPoint(Vector2 screenLocalPoint)
    {
        return shadowScreen.position +
               shadowScreen.right * screenLocalPoint.x +
               shadowScreen.up * screenLocalPoint.y;
    }

    // Purpose: Registers the player when they touch the upper silhouette edge.
    // Input: Collision contact with the projected shadow collider.
    // Output: Stores the current player passenger references.
    void OnCollisionEnter(Collision collision)
    {
        TrySetPassenger(collision);
    }

    // Purpose: Keeps passenger tracking aligned while the player stands on the silhouette.
    // Input: Ongoing collision contact with the projected shadow collider.
    // Output: Refreshes or clears the current passenger state.
    void OnCollisionStay(Collision collision)
    {
        TrySetPassenger(collision);
    }

    // Purpose: Clears passenger tracking when the player leaves the silhouette.
    // Input: Collision exit from the projected shadow collider.
    // Output: Stops carrying that player.
    void OnCollisionExit(Collision collision)
    {
        Transform exitingPlayer = GetPlayerTransform(collision);

        if (exitingPlayer == null)
        {
            return;
        }

        if (exitingPlayer == currentPassenger)
        {
            ClearPassenger();
        }
    }

    // Purpose: Stores the player only when they are standing on the upper shadow edge.
    // Input: Collision data from the projected shadow collider.
    // Output: Current passenger references for shadow carrying.
    void TrySetPassenger(Collision collision)
    {
        Transform playerTransform = GetPlayerTransform(collision);

        if (playerTransform == null)
        {
            return;
        }

        if (!IsTouchingShadowTop(collision))
        {
            if (playerTransform == currentPassenger)
            {
                ClearPassenger();
            }

            return;
        }

        currentPassenger = playerTransform;
        currentPassengerRigidbody = collision.rigidbody;
        currentPassengerController = playerTransform.GetComponent<PlayerController>();

        if (currentPassengerController == null && currentPassengerRigidbody != null)
        {
            currentPassengerController = currentPassengerRigidbody.GetComponent<PlayerController>();
        }
    }

    // Purpose: Checks whether collision contact is close to the silhouette top edge.
    // Input: Collision contact points and the current hull outline.
    // Output: True when the player touches a walkable upper shadow edge.
    bool IsTouchingShadowTop(Collision collision)
    {
        if (currentHull.Count < 3)
        {
            return false;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            Vector2 localContactPoint = GetScreenLocalPoint(contact.point);

            if (!TryGetTopYAtX(currentHull, localContactPoint.x, out float topY))
            {
                continue;
            }

            if (Mathf.Abs(localContactPoint.y - topY) <= topContactTolerance)
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Finds the Player Transform from a collision.
    // Input: Collision data from a collider or Rigidbody tagged as Player.
    // Output: Player Transform or null.
    Transform GetPlayerTransform(Collision collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            return collision.transform;
        }

        if (collision.rigidbody != null && collision.rigidbody.CompareTag("Player"))
        {
            return collision.rigidbody.transform;
        }

        return null;
    }

    // Purpose: Removes the current passenger references.
    // Input: None.
    // Output: Stops carrying the player.
    void ClearPassenger()
    {
        currentPassenger = null;
        currentPassengerRigidbody = null;
        currentPassengerController = null;
    }

    // Purpose: Slightly expands the outline if collider tolerance is needed.
    // Input: Hull points and requested padding amount.
    // Output: Hull points moved outward from their center.
    void ExpandHullFromCenter(List<Vector2> hull, float padding)
    {
        Vector2 center = Vector2.zero;

        for (int i = 0; i < hull.Count; i++)
        {
            center += hull[i];
        }

        center /= hull.Count;

        for (int i = 0; i < hull.Count; i++)
        {
            Vector2 direction = hull[i] - center;

            if (direction.sqrMagnitude > 0.000001f)
            {
                hull[i] += direction.normalized * padding;
            }
        }
    }

    // Purpose: Finds the signed turn direction for convex hull construction.
    // Input: Three 2D points.
    // Output: Positive for a left turn, negative for a right turn.
    float Cross(Vector2 origin, Vector2 a, Vector2 b)
    {
        return (a.x - origin.x) * (b.y - origin.y) -
               (a.y - origin.y) * (b.x - origin.x);
    }

    // Purpose: Sorts points from left to right, then bottom to top.
    // Input: Two 2D points.
    // Output: Comparison value used by List.Sort.
    int ComparePoints(Vector2 a, Vector2 b)
    {
        int xComparison = a.x.CompareTo(b.x);

        if (xComparison != 0)
        {
            return xComparison;
        }

        return a.y.CompareTo(b.y);
    }

    // Purpose: Stores required components from this GameObject.
    // Input: MeshFilter and MeshCollider on this object.
    // Output: Cached references used while rebuilding.
    void CacheComponents()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshCollider == null)
        {
            meshCollider = GetComponent<MeshCollider>();
        }
    }

    // Purpose: Creates the reusable generated mesh once.
    // Input: None.
    // Output: Dynamic mesh object for the projected collider.
    void CreateGeneratedMesh()
    {
        if (generatedMesh != null)
        {
            return;
        }

        generatedMesh = new Mesh();
        generatedMesh.name = "Projected Shadow Collider Mesh";
        generatedMesh.MarkDynamic();
    }

    // Purpose: Prevents rebuilding before required references are assigned.
    // Input: Scene reference fields.
    // Output: True when projection can be calculated.
    bool HasRequiredReferences()
    {
        return shadowCasterRenderer != null &&
               shadowScreen != null &&
               directionalLight != null;
    }
}
