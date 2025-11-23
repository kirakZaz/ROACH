using UnityEngine;
using System.Collections.Generic;

namespace Roach.Assets.Scripts.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class Spike : MonoBehaviour
    {
        [Header("Spike Settings")]
        [SerializeField]
        [Tooltip("Amount of damage this spike deals")]
        private int damage = 1;

        [SerializeField]
        [Tooltip("Cooldown between damage (seconds)")]
        private float damageCooldown = 1.5f;

        public int Damage => damage;

        // Track when each object was last damaged
        private Dictionary<GameObject, float> lastDamageTime = new Dictionary<GameObject, float>();

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

        public bool CanDamage(GameObject target)
        {
            if (!lastDamageTime.ContainsKey(target))
                return true;

            float timeSinceLastDamage = Time.time - lastDamageTime[target];
            return timeSinceLastDamage >= damageCooldown;
        }

        public void RecordDamage(GameObject target)
        {
            lastDamageTime[target] = Time.time;
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