using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class WhitchettyAI : MonoBehaviour
{
    public enum State
    {
        Follow,
        MoveToFood,
        Eating,
    }

    [Header("References")]
    [SerializeField]
    private Transform player;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private Sprite walkSprite;

    [SerializeField]
    private Sprite eatSprite;

    [Header("Follow")]
    [SerializeField]
    private float followSpeed = 3.2f;

    [SerializeField]
    private float followDistance = 1.2f;

    [SerializeField]
    private float catchupDistance = 3.0f;

    [SerializeField]
    private float arriveStop = 0.15f;

    [Header("Food Seeking")]
    [Tooltip("Max radius to search for food by tag")]
    [SerializeField]
    private float detectRadius = 3.5f;

    [Tooltip("Start eating when distance between colliders is below this value")]
    [SerializeField]
    private float eatRangeFromEdges = 0.35f;

    [SerializeField]
    private string edibleTag = "Edible";

    [Tooltip("Used only if food has no Edible component")]
    [SerializeField]
    private float eatDuration = 1.0f;

    [Header("Visuals")]
    [SerializeField]
    private bool flipSprite = true;

    [SerializeField]
    private bool faceTargetWhenEating = true;

    private Rigidbody2D rb;
    private Collider2D selfCol;
    private State state = State.Follow;
    private Transform currentFood;
    private bool isEating;
    private Coroutine eatRoutineHandle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        selfCol = GetComponent<Collider2D>();
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p)
                player = p.transform;
        }
        SetWalkVisual();
    }

    private void Update()
    {
        if (!player)
            return;

        if (!isEating)
        {
            if (!currentFood)
                currentFood = FindNearestFood();
            state = currentFood ? State.MoveToFood : State.Follow;
        }

        if (flipSprite && spriteRenderer && rb != null)
        {
            if (rb.linearVelocity.x > 0.02f)
                spriteRenderer.flipX = false;
            else if (rb.linearVelocity.x < -0.02f)
                spriteRenderer.flipX = true;
        }
    }

    private void FixedUpdate()
    {
        if (!player)
            return;

        switch (state)
        {
            case State.Follow:
                FollowPlayer();
                break;

            case State.MoveToFood:
                MoveToFood();
                break;

            case State.Eating:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void FollowPlayer()
    {
        Vector2 to = (Vector2)(player.position - transform.position);
        float dist = to.magnitude;

        if (dist > followDistance)
        {
            float speed = dist > catchupDistance ? followSpeed * 1.6f : followSpeed;
            rb.linearVelocity = to.normalized * speed;
            if (dist < arriveStop)
                rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (!isEating)
            SetWalkVisual();
    }

    private void MoveToFood()
    {
        if (!currentFood)
        {
            state = State.Follow;
            return;
        }

        // Move toward food
        Vector2 to = (Vector2)(currentFood.position - transform.position);
        float distCenter = to.magnitude;

        // Edge-to-edge distance using colliders (more reliable near contact)
        float edgeDistance = distCenter;
        var foodCol = currentFood.GetComponent<Collider2D>();
        if (selfCol != null && foodCol != null)
        {
            ColliderDistance2D cd = selfCol.Distance(foodCol);
            // If overlapped, cd.distance is negative; treat as 0
            edgeDistance = Mathf.Max(0f, cd.distance);
        }

        if (edgeDistance > eatRangeFromEdges)
        {
            rb.linearVelocity = to.normalized * followSpeed;
            SetWalkVisual();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;

            // Guard against multiple coroutine starts
            if (!isEating && eatRoutineHandle == null)
            {
                eatRoutineHandle = StartCoroutine(EatRoutine());
            }
        }
    }

    private IEnumerator EatRoutine()
    {
        if (!currentFood)
        {
            eatRoutineHandle = null;
            state = State.Follow;
            yield break;
        }

        state = State.Eating;
        isEating = true;

        if (faceTargetWhenEating && spriteRenderer)
            spriteRenderer.flipX = (currentFood.position.x < transform.position.x);
        SetEatVisual();

        var edible = currentFood.GetComponent<Edible>();

        if (edible != null)
        {
            // Assuming Edible.ConsumeAndDestroy() is an IEnumerator in your project
            yield return StartCoroutine(edible.ConsumeAndDestroy());
        }
        else
        {
            // Fallback: simple shrink + destroy over eatDuration
            float t = 0f;
            Vector3 start = currentFood.localScale;
            while (t < eatDuration && currentFood)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / eatDuration);
                currentFood.localScale = Vector3.Lerp(start, Vector3.zero, k);
                yield return null;
            }
            if (currentFood)
                Destroy(currentFood.gameObject);
        }

        currentFood = null;
        isEating = false;
        state = State.Follow;
        SetWalkVisual();
        eatRoutineHandle = null;
    }

    private Transform FindNearestFood()
    {
        GameObject[] foods = GameObject.FindGameObjectsWithTag(edibleTag);
        Transform best = null;
        float bestSqr = detectRadius * detectRadius;

        for (int i = 0; i < foods.Length; i++)
        {
            float sqr = (foods[i].transform.position - transform.position).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = foods[i].transform;
            }
        }
        return best;
    }

    private void SetWalkVisual()
    {
        if (spriteRenderer && walkSprite)
            spriteRenderer.sprite = walkSprite;
    }

    private void SetEatVisual()
    {
        if (spriteRenderer && eatSprite)
            spriteRenderer.sprite = eatSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}


// using System.Collections;
// using UnityEngine;

// public class WitchettyAI : MonoBehaviour
// {
//     public enum State
//     {
//         Follow,
//         MoveToFood,
//         Eating,
//     }

//     [Header("Targets")]
//     [SerializeField]
//     private Transform player;

//     [SerializeField]
//     private string edibleTag = "Edible";

//     [Header("Movement")]
//     [SerializeField]
//     private float moveSpeed = 2.2f;

//     [SerializeField]
//     private float jumpForce = 9f;

//     [Tooltip("Layers considered solid ground (tilemap, platforms, etc.)")]
//     [SerializeField]
//     private LayerMask groundMask;

//     [Header("Ground Probe")]
//     [Tooltip("Ground check box width under the feet")]
//     [SerializeField]
//     private float groundProbeWidth = 0.45f;

//     [Tooltip("Ground check box height under the feet")]
//     [SerializeField]
//     private float groundProbeHeight = 0.12f;

//     [Tooltip("Grace time after leaving ground where jump is still allowed")]
//     [SerializeField]
//     private float coyoteTime = 0.12f;

//     [Header("Jump Detection")]
//     [Tooltip("Horizontal ray to detect a wall in front")]
//     [SerializeField]
//     private float wallCheckDistance = 1.2f;

//     [Tooltip("Downward ray distance to detect a gap ahead")]
//     [SerializeField]
//     private float gapCheckDistance = 1.6f;

//     [Tooltip("How much higher the target must be to trigger a jump")]
//     [SerializeField]
//     private float ledgeStepHeight = 0.9f;

//     [Tooltip("Minimum delay between jumps")]
//     [SerializeField]
//     private float jumpCooldown = 0.25f;

//     [Header("Eating")]
//     [SerializeField]
//     private float detectionRadius = 1.8f;

//     [SerializeField]
//     private float eatDistance = 0.6f;

//     [SerializeField]
//     private float eatDuration = 1.2f;

//     [SerializeField]
//     private LayerMask edibleMask;

//     [Header("Sprites / Animator")]
//     [SerializeField]
//     private Sprite walkSprite;

//     [SerializeField]
//     private Sprite eatSprite;

//     [SerializeField]
//     private bool useAnimator = false;

//     private Rigidbody2D rb;
//     private SpriteRenderer sr;
//     private State state = State.Follow;
//     private Transform currentEdible;
//     private bool facingRight = true;
//     private bool isGrounded = false;
//     private float lastGroundedTime = -999f;
//     private float lastJumpTime = -999f;

//     private void Start()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         sr = GetComponent<SpriteRenderer>();

//         if (!player)
//         {
//             GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
//             if (playerObj)
//                 player = playerObj.transform;
//         }

//         if (!player)
//         {
//             Debug.LogError(
//                 "WITCHETTY ERROR: No player assigned. Drag your player into the Player field in the Inspector."
//             );
//             enabled = false;
//             return;
//         }

//         var myCol = GetComponent<Collider2D>();
//         var playerCol = player.GetComponent<Collider2D>();
//         if (myCol && playerCol)
//         {
//             Physics2D.IgnoreCollision(myCol, playerCol, true);
//         }

//         Debug.Log($"WitchettyAI initialized. Following: {player.name}");
//     }

//     private void Update()
//     {
//         if (!player)
//             return;

//         currentEdible = FindClosestEdible();

//         switch (state)
//         {
//             case State.Follow:
//                 if (currentEdible != null)
//                     state = State.MoveToFood;
//                 break;

//             case State.MoveToFood:
//                 if (currentEdible == null)
//                 {
//                     state = State.Follow;
//                 }
//                 else
//                 {
//                     float distToFood = Vector2.Distance(transform.position, currentEdible.position);
//                     if (distToFood <= eatDistance)
//                     {
//                         StartCoroutine(EatRoutine(currentEdible));
//                     }
//                 }
//                 break;

//             case State.Eating:
//                 break;
//         }

//         SetEatingVisual(state == State.Eating);
//     }

//     private void FixedUpdate()
//     {
//         CheckGround();

//         if (state == State.Eating)
//             return;

//         if (state == State.MoveToFood && currentEdible)
//         {
//             MoveToTargetFixed(currentEdible);
//         }
//         else
//         {
//             FollowPlayerFixed();
//         }
//     }

//     private void CheckGround()
//     {
//         Vector2 center = (Vector2)transform.position + new Vector2(0f, -0.1f);
//         Vector2 size = new Vector2(groundProbeWidth, groundProbeHeight);

//         RaycastHit2D hit = Physics2D.BoxCast(center, size, 0f, Vector2.down, 0.05f, groundMask);
//         isGrounded = hit.collider != null;

//         if (isGrounded)
//             lastGroundedTime = Time.time;
//     }

//     private void FollowPlayerFixed()
//     {
//         if (!player)
//             return;

//         float dx = player.position.x - transform.position.x;

//         if (Mathf.Abs(dx) < 0.8f)
//         {
//             rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
//             return;
//         }

//         MoveToTargetFixed(player);
//     }

//     private void MoveToTargetFixed(Transform target)
//     {
//         if (!target)
//             return;

//         float direction = Mathf.Sign(target.position.x - transform.position.x);

//         if (direction > 0 && !facingRight)
//             Flip();
//         else if (direction < 0 && facingRight)
//             Flip();

//         rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

//         TryJump(target, direction);
//     }

//     private void TryJump(Transform target, float direction)
//     {
//         bool withinCoyote = (Time.time - lastGroundedTime) <= coyoteTime;
//         bool offCooldown = (Time.time - lastJumpTime) >= jumpCooldown;
//         if (!(withinCoyote && offCooldown))
//             return;

//         Vector2 wallOrigin = (Vector2)transform.position + Vector2.up * 0.3f;
//         bool wallAhead = Physics2D.Raycast(
//             wallOrigin,
//             Vector2.right * direction,
//             wallCheckDistance,
//             groundMask
//         );

//         Vector2 gapOrigin = (Vector2)transform.position + Vector2.right * (direction * 0.6f);
//         bool groundAhead = Physics2D.Raycast(gapOrigin, Vector2.down, gapCheckDistance, groundMask);

//         bool targetHigher = target.position.y > transform.position.y + ledgeStepHeight;

//         bool shouldJump = wallAhead || !groundAhead || targetHigher;

//         if (shouldJump)
//         {
//             rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
//             rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
//             lastJumpTime = Time.time;
//         }
//     }

//     private IEnumerator EatRoutine(Transform edible)
//     {
//         state = State.Eating;

//         rb.linearVelocity = new Vector2(0f, 0f);

//         if (useAnimator == false && sr && eatSprite)
//             sr.sprite = eatSprite;
//         if (useAnimator)
//         {
//             var anim = GetComponent<Animator>();
//             if (anim)
//                 anim.SetBool("IsEating", true);
//         }

//         yield return new WaitForSeconds(eatDuration);

//         if (edible)
//         {
//             var edibleComp = edible.GetComponent<Edible>();
//             if (edibleComp != null)
//             {
//                 edibleComp.ConsumeAndDestroy();
//             }
//             else if (edible.CompareTag(edibleTag))
//             {
//                 Destroy(edible.gameObject);
//             }
//         }

//         currentEdible = FindClosestEdible();
//         state = (currentEdible != null) ? State.MoveToFood : State.Follow;

//         if (useAnimator == false && sr && walkSprite)
//             sr.sprite = walkSprite;
//         if (useAnimator)
//         {
//             var anim = GetComponent<Animator>();
//             if (anim)
//                 anim.SetBool("IsEating", false);
//         }
//     }

//     private Transform FindClosestEdible()
//     {
//         Transform best = null;
//         float bestDist = float.MaxValue;

//         if (edibleMask != 0)
//         {
//             Collider2D[] hits = Physics2D.OverlapCircleAll(
//                 transform.position,
//                 detectionRadius,
//                 edibleMask
//             );
//             foreach (var h in hits)
//             {
//                 float d = Vector2.Distance(transform.position, h.transform.position);
//                 if (d < bestDist)
//                 {
//                     best = h.transform;
//                     bestDist = d;
//                 }
//             }
//         }

//         if (!best)
//         {
//             GameObject[] tagged = GameObject.FindGameObjectsWithTag(edibleTag);
//             foreach (var go in tagged)
//             {
//                 float d = Vector2.Distance(transform.position, go.transform.position);
//                 if (d < detectionRadius && d < bestDist)
//                 {
//                     best = go.transform;
//                     bestDist = d;
//                 }
//             }
//         }

//         return best;
//     }

//     private void SetEatingVisual(bool eating)
//     {
//         if (useAnimator)
//         {
//             var anim = GetComponent<Animator>();
//             if (anim)
//                 anim.SetBool("IsEating", eating);
//             return;
//         }

//         if (sr && walkSprite && eatSprite)
//         {
//             sr.sprite = eating ? eatSprite : walkSprite;
//         }
//     }

//     private void Flip()
//     {
//         facingRight = !facingRight;
//         Vector3 s = transform.localScale;
//         s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
//         transform.localScale = s;
//     }

//     private void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(transform.position, detectionRadius);

//         Gizmos.color = Color.green;
//         Gizmos.DrawWireSphere(transform.position, eatDistance);

//         Gizmos.color = Color.red;
//         Vector2 center = (Vector2)transform.position + new Vector2(0f, -0.1f);
//         Vector2 size = new Vector2(groundProbeWidth, groundProbeHeight);
//         Gizmos.DrawWireCube(center, size);

//         float dir = facingRight ? 1f : -1f;

//         Gizmos.color = Color.blue;
//         Vector3 wallCheckOrigin = transform.position + Vector3.up * 0.3f;
//         Gizmos.DrawLine(
//             wallCheckOrigin,
//             wallCheckOrigin + Vector3.right * (dir * wallCheckDistance)
//         );

//         Gizmos.color = Color.magenta;
//         Vector3 gapCheckOrigin = transform.position + Vector3.right * (dir * 0.6f);
//         Gizmos.DrawLine(gapCheckOrigin, gapCheckOrigin + Vector3.down * gapCheckDistance);
//     }
// }


// // using System.Collections;
// // using UnityEngine;

// // public class WitchettyAI : MonoBehaviour
// // {
// //     public enum State
// //     {
// //         Follow,
// //         MoveToFood,
// //         Eating,
// //     }

// //     [Header("Targets")]
// //     [SerializeField]
// //     private Transform player;

// //     [SerializeField]
// //     private string edibleTag = "Edible";

// //     [Header("Movement")]
// //     [SerializeField]
// //     private float moveSpeed = 2.2f;

// //     [SerializeField]
// //     private float jumpCheckDistance = 0.6f;

// //     [SerializeField]
// //     private LayerMask groundMask;

// //     [Header("Eating")]
// //     [SerializeField]
// //     private float detectionRadius = .5f;

// //     [SerializeField]
// //     private float eatDistance = 0.6f;

// //     [SerializeField]
// //     private float eatDuration = 1.2f;

// //     [SerializeField]
// //     private LayerMask edibleMask;

// //     [Header("Sprites / Animator")]
// //     [SerializeField]
// //     private Sprite walkSprite;

// //     [SerializeField]
// //     private Sprite eatSprite;

// //     [SerializeField]
// //     private bool useAnimator = false;

// //     private Rigidbody2D rb;
// //     private SpriteRenderer sr;
// //     private State state = State.Follow;
// //     private Transform currentEdible;
// //     private bool facingRight = true;

// //     private void Awake()
// //     {
// //         rb = GetComponent<Rigidbody2D>();
// //         sr = GetComponent<SpriteRenderer>();

// //         // Try multiple ways to find the player
// //         if (!player)
// //         {
// //             // Method 1: Find by Player tag
// //             GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
// //             if (playerObj)
// //             {
// //                 player = playerObj.transform;
// //                 Debug.Log("WitchettyAI: Found player by tag");
// //             }
// //             else
// //             {
// //                 // Method 2: Find by name (common names)
// //                 string[] possibleNames = { "Player", "Hero", "Character", "player", "hero" };
// //                 foreach (string name in possibleNames)
// //                 {
// //                     playerObj = GameObject.Find(name);
// //                     if (playerObj)
// //                     {
// //                         player = playerObj.transform;
// //                         Debug.Log($"WitchettyAI: Found player by name: {name}");
// //                         break;
// //                     }
// //                 }
// //             }

// //             // Method 3: Find by component type (if your player has a specific script)
// //             if (!player)
// //             {
// //                 // Replace "CharacterController2D" with your actual player script name
// //                 var playerScript = FindAnyObjectByType<PlayerController2D>(); // Updated to new API
// //                 if (playerScript)
// //                 {
// //                     player = playerScript.transform;
// //                     Debug.Log("WitchettyAI: Found player by component");
// //                 }
// //             }
// //         }

// //         if (!player)
// //         {
// //             Debug.LogError(
// //                 "WitchettyAI: PLAYER NOT FOUND! Please assign the player in the Inspector or tag your player as 'Player'"
// //             );
// //         }
// //         else
// //         {
// //             Debug.Log($"WitchettyAI: Player found - {player.name}");
// //         }

// //         // Ignore collision with player
// //         var myCol = GetComponent<Collider2D>();
// //         var playerCol = player ? player.GetComponent<Collider2D>() : null;
// //         if (myCol && playerCol)
// //         {
// //             Physics2D.IgnoreCollision(myCol, playerCol, true);
// //         }
// //     }

// //     private void Update()
// //     {
// //         // Emergency player finding if still null
// //         if (!player)
// //         {
// //             GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
// //             if (playerObj)
// //             {
// //                 player = playerObj.transform;
// //                 Debug.LogWarning("WitchettyAI: Had to find player in Update!");
// //             }
// //             else
// //             {
// //                 Debug.LogError("WitchettyAI: No player to follow!");
// //                 rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Stop moving
// //                 return; // Exit update completely
// //             }
// //         }

// //         // Always scan for food
// //         currentEdible = FindClosestEdible();

// //         // State transitions
// //         switch (state)
// //         {
// //             case State.Follow:
// //                 if (currentEdible != null)
// //                 {
// //                     state = State.MoveToFood;
// //                     Debug.Log("WitchettyAI: Found food, switching to MoveToFood state");
// //                 }
// //                 else
// //                 {
// //                     MoveTowards(player);
// //                 }
// //                 break;

// //             case State.MoveToFood:
// //                 if (currentEdible == null)
// //                 {
// //                     state = State.Follow;
// //                     Debug.Log("WitchettyAI: Food gone, back to following player");
// //                 }
// //                 else if (
// //                     Vector2.Distance(transform.position, currentEdible.position) <= eatDistance
// //                 )
// //                 {
// //                     StartCoroutine(EatRoutine(currentEdible));
// //                 }
// //                 else
// //                 {
// //                     MoveTowards(currentEdible);
// //                 }
// //                 break;

// //             case State.Eating:
// //                 // Handled by coroutine
// //                 break;
// //         }

// //         // Update visual based on state
// //         SetEatingVisual(state == State.Eating);
// //     }

// //     private void MoveTowards(Transform target)
// //     {
// //         if (!target)
// //         {
// //             Debug.LogWarning("WitchettyAI: MoveTowards called with null target!");
// //             rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
// //             return;
// //         }

// //         Vector2 dir = (target.position - transform.position);
// //         dir.y = 0f; // Only horizontal movement

// //         // Don't move if we're very close to the player (optional)
// //         if (target == player && Mathf.Abs(dir.x) < 0.5f)
// //         {
// //             rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
// //             return;
// //         }

// //         // Flip sprite based on direction
// //         if (dir.x > 0 && !facingRight)
// //             Flip();
// //         if (dir.x < 0 && facingRight)
// //             Flip();

// //         // Move horizontally
// //         rb.linearVelocity = new Vector2(Mathf.Sign(dir.x) * moveSpeed, rb.linearVelocity.y);

// //         // Check ground ahead to avoid falling
// //         bool isGroundedAhead = Physics2D.Raycast(
// //             transform.position + new Vector3(Mathf.Sign(dir.x) * 0.3f, 0, 0),
// //             Vector2.down,
// //             jumpCheckDistance,
// //             groundMask
// //         );

// //         if (!isGroundedAhead)
// //         {
// //             rb.linearVelocity = new Vector2(
// //                 rb.linearVelocity.x,
// //                 Mathf.Max(rb.linearVelocity.y, -1.0f)
// //             );
// //         }
// //     }

// //     private IEnumerator EatRoutine(Transform edible)
// //     {
// //         state = State.Eating;
// //         Debug.Log("WitchettyAI: Eating!");

// //         // Stop movement
// //         rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

// //         // Wait for eating duration
// //         yield return new WaitForSeconds(eatDuration);

// //         // Consume the edible
// //         if (edible)
// //         {
// //             var edibleComp = edible.GetComponent<Edible>();
// //             if (edibleComp != null)
// //             {
// //                 edibleComp.ConsumeAndDestroy();
// //             }
// //             else if (edible.CompareTag(edibleTag))
// //             {
// //                 Destroy(edible.gameObject);
// //             }
// //         }

// //         // Check if there's more food nearby, otherwise follow player
// //         currentEdible = FindClosestEdible();
// //         state = (currentEdible != null) ? State.MoveToFood : State.Follow;
// //     }

// //     private Transform FindClosestEdible()
// //     {
// //         Transform best = null;
// //         float bestDist = float.MaxValue;

// //         // First try layer-based search
// //         if (edibleMask != 0)
// //         {
// //             Collider2D[] hits = Physics2D.OverlapCircleAll(
// //                 transform.position,
// //                 detectionRadius,
// //                 edibleMask
// //             );

// //             foreach (var h in hits)
// //             {
// //                 float d = Vector2.Distance(transform.position, h.transform.position);
// //                 if (d < bestDist)
// //                 {
// //                     best = h.transform;
// //                     bestDist = d;
// //                 }
// //             }
// //         }

// //         // Also check by tag if no layer results
// //         if (!best)
// //         {
// //             GameObject[] tagged = GameObject.FindGameObjectsWithTag(edibleTag);
// //             foreach (var go in tagged)
// //             {
// //                 float d = Vector2.Distance(transform.position, go.transform.position);
// //                 if (d < detectionRadius && d < bestDist)
// //                 {
// //                     best = go.transform;
// //                     bestDist = d;
// //                 }
// //             }
// //         }

// //         return best;
// //     }

// //     private void SetEatingVisual(bool eating)
// //     {
// //         if (useAnimator)
// //         {
// //             var anim = GetComponent<Animator>();
// //             if (anim)
// //                 anim.SetBool("IsEating", eating);
// //         }
// //         else if (sr && walkSprite && eatSprite)
// //         {
// //             sr.sprite = eating ? eatSprite : walkSprite;
// //         }
// //     }

// //     private void Flip()
// //     {
// //         facingRight = !facingRight;
// //         Vector3 s = transform.localScale;
// //         s.x *= -1f;
// //         transform.localScale = s;
// //     }

// //     private void OnDrawGizmosSelected()
// //     {
// //         Gizmos.color = Color.yellow;
// //         Gizmos.DrawWireSphere(transform.position, detectionRadius);

// //         Gizmos.color = Color.green;
// //         Gizmos.DrawWireSphere(transform.position, eatDistance);
// //     }
// // }
