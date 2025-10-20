using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Move & Jump")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Variable Jump Height")]
    public float fallGravityMultiplier = 1.8f;
    public float lowJumpMultiplier = 2.0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.16f;
    public LayerMask groundLayer;

    [Header("Wall Climb")]
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public Vector2 wallCheckSize = new Vector2(0.12f, 0.9f);
    public float climbSpeed = 4f;
    public float wallStickTolerance = 0.2f;

    [Header("Crouch")]
    public bool enableCrouch = true;
    public float crouchSpeedMultiplier = 0.5f;
    public Transform ceilingCheck;
    public float ceilingRadius = 0.12f;
    public LayerMask ceilingLayer;
    public float crouchHeightMultiplier = 0.6f;

    [Header("Visuals")]
    public bool flipSpriteOnMove = true;

    [Header("Level Fail")]
    public float fallThreshold = -10f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D bodyCol;

    private bool isGrounded,
        touchingLeftWall,
        touchingRightWall;
    private float horizontal,
        vertical;
    private float defaultGravity;
    private bool jumpHeld;
    private bool isCrouching;

    // collider cache for crouch
    private Vector2 boxDefaultSize,
        boxDefaultOffset;
    private Vector2 capsuleDefaultSize,
        capsuleDefaultOffset;
    private bool hasBox,
        hasCapsule;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        bodyCol = GetComponent<Collider2D>();
        defaultGravity = rb.gravityScale;

        EnsureChecksExist(); // create missing checks

        // cache collider data
        var box = GetComponent<BoxCollider2D>();
        var cap = GetComponent<CapsuleCollider2D>();
        if (box)
        {
            hasBox = true;
            boxDefaultSize = box.size;
            boxDefaultOffset = box.offset;
        }
        if (cap)
        {
            hasCapsule = true;
            capsuleDefaultSize = cap.size;
            capsuleDefaultOffset = cap.offset;
        }

        // fallback: if ceilingLayer not set, use groundLayer
        if (ceilingLayer.value == 0)
            ceilingLayer = groundLayer;
    }

    private void EnsureChecksExist()
    {
        if (!groundCheck)
            groundCheck = MakeChild("GroundCheck", new Vector3(0f, -0.6f, 0f));
        if (!wallCheckLeft)
            wallCheckLeft = MakeChild("WallCheckLeft", new Vector3(-0.5f, 0f, 0f));
        if (!wallCheckRight)
            wallCheckRight = MakeChild("WallCheckRight", new Vector3(0.5f, 0f, 0f));
        if (!ceilingCheck)
            ceilingCheck = MakeChild("CeilingCheck", new Vector3(0f, 0.7f, 0f));
    }

    private Transform MakeChild(string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    private void Update()
    {
        // Safety: if something got deleted at runtime, recreate
        if (!groundCheck || !wallCheckLeft || !wallCheckRight || !ceilingCheck)
        {
            Debug.LogWarning("PlayerController2D: missing check transforms; recreating.");
            EnsureChecksExist();
            if (!groundCheck || !wallCheckLeft || !wallCheckRight || !ceilingCheck)
                return;
        }

        // Input
        horizontal = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
        vertical = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);
        jumpHeld = Input.GetKey(KeyCode.W);

        // Checks
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

        // Jump
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && !isCrouching)
        {
            var v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // Flip
        if (flipSpriteOnMove && sr)
        {
            if (horizontal > 0.01f)
                sr.flipX = false;
            else if (horizontal < -0.01f)
                sr.flipX = true;
        }

        // Crouch
        if (enableCrouch)
        {
            if (Input.GetKey(KeyCode.S) && isGrounded)
            {
                StartCrouch();
            }
            else
            {
                bool blocked = Physics2D.OverlapCircle(
                    ceilingCheck.position,
                    ceilingRadius,
                    ceilingLayer
                );
                if (!blocked)
                    StopCrouch();
            }
        }

        // Fail restart
        if (transform.position.y < fallThreshold)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void FixedUpdate()
    {
        bool pushingLeft = horizontal < -wallStickTolerance;
        bool pushingRight = horizontal > wallStickTolerance;
        bool onWall =
            !isGrounded
            && ((touchingLeftWall && pushingLeft) || (touchingRightWall && pushingRight));

        if (onWall)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, vertical * climbSpeed);
        }
        else
        {
            rb.gravityScale = 1f;
            float speed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
            var v = rb.linearVelocity;
            v.x = horizontal * speed;
            rb.linearVelocity = v;

            // variable jump height
            if (rb.linearVelocity.y < -0.01f)
            {
                rb.gravityScale = fallGravityMultiplier;
            }
            else if (rb.linearVelocity.y > 0.01f && !jumpHeld)
            {
                rb.gravityScale = lowJumpMultiplier;
            }
        }
    }

    // ----- Crouch helpers -----
    private void StartCrouch()
    {
        if (isCrouching)
            return;
        isCrouching = true;

        var box = GetComponent<BoxCollider2D>();
        var cap = GetComponent<CapsuleCollider2D>();

        if (box)
        {
            var newH = box.size.y * crouchHeightMultiplier;
            box.offset = new Vector2(box.offset.x, box.offset.y - (box.size.y - newH) * 0.5f);
            box.size = new Vector2(box.size.x, newH);
        }
        else if (cap)
        {
            var newH = cap.size.y * crouchHeightMultiplier;
            cap.offset = new Vector2(cap.offset.x, cap.offset.y - (cap.size.y - newH) * 0.5f);
            cap.size = new Vector2(cap.size.x, newH);
        }
    }

    private void StopCrouch()
    {
        if (!isCrouching)
            return;
        isCrouching = false;

        var box = GetComponent<BoxCollider2D>();
        var cap = GetComponent<CapsuleCollider2D>();

        if (box)
        {
            box.size = boxDefaultSize == Vector2.zero ? box.size : boxDefaultSize;
            box.offset = boxDefaultOffset;
        }
        else if (cap)
        {
            cap.size = capsuleDefaultSize == Vector2.zero ? cap.size : capsuleDefaultSize;
            cap.offset = capsuleDefaultOffset;
        }
    }

    private void OnDrawGizmosSelected()
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
        if (ceilingCheck)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ceilingCheck.position, ceilingRadius);
        }
    }
}
