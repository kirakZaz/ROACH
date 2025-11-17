using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FlyingEnemyAudio : MonoBehaviour
{
    [Header("Clip")]
    [SerializeField]
    private AudioClip flyLoop;

    [Range(0f, 1f)]
    [SerializeField]
    private float maxVolume = 0.8f;

    [SerializeField]
    private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Movement detection")]
    [SerializeField]
    private Rigidbody2D rb2d;

    [SerializeField]
    private float minMoveSpeed = 0.05f;

    [SerializeField]
    private bool muteWhenIdle = true;

    [Header("Proximity (to player)")]
    [SerializeField]
    private Transform player; // drag your player here

    [SerializeField]
    private float hearDistance = 10f; // max distance to hear enemy

    [SerializeField]
    private bool requireVisibleOnScreen = true; // only play when visible

    private AudioSource audioSource;
    private Vector3 lastPos;
    private bool isVisibleOnScreen;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f; // start muted

        if (flyLoop != null)
        {
            audioSource.clip = flyLoop;
            audioSource.Play();
        }

        if (!rb2d)
            rb2d = GetComponent<Rigidbody2D>();

        lastPos = transform.position;
    }

    private void Update()
    {
        // 1) detect movement (works for Rigidbody2D or transform movement)
        float speedRb = 0f;
        float speedTransform = 0f;

        if (rb2d)
        {
            speedRb = rb2d.linearVelocity.magnitude;
        }

        Vector3 currentPos = transform.position;
        speedTransform = (currentPos - lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = currentPos;

        float speed = Mathf.Max(speedRb, speedTransform);
        bool isMoving = speed >= minMoveSpeed;

        // base volume based on movement
        float targetVolume = 0f;
        if (isMoving)
        {
            targetVolume = maxVolume;
        }

        // 2) visibility gate
        if (requireVisibleOnScreen && !isVisibleOnScreen)
        {
            targetVolume = 0f;
        }

        // 3) distance-to-player fade
        if (player != null && targetVolume > 0f)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            float proximity = Mathf.Clamp01(1f - (dist / hearDistance)); // 1 close, 0 far
            targetVolume *= proximity;
        }

        // 4) apply volume (instant or smooth)
        if (muteWhenIdle)
        {
            audioSource.volume = targetVolume;
        }
        else
        {
            audioSource.volume = Mathf.MoveTowards(
                audioSource.volume,
                targetVolume,
                Time.deltaTime * 2f
            );
        }

        // 5) small pitch variation when moving
        if (isMoving && targetVolume > 0.01f)
        {
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        }
    }

    private void OnBecameVisible()
    {
        isVisibleOnScreen = true;
    }

    private void OnBecameInvisible()
    {
        isVisibleOnScreen = false;
    }
}
