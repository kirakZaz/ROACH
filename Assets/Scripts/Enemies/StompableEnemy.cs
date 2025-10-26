using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class StompableEnemy : MonoBehaviour, IStompable
{
    [Header("Health")]
    [SerializeField, Min(1)]
    private int stompsToKill = 2; // ← edit in Inspector

    [
        SerializeField,
        Tooltip(
            "Ignore additional stomps for this time after a valid stomp to avoid multi-counts from the same contact."
        )
    ]
    private float stompCooldown = 0.15f; // avoids duplicate counts while still on top

    [Header("Partial Hit Feedback")]
    [SerializeField]
    private bool flashOnHit = true;

    [SerializeField]
    private Color hitFlashColor = new Color(1f, 0.6f, 0.6f, 1f);

    [SerializeField]
    private float hitFlashTime = 0.08f;

    [Header("Death")]
    [SerializeField]
    private float deathDuration = 0.45f;

    [Header("Drop On Death")]
    [SerializeField]
    private GameObject rockEdiblePrefab;

    [SerializeField]
    private bool bigRockScaleOn = true;

    [SerializeField, Range(0.5f, 4f)]
    private float bigRockScale = 2.5f;

    [SerializeField]
    private Vector3 dropLocalScale = Vector3.one;

    [SerializeField]
    private string dropSortingLayer = "Default";

    [SerializeField]
    private int dropOrderInLayer = 10;

    [SerializeField]
    private Vector2 dropOffset = new Vector2(0f, -0.05f);

    [SerializeField, Tooltip("Small upward nudge to avoid spawning inside ground")]
    private float dropLift = 0.06f;

    [SerializeField]
    private Sprite fallbackDropSprite;

    // ---- internals ----
    private int stompCount = 0;
    private bool isDying = false;
    private float lastStompTime = -999f;

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
    }

    private void OnEnable()
    {
        // reset state
        isDying = false;
        stompCount = 0;
        lastStompTime = -999f;

        if (spriteRenderer)
        {
            var c = initialColor;
            c.a = 1f;
            spriteRenderer.color = c;
            spriteRenderer.enabled = true;
        }
        transform.localScale = initialScale;

        if (enemyCollider)
            enemyCollider.enabled = true;
        if (enemyRb)
            enemyRb.simulated = true;
    }

    // ===== IStompable =====
    public void TakeStomp(GameObject stomper)
    {
        if (isDying)
            return;

        // cooldown to prevent multiple counts from one continuous contact
        if (Time.time - lastStompTime < stompCooldown)
            return;
        lastStompTime = Time.time;

        stompCount++;

        // debug (optional)
        // Debug.Log($"[{name}] stomp {stompCount}/{stompsToKill}");

        if (stompCount >= stompsToKill)
        {
            StartCoroutine(DeathSequence());
        }
        else
        {
            if (flashOnHit && spriteRenderer)
                StartCoroutine(FlashHit());
        }
    }

    private IEnumerator FlashHit()
    {
        Color before = spriteRenderer.color;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashTime);
        if (spriteRenderer)
            spriteRenderer.color = before;
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

        SpawnDropIfAny();
        Destroy(gameObject);
    }

    private void SpawnDropIfAny()
    {
        if (!rockEdiblePrefab)
            return;

        float direction = transform.localScale.x >= 0 ? -1f : 1f;
        Vector3 spawnPos =
            transform.position + new Vector3(direction * 0.35f, 0.35f + dropLift, 0f);

        GameObject drop = Instantiate(rockEdiblePrefab, spawnPos, Quaternion.identity);

        // Always edible/tagged
        try
        {
            drop.tag = "Edible";
        }
        catch { }

        // Sprite
        var sr = drop.GetComponentInChildren<SpriteRenderer>();
        if (!sr)
            sr = drop.AddComponent<SpriteRenderer>();
        if (sr && sr.sprite == null && fallbackDropSprite)
            sr.sprite = fallbackDropSprite;
        if (sr)
        {
            sr.sortingLayerName = dropSortingLayer;
            sr.sortingOrder = dropOrderInLayer;
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        // Scale/collider/rigidbody
        drop.transform.localScale = dropLocalScale * (bigRockScaleOn ? bigRockScale : 1f);

        var col = drop.GetComponent<Collider2D>();
        if (!col)
            col = drop.AddComponent<CircleCollider2D>();
        col.isTrigger = false;

        var rb = drop.GetComponent<Rigidbody2D>();
        if (!rb)
            rb = drop.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = new Vector2(direction * 1.8f, 2.5f);

        if (!drop.GetComponent<FreezeOnGround>())
            drop.AddComponent<FreezeOnGround>();
    }

    private void OnDrawGizmosSelected()
    {
        // (optional visual) show small dot where drop spawns
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + (Vector3)dropOffset, 0.08f);
    }
}
