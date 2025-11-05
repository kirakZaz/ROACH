using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public Vector3 offset = new Vector3(0f, 2f, -10f);

    [Header("Follow Settings")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.125f;
    public bool followX = true;
    public bool followY = true;

    [Header("Camera Zoom")]
    [Range(3f, 15f)]
    public float cameraSize = 5f; // less -> closer to hero

    [Header("Level Boundaries")]
    public bool useBoundaries = true;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -10f;
    public float maxY = 20f;

    [Header("Dead Zone")]
    public bool useDeadZone = false;
    public Vector2 deadZone = new Vector2(2f, 1f);

    [Header("Lookahead")]
    public bool enableLookahead = true;
    public float lookaheadDistance = 2f;
    public float lookaheadSpeed = 2f;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private float currentLookahead = 0f;
    private PlayerController2D playerController;
    private float lastPlayerDirection = 1f;

    void Start()
    {
        cam = GetComponent<Camera>();

        // camera settings (zoom)
        if (cam.orthographic)
        {
            cam.orthographicSize = cameraSize;
        }

        // looking for a player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponent<PlayerController2D>();
            }
            else
            {
                Debug.LogError(
                    "CameraFollow: Не найден игрок! Назначьте Player в инспекторе или добавьте тег 'Player' вашему герою."
                );
            }
        }
        else
        {
            playerController = player.GetComponent<PlayerController2D>();
        }

        // initialisationg cam to the hero
        if (player != null)
        {
            Vector3 startPos = player.position + offset;
            startPos.z = transform.position.z;
            transform.position = startPos;
        }
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        float currentDirection = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(currentDirection) > 0.1f)
        {
            lastPlayerDirection = currentDirection;
        }

        Vector3 targetPosition = CalculateTargetPosition();

        if (useDeadZone)
        {
            targetPosition = ApplyDeadZone(targetPosition);
        }

        if (enableLookahead && Mathf.Abs(lastPlayerDirection) > 0.1f)
        {
            float targetLookahead = lastPlayerDirection * lookaheadDistance;
            currentLookahead = Mathf.Lerp(
                currentLookahead,
                targetLookahead,
                lookaheadSpeed * Time.deltaTime
            );
            targetPosition.x += currentLookahead;
        }

        if (useBoundaries)
        {
            targetPosition = ApplyBoundaries(targetPosition);
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);

        if (!followX)
            smoothedPosition.x = transform.position.x;
        if (!followY)
            smoothedPosition.y = transform.position.y;

        transform.position = smoothedPosition;
    }

    Vector3 CalculateTargetPosition()
    {
        Vector3 target = player.position + offset;
        target.z = transform.position.z; 
        return target;
    }

    Vector3 ApplyDeadZone(Vector3 targetPos)
    {
        Vector3 currentPos = transform.position;

        float deltaX = targetPos.x - currentPos.x;
        if (Mathf.Abs(deltaX) < deadZone.x)
        {
            targetPos.x = currentPos.x;
        }

        float deltaY = targetPos.y - currentPos.y;
        if (Mathf.Abs(deltaY) < deadZone.y)
        {
            targetPos.y = currentPos.y;
        }

        return targetPos;
    }

    Vector3 ApplyBoundaries(Vector3 position)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        position.x = Mathf.Clamp(position.x, minX + halfWidth, maxX - halfWidth);
        position.y = Mathf.Clamp(position.y, minY + halfHeight, maxY - halfHeight);

        return position;
    }

    public void SetCameraSize(float newSize)
    {
        cameraSize = Mathf.Clamp(newSize, 3f, 15f);
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = cameraSize;
        }
    }

    public void ShakeCamera(float duration = 0.2f, float magnitude = 0.1f)
    {
        StartCoroutine(Shake(duration, magnitude));
    }

    System.Collections.IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
    }

    public void CalculateLevelBounds()
    {
        GameObject[] allObjects = GameObject.FindGameObjectsWithTag("Ground");

        if (allObjects.Length == 0)
        {
            Debug.LogWarning(
                "CameraFollow: Не найдены объекты с тегом 'Ground' для расчёта границ"
            );
            return;
        }

        float minXFound = float.MaxValue;
        float maxXFound = float.MinValue;
        float minYFound = float.MaxValue;
        float maxYFound = float.MinValue;

        foreach (GameObject obj in allObjects)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                minXFound = Mathf.Min(minXFound, rend.bounds.min.x);
                maxXFound = Mathf.Max(maxXFound, rend.bounds.max.x);
                minYFound = Mathf.Min(minYFound, rend.bounds.min.y);
                maxYFound = Mathf.Max(maxYFound, rend.bounds.max.y);
            }
        }

        float padding = 5f;
        minX = minXFound - padding;
        maxX = maxXFound + padding;
        minY = minYFound - padding;
        maxY = maxYFound + padding;

        Debug.Log($"Границы уровня установлены: X({minX}, {maxX}), Y({minY}, {maxY})");
    }

    void OnDrawGizmosSelected()
    {
        if (!useBoundaries)
            return;

        Gizmos.color = Color.yellow;
        Vector3 bottomLeft = new Vector3(minX, minY, 0);
        Vector3 bottomRight = new Vector3(maxX, minY, 0);
        Vector3 topLeft = new Vector3(minX, maxY, 0);
        Vector3 topRight = new Vector3(maxX, maxY, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        if (useDeadZone && player != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(player.position, new Vector3(deadZone.x * 2, deadZone.y * 2, 0.1f));
        }
    }
}
