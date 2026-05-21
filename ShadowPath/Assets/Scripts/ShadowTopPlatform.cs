using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ShadowTopPlatform : MonoBehaviour
{
    [Header("Scene References")]
    public Renderer shadowCasterRenderer;
    public Transform shadowScreen;
    public Light directionalLight;

    [Header("Platform Shape")]
    public float platformThickness = 0.15f;
    public float platformDepth = 3f;
    public float widthPadding = 0.1f;
    public float verticalOffset = 0.03f;

    [Header("Update")]
    public bool updateEveryFrame = true;

    private BoxCollider platformCollider;

    // Purpose: Prepares the physical collider used by the projected shadow platform.
    // Input: The BoxCollider attached to this GameObject.
    // Output: Stores and configures the collider before gameplay starts.
    void Awake()
    {
        platformCollider = GetComponent<BoxCollider>();
        platformCollider.center = Vector3.zero;
        platformCollider.size = Vector3.one;
        platformCollider.isTrigger = false;
    }

    // Purpose: Places the platform correctly when the scene first starts.
    // Input: Current shadow caster, shadow screen, and light references.
    // Output: Updates the platform position and size once at startup.
    void Start()
    {
        UpdateShadowPlatform();
    }

    // Purpose: Keeps the physical platform aligned with the moving projected shadow.
    // Input: Current light rotation and shadow caster bounds.
    // Output: Recalculates the platform every frame when enabled.
    void LateUpdate()
    {
        if (updateEveryFrame)
        {
            UpdateShadowPlatform();
        }
    }

    // Purpose: Projects the caster bounds onto the shadow screen and places a thin platform at the top of the shadow.
    // Input: Shadow caster renderer bounds, shadow screen orientation, and directional light direction.
    // Output: Updates this GameObject transform and collider to match the projected shadow top.
    public void UpdateShadowPlatform()
    {
        if (shadowCasterRenderer == null || shadowScreen == null || directionalLight == null)
        {
            SetPlatformEnabled(false);
            return;
        }

        Vector3[] corners = GetBoundsCorners(shadowCasterRenderer.bounds);

        Vector3 screenOrigin = shadowScreen.position;
        Vector3 screenNormal = shadowScreen.forward.normalized;
        Vector3 screenRight = shadowScreen.right.normalized;
        Vector3 screenUp = shadowScreen.up.normalized;
        Vector3 lightDirection = directionalLight.transform.forward.normalized;

        float denominator = Vector3.Dot(lightDirection, screenNormal);

        if (Mathf.Abs(denominator) < 0.0001f)
        {
            SetPlatformEnabled(false);
            return;
        }

        float minShadowX = float.PositiveInfinity;
        float maxShadowX = float.NegativeInfinity;
        float maxShadowY = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 projectedPoint = ProjectPointOntoScreen(
                corners[i],
                screenOrigin,
                screenNormal,
                lightDirection,
                denominator
            );

            Vector3 fromScreenOrigin = projectedPoint - screenOrigin;

            float shadowX = Vector3.Dot(fromScreenOrigin, screenRight);
            float shadowY = Vector3.Dot(fromScreenOrigin, screenUp);

            minShadowX = Mathf.Min(minShadowX, shadowX);
            maxShadowX = Mathf.Max(maxShadowX, shadowX);
            maxShadowY = Mathf.Max(maxShadowY, shadowY);
        }

        float shadowWidth = maxShadowX - minShadowX;
        float platformWidth = Mathf.Max(0.1f, shadowWidth + widthPadding * 2f);
        float platformCenterX = (minShadowX + maxShadowX) * 0.5f;
        float platformTopY = maxShadowY + verticalOffset;
        float platformCenterY = platformTopY - platformThickness * 0.5f;

        Vector3 worldCenter =
            screenOrigin +
            screenRight * platformCenterX +
            screenUp * platformCenterY;

        transform.position = worldCenter;
        transform.rotation = Quaternion.LookRotation(screenNormal, screenUp);
        transform.localScale = new Vector3(platformWidth, platformThickness, platformDepth);

        SetPlatformEnabled(true);
    }

    // Purpose: Projects one world point along the light direction until it reaches the shadow screen plane.
    // Input: World point, screen plane data, and directional light direction.
    // Output: A world-space point on the shadow screen.
    Vector3 ProjectPointOntoScreen(
        Vector3 worldPoint,
        Vector3 screenOrigin,
        Vector3 screenNormal,
        Vector3 lightDirection,
        float denominator
    )
    {
        float distanceToScreen =
            Vector3.Dot(screenOrigin - worldPoint, screenNormal) / denominator;

        return worldPoint + lightDirection * distanceToScreen;
    }

    // Purpose: Gets all eight corners of the renderer bounds for projection.
    // Input: World-space renderer bounds.
    // Output: Eight world-space corner positions.
    Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        return new Vector3[]
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z)
        };
    }

    // Purpose: Enables or disables the platform collider when the shadow projection is valid or invalid.
    // Input: Boolean state for platform availability.
    // Output: Updates the BoxCollider enabled state.
    void SetPlatformEnabled(bool isEnabled)
    {
        if (platformCollider != null)
        {
            platformCollider.enabled = isEnabled;
        }
    }
}