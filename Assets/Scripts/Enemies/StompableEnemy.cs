using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class StompableEnemy : MonoBehaviour, IStompable
{
    [Header("Big Drop (not edible)")]
    [SerializeField]
    private bool dropBigRock = false; // turn ON to drop a big rock

    [SerializeField]
    private bool bigRockScaleOn = true; // turn ON to drop a big rock

    [SerializeField]
    private float bigRockScale = 2.5f; // 2.0–3.0 works well

    [SerializeField]
    private string bigRockTag = "Untagged"; // or create a "Resource" tag

    [Header("Drop Visual/Debug")]
    [SerializeField]
    private Sprite fallbackDropSprite;

    [SerializeField]
    private string dropSortingLayer = "Default";

    [SerializeField]
    private int dropOrderInLayer = 10;

    [SerializeField]
    private Vector3 dropLocalScale = Vector3.one;

    [SerializeField]
    private float armEdibleDelay = 0.15f;

    private Vector3 lastDropPos;

    [Header("Health")]
    [SerializeField]
    private int stompsToKill = 2;

    [Header("Death")]
    [SerializeField]
    private float deathDuration = 0.45f;

    [SerializeField]
    private GameObject rockEdiblePrefab;

    [SerializeField]
    private Vector2 dropOffset = new Vector2(0f, -0.05f);

    [Tooltip("Small upward nudge to avoid spawning inside ground")]
    [SerializeField]
    private float dropLift = 0.06f;

    private int stompCount = 0;
    private bool isDying = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;
    private Rigidbody2D enemyRb;

    private Color initialColor = Color.white;
    private Vector3 initialScale;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        enemyRb = GetComponent<Rigidbody2D>();

        if (spriteRenderer)
            initialColor = spriteRenderer.color;
        initialScale = transform.localScale;

        // Safety: if animator cleared the sprite somehow, try to restore from Resources (optional)
        if (spriteRenderer && spriteRenderer.sprite == null)
            spriteRenderer.sprite = Resources.Load<Sprite>("bug2 (1)_0");
    }

    private void OnEnable()
    {
        if (spriteRenderer)
        {
            var c = initialColor;
            c.a = 1f;
            spriteRenderer.color = c;
            spriteRenderer.enabled = true;
        }
        transform.localScale = initialScale;

        isDying = false;
        stompCount = 0;

        if (enemyCollider)
            enemyCollider.enabled = true;
        if (enemyRb)
            enemyRb.simulated = true;
    }

    public void TakeStomp(GameObject stomper)
    {
        if (isDying)
            return;

        stompCount++;
        if (stompCount >= stompsToKill)
        {
            StartCoroutine(DeathSequence());
        }
        else
        {
            // optional partial-hit feedback (sound/flash)
        }
    }

    private IEnumerator DeathSequence()
    {
        isDying = true;

        // Stop blocking the player during death
        if (enemyCollider)
            enemyCollider.enabled = false;
        if (enemyRb)
            enemyRb.simulated = false;

        float t = 0f;
        Color start = spriteRenderer ? spriteRenderer.color : Color.white;
        Vector3 startScale = transform.localScale;

        while (t < deathDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / deathDuration);

            if (spriteRenderer)
            {
                Color c = start;
                c.a = Mathf.Lerp(1f, 0f, k);
                spriteRenderer.color = c;
            }

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, k);
            yield return null;
        }

        if (rockEdiblePrefab)
        {
            float direction = transform.localScale.x > 0 ? -1f : 1f;

            Vector3 spawnPos =
                transform.position + new Vector3(direction * 0.35f, 0.35f + dropLift, 0f);

            GameObject drop = Instantiate(rockEdiblePrefab, spawnPos, Quaternion.identity);

            // always edible; scale 1f или bigRockScale — как захочешь
            float scale = bigRockScaleOn ? bigRockScale : 1f;
            PrepareDropAlwaysEdible(drop, direction, scale);

            Debug.Log($"[StompableEnemy] Dropped edible rock (scale {scale}) at {spawnPos}");
        }
        else
        {
            Debug.LogWarning("[StompableEnemy] rockEdiblePrefab is not assigned. Nothing to drop.");
        }

        Destroy(gameObject);
    }

    private void PrepareDropAlwaysEdible(GameObject drop, float direction, float scale)
    {
        if (!drop)
            return;

        // Tag: always edible
        try
        {
            drop.tag = "Edible";
        }
        catch
        { /* ensure the tag exists in Tags & Layers */
        }

        // SpriteRenderer
        var sr = drop.GetComponentInChildren<SpriteRenderer>();
        if (!sr)
            sr = drop.AddComponent<SpriteRenderer>();
        if (sr && sr.sprite == null && fallbackDropSprite != null)
            sr.sprite = fallbackDropSprite;
        if (sr)
        {
            sr.sortingLayerName = dropSortingLayer;
            sr.sortingOrder = dropOrderInLayer;
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        // Scale (2D colliders scale with transform)
        drop.transform.localScale = dropLocalScale * scale;

        // Collider (non-trigger so it lands)
        var col = drop.GetComponent<Collider2D>();
        if (!col)
            col = drop.AddComponent<CircleCollider2D>();
        if (col is BoxCollider2D b)
            b.isTrigger = false;
        if (col is CircleCollider2D cc)
            cc.isTrigger = false;

        // Rigidbody (so it falls), then small toss
        var rb = drop.GetComponent<Rigidbody2D>();
        if (!rb)
            rb = drop.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = new Vector2(direction * 1.8f, 2.5f);

        // Land and freeze when touching Ground (чтобы не скатывался)
        if (!drop.GetComponent<FreezeOnGround>())
            drop.AddComponent<FreezeOnGround>();
    }

    private IEnumerator ArmEdibleAfterDelay(GameObject drop)
    {
        // temporarily untagged
        string originalTag = "Edible";
        try
        {
            drop.tag = "Untagged";
        }
        catch { }

        yield return new WaitForSeconds(armEdibleDelay);

        // now become edible
        try
        {
            drop.tag = originalTag;
        }
        catch
        {
            Debug.LogWarning("[StompableEnemy] Tag 'Edible' missing. Create it in Tags & Layers.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastDropPos, 0.1f);
    }

    private void EnsureEdibleSetup(GameObject drop)
    {
        if (!drop)
            return;

        // Ensure tag
        if (drop.tag != "Edible")
        {
            try
            {
                drop.tag = "Edible";
            }
            catch
            {
                Debug.LogWarning(
                    "[StompableEnemy] Tag 'Edible' is missing in project. Add it or set on prefab."
                );
            }
        }

        // Ensure a visible sprite (optional, in case prefab has no sprite)
        var sr = drop.GetComponentInChildren<SpriteRenderer>();
        if (!sr)
            sr = drop.AddComponent<SpriteRenderer>(); // fallback invisible until you assign sprite on prefab

        // Ensure collider so it sits on ground and Witchetty can measure edge distance
        var col = drop.GetComponent<Collider2D>();
        if (!col)
        {
            col = drop.AddComponent<BoxCollider2D>();
            (col as BoxCollider2D).isTrigger = false;
            Debug.Log("[StompableEnemy] Added BoxCollider2D to drop.");
        }

        // Ensure Rigidbody2D so it falls naturally (optional but nice)
        var rb = drop.GetComponent<Rigidbody2D>();
        if (!rb)
        {
            rb = drop.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            Debug.Log("[StompableEnemy] Added Rigidbody2D to drop.");
        }
    }
}
