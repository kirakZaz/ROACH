using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public Vector3 offset = new Vector3(0, 2, -10);

    [Header("Camera Zoom")]
    public float cameraSize = 5f; // target zoom (set from other script)
    public float zoomSmoothSpeed = 5f; // how fast camera zooms to target

    [Header("Follow Settings")]
    public float smoothSpeed = 0.125f;
    public bool followX = true;
    public bool followY = true;

    [Header("Level Boundaries (manual)")]
    public bool useBoundaries = true;
    public float minX = -25f;
    public float maxX = 90f;
    public float minY = -15f;
    public float maxY = 15f;

    [Header("Background group (auto bounds)")]
    public bool useBackgroundGroupBounds = true;
    public Transform backgroundRoot; // parent that contains all background pieces

    [Header("Layer Filtering")]
    [Tooltip("Which layers to include when calculating bounds (check Default, Ground, etc.)")]
    public LayerMask includeLayers = -1; // -1 means "Everything" by default
    [Tooltip("Always ignore UI layer even if included above")]
    public bool alwaysIgnoreUI = true;

    [Header("Dead Zone")]
    public bool useDeadZone = false;
    public float deadZoneWidth = 2f;
    public float deadZoneHeight = 2f;

    [Header("Zoom Display")]
    public bool showZoomInfo = true;
    [Tooltip("Only show zoom info when game has started (not on menu)")]
    public bool onlyShowDuringGameplay = true;
    [Tooltip("Set this to true when PLAY button is clicked")]
    public bool gameStarted = false;
    public float currentZoom = 5f;

    private Camera cam;
    private float halfHeight;
    private float halfWidth;
    private bool hasAutoBounds = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam)
        {
            Debug.LogError("CameraFollow: Camera component is missing!");
            enabled = false;
            return;
        }

        cam.orthographicSize = cameraSize;
        currentZoom = cameraSize;
        UpdateCameraHalfSize();

        if (useBackgroundGroupBounds)
        {
            RecalculateBoundsFromBackgroundGroup();
        }
    }

    void UpdateCameraHalfSize()
    {
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;
    }

    void RecalculateBoundsFromBackgroundGroup()
    {
        if (backgroundRoot == null)
        {
            Debug.LogWarning(
                "CameraFollow: backgroundRoot is not assigned but useBackgroundGroupBounds is true."
            );
            hasAutoBounds = false;
            return;
        }

        Renderer[] renderers = backgroundRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("CameraFollow: no renderers found under backgroundRoot.");
            hasAutoBounds = false;
            return;
        }

        bool first = true;
        Bounds total = new Bounds();
        foreach (Renderer r in renderers)
        {
            // Check if this renderer's layer should be included
            if (!ShouldIncludeLayer(r.gameObject.layer))
                continue;

            if (first)
            {
                total = r.bounds;
                first = false;
            }
            else
            {
                total.Encapsulate(r.bounds);
            }
        }

        minX = total.min.x;
        maxX = total.max.x;
        minY = total.min.y;
        maxY = total.max.y;
        hasAutoBounds = true;
    }

    // Helper method to check if a layer should be included
    private bool ShouldIncludeLayer(int layer)
    {
        // Always ignore UI if that option is enabled
        if (alwaysIgnoreUI && layer == LayerMask.NameToLayer("UI"))
            return false;

        // Check if the layer is in our include mask
        return ((1 << layer) & includeLayers) != 0;
    }

    void LateUpdate()
    {
        if (!player)
            return;

        // 1. smooth zoom to target cameraSize
        if (Mathf.Abs(cam.orthographicSize - cameraSize) > 0.001f)
        {
            cam.orthographicSize = Mathf.Lerp(
                cam.orthographicSize,
                cameraSize,
                Time.deltaTime * zoomSmoothSpeed
            );
            currentZoom = cam.orthographicSize;
            UpdateCameraHalfSize();

            // if bounds depend on camera/zoom, we can refresh auto bounds
            if (useBackgroundGroupBounds && backgroundRoot != null)
            {
                // we only need real bounds once, but recalculating is safe
                RecalculateBoundsFromBackgroundGroup();
            }
        }
        else
        {
            // keep in sync
            currentZoom = cam.orthographicSize;
            UpdateCameraHalfSize();
        }

        // 2. build desired camera position (follow player)
        Vector3 targetPosition = transform.position;

        if (followX)
            targetPosition.x = player.position.x + offset.x;
        if (followY)
            targetPosition.y = player.position.y + offset.y;

        targetPosition.z = offset.z;

        // 3. clamp to level / background bounds with current camera half size
        if (useBoundaries)
        {
            float clampMinX = minX + halfWidth;
            float clampMaxX = maxX - halfWidth;
            float clampMinY = minY + halfHeight;
            float clampMaxY = maxY - halfHeight;

            // if background smaller than camera — keep centered on that axis
            if (clampMinX > clampMaxX)
            {
                targetPosition.x = (minX + maxX) * 0.5f;
            }
            else
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, clampMinX, clampMaxX);
            }

            if (clampMinY > clampMaxY)
            {
                targetPosition.y = (minY + maxY) * 0.5f;
            }
            else
            {
                targetPosition.y = Mathf.Clamp(targetPosition.y, clampMinY, clampMaxY);
            }
        }

        // 4. smooth move camera to clamped target
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
    }

    [ContextMenu("Calculate Level Bounds From Scene")]
    public void CalculateLevelBounds()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        if (allObjects.Length == 0)
        {
            Debug.LogWarning("CameraFollow: no objects found in scene to calculate bounds.");
            return;
        }

        float foundMinX = float.MaxValue;
        float foundMaxX = float.MinValue;
        float foundMinY = float.MaxValue;
        float foundMaxY = float.MinValue;

        int objectsIncluded = 0;

        foreach (GameObject obj in allObjects)
        {
            // Skip if this layer shouldn't be included
            if (!ShouldIncludeLayer(obj.layer))
                continue;

            // Skip camera objects
            if (obj.GetComponent<Camera>())
                continue;

            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                Bounds bounds = rend.bounds;

                foundMinX = Mathf.Min(foundMinX, bounds.min.x);
                foundMaxX = Mathf.Max(foundMaxX, bounds.max.x);
                foundMinY = Mathf.Min(foundMinY, bounds.min.y);
                foundMaxY = Mathf.Max(foundMaxY, bounds.max.y);
                objectsIncluded++;
            }
        }

        if (objectsIncluded == 0)
        {
            Debug.LogWarning("CameraFollow: No objects with renderers found in included layers.");
            return;
        }

        float padding = 2f;
        minX = foundMinX - padding;
        maxX = foundMaxX + padding;
        minY = foundMinY - padding;
        maxY = foundMaxY + padding;

        Debug.Log(
            $"CameraFollow: bounds set to X({minX:F1}..{maxX:F1}) Y({minY:F1}..{maxY:F1}) from {objectsIncluded} objects"
        );
    }

    void OnDrawGizmosSelected()
    {
        if (!useBoundaries)
            return;

        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam != null)
        {
            // only for view
            float tempHalfHeight = cam.orthographicSize;
            float tempHalfWidth = tempHalfHeight * cam.aspect;

            Gizmos.color = Color.green;
            Vector3 bottomLeft = new Vector3(minX, minY, 0);
            Vector3 bottomRight = new Vector3(maxX, minY, 0);
            Vector3 topLeft = new Vector3(minX, maxY, 0);
            Vector3 topRight = new Vector3(maxX, maxY, 0);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }

        if (useDeadZone && player != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireCube(player.position, new Vector3(deadZoneWidth, deadZoneHeight, 0));
        }
    }

    void OnGUI()
    {
        if (!showZoomInfo || !Application.isPlaying)
            return;

        // Only show during gameplay if that option is enabled
        if (onlyShowDuringGameplay && !gameStarted)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.white },
            alignment = TextAnchor.LowerRight,
            padding = new RectOffset(5, 5, 5, 5),
        };

        string zoomText = $"Zoom: {currentZoom:F1}\nQ/E or mouse wheel\nR for reset";

        Vector2 textSize = style.CalcSize(new GUIContent(zoomText));
        float width = textSize.x + 20;
        float height = textSize.y + 10;

        float xPos = Screen.width - width - 10;
        float yPos = Screen.height - height - 10;

        Color oldColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(new Rect(xPos, yPos, width, height), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Label(new Rect(xPos, yPos, width, height), zoomText, style);

        GUI.color = oldColor;
    }
}