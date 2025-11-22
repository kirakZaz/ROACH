using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roach.Assets.Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField]
        private float gameDuration = 300f; // 5 minutes in seconds

        [SerializeField]
        private int requiredResources = 10;

        private float timeRemaining;
        private int collectedResources = 0;
        private bool gameStarted = false;
        private bool gamePaused = false;
        private bool gameEnded = false;

        // Events for UI
        public event Action<float> OnTimeChanged;
        public event Action<int, int> OnResourcesChanged; // current, required
        public event Action OnGameOver;
        public event Action OnLevelComplete;

        public bool GameStarted => gameStarted;
        public bool GamePaused => gamePaused;
        public float TimeRemaining => timeRemaining;
        public int CollectedResources => collectedResources;
        public int RequiredResources => requiredResources;

        public bool CanFinishLevel
        {
            get
            {
                // Automatically get total from WichettyBagUI
                if (WichettyBagUI.Instance != null)
                {
                    return WichettyBagUI.Instance.GetTotalItemCount() >= requiredResources;
                }

                // Fallback to simple counter
                return collectedResources >= requiredResources;
            }
        }

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
            if (!gameStarted || gamePaused || gameEnded)
                return;

            // Countdown timer
            timeRemaining -= Time.deltaTime;
            OnTimeChanged?.Invoke(timeRemaining);

            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                GameOver();
            }
        }

        public void StartGame()
        {
            gameStarted = true;
            gamePaused = false;
            gameEnded = false;
            Time.timeScale = 1f;
            OnTimeChanged?.Invoke(timeRemaining);
            OnResourcesChanged?.Invoke(collectedResources, requiredResources);
            Debug.Log("Game Started!");
        }

        public void CollectResource()
        {
            if (!gameStarted || gameEnded)
                return;

            collectedResources++;
            OnResourcesChanged?.Invoke(collectedResources, requiredResources);
            Debug.Log($"Collected! {collectedResources}/{requiredResources}");
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

        public void GameOver()
        {
            if (gameEnded)
                return;

            gameEnded = true;
            Time.timeScale = 0f;
            OnGameOver?.Invoke();
            Debug.Log("Game Over!");
        }

        public void LevelComplete()
        {
            if (gameEnded)
                return;

            gameEnded = true;
            Time.timeScale = 0f;
            OnLevelComplete?.Invoke();
            Debug.Log("Level Complete!");
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
