using UnityEngine;

namespace Roach.Assets.Scripts.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class Spike : MonoBehaviour
    {
        [Header("Spike Settings")]
        [SerializeField]
        [Tooltip("Amount of damage this spike deals")]
        private int damage = 1;

        public int Damage => damage;

        private void Awake()
        {
            // Ensure spike collider is set as trigger
            var col = GetComponent<Collider2D>();
            if (col && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning(
                    "Spike collider was not set as trigger. Fixed automatically.",
                    this
                );
            }
        }

        private void OnDrawGizmos()
        {
            // Visualize spike danger zone in editor
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            var col = GetComponent<Collider2D>();
            if (col)
            {
                Gizmos.DrawCube(transform.position, col.bounds.size);
            }
        }
    }
}
