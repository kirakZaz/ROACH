using UnityEngine;

namespace Roach.Assets.Scripts.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string playerTag = "Player";
        
        [Header("Audio (optional)")]
        [SerializeField] private AudioClip collectSound;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col)
                col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag))
                return;

            // Tell GameManager we collected a resource
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CollectResource();
            }

            // Play sound if available
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            // Destroy the collectible
            Destroy(gameObject);
        }
    }
}