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
    private float stompCooldown = 0.1f;

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

        if (Time.time - lastStompTime < stompCooldown)
            return;

        if (!isEnter && playerRb.linearVelocity.y > minDownSpeed)
            return;

        IStompable stompable = other.GetComponentInParent<IStompable>();
        if (stompable == null)
            return;

        Transform enemyRoot = (stompable as MonoBehaviour).transform;
        if (transform.position.y < enemyRoot.position.y)
            return;

        if (!isEnter && isStandingOnEnemy && stompable == currentEnemyUnderfoot)
        {
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
        if (playStompSfx && sfxAttack != null)
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

    public bool IsStandingOnEnemy()
    {
        return isStandingOnEnemy;
    }
}