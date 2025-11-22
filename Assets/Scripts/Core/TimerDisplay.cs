using UnityEngine;
using TMPro;
using Roach.Assets.Scripts.Core;


namespace Roach.Assets.Scripts.Core
{
    public class TimerDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                // Subscribe to timer updates
                GameManager.Instance.OnTimeChanged += UpdateTimerDisplay;
                
                // Display initial time
                UpdateTimerDisplay(GameManager.Instance.TimeRemaining);
            }
            else
            {
                Debug.LogError("TimerDisplay: GameManager.Instance is null!");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe when destroyed
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTimeChanged -= UpdateTimerDisplay;
            }
        }

        private void UpdateTimerDisplay(float timeRemaining)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            
            if (timerText != null)
            {
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }
    }
}