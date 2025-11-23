using UnityEngine;
using UnityEngine.UI;

public class LivesUI : MonoBehaviour
{
    [SerializeField]
    private PlayerLives playerLives;

    [SerializeField]
    private Image[] heartImages; // 3 сердечка

    [SerializeField]
    private int heartPixelSize = 8;

    [SerializeField]
    private int heartPPU = 64;

    private void Awake()
    {
        // Создаём спрайт сердечка
        var heartSprite = HeartSpriteGenerator.CreatePixelHeartSprite(heartPixelSize, heartPPU);
        
        // Применяем ко всем изображениям
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (!heartImages[i])
                continue;
            
            heartImages[i].sprite = heartSprite;
            heartImages[i].preserveAspect = true;
            heartImages[i].color = Color.red; // полные сердечки
        }
    }

    private void OnEnable()
    {
        if (!playerLives)
        {
            playerLives = FindObjectOfType<PlayerLives>();
            if (!playerLives)
            {
                Debug.LogError("LivesUI: PlayerLives not found in scene!", this);
                return;
            }
        }

        playerLives.OnLivesChanged += UpdateHearts;
        UpdateHearts(playerLives.CurrentLives);
    }

    private void OnDisable()
    {
        if (playerLives)
            playerLives.OnLivesChanged -= UpdateHearts;
    }

    private void UpdateHearts(int current)
    {
        Debug.Log($"LivesUI: Updating hearts to {current}");
        
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (!heartImages[i])
                continue;
            
            bool on = i < current;
            heartImages[i].color = on ? Color.red : new Color(1f, 1f, 1f, 0.25f);
        }
    }
}