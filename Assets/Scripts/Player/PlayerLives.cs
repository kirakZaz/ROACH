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
        Debug.Log($"PlayerLives Awake: CurrentLives = {CurrentLives}");
    }

    private void Start()
    {
        OnLivesChanged?.Invoke(CurrentLives);
        Debug.Log($"PlayerLives Start: Invoked OnLivesChanged with {CurrentLives}");
    }

    public void LoseLife(int amount = 1)
    {
        Debug.Log($"LoseLife called: amount={amount}, CurrentLives before={CurrentLives}");

        if (CurrentLives <= 0)
        {
            Debug.Log("LoseLife: Already dead, returning");
            return;
        }

        CurrentLives = Mathf.Max(0, CurrentLives - amount);
        OnLivesChanged?.Invoke(CurrentLives);

        Debug.Log($"Player lost {amount} life! Remaining: {CurrentLives}");

        if (CurrentLives == 0)
        {
            Debug.Log("=== PLAYER DIED ===");
            Debug.Log($"OnGameOver subscribers: {OnGameOver?.GetInvocationList().Length ?? 0}");
            OnGameOver?.Invoke();

            Debug.Log($"GameManager.Instance exists: {GameManager.Instance != null}");
            if (GameManager.Instance != null)
            {
                Debug.Log("Calling GameManager.GameOver()");
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
