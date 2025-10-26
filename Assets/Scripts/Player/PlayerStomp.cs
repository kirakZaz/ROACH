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

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource; // ← assign Player's AudioSource or leave null to auto-find

    [SerializeField]
    private AudioClip sfxAttack; // ← assign Player_Attack.wav

    [SerializeField]
    [Range(0f, 1f)]
    private float sfxVolume = 1f;

    [SerializeField]
    private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [SerializeField]
    private float cutOffAfterSeconds = 0f; // 0 = don't cut; e.g. 0.25f to truncate a long clip

    private void Reset()
    {
        var rb = GetComponentInParent<Rigidbody2D>();
        if (rb)
            playerRb = rb;

        // try to auto-grab a sibling/parent AudioSource (Player)
        var src = GetComponentInParent<AudioSource>();
        if (src)
            audioSource = src;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHandleStomp(other);

    private void OnTriggerStay2D(Collider2D other) => TryHandleStomp(other);

    private void TryHandleStomp(Collider2D other)
    {
        if (!playerRb)
            return;

        // must be moving downward at least this fast
        if (playerRb.linearVelocity.y > minDownSpeed)
            return;

        // must hit something stompable
        IStompable stompable = other.GetComponentInParent<IStompable>();
        if (stompable == null)
            return;

        // ensure our stomp collider is above enemy root (top hit)
        Transform enemyRoot = (stompable as MonoBehaviour).transform;
        if (transform.position.y < enemyRoot.position.y)
            return;

        // ---- STOMP CONFIRMED ----

        // 1) bounce
        Vector2 v = playerRb.linearVelocity;
        v.y = bounceVelocity;
        playerRb.linearVelocity = v;

        // 2) TEMPORARY invulnerability vs enemy collider
        if (playerHurtCollider != null)
        {
            Collider2D enemyCol = other;
            StartCoroutine(TempIgnoreCollision(playerHurtCollider, enemyCol, postStompInvuln));
        }

        // 3) PLAY SOUND (this is the spot you asked about)
        if (sfxAttack != null)
        {
            if (!audioSource)
            {
                // last-chance: try to find a source at runtime
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

        // 4) notify enemy
        stompable.TakeStomp(playerRb.gameObject);
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
        // waits then stops the source (cuts off any currently playing one-shot tail)
        yield return new WaitForSeconds(seconds);
        if (src)
            src.Stop();
    }
}
