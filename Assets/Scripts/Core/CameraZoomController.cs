using UnityEngine;

public class CameraZoomController : MonoBehaviour
{
    [Header("Zoom Settings")]
    [Range(2f, 15f)]
    public float currentZoom = 5f;
    public float zoomSpeed = 1f;
    public float minZoom = 3f;
    public float maxZoom = 10f;

    [Header("Quick Zoom Presets")]
    public float closeZoom = 3.5f;
    public float mediumZoom = 5f; 
        public float farZoom = 8f; 

    [Header("Controls")]
    public KeyCode zoomInKey = KeyCode.Q;
    public KeyCode zoomOutKey = KeyCode.E;
    public KeyCode preset1Key = KeyCode.Alpha1; 
    public KeyCode preset2Key = KeyCode.Alpha2; 
    public KeyCode preset3Key = KeyCode.Alpha3;

    private Camera cam;
    private CameraFollow cameraFollow;

    void Start()
    {
        cam = GetComponent<Camera>();
        cameraFollow = GetComponent<CameraFollow>();

        if (cam == null)
        {
            Debug.LogError("CameraZoomController: Нужен компонент Camera!");
            enabled = false;
            return;
        }

        // Устанавливаем начальный зум
        if (cam.orthographic)
        {
            cam.orthographicSize = currentZoom;
            if (cameraFollow != null)
            {
                cameraFollow.cameraSize = currentZoom;
            }
        }
    }

    void Update()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            currentZoom -= scrollInput * zoomSpeed * 3f;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            ApplyZoom();
        }

        if (Input.GetKey(zoomInKey))
        {
            currentZoom -= zoomSpeed * Time.deltaTime;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            ApplyZoom();
        }

        if (Input.GetKey(zoomOutKey))
        {
            currentZoom += zoomSpeed * Time.deltaTime;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            ApplyZoom();
        }

        // Быстрые пресеты на цифры 1-2-3
        if (Input.GetKeyDown(preset1Key))
        {
            currentZoom = closeZoom;
            ApplyZoom();
            ShowZoomInfo("Зум: БЛИЗКО");
        }

        if (Input.GetKeyDown(preset2Key))
        {
            currentZoom = mediumZoom;
            ApplyZoom();
            ShowZoomInfo("Зум: СРЕДНЕ");
        }

        if (Input.GetKeyDown(preset3Key))
        {
            currentZoom = farZoom;
            ApplyZoom();
            ShowZoomInfo("Зум: ДАЛЕКО");
        }

        // Debug info
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ShowDebugInfo();
        }
    }

    void ApplyZoom()
    {
        if (cam.orthographic)
        {
            cam.orthographicSize = currentZoom;

            if (cameraFollow != null)
            {
                cameraFollow.cameraSize = currentZoom;
            }
        }
    }

    void ShowZoomInfo(string message)
    {
        Debug.Log($"🎥 {message} (значение: {currentZoom:F1})");
    }

    void ShowDebugInfo()
    {
        string info =
            $@"
📷 CAMERA DEBUG INFO:
- Current Zoom: {currentZoom:F1}
- Camera Size: {cam.orthographicSize:F1}
- Aspect Ratio: {cam.aspect:F2}
- View Width: {cam.orthographicSize * 2f * cam.aspect:F1}
- View Height: {cam.orthographicSize * 2f:F1}

🎮 CONTROLS:
- Mouse Wheel: Zoom In/Out
- Q/E: Zoom In/Out
- 1/2/3: Quick presets
- F1: Show this info
        ";
        Debug.Log(info);
    }

    void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0, 0, 0, 0.5f);

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.MiddleLeft;
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            string zoomText = $"Zoom: {currentZoom:F1}\n";
            zoomText += "Q/E or mouse wheel \n";
            zoomText += "1-2-3 for presets";

            GUI.Box(new Rect(100, 10, 200, 60), zoomText, style);
        }
    }

    public void AnimateZoomTo(float targetZoom, float duration = 0.5f)
    {
        StartCoroutine(AnimateZoom(targetZoom, duration));
    }

    System.Collections.IEnumerator AnimateZoom(float targetZoom, float duration)
    {
        float startZoom = currentZoom;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            t = Mathf.SmoothStep(0, 1, t);

            currentZoom = Mathf.Lerp(startZoom, targetZoom, t);
            ApplyZoom();

            yield return null;
        }

        currentZoom = targetZoom;
        ApplyZoom();
    }
}
