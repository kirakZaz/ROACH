using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Move & Jump")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f; // W to jump (ground only)

    [Header("Ground Check")]
    public Transform groundCheck; // child below feet
    public float groundRadius = 0.16f; // small circle
    public LayerMask groundLayer; // set to Ground

    [Header("Wall Climb")]
    public Transform wallCheckLeft; // child at left side
    public Transform wallCheckRight; // child at right side
    public Vector2 wallCheckSize = new Vector2(0.12f, 0.9f); // thin box along body
    public float climbSpeed = 4f; // W/S while on wall
    public float wallStickTolerance = 0.2f; // must press into the wall this much

    [Header("Visuals")]
    public bool flipSpriteOnMove = true;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool isGrounded,
        touchingLeftWall,
        touchingRightWall;
    private float horizontal,
        vertical;
    private float defaultGravity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        defaultGravity = rb.gravityScale;

        // safety: auto-create checks if not assigned
        if (!groundCheck)
            groundCheck = MakeChild("GroundCheck", new Vector3(0f, -0.6f, 0f));
        if (!wallCheckLeft)
            wallCheckLeft = MakeChild("WallCheckLeft", new Vector3(-0.5f, 0f, 0f));
        if (!wallCheckRight)
            wallCheckRight = MakeChild("WallCheckRight", new Vector3(0.5f, 0f, 0f));
    }

    Transform MakeChild(string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    void Update()
    {
        // WASD
        horizontal = 0f;
        vertical = 0f;
        if (Input.GetKey(KeyCode.A))
            horizontal = -1f;
        if (Input.GetKey(KeyCode.D))
            horizontal = 1f;
        if (Input.GetKey(KeyCode.W))
            vertical = 1f;
        if (Input.GetKey(KeyCode.S))
            vertical = -1f;

        // ground & wall checks
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        touchingLeftWall = Physics2D.OverlapBox(
            (Vector2)wallCheckLeft.position,
            wallCheckSize,
            0f,
            groundLayer
        );
        touchingRightWall = Physics2D.OverlapBox(
            (Vector2)wallCheckRight.position,
            wallCheckSize,
            0f,
            groundLayer
        );

        // jump from ground on W press
        if (Input.GetKeyDown(KeyCode.W) && isGrounded)
        {
            var v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v; // consistent jump
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // flip sprite
        if (flipSpriteOnMove && sr != null)
        {
            if (horizontal > 0.01f)
                sr.flipX = false;
            else if (horizontal < -0.01f)
                sr.flipX = true;
        }
    }

    void FixedUpdate()
    {
        bool pushingLeft = horizontal < -wallStickTolerance;
        bool pushingRight = horizontal > wallStickTolerance;
        bool onWall =
            !isGrounded
            && ((touchingLeftWall && pushingLeft) || (touchingRightWall && pushingRight));

        if (onWall)
        {
            // stick & climb
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, vertical * climbSpeed);
        }
        else
        {
            // normal move
            rb.gravityScale = defaultGravity;
            var v = rb.linearVelocity;
            v.x = horizontal * moveSpeed;
            rb.linearVelocity = v;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
        if (wallCheckLeft)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(wallCheckLeft.position, wallCheckSize);
        }
        if (wallCheckRight)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(wallCheckRight.position, wallCheckSize);
        }
    }

    [Header("Level Fail Settings")]
    public float fallThreshold = -10f; // Y position below which the level ends

    void LateUpdate()
    {
        if (transform.position.y < fallThreshold)
        {
            LevelFailed();
        }
    }

    private void LevelFailed()
    {
        Debug.Log("💀 Player fell out of level — restarting...");
        // Simple restart:
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}
