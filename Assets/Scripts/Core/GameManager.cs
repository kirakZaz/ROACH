using System;
using UnityEngine;

namespace Roach.Assets.Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private float gameDuration = 300f; // 5 minutes in seconds

        private float timeRemaining;
        private bool gameStarted = false;
        private bool gamePaused = false;

        // Events for UI
        public event Action<float> OnTimeChanged;

        public bool GameStarted => gameStarted;
        public bool GamePaused => gamePaused;
        public float TimeRemaining => timeRemaining;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            timeRemaining = gameDuration;
        }

        private void Start()
        {
            // Game starts paused
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (!gameStarted || gamePaused)
                return;

            // Countdown timer
            timeRemaining -= Time.deltaTime;
            OnTimeChanged?.Invoke(timeRemaining);

            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                // TODO: Game over logic later
                Debug.Log("Time's up!");
            }
        }

        public void StartGame()
        {
            gameStarted = true;
            gamePaused = false;
            Time.timeScale = 1f;
            OnTimeChanged?.Invoke(timeRemaining);
            Debug.Log("Game Started!");
        }

        public void PauseGame()
        {
            gamePaused = true;
            Time.timeScale = 0f;
            Debug.Log("Game Paused!");
        }

        public void ResumeGame()
        {
            gamePaused = false;
            Time.timeScale = 1f;
            Debug.Log("Game Resumed!");
        }
    }
}