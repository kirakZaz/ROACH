using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitter2D : MonoBehaviour
{
    public Camera targetCamera;

    [Tooltip("Extra scale to bleed beyond edges (1 = exact fit).")]
    public float bleed = 1.02f; // small padding so no gaps at edges

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || targetCamera == null)
            return;
        if (!targetCamera.orthographic)
        {
            Debug.LogWarning("BackgroundFitter2D works with an orthographic camera.");
            return;
        }

        // Camera world size
        float worldHeight = targetCamera.orthographicSize * 2f;
        float worldWidth = worldHeight * targetCamera.aspect;

        // Sprite world size (respecting PPU & sprite pixels)
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        // Scale needed to cover the whole camera
        float scaleX = worldWidth / spriteSize.x;
        float scaleY = worldHeight / spriteSize.y;
        float scale = Mathf.Max(scaleX, scaleY) * bleed; // cover fully in both axes

        transform.localScale = new Vector3(scale, scale, 1f);

        // Optional: keep background centered on camera (no parallax)
        // Comment this out if you have parallax follow instead.
        Vector3 camPos = targetCamera.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
    }
}
