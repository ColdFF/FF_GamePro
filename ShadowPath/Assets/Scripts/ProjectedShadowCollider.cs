using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Purpose: Turns the whole shadow shape into one collider.
/// Input: The shadow caster object, the shadow screen, and the light direction.
/// Output: The player can collide with or stand on the generated shadow shape.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class ProjectedShadowCollider : MonoBehaviour
{
    [Header("Scene References")]
    // The object whose shadow becomes a collider.
    public Renderer shadowCasterRenderer;
    // The wall/screen where the shadow appears.
    public Transform shadowScreen;
    // The light that decides where the shadow is projected.
    public Light directionalLight;

    [Header("Collider Shape")]
    // How deep the generated collider is in 3D space.
    [Min(0.1f)]
    public float colliderDepth = 6f;

    // Extra size added around the shadow outline.
    [Min(0f)]
    public float outlinePadding = 0f;

    // If true, rebuilds the collider every frame while the light or object moves.
    public bool rebuildEveryFrame = true;

    [Header("Passenger Carry")]
    // If true, a moving shadow can carry the player standing on it.
    public bool carryPlayerWithShadow = true;

    // How close the player must be to the top of the shadow to count as standing on it.
    [Min(0.01f)]
    public float topContactTolerance = 0.2f;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh generatedMesh;

    private readonly List<Vector2> currentHull = new List<Vector2>();

    private Transform currentPassenger;
    private Rigidbody currentPassengerRigidbody;
    private PlayerController currentPassengerController;

    // Purpose: Sets up the mesh parts this script needs.
    // Input: MeshFilter and MeshCollider on this GameObject.
    // Output: A reusable generated mesh is ready.
    void Awake()
    {
        CacheComponents();
        CreateGeneratedMesh();
    }

    // Purpose: Builds the first shadow collider when the level starts.
    // Input: Current caster, screen, and light references.
    // Output: The shadow has a collider before the player uses it.
    void Start()
    {
        RebuildShadowCollider();
    }

    // Purpose: Keeps the shadow collider following the moving light.
    // Input: The current light direction each frame.
    // Output: The collider is rebuilt when needed.
    void LateUpdate()
    {
        if (rebuildEveryFrame)
        {
            RebuildShadowCollider();
        }
    }

    // Purpose: Rebuilds the collider from the current shadow shape.
    // Input: Mesh points from the caster projected onto the screen.
    // Output: A 3D MeshCollider follows the shadow outline.
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

    // Purpose: Projects one object point onto the shadow screen.
    // Input: One world point, the light direction, and the screen plane.
    // Output: The point where that object point lands as a shadow.
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

    // Purpose: Builds the outside outline of the shadow.
    // Input: All 2D shadow points on the screen.
    // Output: Ordered points around the shadow edge.
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

    // Purpose: Turns the 2D shadow outline into a 3D collider shape.
    // Input: Ordered shadow outline points.
    // Output: MeshFilter and MeshCollider use the new shadow mesh.
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

    // Purpose: Moves the player when the shadow shape under them moves.
    // Input: Old shadow outline and new shadow outline.
    // Output: The player shifts with the top of the shadow.
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

    // Purpose: Finds the old and new top points under the player's feet.
    // Input: Old shadow outline, new shadow outline, and player foot X position.
    // Output: Matching support points before and after the shadow moved.
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

    // Purpose: Finds the top shadow edge under one horizontal position.
    // Input: Shadow outline and the player's foot X position.
    // Output: The edge number and exact support point on the top edge.
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

    // Purpose: Finds where the player's feet are on the shadow screen.
    // Input: Player Transform and Collider.
    // Output: A 2D foot position on the screen.
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

    // Purpose: Moves the player by the shadow movement.
    // Input: How far the support point moved.
    // Output: The player position moves by the same amount.
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

    // Purpose: Finds the top of the shadow at one horizontal position.
    // Input: Shadow outline and an X position on the screen.
    // Output: The highest Y position there.
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

    // Purpose: Converts a real Unity position into a 2D screen position.
    // Input: A world position near the shadow screen.
    // Output: X/Y coordinates on the shadow screen.
    Vector2 GetScreenLocalPoint(Vector3 worldPoint)
    {
        Vector3 offsetFromScreenOrigin = worldPoint - shadowScreen.position;

        float localX = Vector3.Dot(offsetFromScreenOrigin, shadowScreen.right);
        float localY = Vector3.Dot(offsetFromScreenOrigin, shadowScreen.up);

        return new Vector2(localX, localY);
    }

    // Purpose: Converts a 2D screen position back into the Unity scene.
    // Input: X/Y coordinates on the shadow screen.
    // Output: A world position on the shadow screen.
    Vector3 GetScreenWorldPoint(Vector2 screenLocalPoint)
    {
        return shadowScreen.position +
               shadowScreen.right * screenLocalPoint.x +
               shadowScreen.up * screenLocalPoint.y;
    }

    // Purpose: Starts tracking the player when they touch the shadow.
    // Input: Collision with the generated shadow collider.
    // Output: The script remembers the player if they are on top.
    void OnCollisionEnter(Collision collision)
    {
        TrySetPassenger(collision);
    }

    // Purpose: Keeps checking whether the player is still standing on the shadow.
    // Input: Ongoing collision with the generated shadow collider.
    // Output: Player tracking stays correct.
    void OnCollisionStay(Collision collision)
    {
        TrySetPassenger(collision);
    }

    // Purpose: Stops tracking the player when they leave the shadow.
    // Input: Collision exit from the generated shadow collider.
    // Output: The script stops carrying that player.
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

    // Purpose: Stores the player only if they are standing on the top of the shadow.
    // Input: Collision data from the shadow collider.
    // Output: Player references are saved for carrying.
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

    // Purpose: Checks if the player is touching the top of the shadow.
    // Input: Collision contact points and the current shadow outline.
    // Output: True means the player is on a usable top edge.
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

    // Purpose: Finds the player object from a collision.
    // Input: Collision data from an object tagged Player.
    // Output: Player Transform, or null if this collision is not the player.
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

    // Purpose: Clears the saved player references.
    // Input: None.
    // Output: The script stops treating anyone as being carried.
    void ClearPassenger()
    {
        currentPassenger = null;
        currentPassengerRigidbody = null;
        currentPassengerController = null;
    }

    // Purpose: Makes the shadow outline a little bigger.
    // Input: Shadow outline points and padding amount.
    // Output: Points move outward from the center.
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

    // Purpose: Checks which way three points turn.
    // Input: Three 2D points.
    // Output: A number used to build the outside outline correctly.
    float Cross(Vector2 origin, Vector2 a, Vector2 b)
    {
        return (a.x - origin.x) * (b.y - origin.y) -
               (a.y - origin.y) * (b.x - origin.x);
    }

    // Purpose: Sorts shadow points in a stable order.
    // Input: Two 2D points.
    // Output: Which point should come first.
    int ComparePoints(Vector2 a, Vector2 b)
    {
        int xComparison = a.x.CompareTo(b.x);

        if (xComparison != 0)
        {
            return xComparison;
        }

        return a.y.CompareTo(b.y);
    }

    // Purpose: Gets the MeshFilter and MeshCollider from this object.
    // Input: Components on this GameObject.
    // Output: References are saved for rebuilding.
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

    // Purpose: Creates the mesh object used by the generated shadow collider.
    // Input: None.
    // Output: One reusable mesh is ready.
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

    // Purpose: Checks that the needed scene references exist.
    // Input: Caster object, shadow screen, and light fields.
    // Output: True means the script can build the shadow collider.
    bool HasRequiredReferences()
    {
        return shadowCasterRenderer != null &&
               shadowScreen != null &&
               directionalLight != null;
    }
}
