using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Roach.Assets.Scripts.Core;

namespace Roach.Assets.Scripts.Core
{
    public class PauseButton : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject startPanel; // Reference to the start panel
        [SerializeField] private TextMeshProUGUI playButtonText; // Reference to the play button text

        private void Start()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }
        }

        private void OnPauseButtonClicked()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("PauseButton: GameManager.Instance is null!");
                return;
            }

            // Only allow pausing if game has started
            if (!GameManager.Instance.GameStarted)
                return;

            // Pause the game
            GameManager.Instance.PauseGame();

            // Show start panel with "Resume" text
            if (startPanel != null)
            {
                startPanel.SetActive(true);

                // Change button text to "Resume"
                if (playButtonText != null)
                {
                    playButtonText.text = "RESUME";
                }
            }
        }
    }
}