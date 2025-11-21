using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FreezeOnGround : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool landed = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // already landed? skip
        if (landed)
            return;

        // check layer or tag
        if (
            collision.collider.gameObject.layer == LayerMask.NameToLayer("Ground")
            || collision.collider.CompareTag("Ground")
        )
        {
            landed = true;

            // small correction upward so it doesn't sink in
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y + 0.02f,
                transform.position.z
            );

            // stop all motion
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // disable further physics completely
            rb.bodyType = RigidbodyType2D.Static;

            // optional: remove this component once done
            Destroy(this);
        }
    }
}
