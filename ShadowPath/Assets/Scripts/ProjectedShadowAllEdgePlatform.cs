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

    [Header("Edge Selection Debug")]
    public bool buildAllEdgesForDebug = true;
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
    public float maxCarryDistancePerStep = 0.25f;
    public float maxEdgeMatchDistance = 1.2f;

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
    [SerializeField] private string lastBuildMessage;

    private readonly List<GameObject> generatedEdges = new List<GameObject>();
    private readonly List<EdgeSnapshot> previousEdges = new List<EdgeSnapshot>();
    private readonly List<EdgeSnapshot> nextEdges = new List<EdgeSnapshot>();

    private readonly Dictionary<Collider, int> colliderGeneratedIndices = new Dictionary<Collider, int>();
    private readonly Dictionary<Collider, bool> colliderWalkableStates = new Dictionary<Collider, bool>();

    private Rigidbody playerRigidbody;
    private Collider playerCollider;
    private PlayerController playerController;

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
        lastBuildMessage = "";

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
        BuildEdgesFromHull(hull);

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
    /// Purpose: Projects each caster mesh vertex onto the shadow screen plane.
    /// Input: Caster mesh vertices, caster transform, directional light direction, and shadow screen plane.
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
            Ray projectionRay = new Ray(worldVertex, lightDirection);

            if (screenPlane.Raycast(projectionRay, out float distance))
            {
                Vector3 worldHit = projectionRay.GetPoint(distance);
                Vector3 screenLocalHit = shadowScreen.InverseTransformPoint(worldHit);
                points.Add(new Vector2(screenLocalHit.x, screenLocalHit.y));
            }
        }

        return points;
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
    /// Input: Ordered hull points in shadow screen local XY space.
    /// Output: Generated edge colliders and edge snapshots for the current physics step.
    /// </summary>
    private void BuildEdgesFromHull(List<Vector2> hull)
    {
        int generatedIndex = 0;

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

            if (!buildAllEdgesForDebug && !isWalkable)
            {
                continue;
            }

            CreateOrUpdateEdge(
                generatedIndex,
                i,
                worldStart,
                worldEnd,
                worldNormal,
                worldLength,
                isWalkable
            );

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

        lastGeneratedEdgeCount = generatedIndex;
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
        GameObject edgeObject = GetOrCreateGeneratedEdge(generatedIndex);

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
            - worldNormal * (platformThickness * 0.5f);

        edgeObject.name = isWalkable
            ? $"GeneratedWalkableEdge_{generatedIndex:00}"
            : $"GeneratedSteepEdge_{generatedIndex:00}";

        edgeObject.transform.SetPositionAndRotation(
            worldCenter,
            Quaternion.LookRotation(worldDepth, worldNormal)
        );

        edgeObject.transform.localScale = new Vector3(
            worldLength + edgePadding * 2f,
            platformThickness,
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
            edgeObject.transform.SetParent(transform, true);
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

            if (directionDot < minEdgeDirectionDot)
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
