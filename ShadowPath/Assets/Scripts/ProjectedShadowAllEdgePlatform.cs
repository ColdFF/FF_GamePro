using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Purpose: Generates collider strips from projected shadow edges and separates walkable edges from steep sliding edges.
/// Input: A shadow caster MeshRenderer, a shadow screen Transform, a Directional Light, and tuning values.
/// Output: Runtime BoxCollider edge strips that follow shadow edges; walkable edges can carry the player, steep edges only slide/block.
/// </summary>
[DefaultExecutionOrder(-50)]
public class ProjectedShadowAllEdgePlatform : MonoBehaviour
{
    [Header("Scene References")]
    public MeshRenderer shadowCasterRenderer;
    public Transform shadowScreen;
    public Light directionalLight;

    [Header("Walkable Edge Shape")]
    public float platformThickness = 0.08f;
    public float platformDepth = 8f;
    public float edgePadding = 0.04f;
    public float surfaceOffset = -0.02f;
    public float minEdgeLength = 0.001f;

    [Range(0f, 1f)]
    public float minWalkableNormalY = 0.65f;

    [Header("Curved Caster Sampling")]
    public bool addCurvedCasterSamples = true;

    [Min(8)]
    public int curvedCasterSampleCount = 48;

    [Range(0f, 1f)]
    public float curvedCasterRadiusTolerance = 0.18f;

    [Min(0f)]
    public float curvedEdgeExtraPadding = 0.02f;

    public bool addCurvedTopSupport = true;

    [Min(0.01f)]
    public float curvedTopSupportThickness = 0.04f;

    [Min(0f)]
    public float curvedTopSupportPadding = 0.035f;

    [Min(0f)]
    public float curvedTopEnvelopeTolerance = 0.06f;

    [Range(0f, 1f)]
    public float curvedTopSupportMinSourceNormalY = 0.25f;

    [Header("Edge Selection Debug")]
    public bool buildAllEdgesForDebug = true;
    public bool walkableEdgesMustBeUpperEnvelope = false;
    public bool flipWalkableNormal = false;
    public bool useOppositeNormalFallback = true;

    [Header("Update")]
    public bool rebuildEveryFrame = true;

    [Header("Generated Collider Settings")]
    public string groundLayerName = "Ground";
    public bool addKinematicRigidbody = true;
    public PhysicMaterial platformPhysicsMaterial;
    public PhysicMaterial steepEdgePhysicsMaterial;

    [Header("Passenger Carry")]
    public bool carryPlayerWithShadow = true;
    public string playerTag = "Player";
    public float passengerContactTolerance = 0.35f;
    public float maxPassengerUpwardSpeed = 1f;
    public float minCarryDistancePerStep = 0.005f;
    public float maxCarryDistancePerStep = 0.25f;
    public float maxEdgeMatchDistance = 1.2f;
    [Range(0f, 1f)]
    public float minCarryEdgeDirectionDot = 0.75f;

    [Range(-1f, 1f)]
    public float minEdgeDirectionDot = 0f;

    [Header("Debug Visual")]
    public bool showDebugSurface = true;
    public Material debugMaterial;

    [Header("Read Only Diagnostics")]
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
        public int generatedIndex;
        public int edgeId;
        public Vector3 worldStart;
        public Vector3 worldEnd;
        public bool isWalkable;

        /// <summary>
        /// Purpose: Stores one projected shadow edge for matching between physics frames.
        /// Input: Generated object index, source hull edge id, world-space endpoints, and walkable state.
        /// Output: An immutable edge snapshot used by player carrying logic.
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
    /// Purpose: Repairs serialized defaults for curved-caster fields added after existing scene instances were created.
    /// Input: Current serialized values on this component.
    /// Output: Curved support starts enabled with safe values unless the scene has explicit non-default tuning.
    /// </summary>
    private void Awake()
    {
        NormalizeCurvedCasterDefaults();
    }

    /// <summary>
    /// Purpose: Repairs newly added curved-caster defaults while editing older scene instances.
    /// Input: Inspector value changes and script recompiles.
    /// Output: Existing Level02 instances pick up safe curved-shadow defaults without manual scene rewiring.
    /// </summary>
    private void OnValidate()
    {
        NormalizeCurvedCasterDefaults();
    }

    /// <summary>
    /// Purpose: Builds the first set of generated shadow edge colliders.
    /// Input: Current scene references and inspector tuning values.
    /// Output: Generated edge colliders are created before gameplay interaction begins.
    /// </summary>
    private void Start()
    {
        Rebuild();
    }

    /// <summary>
    /// Purpose: Keeps generated edge colliders aligned with changing shadows during physics updates.
    /// Input: Current light angle, caster transform, and previous edge snapshots.
    /// Output: Edge colliders update and the player is carried when standing on a matching walkable edge.
    /// </summary>
    private void FixedUpdate()
    {
        if (rebuildEveryFrame)
        {
            Rebuild();
        }
    }

    /// <summary>
    /// Purpose: Recalculates the projected shadow hull and refreshes generated edge colliders.
    /// Input: Shadow caster mesh vertices, light direction, and shadow screen plane.
    /// Output: Current edge colliders, diagnostics, and previous edge snapshots are updated.
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
    /// Purpose: Validates that projection can be calculated.
    /// Input: Shadow caster renderer, shadow screen, and directional light references.
    /// Output: True if all required references exist; otherwise false with a diagnostic message.
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
    /// Purpose: Projects each caster mesh vertex and optional curved-surface samples onto the shadow screen plane.
    /// Input: Caster mesh vertices, optional analytic round-caster samples, caster transform, directional light direction, and shadow screen plane.
    /// Output: Projected points in shadow screen local XY space.
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
    /// Purpose: Adds one projected world point to the current screen-local point list.
    /// Input: A world-space caster point, the shadow screen plane, and light direction.
    /// Output: Projected 2D screen-local point is appended when the projection is valid.
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
    /// Purpose: Adds extra projection samples for round casters whose true silhouette can pass between low-poly mesh vertices.
    /// Input: Caster mesh bounds, shadow screen plane, and light direction.
    /// Output: Additional circular-ring samples make cylinder shadows generate continuous walkable edge strips.
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
    /// Purpose: Detects whether the caster should receive extra circular silhouette samples.
    /// Input: Mesh name, vertex count, and local bounds.
    /// Output: True for cylinder-like meshes, false for ordinary box/platform meshes.
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
    /// Purpose: Samples one local-space circular ring on a cylinder-like caster.
    /// Input: Ring center, local Y height, radii, sample count, shadow screen plane, and light direction.
    /// Output: Projected ring points are appended to the projection point list.
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
    /// Purpose: Builds a convex hull around projected shadow points.
    /// Input: Projected screen-local 2D points.
    /// Output: Ordered convex hull points around the projected shadow silhouette.
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
    /// Purpose: Removes repeated projected points before hull generation.
    /// Input: Sorted projected points.
    /// Output: Unique points that keep hull construction stable for dense curved sampling.
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
    /// Purpose: Calculates signed 2D turn direction for hull construction.
    /// Input: Origin point, first point, and second point.
    /// Output: Positive, negative, or zero cross-product value.
    /// </summary>
    private float Cross(Vector2 origin, Vector2 a, Vector2 b)
    {
        return (a.x - origin.x) * (b.y - origin.y) -
               (a.y - origin.y) * (b.x - origin.x);
    }

    /// <summary>
    /// Purpose: Converts hull edges into collider strips and classifies each edge as walkable or steep.
    /// Input: Ordered hull points in shadow screen local XY space and the first generated edge index.
    /// Output: Next free generated edge index after all regular hull edge strips.
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
    /// Purpose: Checks whether a hull edge lies on the upper visible silhouette rather than on the side or bottom.
    /// Input: Full hull and one candidate local edge.
    /// Output: True when the edge should receive curved walkable support.
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
    /// Purpose: Finds the highest point where a vertical screen-space line intersects the hull.
    /// Input: Hull outline and a screen-local X coordinate.
    /// Output: Top Y coordinate at that X if the hull crosses the line.
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
    /// Purpose: Converts one local hull edge into world-space edge data.
    /// Input: Local start and end points on the shadow screen.
    /// Output: World endpoints, upward-facing edge normal, and world edge length.
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
    /// Purpose: Creates or updates one generated edge collider object.
    /// Input: Generated object index, source edge id, world endpoints, world normal, world length, and walkable state.
    /// Output: A positioned BoxCollider strip with the correct material, layer, visual, and edge snapshot.
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
    /// Purpose: Creates or updates one generated edge collider object with optional curved-support overrides.
    /// Input: Generated object index, source edge id, world endpoints, world normal, world length, walkable state, optional name prefix, thickness, and padding.
    /// Output: A positioned BoxCollider strip with the correct material, layer, visual, and edge snapshot.
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
    /// Purpose: Gives densely sampled curved edges a little more overlap so tiny segment joints do not create foot gaps.
    /// Input: Base edge padding and whether curved samples were used for this rebuild.
    /// Output: Effective padding applied to generated edge colliders.
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
    /// Purpose: Normalizes curved-caster settings when older scene instances deserialize newly added fields as zero values.
    /// Input: Current curved-caster configuration.
    /// Output: Safe runtime defaults for curved sampling and top support.
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
    /// Purpose: Gets or creates a reusable generated edge GameObject.
    /// Input: Generated edge object index.
    /// Output: Child cube GameObject used as a visual and BoxCollider platform strip.
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
    /// Purpose: Hides generated edge objects that are not used by the current hull.
    /// Input: Number of generated edges that should remain active.
    /// Output: Extra generated edge objects are deactivated.
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
    /// Purpose: Carries the player only when they are currently grounded on this controller's walkable generated edge.
    /// Input: Player ground collider, previous edge snapshots, and current edge snapshots.
    /// Output: Player Rigidbody is moved by the matched edge delta, or no movement is applied.
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
        float minCarryDistance = Mathf.Max(0.005f, minCarryDistancePerStep);

        if (delta.sqrMagnitude < minCarryDistance * minCarryDistance)
        {
            return;
        }

        if (delta.magnitude > maxCarryDistancePerStep)
        {
            delta = delta.normalized * maxCarryDistancePerStep;
        }

        playerRigidbody.MovePosition(playerRigidbody.position + delta);
    }

    /// <summary>
    /// Purpose: Finds and caches player components by tag.
    /// Input: Configured playerTag.
    /// Output: Cached Rigidbody, Collider, and PlayerController references.
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
    /// Purpose: Checks whether the player is currently grounded on one of this controller's walkable generated colliders.
    /// Input: PlayerController grounded state and current ground collider.
    /// Output: Generated edge index if the current ground belongs to this controller and is walkable.
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
    /// Purpose: Finds the previous edge that corresponds to the player's current grounded generated edge.
    /// Input: Player foot world position, required generated index, and previous edge snapshots.
    /// Output: Supporting previous edge and normalized amount along it.
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

            if (distance <= passengerContactTolerance && distance < bestDistance)
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
    /// Purpose: Finds the current matching walkable edge for the previous support edge.
    /// Input: Previous edge, previous anchor point, previous normalized edge amount, and current edge snapshots.
    /// Output: Matching current edge and normalized amount if a safe match is found.
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
    /// Purpose: Gets the midpoint of an edge snapshot.
    /// Input: Edge snapshot with world endpoints.
    /// Output: World-space midpoint.
    /// </summary>
    private Vector3 GetEdgeMidpoint(EdgeSnapshot edge)
    {
        return (edge.worldStart + edge.worldEnd) * 0.5f;
    }

    /// <summary>
    /// Purpose: Gets the normalized direction of an edge snapshot.
    /// Input: Edge snapshot with world endpoints.
    /// Output: Normalized direction from start to end.
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
    /// Purpose: Converts a world point into 2D coordinates on the shadow screen axes.
    /// Input: A world-space point.
    /// Output: 2D coordinate using shadow screen right and up directions.
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
    /// Purpose: Stores current edge snapshots for use during the next physics update.
    /// Input: nextEdges from the current rebuild.
    /// Output: previousEdges is refreshed with the latest edge data.
    /// </summary>
    private void StoreCurrentEdges()
    {
        previousEdges.Clear();
        previousEdges.AddRange(nextEdges);
    }
}

