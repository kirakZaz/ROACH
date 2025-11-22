using TMPro;
using UnityEngine;

namespace Roach.Assets.Scripts.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        private string playerTag = "Player";

        [SerializeField]
        private KeyCode interactKey = KeyCode.T;

        [Header("UI")]
        [SerializeField]
        private GameObject promptUI;

        [SerializeField]
        private TextMeshProUGUI promptText;

        [Header("Messages")]
        [SerializeField]
        private string canFinishMessage = "Press T to finish level";

        [SerializeField]
        private string needResourcesMessage = "Collect {0} more resources!";

        private bool playerNearby = false;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col)
                col.isTrigger = true;
        }

        private void Start()
        {
            // Hide prompt at start
            if (promptUI)
                promptUI.SetActive(false);
        }

        private void Update()
        {
            if (!playerNearby || GameManager.Instance == null)
                return;

            // Update prompt message
            UpdatePromptMessage();

            // Check for interaction only if player can finish
            if (Input.GetKeyDown(interactKey) && GameManager.Instance.CanFinishLevel)
            {
                FinishLevel();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                playerNearby = true;
                ShowPrompt();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(playerTag))
            {
                playerNearby = false;
                HidePrompt();
            }
        }

        private void ShowPrompt()
        {
            if (promptUI)
            {
                promptUI.SetActive(true);
            }

            UpdatePromptMessage();
        }

        private void UpdatePromptMessage()
        {
            if (promptText == null || GameManager.Instance == null)
                return;

            if (GameManager.Instance.CanFinishLevel)
            {
                promptText.text = canFinishMessage;
                promptText.color = Color.green;
            }
            else
            {
                int totalCollected = 0;
                if (WichettyBagUI.Instance != null)
                {
                    totalCollected = WichettyBagUI.Instance.GetTotalItemCount();
                }

                int needed = GameManager.Instance.RequiredResources - totalCollected;
                promptText.text = string.Format(needResourcesMessage, needed);
                promptText.color = Color.yellow;
            }
        }

        private void HidePrompt()
        {
            if (promptUI)
            {
                promptUI.SetActive(false);
            }
        }

        private void FinishLevel()
        {
            HidePrompt();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LevelComplete();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            var col = GetComponent<Collider2D>();
            if (col)
            {
                Gizmos.DrawWireCube(transform.position, col.bounds.size);
            }
        }
    }
}
