using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
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

    // ---------- AUDIO ----------
    [Header("Audio (Eating)")]
    [SerializeField]
    private AudioClip eatClip;

    [SerializeField, Range(0f, 1f)]
    private float eatVolume = 0.3f;

    [SerializeField]
    private bool playAsLoop = true;

    [SerializeField]
    private float biteInterval = 0.35f;

    [SerializeField]
    private Vector2 bitePitchRange = new Vector2(0.95f, 1.05f);

    // ---------- UI ----------


    [Header("UI / Inventory")]
    [SerializeField]
    private WichettyBagUI wichettyBagUI;

    [SerializeField]
    private WichettyItem defaultItem;

    [SerializeField]
    private int defaultFoodAmount = 1;

    private Rigidbody2D rb;
    private Collider2D selfCol;
    private State state = State.Follow;
    private Transform currentFood;
    private bool isEating;
    private Coroutine eatRoutineHandle;

    private AudioSource audioSrc;
    private Coroutine biteLoopCo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        selfCol = GetComponent<Collider2D>();
        audioSrc = GetComponent<AudioSource>();

        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p)
                player = p.transform;
        }

        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;
        audioSrc.loop = false;
        audioSrc.volume = 0.5f;

        SetWalkVisual();
    }

    private void OnDisable()
    {
        StopEatSound();
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

        Vector2 to = (Vector2)(currentFood.position - transform.position);
        float edgeDistance = to.magnitude;

        var foodCol = currentFood.GetComponent<Collider2D>();
        if (selfCol != null && foodCol != null)
        {
            ColliderDistance2D cd = selfCol.Distance(foodCol);
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

            if (!isEating && eatRoutineHandle == null)
                eatRoutineHandle = StartCoroutine(EatRoutine());
        }
    }

    private System.Collections.IEnumerator EatRoutine()
    {
        if (!currentFood)
        {
            eatRoutineHandle = null;
            state = State.Follow;
            yield break;
        }

        Debug.Log("[Pet] Starting to eat!");

        state = State.Eating;
        isEating = true;

        // --- detect what we're eating ---
        WichettyItem eatenItem = defaultItem;
        int eatenAmount = defaultFoodAmount;

        var foodRes = currentFood.GetComponent<WichettyFoodResource>();
        if (foodRes != null)
        {
            // Отмечаем что пет ест это!
            foodRes.IsBeingEaten = true;
            Debug.Log("[Pet] Marked resource as IsBeingEaten = true");
            
            if (foodRes.Item != null)
                eatenItem = foodRes.Item;
            eatenAmount = foodRes.Amount;
        }

        if (faceTargetWhenEating && spriteRenderer)
            spriteRenderer.flipX = (currentFood.position.x < transform.position.x);

        SetEatVisual();
        StartEatSound();

        // your old eat logic
        var edible = currentFood.GetComponent<Edible>();
        if (edible != null)
        {
            yield return StartCoroutine(edible.ConsumeAndDestroy());
        }
        else
        {
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

        // Добавляем в мешок
        if (wichettyBagUI != null && eatenItem != null)
        {
            wichettyBagUI.Add(eatenItem, eatenAmount);
            Debug.Log($"[Pet] +{eatenAmount} x {eatenItem.DisplayName}");
        }
        else
        {
            Debug.LogWarning("[EatRoutine] Could not add to bag!");
        }

        StopEatSound();

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

    // ---------- AUDIO HELPERS ----------
    private void StartEatSound()
    {
        if (!eatClip || !audioSrc)
            return;

        if (playAsLoop)
        {
            audioSrc.Stop();
            audioSrc.clip = eatClip;
            audioSrc.loop = true;
            audioSrc.volume = eatVolume;
            audioSrc.pitch = 1f;
            audioSrc.Play();
        }
        else
        {
            if (biteLoopCo != null)
                StopCoroutine(biteLoopCo);
            biteLoopCo = StartCoroutine(BiteLoop());
        }
    }

    private void StopEatSound()
    {
        if (!audioSrc)
            return;

        if (playAsLoop)
        {
            if (audioSrc.isPlaying && audioSrc.clip == eatClip)
                audioSrc.Stop();
            audioSrc.clip = null;
            audioSrc.loop = false;
        }
        else
        {
            if (biteLoopCo != null)
                StopCoroutine(biteLoopCo);
            biteLoopCo = null;
        }
    }

    private IEnumerator BiteLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.05f, biteInterval));
        while (isEating)
        {
            float old = audioSrc.pitch;
            audioSrc.pitch = Random.Range(bitePitchRange.x, bitePitchRange.y);
            audioSrc.PlayOneShot(eatClip, eatVolume);
            audioSrc.pitch = old;
            yield return wait;
        }
    }
}