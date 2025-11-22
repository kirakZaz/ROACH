using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PlayerStomp : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Rigidbody2D playerRb;

    [SerializeField]
    private Collider2D playerHurtCollider;

    [Header("Stomp Tuning")]
    [SerializeField]
    private float minDownSpeed = -1.0f;

    [SerializeField]
    private float bounceVelocity = 9.0f;

    [SerializeField]
    private float postStompInvuln = 0.25f;

    [SerializeField]
    private float stompCooldown = 0.1f; // Prevent multiple stomps on same enemy too quickly

    [Header("Audio")]
    [SerializeField]
    private bool playStompSfx = true;
    
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip sfxAttack;

    [SerializeField]
    [Range(0f, 1f)]
    private float sfxVolume = 0.5f;

    [SerializeField]
    private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [SerializeField]
    private float cutOffAfterSeconds = 0f;

    // Track what we're currently standing on
    private bool isStandingOnEnemy = false;
    private float lastStompTime = -999f;
    private IStompable currentEnemyUnderfoot = null;

    private void Reset()
    {
        var rb = GetComponentInParent<Rigidbody2D>();
        if (rb)
            playerRb = rb;

        var src = GetComponentInParent<AudioSource>();
        if (src)
            audioSource = src;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHandleStomp(other, true);

    private void OnTriggerStay2D(Collider2D other)
    {
        // Check if we're standing on an enemy
        if (playerRb && playerRb.linearVelocity.y > -0.1f && playerRb.linearVelocity.y < 0.1f)
        {
            IStompable stompable = other.GetComponentInParent<IStompable>();
            if (stompable != null)
            {
                Transform enemyRoot = (stompable as MonoBehaviour).transform;
                if (transform.position.y >= enemyRoot.position.y)
                {
                    isStandingOnEnemy = true;
                    currentEnemyUnderfoot = stompable;
                    return;
                }
            }
        }
        
        TryHandleStomp(other, false);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Clear standing status when leaving enemy
        IStompable stompable = other.GetComponentInParent<IStompable>();
        if (stompable != null && stompable == currentEnemyUnderfoot)
        {
            isStandingOnEnemy = false;
            currentEnemyUnderfoot = null;
        }
    }

    private void TryHandleStomp(Collider2D other, bool isEnter)
    {
        if (!playerRb)
            return;

        // Cooldown to prevent double-stomps
        if (Time.time - lastStompTime < stompCooldown)
            return;

        // Must be moving downward (or just entered)
        if (!isEnter && playerRb.linearVelocity.y > minDownSpeed)
            return;

        // Must hit something stompable
        IStompable stompable = other.GetComponentInParent<IStompable>();
        if (stompable == null)
            return;

        // Ensure our stomp collider is above enemy root (top hit)
        Transform enemyRoot = (stompable as MonoBehaviour).transform;
        if (transform.position.y < enemyRoot.position.y)
            return;

        // Don't stomp if we're just standing (allow jumping while on enemy)
        if (!isEnter && isStandingOnEnemy && stompable == currentEnemyUnderfoot)
        {
            // Player is standing on enemy, don't auto-stomp, let them jump
            return;
        }

        // ---- STOMP CONFIRMED ----
        lastStompTime = Time.time;

        // 1) Bounce
        Vector2 v = playerRb.linearVelocity;
        v.y = bounceVelocity;
        playerRb.linearVelocity = v;

        // 2) Temporary invulnerability
        if (playerHurtCollider != null)
        {
            Collider2D enemyCol = other;
            StartCoroutine(TempIgnoreCollision(playerHurtCollider, enemyCol, postStompInvuln));
        }

        // 3) Play sound
        if (sfxAttack != null)
        {
            if (!audioSource)
            {
                audioSource = GetComponentInParent<AudioSource>();
            }

            if (audioSource)
            {
                audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
                audioSource.PlayOneShot(sfxAttack, sfxVolume);

                if (cutOffAfterSeconds > 0f)
                    StartCoroutine(StopClipAfter(audioSource, cutOffAfterSeconds));
            }
        }

        // 4) Notify enemy
        stompable.TakeStomp(playerRb.gameObject);
        
        // Clear standing status since we just stomped
        isStandingOnEnemy = false;
        currentEnemyUnderfoot = null;
    }

    private IEnumerator TempIgnoreCollision(Collider2D a, Collider2D b, float seconds)
    {
        if (a && b)
        {
            Physics2D.IgnoreCollision(a, b, true);
            yield return new WaitForSeconds(seconds);
            if (a && b)
                Physics2D.IgnoreCollision(a, b, false);
        }
    }

    private IEnumerator StopClipAfter(AudioSource src, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (src)
            src.Stop();
    }

    // Public method to check if player can jump (called from PlayerController)
    public bool IsStandingOnEnemy()
    {
        return isStandingOnEnemy;
    }
}