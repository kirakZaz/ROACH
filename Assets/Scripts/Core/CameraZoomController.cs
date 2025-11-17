using UnityEngine;

public class CameraZoomController : MonoBehaviour
{
    [Header("Zoom Settings")]
    [Range(2f, 15f)]
    public float currentZoom = 5f;
    public float zoomSpeed = 2f;
    public float minZoom = 3f;
    public float maxZoom = 10f;

    [Header("Controls")]
    public KeyCode zoomInKey = KeyCode.Q;
    public KeyCode zoomOutKey = KeyCode.E;
    public KeyCode resetKey = KeyCode.R;

    private Camera cam;
    private CameraFollow cameraFollow;
    private float defaultZoom;

    void Start()
    {
        cam = GetComponent<Camera>();
        cameraFollow = GetComponent<CameraFollow>();

        if (!cam)
        {
            Debug.LogError("CameraZoomController: Camera component is missing!");
            enabled = false;
            return;
        }

        defaultZoom = currentZoom;
        ApplyZoom();
    }

    void Update()
    {
        bool zoomChanged = false;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            currentZoom -= scrollInput * zoomSpeed * 3f;
            zoomChanged = true;
        }

        if (Input.GetKey(zoomInKey))
        {
            currentZoom -= zoomSpeed * Time.deltaTime;
            zoomChanged = true;
        }

        if (Input.GetKey(zoomOutKey))
        {
            currentZoom += zoomSpeed * Time.deltaTime;
            zoomChanged = true;
        }

        if (Input.GetKeyDown(resetKey))
        {
            currentZoom = defaultZoom;
            zoomChanged = true;
        }

        if (zoomChanged)
        {
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            ApplyZoom();
        }
    }

    void ApplyZoom()
    {
        if (cam && cam.orthographic)
        {
            cam.orthographicSize = currentZoom;

            if (cameraFollow != null)
            {
                cameraFollow.cameraSize = currentZoom;
                cameraFollow.currentZoom = currentZoom;

                // force cameraFollow to recalc background bounds on next frame
                // (it checks size change in LateUpdate)
            }
        }
    }
}
