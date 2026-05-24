using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Purpose: Generates walkable BoxCollider strips from the projected shadow outline of one 3D caster.
/// Input: A shadow caster MeshRenderer, the shadow screen Transform, and the Directional Light.
/// Output: Runtime child objects named GeneratedWalkableEdge_## that can be used as Ground colliders.
/// </summary>
[ExecuteAlways]
public class ProjectedShadowWalkableEdges : MonoBehaviour
{
    [Header("Scene References")]
    public MeshRenderer shadowCasterRenderer;
    public Transform shadowScreen;
    public Light directionalLight;

    [Header("Walkable Edge Shape")]
    public float platformThickness = 0.14f;
    public float platformDepth = 8f;
    public float edgePadding = 0.04f;
    public float surfaceOffset = -0.02f;
    public float minEdgeLength = 0.001f;
    [Range(0f, 1f)]
    public float minWalkableNormalY = 0.35f;

    [Header("Edge Selection Debug")]
    public bool buildAllEdgesForDebug = true;
    public bool flipWalkableNormal = false;
    public bool useOppositeNormalFallback = true;

    [Header("Update")]
    public bool rebuildEveryFrame = true;

    [Header("Generated Collider Settings")]
    public string groundLayerName = "Ground";
    public bool addKinematicRigidbody = true;

    [Header("Debug Visual")]
    public bool showDebugSurface = true;
    public Material debugMaterial;

    [Header("Read Only Diagnostics")]
    [SerializeField] private int lastProjectedPointCount;
    [SerializeField] private int lastHullPointCount;
    [SerializeField] private int lastGeneratedEdgeCount;
    [SerializeField] private string lastBuildMessage;

    private readonly List<GameObject> generatedEdges = new List<GameObject>();

    /// <summary>
    /// Purpose: Builds the generated shadow edge colliders when the object starts.
    /// Input: Current scene references and public tuning values.
    /// Output: Generated edge colliders are created or refreshed.
    /// </summary>
    private void Start()
    {
        Rebuild();
    }

    /// <summary>
    /// Purpose: Keeps generated walkable edges aligned with moving light/shadow changes.
    /// Input: Current light direction and caster transform.
    /// Output: Generated colliders follow the projected shadow outline.
    /// </summary>
    private void LateUpdate()
    {
        if (rebuildEveryFrame)
        {
            Rebuild();
        }
    }

    /// <summary>
    /// Purpose: Recalculates the projected shadow outline and updates generated edge strips.
    /// Input: Mesh vertices from the caster, the shadow screen plane, and the light direction.
    /// Output: Runtime child objects aligned to the projected shadow edges.
    /// </summary>
    public void Rebuild()
    {
        lastProjectedPointCount = 0;
        lastHullPointCount = 0;
        lastGeneratedEdgeCount = 0;
        lastBuildMessage = "";

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

        BuildEdgesFromHull(hull);
        HideUnusedEdges(lastGeneratedEdgeCount);

        if (lastGeneratedEdgeCount == 0)
        {
            lastBuildMessage = "Projected hull exists, but no edge passed the filter.";
        }
        else if (buildAllEdgesForDebug)
        {
            lastBuildMessage = "Debug mode generated all usable hull edges.";
        }
        else
        {
            lastBuildMessage = "Generated filtered walkable edges.";
        }
    }

    /// <summary>
    /// Purpose: Validates that all required scene references exist.
    /// Input: Inspector-assigned references.
    /// Output: True if projection can be calculated.
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
    /// Purpose: Projects every mesh vertex from the caster onto the shadow screen plane.
    /// Input: Caster mesh vertices, caster transform, light direction, and screen plane.
    /// Output: 2D points in the shadow screen's local XY coordinate space.
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
    /// Purpose: Builds a convex hull from projected 2D points.
    /// Input: Projected points in shadow screen local XY space.
    /// Output: Ordered hull points around the projected shadow silhouette.
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
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(point);
        }

        int lowerCount = hull.Count;

        for (int i = sorted.Count - 2; i >= 0; i--)
        {
            Vector2 point = sorted[i];

            while (hull.Count > lowerCount && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
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
    /// Purpose: Calculates the 2D cross product used by convex hull construction.
    /// Input: Three 2D points.
    /// Output: Positive, negative, or zero turn direction value.
    /// </summary>
    private float Cross(Vector2 origin, Vector2 a, Vector2 b)
    {
        return (a.x - origin.x) * (b.y - origin.y) - (a.y - origin.y) * (b.x - origin.x);
    }

    /// <summary>
    /// Purpose: Converts hull edges into thin walkable BoxCollider strips.
    /// Input: Ordered hull points in shadow screen local XY space.
    /// Output: Generated child colliders placed in world space on the shadow screen.
    /// </summary>
    private void BuildEdgesFromHull(List<Vector2> hull)
    {
        int edgeIndex = 0;

        for (int i = 0; i < hull.Count; i++)
        {
            Vector2 start = hull[i];
            Vector2 end = hull[(i + 1) % hull.Count];

            Vector2 edge = end - start;
            float edgeLength = edge.magnitude;

            if (edgeLength < minEdgeLength)
            {
                continue;
            }

            Vector2 edgeDirection = edge.normalized;
            Vector2 normalA = new Vector2(-edgeDirection.y, edgeDirection.x);
            Vector2 normalB = -normalA;

            Vector2 walkableNormal = normalA.y >= normalB.y ? normalA : normalB;

            if (flipWalkableNormal)
            {
                walkableNormal = -walkableNormal;
            }

            if (!buildAllEdgesForDebug && walkableNormal.y < minWalkableNormalY)
            {
                if (useOppositeNormalFallback && (-walkableNormal).y >= minWalkableNormalY)
                {
                    walkableNormal = -walkableNormal;
                }
                else
                {
                    continue;
                }
            }

            Vector2 center = (start + end) * 0.5f;
            CreateOrUpdateEdge(edgeIndex, center, edgeDirection, walkableNormal, edgeLength);
            edgeIndex++;
        }

        lastGeneratedEdgeCount = edgeIndex;
    }

    /// <summary>
    /// Purpose: Creates or updates one generated edge collider in the correct world-space position.
    /// Input: Edge center, tangent, normal, and length in shadow screen local space.
    /// Output: A child GameObject with a BoxCollider aligned to the projected shadow edge.
    /// </summary>
    private void CreateOrUpdateEdge(int index, Vector2 localCenter, Vector2 localTangent, Vector2 localNormal, float edgeLength)
    {
        GameObject edgeObject = GetOrCreateGeneratedEdge(index);

        Vector3 screenLocalCenter = new Vector3(localCenter.x, localCenter.y, surfaceOffset);
        Vector3 worldCenter = shadowScreen.TransformPoint(screenLocalCenter);

        Vector3 worldNormal = shadowScreen.TransformDirection(new Vector3(localNormal.x, localNormal.y, 0f)).normalized;
        Vector3 worldDepth = shadowScreen.TransformDirection(Vector3.forward).normalized;

        edgeObject.transform.SetPositionAndRotation(
            worldCenter,
            Quaternion.LookRotation(worldDepth, worldNormal)
        );

        edgeObject.transform.localScale = new Vector3(
            edgeLength + edgePadding * 2f,
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

            if (debugMaterial != null)
            {
                meshRenderer.sharedMaterial = debugMaterial;
            }
        }

        edgeObject.SetActive(true);
    }

    /// <summary>
    /// Purpose: Gets an existing generated edge object or creates a new cube-based collider.
    /// Input: Generated edge index.
    /// Output: A reusable generated edge GameObject.
    /// </summary>
    private GameObject GetOrCreateGeneratedEdge(int index)
    {
        while (generatedEdges.Count <= index)
        {
            GameObject edgeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edgeObject.name = $"GeneratedWalkableEdge_{generatedEdges.Count:00}";
            edgeObject.transform.SetParent(transform, true);
            generatedEdges.Add(edgeObject);
        }

        return generatedEdges[index];
    }

    /// <summary>
    /// Purpose: Hides unused generated edge objects when the current hull has fewer valid edges.
    /// Input: Number of generated edge objects that should remain active.
    /// Output: Extra generated objects are deactivated.
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
}