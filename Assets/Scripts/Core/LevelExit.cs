using UnityEngine;
using TMPro;

namespace Roach.Assets.Scripts.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        
        [Header("UI")]
        [SerializeField] private GameObject promptUI;
        [SerializeField] private TextMeshProUGUI promptText;
        
        private bool playerNearby = false;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col)
                col.isTrigger = true;
                
            // Hide prompt at start
            if (promptUI)
                promptUI.SetActive(false);
        }

        private void Update()
        {
            if (!playerNearby || GameManager.Instance == null)
                return;

            // Check for interaction
            if (Input.GetKeyDown(interactKey))
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
            
            if (promptText)
            {
                promptText.text = $"Press {interactKey} to finish level";
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