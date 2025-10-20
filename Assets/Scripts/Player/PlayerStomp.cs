using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PlayerStomp : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private Collider2D playerHurtCollider;

    [Header("Stomp Tuning")]
    [SerializeField] private float minDownSpeed = -1.0f;
    [SerializeField] private float bounceVelocity = 9.0f;
    [SerializeField] private float postStompInvuln = 0.25f;

    private void Reset()
    {
        var rb = GetComponentInParent<Rigidbody2D>();
        if (rb) playerRb = rb;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHandleStomp(other);
    private void OnTriggerStay2D(Collider2D other)  => TryHandleStomp(other);

    private void TryHandleStomp(Collider2D other)
    {
        if (!playerRb) return;

        if (playerRb.linearVelocity.y > minDownSpeed) return;

        IStompable stompable = other.GetComponentInParent<IStompable>();
        if (stompable == null) return;

        Transform enemyRoot = (stompable as MonoBehaviour).transform;
        if (transform.position.y < enemyRoot.position.y) return;

        Vector2 v = playerRb.linearVelocity;
        v.y = bounceVelocity;
        playerRb.linearVelocity = v;

        if (playerHurtCollider != null)
        {
            Collider2D enemyCol = other;
            StartCoroutine(TempIgnoreCollision(playerHurtCollider, enemyCol, postStompInvuln));
        }

        stompable.TakeStomp(playerRb.gameObject);
    }

    private IEnumerator TempIgnoreCollision(Collider2D a, Collider2D b, float seconds)
    {
        if (a && b)
        {
            Physics2D.IgnoreCollision(a, b, true);
            yield return new WaitForSeconds(seconds);
            if (a && b) Physics2D.IgnoreCollision(a, b, false);
        }
    }
}
