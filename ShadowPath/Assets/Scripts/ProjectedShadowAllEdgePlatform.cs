using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Purpose: Turns the visible edge of a shadow into real Unity colliders.
/// Input: The object casting the shadow, the screen/wall receiving it, and the light direction.
/// Output: The player can stand on useful shadow edges, while steep edges work like walls.
/// </summary>
[DefaultExecutionOrder(-50)]
public class ProjectedShadowAllEdgePlatform : MonoBehaviour
{
    [Header("Scene References")]
    // The object whose shadow should become playable.
    public MeshRenderer shadowCasterRenderer;
    // The wall/screen where the shadow appears.
    public Transform shadowScreen;
    // The light that decides where the shadow is projected.
    public Light directionalLight;

    [Header("Walkable Edge Shape")]
    // Thickness of each generated shadow platform strip.
    public float platformThickness = 0.08f;
    // Depth of the generated collider, so the player can collide with it in 3D space.
    public float platformDepth = 8f;
    // Extra length added to both ends of each edge to avoid tiny gaps.
    public float edgePadding = 0.04f;
    // Small offset from the screen so the collider does not visually overlap the wall.
    public float surfaceOffset = -0.02f;
    // Very short shadow edges below this length are ignored.
    public float minEdgeLength = 0.001f;

    // How upward-facing an edge must be before the player can stand on it.
    [Range(0f, 1f)]
    public float minWalkableNormalY = 0.65f;

    [Header("Curved Caster Sampling")]
    // If true, round objects get extra sample points so their shadows are smoother.
    public bool addCurvedCasterSamples = true;

    // Number of extra points used around a round object.
    [Min(8)]
    public int curvedCasterSampleCount = 48;

    // How close the object's X/Z size must be before it is treated as round.
    [Range(0f, 1f)]
    public float curvedCasterRadiusTolerance = 0.18f;

    // Extra padding for curved shadow edges to avoid small cracks.
    [Min(0f)]
    public float curvedEdgeExtraPadding = 0.02f;

    // If true, adds a more stable top support edge for round shadows.
    public bool addCurvedTopSupport = true;

    // Thickness used for the extra top support on curved shadows.
    [Min(0.01f)]
    public float curvedTopSupportThickness = 0.04f;

    // Extra length for the curved top support.
    [Min(0f)]
    public float curvedTopSupportPadding = 0.035f;

    // How close an edge must be to the top of the shadow before it counts as top support.
    [Min(0f)]
    public float curvedTopEnvelopeTolerance = 0.06f;

    // How upward-facing the original surface should be before curved top support is allowed.
    [Range(0f, 1f)]
    public float curvedTopSupportMinSourceNormalY = 0.25f;

    [Header("Edge Selection Debug")]
    // If true, creates all shadow edges so you can see/debug them; if false, only useful walkable edges are made.
    public bool buildAllEdgesForDebug = true;
    // If true, only the highest/top shadow edges can be walkable.
    public bool walkableEdgesMustBeUpperEnvelope = false;
    // Reverses the walkable side if the generated edge direction is wrong.
    public bool flipWalkableNormal = false;
    // If true, the script tries the opposite side when an edge points downward.
    public bool useOppositeNormalFallback = true;

    [Header("Update")]
    // If true, rebuilds the shadow colliders while the light or object moves.
    public bool rebuildEveryFrame = true;

    [Header("Generated Collider Settings")]
    // Layer name assigned to generated shadow colliders.
    public string groundLayerName = "Ground";
    // If true, adds a kinematic Rigidbody so generated colliders behave correctly in physics.
    public bool addKinematicRigidbody = true;
    // Physics material used on walkable shadow edges.
    public PhysicMaterial platformPhysicsMaterial;
    // Physics material used on steep shadow edges.
    public PhysicMaterial steepEdgePhysicsMaterial;

    [Header("Passenger Carry")]
    // If true, moving shadows can carry the player standing on them.
    public bool carryPlayerWithShadow = true;
    // Tag used to find the player object.
    public string playerTag = "Player";
    // How close the player must be to a shadow edge to count as standing on it.
    public float passengerContactTolerance = 0.35f;
    // If the player is moving upward faster than this, the shadow will not carry them.
    public float maxPassengerUpwardSpeed = 1f;
    // Tiny shadow movements below this are ignored.
    public float minCarryDistancePerStep = 0.005f;
    // Shadow movements above this are considered too large to safely carry the player.
    public float maxCarryDistancePerStep = 0.25f;
    // Maximum distance allowed when matching an old shadow edge to the new one.
    public float maxEdgeMatchDistance = 1.2f;
    // How similar the old and new edge direction must be before the player can be carried.
    [Range(0f, 1f)]
    public float minCarryEdgeDirectionDot = 0.75f;

    // Extra direction check for edge matching; lower is more forgiving.
    [Range(-1f, 1f)]
    public float minEdgeDirectionDot = 0f;

    [Header("Debug Visual")]
    // If true, generated shadow collider strips are visible in the Scene/Game view.
    public bool showDebugSurface = true;
    // Material used to draw the debug shadow collider strips.
    public Material debugMaterial;

    [Header("Read Only Status")]
    [SerializeField] private int lastProjectedPointCount;
    [SerializeField] private int lastHullPointCount;
    [SerializeField] private int lastGeneratedEdgeCount;
    [SerializeField] private int lastWalkableEdgeCount;
    [SerializeField] private int lastSteepEdgeCount;
    [SerializeField] private int lastCurvedSamplePointCount;
    [SerializeField] private int lastCurvedTopSupportEdgeCount;
    [SerializeField] private string lastBuildMessage;

    private readonly List<GameObject> generatedEdges = new List<GameObject>();
    private Transform generatedEdgeRoot;
    private readonly List<EdgeSnapshot> previousEdges = new List<EdgeSnapshot>();
    private readonly List<EdgeSnapshot> nextEdges = new List<EdgeSnapshot>();

    private readonly Dictionary<Collider, int> colliderGeneratedIndices = new Dictionary<Collider, int>();
    private readonly Dictionary<Collider, bool> colliderWalkableStates = new Dictionary<Collider, bool>();

    private Rigidbody playerRigidbody;
    private Collider playerCollider;
    private PlayerController playerController;
    private bool currentBuildUsesCurvedSamples;

    private struct EdgeSnapshot
    {
        // Which generated collider object this edge belongs to.
        public int generatedIndex;
        // Which original shadow outline edge this came from.
        public int edgeId;
        // Start point of the edge in the Unity scene.
        public Vector3 worldStart;
        // End point of the edge in the Unity scene.
        public Vector3 worldEnd;
        // True means the player is allowed to stand on this edge.
        public bool isWalkable;

        /// <summary>
        /// Purpose: Saves one shadow edge so the script can compare it with the next frame.
        /// Input: Edge index, edge start/end points, and whether the player can stand on it.
        /// Output: Player-carrying code can tell how the shadow moved.
        /// </summary>
        public EdgeSnapshot(
            int generatedIndex,
            int edgeId,
            Vector3 worldStart,
            Vector3 worldEnd,
            bool isWalkable
        )
        {
            this.generatedIndex = generatedIndex;
            this.edgeId = edgeId;
            this.worldStart = worldStart;
            this.worldEnd = worldEnd;
            this.isWalkable = isWalkable;
        }
    }

    /// <summary>
    /// Purpose: Fixes older scene values before gameplay starts.
    /// Input: Current Inspector values.
    /// Output: Curved shadow settings have safe default values.
    /// </summary>
    private void Awake()
    {
        NormalizeCurvedCasterDefaults();
    }

    /// <summary>
    /// Purpose: Keeps Inspector values valid while editing.
    /// Input: Changed Inspector values or script reloads.
    /// Output: Curved shadow settings do not accidentally become zero.
    /// </summary>
    private void OnValidate()
    {
        NormalizeCurvedCasterDefaults();
    }

    /// <summary>
    /// Purpose: Builds the first shadow colliders when the level starts.
    /// Input: Scene references and Inspector settings.
    /// Output: Shadow edge colliders exist before the player uses them.
    /// </summary>
    private void Start()
    {
        Rebuild();
    }

    /// <summary>
    /// Purpose: Updates shadow colliders during physics.
    /// Input: Current light/object position and the previous shadow edge positions.
    /// Output: Colliders follow the shadow, and moving shadows can carry the player.
    /// </summary>
    private void FixedUpdate()
    {
        if (rebuildEveryFrame)
        {
            Rebuild();
        }
    }

    /// <summary>
    /// Purpose: Rebuilds the playable shadow edges.
    /// Input: The caster mesh, light direction, and shadow screen.
    /// Output: New BoxColliders are placed along the current shadow outline.
    /// </summary>
    public void Rebuild()
    {
        lastProjectedPointCount = 0;
        lastHullPointCount = 0;
        lastGeneratedEdgeCount = 0;
        lastWalkableEdgeCount = 0;
        lastSteepEdgeCount = 0;
        lastCurvedSamplePointCount = 0;
        lastCurvedTopSupportEdgeCount = 0;
        lastBuildMessage = "";
        currentBuildUsesCurvedSamples = false;

        colliderGeneratedIndices.Clear();
        colliderWalkableStates.Clear();

        if (!HasRequiredReferences())
        {
            HideUnusedEdges(0);
            return;
        }

        List<Vector2> projectedPoints = GetProjectedPointsOnScreen();
        lastProjectedPointCount = projectedPoints.Count;

        if (projectedPoints.Count < 3)
        {
            lastBuildMessage = "Not enough projected points.";
            HideUnusedEdges(0);
            return;
        }

        List<Vector2> hull = BuildConvexHull(projectedPoints);
        lastHullPointCount = hull.Count;

        if (hull.Count < 2)
        {
            lastBuildMessage = "Projected hull is too small.";
            HideUnusedEdges(0);
            return;
        }

        nextEdges.Clear();
        lastGeneratedEdgeCount = BuildEdgesFromHull(hull, 0);

        if (carryPlayerWithShadow)
        {
            CarryPlayerWithShadowEdges();
        }

        StoreCurrentEdges();
        HideUnusedEdges(lastGeneratedEdgeCount);

        if (lastGeneratedEdgeCount == 0)
        {
            lastBuildMessage = "Projected hull exists, but no edge was generated.";
        }
        else if (buildAllEdgesForDebug)
        {
            lastBuildMessage = "Generated all shadow edges. Only walkable current-ground edges can carry the player.";
        }
        else
        {
            lastBuildMessage = "Generated walkable shadow edges only.";
        }
    }

    /// <summary>
    /// Purpose: Checks that the important scene references are assigned.
    /// Input: Caster object, shadow screen, and light fields.
    /// Output: True means the script has enough information to build a shadow.
    /// </summary>
    private bool HasRequiredReferences()
    {
        if (shadowCasterRenderer == null)
        {
            lastBuildMessage = "Missing shadow caster renderer.";
            return false;
        }

        if (shadowScreen == null)
        {
            lastBuildMessage = "Missing shadow screen.";
            return false;
        }

        if (directionalLight == null)
        {
            lastBuildMessage = "Missing directional light.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Purpose: Gets the shadow points on the screen.
    /// Input: Mesh points from the caster object and the current light direction.
    /// Output: 2D points showing where the shadow lands on the screen.
    /// </summary>
    private List<Vector2> GetProjectedPointsOnScreen()
    {
        List<Vector2> points = new List<Vector2>();

        MeshFilter meshFilter = shadowCasterRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            lastBuildMessage = "Caster has no MeshFilter or mesh.";
            return points;
        }

        Plane screenPlane = new Plane(shadowScreen.forward, shadowScreen.position);
        Vector3 lightDirection = directionalLight.transform.forward.normalized;

        foreach (Vector3 localVertex in meshFilter.sharedMesh.vertices)
        {
            Vector3 worldVertex = shadowCasterRenderer.transform.TransformPoint(localVertex);
            AddProjectedPoint(worldVertex, screenPlane, lightDirection, points);
        }

        AddCurvedCasterProjectionSamples(
            meshFilter.sharedMesh,
            screenPlane,
            lightDirection,
            points
        );

        return points;
    }

    /// <summary>
    /// Purpose: Projects one point from the object onto the shadow screen.
    /// Input: One world point, the screen plane, and the light direction.
    /// Output: One 2D shadow point is added if it reaches the screen.
    /// </summary>
    private void AddProjectedPoint(
        Vector3 worldPoint,
        Plane screenPlane,
        Vector3 lightDirection,
        List<Vector2> points
    )
    {
        Ray projectionRay = new Ray(worldPoint, lightDirection);

        if (!screenPlane.Raycast(projectionRay, out float distance))
        {
            return;
        }

        Vector3 worldHit = projectionRay.GetPoint(distance);
        Vector3 screenLocalHit = shadowScreen.InverseTransformPoint(worldHit);
        points.Add(new Vector2(screenLocalHit.x, screenLocalHit.y));
    }

    /// <summary>
    /// Purpose: Adds extra points for round objects.
    /// Input: The caster mesh size, the screen, and the light direction.
    /// Output: Round shadows become smoother and less broken.
    /// </summary>
    private void AddCurvedCasterProjectionSamples(
        Mesh casterMesh,
        Plane screenPlane,
        Vector3 lightDirection,
        List<Vector2> points
    )
    {
        if (!ShouldAddCurvedCasterSamples(casterMesh))
        {
            return;
        }

        Bounds localBounds = casterMesh.bounds;
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;

        float radiusX = Mathf.Max(0.0001f, extents.x);
        float radiusZ = Mathf.Max(0.0001f, extents.z);
        float lowerY = center.y - extents.y;
        float upperY = center.y + extents.y;
        int sampleCount = Mathf.Clamp(curvedCasterSampleCount, 8, 128);

        currentBuildUsesCurvedSamples = true;

        AddCurvedCasterRingSamples(
            center,
            lowerY,
            radiusX,
            radiusZ,
            sampleCount,
            screenPlane,
            lightDirection,
            points
        );

        AddCurvedCasterRingSamples(
            center,
            upperY,
            radiusX,
            radiusZ,
            sampleCount,
            screenPlane,
            lightDirection,
            points
        );
    }

    /// <summary>
    /// Purpose: Decides if this object looks round enough to need extra points.
    /// Input: Mesh name, vertex count, and object size.
    /// Output: True means treat it like a cylinder/capsule shadow.
    /// </summary>
    private bool ShouldAddCurvedCasterSamples(Mesh casterMesh)
    {
        NormalizeCurvedCasterDefaults();

        if (!addCurvedCasterSamples || casterMesh == null)
        {
            return false;
        }

        string meshName = casterMesh.name.ToLowerInvariant();

        if (meshName.Contains("cylinder") || meshName.Contains("capsule"))
        {
            return true;
        }

        if (casterMesh.vertexCount < 32)
        {
            return false;
        }

        Vector3 extents = casterMesh.bounds.extents;
        float largerRadius = Mathf.Max(extents.x, extents.z);

        if (largerRadius <= 0.0001f)
        {
            return false;
        }

        float radiusDifference = Mathf.Abs(extents.x - extents.z) / largerRadius;

        return radiusDifference <= curvedCasterRadiusTolerance;
    }

    /// <summary>
    /// Purpose: Adds points around one circular slice of a round object.
    /// Input: Circle position, circle size, sample count, screen, and light direction.
    /// Output: More shadow points are added around the round shape.
    /// </summary>
    private void AddCurvedCasterRingSamples(
        Vector3 center,
        float localY,
        float radiusX,
        float radiusZ,
        int sampleCount,
        Plane screenPlane,
        Vector3 lightDirection,
        List<Vector2> points
    )
    {
        for (int i = 0; i < sampleCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / sampleCount;
            Vector3 localPoint = new Vector3(
                center.x + Mathf.Cos(angle) * radiusX,
                localY,
                center.z + Mathf.Sin(angle) * radiusZ
            );

            Vector3 worldPoint = shadowCasterRenderer.transform.TransformPoint(localPoint);
            AddProjectedPoint(worldPoint, screenPlane, lightDirection, points);
            lastCurvedSamplePointCount++;
        }
    }

    /// <summary>
    /// Purpose: Builds the outside outline of the shadow.
    /// Input: All 2D shadow points.
    /// Output: Ordered points around the edge of the shadow shape.
    /// </summary>
    private List<Vector2> BuildConvexHull(List<Vector2> points)
    {
        List<Vector2> sorted = new List<Vector2>(points);
        sorted.Sort((a, b) =>
        {
            int xCompare = a.x.CompareTo(b.x);
            return xCompare != 0 ? xCompare : a.y.CompareTo(b.y);
        });

        List<Vector2> uniquePoints = RemoveDuplicatePoints(sorted);
        sorted = uniquePoints;

        List<Vector2> hull = new List<Vector2>();

        foreach (Vector2 point in sorted)
        {
            while (hull.Count >= 2 &&
                   Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(point);
        }

        int lowerCount = hull.Count;

        for (int i = sorted.Count - 2; i >= 0; i--)
        {
            Vector2 point = sorted[i];

            while (hull.Count > lowerCount &&
                   Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(point);
        }

        if (hull.Count > 1)
        {
            hull.RemoveAt(hull.Count - 1);
        }

        return hull;
    }

    /// <summary>
    /// Purpose: Removes duplicate points before building the outline.
    /// Input: Sorted shadow points.
    /// Output: A cleaner point list with repeats removed.
    /// </summary>
    private List<Vector2> RemoveDuplicatePoints(List<Vector2> sortedPoints)
    {
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

        return uniquePoints;
    }

    /// <summary>
    /// Purpose: Checks which way three points turn.
    /// Input: Three 2D points.
    /// Output: A number used to build the outside outline correctly.
    /// </summary>
    private float Cross(Vector2 origin, Vector2 a, Vector2 b)
    {
        return (a.x - origin.x) * (b.y - origin.y) -
               (a.y - origin.y) * (b.x - origin.x);
    }

    /// <summary>
    /// Purpose: Turns the shadow outline into separate edge colliders.
    /// Input: Ordered shadow outline points.
    /// Output: Collider strips are created, and each one is marked walkable or steep.
    /// </summary>
    private int BuildEdgesFromHull(List<Vector2> hull, int generatedIndex)
    {
        for (int i = 0; i < hull.Count; i++)
        {
            Vector2 localStart = hull[i];
            Vector2 localEnd = hull[(i + 1) % hull.Count];

            if ((localEnd - localStart).magnitude < minEdgeLength)
            {
                continue;
            }

            if (!TryBuildWorldEdgeGeometry(
                    localStart,
                    localEnd,
                    out Vector3 worldStart,
                    out Vector3 worldEnd,
                    out Vector3 worldNormal,
                    out float worldLength))
            {
                continue;
            }

            bool isWalkable = Vector3.Dot(worldNormal, Vector3.up) >= minWalkableNormalY;
            bool isUpperEnvelopeEdge = IsUpperEnvelopeEdge(hull, localStart, localEnd);

            if (isWalkable && walkableEdgesMustBeUpperEnvelope && !isUpperEnvelopeEdge)
            {
                isWalkable = false;
            }

            bool useCurvedSupportOverrides =
                currentBuildUsesCurvedSamples &&
                addCurvedTopSupport &&
                isWalkable &&
                isUpperEnvelopeEdge;

            if (!buildAllEdgesForDebug && !isWalkable)
            {
                continue;
            }

            if (useCurvedSupportOverrides)
            {
                CreateOrUpdateEdge(
                    generatedIndex,
                    i,
                    worldStart,
                    worldEnd,
                    worldNormal,
                    worldLength,
                    isWalkable,
                    "GeneratedCurvedWalkableEdge",
                    curvedTopSupportThickness,
                    curvedTopSupportPadding
                );

                lastCurvedTopSupportEdgeCount++;
            }
            else
            {
                CreateOrUpdateEdge(
                    generatedIndex,
                    i,
                    worldStart,
                    worldEnd,
                    worldNormal,
                    worldLength,
                    isWalkable
                );
            }

            generatedIndex++;

            if (isWalkable)
            {
                lastWalkableEdgeCount++;
            }
            else
            {
                lastSteepEdgeCount++;
            }
        }

        return generatedIndex;
    }

    /// <summary>
    /// Purpose: Checks if one edge is on the top of the shadow.
    /// Input: The whole shadow outline and one edge.
    /// Output: True means this edge can be used as extra top support.
    /// </summary>
    private bool IsUpperEnvelopeEdge(List<Vector2> hull, Vector2 localStart, Vector2 localEnd)
    {
        if (Mathf.Abs(localEnd.x - localStart.x) < minEdgeLength)
        {
            return false;
        }

        Vector2 midpoint = (localStart + localEnd) * 0.5f;

        if (!TryGetTopYAtX(hull, midpoint.x, out float topYAtMidpoint))
        {
            return false;
        }

        float tolerance = Mathf.Max(0.001f, curvedTopEnvelopeTolerance);

        return Mathf.Abs(midpoint.y - topYAtMidpoint) <= tolerance;
    }

    /// <summary>
    /// Purpose: Finds the top of the shadow at one horizontal position.
    /// Input: Shadow outline and an X position on the screen.
    /// Output: The highest Y position of the shadow there.
    /// </summary>
    private bool TryGetTopYAtX(List<Vector2> hull, float x, out float topY)
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

    /// <summary>
    /// Purpose: Converts one screen edge into real Unity scene positions.
    /// Input: Start and end points on the shadow screen.
    /// Output: Real start/end positions, edge direction, and edge length.
    /// </summary>
    private bool TryBuildWorldEdgeGeometry(
        Vector2 localStart,
        Vector2 localEnd,
        out Vector3 worldStart,
        out Vector3 worldEnd,
        out Vector3 worldNormal,
        out float worldLength
    )
    {
        worldStart = shadowScreen.TransformPoint(
            new Vector3(localStart.x, localStart.y, surfaceOffset)
        );

        worldEnd = shadowScreen.TransformPoint(
            new Vector3(localEnd.x, localEnd.y, surfaceOffset)
        );

        Vector3 worldTangent = worldEnd - worldStart;
        worldLength = worldTangent.magnitude;

        if (worldLength < 0.0001f)
        {
            worldNormal = Vector3.up;
            return false;
        }

        worldTangent.Normalize();

        Vector3 worldDepth = shadowScreen.forward.normalized;
        worldNormal = Vector3.Cross(worldDepth, worldTangent).normalized;

        if (worldNormal.sqrMagnitude < 0.0001f)
        {
            worldNormal = Vector3.up;
        }

        if (flipWalkableNormal)
        {
            worldNormal = -worldNormal;
        }

        if (Vector3.Dot(worldNormal, Vector3.up) < 0f && useOppositeNormalFallback)
        {
            worldNormal = -worldNormal;
        }

        return true;
    }

    /// <summary>
    /// Purpose: Creates or updates one normal shadow edge collider.
    /// Input: Edge index, start/end positions, edge direction, length, and walkable state.
    /// Output: One BoxCollider strip is placed on that shadow edge.
    /// </summary>
    private void CreateOrUpdateEdge(
        int generatedIndex,
        int edgeId,
        Vector3 worldStart,
        Vector3 worldEnd,
        Vector3 worldNormal,
        float worldLength,
        bool isWalkable
    )
    {
        CreateOrUpdateEdge(
            generatedIndex,
            edgeId,
            worldStart,
            worldEnd,
            worldNormal,
            worldLength,
            isWalkable,
            "",
            -1f,
            -1f
        );
    }

    /// <summary>
    /// Purpose: Creates or updates one shadow edge collider with optional custom size.
    /// Input: Edge position, edge size, walkable state, and optional curved-edge settings.
    /// Output: One generated GameObject becomes a usable shadow collider.
    /// </summary>
    private void CreateOrUpdateEdge(
        int generatedIndex,
        int edgeId,
        Vector3 worldStart,
        Vector3 worldEnd,
        Vector3 worldNormal,
        float worldLength,
        bool isWalkable,
        string customNamePrefix,
        float customThickness,
        float customPadding
    )
    {
        GameObject edgeObject = GetOrCreateGeneratedEdge(generatedIndex);
        float effectiveThickness = customThickness > 0f
            ? customThickness
            : platformThickness;

        float effectivePadding = customPadding >= 0f
            ? customPadding
            : GetEffectiveEdgePadding();

        nextEdges.Add(new EdgeSnapshot(
            generatedIndex,
            edgeId,
            worldStart,
            worldEnd,
            isWalkable
        ));

        Vector3 worldDepth = shadowScreen.forward.normalized;
        Vector3 worldCenter =
            (worldStart + worldEnd) * 0.5f
            - worldNormal * (effectiveThickness * 0.5f);

        if (!string.IsNullOrEmpty(customNamePrefix))
        {
            edgeObject.name = $"{customNamePrefix}_{generatedIndex:00}";
        }
        else
        {
            edgeObject.name = isWalkable
                ? $"GeneratedWalkableEdge_{generatedIndex:00}"
                : $"GeneratedSteepEdge_{generatedIndex:00}";
        }

        edgeObject.transform.SetPositionAndRotation(
            worldCenter,
            Quaternion.LookRotation(worldDepth, worldNormal)
        );

        edgeObject.transform.localScale = new Vector3(
            worldLength + effectivePadding * 2f,
            effectiveThickness,
            platformDepth
        );

        int groundLayer = LayerMask.NameToLayer(groundLayerName);
        if (groundLayer >= 0)
        {
            edgeObject.layer = groundLayer;
        }

        BoxCollider boxCollider = edgeObject.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = edgeObject.AddComponent<BoxCollider>();
        }

        boxCollider.isTrigger = false;
        boxCollider.center = Vector3.zero;
        boxCollider.size = Vector3.one;
        boxCollider.sharedMaterial = isWalkable
            ? platformPhysicsMaterial
            : (steepEdgePhysicsMaterial != null ? steepEdgePhysicsMaterial : platformPhysicsMaterial);

        colliderGeneratedIndices[boxCollider] = generatedIndex;
        colliderWalkableStates[boxCollider] = isWalkable;

        if (addKinematicRigidbody)
        {
            Rigidbody body = edgeObject.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = edgeObject.AddComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;
        }

        MeshRenderer meshRenderer = edgeObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = showDebugSurface;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            if (debugMaterial != null)
            {
                meshRenderer.sharedMaterial = debugMaterial;
            }
        }

        edgeObject.SetActive(true);
    }

    /// <summary>
    /// Purpose: Chooses how much extra length to add to each edge.
    /// Input: Normal padding and whether this shadow came from a curved object.
    /// Output: A padding value that helps avoid tiny gaps.
    /// </summary>
    private float GetEffectiveEdgePadding()
    {
        if (lastCurvedSamplePointCount <= 0)
        {
            return edgePadding;
        }

        return edgePadding + curvedEdgeExtraPadding;
    }

    /// <summary>
    /// Purpose: Fixes curved-shadow settings if older scene data left them empty.
    /// Input: Current curved-shadow settings.
    /// Output: Safe values are filled in automatically.
    /// </summary>
    private void NormalizeCurvedCasterDefaults()
    {
        bool curvedSamplingLookedUninitialized =
            !addCurvedCasterSamples &&
            curvedCasterSampleCount == 0 &&
            Mathf.Approximately(curvedCasterRadiusTolerance, 0f) &&
            Mathf.Approximately(curvedEdgeExtraPadding, 0f);

        bool curvedTopSupportLookedUninitialized =
            !addCurvedTopSupport &&
            Mathf.Approximately(curvedTopSupportThickness, 0f) &&
            Mathf.Approximately(curvedTopSupportPadding, 0f) &&
            Mathf.Approximately(curvedTopEnvelopeTolerance, 0f) &&
            Mathf.Approximately(curvedTopSupportMinSourceNormalY, 0f);

        if (curvedSamplingLookedUninitialized)
        {
            addCurvedCasterSamples = true;
        }

        if (curvedTopSupportLookedUninitialized)
        {
            addCurvedTopSupport = true;
        }

        if (curvedCasterSampleCount < 8)
        {
            curvedCasterSampleCount = 48;
        }

        if (curvedCasterRadiusTolerance <= 0f)
        {
            curvedCasterRadiusTolerance = 0.18f;
        }

        if (curvedEdgeExtraPadding <= 0f)
        {
            curvedEdgeExtraPadding = 0.02f;
        }

        if (curvedTopSupportThickness <= 0f)
        {
            curvedTopSupportThickness = 0.04f;
        }

        if (curvedTopSupportPadding <= 0f)
        {
            curvedTopSupportPadding = 0.035f;
        }

        if (curvedTopEnvelopeTolerance <= 0f)
        {
            curvedTopEnvelopeTolerance = 0.06f;
        }

        if (curvedTopSupportMinSourceNormalY <= 0f)
        {
            curvedTopSupportMinSourceNormalY = 0.25f;
        }
    }

    /// <summary>
    /// Purpose: Gets the parent object that stores generated shadow edge objects.
    /// Input: Existing generated edge root, if it already exists.
    /// Output: A Transform used as the parent for generated edge colliders.
    /// </summary>
    private Transform GetOrCreateGeneratedEdgeRoot()
    {
        if (generatedEdgeRoot != null)
        {
            return generatedEdgeRoot;
        }

        GameObject root = new GameObject($"{name}_GeneratedShadowEdges");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;
        generatedEdgeRoot = root.transform;
        return generatedEdgeRoot;
    }

    /// <summary>
    /// Purpose: Gets one reusable generated edge object.
    /// Input: The edge object number needed.
    /// Output: A cube object that can be resized into a shadow collider.
    /// </summary>
    private GameObject GetOrCreateGeneratedEdge(int index)
    {
        while (generatedEdges.Count <= index)
        {
            GameObject edgeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edgeObject.name = $"GeneratedEdge_{generatedEdges.Count:00}";
            edgeObject.transform.SetParent(GetOrCreateGeneratedEdgeRoot(), true);
            generatedEdges.Add(edgeObject);
        }

        return generatedEdges[index];
    }

    /// <summary>
    /// Purpose: Hides old shadow edge objects that are not needed now.
    /// Input: Number of edges used by the current shadow.
    /// Output: Extra generated objects are turned off.
    /// </summary>
    private void HideUnusedEdges(int activeCount)
    {
        for (int i = activeCount; i < generatedEdges.Count; i++)
        {
            if (generatedEdges[i] != null)
            {
                generatedEdges[i].SetActive(false);
            }
        }
    }

    /// <summary>
    /// Purpose: Moves the player along with a moving shadow platform.
    /// Input: Player's current ground and old/new shadow edge positions.
    /// Output: The player is shifted by the same movement as the shadow edge.
    /// </summary>
    private void CarryPlayerWithShadowEdges()
    {
        CachePlayer();

        if (playerRigidbody == null || playerCollider == null || playerController == null)
        {
            return;
        }

        if (!playerController.enabled)
        {
            return;
        }

        if (previousEdges.Count == 0 || nextEdges.Count == 0)
        {
            return;
        }

        if (playerRigidbody.velocity.y > maxPassengerUpwardSpeed)
        {
            return;
        }

        if (!TryGetCurrentGroundGeneratedIndex(out int currentGeneratedIndex))
        {
            return;
        }

        Vector3 footWorld = new Vector3(
            playerCollider.bounds.center.x,
            playerCollider.bounds.min.y,
            playerCollider.bounds.center.z
        );

        if (!TryFindSupportingPreviousEdge(
                footWorld,
                currentGeneratedIndex,
                out EdgeSnapshot previousEdge,
                out float edgeAmount))
        {
            return;
        }

        Vector3 previousAnchor = Vector3.Lerp(
            previousEdge.worldStart,
            previousEdge.worldEnd,
            edgeAmount
        );

        if (!TryFindMatchingNextEdge(
                previousEdge,
                previousAnchor,
                edgeAmount,
                out EdgeSnapshot nextEdge,
                out float nextEdgeAmount))
        {
            return;
        }

        Vector3 nextAnchor = Vector3.Lerp(
            nextEdge.worldStart,
            nextEdge.worldEnd,
            nextEdgeAmount
        );

        Vector3 delta = nextAnchor - previousAnchor;

        if (delta.sqrMagnitude < 0.00000001f)
        {
            return;
        }

        playerController.ApplyShadowCarry(delta);
    }

    /// <summary>
    /// Purpose: Finds the player and remembers the useful player components.
    /// Input: The player tag.
    /// Output: Rigidbody, Collider, and PlayerController references are cached.
    /// </summary>
    private void CachePlayer()
    {
        if (playerRigidbody != null && playerCollider != null && playerController != null)
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
        playerController = playerObject.GetComponent<PlayerController>();
    }

    /// <summary>
    /// Purpose: Checks if the player is standing on one of this script's walkable shadow edges.
    /// Input: Player grounded state and current ground collider.
    /// Output: The edge number if the player is on a valid shadow edge.
    /// </summary>
    private bool TryGetCurrentGroundGeneratedIndex(out int generatedIndex)
    {
        generatedIndex = -1;

        if (playerController == null || !playerController.IsGrounded)
        {
            return false;
        }

        Collider groundCollider = playerController.CurrentGroundCollider;

        if (groundCollider == null)
        {
            return false;
        }

        if (!colliderGeneratedIndices.TryGetValue(groundCollider, out generatedIndex))
        {
            return false;
        }

        if (!colliderWalkableStates.TryGetValue(groundCollider, out bool isWalkable) || !isWalkable)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Purpose: Finds the old shadow edge that supported the player before the rebuild.
    /// Input: Player foot position and the previous edge list.
    /// Output: The old support edge and where the player was on it.
    /// </summary>
    private bool TryFindSupportingPreviousEdge(
        Vector3 footWorld,
        int requiredGeneratedIndex,
        out EdgeSnapshot supportEdge,
        out float edgeAmount
    )
    {
        supportEdge = new EdgeSnapshot();
        edgeAmount = 0f;

        Vector2 footPoint = GetScreenWorld2D(footWorld);
        float bestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < previousEdges.Count; i++)
        {
            EdgeSnapshot edge = previousEdges[i];

            if (!edge.isWalkable)
            {
                continue;
            }

            if (edge.generatedIndex != requiredGeneratedIndex)
            {
                continue;
            }

            Vector2 start = GetScreenWorld2D(edge.worldStart);
            Vector2 end = GetScreenWorld2D(edge.worldEnd);
            Vector2 segment = end - start;

            if (segment.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            float amount = Vector2.Dot(footPoint - start, segment) / segment.sqrMagnitude;
            amount = Mathf.Clamp01(amount);

            Vector2 closest = Vector2.Lerp(start, end, amount);
            float distance = Vector2.Distance(footPoint, closest);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                supportEdge = edge;
                edgeAmount = amount;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// Purpose: Finds the new edge that matches the old edge the player stood on.
    /// Input: Old edge, old player position on that edge, and current edge list.
    /// Output: Matching new edge if it is close and points the same way.
    /// </summary>
    private bool TryFindMatchingNextEdge(
        EdgeSnapshot previousEdge,
        Vector3 previousAnchor,
        float previousAmount,
        out EdgeSnapshot matchingEdge,
        out float matchingAmount
    )
    {
        matchingEdge = new EdgeSnapshot();
        matchingAmount = Mathf.Clamp01(previousAmount);

        Vector3 previousDirection = GetEdgeDirection(previousEdge);
        Vector3 previousMidpoint = GetEdgeMidpoint(previousEdge);

        float bestScore = float.MaxValue;
        bool found = false;

        for (int i = 0; i < nextEdges.Count; i++)
        {
            EdgeSnapshot candidate = nextEdges[i];

            if (!candidate.isWalkable)
            {
                continue;
            }

            if (candidate.generatedIndex != previousEdge.generatedIndex)
            {
                continue;
            }

            Vector3 candidateDirection = GetEdgeDirection(candidate);

            float directionDot = Mathf.Abs(
                Vector3.Dot(previousDirection, candidateDirection)
            );

            float requiredDirectionDot = Mathf.Max(
                minEdgeDirectionDot,
                Mathf.Max(0.75f, minCarryEdgeDirectionDot)
            );

            if (directionDot < requiredDirectionDot)
            {
                continue;
            }

            float preservedAmount = Mathf.Clamp01(previousAmount);

            Vector3 candidateAnchor = Vector3.Lerp(
                candidate.worldStart,
                candidate.worldEnd,
                preservedAmount
            );

            float anchorDistance = Vector3.Distance(
                previousAnchor,
                candidateAnchor
            );

            if (anchorDistance > maxEdgeMatchDistance)
            {
                continue;
            }

            float midpointDistance = Vector3.Distance(
                previousMidpoint,
                GetEdgeMidpoint(candidate)
            );

            float edgeIdBonus = candidate.edgeId == previousEdge.edgeId ? -0.1f : 0f;

            float score =
                anchorDistance
                + midpointDistance * 0.15f
                + (1f - directionDot) * 0.25f
                + edgeIdBonus;

            if (score < bestScore)
            {
                bestScore = score;
                matchingEdge = candidate;
                matchingAmount = preservedAmount;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// Purpose: Finds the middle point of one edge.
    /// Input: Edge start and end positions.
    /// Output: The middle position.
    /// </summary>
    private Vector3 GetEdgeMidpoint(EdgeSnapshot edge)
    {
        return (edge.worldStart + edge.worldEnd) * 0.5f;
    }

    /// <summary>
    /// Purpose: Finds which direction one edge points.
    /// Input: Edge start and end positions.
    /// Output: A direction from start to end.
    /// </summary>
    private Vector3 GetEdgeDirection(EdgeSnapshot edge)
    {
        Vector3 segment = edge.worldEnd - edge.worldStart;

        if (segment.sqrMagnitude < 0.0001f)
        {
            return Vector3.right;
        }

        return segment.normalized;
    }

    /// <summary>
    /// Purpose: Converts a real Unity position into a 2D screen position.
    /// Input: A world position.
    /// Output: X/Y coordinates on the shadow screen.
    /// </summary>
    private Vector2 GetScreenWorld2D(Vector3 worldPoint)
    {
        Vector3 offset = worldPoint - shadowScreen.position;

        return new Vector2(
            Vector3.Dot(offset, shadowScreen.right),
            Vector3.Dot(offset, shadowScreen.up)
        );
    }

    /// <summary>
    /// Purpose: Saves the current shadow edges for the next frame.
    /// Input: The edge list just built.
    /// Output: Next frame can compare old and new edge positions.
    /// </summary>
    private void StoreCurrentEdges()
    {
        previousEdges.Clear();
        previousEdges.AddRange(nextEdges);
    }
}

