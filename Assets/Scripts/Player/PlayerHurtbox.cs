using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roach.Assets.Scripts.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))] // trigger on child
    public class PlayerHurtbox : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField]
        private string enemyTag = "Enemy";

        [Header("Lives")]
        [SerializeField]
        private PlayerLives playerLives; // auto-find in parent if null

        [Header("Knockback")]
        [SerializeField]
        private float knockbackHorizontal = 6f;

        [SerializeField]
        private float knockbackVertical = 4f;

        [SerializeField]
        private float hurtInvulnerability = 1.0f;

        [Header("Feedback (optional)")]
        [SerializeField]
        private SpriteRenderer spriteRenderer; // blink target (usually on Player root)

        private Rigidbody2D rb; // Player root RB
        private bool isInvulnerable;

        private void Awake()
        {
            rb = GetComponentInParent<Rigidbody2D>();
            if (!playerLives)
                playerLives = GetComponentInParent<PlayerLives>();
            if (!spriteRenderer)
                spriteRenderer = GetComponentInParent<SpriteRenderer>();

            if (!rb)
                Debug.LogError("PlayerHurtbox: Rigidbody2D not found in parent Player.", this);
            if (!playerLives)
                Debug.LogError("PlayerHurtbox: PlayerLives not found in parent Player.", this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isInvulnerable)
                return;
            if (!other.CompareTag(enemyTag))
                return;
            if (!rb || !playerLives)
                return;

            // lose one life
            playerLives.LoseLife(1);

            // if no lives left -> reload
            if (playerLives.CurrentLives <= 0)
            {
                ReloadScene();
                return;
            }

            // knockback and brief i-frames
            DoKnockbackFrom(other.transform);
            StartCoroutine(InvulnerabilityFrames());
        }

        private void DoKnockbackFrom(Transform enemy)
        {
            // if enemy is to the right, knock left; else right
            float dir = (transform.position.x < enemy.position.x) ? -1f : 1f;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(
                new Vector2(dir * knockbackHorizontal, knockbackVertical),
                ForceMode2D.Impulse
            );
        }

        private IEnumerator InvulnerabilityFrames()
        {
            isInvulnerable = true;

            if (spriteRenderer)
            {
                const float period = 0.1f;
                float t = 0f;
                while (t < hurtInvulnerability)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                    yield return new WaitForSeconds(period);
                    t += period;
                }
                spriteRenderer.enabled = true;
            }
            else
            {
                yield return new WaitForSeconds(hurtInvulnerability);
            }

            isInvulnerable = false;
        }

        private void ReloadScene()
        {
            var sc = SceneManager.GetActiveScene();
            SceneManager.LoadScene(sc.buildIndex);
        }

        private void OnValidate()
        {
            var col = GetComponent<Collider2D>();
            if (col && !col.isTrigger)
                Debug.LogWarning(
                    "PlayerHurtbox: this Collider2D should be set to IsTrigger.",
                    this
                );
        }
    }
}
