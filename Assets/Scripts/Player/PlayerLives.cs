using System;
using UnityEngine;

[DefaultExecutionOrder(-100)] // initialize before UI
public class PlayerLives : MonoBehaviour
{
    [SerializeField] private int maxLives = 3;
    public int CurrentLives { get; private set; }

    public event Action<int> OnLivesChanged;
    public event Action OnGameOver;

    private void Awake()
    {
        CurrentLives = Mathf.Max(0, maxLives);
        OnLivesChanged?.Invoke(CurrentLives); // make UI correct at start
    }

    public void LoseLife(int amount = 1)
    {
        if (CurrentLives <= 0) return;

        CurrentLives = Mathf.Max(0, CurrentLives - amount);
        OnLivesChanged?.Invoke(CurrentLives);

        if (CurrentLives == 0)
            OnGameOver?.Invoke();
    }

    public void ResetLives()
    {
        CurrentLives = Mathf.Max(0, maxLives);
        OnLivesChanged?.Invoke(CurrentLives);
    }
}
