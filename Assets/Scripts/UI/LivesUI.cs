using UnityEngine;
using UnityEngine.UI;

public class LivesUI : MonoBehaviour
{
    [SerializeField]
    private PlayerLives playerLives; // drag Player here

    [SerializeField]
    private Image[] heartImages; // Heart1..Heart3

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
            bool on = i < current;
            heartImages[i].color = on ? Color.red : new Color(1f, 1f, 1f, 0.25f);
        }
    }
}
