using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Roach.Assets.Scripts.Core
{
    public class StartScreenUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI playButtonText;

        private void Start()
        {
            // Setup play button
            if (playButton)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            // Show start panel initially
            if (startPanel)
            {
                startPanel.SetActive(true);
            }

            // Set initial button text to "PLAY"
            if (playButtonText)
            {
                playButtonText.text = "PLAY";
            }
        }

        private void OnPlayClicked()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("StartScreenUI: GameManager.Instance is null!");
                return;
            }

            // Check if we're resuming or starting new
            if (GameManager.Instance.GamePaused)
            {
                // Resume game
                GameManager.Instance.ResumeGame();
            }
            else
            {
                // Start new game
                GameManager.Instance.StartGame();
                
                // Tell camera to start following
                var cameraFollow = Camera.main.GetComponent<CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.gameStarted = true;
                }
            }

            // Hide start panel
            if (startPanel)
            {
                startPanel.SetActive(false);
            }

            // Reset button text to "PLAY" for next time
            if (playButtonText)
            {
                playButtonText.text = "PLAY";
            }
        }
    }
}