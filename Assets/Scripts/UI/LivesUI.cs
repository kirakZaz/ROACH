using UnityEngine;
using UnityEngine.UI;

public class LivesUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField]
    private Image[] lifeImages;

    [Header("Sprites")]
    [SerializeField]
    private Sprite fullHeart;

    [SerializeField]
    private Sprite emptyHeart;

    private PlayerLives playerLives;

    private void Start()
    {
        // Find PlayerLives component
        playerLives = FindObjectOfType<PlayerLives>();

        if (playerLives != null)
        {
            playerLives.OnLivesChanged += UpdateLivesDisplay;
            UpdateLivesDisplay(playerLives.CurrentLives);
        }
        else
        {
            Debug.LogError("PlayerLives not found in scene!");
        }
    }

    private void UpdateLivesDisplay(int currentLives)
    {
        if (lifeImages == null)
            return;

        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] != null)
            {
                lifeImages[i].sprite = (i < currentLives) ? fullHeart : emptyHeart;
            }
        }
    }

    private void OnDestroy()
    {
        if (playerLives != null)
        {
            playerLives.OnLivesChanged -= UpdateLivesDisplay;
        }
    }
}
