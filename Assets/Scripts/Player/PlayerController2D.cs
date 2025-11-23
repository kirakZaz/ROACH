using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Move & Jump")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    public KeyCode jumpKey = KeyCode.W;

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

    [Header("Animation")]
    [SerializeField]
    private Sprite[] idleSprites;
    [SerializeField]
    private float idleAnimSpeed = 0.2f;

    [SerializeField]
    private Sprite[] walkSprites;
    [SerializeField]
    private float walkAnimSpeed = 0.15f;

    [SerializeField]
    private Sprite[] climbSprites;
    [SerializeField]
    private float climbAnimSpeed = 0.2f;

    [Header("Level Fail")]
    public float fallThreshold = -10f;

    [Header("Background Sound")]
    public AudioClip sfxBackground;

    [Range(0f, 1f)]
    public float backgroundVolume = 1f;
    public bool persistBackgroundAcrossScenes = true;

    [Header("Footstep Audio")]
    public AudioClip sfxFootstep;

    [Range(0f, 1f)]
    public float footstepVolume = 0.6f;
    public float footstepInterval = 0.35f;
    private float footstepTimer;

    [Header("Enemy Stomp")]
    public PlayerStomp playerStomp;

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
    private bool isClimbing;

    private Vector2 boxDefaultSize, boxDefaultOffset;
    private Vector2 capsuleDefaultSize, capsuleDefaultOffset;
    private bool hasBox, hasCapsule;
    private bool wasGrounded;

    // Animation
    private int currentFrame = 0;
    private float animTimer = 0f;

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

        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;
        audioSrc.loop = false;
        audioSrc.volume = 1f;

        TryStartBackgroundAudio();
    }

    private void Start()
    {
        StartCoroutine(AnimationLoop());
    }

    private void TryStartBackgroundAudio()
    {
        if (sfxBackground == null)
            return;

        if (bgSource != null)
        {
            bgSource.volume = backgroundVolume;
            if (!bgSource.isPlaying)
                bgSource.Play();
            return;
        }

        GameObject go = new GameObject("BackgroundAudio");
        var src = go.AddComponent<AudioSource>();
        src.clip = sfxBackground;
        src.loop = true;
        src.volume = backgroundVolume;
        src.spatialBlend = 0f;
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
                Debug.LogWarning("Create a 'Ground' layer and assign it to groundLayer.");
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
        if (!groundCheck || !wallCheckLeft || !wallCheckRight || !ceilingCheck)
            EnsureChecksExist();

        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        jumpHeld = Input.GetKey(jumpKey) || Input.GetKey(KeyCode.Space);
        bool jumpPressed = Input.GetKeyDown(jumpKey) || Input.GetKeyDown(KeyCode.Space);

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

        // Jump - allow jumping on ground OR on enemy
        bool canJump = isGrounded || (playerStomp != null && playerStomp.IsStandingOnEnemy());
        if (jumpPressed && canJump && !isCrouching)
        {
            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        if (flipSpriteOnMove && sr)
        {
            if (horizontal > 0.01f)
                sr.flipX = false;
            else if (horizontal < -0.01f)
                sr.flipX = true;
        }

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

        if (transform.position.y < fallThreshold)
            StartCoroutine(ReloadAfterFail());

        wasGrounded = isGrounded;
        HandleFootsteps();

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
            isClimbing = true;
            rb.gravityScale = 0f;
            float wallDirection = touchingLeftWall ? -1f : 0.5f;
            float horizontalSpeed = wallDirection * moveSpeed * 0.3f;
            rb.linearVelocity = new Vector2(horizontalSpeed, vertical * climbSpeed);
        }
        else
        {
            isClimbing = false;
            rb.gravityScale = 1f;
            float speed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
            Vector2 v = rb.linearVelocity;
            v.x = horizontal * speed;
            rb.linearVelocity = v;

            if (rb.linearVelocity.y < -0.01f)
                rb.gravityScale = fallGravityMultiplier;
            else if (rb.linearVelocity.y > 0.01f && !jumpHeld)
                rb.gravityScale = lowJumpMultiplier;
        }
    }

    private IEnumerator AnimationLoop()
    {
        while (true)
        {
            // Determine animation state
            Sprite[] currentAnim;
            float currentSpeed;

            if (isClimbing && climbSprites != null && climbSprites.Length > 0)
            {
                // Climbing
                currentAnim = climbSprites;
                currentSpeed = climbAnimSpeed;
            }
            else if (isGrounded && Mathf.Abs(horizontal) > 0.1f && walkSprites != null && walkSprites.Length > 0)
            {
                // Walking
                currentAnim = walkSprites;
                currentSpeed = walkAnimSpeed;
            }
            else if (idleSprites != null && idleSprites.Length > 0)
            {
                // Idle
                currentAnim = idleSprites;
                currentSpeed = idleAnimSpeed;
            }
            else
            {
                yield return null;
                continue;
            }

            animTimer += Time.deltaTime;

            if (animTimer >= currentSpeed)
            {
                animTimer = 0f;
                currentFrame = (currentFrame + 1) % currentAnim.Length;
                sr.sprite = currentAnim[currentFrame];
            }

            yield return null;
        }
    }

    private void HandleFootsteps()
    {
        if (!audioSrc || sfxFootstep == null)
            return;

        bool isMoving = Mathf.Abs(horizontal) > 0.1f;
        if (isGrounded && isMoving)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                float oldPitch = audioSrc.pitch;
                audioSrc.pitch = Random.Range(0.97f, 1.03f);
                audioSrc.PlayOneShot(sfxFootstep, footstepVolume);
                audioSrc.pitch = oldPitch;
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

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

    private IEnumerator ReloadAfterFail()
    {
        if (!enabled)
            yield break;
        enabled = false;
        yield return new WaitForSeconds(0.25f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

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