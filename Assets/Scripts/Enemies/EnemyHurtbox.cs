using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roach.Assets.Scripts.Enemies
{
    public class EnemyHurtbox : MonoBehaviour
    {
        [SerializeField]
        private string playerTag = "Player";

        [SerializeField]
        private float reloadDelay = 0.15f;
        private bool triggered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered)
                return;
            if (!other.CompareTag(playerTag))
                return;

            triggered = true;

            // // Preferred: fade + reload if GameManager exists
            // if (GameManager.Instance)
            // {
            //     GameManager.Instance.KillPlayer();
            // }
            // else
            // {
                Invoke(nameof(Reload), reloadDelay);
            // }
        }

        private void Reload()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
