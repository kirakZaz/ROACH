using UnityEngine;
using UnityEngine.UI;

public class LivesUI : MonoBehaviour
{
    [SerializeField]
    private PlayerLives playerLives; // drag your Player here

    [SerializeField]
    private Image[] heartImages; // Heart1..Heart3

    [SerializeField]
    private int heartSize = 64; // pixels

    [SerializeField]
    private int aaSamples = 4; // 1..5 (edge smoothness)

    [SerializeField]
    private int heartPixelSize = 8; // 6..16 — подгони под размер UI

    [SerializeField]
    private int heartPPU = 64;

    private void Awake()
    {
        var heartSprite = HeartSpriteGenerator.CreatePixelHeartSprite(heartPixelSize, heartPPU);
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (!heartImages[i])
                continue;
            if (!heartImages[i].sprite)
            {
                heartImages[i].sprite = heartSprite;
                heartImages[i].preserveAspect = true;
            }
            heartImages[i].color = Color.red; // full heart
        }
    }

    private void OnEnable()
    {
        if (!playerLives)
        {
            Debug.LogError("LivesUI: PlayerLives is not assigned.", this);
            return;
        }

        playerLives.OnLivesChanged += UpdateHearts;
        UpdateHearts(playerLives.CurrentLives); // sync immediately
    }

    private void OnDisable()
    {
        if (playerLives)
            playerLives.OnLivesChanged -= UpdateHearts;
    }

    private void UpdateHearts(int current)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (!heartImages[i])
                continue;
            bool on = i < current;

            // keep the red tint for full hearts, fade out for empty
            heartImages[i].color = on ? Color.red : new Color(1f, 1f, 1f, 0.25f);
        }
    }
}
