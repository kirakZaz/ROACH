using System;
using Roach.Assets.Scripts.Core;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlayerLives : MonoBehaviour
{
    [SerializeField]
    private int maxLives = 3;

    public int CurrentLives { get; private set; }

    public event Action<int> OnLivesChanged;
    public event Action OnGameOver;

    private void Awake()
    {
        CurrentLives = Mathf.Max(0, maxLives);
    }

    private void Start()
    {
        OnLivesChanged?.Invoke(CurrentLives);
    }

    public void LoseLife(int amount = 1)
    {
        if (CurrentLives <= 0)
            return;

        CurrentLives = Mathf.Max(0, CurrentLives - amount);
        OnLivesChanged?.Invoke(CurrentLives);

        Debug.Log($"Player lost {amount} life! Remaining: {CurrentLives}");

        if (CurrentLives == 0)
        {
            Debug.Log("Player died! Calling GameManager.GameOver()");
            OnGameOver?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
            else
            {
                Debug.LogError("GameManager.Instance is NULL!");
            }
        }
    }

    public void ResetLives()
    {
        CurrentLives = Mathf.Max(0, maxLives);
        OnLivesChanged?.Invoke(CurrentLives);
    }
}
