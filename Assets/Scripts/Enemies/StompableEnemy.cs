using System.Collections;
using UnityEngine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class StompableEnemy : MonoBehaviour, IStompable
{
    [Header("Health")]
    [Tooltip("How many stomps are needed to kill this enemy")]
    [SerializeField]
    private int stompsToKill = 2;

    [Header("Death")]
    [Tooltip("Fade+shrink duration on death")]
    [SerializeField]
    private float deathDuration = 0.45f;

    [Tooltip("Prefab to drop on death (must have Tag=Edible and a Collider2D)")]
    [SerializeField]
    private GameObject rockEdiblePrefab;

    [Tooltip("Optional offset for spawn position")]
    [SerializeField]
    private Vector2 dropOffset = new Vector2(0f, -0.05f);

    private int stompCount = 0;
    private bool isDying = false;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TakeStomp(GameObject stomper)
    {
        if (isDying)
            return;

        stompCount++;
        if (stompCount >= stompsToKill)
        {
            StartCoroutine(DeathSequence());
        }
        else
        {
            // Optional small flinch feedback on first stomp
            // You can add particles/sound here if you want.
        }
    }

    private IEnumerator DeathSequence()
    {
        isDying = true;

        float t = 0f;
        Color start = spriteRenderer ? spriteRenderer.color : Color.white;
        Vector3 startScale = transform.localScale;

        while (t < deathDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / deathDuration);

            if (spriteRenderer)
            {
                Color c = start;
                c.a = Mathf.Lerp(1f, 0f, k);
                spriteRenderer.color = c;
            }

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, k);
            yield return null;
        }

        if (rockEdiblePrefab)
        {
            Vector3 pos = transform.position + (Vector3)dropOffset;
            GameObject drop = Instantiate(rockEdiblePrefab, pos, Quaternion.identity);
            // Make sure the prefab has Tag=Edible and a Collider2D.
        }

        Destroy(gameObject);
    }
}

internal interface IStompable { }
