using UnityEngine;

public class CrystalAnimator : MonoBehaviour
{
    [SerializeField]
    private Sprite[] frames;

    [SerializeField]
    private float frameRate = 0.1f; // How long each frame shows

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int currentFrame;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (frames.Length == 0)
            Debug.LogWarning("No frames assigned!");
    }

    private void Update()
    {
        if (frames.Length == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= frameRate)
        {
            currentFrame = (currentFrame + 1) % frames.Length;
            spriteRenderer.sprite = frames[currentFrame];
            timer = 0f;
        }
    }
}
