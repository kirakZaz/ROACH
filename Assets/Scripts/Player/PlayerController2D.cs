using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Move & Jump")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    public KeyCode jumpKey = KeyCode.W; // Space also works

    [Header("Variable Jump Height")]
    public float fallGravityMultiplier = 1.8f;
    public float lowJumpMultiplier = 2.0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.25f;
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

    // ----------------- AUDIO: background + stomp -----------------
    [Header("Background Sound")]
    public AudioClip sfxBackground; // ← assign Toxic_Crunch.wav here

    [Range(0f, 1f)]
    public float backgroundVolume = 1f;
    public bool persistBackgroundAcrossScenes = true;

    [Header("Audio (Stomp Only)")]
    public AudioClip sfxAttack; // Assign Player_Attack(.wav or _Short)
    public float stompBounceForce = 1f; // Upward bounce after a stomp
    public LayerMask enemyLayer; // Set to your enemy layer (optional if using tag)
    public bool requireDownwardVelocity = true; // Only count stomp if we were falling

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D bodyCol;
    private AudioSource audioSrc;

    private bool isGrounded;
    private bool touchingLeftWall;
    private bool touchingRightWall;
    private float horizontal;
    private float vertical;
    private float defaultGravity;
    private bool jumpHeld;
    private bool isCrouching;

    // crouch collider cache
    private Vector2 boxDefaultSize,
        boxDefaultOffset;
    private Vector2 capsuleDefaultSize,
        capsuleDefaultOffset;
    private bool hasBox,
        hasCapsule;

    // state
    private bool wasGrounded;

    // static cache so we don’t duplicate background on reloads
    private static AudioSource bgSource;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        bodyCol = GetComponent<Collider2D>();
        audioSrc = GetComponent<AudioSource>();

        defaultGravity = rb.gravityScale;

        EnsureChecksExist();
        EnsureGroundLayer();
        CacheColliderDefaults();

        if (ceilingLayer.value == 0)
            ceilingLayer = groundLayer;

        // Player SFX source setup
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f; // 2D
        audioSrc.loop = false;
        audioSrc.volume = 1f;

        // --- BACKGROUND AUDIO: create or reuse ---
        TryStartBackgroundAudio();
    }

    private void TryStartBackgroundAudio()
    {
        if (sfxBackground == null)
            return;

        // If we already have a bg source from a previous scene/reload, reuse it and just sync volume.
        if (bgSource != null)
        {
            bgSource.volume = backgroundVolume;
            if (!bgSource.isPlaying)
                bgSource.Play();
            return;
        }

        // Create a dedicated background audio object
        GameObject go = new GameObject("BackgroundAudio");
        var src = go.AddComponent<AudioSource>();
        src.clip = sfxBackground;
        src.loop = true;
        src.volume = backgroundVolume;
        src.spatialBlend = 0f; // 2D ambient
        src.playOnAwake = false;

        if (persistBackgroundAcrossScenes)
            DontDestroyOnLoad(go);

        src.Play();
        bgSource = src;
    }

    private void EnsureGroundLayer()
    {
        if (groundLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Ground");
            if (idx != -1)
                groundLayer = 1 << idx;
            else
                Debug.LogWarning(
                    "Create a 'Ground' layer and assign your platforms, then assign it to groundLayer."
                );
        }
    }

    private void CacheColliderDefaults()
    {
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
        // safety
        if (!groundCheck || !wallCheckLeft || !wallCheckRight || !ceilingCheck)
            EnsureChecksExist();

        // Input
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        jumpHeld = Input.GetKey(jumpKey) || Input.GetKey(KeyCode.Space);
        bool jumpPressed = Input.GetKeyDown(jumpKey) || Input.GetKeyDown(KeyCode.Space);

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
        if (jumpPressed && isGrounded && !isCrouching)
        {
            Vector2 v = rb.linearVelocity;
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
                StartCrouch();
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
            StartCoroutine(ReloadAfterFail());

        wasGrounded = isGrounded;

        // Keep background volume synced with inspector changes (optional)
        if (bgSource)
            bgSource.volume = backgroundVolume;
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
            Vector2 v = rb.linearVelocity;
            v.x = horizontal * speed;
            rb.linearVelocity = v;

            // variable jump height
            if (rb.linearVelocity.y < -0.01f)
                rb.gravityScale = fallGravityMultiplier;
            else if (rb.linearVelocity.y > 0.01f && !jumpHeld)
                rb.gravityScale = lowJumpMultiplier;
        }
    }

    // ----- STOMP DETECTION -----
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsEnemy(collision.collider))
            return;

        // We stomped if any contact normal points up into us (we're on top of them)
        bool hitFromAbove = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var n = collision.GetContact(i).normal;
            if (n.y > 0.5f)
            {
                hitFromAbove = true;
                break;
            }
        }

        if (!hitFromAbove)
            return;
        if (requireDownwardVelocity && rb.linearVelocity.y > 0f)
            return;

        // Stomp confirmed: play attack sfx + bounce
        PlayOneShot(sfxAttack, 1f);

        var v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
        rb.AddForce(Vector2.up * stompBounceForce, ForceMode2D.Impulse);

        // TODO: damage/kill enemy here if needed
        // var enemy = collision.collider.GetComponent<EnemyHealth>();
        // if (enemy) enemy.TakeDamage(1);
    }

    private bool IsEnemy(Collider2D col)
    {
        // by layer
        if (enemyLayer.value != 0 && ((1 << col.gameObject.layer) & enemyLayer) != 0)
            return true;

        // by tag (fallback)
        return col.CompareTag("Enemy");
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
            float newH = box.size.y * crouchHeightMultiplier;
            box.offset = new Vector2(box.offset.x, box.offset.y - (box.size.y - newH) * 0.5f);
            box.size = new Vector2(box.size.x, newH);
        }
        else if (cap)
        {
            float newH = cap.size.y * crouchHeightMultiplier;
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

    // ----- Audio helper -----
    private void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (!audioSrc || clip == null)
            return;
        // small natural pitch variance for variety
        float pitch = Random.Range(0.96f, 1.04f);
        float oldPitch = audioSrc.pitch;
        audioSrc.pitch = pitch;
        audioSrc.PlayOneShot(clip, volume);
        audioSrc.pitch = oldPitch;
    }

    // ----- Fail / reload -----
    private IEnumerator ReloadAfterFail()
    {
        if (!enabled)
            yield break;
        enabled = false;

        // optional: could fade background here if you want
        yield return new WaitForSeconds(0.25f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ----- Gizmos -----
    private void OnDrawGizmos()
    {
        if (groundCheck)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
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
