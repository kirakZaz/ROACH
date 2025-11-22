using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roach.Assets.Scripts.Core
{
    public class StartScreenUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField]
        private GameObject startPanel;

        [SerializeField]
        private GameObject gamePanel;

        [SerializeField]
        private GameObject gameOverPanel;

        [SerializeField]
        private GameObject winPanel;

        [Header("Start Panel")]
        [SerializeField]
        private Button playButton;

        [SerializeField]
        private TextMeshProUGUI playButtonText;

        [Header("Game Panel")]
        [SerializeField]
        private TextMeshProUGUI timerText;

        [SerializeField]
        private TextMeshProUGUI resourceText;

        [SerializeField]
        private Button pauseButton;

        [Header("Game Over Panel")]
        [SerializeField]
        private Button playAgainButton;

        [Header("Win Panel")]
        [SerializeField]
        private Button restartButton;

        private void Start()
        {
            // Setup buttons
            if (playButton)
                playButton.onClick.AddListener(OnPlayClicked);

            if (pauseButton)
                pauseButton.onClick.AddListener(OnPauseClicked);

            if (playAgainButton)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);

            if (restartButton)
                restartButton.onClick.AddListener(OnPlayAgainClicked);

            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTimeChanged += UpdateTimerUI;
                GameManager.Instance.OnResourcesChanged += UpdateResourceUI;
                GameManager.Instance.OnGameOver += ShowGameOverPanel;
                GameManager.Instance.OnLevelComplete += ShowWinPanel;
            }

            // Show start panel at the beginning
            ShowStartPanel();
        }

        private void ShowStartPanel()
        {
            SetActivePanel(startPanel);

            // Set button text based on game state
            if (playButtonText && GameManager.Instance != null)
            {
                playButtonText.text = GameManager.Instance.GameStarted ? "RESUME" : "PLAY";
            }
        }

        private void ShowGamePanel()
        {
            SetActivePanel(gamePanel);
        }

        private void ShowGameOverPanel()
        {
            Debug.Log("ShowGameOverPanel called!");
            SetActivePanel(gameOverPanel);
        }

        private void ShowWinPanel()
        {
            Debug.Log("ShowWinPanel called!");
            SetActivePanel(winPanel);
        }

        private void SetActivePanel(GameObject panel)
        {
            if (startPanel)
                startPanel.SetActive(panel == startPanel);
            if (gamePanel)
                gamePanel.SetActive(panel == gamePanel);
            if (gameOverPanel)
                gameOverPanel.SetActive(panel == gameOverPanel);
            if (winPanel)
                winPanel.SetActive(panel == winPanel);

            Debug.Log($"Active panel: {panel?.name ?? "none"}");
        }

        private void OnPlayClicked()
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.GameStarted && GameManager.Instance.GamePaused)
                {
                    GameManager.Instance.ResumeGame();
                }
                else
                {
                    GameManager.Instance.StartGame();
                }
            }

            ShowGamePanel(); // Hide start panel, show game panel
        }

        private void OnPauseClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame();
            }

            ShowStartPanel(); // Hide game panel, show start panel
        }

        private void OnPlayAgainClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }

        private void UpdateTimerUI(float timeRemaining)
        {
            if (timerText == null)
                return;

            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";

            // Color change when time is running out
            if (timeRemaining < 30f)
                timerText.color = Color.red;
            else if (timeRemaining < 60f)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.white;
        }

        private void UpdateResourceUI(int current, int required)
        {
            if (resourceText == null)
                return;

            // Get actual count from WichettyBagUI if available
            int actualCount = current;
            if (WichettyBagUI.Instance != null)
            {
                actualCount = WichettyBagUI.Instance.GetTotalItemCount();
            }

            resourceText.text = $"Resources: {actualCount}/{required}";

            // Color change when goal is reached
            if (actualCount >= required)
                resourceText.color = Color.green;
            else
                resourceText.color = Color.white;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTimeChanged -= UpdateTimerUI;
                GameManager.Instance.OnResourcesChanged -= UpdateResourceUI;
                GameManager.Instance.OnGameOver -= ShowGameOverPanel;
                GameManager.Instance.OnLevelComplete -= ShowWinPanel;
            }
        }
    }
}
