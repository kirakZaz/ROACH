using System.Collections;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [Header("Explosion Sprites")]
    [SerializeField]
    private Sprite[] explosionFrames;

    [SerializeField]
    private float frameRate = 8f; // Slower = easier to see

    [SerializeField]
    private float explosionScale = 1f;

    [Header("Explosion Timing")]
    [SerializeField]
    private float damageDelay = 0.3f; // Wait before dealing damage

    [Header("Explosion Settings")]
    [SerializeField]
    private float explosionRadius = 3f;

    [SerializeField]
    private int explosionDamage = 2;

    [SerializeField]
    private float explosionForce = 500f;

    [Header("Detection")]
    [SerializeField]
    private string playerTag = "Player";

    [SerializeField]
    private bool explodeOnTouch = true;

    [Header("Audio (Optional)")]
    [SerializeField]
    private AudioClip explosionSound;

    private SpriteRenderer spriteRenderer;
    private Collider2D barrelCollider;
    private Sprite originalSprite;
    private Vector3 originalScale;
    private float barrelSpriteWidth;
    private bool hasExploded = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        barrelCollider = GetComponent<Collider2D>();

        if (!spriteRenderer)
        {
            Debug.LogError("Explosion: SpriteRenderer not found!", this);
        }
        else
        {
            originalSprite = spriteRenderer.sprite;
            originalScale = transform.localScale;

            if (originalSprite)
            {
                barrelSpriteWidth = originalSprite.bounds.size.x * transform.localScale.x;
            }
        }

        if (barrelCollider && barrelCollider.isTrigger)
        {
            barrelCollider.isTrigger = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag(playerTag) || hasExploded)
            return;

        if (explodeOnTouch)
        {
            Explode();
            return;
        }

        bool hitFromAbove = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (contact.normal.y > 0.5f)
            {
                hitFromAbove = true;
                break;
            }
        }

        if (hitFromAbove)
        {
            Explode();
        }
    }

    public void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        Debug.Log("BARREL EXPLODING!");

        if (explosionSound)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        if (barrelCollider)
        {
            barrelCollider.enabled = false;
        }

        // Start animation immediately
        StartCoroutine(PlayExplosionAnimation());

        // Deal damage after delay (so player sees explosion start)
        StartCoroutine(DealDamageAfterDelay());
    }

    private IEnumerator DealDamageAfterDelay()
    {
        // Wait a bit so player sees explosion animation start
        yield return new WaitForSeconds(damageDelay);

        DealExplosionDamage();
    }

    private IEnumerator PlayExplosionAnimation()
    {
        if (explosionFrames == null || explosionFrames.Length == 0)
        {
            Debug.LogWarning("Explosion: No explosion frames assigned!");
            Destroy(gameObject, 0.5f);
            yield break;
        }

        float frameDuration = 1f / frameRate;

        // Play each frame
        for (int i = 0; i < explosionFrames.Length; i++)
        {
            if (spriteRenderer && explosionFrames[i])
            {
                spriteRenderer.sprite = explosionFrames[i];

                // Calculate scale to match barrel size
                float explosionSpriteWidth = explosionFrames[i].bounds.size.x;
                float scaleMultiplier = barrelSpriteWidth / explosionSpriteWidth;

                transform.localScale = originalScale * scaleMultiplier * explosionScale;
            }

            yield return new WaitForSeconds(frameDuration);
        }

        Destroy(gameObject);
    }

    private void DealExplosionDamage()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag(playerTag))
            {
                // Deal damage
                var playerLives = col.GetComponent<PlayerLives>();
                if (playerLives)
                {
                    playerLives.LoseLife(explosionDamage);
                }

                // Apply knockback
                var rb = col.GetComponent<Rigidbody2D>();
                if (rb)
                {
                    Vector2 direction = (col.transform.position - transform.position).normalized;
                    rb.AddForce(direction * explosionForce);
                }

                // Trigger red flash effect (find PlayerHurtbox and trigger its flash)
                var playerHurtbox =
                    col.GetComponentInChildren<Roach.Assets.Scripts.Player.PlayerHurtbox>();
                if (playerHurtbox)
                {
                    // Start the flash coroutine in PlayerHurtbox
                    playerHurtbox.StartCoroutine("QuickFlash");
                }
            }

            var otherBarrel = col.GetComponent<Explosion>();
            if (otherBarrel && otherBarrel != this)
            {
                otherBarrel.Explode();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}
