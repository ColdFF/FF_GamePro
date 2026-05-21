using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Projects one shadow caster onto the shadow screen,
/// extracts the upper visible shadow edges,
/// and creates thin BoxCollider strips that the player can stand on.
/// </summary>
public class ProjectedShadowEdgeWalkway : MonoBehaviour
{
    [Header("Scene References")]
    public Renderer shadowCasterRenderer;
    public Transform shadowScreen;
    public Light directionalLight;

    [Header("Platform Shape")]
    [Min(0.01f)]
    public float platformThickness = 0.15f;

    [Min(0.1f)]
    public float platformDepth = 6f;

    [Min(0f)]
    public float widthPadding = 0.02f;

    public float verticalOffset = -0.03f;

    [Min(0.01f)]
    public float minEdgeLength = 0.05f;

    [Min(0.001f)]
    public float minHorizontalSpan = 0.02f;

    [Header("Passenger Carry")]
    public bool carryPlayerWithShadow = true;
    public string playerTag = "Player";

    [Min(0.01f)]
    public float passengerContactTolerance = 0.24f;

    [Min(0f)]
    public float passengerHorizontalTolerance = 0.12f;

    [Min(0f)]
    public float maxPassengerUpwardSpeed = 0.2f;

    [Min(0.01f)]
    public float maxCarryDistancePerPhysicsStep = 1f;

    [Min(0.01f)]
    public float fallbackEdgeMatchDistance = 1.5f;

    [Header("Debug")]
    public bool showDebugWalkway = true;
    public Material debugMaterial;

    private Rigidbody playerRigidbody;
    private Collider playerCollider;

    private MeshCollider oldMeshCollider;
    private MeshRenderer oldMeshRenderer;

    private Transform generatedStripRoot;

    private readonly List<GameObject> generatedStripObjects = new List<GameObject>();
    private readonly List<EdgeSegment> currentEdges = new List<EdgeSegment>();

    /// <summary>
    /// Stores one projected point and the source caster corner
    /// that created this point.
    /// </summary>
    private struct ProjectedPoint
    {
        public Vector2 position;
        public int sourceCornerIndex;

        /// <summary>
        /// Creates one projected point.
        /// </summary>
        /// <param name="position">Point position on the shadow screen.</param>
        /// <param name="sourceCornerIndex">Original shadow caster bounds-corner index.</param>
        public ProjectedPoint(Vector2 position, int sourceCornerIndex)
        {
            this.position = position;
            this.sourceCornerIndex = sourceCornerIndex;
        }
    }

    /// <summary>
    /// Stores one generated walkable upper shadow edge.
    /// </summary>
    private struct EdgeSegment
    {
        public Vector2 left;
        public Vector2 right;
        public int leftSourceCornerIndex;
        public int rightSourceCornerIndex;

        /// <summary>
        /// Creates one projected edge segment.
        /// </summary>
        /// <param name="left">Left end of the edge.</param>
        /// <param name="right">Right end of the edge.</param>
        /// <param name="leftSourceCornerIndex">Source caster corner for the left end.</param>
        /// <param name="rightSourceCornerIndex">Source caster corner for the right end.</param>
        public EdgeSegment(
            Vector2 left,
            Vector2 right,
            int leftSourceCornerIndex,
            int rightSourceCornerIndex
        )
        {
            this.left = left;
            this.right = right;
            this.leftSourceCornerIndex = leftSourceCornerIndex;
            this.rightSourceCornerIndex = rightSourceCornerIndex;
        }

        /// <summary>
        /// Returns the midpoint of this edge.
        /// </summary>
        public Vector2 MidPoint
        {
            get
            {
                return (left + right) * 0.5f;
            }
        }
    }

    /// <summary>
    /// Caches references and disables old mesh-based collision
    /// left on this GameObject by previous prototypes.
    /// </summary>
    private void Awake()
    {
        oldMeshCollider = GetComponent<MeshCollider>();
        oldMeshRenderer = GetComponent<MeshRenderer>();

        DisableOldMeshCollision();
        CreateGeneratedStripRoot();
        CachePlayer();
    }

    /// <summary>
    /// Builds the first set of walkable shadow strips
    /// when the scene starts.
    /// </summary>
    private void Start()
    {
        List<EdgeSegment> firstEdges = BuildWalkableEdges();

        UpdateGeneratedStrips(firstEdges);
        StoreCurrentEdges(firstEdges);
    }

    /// <summary>
    /// Rebuilds the shadow edge strips during physics updates
    /// and carries the player with the updated projected edge.
    /// </summary>
    private void FixedUpdate()
    {
        if (!CanBuildWalkway())
        {
            DisableAllGeneratedStrips();
            currentEdges.Clear();
            return;
        }

        CachePlayer();

        List<EdgeSegment> nextEdges = BuildWalkableEdges();

        if (carryPlayerWithShadow)
        {
            CarryPlayerWithShadowEdge(currentEdges, nextEdges);
        }

        UpdateGeneratedStrips(nextEdges);
        StoreCurrentEdges(nextEdges);
    }

    /// <summary>
    /// Disables the previous runtime MeshCollider approach
    /// so only generated BoxCollider strips are used for support.
    /// </summary>
    private void DisableOldMeshCollision()
    {
        if (oldMeshCollider != null)
        {
            oldMeshCollider.enabled = false;
        }

        if (oldMeshRenderer != null)
        {
            oldMeshRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Creates or finds the child container used
    /// for generated BoxCollider shadow strips.
    /// </summary>
    private void CreateGeneratedStripRoot()
    {
        Transform existingRoot = transform.Find("GeneratedShadowEdgeStrips");

        if (existingRoot != null)
        {
            generatedStripRoot = existingRoot;
            return;
        }

        GameObject rootObject = new GameObject("GeneratedShadowEdgeStrips");
        rootObject.transform.SetParent(transform, false);

        generatedStripRoot = rootObject.transform;
    }

    /// <summary>
    /// Finds and caches the player Rigidbody and Collider
    /// using the configured player tag.
    /// </summary>
    private void CachePlayer()
    {
        if (playerRigidbody != null && playerCollider != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject == null)
        {
            return;
        }

        playerRigidbody = playerObject.GetComponent<Rigidbody>();
        playerCollider = playerObject.GetComponent<Collider>();
    }

    /// <summary>
    /// Checks whether every scene reference needed
    /// for projected edge generation exists.
    /// </summary>
    /// <returns>True when the walkway can be rebuilt.</returns>
    private bool CanBuildWalkway()
    {
        return shadowCasterRenderer != null
            && shadowScreen != null
            && directionalLight != null
            && generatedStripRoot != null;
    }

    /// <summary>
    /// Projects the caster corners, builds the projected hull,
    /// and extracts the upper walkable shadow edges.
    /// </summary>
    /// <returns>Walkable upper projected shadow edges.</returns>
    private List<EdgeSegment> BuildWalkableEdges()
    {
        List<ProjectedPoint> projectedPoints = ProjectShadowCasterBounds();

        if (projectedPoints.Count < 3)
        {
            return new List<EdgeSegment>();
        }

        List<ProjectedPoint> hull = BuildConvexHull(projectedPoints);

        if (hull.Count < 3)
        {
            return new List<EdgeSegment>();
        }

        EnsureCounterClockwiseHull(hull);

        return ExtractUpperEdges(hull);
    }

    /// <summary>
    /// Projects all eight world-space bounds corners
    /// of the shadow caster onto the shadow screen.
    /// </summary>
    /// <returns>Projected points on the shadow screen.</returns>
    private List<ProjectedPoint> ProjectShadowCasterBounds()
    {
        List<ProjectedPoint> projectedPoints = new List<ProjectedPoint>();

        Bounds bounds = shadowCasterRenderer.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
        {
            Vector3 cornerOffset = new Vector3(
                (cornerIndex & 1) == 0 ? -extents.x : extents.x,
                (cornerIndex & 2) == 0 ? -extents.y : extents.y,
                (cornerIndex & 4) == 0 ? -extents.z : extents.z
            );

            Vector3 worldCorner = center + cornerOffset;

            if (TryProjectPointToShadowScreen(worldCorner, out Vector2 projectedPoint))
            {
                projectedPoints.Add(
                    new ProjectedPoint(projectedPoint, cornerIndex)
                );
            }
        }

        return projectedPoints;
    }

    /// <summary>
    /// Projects one world-space point along the directional light
    /// onto the shadow screen plane.
    /// </summary>
    /// <param name="worldPoint">World-space point to project.</param>
    /// <param name="projectedPoint">Projected 2D point on the shadow screen.</param>
    /// <returns>True when the projection is valid.</returns>
    private bool TryProjectPointToShadowScreen(
        Vector3 worldPoint,
        out Vector2 projectedPoint
    )
    {
        projectedPoint = Vector2.zero;

        Vector3 screenNormal = shadowScreen.forward.normalized;
        Vector3 projectionDirection = directionalLight.transform.forward.normalized;

        float denominator = Vector3.Dot(screenNormal, projectionDirection);

        if (Mathf.Abs(denominator) < 0.0001f)
        {
            return false;
        }

        float projectionDistance =
            Vector3.Dot(screenNormal, shadowScreen.position - worldPoint)
            / denominator;

        Vector3 projectedWorldPoint =
            worldPoint + projectionDirection * projectionDistance;

        Vector3 projectedScreenLocalPoint =
            shadowScreen.InverseTransformPoint(projectedWorldPoint);

        projectedPoint = new Vector2(
            projectedScreenLocalPoint.x,
            projectedScreenLocalPoint.y
        );

        return true;
    }

    /// <summary>
    /// Builds a convex hull around projected shadow points.
    /// </summary>
    /// <param name="points">Projected caster points.</param>
    /// <returns>Ordered projected convex hull points.</returns>
    private List<ProjectedPoint> BuildConvexHull(List<ProjectedPoint> points)
    {
        List<ProjectedPoint> sortedPoints = RemoveDuplicatePoints(points);

        sortedPoints.Sort((a, b) =>
        {
            int xComparison = a.position.x.CompareTo(b.position.x);

            if (xComparison != 0)
            {
                return xComparison;
            }

            return a.position.y.CompareTo(b.position.y);
        });

        if (sortedPoints.Count <= 2)
        {
            return sortedPoints;
        }

        List<ProjectedPoint> lowerHull = new List<ProjectedPoint>();

        for (int i = 0; i < sortedPoints.Count; i++)
        {
            while (
                lowerHull.Count >= 2
                && Cross(
                    lowerHull[lowerHull.Count - 2].position,
                    lowerHull[lowerHull.Count - 1].position,
                    sortedPoints[i].position
                ) <= 0f
            )
            {
                lowerHull.RemoveAt(lowerHull.Count - 1);
            }

            lowerHull.Add(sortedPoints[i]);
        }

        List<ProjectedPoint> upperHull = new List<ProjectedPoint>();

        for (int i = sortedPoints.Count - 1; i >= 0; i--)
        {
            while (
                upperHull.Count >= 2
                && Cross(
                    upperHull[upperHull.Count - 2].position,
                    upperHull[upperHull.Count - 1].position,
                    sortedPoints[i].position
                ) <= 0f
            )
            {
                upperHull.RemoveAt(upperHull.Count - 1);
            }

            upperHull.Add(sortedPoints[i]);
        }

        lowerHull.RemoveAt(lowerHull.Count - 1);
        upperHull.RemoveAt(upperHull.Count - 1);

        List<ProjectedPoint> hull = new List<ProjectedPoint>();
        hull.AddRange(lowerHull);
        hull.AddRange(upperHull);

        return hull;
    }

    /// <summary>
    /// Ensures the hull point order is counterclockwise,
    /// which makes upward-facing edge detection reliable.
    /// </summary>
    /// <param name="hull">Projected convex hull to check.</param>
    private void EnsureCounterClockwiseHull(List<ProjectedPoint> hull)
    {
        if (CalculateSignedArea(hull) < 0f)
        {
            hull.Reverse();
        }
    }

    /// <summary>
    /// Calculates the signed area of the projected hull.
    /// </summary>
    /// <param name="hull">Projected hull points.</param>
    /// <returns>Signed polygon area.</returns>
    private float CalculateSignedArea(List<ProjectedPoint> hull)
    {
        float area = 0f;

        for (int i = 0; i < hull.Count; i++)
        {
            Vector2 current = hull[i].position;
            Vector2 next = hull[(i + 1) % hull.Count].position;

            area += current.x * next.y - next.x * current.y;
        }

        return area * 0.5f;
    }

    /// <summary>
    /// Extracts edges whose outward direction faces upward
    /// on the shadow screen.
    /// </summary>
    /// <param name="hull">Counterclockwise projected shadow hull.</param>
    /// <returns>Walkable upper shadow edges.</returns>
    private List<EdgeSegment> ExtractUpperEdges(List<ProjectedPoint> hull)
    {
        List<EdgeSegment> edges = new List<EdgeSegment>();

        for (int i = 0; i < hull.Count; i++)
        {
            ProjectedPoint firstPoint = hull[i];
            ProjectedPoint secondPoint = hull[(i + 1) % hull.Count];

            Vector2 rawEdge = secondPoint.position - firstPoint.position;

            if (rawEdge.magnitude < minEdgeLength)
            {
                continue;
            }

            if (Mathf.Abs(rawEdge.x) < minHorizontalSpan)
            {
                continue;
            }

            Vector2 outwardNormal = new Vector2(
                rawEdge.y,
                -rawEdge.x
            ).normalized;

            if (outwardNormal.y <= 0.01f)
            {
                continue;
            }

            ProjectedPoint leftPoint = firstPoint;
            ProjectedPoint rightPoint = secondPoint;

            if (leftPoint.position.x > rightPoint.position.x)
            {
                ProjectedPoint temporaryPoint = leftPoint;
                leftPoint = rightPoint;
                rightPoint = temporaryPoint;
            }

            Vector2 edgeDirection =
                (rightPoint.position - leftPoint.position).normalized;

            Vector2 paddedLeft =
                leftPoint.position
                - edgeDirection * widthPadding
                + Vector2.up * verticalOffset;

            Vector2 paddedRight =
                rightPoint.position
                + edgeDirection * widthPadding
                + Vector2.up * verticalOffset;

            edges.Add(
                new EdgeSegment(
                    paddedLeft,
                    paddedRight,
                    leftPoint.sourceCornerIndex,
                    rightPoint.sourceCornerIndex
                )
            );
        }

        edges.Sort((a, b) =>
        {
            return a.MidPoint.x.CompareTo(b.MidPoint.x);
        });

        return edges;
    }

    /// <summary>
    /// Removes projected duplicate points before hull generation.
    /// </summary>
    /// <param name="points">Raw projected points.</param>
    /// <returns>Unique projected points.</returns>
    private List<ProjectedPoint> RemoveDuplicatePoints(
        List<ProjectedPoint> points
    )
    {
        List<ProjectedPoint> uniquePoints = new List<ProjectedPoint>();

        for (int i = 0; i < points.Count; i++)
        {
            bool alreadyStored = false;

            for (int j = 0; j < uniquePoints.Count; j++)
            {
                if (
                    Vector2.Distance(
                        points[i].position,
                        uniquePoints[j].position
                    ) < 0.0001f
                )
                {
                    alreadyStored = true;
                    break;
                }
            }

            if (!alreadyStored)
            {
                uniquePoints.Add(points[i]);
            }
        }

        return uniquePoints;
    }

    /// <summary>
    /// Calculates the signed 2D cross product used
    /// by the convex hull algorithm.
    /// </summary>
    /// <param name="a">First point.</param>
    /// <param name="b">Second point.</param>
    /// <param name="c">Third point.</param>
    /// <returns>Signed turn value.</returns>
    private float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a;
        Vector2 ac = c - a;

        return ab.x * ac.y - ab.y * ac.x;
    }

    /// <summary>
    /// Updates or creates BoxCollider strips
    /// for every current walkable edge.
    /// </summary>
    /// <param name="edges">Current projected walkable edges.</param>
    private void UpdateGeneratedStrips(List<EdgeSegment> edges)
    {
        EnsureGeneratedStripCount(edges.Count);

        for (int i = 0; i < generatedStripObjects.Count; i++)
        {
            bool shouldBeActive = i < edges.Count;

            generatedStripObjects[i].SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                ConfigureGeneratedStrip(
                    generatedStripObjects[i],
                    edges[i],
                    i
                );
            }
        }
    }

    /// <summary>
    /// Creates enough generated strip GameObjects
    /// to represent all current walkable edges.
    /// </summary>
    /// <param name="requiredCount">Required number of edge strips.</param>
    private void EnsureGeneratedStripCount(int requiredCount)
    {
        while (generatedStripObjects.Count < requiredCount)
        {
            GameObject stripObject = CreateGeneratedStrip(
                generatedStripObjects.Count
            );

            generatedStripObjects.Add(stripObject);
        }
    }

    /// <summary>
    /// Creates one cube strip with a BoxCollider
    /// for a projected shadow edge.
    /// </summary>
    /// <param name="index">Strip index for naming.</param>
    /// <returns>Generated strip GameObject.</returns>
    private GameObject CreateGeneratedStrip(int index)
    {
        GameObject stripObject =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        stripObject.name = "GeneratedShadowEdgeStrip_" + index;
        stripObject.transform.SetParent(generatedStripRoot, false);
        stripObject.layer = gameObject.layer;

        BoxCollider boxCollider = stripObject.GetComponent<BoxCollider>();

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
        }

        MeshRenderer stripRenderer =
            stripObject.GetComponent<MeshRenderer>();

        if (stripRenderer != null)
        {
            stripRenderer.enabled = showDebugWalkway;
            stripRenderer.shadowCastingMode = ShadowCastingMode.Off;
            stripRenderer.receiveShadows = false;

            Material stripMaterial = ResolveDebugMaterial();

            if (stripMaterial != null)
            {
                stripRenderer.sharedMaterial = stripMaterial;
            }
        }

        return stripObject;
    }

    /// <summary>
    /// Positions, rotates, and sizes one generated BoxCollider strip
    /// so its top face matches a projected shadow edge.
    /// </summary>
    /// <param name="stripObject">Generated strip object to configure.</param>
    /// <param name="edge">Projected edge represented by the strip.</param>
    /// <param name="index">Strip index for naming.</param>
    private void ConfigureGeneratedStrip(
        GameObject stripObject,
        EdgeSegment edge,
        int index
    )
    {
        Vector2 edgeVector = edge.right - edge.left;
        float edgeLength = edgeVector.magnitude;

        if (edgeLength < 0.0001f)
        {
            stripObject.SetActive(false);
            return;
        }

        Vector2 edgeDirection = edgeVector.normalized;

        Vector2 upperNormal = new Vector2(
            -edgeDirection.y,
            edgeDirection.x
        );

        if (upperNormal.y < 0f)
        {
            upperNormal = -upperNormal;
        }

        Vector2 edgeMidpoint =
            (edge.left + edge.right) * 0.5f;

        Vector2 stripCenter =
            edgeMidpoint - upperNormal * (platformThickness * 0.5f);

        Vector3 worldCenter = shadowScreen.TransformPoint(
            new Vector3(stripCenter.x, stripCenter.y, 0f)
        );

        float edgeAngle =
            Mathf.Atan2(edgeDirection.y, edgeDirection.x)
            * Mathf.Rad2Deg;

        Quaternion worldRotation =
            shadowScreen.rotation
            * Quaternion.Euler(0f, 0f, edgeAngle);

        stripObject.name =
            "GeneratedShadowEdgeStrip_" + index;

        stripObject.transform.position = worldCenter;
        stripObject.transform.rotation = worldRotation;
        stripObject.transform.localScale = new Vector3(
            edgeLength,
            platformThickness,
            platformDepth
        );

        MeshRenderer stripRenderer =
            stripObject.GetComponent<MeshRenderer>();

        if (stripRenderer != null)
        {
            stripRenderer.enabled = showDebugWalkway;
        }
    }

    /// <summary>
    /// Resolves the material used for visible debug shadow edge strips.
    /// </summary>
    /// <returns>Configured debug material or parent renderer material.</returns>
    private Material ResolveDebugMaterial()
    {
        if (debugMaterial != null)
        {
            return debugMaterial;
        }

        if (oldMeshRenderer != null)
        {
            return oldMeshRenderer.sharedMaterial;
        }

        return null;
    }

    /// <summary>
    /// Carries the player from the previous support edge point
    /// to the matching point on the changed projected edge.
    /// </summary>
    /// <param name="previousEdges">Edges before the light update.</param>
    /// <param name="nextEdges">Edges after the light update.</param>
    private void CarryPlayerWithShadowEdge(
        List<EdgeSegment> previousEdges,
        List<EdgeSegment> nextEdges
    )
    {
        if (
            playerRigidbody == null
            || playerCollider == null
            || previousEdges.Count == 0
            || nextEdges.Count == 0
        )
        {
            return;
        }

        if (playerRigidbody.velocity.y > maxPassengerUpwardSpeed)
        {
            return;
        }

        Vector3 playerFootWorldPoint = new Vector3(
            playerCollider.bounds.center.x,
            playerCollider.bounds.min.y,
            playerCollider.bounds.center.z
        );

        Vector3 playerFootScreenPoint3D =
            shadowScreen.InverseTransformPoint(playerFootWorldPoint);

        Vector2 playerFootPoint = new Vector2(
            playerFootScreenPoint3D.x,
            playerFootScreenPoint3D.y
        );

        if (
            !TryFindSupportEdge(
                previousEdges,
                playerFootPoint,
                out EdgeSegment previousSupportEdge,
                out int previousSupportIndex,
                out float edgeAmount,
                out Vector2 previousAnchorPoint
            )
        )
        {
            return;
        }

        if (
            !TryFindMatchingNextEdge(
                previousSupportEdge,
                previousSupportIndex,
                nextEdges,
                out EdgeSegment nextSupportEdge
            )
        )
        {
            return;
        }

        Vector2 nextAnchorPoint = Vector2.Lerp(
            nextSupportEdge.left,
            nextSupportEdge.right,
            edgeAmount
        );

        Vector2 screenDelta =
            nextAnchorPoint - previousAnchorPoint;

        Vector3 worldDelta = shadowScreen.TransformVector(
            new Vector3(screenDelta.x, screenDelta.y, 0f)
        );

        if (worldDelta.magnitude > maxCarryDistancePerPhysicsStep)
        {
            worldDelta =
                worldDelta.normalized * maxCarryDistancePerPhysicsStep;
        }

        playerRigidbody.MovePosition(
            playerRigidbody.position + worldDelta
        );
    }

    /// <summary>
    /// Finds which previous projected edge is supporting
    /// the player's current foot point.
    /// </summary>
    /// <param name="edges">Previous walkable edges.</param>
    /// <param name="playerFootPoint">Foot point on shadow screen.</param>
    /// <param name="supportEdge">Supporting edge.</param>
    /// <param name="supportIndex">Supporting edge list index.</param>
    /// <param name="edgeAmount">Normalized location along the support edge.</param>
    /// <param name="anchorPoint">Closest point on the support edge.</param>
    /// <returns>True when a support edge is found.</returns>
    private bool TryFindSupportEdge(
        List<EdgeSegment> edges,
        Vector2 playerFootPoint,
        out EdgeSegment supportEdge,
        out int supportIndex,
        out float edgeAmount,
        out Vector2 anchorPoint
    )
    {
        supportEdge = new EdgeSegment();
        supportIndex = -1;
        edgeAmount = 0f;
        anchorPoint = Vector2.zero;

        float bestDistance = float.MaxValue;
        bool foundSupport = false;

        for (int i = 0; i < edges.Count; i++)
        {
            EdgeSegment edge = edges[i];

            float minX =
                Mathf.Min(edge.left.x, edge.right.x)
                - passengerHorizontalTolerance;

            float maxX =
                Mathf.Max(edge.left.x, edge.right.x)
                + passengerHorizontalTolerance;

            if (
                playerFootPoint.x < minX
                || playerFootPoint.x > maxX
            )
            {
                continue;
            }

            float candidateAmount = GetClosestAmountOnSegment(
                playerFootPoint,
                edge.left,
                edge.right
            );

            Vector2 candidateAnchor = Vector2.Lerp(
                edge.left,
                edge.right,
                candidateAmount
            );

            float candidateDistance = Vector2.Distance(
                playerFootPoint,
                candidateAnchor
            );

            if (
                candidateDistance <= passengerContactTolerance
                && candidateDistance < bestDistance
            )
            {
                bestDistance = candidateDistance;
                supportEdge = edge;
                supportIndex = i;
                edgeAmount = candidateAmount;
                anchorPoint = candidateAnchor;
                foundSupport = true;
            }
        }

        return foundSupport;
    }

    /// <summary>
    /// Finds the next projected edge corresponding
    /// to the player's previous support edge.
    /// </summary>
    /// <param name="previousEdge">Previous support edge.</param>
    /// <param name="previousIndex">Previous support edge list index.</param>
    /// <param name="nextEdges">Updated projected edges.</param>
    /// <param name="matchingEdge">Matched updated edge.</param>
    /// <returns>True when a matching edge is found.</returns>
    private bool TryFindMatchingNextEdge(
        EdgeSegment previousEdge,
        int previousIndex,
        List<EdgeSegment> nextEdges,
        out EdgeSegment matchingEdge
    )
    {
        for (int i = 0; i < nextEdges.Count; i++)
        {
            if (HasSameSourceCorners(previousEdge, nextEdges[i]))
            {
                matchingEdge = nextEdges[i];
                return true;
            }
        }

        if (
            previousIndex >= 0
            && previousIndex < nextEdges.Count
        )
        {
            matchingEdge = nextEdges[previousIndex];
            return true;
        }

        float bestDistance = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < nextEdges.Count; i++)
        {
            float distance = Vector2.Distance(
                previousEdge.MidPoint,
                nextEdges[i].MidPoint
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        if (
            bestIndex >= 0
            && bestDistance <= fallbackEdgeMatchDistance
        )
        {
            matchingEdge = nextEdges[bestIndex];
            return true;
        }

        matchingEdge = new EdgeSegment();
        return false;
    }

    /// <summary>
    /// Checks whether two edge segments come
    /// from the same caster corner pair.
    /// </summary>
    /// <param name="firstEdge">First edge.</param>
    /// <param name="secondEdge">Second edge.</param>
    /// <returns>True when both edges share source corners.</returns>
    private bool HasSameSourceCorners(
        EdgeSegment firstEdge,
        EdgeSegment secondEdge
    )
    {
        bool sameDirection =
            firstEdge.leftSourceCornerIndex == secondEdge.leftSourceCornerIndex
            && firstEdge.rightSourceCornerIndex == secondEdge.rightSourceCornerIndex;

        bool oppositeDirection =
            firstEdge.leftSourceCornerIndex == secondEdge.rightSourceCornerIndex
            && firstEdge.rightSourceCornerIndex == secondEdge.leftSourceCornerIndex;

        return sameDirection || oppositeDirection;
    }

    /// <summary>
    /// Calculates the closest normalized location
    /// on a segment to a point.
    /// </summary>
    /// <param name="point">Point to compare.</param>
    /// <param name="segmentStart">Segment start.</param>
    /// <param name="segmentEnd">Segment end.</param>
    /// <returns>Normalized segment amount from 0 to 1.</returns>
    private float GetClosestAmountOnSegment(
        Vector2 point,
        Vector2 segmentStart,
        Vector2 segmentEnd
    )
    {
        Vector2 segment = segmentEnd - segmentStart;
        float segmentLengthSquared = segment.sqrMagnitude;

        if (segmentLengthSquared < 0.0001f)
        {
            return 0f;
        }

        float amount = Vector2.Dot(
            point - segmentStart,
            segment
        ) / segmentLengthSquared;

        return Mathf.Clamp01(amount);
    }

    /// <summary>
    /// Stores the latest projected edges
    /// for the next physics update.
    /// </summary>
    /// <param name="edges">Latest walkable edges.</param>
    private void StoreCurrentEdges(List<EdgeSegment> edges)
    {
        currentEdges.Clear();
        currentEdges.AddRange(edges);
    }

    /// <summary>
    /// Disables every generated strip
    /// when the walkway cannot be rebuilt.
    /// </summary>
    private void DisableAllGeneratedStrips()
    {
        for (int i = 0; i < generatedStripObjects.Count; i++)
        {
            generatedStripObjects[i].SetActive(false);
        }
    }
}